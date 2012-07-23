namespace AliCooper.Sync
{
    public static class AliYunSyncSettings
    {
        public static string AccessCode = "accesscode001";
        public static string AccessPassword = "hell05a";
        public static string ApiServer = "http://10.249.0.86:20089/alimailws/"; //测试环境
        public static string AliyunDomain = "";
        public static string LoginUrl = ApiServer + "control/wsLogin";
        public static string QueryCalendarEventsUrl = ApiServer + "calendar/queryCalendarItemsByFolderIds";
        public static string QueryCalendarEventUrl = ApiServer + "calendar/loadCalendarItem";
        public static string CreateCalendarEventUrl = ApiServer + "calendar/manipulateCalendarItem";
        public static string UpdateCalendarEventUrl = ApiServer + "calendar/manipulateCalendarItem";
        public static string DeleteCalendarEventUrl = ApiServer + "calendar/removeCalendarItem";
        public static string SuccessStatusCode = "100";

        // 表示默认导入日历中从现在开始往后一个月的日历事件
        public static readonly int DefaultCalendarImportTimeMonths = 1;
    }
}
