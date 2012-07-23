using System;
using Cooper.Sync;
using Microsoft.Exchange.WebServices.Data;

namespace AliCooper.Sync
{
    public class ExchangeCalendarEventSyncData : ISyncData
    {
        public ExchangeCalendarEventSyncData(Appointment exchangeCalendarEvent)
        {
            ExchangeCalendarEvent = exchangeCalendarEvent;
            IsFromDefault = true;
        }

        public Appointment ExchangeCalendarEvent { get; private set; }

        public string Id
        {
            get
            {
                return ExchangeCalendarEvent.Id.UniqueId;
            }
        }
        public string Subject
        {
            get { return ExchangeCalendarEvent.Subject; }
        }
        public DateTime LastUpdateLocalTime
        {
            get
            {
                return ExchangeCalendarEvent.LastModifiedTime.ToLocalTime();
            }
            set
            { }
        }

        public string SyncId { get; set; }
        public int SyncType { get; set; }
        public bool IsFromDefault { get; set; }
    }
}
