using Cooper.Sync;

namespace AliCooper.Sync
{
    public interface IExchangeCalendarEventSyncService : IExchangeSyncService, ISyncService<TaskSyncData, ExchangeCalendarEventSyncData, TaskSyncResult>
    {
    }
    public class ExchangeCalendarEventSyncService : SyncService<TaskSyncData, ExchangeCalendarEventSyncData, TaskSyncResult>, IExchangeCalendarEventSyncService
    {
        private IExchangeCalendarEventSyncDataService _syncDataService;

        public ExchangeCalendarEventSyncService(ISyncDataService<TaskSyncData, ExchangeCalendarEventSyncData> localDataService, IExchangeCalendarEventSyncDataService syncDataService)
            : base(localDataService, syncDataService)
        {
            _syncDataService = syncDataService;
        }

        protected override int GetSyncDataType()
        {
            return (int)SyncDataType.ExchangeCalendarEvent;
        }

        public void SetCredential(ExchangeUserCredential credential)
        {
            _syncDataService.SetCredential(credential);
        }
    }
}