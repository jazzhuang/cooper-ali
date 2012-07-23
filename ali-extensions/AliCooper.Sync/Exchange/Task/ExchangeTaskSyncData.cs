using System;
using Cooper.Sync;
using Microsoft.Exchange.WebServices.Data;

namespace AliCooper.Sync
{
    public class ExchangeTaskSyncData : ISyncData
    {
        public ExchangeTaskSyncData(Task exchangeTask)
        {
            ExchangeTask = exchangeTask;
            IsFromDefault = true;
        }

        public Task ExchangeTask { get; private set; }

        public string Id
        {
            get
            {
                return ExchangeTask.Id.UniqueId;
            }
        }
        public string Subject
        {
            get { return ExchangeTask.Subject; }
        }
        public DateTime LastUpdateLocalTime
        {
            get
            {
                return ExchangeTask.LastModifiedTime.ToLocalTime();
            }
            set
            { }
        }

        public string SyncId { get; set; }
        public int SyncType { get; set; }
        public bool IsFromDefault { get; set; }
    }
}
