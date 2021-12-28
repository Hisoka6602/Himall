using System;

namespace Himall.Core.Tasks
{
    /// <summary>
    /// 任务周期
    /// </summary>
    public class TaskInterval
    {
        public TaskIntervalType Type { get; set; }
        public int MinutesValue { get; set; }
        public TimeSpan DailyValue { get; set; }
        public string CronExpression { get; set; }
    }
}
