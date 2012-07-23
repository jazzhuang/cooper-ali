using Microsoft.Exchange.WebServices.Data;

namespace AliCooper.Sync
{
    public static class ExchangeSyncSettings
    {
        public static readonly string DefaultTaskListName = "CooperTaskList";
        public static readonly string DefaultCalendarName = "CooperCalendar";

        // 表示默认导入日历中从现在开始往后一个月的日历事件
        public static readonly int DefaultCalendarImportTimeMonths = 3;

        //Exchange Task的默认加载属性
        public static readonly PropertySet TaskPropertySet = new PropertySet(
            ItemSchema.Id,
            ItemSchema.Subject,
            ItemSchema.Body,
            TaskSchema.DueDate,
            TaskSchema.Status,
            TaskSchema.Importance,
            ItemSchema.LastModifiedTime);

        //Exchange Calendar Event的默认加载属性
        public static readonly PropertySet CalendarEventPropertySet = new PropertySet(
            ItemSchema.Id,
            ItemSchema.Subject,
            ItemSchema.Body,
            AppointmentSchema.Start,
            AppointmentSchema.End,
            ItemSchema.LastModifiedTime);

        //Exchange Contact的默认加载属性
        public static readonly PropertySet ContactPropertySet = new PropertySet(
            ItemSchema.Id,
            ContactSchema.Surname,
            ContactSchema.PhoneNumbers,
            ContactSchema.EmailAddresses,
            ItemSchema.LastModifiedTime);
    }
}
