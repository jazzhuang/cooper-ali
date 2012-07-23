using System;

namespace AliCooper.Sync
{
    #region AliYun相关DTO对象定义

    public class AliYunCalendarEventsQueryResult
    {
        public ResultStatus status { get; set; }
        public AliYunCalendarItemResultData data { get; set; }
    }
    public class AliYunCalendarEventQueryResult
    {
        public ResultStatus status { get; set; }
        public AliYunCalendarResultData data { get; set; }
    }
    public class AliYunCalendarResultData
    {
        public string revision { get; set; }
        public AliYunCalendar calendar { get; set; }
    }
    public class AliYunCalendarItemResultData
    {
        public AliYunCalendarItem[] singleItemList { get; set; }
    }
    public class CalendarEventCreateResult
    {
        public ResultStatus status { get; set; }
        public CalendarEventCreateResultData data { get; set; }
    }
    public class CalendarEventCreateResultData
    {
        public string calendarId { get; set; }
    }
    public class CalendarEventUpdateResult
    {
        public ResultStatus status { get; set; }
        public CalendarEventUpdateResultData data { get; set; }
    }
    public class CalendarEventUpdateResultData
    {
        public string calendarId { get; set; }
    }
    public class CalendarEventDeleteResult
    {
        public ResultStatus status { get; set; }
        public CalendarEventDeleteResultData data { get; set; }
    }
    public class CalendarEventDeleteResultData
    {
    }
    public class ResultStatus
    {
        public string statusCode { get; set; }
        public string statusMsg { get; set; }
    }
    public class AliYunCalendarItem
    {
        public string method { get; set; }
        public TimeZone[] timeZoneList { get; set; }
        public string uid { get; set; }
        public string summary { get; set; }
        public string description { get; set; }
        public string seq { get; set; }
        public Time created { get; set; }
        public TimeZoneTime dtStart { get; set; }
        public TimeZoneTime dtEnd { get; set; }
        public Time lastModified { get; set; }
    }
    public class AliYunCalendar
    {
        public string method { get; set; }
        public AliYunCalendarEvent[] eventList { get; set; }
        public TimeZone[] timeZoneList { get; set; }
    }
    public class AliYunCalendarEvent
    {
        public string uid { get; set; }
        public string summary { get; set; }
        public string description { get; set; }
        public string seq { get; set; }
        public Time created { get; set; }
        public TimeZoneTime dtStart { get; set; }
        public TimeZoneTime dtEnd { get; set; }
        public Time lastModified { get; set; }
    }
    public class TimeZone
    {
        public string tzId { get; set; }
        public TimeZoneData[] standardcList { get; set; }
    }
    public class TimeZoneData
    {
        public Time dtStart { get; set; }
        public string tzoffsetFrom { get; set; }
        public string tzoffsetTo { get; set; }
    }
    public class Time
    {
        public string year { get; set; }
        public string month { get; set; }
        public string day { get; set; }
        public string hour { get; set; }
        public string minute { get; set; }
        public string second { get; set; }

        public Time()
        {
        }
        public Time(DateTime time)
        {
            year = time.Year.ToString();
            month = time.Month.ToString();
            day = time.Day.ToString();
            hour = time.Hour.ToString();
            minute = time.Minute.ToString();
            second = time.Second.ToString();
        }

        public virtual DateTime ToDateTime(double offsetHours = 0)
        {
            int yearValue = int.Parse(year);
            int monthValue = int.Parse(month);
            int dayValue = int.Parse(day);
            int hourValue = !string.IsNullOrEmpty(hour) ? int.Parse(hour) : 0;
            int minuteValue = !string.IsNullOrEmpty(minute) ? int.Parse(minute) : 0;
            int secondValue = !string.IsNullOrEmpty(second) ? int.Parse(second) : 0;

            return new DateTime(yearValue, monthValue, dayValue, hourValue, minuteValue, secondValue).AddHours(offsetHours);
        }
    }
    public class TimeZoneTime : Time
    {
        public TimeZoneTime() { }
        public TimeZoneTime(DateTime time)
        {
            year = time.Year.ToString();
            month = time.Month.ToString();
            day = time.Day.ToString();
            hour = time.Hour.ToString();
            minute = time.Minute.ToString();
            second = time.Second.ToString();
        }
        public TimeZoneTime(DateTime time, string isUTC, string tzId) : this(time)
        {
            this.isUTC = isUTC;
            this.tzId = tzId;
        }
        public string isUTC { get; set; }
        public string tzId { get; set; }
    }
    public class LoginParameter
    {
        public Token access { get; set; }
        public object param { get; set; }
    }
    public class Token
    {
        public string accessToken { get; set; }
        public string accessTarget { get; set; }
    }
    public class Access
    {
        public string accessCode { get; set; }
        public string accessPassword { get; set; }
    }

    #endregion
}
