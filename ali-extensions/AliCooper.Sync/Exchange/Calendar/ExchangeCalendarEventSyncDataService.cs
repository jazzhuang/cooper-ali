using System;
using System.Collections.Generic;
using System.Linq;
using CodeSharp.Core;
using CodeSharp.Core.Services;
using Cooper.Sync;
using Microsoft.Exchange.WebServices.Data;

namespace AliCooper.Sync
{
    public interface IExchangeCalendarEventSyncDataService : IExchangeSyncService, ISyncDataService<ExchangeCalendarEventSyncData, TaskSyncData>
    {
        Folder GetDefaultCalendarFolder(ExchangeUserCredential credential, out bool isDefaultTaskListExist);
    }
    public class ExchangeCalendarEventSyncDataService : IExchangeCalendarEventSyncDataService
    {
        private Folder _defaultCalendar;
        private ExchangeUserCredential _credential;
        private ExchangeService _exchangeService;
        private IMicrosoftExchangeServiceProvider _externalServiceProvider;
        private ILog _logger;

        public ExchangeCalendarEventSyncDataService(IMicrosoftExchangeServiceProvider microsoftExchangeServiceProvider, ILoggerFactory loggerFactory)
        {
            _externalServiceProvider = microsoftExchangeServiceProvider;
            _logger = loggerFactory.Create(GetType());
        }

        public IList<ExchangeCalendarEventSyncData> GetSyncDataList()
        {
            bool isDefaultCalendarExist = false;
            var defaultCalendar = GetDefaultCalendarFolder(_credential, out isDefaultCalendarExist);

            var items = new List<ExchangeCalendarEventSyncData>();

            DateTime startTime = DateTime.Now.Date;
            DateTime endTime = startTime.AddMonths(ExchangeSyncSettings.DefaultCalendarImportTimeMonths);

            CalendarView view = new CalendarView(startTime, endTime, int.MaxValue);
            IEnumerable<Appointment> appointments = _exchangeService.FindAppointments(defaultCalendar.Id, view);

            if (appointments != null && appointments.Count() > 0)
            {
                _exchangeService.LoadPropertiesForItems(appointments, ExchangeSyncSettings.CalendarEventPropertySet);
                foreach (var appointment in appointments)
                {
                    if (appointment.Body != null && appointment.Body.BodyType == BodyType.HTML)
                    {
                        string body = string.Empty;
                        try
                        {
                            body = StringHelpers.StripHTML(appointment.Body.Text);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error("取出Exchange Calendar Event的Body的Html遇到异常，详细信息：", ex);
                        }
                        appointment.Body = new MessageBody(BodyType.Text, body);
                    }

                    items.Add(new ExchangeCalendarEventSyncData(appointment));
                }
            }

            return items;
        }
        public ExchangeCalendarEventSyncData CreateFrom(TaskSyncData syncDataSource)
        {
            Appointment appointment = new Appointment(_exchangeService);

            appointment.Subject = syncDataSource.Subject ?? string.Empty;
            appointment.Body = new MessageBody(BodyType.Text, syncDataSource.Body);

            DateTime endTime = syncDataSource.DueTime != null ? syncDataSource.DueTime.Value : syncDataSource.CreateTime.AddHours(3);

            appointment.Start = syncDataSource.CreateTime;
            appointment.End = endTime;

            //foreach (var attendeeEmail in attendeeEmailList)
            //{
            //    appointment.RequiredAttendees.Add(attendeeEmail);
            //}

            return new ExchangeCalendarEventSyncData(appointment);
        }
        public void UpdateSyncData(ExchangeCalendarEventSyncData syncData, TaskSyncData syncDataSource)
        {
            syncData.ExchangeCalendarEvent.Subject = syncDataSource.Subject;
            syncData.ExchangeCalendarEvent.Body = new MessageBody(BodyType.Text, syncDataSource.Body);
        }

        public void SetCredential(ExchangeUserCredential credential)
        {
            _credential = credential;
        }
        public Folder GetDefaultCalendarFolder(ExchangeUserCredential credential, out bool isDefaultCalendarExist)
        {
            isDefaultCalendarExist = false;
            SetCredential(credential);
            if (_defaultCalendar == null)
            {
                _exchangeService = _externalServiceProvider.GetMicrosoftExchangeService(_credential);

                FindFoldersResults findResults = _exchangeService.FindFolders(WellKnownFolderName.Calendar, new FolderView(int.MaxValue));
                var defaultCalendar = findResults.FirstOrDefault(x => x.DisplayName == ExchangeSyncSettings.DefaultCalendarName && x.FolderClass == "IPF.Appointment");

                if (defaultCalendar == null)
                {
                    defaultCalendar = new Folder(_exchangeService);
                    defaultCalendar.DisplayName = ExchangeSyncSettings.DefaultCalendarName;
                    defaultCalendar.FolderClass = "IPF.Appointment";
                    defaultCalendar.Save(WellKnownFolderName.Calendar);
                }
                else
                {
                    isDefaultCalendarExist = true;
                }
                _defaultCalendar = defaultCalendar;
            }

            return _defaultCalendar;
        }
    }
}
