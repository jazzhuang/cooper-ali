using System;
using Cooper.Sync;

namespace AliCooper.Sync
{
    public class AliyunCalendarEventSyncData : ISyncData
    {
        public AliyunCalendarEventSyncData()
        {
            IsFromDefault = true;
        }

        /// <summary>
        /// 唯一标识
        /// </summary>
        public string Id { get; set; }
        /// <summary>获取标题/主题
        /// </summary>
        public string Subject { get; set; }
        /// <summary>获取内容/描述
        /// </summary>
        public string Body { get; set; }
        /// <summary>获取截止时间
        /// </summary>
        public DateTime? DueTime { get; set; }
        /// <summary>
        /// 日历事件的开始时间
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 日历事件的结束时间
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>获取最后更新时间
        /// </summary>
        public DateTime LastUpdateLocalTime { get; set; }

        public string SyncId { get; set; }
        public int SyncType { get; set; }
        public bool IsFromDefault { get; set; }
    }
}
