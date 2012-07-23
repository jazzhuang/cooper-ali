using System.Collections.Generic;
using Cooper.Sync;
using Microsoft.Exchange.WebServices.Data;

namespace AliCooper.Sync
{
    public class TaskSyncExchangeTaskDataService : ISyncDataService<TaskSyncData, ExchangeTaskSyncData>
    {
        public IList<TaskSyncData> GetSyncDataList()
        {
            return new List<TaskSyncData>();
        }
        public TaskSyncData CreateFrom(ExchangeTaskSyncData syncDataSource)
        {
            TaskSyncData taskSyncData = new TaskSyncData();

            taskSyncData.Subject = syncDataSource.ExchangeTask.Subject;
            if (syncDataSource.ExchangeTask.Body != null)
            {
                taskSyncData.Body = syncDataSource.ExchangeTask.Body.Text;
            }
            taskSyncData.DueTime = syncDataSource.ExchangeTask.DueDate;
            if (syncDataSource.ExchangeTask.Status == TaskStatus.Completed)
            {
                taskSyncData.IsCompleted = true;
            }
            else
            {
                taskSyncData.IsCompleted = false;
            }

            taskSyncData.Priority = ExchangeSyncHelper.ConvertToTaskPriority(syncDataSource.ExchangeTask.Importance);

            return taskSyncData;
        }
        public void UpdateSyncData(TaskSyncData syncData, ExchangeTaskSyncData syncDataSource)
        {
            syncData.Subject = syncDataSource.ExchangeTask.Subject;
            syncData.Body = syncDataSource.ExchangeTask.Body.Text;
            syncData.DueTime = syncDataSource.ExchangeTask.DueDate;
            if (syncDataSource.ExchangeTask.Status == TaskStatus.Completed)
            {
                syncData.IsCompleted = true;
            }
            else
            {
                syncData.IsCompleted = false;
            }

            syncData.Priority = ExchangeSyncHelper.ConvertToTaskPriority(syncDataSource.ExchangeTask.Importance);
        }
    }
}
