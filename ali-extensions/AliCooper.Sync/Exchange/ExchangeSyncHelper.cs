using Cooper.Sync;
using Microsoft.Exchange.WebServices.Data;

namespace AliCooper.Sync
{
    public class ExchangeSyncHelper
    {
        public static Importance ConvertToExchangeImportance(TaskSyncDataPriority priority)
        {
            if (priority == TaskSyncDataPriority.Today)
            {
                return Importance.High;
            }
            else if (priority == TaskSyncDataPriority.Upcoming)
            {
                return Importance.Normal;
            }
            else if (priority == TaskSyncDataPriority.Later)
            {
                return Importance.Low;
            }

            return Importance.High;
        }
        public static TaskSyncDataPriority ConvertToTaskPriority(Importance importance)
        {
            if (importance == Importance.High)
            {
                return TaskSyncDataPriority.Today;
            }
            else if (importance == Importance.Normal)
            {
                return TaskSyncDataPriority.Upcoming;
            }
            else if (importance == Importance.Low)
            {
                return TaskSyncDataPriority.Later;
            }

            return TaskSyncDataPriority.Today;
        }
    }
}
