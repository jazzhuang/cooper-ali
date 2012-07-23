using System;
using System.Collections.Generic;
using System.Linq;
using AliCooper.Model.Accounts;
using Castle.Facilities.NHibernateIntegration;
using CodeSharp.Core;
using CodeSharp.Core.Services;
using CodeSharp.Core.Utils;
using Cooper.Model;
using Cooper.Model.Accounts;
using Cooper.Model.Tasks;
using Cooper.Sync;
using Microsoft.Exchange.WebServices.Data;
using NUnit.Framework;
using CooperTask = Cooper.Model.Tasks.Task;

namespace AliCooper.Sync.Test
{
    /// <summary>
    /// 数据同步处理器接口定义，该处理器负责实现整个同步过程
    /// </summary>
    public interface ISyncProcesser
    {
        /// <summary>
        /// 根据指定的同步服务同步数据
        /// </summary>
        void SyncTasksAndContacts(int connectionId, IList<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>> taskSyncServices, IList<ISyncService<ContactSyncData, ISyncData, ContactSyncResult>> contactSyncServices);
        /// <summary>
        /// 根据默认支持的同步服务来同步任务与联系人
        /// </summary>
        void SyncTasksAndContacts(int connectionId);
    }

    /// <summary>
    /// 数据同步处理器实现类
    /// </summary>
    [Component]
    public class SyncProcesser : ISyncProcesser
    {
        #region Private Members

        private string _exchangeServer;
        private ILog _logger;
        private ISessionManager _sessionManager;
        private Account _account;
        private ArkConnection _arkConnection;
        private AliyunMailConnection _aliyunMailConnection;

        private ITaskService _taskService;
        private IAccountService _accountService;
        private IAccountConnectionService _accountConnectionService;
        private IAccountHelper _accountHelper;
        private IAliyunDao _aliyunDataAccess;
        private IMicrosoftExchangeServiceProvider _microsoftExchangeServiceProvider;
        private IList<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>> _defaultTaskSyncServices;
        private IList<ISyncService<ContactSyncData, ISyncData, ContactSyncResult>> _defaultContactSyncServices;

        #endregion

        #region Constructors

        public SyncProcesser(string exchangeWebService, IAliyunDao aliyunDataAccess)
        {
            _exchangeServer = exchangeWebService;
            _aliyunDataAccess = aliyunDataAccess;

            //初始化同步锁
            DependencyResolver.Resolve<ILockHelper>().Init<Account>();
            DependencyResolver.Resolve<ILockHelper>().Init<ArkConnection>();
            DependencyResolver.Resolve<ILockHelper>().Init<AliyunMailConnection>();

            _logger = DependencyResolver.Resolve<ILoggerFactory>().Create(GetType());
            _sessionManager = DependencyResolver.Resolve<ISessionManager>();

            _accountHelper = DependencyResolver.Resolve<IAccountHelper>();
            _accountService = DependencyResolver.Resolve<IAccountService>();
            _accountConnectionService = DependencyResolver.Resolve<IAccountConnectionService>();
            _taskService = DependencyResolver.Resolve<ITaskService>();

            _microsoftExchangeServiceProvider = DependencyResolver.Resolve<IMicrosoftExchangeServiceProvider>();

            _defaultTaskSyncServices = new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>();

            _defaultTaskSyncServices.Add(DependencyResolver.Resolve<IExchangeTaskSyncService>());
            _defaultTaskSyncServices.Add(DependencyResolver.Resolve<IExchangeCalendarEventSyncService>());
            _defaultTaskSyncServices.Add(DependencyResolver.Resolve<IAliyunCalendarEventSyncService>());

            _defaultContactSyncServices = new List<ISyncService<ContactSyncData, ISyncData, ContactSyncResult>>();
            //暂时不对联系人进行同步
            //_defaultContactSyncServices.Add(DependencyResolver.Resolve<IExchangeContactSyncService>());
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 根据默认支持的同步服务来同步任务与联系人
        /// </summary>
        public void SyncTasksAndContacts(int connectionId)
        {
            SyncTasksAndContacts(connectionId, _defaultTaskSyncServices, _defaultContactSyncServices);
        }
        /// <summary>
        /// 根据指定的同步服务同步数据
        /// </summary>
        public void SyncTasksAndContacts(int connectionId, IList<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>> taskSyncServices, IList<ISyncService<ContactSyncData, ISyncData, ContactSyncResult>> contactSyncServices)
        {
            _logger.InfoFormat("开始同步任务及联系人, connectionId:{0}", connectionId);

            try
            {
                InitializeAccountAndConnection(connectionId);
                SyncTasks(taskSyncServices);
                SyncContacts(contactSyncServices);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
            finally
            {
                _logger.InfoFormat("结束同步任务及联系人, connectionId:{0}", connectionId);
            }
        }

        #endregion

        #region Helper Methods

        #region 处理同步结果相关函数

        /// <summary>
        /// 处理任务同步结果
        /// </summary>
        /// <param name="taskSyncResult">任务同步结果</param>
        /// <param name="account">任务所属账号</param>
        private void ProcessTaskSyncResult(TaskSyncResult taskSyncResult, Account account)
        {
            //处理本地任务
            ProcessLocalTaskDatas(taskSyncResult, account);

            var exchangeTasksToCreate = taskSyncResult.SyncDatasToCreate.Where(x => x.GetType() == typeof(ExchangeTaskSyncData)).Cast<ExchangeTaskSyncData>();
            var exchangeTasksToUpdate = taskSyncResult.SyncDatasToUpdate.Where(x => x.GetType() == typeof(ExchangeTaskSyncData)).Cast<ExchangeTaskSyncData>();
            var exchangeTasksToDelete = taskSyncResult.SyncDatasToDelete.Where(x => x.GetType() == typeof(ExchangeTaskSyncData)).Cast<ExchangeTaskSyncData>();
            var exchangeCalendarEventsToCreate = taskSyncResult.SyncDatasToCreate.Where(x => x.GetType() == typeof(ExchangeCalendarEventSyncData)).Cast<ExchangeCalendarEventSyncData>();
            var exchangeCalendarEventsToUpdate = taskSyncResult.SyncDatasToUpdate.Where(x => x.GetType() == typeof(ExchangeCalendarEventSyncData)).Cast<ExchangeCalendarEventSyncData>();
            var exchangeCalendarEventsToDelete = taskSyncResult.SyncDatasToDelete.Where(x => x.GetType() == typeof(ExchangeCalendarEventSyncData)).Cast<ExchangeCalendarEventSyncData>();

            if (exchangeTasksToCreate.Count() > 0 || exchangeTasksToUpdate.Count() > 0 || exchangeTasksToDelete.Count() > 0 || exchangeCalendarEventsToCreate.Count() > 0 || exchangeCalendarEventsToUpdate.Count() > 0 || exchangeCalendarEventsToDelete.Count() > 0)
            {
                var credential = GetExchangeUserCredential();
                var exchangeService = _microsoftExchangeServiceProvider.GetMicrosoftExchangeService(credential);

                //处理Exchange Task
                ProcessExchangeTaskDatas(account, exchangeService, exchangeTasksToCreate, exchangeTasksToUpdate, exchangeTasksToDelete);
                //处理Exchange Calendar Event
                ProcessExchangeCalendarEventDatas(account, credential, exchangeService, exchangeCalendarEventsToCreate, exchangeCalendarEventsToUpdate, exchangeCalendarEventsToDelete);
            }

            var aliyunMailCalendarEventsToCreate = taskSyncResult.SyncDatasToCreate.Where(x => x.GetType() == typeof(AliyunCalendarEventSyncData)).Cast<AliyunCalendarEventSyncData>();
            var aliyunMailCalendarEventsToUpdate = taskSyncResult.SyncDatasToUpdate.Where(x => x.GetType() == typeof(AliyunCalendarEventSyncData)).Cast<AliyunCalendarEventSyncData>();
            var aliyunMailCalendarEventsToDelete = taskSyncResult.SyncDatasToDelete.Where(x => x.GetType() == typeof(AliyunCalendarEventSyncData)).Cast<AliyunCalendarEventSyncData>();

            if (aliyunMailCalendarEventsToCreate.Count() > 0 || aliyunMailCalendarEventsToUpdate.Count() > 0 || aliyunMailCalendarEventsToDelete.Count() > 0)
            {
                ProcessAliyunEmailCalendarEventDatas(account, aliyunMailCalendarEventsToCreate, aliyunMailCalendarEventsToUpdate, aliyunMailCalendarEventsToDelete);
            }
        }
        /// <summary>
        /// 处理联系人同步结果
        /// </summary>
        /// <param name="contactSyncResult">联系人同步结果</param>
        /// <param name="account">联系人所属账号</param>
        private void ProcessContactSyncResult(ContactSyncResult contactSyncResult, Account account)
        {
            //处理本地联系人
            ProcessLocalContactDatas(contactSyncResult, account);

            var exchangeContactsToCreate = contactSyncResult.SyncDatasToCreate.Where(x => x.GetType() == typeof(ExchangeContactSyncData)).Cast<ExchangeContactSyncData>();
            var exchangeContactsToUpdate = contactSyncResult.SyncDatasToUpdate.Where(x => x.GetType() == typeof(ExchangeContactSyncData)).Cast<ExchangeContactSyncData>();
            var exchangeContactsToDelete = contactSyncResult.SyncDatasToDelete.Where(x => x.GetType() == typeof(ExchangeContactSyncData)).Cast<ExchangeContactSyncData>();

            //处理Exchange联系人
            if (exchangeContactsToCreate.Count() > 0 || exchangeContactsToUpdate.Count() > 0 || exchangeContactsToDelete.Count() > 0)
            {
                var credential = GetExchangeUserCredential();
                var exchangeService = _microsoftExchangeServiceProvider.GetMicrosoftExchangeService(credential);
                ProcessExchangeContactDatas(account, exchangeService, exchangeContactsToCreate, exchangeContactsToUpdate, exchangeContactsToDelete);
            }
        }

        #region 处理本地任务数据的同步结果

        /// <summary>
        /// 持久化本地需要增删改的任务
        /// </summary>
        private void ProcessLocalTaskDatas(TaskSyncResult taskSyncResult, Account account)
        {
            if (taskSyncResult.LocalDatasToCreate.Count() == 0 && taskSyncResult.LocalDatasToUpdate.Count() == 0 && taskSyncResult.LocalDatasToDelete.Count() == 0)
            {
                return;
            }
            //处理在本地需要新增的任务
            foreach (var taskData in taskSyncResult.LocalDatasToCreate)
            {
                //TODO， 以后下面这三步需要放在一个Transaction中实现

                //创建任务
                CooperTask task = new CooperTask(account);

                task.SetSubject(taskData.Subject ?? string.Empty);
                task.SetBody(FormatTaskBody(taskData.Body));
                task.SetDueTime(FormatTaskDueTime(taskData.DueTime));
                if (taskData.IsCompleted)
                {
                    task.MarkAsCompleted();
                }
                else
                {
                    task.MarkAsInCompleted();
                }
                task.SetPriority(ConvertToPriority(taskData.Priority));

                _taskService.Create(task);

                //任务创建后更新最后更新时间，更新为和这条任务关联的外部系统任务的最后更新时间
                task.SetLastUpdateTime(taskData.LastUpdateLocalTime);
                _taskService.Update(task);

                //创建同步信息
                if (taskData.IsFromDefault)
                {
                    SyncInfo syncInfo = new SyncInfo();
                    syncInfo.AccountId = account.ID;
                    syncInfo.LocalDataId = task.ID.ToString();
                    syncInfo.SyncDataId = taskData.SyncId;
                    syncInfo.SyncDataType = taskData.SyncType;
                    InsertSyncInfo(syncInfo);
                }
            }

            //处理在本地需要更新的任务
            foreach (var taskData in taskSyncResult.LocalDatasToUpdate)
            {
                //更新任务
                CooperTask task = _taskService.GetTask(long.Parse(taskData.Id));

                task.SetSubject(taskData.Subject ?? string.Empty);
                task.SetBody(FormatTaskBody(taskData.Body));
                task.SetDueTime(FormatTaskDueTime(taskData.DueTime));
                if (taskData.IsCompleted)
                {
                    task.MarkAsCompleted();
                }
                else
                {
                    task.MarkAsInCompleted();
                }
                task.SetPriority(ConvertToPriority(taskData.Priority));

                _taskService.Update(task);

                //任务更新后更新最后更新时间，更新为和这条任务关联的外部系统任务的最后更新时间
                task.SetLastUpdateTime(taskData.LastUpdateLocalTime);
                _taskService.Update(task);
            }

            //暂时去掉删除本地数据的功能
            ////处理在本地需要删除的任务
            //foreach (var taskData in taskSyncResult.LocalDatasToDelete)
            //{
            //    Cooper.Model.Tasks.Task task = _taskService.GetTask(long.Parse(taskData.Id));
            //    _taskService.Delete(task);

            //    //删除同步信息
            //    SyncInfo syncInfo = new SyncInfo();
            //    syncInfo.AccountId = account.ID;
            //    syncInfo.LocalDataId = task.ID.ToString();
            //    syncInfo.SyncDataId = taskData.SyncId;
            //    syncInfo.SyncDataType = taskData.SyncType;
            //    DeleteSyncInfo(syncInfo);
            //}
        }
        /// <summary>
        /// 持久化本地需要增删改的联系人
        /// </summary>
        private void ProcessLocalContactDatas(ContactSyncResult contactSyncResult, Account account)
        {
            //TODO，由于联系人功能暂时还未做好，所以这里先不实现

            if (contactSyncResult.LocalDatasToCreate.Count() == 0 && contactSyncResult.LocalDatasToUpdate.Count() == 0 && contactSyncResult.LocalDatasToDelete.Count() == 0)
            {
                return;
            }
            //处理在本地需要新增的联系人
            foreach (var contactData in contactSyncResult.LocalDatasToCreate)
            {

            }

            //处理在本地需要更新的联系人
            foreach (var contactData in contactSyncResult.LocalDatasToUpdate)
            {

            }

            //处理在本地需要删除的联系人
            foreach (var contactData in contactSyncResult.LocalDatasToDelete)
            {

            }
        }

        private string FormatTaskBody(string bodyToFormat)
        {
            string formattedBody = bodyToFormat;

            if (formattedBody == null)
            {
                formattedBody = string.Empty;
            }
            else if (formattedBody.Length > 5000)
            {
                formattedBody = formattedBody.Substring(5000);
            }

            return formattedBody;
        }
        private DateTime? FormatTaskDueTime(DateTime? dueTimeToFormat)
        {
            DateTime? formattedDueTime = dueTimeToFormat;

            if (formattedDueTime != null && formattedDueTime.Value < new DateTime(1753, 1, 1))
            {
                formattedDueTime = null;
            }

            return formattedDueTime;
        }

        #endregion

        #region 处理Exchange相关数据的同步结果

        /// <summary>
        /// 持久化Exchange Task
        /// </summary>
        private void ProcessExchangeTaskDatas(Account account, ExchangeService exchangeService, IEnumerable<ExchangeTaskSyncData> datasToCreate, IEnumerable<ExchangeTaskSyncData> datasToUpdate, IEnumerable<ExchangeTaskSyncData> datasToDelete)
        {
            foreach (var dataToCreate in datasToCreate)
            {
                CreateExchangeTask(account, dataToCreate, exchangeService);
            }

            foreach (var dataToUpdate in datasToUpdate)
            {
                UpdateExchangeTask(dataToUpdate, exchangeService);
            }

            foreach (var dataToDelete in datasToDelete)
            {
                DeleteExchangeTask(account, dataToDelete, exchangeService);
            }
        }
        /// <summary>
        /// 持久化Exchange Calendar Event
        /// </summary>
        private void ProcessExchangeCalendarEventDatas(Account account, ExchangeUserCredential credential, ExchangeService exchangeService, IEnumerable<ExchangeCalendarEventSyncData> datasToCreate, IEnumerable<ExchangeCalendarEventSyncData> datasToUpdate, IEnumerable<ExchangeCalendarEventSyncData> datasToDelete)
        {
            bool isDefaultCalendarExist = false;
            var defaultCalendar = DependencyResolver.Resolve<IExchangeCalendarEventSyncDataService>().GetDefaultCalendarFolder(credential, out isDefaultCalendarExist);
            foreach (var dataToCreate in datasToCreate)
            {
                CreateExchangeCalendarEvent(account, dataToCreate, defaultCalendar, exchangeService);
            }

            foreach (var dataToUpdate in datasToUpdate)
            {
                UpdateExchangeCalendarEvent(dataToUpdate, exchangeService);
            }

            foreach (var dataToDelete in datasToDelete)
            {
                DeleteExchangeCalendarEvent(account, dataToDelete, exchangeService);
            }
        }
        /// <summary>
        /// 持久化Exchange Contact
        /// </summary>
        private void ProcessExchangeContactDatas(Account account, ExchangeService exchangeService, IEnumerable<ExchangeContactSyncData> datasToCreate, IEnumerable<ExchangeContactSyncData> datasToUpdate, IEnumerable<ExchangeContactSyncData> datasToDelete)
        {
            foreach (var dataToCreate in datasToCreate)
            {
                CreateExchangeContact(account, dataToCreate, exchangeService);
            }

            foreach (var dataToUpdate in datasToUpdate)
            {
                UpdateExchangeContact(dataToUpdate, exchangeService);
            }

            foreach (var dataToDelete in datasToDelete)
            {
                DeleteExchangeContact(account, dataToDelete, exchangeService);
            }
        }

        #endregion

        #region 处理Aliyun邮箱相关数据的同步结果

        /// <summary>
        /// 持久化Aliyun Email Calendar Event
        /// </summary>
        private void ProcessAliyunEmailCalendarEventDatas(Account account, IEnumerable<AliyunCalendarEventSyncData> datasToCreate, IEnumerable<AliyunCalendarEventSyncData> datasToUpdate, IEnumerable<AliyunCalendarEventSyncData> datasToDelete)
        {
            var userEmail = GetAliyunUserMail();

            foreach (var dataToCreate in datasToCreate)
            {
                CreateAliyunEmailCalendarEvent(account, userEmail, dataToCreate);
            }

            foreach (var dataToUpdate in datasToUpdate)
            {
                UpdateAliyunEmailCalendarEvent(userEmail, dataToUpdate);
            }

            foreach (var dataToDelete in datasToDelete)
            {
                DeleteAliyunEmailCalendarEvent(account, userEmail, dataToDelete);
            }
        }

        #endregion

        #region 持久化Exchange相关数据的函数

        /// <summary>
        /// 创建Exchange Task
        /// </summary>
        private void CreateExchangeTask(Account account, ExchangeTaskSyncData taskData, ExchangeService exchangeService)
        {
            if (SafeAction("CreateExchangeTask", () => taskData.ExchangeTask.Save()))
            {
                var task = GetExchangeTask(exchangeService, taskData.ExchangeTask.Id.UniqueId);
                _logger.InfoFormat("新增Exchange任务#{0}|{1}|{2}", task.Id.UniqueId, taskData.Subject, account.ID);

                //更新任务最后更新时间，确保与Exchange Task的最后更新时间一致
                UpdateTaskLastUpdateTime(long.Parse(taskData.SyncId), task.LastModifiedTime);

                //创建同步信息
                SyncInfo syncInfo = new SyncInfo();
                syncInfo.AccountId = account.ID;
                syncInfo.LocalDataId = taskData.SyncId;
                syncInfo.SyncDataId = task.Id.UniqueId;
                syncInfo.SyncDataType = taskData.SyncType;
                InsertSyncInfo(syncInfo);
            }
        }
        /// <summary>
        /// 创建Exchange Calendar Event
        /// </summary>
        private void CreateExchangeCalendarEvent(Account account, ExchangeCalendarEventSyncData calendarEventData, Folder defaultCalendar, ExchangeService exchangeService)
        {
            if (SafeAction("CreateExchangeCalendarEvent", () => calendarEventData.ExchangeCalendarEvent.Save(defaultCalendar.Id)))
            {
                var calendarEvent = GetExchangeCalendarEvent(exchangeService, calendarEventData.ExchangeCalendarEvent.Id.UniqueId);
                _logger.InfoFormat("新增Exchange日历事件#{0}|{1}|{2}", calendarEvent.Id.UniqueId, calendarEventData.Subject, account.ID);

                //更新任务最后更新时间，确保与Exchange Calendar Event的最后更新时间一致
                UpdateTaskLastUpdateTime(long.Parse(calendarEventData.SyncId), calendarEvent.LastModifiedTime);

                //创建同步信息
                if (defaultCalendar.DisplayName == ExchangeSyncSettings.DefaultCalendarName)
                {
                    SyncInfo syncInfo = new SyncInfo();
                    syncInfo.AccountId = account.ID;
                    syncInfo.LocalDataId = calendarEventData.SyncId;
                    syncInfo.SyncDataId = calendarEvent.Id.UniqueId;
                    syncInfo.SyncDataType = calendarEventData.SyncType;
                    InsertSyncInfo(syncInfo);
                }
            }
        }
        /// <summary>
        /// 创建Exchange Contact
        /// </summary>
        private void CreateExchangeContact(Account account, ExchangeContactSyncData contactData, ExchangeService exchangeService)
        {
            if (SafeAction("CreateExchangeContact", () => contactData.ExchangeContact.Save()))
            {
                var contact = GetExchangeContact(exchangeService, contactData.ExchangeContact.Id.UniqueId);
                _logger.InfoFormat("新增Exchange联系人#{0}|{1}|{2}", contact.Id.UniqueId, contactData.Subject, account.ID);

                //更新联系人最后更新时间，确保与Exchange Contact的最后更新时间一致
                UpdateContactLastUpdateTime(int.Parse(contactData.SyncId), contact.LastModifiedTime);

                //创建同步信息
                SyncInfo syncInfo = new SyncInfo();
                syncInfo.AccountId = account.ID;
                syncInfo.LocalDataId = contactData.SyncId;
                syncInfo.SyncDataId = contact.Id.UniqueId;
                syncInfo.SyncDataType = contactData.SyncType;
                InsertSyncInfo(syncInfo);
            }
        }

        /// <summary>
        /// 更新Exchange Task
        /// </summary>
        private void UpdateExchangeTask(ExchangeTaskSyncData taskData, ExchangeService exchangeService)
        {
            SafeAction("UpdateExchangeTask", () =>
            {
                taskData.ExchangeTask.Update(ConflictResolutionMode.AlwaysOverwrite);
                _logger.InfoFormat("更新Exchange任务#{0}|{1}|{2}", taskData.Id, taskData.Subject, _account.ID);

                var task = GetExchangeTask(exchangeService, taskData.ExchangeTask.Id.UniqueId);

                //更新任务最后更新时间，确保与Exchange Task的最后更新时间一致
                UpdateTaskLastUpdateTime(long.Parse(taskData.SyncId), task.LastModifiedTime);
            });
        }
        /// <summary>
        /// 更新Exchange Calendar Event
        /// </summary>
        private void UpdateExchangeCalendarEvent(ExchangeCalendarEventSyncData calendarEventData, ExchangeService exchangeService)
        {
            SafeAction("UpdateExchangeCalendarEvent", () =>
            {
                calendarEventData.ExchangeCalendarEvent.Update(ConflictResolutionMode.AlwaysOverwrite);
                _logger.InfoFormat("更新Exchange日历事件#{0}|{1}|{2}", calendarEventData.Id, calendarEventData.Subject, _account.ID);

                var appointment = GetExchangeCalendarEvent(exchangeService, calendarEventData.ExchangeCalendarEvent.Id.UniqueId);

                //更新任务最后更新时间，确保与Exchange Calendar Event的最后更新时间一致
                UpdateTaskLastUpdateTime(long.Parse(calendarEventData.SyncId), appointment.LastModifiedTime);
            });
        }
        /// <summary>
        /// 更新Exchange Contact
        /// </summary>
        private void UpdateExchangeContact(ExchangeContactSyncData contactData, ExchangeService exchangeService)
        {
            SafeAction("UpdateExchangeContact", () =>
            {
                contactData.ExchangeContact.Update(ConflictResolutionMode.AlwaysOverwrite);
                _logger.InfoFormat("更新Exchange联系人#{0}|{1}|{2}", contactData.Id, contactData.Subject, _account.ID);

                var contact = GetExchangeContact(exchangeService, contactData.ExchangeContact.Id.UniqueId);

                //更新联系人最后更新时间，确保与Exchange Contact的最后更新时间一致
                UpdateContactLastUpdateTime(int.Parse(contactData.SyncId), contact.LastModifiedTime);
            });
        }

        /// <summary>
        /// 删除Exchange Task
        /// </summary>
        private void DeleteExchangeTask(Account account, ExchangeTaskSyncData taskData, ExchangeService exchangeService)
        {
            if (SafeAction("DeleteExchangeTask", () =>
            {
                var task = Microsoft.Exchange.WebServices.Data.Task.Bind(exchangeService, taskData.ExchangeTask.Id.UniqueId);
                if (task != null)
                {
                    task.Delete(DeleteMode.HardDelete);
                    _logger.InfoFormat("删除Exchange任务#{0}|{1}|{2}", taskData.Id, taskData.Subject, _account.ID);
                }
            }))
            {
                //删除同步信息
                SyncInfo syncInfo = new SyncInfo();
                syncInfo.AccountId = account.ID;
                syncInfo.LocalDataId = taskData.SyncId;
                syncInfo.SyncDataId = taskData.Id;
                syncInfo.SyncDataType = taskData.SyncType;
                DeleteSyncInfo(syncInfo);
            }
        }
        /// <summary>
        /// 删除Exchange Calendar Event
        /// </summary>
        private void DeleteExchangeCalendarEvent(Account account, ExchangeCalendarEventSyncData calendarEventData, ExchangeService exchangeService)
        {
            if (SafeAction("DeleteExchangeCalendarEvent", () =>
            {
                var calendarEvent = Appointment.Bind(exchangeService, calendarEventData.ExchangeCalendarEvent.Id.UniqueId);
                if (calendarEvent != null)
                {
                    calendarEvent.Delete(DeleteMode.HardDelete);
                    _logger.InfoFormat("删除Exchange日历事件#{0}|{1}|{2}", calendarEventData.Id, calendarEventData.Subject, account.ID);
                }
            }))
            {
                //删除同步信息
                SyncInfo syncInfo = new SyncInfo();
                syncInfo.AccountId = account.ID;
                syncInfo.LocalDataId = calendarEventData.SyncId;
                syncInfo.SyncDataId = calendarEventData.Id;
                syncInfo.SyncDataType = calendarEventData.SyncType;
                DeleteSyncInfo(syncInfo);
            }
        }
        /// <summary>
        /// 删除Exchange Contact
        /// </summary>
        private void DeleteExchangeContact(Account account, ExchangeContactSyncData contactData, ExchangeService exchangeService)
        {
            if (SafeAction("DeleteExchangeContact", () =>
            {
                var contact = Microsoft.Exchange.WebServices.Data.Contact.Bind(exchangeService, contactData.ExchangeContact.Id.UniqueId);
                if (contact != null)
                {
                    contact.Delete(DeleteMode.HardDelete);
                    _logger.InfoFormat("删除Exchange联系人#{0}|{1}|{2}", contactData.Id, contactData.Subject, _account.ID);
                }
            }))
            {
                //删除同步信息
                SyncInfo syncInfo = new SyncInfo();
                syncInfo.AccountId = account.ID;
                syncInfo.LocalDataId = contactData.SyncId;
                syncInfo.SyncDataId = contactData.Id;
                syncInfo.SyncDataType = contactData.SyncType;
                DeleteSyncInfo(syncInfo);
            }
        }

        private Microsoft.Exchange.WebServices.Data.Task GetExchangeTask(ExchangeService exchangeService, string id)
        {
            return Microsoft.Exchange.WebServices.Data.Task.Bind(exchangeService, id, ExchangeSyncSettings.TaskPropertySet);
        }
        private Microsoft.Exchange.WebServices.Data.Appointment GetExchangeCalendarEvent(ExchangeService exchangeService, string id)
        {
            return Microsoft.Exchange.WebServices.Data.Appointment.Bind(exchangeService, id, ExchangeSyncSettings.CalendarEventPropertySet);
        }
        private Microsoft.Exchange.WebServices.Data.Contact GetExchangeContact(ExchangeService exchangeService, string id)
        {
            return Microsoft.Exchange.WebServices.Data.Contact.Bind(exchangeService, id, ExchangeSyncSettings.ContactPropertySet);
        }

        #endregion

        #region 持久化AliyunEmail相关数据的函数

        /// <summary>
        /// 创建AliyunEmail Calendar Event
        /// </summary>
        private void CreateAliyunEmailCalendarEvent(Account account, string userEmail, AliyunCalendarEventSyncData calendarEventData)
        {
            string eventId = null;

            if (SafeAction("CreateAliyunEmailCalendarEvent", () =>
            {
                var result = _aliyunDataAccess.CreateAliYunCalendarEvent(userEmail, calendarEventData.Subject, calendarEventData.Body, calendarEventData.StartTime, calendarEventData.EndTime);
                if (result.status.statusCode != AliYunSyncSettings.SuccessStatusCode)
                {
                    throw new Exception(result.status.statusMsg);
                }
                eventId = result.data.calendarId;
                _logger.InfoFormat("新增阿里云日历事件#{0}|{1}|{2}", eventId, calendarEventData.Subject, account.ID);
            }))
            {
                var result = _aliyunDataAccess.QueryAliYunCalendarEvent(userEmail, eventId);

                //更新任务最后更新时间，确保与AliyunEmail Calendar Event的最后更新时间一致
                UpdateTaskLastUpdateTime(long.Parse(calendarEventData.SyncId), result.data.calendar.eventList.First().lastModified.ToDateTime());

                //创建同步信息
                SyncInfo syncInfo = new SyncInfo();
                syncInfo.AccountId = account.ID;
                syncInfo.LocalDataId = calendarEventData.SyncId;
                syncInfo.SyncDataId = eventId;
                syncInfo.SyncDataType = calendarEventData.SyncType;
                InsertSyncInfo(syncInfo);
            }
        }
        /// <summary>
        /// 更新AliyunEmail Calendar Event
        /// </summary>
        private void UpdateAliyunEmailCalendarEvent(string userEmail, AliyunCalendarEventSyncData calendarEventData)
        {
            if (SafeAction("UpdateAliyunEmailCalendarEvent", () =>
            {
                var result = _aliyunDataAccess.UpdateAliYunCalendarEvent(userEmail, calendarEventData.Id, calendarEventData.Subject, calendarEventData.Body);
                if (result.status.statusCode != AliYunSyncSettings.SuccessStatusCode)
                {
                    throw new Exception(result.status.statusMsg);
                }
                _logger.InfoFormat("更新阿里云日历事件#{0}|{1}|{2}", calendarEventData.Id, calendarEventData.Subject, _account.ID);
            }))
            {
                var result = _aliyunDataAccess.QueryAliYunCalendarEvent(userEmail, calendarEventData.Id);

                //更新任务最后更新时间，确保与AliyunEmail Calendar Event的最后更新时间一致
                UpdateTaskLastUpdateTime(long.Parse(calendarEventData.SyncId), result.data.calendar.eventList.First().lastModified.ToDateTime());
            }
        }
        /// <summary>
        /// 删除AliyunEmail Calendar Event
        /// </summary>
        private void DeleteAliyunEmailCalendarEvent(Account account, string userEmail, AliyunCalendarEventSyncData calendarEventData)
        {
            if (SafeAction("DeleteAliyunEmailCalendarEvent", () =>
            {
                var result = _aliyunDataAccess.DeleteAliYunCalendarEvent(userEmail, calendarEventData.Id);
                if (result.status.statusCode != AliYunSyncSettings.SuccessStatusCode)
                {
                    throw new Exception(result.status.statusMsg);
                }
                _logger.InfoFormat("删除阿里云日历事件#{0}|{1}|{2}", calendarEventData.Id, calendarEventData.Subject, _account.ID);
            }))
            {
                //删除同步信息
                SyncInfo syncInfo = new SyncInfo();
                syncInfo.AccountId = account.ID;
                syncInfo.LocalDataId = calendarEventData.SyncId;
                syncInfo.SyncDataId = calendarEventData.Id;
                syncInfo.SyncDataType = calendarEventData.SyncType;
                DeleteSyncInfo(syncInfo);
            }
        }

        #endregion

        /// <summary>
        /// 插入一条同步信息
        /// </summary>
        private void InsertSyncInfo(SyncInfo syncInfo)
        {
            var session = _sessionManager.OpenSession();
            var sqlFormat = "insert into Cooper_SyncInfo (AccountId,LocalDataId,SyncDataId,SyncDataType) values ({0},'{1}','{2}',{3})";
            var sql = string.Format(sqlFormat, syncInfo.AccountId, syncInfo.LocalDataId, syncInfo.SyncDataId, syncInfo.SyncDataType);
            var query = session.CreateSQLQuery(sql);
            query.ExecuteUpdate();
        }
        /// <summary>
        /// 删除一条同步信息
        /// </summary>
        private void DeleteSyncInfo(SyncInfo syncInfo)
        {
            var session = _sessionManager.OpenSession();
            var sqlFormat = "delete from Cooper_SyncInfo where AccountId={0} and LocalDataId='{1}' and SyncDataId='{2}' and SyncDataType={3}";
            var sql = string.Format(sqlFormat, syncInfo.AccountId, syncInfo.LocalDataId, syncInfo.SyncDataId, syncInfo.SyncDataType);
            var query = session.CreateSQLQuery(sql);
            query.ExecuteUpdate();
        }

        /// <summary>
        /// 设置任务的最后更新时间
        /// </summary>
        private void UpdateTaskLastUpdateTime(long taskId, DateTime lastUpdateTime)
        {
            Cooper.Model.Tasks.Task task = _taskService.GetTask(taskId);
            task.SetLastUpdateTime(lastUpdateTime);
            _taskService.Update(task);
        }
        /// <summary>
        /// 设置联系人的最后更新时间
        /// </summary>
        private void UpdateContactLastUpdateTime(int contactId, DateTime lastUpdateTime)
        {
            //TODO
            //Cooper.Model.Tasks.Contacter contacter = _contactService.GetTask(contactId);
            //contacter.SetLastUpdateTime(lastUpdateTime);
            //_contactService.Update(contacter);
        }

        /// <summary>
        /// 将任务数据的同步结果进行日志记录
        /// </summary>
        private void LogSyncResult<T>(ISyncResult<T> syncResult) where T : class, ISyncData
        {
            _logger.InfoFormat("----LocalDatasToCreate, Count:{0}, Details:", syncResult.LocalDatasToCreate.Count());
            foreach (var taskData in syncResult.LocalDatasToCreate)
            {
                LogToCreatedSyncData(taskData);
            }

            _logger.InfoFormat("----LocalDatasToUpdate, Count:{0}, Details:", syncResult.LocalDatasToUpdate.Count());
            foreach (var taskData in syncResult.LocalDatasToUpdate)
            {
                LogSyncData(taskData);
            }

            _logger.InfoFormat("----LocalDatasToDelete, Count:{0}, Details:", syncResult.LocalDatasToDelete.Count());
            foreach (var taskData in syncResult.LocalDatasToDelete)
            {
                LogSyncData(taskData);
            }

            _logger.InfoFormat("----SyncDatasToCreate, Count:{0}, Details:", syncResult.SyncDatasToCreate.Count());
            foreach (var syncData in syncResult.SyncDatasToCreate)
            {
                LogToCreatedSyncData(syncData);
            }

            _logger.InfoFormat("----SyncDatasToUpdate, Count:{0}, Details:", syncResult.SyncDatasToUpdate.Count());
            foreach (var syncData in syncResult.SyncDatasToUpdate)
            {
                LogSyncData(syncData);
            }

            _logger.InfoFormat("----SyncDatasToDelete, Count:{0}, Details:", syncResult.SyncDatasToDelete.Count());
            foreach (var syncData in syncResult.SyncDatasToDelete)
            {
                LogSyncData(syncData);
            }
        }
        private void LogToCreatedSyncData(ISyncData syncData)
        {
            if (syncData != null)
            {
                _logger.InfoFormat("--------Data Type:{0}, Subject:{1}, SyncId:{2}, SyncType:{3}, IsFromDefault:{4}",
                    syncData.GetType().Name,
                    syncData.Subject,
                    syncData.SyncId,
                    syncData.SyncType,
                    syncData.IsFromDefault);
            }
        }
        private void LogSyncData(ISyncData syncData)
        {
            if (syncData != null)
            {
                _logger.InfoFormat("--------Data Type:{0}, Id:{1}, Subject:{2}, SyncId:{3}, SyncType:{4}, IsFromDefault:{5}, LastUpdateLocalTime:{6}",
                    syncData.GetType().Name,
                    syncData.Id,
                    syncData.Subject,
                    syncData.SyncId,
                    syncData.SyncType,
                    syncData.IsFromDefault,
                    syncData.LastUpdateLocalTime);
            }
        }
        private bool SafeAction(string actionName, Action action)
        {
            bool success = false;

            try
            {
                action();
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                _logger.Error(string.Format("{0} has exception.", actionName), ex);
            }

            return success;
        }

        #endregion

        /// <summary>
        /// 同步任务
        /// </summary>
        private void SyncTasks(IEnumerable<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>> taskSyncServices)
        {
            if (taskSyncServices != null && taskSyncServices.Count() > 0)
            {
                if (_arkConnection != null)
                {
                    SyncTaskWithExchange(taskSyncServices.Where(x => typeof(IExchangeSyncService).IsAssignableFrom(x.GetType())));
                }
                if (_aliyunMailConnection != null)
                {
                    SyncTaskWithAliyunMail(taskSyncServices.Where(x => typeof(IAliYunSyncService).IsAssignableFrom(x.GetType())));
                }
            }
        }
        /// <summary>
        /// 同步联系人
        /// </summary>
        private void SyncContacts(IEnumerable<ISyncService<ContactSyncData, ISyncData, ContactSyncResult>> contactSyncServices)
        {
            if (contactSyncServices != null && contactSyncServices.Count() > 0)
            {
                if (_arkConnection != null)
                {
                    SyncContactWithExchange(contactSyncServices.Where(x => typeof(IExchangeSyncService).IsAssignableFrom(x.GetType())));
                }
            }
        }

        private void SyncTaskWithExchange(IEnumerable<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>> exchangeTaskSyncServices)
        {
            _logger.Info("--------开始与Exchange进行任务同步--------");

            if (exchangeTaskSyncServices == null || exchangeTaskSyncServices.Count() == 0)
            {
                _logger.Info("因为没有注册的同步服务，故同步操作未进行.");
                return;
            }

            var exchangeUserCredential = GetExchangeUserCredential();

            if (exchangeUserCredential == null)
            {
                _logger.ErrorFormat("Exchange User Credential无效，无法进行同步, ark connectionId:{0}", _arkConnection.ID);
                return;
            }

            //set user credential for each exchange sync service.
            foreach (IExchangeSyncService taskSyncService in exchangeTaskSyncServices)
            {
                taskSyncService.SetCredential(exchangeUserCredential);
            }

            //register all the exchange sync services.
            var businessSyncService = DependencyResolver.Resolve<IBusinessSyncService>();
            foreach (var taskSyncService in exchangeTaskSyncServices)
            {
                businessSyncService.RegisterTaskSyncService(taskSyncService);
            }

            DoSyncTasks(businessSyncService);

            _logger.Info("--------结束与Exchange进行任务同步--------");
        }
        private void SyncContactWithExchange(IEnumerable<ISyncService<ContactSyncData, ISyncData, ContactSyncResult>> exchangeContactSyncServices)
        {
            _logger.Info("--------开始与Exchange进行联系人同步--------");

            if (exchangeContactSyncServices == null || exchangeContactSyncServices.Count() == 0)
            {
                _logger.Info("因为没有注册的同步服务，故同步操作未进行.");
                return;
            }

            var exchangeUserCredential = GetExchangeUserCredential();

            if (exchangeUserCredential == null)
            {
                _logger.ErrorFormat("Exchange User Credential无效，无法进行同步, ark connectionId:{0}", _arkConnection.ID);
                return;
            }

            //set user credential for each exchange sync service.
            foreach (IExchangeSyncService contactSyncService in exchangeContactSyncServices)
            {
                contactSyncService.SetCredential(exchangeUserCredential);
            }

            //register all the exchange sync services.
            var businessSyncService = DependencyResolver.Resolve<IBusinessSyncService>();
            foreach (var contactSyncService in exchangeContactSyncServices)
            {
                businessSyncService.RegisterContactSyncService(contactSyncService);
            }

            DoSyncContacts(businessSyncService);

            _logger.Info("--------结束与Exchange进行联系人同步--------");
        }
        private void SyncTaskWithAliyunMail(IEnumerable<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>> aliyunMailSyncServices)
        {
            _logger.Info("--------开始与阿里云邮箱进行任务同步--------");

            if (aliyunMailSyncServices == null || aliyunMailSyncServices.Count() == 0)
            {
                _logger.Info("因为没有注册的同步服务，故同步操作未进行.");
                return;
            }

            var userMail = GetAliyunUserMail();

            if (userMail == null)
            {
                _logger.ErrorFormat("阿里云邮箱为空，无法进行同步, connectionId:{0}", _aliyunMailConnection.ID);
                return;
            }

            if (!_aliyunDataAccess.IsEmailExist(userMail))
            {
                _logger.InfoFormat("阿里云邮箱不存在，无法进行同步, connectionId:{0}, mail:{1}", _aliyunMailConnection.ID, userMail);
                return;
            }

            //set email for each aliyun email sync service.
            foreach (IAliYunSyncService aliyunMailSyncService in aliyunMailSyncServices)
            {
                aliyunMailSyncService.SetEmail(userMail);
            }

            //register all the aliyun email sync services.
            var businessSyncService = DependencyResolver.Resolve<IBusinessSyncService>();
            foreach (var aliyunMailSyncService in aliyunMailSyncServices)
            {
                businessSyncService.RegisterTaskSyncService(aliyunMailSyncService);
            }

            DoSyncTasks(businessSyncService);

            _logger.Info("--------结束与阿里云邮箱进行任务同步--------");
        }

        private void DoSyncTasks(IBusinessSyncService businessSyncService)
        {
            var taskSyncDataList = GetTaskSyncDatas(_account);
            _logger.InfoFormat("同步前的本地任务数据, 总记录数:{0}，明细如下：", taskSyncDataList.Count());
            foreach (var taskSyncData in taskSyncDataList)
            {
                LogSyncData(taskSyncData);
            }

            var syncInfoList = GetSyncInfos(_account);
            _logger.InfoFormat("同步任务数据前的所有与当前帐号相关的任务同步信息，总数：{0}", syncInfoList.Count());
            foreach (var syncInfo in syncInfoList)
            {
                _logger.InfoFormat("任务同步信息, AccountId:{0}, LocalDataId:{1}, SyncDataId:{2}, SyncDataType:{3}", syncInfo.AccountId, syncInfo.LocalDataId, syncInfo.SyncDataId, syncInfo.SyncDataType);
            }

            _logger.InfoFormat("开始比较任务数据.");
            var result = businessSyncService.SyncTasks(taskSyncDataList, syncInfoList);
            _logger.InfoFormat("比较任务数据结束，比较结果：:");
            LogSyncResult(result);

            _logger.InfoFormat("开始处理比较结果：");
            ProcessTaskSyncResult(result, _account);
            _logger.InfoFormat("处理比较结果结束");
        }
        private void DoSyncContacts(IBusinessSyncService businessSyncService)
        {
            var contactSyncDataList = GetContactSyncDatas(_account);
            _logger.InfoFormat("同步前的本地联系人数据, 总记录数:{0}，明细如下：", contactSyncDataList.Count());
            foreach (var contactSyncData in contactSyncDataList)
            {
                LogSyncData(contactSyncData);
            }

            var syncInfoList = GetSyncInfos(_account);
            _logger.InfoFormat("同步联系人数据前的所有与当前帐号相关的联系人同步信息，总数：{0}", syncInfoList.Count());
            foreach (var syncInfo in syncInfoList)
            {
                _logger.InfoFormat("联系人同步信息, AccountId:{0}, LocalDataId:{1}, SyncDataId:{2}, SyncDataType:{3}", syncInfo.AccountId, syncInfo.LocalDataId, syncInfo.SyncDataId, syncInfo.SyncDataType);
            }

            _logger.InfoFormat("开始比较联系人数据.");
            var result = businessSyncService.SyncContacts(contactSyncDataList, syncInfoList);
            _logger.InfoFormat("结束比较联系人数据，比较结果:");
            LogSyncResult(result);

            _logger.InfoFormat("开始处理比较结果：");
            ProcessContactSyncResult(result, _account);
            _logger.InfoFormat("处理比较结果结束");
        }

        /// <summary>
        /// 初始化Account以及Connection
        /// </summary>
        /// <param name="connectionId"></param>
        private void InitializeAccountAndConnection(int connectionId)
        {
            var connection = _accountConnectionService.GetConnection(connectionId);
            if (connection != null)
            {
                _account = _accountService.GetAccount(connection.AccountId);

                if (connection.GetType() == typeof(ArkConnection))
                {
                    _arkConnection = connection as ArkConnection;
                }
                else if (connection.GetType() == typeof(AliyunMailConnection))
                {
                    _aliyunMailConnection = connection as AliyunMailConnection;
                }
            }
        }
        /// <summary>
        /// 获取账号的Exchange认证信息
        /// </summary>
        private ExchangeUserCredential GetExchangeUserCredential()
        {
            if (_arkConnection != null)
            {
                try
                {
                    var position = _arkConnection.Name.IndexOf('\\');
                    var domain = _arkConnection.Name.Substring(0, position);
                    var userName = _arkConnection.Name.Substring(position + 1);
                    var password = SecurityHelper.Base64Decrypt(_arkConnection.Token);

                    return new ExchangeUserCredential
                    {
                        Domain = domain,
                        UserName = userName,
                        Password = password,
                        EmailAddress = null,   //配置了ExchangeServer属性就不需要设置该属性
                        ExchangeServerUrl = _exchangeServer
                    };
                }
                catch (Exception ex)
                {
                    _logger.Error("GetExchangeUserCredential has exception.", ex);
                }
            }

            return null;
        }
        /// <summary>
        /// 获取当前AliyunMailConnection对应的阿里云邮箱
        /// </summary>
        private string GetAliyunUserMail()
        {
            if (_aliyunMailConnection != null)
            {
                return _aliyunMailConnection.Name;
            }

            return null;
        }
        /// <summary>
        /// 获取帐号的所有任务
        /// </summary>
        private IEnumerable<TaskSyncData> GetTaskSyncDatas(Account account)
        {
            List<TaskSyncData> dataList = new List<TaskSyncData>();

            var tasks = _taskService.GetTasks(account);
            foreach (var task in tasks)
            {
                dataList.Add(CreateFromTask(task));
            }

            return dataList;
        }
        /// <summary>
        /// 获取账号的所有联系人信息，目前还未实现
        /// </summary>
        private IEnumerable<ContactSyncData> GetContactSyncDatas(Account account)
        {
            List<ContactSyncData> dataList = new List<ContactSyncData>();

            //TODO
            //var tasks = _taskService.GetTasks(account);
            //foreach (var task in tasks)
            //{
            //    dataList.Add(CreateFromTask(task));
            //}

            return dataList;
        }
        /// <summary>
        /// 获取账号的所有同步信息
        /// </summary>
        private IEnumerable<SyncInfo> GetSyncInfos(Account account)
        {
            var session = _sessionManager.OpenSession();
            var sql = "select AccountId,LocalDataId,SyncDataId,SyncDataType from Cooper_SyncInfo where AccountId=" + account.ID;
            var query = session.CreateSQLQuery(sql);
            var objectArrayList = query.List();
            List<SyncInfo> syncInfoList = new List<SyncInfo>();

            foreach (object[] objectArray in objectArrayList)
            {
                SyncInfo syncInfo = new SyncInfo();
                syncInfo.AccountId = int.Parse(objectArray[0].ToString());
                syncInfo.LocalDataId = objectArray[1].ToString();
                syncInfo.SyncDataId = objectArray[2].ToString();
                syncInfo.SyncDataType = int.Parse(objectArray[3].ToString());
                syncInfoList.Add(syncInfo);
            }

            return syncInfoList;
        }
        /// <summary>
        /// 将Task转换为TaskSyncData
        /// </summary>
        private TaskSyncData CreateFromTask(Cooper.Model.Tasks.Task task)
        {
            TaskSyncData data = new TaskSyncData();
            data.Id = task.ID.ToString();
            data.Subject = task.Subject;
            data.Body = task.Body;
            data.DueTime = task.DueTime;
            data.IsCompleted = task.IsCompleted;
            data.Priority = ConvertToSyncDataPriority(task.Priority);
            data.CreateTime = task.CreateTime;
            data.LastUpdateLocalTime = task.LastUpdateTime;
            return data;
        }
        private TaskSyncDataPriority ConvertToSyncDataPriority(Cooper.Model.Tasks.Priority priority)
        {
            if (priority == Cooper.Model.Tasks.Priority.Today)
            {
                return TaskSyncDataPriority.Today;
            }
            else if (priority == Cooper.Model.Tasks.Priority.Upcoming)
            {
                return TaskSyncDataPriority.Upcoming;
            }
            else if (priority == Cooper.Model.Tasks.Priority.Later)
            {
                return TaskSyncDataPriority.Later;
            }
            return TaskSyncDataPriority.Today;
        }
        private Cooper.Model.Tasks.Priority ConvertToPriority(TaskSyncDataPriority priority)
        {
            if (priority == TaskSyncDataPriority.Today)
            {
                return Cooper.Model.Tasks.Priority.Today;
            }
            else if (priority == TaskSyncDataPriority.Upcoming)
            {
                return Cooper.Model.Tasks.Priority.Upcoming;
            }
            else if (priority == TaskSyncDataPriority.Later)
            {
                return Cooper.Model.Tasks.Priority.Later;
            }
            return Cooper.Model.Tasks.Priority.Today;
        }

        #endregion
    }
}
