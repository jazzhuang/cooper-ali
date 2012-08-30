using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AliCooper.Model.Accounts;
using CodeSharp.Core.Services;
using CodeSharp.Core.Utils;
using Cooper.Model.Tasks;
using Cooper.Sync;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using CooperTask = Cooper.Model.Tasks.PersonalTask;
using ExchangeTask = Microsoft.Exchange.WebServices.Data.Task;
using MicrosoftAssert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace AliCooper.Sync.Test
{
    [TestFixture]
    [TestClass]
    public class ExchangeSyncTest : TestBase
    {
        private int _arkConnectionId = 20; //我的ark账号连接

        /// <summary>
        /// 测试与Exchange同步的方法1
        /// </summary>
        [Test]
        [TestMethod]
        public void Test1_Sync_Tasks_And_CalendarEvents_With_Exchange()
        {
            Sync_Create_ExchangeTask_AccordingWith_Task_Test();
            Sync_Update_ExchangeTask_AccordingWith_Task_Test();
            Sync_Delete_ExchangeTask_AccordingWith_Task_Test();
            Sync_Create_Task_AccordingWith_ExchangeTask_Test();
            Sync_Update_Task_AccordingWith_ExchangeTask_Test();

            Sync_Create_Task_AccordingWith_ExchangeCalendarEvent_Test();
            Sync_Update_Task_AccordingWith_ExchangeCalendarEvent_Test();
            Sync_Update_ExchangeCalendarEvent_AccordingWith_Task_Test();
            Sync_Delete_ExchangeCalendarEvent_AccordingWith_Task_Test();
        }

        #region Exchange 任务同步测试

        //由于并且任务系统不支持根据Exchange任务删除任务的功能，所以这个测试用例不需要写

        /// <summary>
        /// 测试同步时根据任务创建Exchange任务
        /// </summary>
        [Test]
        [TestMethod]
        public void Sync_Create_ExchangeTask_AccordingWith_Task_Test()
        {
            _logger.Info("--------------开始执行测试：Sync_Create_ExchangeTask_AccordingWith_Task_Test");
            InitializeAccountAndConnection(_arkConnectionId);

            var cooperTask = CreateCooperTask("cooper task001", "description of task", DateTime.Now.Date.AddDays(2), false);
            MicrosoftAssert.IsNotNull(cooperTask);

            _syncProcessor.SyncTasksAndContacts(
                _arkConnectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IExchangeTaskSyncService>()
                },
                null);

            var syncInfo = GetSyncInfoByLocalDataId(_account.ID, cooperTask.ID.ToString(), SyncDataType.ExchangeTask);
            MicrosoftAssert.IsNotNull(syncInfo);

            cooperTask = GetCooperTask(cooperTask.ID);
            var exchangeTask = GetExchangeTask(syncInfo.SyncDataId);
            MicrosoftAssert.IsNotNull(exchangeTask);
            AssertTaskAndExchangeTaskAreEqual(cooperTask, exchangeTask);
        }
        /// <summary>
        /// 测试同步时根据任务更新Exchange任务
        /// </summary>
        [Test]
        [TestMethod]
        public void Sync_Update_ExchangeTask_AccordingWith_Task_Test()
        {
            _logger.Info("--------------开始执行测试：Sync_Update_ExchangeTask_AccordingWith_Task_Test");
            InitializeAccountAndConnection(_arkConnectionId);

            //首先创建
            var cooperTask = CreateCooperTask("cooper task or update test 0001", "description of task", DateTime.Now.Date.AddDays(2), false);
            MicrosoftAssert.IsNotNull(cooperTask);

            //同步
            _syncProcessor.SyncTasksAndContacts(
                _arkConnectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IExchangeTaskSyncService>()
                },
                null);

            //重新获取
            var syncInfo = GetSyncInfoByLocalDataId(_account.ID, cooperTask.ID.ToString(), SyncDataType.ExchangeTask);
            MicrosoftAssert.IsNotNull(syncInfo);

            var exchangeTask = GetExchangeTask(syncInfo.SyncDataId);
            MicrosoftAssert.IsNotNull(exchangeTask);
            AssertTaskAndExchangeTaskAreEqual(cooperTask, exchangeTask);

            //更新Task
            cooperTask = UpdateCooperTask(cooperTask.ID, cooperTask.Subject + "_updated", cooperTask.Body + "_updated", cooperTask.DueTime.Value.Date.AddDays(1), true);
            UpdateTaskLastUpdateTime(cooperTask, exchangeTask.LastModifiedTime.AddSeconds(1));

            //同步
            _syncProcessor.SyncTasksAndContacts(
                _arkConnectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IExchangeTaskSyncService>()
                },
                null);

            //重新获取
            cooperTask = GetCooperTask(cooperTask.ID);
            exchangeTask = GetExchangeTask(syncInfo.SyncDataId);

            //对比结果
            MicrosoftAssert.IsNotNull(exchangeTask);
            AssertTaskAndExchangeTaskAreEqual(cooperTask, exchangeTask);
        }
        /// <summary>
        /// 测试同步时根据任务删除Exchange任务
        /// </summary>
        [Test]
        [TestMethod]
        public void Sync_Delete_ExchangeTask_AccordingWith_Task_Test()
        {
            _logger.Info("--------------开始执行测试：Sync_Delete_ExchangeTask_AccordingWith_Task_Test");
            InitializeAccountAndConnection(_arkConnectionId);

            //首先创建
            var cooperTask = CreateCooperTask("cooper task or update test 0001", "description of task", DateTime.Now.Date.AddDays(2), false);
            MicrosoftAssert.IsNotNull(cooperTask);

            //同步
            _syncProcessor.SyncTasksAndContacts(
                _arkConnectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IExchangeTaskSyncService>()
                },
                null);

            //重新获取
            var syncInfo = GetSyncInfoByLocalDataId(_account.ID, cooperTask.ID.ToString(), SyncDataType.ExchangeTask);
            MicrosoftAssert.IsNotNull(syncInfo);

            var exchangeTask = GetExchangeTask(syncInfo.SyncDataId);
            MicrosoftAssert.IsNotNull(exchangeTask);
            AssertTaskAndExchangeTaskAreEqual(cooperTask, exchangeTask);

            //删除本地Task
            DeleteCooperTask(cooperTask.ID);

            //同步
            _syncProcessor.SyncTasksAndContacts(
                _arkConnectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IExchangeTaskSyncService>()
                },
                null);

            //重新获取
            bool isExchangeTaskExist = IsExchangeTaskExist(syncInfo.SyncDataId);

            //Assert结果
            MicrosoftAssert.IsFalse(isExchangeTaskExist);
        }
        /// <summary>
        /// 测试同步时根据Exchange任务创建任务
        /// </summary>
        [Test]
        [TestMethod]
        public void Sync_Create_Task_AccordingWith_ExchangeTask_Test()
        {
            _logger.Info("--------------开始执行测试：Sync_Create_Task_AccordingWith_ExchangeTask_Test");
            InitializeAccountAndConnection(_arkConnectionId);

            var exchangeTask = CreateExchangeTask("cooper task 00001", "description of task", DateTime.Now.Date.AddDays(2), TaskStatus.Completed);
            MicrosoftAssert.IsNotNull(exchangeTask);

            _syncProcessor.SyncTasksAndContacts(
                _arkConnectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IExchangeTaskSyncService>()
                },
                null);

            var syncInfo = GetSyncInfoBySyncDataId(_account.ID, exchangeTask.Id.UniqueId, SyncDataType.ExchangeTask);
            MicrosoftAssert.IsNotNull(syncInfo);

            var cooperTask = GetCooperTask(long.Parse(syncInfo.LocalDataId));
            exchangeTask = GetExchangeTask(exchangeTask.Id.UniqueId);
            MicrosoftAssert.IsNotNull(cooperTask);
            AssertTaskAndExchangeTaskAreEqual(cooperTask, exchangeTask);
        }
        /// <summary>
        /// 测试同步时根据Exchange任务更新任务
        /// </summary>
        [Test]
        [TestMethod]
        public void Sync_Update_Task_AccordingWith_ExchangeTask_Test()
        {
            _logger.Info("--------------开始执行测试：Sync_Update_Task_AccordingWith_ExchangeTask_Test");
            InitializeAccountAndConnection(_arkConnectionId);

            var exchangeTask = CreateExchangeTask("cooper task 0000A", "description of task", DateTime.Now.Date.AddDays(2), TaskStatus.Completed);
            MicrosoftAssert.IsNotNull(exchangeTask);

            _syncProcessor.SyncTasksAndContacts(
                _arkConnectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IExchangeTaskSyncService>()
                },
                null);

            var syncInfo = GetSyncInfoBySyncDataId(_account.ID, exchangeTask.Id.UniqueId, SyncDataType.ExchangeTask);
            MicrosoftAssert.IsNotNull(syncInfo);

            var cooperTask = GetCooperTask(long.Parse(syncInfo.LocalDataId));
            MicrosoftAssert.IsNotNull(cooperTask);
            AssertTaskAndExchangeTaskAreEqual(cooperTask, exchangeTask);

            Thread.Sleep(1000 * 2);
            exchangeTask = UpdateExchangeTask(exchangeTask.Id.UniqueId, exchangeTask.Subject + "_updated", exchangeTask.Body.Text + "_updated", exchangeTask.DueDate.Value.AddDays(1), TaskStatus.InProgress);

            _syncProcessor.SyncTasksAndContacts(
                _arkConnectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IExchangeTaskSyncService>()
                },
                null);

            cooperTask = GetCooperTask(long.Parse(syncInfo.LocalDataId));
            exchangeTask = GetExchangeTask(exchangeTask.Id.UniqueId);
            MicrosoftAssert.IsNotNull(cooperTask);
            AssertTaskAndExchangeTaskAreEqual(cooperTask, exchangeTask);
        }

        #endregion

        #region Exchange 日历事件同步测试

        //由于1）日历的事件不会主动从任务系统同步过去，2）并且任务系统不支持根据Exchange日历事件删除任务的功能
        //所以这两个测试用例不需要写

        /// <summary>
        /// 测试同步时根据Exchange日历事件创建任务
        /// </summary>
        [Test]
        [TestMethod]
        public void Sync_Create_Task_AccordingWith_ExchangeCalendarEvent_Test()
        {
            _logger.Info("--------------开始执行测试：Sync_Create_Task_AccordingWith_ExchangeCalendarEvent_Test");
            InitializeAccountAndConnection(_arkConnectionId);

            var exchangeCalendarEvent = CreateExchangeCalendarEvent("cooper calendar event00001", "description of event", DateTime.Now, DateTime.Now.AddHours(2));
            MicrosoftAssert.IsNotNull(exchangeCalendarEvent);

            _syncProcessor.SyncTasksAndContacts(
                _arkConnectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IExchangeCalendarEventSyncService>()
                },
                null);

            var syncInfo = GetSyncInfoBySyncDataId(_account.ID, exchangeCalendarEvent.Id.UniqueId, SyncDataType.ExchangeCalendarEvent);
            MicrosoftAssert.IsNotNull(syncInfo);

            var cooperTask = GetCooperTask(long.Parse(syncInfo.LocalDataId));
            exchangeCalendarEvent = GetExchangeCalendarEvent(exchangeCalendarEvent.Id.UniqueId);
            MicrosoftAssert.IsNotNull(cooperTask);
            AssertTaskAndExchangeCalendarEventAreEqual(cooperTask, exchangeCalendarEvent);
        }
        /// <summary>
        /// 测试同步时根据Exchange日历事件更新任务
        /// </summary>
        [Test]
        [TestMethod]
        public void Sync_Update_Task_AccordingWith_ExchangeCalendarEvent_Test()
        {
            _logger.Info("--------------开始执行测试：Sync_Update_Task_AccordingWith_ExchangeCalendarEvent_Test");
            InitializeAccountAndConnection(_arkConnectionId);

            var exchangeCalendarEvent = CreateExchangeCalendarEvent("cooper calendar event00001", "description of event", DateTime.Now, DateTime.Now.AddHours(2));
            MicrosoftAssert.IsNotNull(exchangeCalendarEvent);

            _syncProcessor.SyncTasksAndContacts(
                _arkConnectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IExchangeCalendarEventSyncService>()
                },
                null);

            var syncInfo = GetSyncInfoBySyncDataId(_account.ID, exchangeCalendarEvent.Id.UniqueId, SyncDataType.ExchangeCalendarEvent);
            MicrosoftAssert.IsNotNull(syncInfo);

            var cooperTask = GetCooperTask(long.Parse(syncInfo.LocalDataId));
            MicrosoftAssert.IsNotNull(cooperTask);
            AssertTaskAndExchangeCalendarEventAreEqual(cooperTask, exchangeCalendarEvent);

            Thread.Sleep(1000 * 2);
            exchangeCalendarEvent = UpdateExchangeCalendarEvent(exchangeCalendarEvent.Id.UniqueId, exchangeCalendarEvent.Subject + "_updated", exchangeCalendarEvent.Body.Text + "_updated");

            _syncProcessor.SyncTasksAndContacts(
                _arkConnectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IExchangeCalendarEventSyncService>()
                },
                null);

            cooperTask = GetCooperTask(cooperTask.ID);
            exchangeCalendarEvent = GetExchangeCalendarEvent(exchangeCalendarEvent.Id.UniqueId);
            MicrosoftAssert.IsNotNull(cooperTask);
            AssertTaskAndExchangeCalendarEventAreEqual(cooperTask, exchangeCalendarEvent);
        }
        /// <summary>
        /// 测试同步时根据任务更新Exchange日历事件
        /// </summary>
        [Test]
        [TestMethod]
        public void Sync_Update_ExchangeCalendarEvent_AccordingWith_Task_Test()
        {
            _logger.Info("--------------开始执行测试：Sync_Update_ExchangeCalendarEvent_AccordingWith_Task_Test");
            InitializeAccountAndConnection(_arkConnectionId);

            var exchangeCalendarEvent = CreateExchangeCalendarEvent("cooper calendar event00001", "description of event", DateTime.Now, DateTime.Now.AddHours(2));
            MicrosoftAssert.IsNotNull(exchangeCalendarEvent);

            _syncProcessor.SyncTasksAndContacts(
                _arkConnectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IExchangeCalendarEventSyncService>()
                },
                null);

            var syncInfo = GetSyncInfoBySyncDataId(_account.ID, exchangeCalendarEvent.Id.UniqueId, SyncDataType.ExchangeCalendarEvent);
            MicrosoftAssert.IsNotNull(syncInfo);

            var cooperTask = GetCooperTask(long.Parse(syncInfo.LocalDataId));
            MicrosoftAssert.IsNotNull(cooperTask);
            AssertTaskAndExchangeCalendarEventAreEqual(cooperTask, exchangeCalendarEvent);

            //更新Task
            cooperTask = UpdateCooperTask(cooperTask.ID, cooperTask.Subject + "_updated", cooperTask.Body + "_updated", cooperTask.DueTime.Value.Date.AddDays(1), true);
            UpdateTaskLastUpdateTime(cooperTask, exchangeCalendarEvent.LastModifiedTime.AddSeconds(1));
            //同步
            _syncProcessor.SyncTasksAndContacts(
                _arkConnectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IExchangeCalendarEventSyncService>()
                },
                null);

            //重新获取
            cooperTask = GetCooperTask(cooperTask.ID);
            exchangeCalendarEvent = GetExchangeCalendarEvent(syncInfo.SyncDataId);

            //对比结果
            MicrosoftAssert.IsNotNull(exchangeCalendarEvent);
            MicrosoftAssert.AreEqual(cooperTask.Subject, exchangeCalendarEvent.Subject);
            MicrosoftAssert.AreEqual(BodyType.Text, exchangeCalendarEvent.Body.BodyType);
            MicrosoftAssert.AreEqual(cooperTask.Body, exchangeCalendarEvent.Body.Text);
            MicrosoftAssert.AreEqual(FormatTime(cooperTask.LastUpdateTime), FormatTime(exchangeCalendarEvent.LastModifiedTime));
        }
        /// <summary>
        /// 测试同步时根据任务删除Exchange日历事件
        /// </summary>
        [Test]
        [TestMethod]
        public void Sync_Delete_ExchangeCalendarEvent_AccordingWith_Task_Test()
        {
            _logger.Info("--------------开始执行测试：Sync_Delete_ExchangeCalendarEvent_AccordingWith_Task_Test");
            InitializeAccountAndConnection(_arkConnectionId);

            var exchangeCalendarEvent = CreateExchangeCalendarEvent("cooper calendar event00001", "description of event", DateTime.Now, DateTime.Now.AddHours(2));
            MicrosoftAssert.IsNotNull(exchangeCalendarEvent);

            _syncProcessor.SyncTasksAndContacts(
                _arkConnectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IExchangeCalendarEventSyncService>()
                },
                null);

            var syncInfo = GetSyncInfoBySyncDataId(_account.ID, exchangeCalendarEvent.Id.UniqueId, SyncDataType.ExchangeCalendarEvent);
            MicrosoftAssert.IsNotNull(syncInfo);

            var cooperTask = GetCooperTask(long.Parse(syncInfo.LocalDataId));
            MicrosoftAssert.IsNotNull(cooperTask);
            AssertTaskAndExchangeCalendarEventAreEqual(cooperTask, exchangeCalendarEvent);

            //删除Task
            DeleteCooperTask(cooperTask.ID);

            //同步
            _syncProcessor.SyncTasksAndContacts(
                _arkConnectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IExchangeCalendarEventSyncService>()
                },
                null);

            //重新获取
            var isExchangeCalendarEventExist = IsExchangeCalendarEventExist(syncInfo.SyncDataId);

            //检查结果
            MicrosoftAssert.IsFalse(isExchangeCalendarEventExist);
        }

        #endregion

        #region Helper Methods

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
                if (_account != null)
                {
                    var accountConnection = _accountConnectionService.GetConnections(_account).SingleOrDefault(x => x.ID == connectionId);

                    if (accountConnection != null)
                    {
                        if (accountConnection.GetType() == typeof(ArkConnection))
                        {
                            _arkConnection = accountConnection as ArkConnection;
                        }
                    }
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

        private CooperTask CreateCooperTask(string subject, string body, DateTime? dueTime, bool isCompleted)
        {
            CooperTask task = new CooperTask(_account);

            task.SetSubject(subject);
            task.SetBody(body);
            task.SetDueTime(dueTime);
            if (isCompleted)
            {
                task.MarkAsCompleted();
            }
            else
            {
                task.MarkAsInCompleted();
            }
            task.SetPriority(Priority.Upcoming);

            _taskService.Create(task);

            return task;
        }
        private CooperTask UpdateCooperTask(long taskId, string subject, string body, DateTime? dueTime, bool isCompleted)
        {
            CooperTask task = _taskService.GetTask(taskId) as CooperTask;

            task.SetSubject(subject);
            task.SetBody(body);
            task.SetDueTime(dueTime);
            if (isCompleted)
            {
                task.MarkAsCompleted();
            }
            else
            {
                task.MarkAsInCompleted();
            }
            task.SetPriority(Priority.Later);

            _taskService.Update(task);

            return task;
        }
        /// <summary>
        /// 更新任务的最后更新时间
        /// </summary>
        private void UpdateTaskLastUpdateTime(CooperTask task, DateTime lastUpdateTime)
        {
            task.SetLastUpdateTime(lastUpdateTime);
            _taskService.Update(task);
        }
        private CooperTask GetCooperTask(long taskId)
        {
            return _taskService.GetTask(taskId) as CooperTask;
        }
        private void DeleteCooperTask(long taskId)
        {
            _taskService.Delete(_taskService.GetTask(taskId));
        }

        private void AssertTaskAndExchangeTaskAreEqual(CooperTask cooperTask, ExchangeTask exchangeTask)
        {
            MicrosoftAssert.AreEqual(cooperTask.Subject, exchangeTask.Subject);
            MicrosoftAssert.AreEqual(cooperTask.Body, exchangeTask.Body.Text);
            MicrosoftAssert.AreEqual(cooperTask.DueTime, exchangeTask.DueDate);
            if (cooperTask.IsCompleted)
            {
                MicrosoftAssert.AreEqual(TaskStatus.Completed, exchangeTask.Status);
            }
            else
            {
                MicrosoftAssert.AreEqual(TaskStatus.InProgress, exchangeTask.Status);
            }

            MicrosoftAssert.AreEqual(cooperTask.Priority, ConvertToTaskPriority(exchangeTask.Importance));
            MicrosoftAssert.AreEqual(FormatTime(cooperTask.LastUpdateTime), FormatTime(exchangeTask.LastModifiedTime));
        }
        private void AssertTaskAndExchangeCalendarEventAreEqual(CooperTask cooperTask, Appointment appointment)
        {
            MicrosoftAssert.AreEqual(cooperTask.Subject, appointment.Subject);
            MicrosoftAssert.AreEqual(BodyType.Text, appointment.Body.BodyType);
            MicrosoftAssert.AreEqual(cooperTask.Body, appointment.Body.Text);
            MicrosoftAssert.AreEqual(cooperTask.DueTime.Value.Date, appointment.End.Date);
            MicrosoftAssert.AreEqual(FormatTime(cooperTask.LastUpdateTime), FormatTime(appointment.LastModifiedTime));
        }

        private SyncInfo GetSyncInfoByLocalDataId(int accountId, string localDataId, SyncDataType syncDataType)
        {
            var sql = string.Format(
                "select AccountId,LocalDataId,SyncDataId,SyncDataType from Cooper_SyncInfo where AccountId={0} and LocalDataId='{1}' and SyncDataType={2}",
                accountId,
                localDataId,
                (int)syncDataType);

            var query = _sessionManager.OpenSession().CreateSQLQuery(sql);
            var objectArrayList = query.List();

            if (objectArrayList.Count > 0)
            {
                object[] objectArray = objectArrayList[0] as object[];

                SyncInfo syncInfo = new SyncInfo();
                syncInfo.AccountId = int.Parse(objectArray[0].ToString());
                syncInfo.LocalDataId = objectArray[1].ToString();
                syncInfo.SyncDataId = objectArray[2].ToString();
                syncInfo.SyncDataType = int.Parse(objectArray[3].ToString());

                return syncInfo;
            }

            return null;
        }
        private SyncInfo GetSyncInfoBySyncDataId(int accountId, string syncDataId, SyncDataType syncDataType)
        {
            var sql = string.Format(
                "select AccountId,LocalDataId,SyncDataId,SyncDataType from Cooper_SyncInfo where AccountId={0} and SyncDataId='{1}' collate Chinese_PRC_CS_AI and SyncDataType={2}",
                accountId,
                syncDataId,
                (int)syncDataType);

            var query = _sessionManager.OpenSession().CreateSQLQuery(sql);
            var objectArrayList = query.List();

            if (objectArrayList.Count > 0)
            {
                object[] objectArray = objectArrayList[0] as object[];

                SyncInfo syncInfo = new SyncInfo();
                syncInfo.AccountId = int.Parse(objectArray[0].ToString());
                syncInfo.LocalDataId = objectArray[1].ToString();
                syncInfo.SyncDataId = objectArray[2].ToString();
                syncInfo.SyncDataType = int.Parse(objectArray[3].ToString());

                return syncInfo;
            }

            return null;
        }

        private ExchangeTask CreateExchangeTask(string subject, string body, DateTime dueTime, TaskStatus status)
        {
            var userCredential = GetExchangeUserCredential();
            var exchangeService = _microsoftExchangeServiceProvider.GetMicrosoftExchangeService(userCredential);

            ExchangeTask task = new ExchangeTask(exchangeService);

            task.Subject = subject;
            task.Body = new MessageBody(BodyType.Text, body);
            task.DueDate = dueTime;
            task.Status = status;
            task.Importance = Importance.High;

            task.Save();

            task = GetExchangeTask(task.Id.UniqueId);

            return task;
        }
        private ExchangeTask UpdateExchangeTask(string id, string subject, string body, DateTime dueTime, TaskStatus status)
        {
            var userCredential = GetExchangeUserCredential();
            var exchangeService = _microsoftExchangeServiceProvider.GetMicrosoftExchangeService(userCredential);

            var task = ExchangeTask.Bind(exchangeService, id, ExchangeSyncSettings.TaskPropertySet);

            task.Subject = subject;
            task.Body = new MessageBody(BodyType.Text, body);
            task.DueDate = dueTime;
            task.Status = status;
            task.Importance = Importance.Low;

            task.Update(ConflictResolutionMode.AlwaysOverwrite);

            task = GetExchangeTask(task.Id.UniqueId);

            return task;
        }
        private ExchangeTask GetExchangeTask(string id)
        {
            var userCredential = GetExchangeUserCredential();
            var exchangeService = _microsoftExchangeServiceProvider.GetMicrosoftExchangeService(userCredential);
            var task = ExchangeTask.Bind(exchangeService, id, ExchangeSyncSettings.TaskPropertySet);

            return task;
        }
        private bool IsExchangeTaskExist(string id)
        {
            var userCredential = GetExchangeUserCredential();
            var exchangeService = _microsoftExchangeServiceProvider.GetMicrosoftExchangeService(userCredential);

            try
            {
                var task = ExchangeTask.Bind(exchangeService, id, ExchangeSyncSettings.TaskPropertySet);
                return task != null;
            }
            catch (ServiceResponseException ex)
            {
                if (ex.ErrorCode == ServiceError.ErrorItemNotFound)
                {
                    return false;
                }
            }

            return false;
        }

        private Appointment CreateExchangeCalendarEvent(string subject, string body, DateTime start, DateTime end)
        {
            var userCredential = GetExchangeUserCredential();
            var exchangeService = _microsoftExchangeServiceProvider.GetMicrosoftExchangeService(userCredential);
            var isDefaultCalendarExist = false;
            var defaultCalendar = DependencyResolver.Resolve<IExchangeCalendarEventSyncDataService>().GetDefaultCalendarFolder(userCredential, out isDefaultCalendarExist);

            Appointment appointment = new Appointment(exchangeService);

            appointment.Subject = subject;
            appointment.Body = new MessageBody(BodyType.Text, body);
            appointment.Start = start;
            appointment.End = end;

            appointment.Save(defaultCalendar.Id);

            appointment = GetExchangeCalendarEvent(appointment.Id.UniqueId);

            return appointment;
        }
        private Appointment UpdateExchangeCalendarEvent(string id, string subject, string body)
        {
            var userCredential = GetExchangeUserCredential();
            var exchangeService = _microsoftExchangeServiceProvider.GetMicrosoftExchangeService(userCredential);
            var appointment = Appointment.Bind(exchangeService, id, ExchangeSyncSettings.CalendarEventPropertySet);

            appointment.Subject = subject;
            appointment.Body = new MessageBody(BodyType.Text, body);

            appointment.Update(ConflictResolutionMode.AlwaysOverwrite);

            appointment = GetExchangeCalendarEvent(appointment.Id.UniqueId);

            return appointment;
        }
        private Appointment GetExchangeCalendarEvent(string id)
        {
            var userCredential = GetExchangeUserCredential();
            var exchangeService = _microsoftExchangeServiceProvider.GetMicrosoftExchangeService(userCredential);
            var appointment = Appointment.Bind(exchangeService, id, ExchangeSyncSettings.CalendarEventPropertySet);

            return appointment;
        }
        private bool IsExchangeCalendarEventExist(string id)
        {
            var userCredential = GetExchangeUserCredential();
            var exchangeService = _microsoftExchangeServiceProvider.GetMicrosoftExchangeService(userCredential);

            try
            {
                var appointment = Appointment.Bind(exchangeService, id, ExchangeSyncSettings.CalendarEventPropertySet);
                return appointment != null;
            }
            catch (ServiceResponseException ex)
            {
                if (ex.ErrorCode == ServiceError.ErrorItemNotFound)
                {
                    return false;
                }
            }

            return false;
        }

        private DateTime FormatTime(DateTime originalTime)
        {
            return new DateTime(originalTime.Year,
                                originalTime.Month,
                                originalTime.Day,
                                originalTime.Hour,
                                originalTime.Minute,
                                originalTime.Second);
        }
        private Priority ConvertToTaskPriority(Importance importance)
        {
            if (importance == Importance.High)
            {
                return Priority.Today;
            }
            else if (importance == Importance.Normal)
            {
                return Priority.Upcoming;
            }
            else if (importance == Importance.Low)
            {
                return Priority.Later;
            }

            return Priority.Today;
        }

        #endregion
    }
}
