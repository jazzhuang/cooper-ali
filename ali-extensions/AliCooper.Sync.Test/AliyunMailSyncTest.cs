using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AliCooper.Model.Accounts;
using CodeSharp.Core.Services;
using Cooper.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using CooperTask = Cooper.Model.Tasks.PersonalTask;
using MicrosoftAssert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace AliCooper.Sync.Test
{
    [TestFixture]
    [TestClass]
    public class AliyunSyncTest : TestBase
    {
        private int _connectionId = 19; //宋五提供的aliyun账号连接

        /// <summary>
        /// 测试与Aliyun同步的方法1
        /// </summary>
        [Test]
        [TestMethod]
        public void Test1_Sync_Tasks_And_CalendarEvents_With_Aliyun()
        {
            Sync_Create_Task_AccordingWith_AliyunCalendarEvent_Test();
            Sync_Update_Task_AccordingWith_AliyunCalendarEvent_Test();
            Sync_Create_AliyunCalendarEvent_AccordingWith_Task_Test();
            Sync_Update_AliyunCalendarEvent_AccordingWith_Task_Test();
            Sync_Delete_AliyunCalendarEvent_AccordingWith_Task_Test();
        }

        #region Aliyun 日历事件同步测试

        //由于任务系统不支持根据Aliyun任务删除任务的功能，所以这个测试用例不需要写

        /// <summary>
        /// 测试同步时根据Aliyun日历事件创建任务
        /// </summary>
        [Test]
        [TestMethod]
        public void Sync_Create_Task_AccordingWith_AliyunCalendarEvent_Test()
        {
            _logger.Info("--------------开始执行测试：Sync_Create_Task_AccordingWith_AliyunCalendarEvent_Test");
            InitializeAccountAndConnection(_connectionId);

            var aliyunCalendarEvent = CreateAliyunCalendarEvent("calendar event", "description of event", DateTime.Now, DateTime.Now.AddHours(2));
            MicrosoftAssert.IsNotNull(aliyunCalendarEvent);

            _syncProcessor.SyncTasksAndContacts(
                _connectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IAliyunCalendarEventSyncService>()
                },
                null);

            var syncInfo = GetSyncInfoBySyncDataId(_account.ID, aliyunCalendarEvent.Id, SyncDataType.AliyunCalendarEvent);
            MicrosoftAssert.IsNotNull(syncInfo);

            var cooperTask = GetCooperTask(long.Parse(syncInfo.LocalDataId));
            aliyunCalendarEvent = GetAliyunCalendarEvent(aliyunCalendarEvent.Id);
            MicrosoftAssert.IsNotNull(cooperTask);
            AssertTaskAndAliyunCalendarEventAreEqual(cooperTask, aliyunCalendarEvent);
        }
        /// <summary>
        /// 测试同步时根据Aliyun日历事件更新任务
        /// </summary>
        [Test]
        [TestMethod]
        public void Sync_Update_Task_AccordingWith_AliyunCalendarEvent_Test()
        {
            _logger.Info("--------------开始执行测试：Sync_Update_Task_AccordingWith_AliyunCalendarEvent_Test");
            InitializeAccountAndConnection(_connectionId);

            var aliyunCalendarEvent = CreateAliyunCalendarEvent("calendar event", "description of event", DateTime.Now, DateTime.Now.AddHours(2));
            MicrosoftAssert.IsNotNull(aliyunCalendarEvent);

            _syncProcessor.SyncTasksAndContacts(
                _connectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IAliyunCalendarEventSyncService>()
                },
                null);

            var syncInfo = GetSyncInfoBySyncDataId(_account.ID, aliyunCalendarEvent.Id, SyncDataType.AliyunCalendarEvent);
            MicrosoftAssert.IsNotNull(syncInfo);

            var cooperTask = GetCooperTask(long.Parse(syncInfo.LocalDataId));
            MicrosoftAssert.IsNotNull(cooperTask);
            AssertTaskAndAliyunCalendarEventAreEqual(cooperTask, aliyunCalendarEvent);

            Thread.Sleep(1000 * 2);
            aliyunCalendarEvent = UpdateAliyunCalendarEvent(aliyunCalendarEvent.Id, aliyunCalendarEvent.Subject + "_updated", aliyunCalendarEvent.Body + "_updated");

            _syncProcessor.SyncTasksAndContacts(
                _connectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IAliyunCalendarEventSyncService>()
                },
                null);

            cooperTask = GetCooperTask(cooperTask.ID);
            aliyunCalendarEvent = GetAliyunCalendarEvent(aliyunCalendarEvent.Id);
            MicrosoftAssert.IsNotNull(cooperTask);
            AssertTaskAndAliyunCalendarEventAreEqual(cooperTask, aliyunCalendarEvent);
        }
        /// <summary>
        /// 测试同步时根据任务创建Aliyun Calendar Event
        /// </summary>
        [Test]
        [TestMethod]
        public void Sync_Create_AliyunCalendarEvent_AccordingWith_Task_Test()
        {
            _logger.Info("--------------开始执行测试：Sync_Create_AliyunCalendarEvent_AccordingWith_Task_Test");
            InitializeAccountAndConnection(_connectionId);

            var cooperTask = CreateCooperTask("cooper task", "description of task", DateTime.Now.Date.AddDays(2), false);
            MicrosoftAssert.IsNotNull(cooperTask);

            _syncProcessor.SyncTasksAndContacts(
                _connectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IAliyunCalendarEventSyncService>()
                },
                null);

            var syncInfo = GetSyncInfoByLocalDataId(_account.ID, cooperTask.ID.ToString(), SyncDataType.AliyunCalendarEvent);
            MicrosoftAssert.IsNotNull(syncInfo);

            cooperTask = GetCooperTask(cooperTask.ID);
            var aliyunCalendarEvent = GetAliyunCalendarEvent(syncInfo.SyncDataId);
            MicrosoftAssert.IsNotNull(aliyunCalendarEvent);
            AssertTaskAndAliyunCalendarEventAreEqual(cooperTask, aliyunCalendarEvent);
        }
        /// <summary>
        /// 测试同步时根据任务更新Aliyun日历事件
        /// </summary>
        [Test]
        [TestMethod]
        public void Sync_Update_AliyunCalendarEvent_AccordingWith_Task_Test()
        {
            _logger.Info("--------------开始执行测试：Sync_Update_AliyunCalendarEvent_AccordingWith_Task_Test");
            InitializeAccountAndConnection(_connectionId);

            var cooperTask = CreateCooperTask("cooper task", "description of task", DateTime.Now.Date.AddDays(2), false);
            MicrosoftAssert.IsNotNull(cooperTask);

            _syncProcessor.SyncTasksAndContacts(
                _connectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IAliyunCalendarEventSyncService>()
                },
                null);

            var syncInfo = GetSyncInfoByLocalDataId(_account.ID, cooperTask.ID.ToString(), SyncDataType.AliyunCalendarEvent);
            MicrosoftAssert.IsNotNull(syncInfo);

            cooperTask = GetCooperTask(cooperTask.ID);
            var aliyunCalendarEvent = GetAliyunCalendarEvent(syncInfo.SyncDataId);
            MicrosoftAssert.IsNotNull(cooperTask);
            AssertTaskAndAliyunCalendarEventAreEqual(cooperTask, aliyunCalendarEvent);

            //更新Task
            cooperTask = UpdateCooperTask(cooperTask.ID, cooperTask.Subject + "_updated", cooperTask.Body + "_updated", cooperTask.DueTime.Value, true);
            UpdateTaskLastUpdateTime(cooperTask, aliyunCalendarEvent.LastUpdateLocalTime.AddSeconds(1));

            //同步
            _syncProcessor.SyncTasksAndContacts(
                _connectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IAliyunCalendarEventSyncService>()
                },
                null);

            //重新获取
            cooperTask = GetCooperTask(cooperTask.ID);
            aliyunCalendarEvent = GetAliyunCalendarEvent(syncInfo.SyncDataId);

            //对比结果
            MicrosoftAssert.IsNotNull(aliyunCalendarEvent);
            AssertTaskAndAliyunCalendarEventAreEqual(cooperTask, aliyunCalendarEvent);
        }
        /// <summary>
        /// 测试同步时根据任务删除Aliyun日历事件
        /// </summary>
        [Test]
        [TestMethod]
        public void Sync_Delete_AliyunCalendarEvent_AccordingWith_Task_Test()
        {
            _logger.Info("--------------开始执行测试：Sync_Delete_AliyunCalendarEvent_AccordingWith_Task_Test");
            InitializeAccountAndConnection(_connectionId);

            var cooperTask = CreateCooperTask("cooper task", "description of task", DateTime.Now.Date.AddDays(2), false);
            MicrosoftAssert.IsNotNull(cooperTask);

            _syncProcessor.SyncTasksAndContacts(
                _connectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IAliyunCalendarEventSyncService>()
                },
                null);

            var syncInfo = GetSyncInfoByLocalDataId(_account.ID, cooperTask.ID.ToString(), SyncDataType.AliyunCalendarEvent);
            MicrosoftAssert.IsNotNull(syncInfo);

            cooperTask = GetCooperTask(cooperTask.ID);
            var aliyunCalendarEvent = GetAliyunCalendarEvent(syncInfo.SyncDataId);
            MicrosoftAssert.IsNotNull(cooperTask);
            AssertTaskAndAliyunCalendarEventAreEqual(cooperTask, aliyunCalendarEvent);

            //删除Task
            DeleteCooperTask(cooperTask.ID);

            //同步
            _syncProcessor.SyncTasksAndContacts(
                _connectionId,
                new List<ISyncService<TaskSyncData, ISyncData, TaskSyncResult>>
                {
                    DependencyResolver.Resolve<IAliyunCalendarEventSyncService>()
                },
                null);

            //重新获取
            var isAliyunCalendarEventExist = IsAliyunCalendarEventExist(syncInfo.SyncDataId);

            //检查结果
            MicrosoftAssert.IsFalse(isAliyunCalendarEventExist);
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

                if (connection.GetType() == typeof(AliyunMailConnection))
                {
                    _aliyunMailConnection = connection as AliyunMailConnection;
                }
            }
        }
        /// <summary>
        /// 获取账号的Aliyun邮箱信息
        /// </summary>
        private string GetAliyunUserEmail()
        {
            if (_aliyunMailConnection != null)
            {
                return _aliyunMailConnection.Name;
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

        private void AssertTaskAndAliyunCalendarEventAreEqual(CooperTask cooperTask, AliyunCalendarEventSyncData aliyunCalendarEvent)
        {
            MicrosoftAssert.AreEqual(cooperTask.Subject, aliyunCalendarEvent.Subject);
            MicrosoftAssert.AreEqual(cooperTask.Body, aliyunCalendarEvent.Body);
            MicrosoftAssert.AreEqual(cooperTask.DueTime, aliyunCalendarEvent.DueTime);
            MicrosoftAssert.AreEqual(FormatTime(cooperTask.LastUpdateTime), FormatTime(aliyunCalendarEvent.LastUpdateLocalTime));
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

        private AliyunCalendarEventSyncData CreateAliyunCalendarEvent(string subject, string body, DateTime start, DateTime end)
        {
            var userEmail = GetAliyunUserEmail();

            var createResult = _aliyunDao.CreateAliYunCalendarEvent(userEmail, subject, body, start, end);
            MicrosoftAssert.AreEqual(createResult.status.statusCode, AliYunSyncSettings.SuccessStatusCode);

            var calendarEventId = createResult.data.calendarId;
            var result = _aliyunDao.QueryAliYunCalendarEvent(userEmail, calendarEventId);

            return BuildAliYunCalendarEventSyncData(result.data.calendar.eventList.First());
        }
        private AliyunCalendarEventSyncData UpdateAliyunCalendarEvent(string id, string subject, string body)
        {
            var userEmail = GetAliyunUserEmail();
            var resultStatus = _aliyunDao.UpdateAliYunCalendarEvent(userEmail, id, subject, body);

            MicrosoftAssert.AreEqual(resultStatus.status.statusCode, AliYunSyncSettings.SuccessStatusCode);

            var result = _aliyunDao.QueryAliYunCalendarEvent(userEmail, id);
            return BuildAliYunCalendarEventSyncData(result.data.calendar.eventList.First());
        }
        private AliyunCalendarEventSyncData GetAliyunCalendarEvent(string id)
        {
            var userEmail = GetAliyunUserEmail();
            var result = _aliyunDao.QueryAliYunCalendarEvent(userEmail, id);

            MicrosoftAssert.IsTrue(result.data.calendar.eventList.Count() == 1);

            return BuildAliYunCalendarEventSyncData(result.data.calendar.eventList.First());
        }
        private bool IsAliyunCalendarEventExist(string id)
        {
            var userEmail = GetAliyunUserEmail();
            var result = _aliyunDao.QueryAliYunCalendarEvent(userEmail, id);

            return result.status.statusCode == AliYunSyncSettings.SuccessStatusCode;
        }

        private AliyunCalendarEventSyncData BuildAliYunCalendarEventSyncData(AliYunCalendarEvent evnt)
        {
            var data = new AliyunCalendarEventSyncData();

            data.Id = evnt.uid;
            data.Subject = evnt.summary;
            data.Body = evnt.description;
            data.DueTime = evnt.dtEnd.ToDateTime();
            data.LastUpdateLocalTime = evnt.lastModified.ToDateTime();

            return data;
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

        #endregion
    }
}
