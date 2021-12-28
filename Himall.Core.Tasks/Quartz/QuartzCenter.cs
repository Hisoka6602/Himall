using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Core.Tasks.Quartz
{
    public class QuartzTaskCenter:ITaskCenter
    {
        //调度器
        IScheduler scheduler;
        //调度器工厂
        ISchedulerFactory factory;

        public QuartzTaskCenter()
        {
            Init();
        }

        private void Init()
        {
            var props = new NameValueCollection
            {
                { "quartz.serializer.type", "binary" }
            };
            this.factory = new StdSchedulerFactory();
            this.scheduler = factory.GetScheduler();
        }

        public void Subscribe(TaskDetail detail)
        {
            var job = BuilderJob(detail);
            var trigger = BuilderTrigger(detail);
            this.scheduler.ScheduleJob(job, trigger);
        }

        private IJobDetail BuilderJob(TaskDetail detail) {
            return JobBuilder.Create<QuartzJob>()
                .WithIdentity(detail.Name, detail.Group)
                .SetJobData(new JobDataMap {
                    { "TaskDetail", detail }
                })
                .Build();
        }

        private ITrigger BuilderTrigger(TaskDetail detail)
        {
            var interval = detail.Interval;
            var builder = TriggerBuilder.Create()
                .WithIdentity(detail.Name + "Trigger", detail.Group);
            if (detail.StartNow)
                builder.StartNow();

            if (interval.Type == TaskIntervalType.Minutes)
            {
                builder.WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(interval.MinutesValue)
                    .RepeatForever());
            }
            else if (interval.Type == TaskIntervalType.Daily)
            {
                builder.WithDailyTimeIntervalSchedule(x =>
                x.StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(interval.DailyValue.Hours, interval.DailyValue.Minutes))
                .EndingDailyAfterCount(1)
                .WithIntervalInHours(1));
            }
            else if (interval.Type == TaskIntervalType.Cron)
            {
                builder.WithCronSchedule(interval.CronExpression);
            }
            return builder.Build();
        }

        public void Start()
        {
            scheduler.Start();
        }
        public void Stop()
        {

            if (this.scheduler != null)
            {
                this.scheduler.Shutdown(true);
            }
        }
    }
}
