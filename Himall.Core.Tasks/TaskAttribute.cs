using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Core.Tasks
{
    public class TaskAttribute : Attribute
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 任务描述
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 分组(默认:DefaultGroup)
        /// </summary>
        public string Group { get; } = "DefaultGroup";
        /// <summary>
        /// 立即执行(默认:False)
        /// </summary>
        public bool StartNow { get; set; } = false;

        public TaskInterval Interval { get; }

        /// <summary>
        /// 间隔周期执行
        /// </summary>
        /// <param name="name">任务名</param>
        /// <param name="minutes">间隔分钟</param>
        public TaskAttribute(string name, int interval_minutres)
        {
            this.Name = name;
            this.Interval = new TaskInterval
            {
                Type = TaskIntervalType.Minutes,
                MinutesValue = interval_minutres
            };
        }
        /// <summary>
        /// 每日定点执行
        /// </summary>
        /// <param name="name">任务名</param>
        /// <param name="hours">时</param>
        /// <param name="minutes">分</param>
        public TaskAttribute(string name, int hours, int minutes)
        {
            this.Name = name;
            this.Interval = new TaskInterval
            {
                Type = TaskIntervalType.Daily,
                DailyValue = new TimeSpan(hours, minutes, 0)
            };
        }
        /// <summary>
        /// Cron表达式执行
        /// </summary>
        /// <param name="name"></param>
        /// <param name="corn"></param>
        public TaskAttribute(string name, string cronExpression)
        {
            this.Name = name;
            this.Interval = new TaskInterval
            {
                Type = TaskIntervalType.Cron,
                CronExpression = cronExpression
            };
        }
    }
}
