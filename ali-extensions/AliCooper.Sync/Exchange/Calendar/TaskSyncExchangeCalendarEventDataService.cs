using System.Collections.Generic;
using Cooper.Sync;

namespace AliCooper.Sync
{
    public class TaskSyncExchangeCalendarEventDataService : ISyncDataService<TaskSyncData, ExchangeCalendarEventSyncData>
    {
        public IList<TaskSyncData> GetSyncDataList()
        {
            return new List<TaskSyncData>();
        }
        public TaskSyncData CreateFrom(ExchangeCalendarEventSyncData syncDataSource)
        {
            TaskSyncData taskSyncData = new TaskSyncData();

            taskSyncData.Subject = syncDataSource.ExchangeCalendarEvent.Subject;
            taskSyncData.Body = syncDataSource.ExchangeCalendarEvent.Body;
            taskSyncData.DueTime = syncDataSource.ExchangeCalendarEvent.End;

            return taskSyncData;
        }
        public void UpdateSyncData(TaskSyncData syncData, ExchangeCalendarEventSyncData syncDataSource)
        {
            syncData.Subject = syncDataSource.ExchangeCalendarEvent.Subject;
            syncData.Body = syncDataSource.ExchangeCalendarEvent.Body;
            syncData.DueTime = syncDataSource.ExchangeCalendarEvent.End;
        }
    }
}
