using System.Collections.Generic;
using Cooper.Sync;

namespace AliCooper.Sync
{
    public class TaskSyncAliyunCalendarEventDataService : ISyncDataService<TaskSyncData, AliyunCalendarEventSyncData>
    {
        public IList<TaskSyncData> GetSyncDataList()
        {
            return new List<TaskSyncData>();
        }
        public TaskSyncData CreateFrom(AliyunCalendarEventSyncData syncDataSource)
        {
            TaskSyncData taskSyncData = new TaskSyncData();

            taskSyncData.Subject = syncDataSource.Subject;
            taskSyncData.Body = syncDataSource.Body;
            taskSyncData.DueTime = syncDataSource.DueTime;

            return taskSyncData;
        }
        public void UpdateSyncData(TaskSyncData syncData, AliyunCalendarEventSyncData syncDataSource)
        {
            syncData.Subject = syncDataSource.Subject;
            syncData.Body = syncDataSource.Body;
            syncData.DueTime = syncDataSource.DueTime;
        }
    }
}
