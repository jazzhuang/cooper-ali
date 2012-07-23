using System.Collections.Generic;
using System.Linq;
using Cooper.Sync;
using Microsoft.Exchange.WebServices.Data;

namespace AliCooper.Sync
{
    public interface IExchangeContactSyncDataService : IExchangeSyncService, ISyncDataService<ExchangeContactSyncData, ContactSyncData>
    {
    }
    public class ExchangeContactSyncDataService : IExchangeContactSyncDataService
    {
        private ExchangeUserCredential _credential;
        private ExchangeService _exchangeService;
        private IMicrosoftExchangeServiceProvider _externalServiceProvider;

        public ExchangeContactSyncDataService(IMicrosoftExchangeServiceProvider microsoftExchangeServiceProvider)
        {
            _externalServiceProvider = microsoftExchangeServiceProvider;
        }

        public IList<ExchangeContactSyncData> GetSyncDataList()
        {
            _exchangeService = _externalServiceProvider.GetMicrosoftExchangeService(_credential);

            var view = new ItemView(int.MaxValue, 0);
            var exchangeContacts = _exchangeService.FindItems(WellKnownFolderName.Contacts, view);
            var items = new List<ExchangeContactSyncData>();

            if (exchangeContacts != null && exchangeContacts.Count() > 0)
            {
                foreach (Contact exchangeContact in exchangeContacts)
                {
                    items.Add(new ExchangeContactSyncData(exchangeContact));
                }
            }

            return items;
        }
        public ExchangeContactSyncData CreateFrom(ContactSyncData syncDataSource)
        {
            Contact contact = new Contact(_exchangeService);

            contact.Surname = syncDataSource.FullName ?? string.Empty;
            contact.PhoneNumbers[PhoneNumberKey.BusinessPhone] = syncDataSource.Phone;
            contact.EmailAddresses[EmailAddressKey.EmailAddress1] = new EmailAddress(syncDataSource.Email);

            return new ExchangeContactSyncData(contact);
        }
        public void UpdateSyncData(ExchangeContactSyncData syncData, ContactSyncData syncDataSource)
        {
            syncData.ExchangeContact.Surname = syncDataSource.FullName;
            syncData.ExchangeContact.PhoneNumbers[PhoneNumberKey.BusinessPhone] = syncDataSource.Phone;
            syncData.ExchangeContact.EmailAddresses[EmailAddressKey.EmailAddress1] = syncDataSource.Email;
        }

        public void SetCredential(ExchangeUserCredential credential)
        {
            _credential = credential;
        }
    }
}
