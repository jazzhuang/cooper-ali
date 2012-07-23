using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using CodeSharp.Core;
using Cooper.Sync;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AliCooper.Sync
{
    public interface IAliyunDao
    {
        string GetAccessToken();
        bool IsEmailExist(string email);
        AliYunCalendarEventsQueryResult QueryAliYunCalendarEvents(string email, DateTime startTime, DateTime endTime);
        AliYunCalendarEventQueryResult QueryAliYunCalendarEvent(string email, string id);
        CalendarEventCreateResult CreateAliYunCalendarEvent(string email, string subject, string body, DateTime startTime, DateTime endTime);
        CalendarEventUpdateResult UpdateAliYunCalendarEvent(string email, string id, string subject, string body);
        CalendarEventDeleteResult DeleteAliYunCalendarEvent(string email, string id);
    }
    [Component]
    public class AliyunDao : IAliyunDao
    {
        public string GetAccessToken()
        {
            var token = new Token();
            token.accessTarget = string.Empty; //登录时不需要这两个参数
            token.accessToken = string.Empty;

            var access = new Access();
            access.accessCode = AliYunSyncSettings.AccessCode;
            access.accessPassword = AliYunSyncSettings.AccessPassword;

            var loginParameter = new LoginParameter();
            loginParameter.access = token;
            loginParameter.param = access;

            var parameter = JsonConvert.SerializeObject(loginParameter);
            var client = new WebClient { Encoding = Encoding.UTF8 };
            var result = client.UploadString(AliYunSyncSettings.LoginUrl, parameter);
            var data = JObject.Parse(result)["data"];

            if (data != null)
            {
                var accessToken = data["accessToken"];
                if (accessToken != null)
                {
                    return accessToken.ToString();
                }
            }

            return null;
        }
        public bool IsEmailExist(string email)
        {
            //TODO,应该通过SearchAccount方法实现该功能，但目前阿里云接口还有点问题，暂时总是返回true.
            return true;
        }
        public AliYunCalendarEventsQueryResult QueryAliYunCalendarEvents(string email, DateTime startTime, DateTime endTime)
        {
            var timeSpan = System.TimeZone.CurrentTimeZone.GetUtcOffset(startTime);
            var timeZone = timeSpan.ToString().Substring(0, timeSpan.ToString().IndexOf(':')) + "00";
            if (!timeZone.StartsWith("-"))
            {
                timeZone = "+" + timeZone;
            }

            var beginTimeValue = startTime.ToString("yyyy/MM/dd HH:mm:ss ") + timeZone;
            var endTimeValue = endTime.ToString("yyyy/MM/dd HH:mm:ss ") + timeZone;

            var parameter = new JObject(
                new JProperty("access",
                    new JObject {
                        new JProperty("accessToken", GetAccessToken()),
                        new JProperty("accessTarget", email)
                    }
                ),
                new JProperty("param",
                    new JObject(
                        new JProperty("isExtractRecurrence", "0"),
                        new JProperty("folderIds", new JArray("1")),
                        new JProperty("beginTime", beginTimeValue),
                        new JProperty("endTime", endTimeValue)
                    )
                )
            ).ToString();

            var client = new WebClient { Encoding = Encoding.UTF8 };
            var result = client.UploadString(AliYunSyncSettings.QueryCalendarEventsUrl, parameter);

            var queryResult = JsonConvert.DeserializeObject<AliYunCalendarEventsQueryResult>(result);

            if (queryResult.status.statusCode == AliYunSyncSettings.SuccessStatusCode)
            {
                foreach (var calendarItem in queryResult.data.singleItemList)
                {
                    var timeZoneList = calendarItem.timeZoneList;
                    if (timeZoneList.Count() > 0)
                    {
                        var tzoffsetFrom = timeZoneList.First().standardcList.First().tzoffsetFrom;
                        if (!string.IsNullOrEmpty(tzoffsetFrom))
                        {
                            var offsetHours = double.Parse(tzoffsetFrom) / 1000 / 60 / 60;
                            calendarItem.lastModified = new Time(calendarItem.lastModified.ToDateTime(offsetHours));
                        }
                    }
                    //这里因为返回列表的接口aliyun没有返回详情字段信息，所以还需要再调用一次接口获取详情信息
                    //已联系aliyun接口负责人，希望他们能把详情信息也返回，不知道是否处于性能问题才不返回该字段？
                    var evnt = QueryAliYunCalendarEvent(email, calendarItem.uid);
                    calendarItem.description = evnt.data.calendar.eventList.First().description;
                }
            }

            return queryResult;
        }
        public AliYunCalendarEventQueryResult QueryAliYunCalendarEvent(string email, string id)
        {
            var parameter = new JObject(
                new JProperty("access",
                    new JObject {
                        new JProperty("accessToken", GetAccessToken()),
                        new JProperty("accessTarget", email)
                    }
                ),
                new JProperty("param",
                    new JObject(
                        new JProperty("calendarId", id)
                    )
                )
            ).ToString();

            var client = new WebClient { Encoding = Encoding.UTF8 };
            var result = client.UploadString(AliYunSyncSettings.QueryCalendarEventUrl, parameter);

            AliYunCalendarEventQueryResult queryResult = JsonConvert.DeserializeObject<AliYunCalendarEventQueryResult>(result);

            if (queryResult.status.statusCode == AliYunSyncSettings.SuccessStatusCode)
            {
                var timeZoneList = queryResult.data.calendar.timeZoneList;
                if (timeZoneList.Count() > 0)
                {
                    var timeZone = timeZoneList.First();
                    var tzoffsetFrom = timeZone.standardcList.First().tzoffsetFrom;
                    if (!string.IsNullOrEmpty(tzoffsetFrom))
                    {
                        var offsetHours = double.Parse(tzoffsetFrom) / 1000 / 60 / 60;
                        var evnt = queryResult.data.calendar.eventList.First();
                        evnt.lastModified = new Time(evnt.lastModified.ToDateTime(offsetHours));
                    }
                }
            }

            return queryResult;
        }
        public CalendarEventCreateResult CreateAliYunCalendarEvent(string email, string subject, string body, DateTime startTime, DateTime endTime)
        {
            var calendar = CreateAliYunCalendarEvent(subject, body, startTime, endTime);

            var parameter = new JObject(
                new JProperty("access",
                    new JObject {
                        new JProperty("accessToken", GetAccessToken()),
                        new JProperty("accessTarget", email)
                    }
                ),
                new JProperty("param",
                    new JObject(
                        new JProperty("sendNotify", "0"),
                        new JProperty("revision", "0"),
                        new JProperty("calendar", JObject.FromObject(calendar))
                    )
                )
            ).ToString();

            var client = new WebClient { Encoding = Encoding.UTF8 };
            var result = client.UploadString(AliYunSyncSettings.CreateCalendarEventUrl, parameter);

            return JsonConvert.DeserializeObject<CalendarEventCreateResult>(result);
        }
        public CalendarEventUpdateResult UpdateAliYunCalendarEvent(string email, string id, string subject, string body)
        {
            var calendarResult = QueryAliYunCalendarEvent(email, id);
            var calendar = calendarResult.data.calendar;
            var calendarEvent = calendar.eventList[0];

            calendarEvent.summary = subject;
            calendarEvent.description = body;

            var parameter = new JObject(
                new JProperty("access",
                    new JObject {
                        new JProperty("accessToken", GetAccessToken()),
                        new JProperty("accessTarget", email)
                    }
                ),
                new JProperty("param",
                    new JObject(
                        new JProperty("sendNotify", "0"),
                        new JProperty("revision", calendarResult.data.revision),
                        new JProperty("calendar", JObject.FromObject(calendarResult.data.calendar))
                    )
                )
            ).ToString();

            var client = new WebClient { Encoding = Encoding.UTF8 };
            var result = client.UploadString(AliYunSyncSettings.UpdateCalendarEventUrl, parameter);

            return JsonConvert.DeserializeObject<CalendarEventUpdateResult>(result);
        }
        public CalendarEventDeleteResult DeleteAliYunCalendarEvent(string email, string id)
        {
            var parameter = new JObject(
                new JProperty("access",
                    new JObject {
                        new JProperty("accessToken", GetAccessToken()),
                        new JProperty("accessTarget", email)
                    }
                ),
                new JProperty("param",
                    new JObject(
                        new JProperty("calendarId", id)
                    )
                )
            ).ToString();


            var client = new WebClient { Encoding = Encoding.UTF8 };
            var result = client.UploadString(AliYunSyncSettings.DeleteCalendarEventUrl, parameter);

            return JsonConvert.DeserializeObject<CalendarEventDeleteResult>(result);
        }

        private AliYunCalendar CreateAliYunCalendarEvent(string subject, string body, DateTime startTime, DateTime endTime)
        {
            var calendar = new AliYunCalendar() { method = "PUBLISH" };
            calendar.timeZoneList = GetDefaultTimeList();
            var calendarEventList = new List<AliYunCalendarEvent>() { new AliYunCalendarEvent() };
            var calendarEvent = calendarEventList.First();

            calendarEvent.summary = subject;
            calendarEvent.description = body;
            calendarEvent.dtStart = new TimeZoneTime(startTime, "0", "CST");
            calendarEvent.dtEnd = new TimeZoneTime(endTime, "0", "CST");

            calendar.eventList = calendarEventList.ToArray();

            return calendar;
        }
        private TimeZone[] GetDefaultTimeList()
        {
            var timeZoneList = new List<TimeZone>() { new TimeZone() { tzId = "CST" } };
            var timeZone = timeZoneList.First();
            var timeZoneDataList = new List<TimeZoneData>() { new TimeZoneData() };
            var timeZoneData = timeZoneDataList.First();
            var dtStart = new Time() { year = "1970", month = "1", day = "1", hour = "0", minute = "0", second = "0" };

            timeZoneData.dtStart = dtStart;
            timeZoneData.tzoffsetFrom = Utils.GetCurrentTimeUtcOffset().TotalMilliseconds.ToString();
            timeZoneData.tzoffsetTo = Utils.GetCurrentTimeUtcOffset().TotalMilliseconds.ToString();
            timeZone.standardcList = timeZoneDataList.ToArray();

            return timeZoneList.ToArray();
        }
    }
}
