using Cooper.Sync;

namespace AliCooper.Sync
{
    public interface IExchangeTaskSyncService : IExchangeSyncService, ISyncService<TaskSyncData, ExchangeTaskSyncData, TaskSyncResult>
    {
    }
    public class ExchangeTaskSyncService : SyncService<TaskSyncData, ExchangeTaskSyncData, TaskSyncResult>, IExchangeTaskSyncService
    {
        private IExchangeTaskSyncDataService _syncDataService;

        public ExchangeTaskSyncService(ISyncDataService<TaskSyncData, ExchangeTaskSyncData> localDataService, IExchangeTaskSyncDataService syncDataService)
            : base(localDataService, syncDataService)
        {
            AllowAutoCreateSyncInfo = true;
            _syncDataService = syncDataService;
        }

        protected override int GetSyncDataType()
        {
            return (int)SyncDataType.ExchangeTask;
        }

        public void SetCredential(ExchangeUserCredential credential)
        {
            _syncDataService.SetCredential(credential);
        }
    }
}