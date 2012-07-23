using System.Collections.Generic;
using Cooper.Sync;

namespace AliCooper.Sync
{
    public class ContactSyncExchangeTaskDataService : ISyncDataService<ContactSyncData, ExchangeContactSyncData>
    {
        public IList<ContactSyncData> GetSyncDataList()
        {
            return new List<ContactSyncData>();
        }
        public ContactSyncData CreateFrom(ExchangeContactSyncData syncDataSource)
        {
            ContactSyncData contactSyncData = new ContactSyncData();

            contactSyncData.FullName = syncDataSource.ExchangeContact.Surname;

            return contactSyncData;
        }
        public void UpdateSyncData(ContactSyncData syncData, ExchangeContactSyncData syncDataSource)
        {
            syncData.FullName = syncDataSource.ExchangeContact.Surname;
        }
    }
}
