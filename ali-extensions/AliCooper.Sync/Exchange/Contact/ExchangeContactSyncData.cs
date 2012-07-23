using System;
using Cooper.Sync;
using Microsoft.Exchange.WebServices.Data;

namespace AliCooper.Sync
{
    public class ExchangeContactSyncData : ISyncData
    {
        public ExchangeContactSyncData(Contact exchangeContact)
        {
            ExchangeContact = exchangeContact;
            IsFromDefault = true;
        }

        public Contact ExchangeContact { get; private set; }

        public string Id
        {
            get
            {
                return ExchangeContact.Id.UniqueId;
            }
        }
        public string Subject
        {
            get { return ExchangeContact.Surname; }
        }
        public DateTime LastUpdateLocalTime
        {
            get
            {
                return ExchangeContact.LastModifiedTime.ToLocalTime();
            }
            set
            { }
        }

        public string SyncId { get; set; }
        public int SyncType { get; set; }
        public bool IsFromDefault { get; set; }
    }
}
