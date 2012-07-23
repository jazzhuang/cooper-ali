using Cooper.Sync;

namespace AliCooper.Sync
{
    public interface IAliyunCalendarEventSyncService : IAliYunSyncService, ISyncService<TaskSyncData, AliyunCalendarEventSyncData, TaskSyncResult>
    {
    }
    public class AliyunCalendarEventSyncService : SyncService<TaskSyncData, AliyunCalendarEventSyncData, TaskSyncResult>, IAliyunCalendarEventSyncService
    {
        private IAliyunCalendarEventSyncDataService _syncDataService;

        public AliyunCalendarEventSyncService(ISyncDataService<TaskSyncData, AliyunCalendarEventSyncData> localDataService, IAliyunCalendarEventSyncDataService syncDataService)
            : base(localDataService, syncDataService)
        {
            _syncDataService = syncDataService;
            AllowAutoCreateSyncInfo = true;
        }

        protected override int GetSyncDataType()
        {
            return (int)SyncDataType.AliyunCalendarEvent;
        }

        public void SetEmail(string email)
        {
            _syncDataService.SetEmail(email);
        }
    }
}