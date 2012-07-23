using System;
using System.Collections.Generic;
using System.Linq;
using CodeSharp.Core;
using CodeSharp.Core.Services;
using Cooper.Sync;

namespace AliCooper.Sync
{
    public interface IAliyunCalendarEventSyncDataService : IAliYunSyncService, ISyncDataService<AliyunCalendarEventSyncData, TaskSyncData>
    {
    }
    public class AliyunCalendarEventSyncDataService : IAliyunCalendarEventSyncDataService
    {
        private string _email;
        private IAliyunDao _aliyunDataAccess;
        private ILog _logger;

        public AliyunCalendarEventSyncDataService(IAliyunDao aliyunDataAccess, ILoggerFactory loggerFactory)
        {
            _aliyunDataAccess = aliyunDataAccess;
            _logger = loggerFactory.Create(GetType());
        }

        public IList<AliyunCalendarEventSyncData> GetSyncDataList()
        {
            var items = new List<AliyunCalendarEventSyncData>();

            var startTime = DateTime.Now.Date;
            var endTime = startTime.AddMonths(AliYunSyncSettings.DefaultCalendarImportTimeMonths);

            var result = _aliyunDataAccess.QueryAliYunCalendarEvents(_email, startTime, endTime);

            if (result.status.statusCode != AliYunSyncSettings.SuccessStatusCode)
            {
                throw new Exception(result.status.statusMsg);
            }

            var calendarItems = result.data.singleItemList;

            if (calendarItems != null && calendarItems.Count() > 0)
            {
                foreach (var calendarItem in calendarItems)
                {
                    var data = new AliyunCalendarEventSyncData();

                    data.Id = calendarItem.uid;
                    data.Subject = calendarItem.summary;
                    data.Body = calendarItem.description;
                    data.StartTime = calendarItem.dtStart.ToDateTime();
                    data.EndTime = calendarItem.dtEnd.ToDateTime();
                    data.DueTime = calendarItem.dtEnd.ToDateTime();
                    data.LastUpdateLocalTime = calendarItem.lastModified.ToDateTime();

                    items.Add(data);
                }
            }

            return items;
        }
        public AliyunCalendarEventSyncData CreateFrom(TaskSyncData syncDataSource)
        {
            AliyunCalendarEventSyncData data = new AliyunCalendarEventSyncData();

            data.Subject = syncDataSource.Subject;
            data.Body = syncDataSource.Body;

            DateTime endTime = syncDataSource.DueTime != null ? syncDataSource.DueTime.Value : syncDataSource.CreateTime.AddHours(3);

            data.StartTime = endTime.AddHours(-1);
            data.EndTime = endTime;

            return data;
        }
        public void UpdateSyncData(AliyunCalendarEventSyncData syncData, TaskSyncData syncDataSource)
        {
            syncData.Subject = syncDataSource.Subject;
            syncData.Body = syncDataSource.Body;
        }
        public void SetEmail(string email)
        {
            _email = email;
        }
    }
}
