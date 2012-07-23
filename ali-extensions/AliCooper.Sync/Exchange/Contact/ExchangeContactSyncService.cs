using Cooper.Sync;

namespace AliCooper.Sync
{
    public interface IExchangeContactSyncService : IExchangeSyncService, ISyncService<ContactSyncData, ExchangeContactSyncData, ContactSyncResult>
    {
    }
    public class ExchangeContactSyncService : SyncService<ContactSyncData, ExchangeContactSyncData, ContactSyncResult>, IExchangeContactSyncService
    {
        private IExchangeContactSyncDataService _syncDataService;

        public ExchangeContactSyncService(ISyncDataService<ContactSyncData, ExchangeContactSyncData> localDataService, IExchangeContactSyncDataService syncDataService)
            : base(localDataService, syncDataService)
        {
            AllowAutoCreateSyncInfo = true;
            _syncDataService = syncDataService;
        }

        protected override int GetSyncDataType()
        {
            return (int)SyncDataType.ExchangeContact;
        }

        public void SetCredential(ExchangeUserCredential credential)
        {
            _syncDataService.SetCredential(credential);
        }
    }
}