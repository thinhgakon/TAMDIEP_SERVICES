using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using XHTD_SERVICES_LED.Jobs;
using System.Configuration;

namespace XHTD_SERVICES_LED.Schedules
{
    public class JobScheduler
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IScheduler _scheduler;

        public JobScheduler(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public async void Start()
        {
            await _scheduler.Start();

            IJobDetail showLed1XiBaoJob = JobBuilder.Create<Led12XiBaoJob>().Build();
            ITrigger showLed1XiBaoTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(1)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(showLed1XiBaoJob, showLed1XiBaoTrigger);

            IJobDetail showLed3XiBaoJob = JobBuilder.Create<Led34XiBaoJob>().Build();
            ITrigger showLed3XiBaoTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(1)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(showLed3XiBaoJob, showLed3XiBaoTrigger);

            IJobDetail led12RealtimeJob = JobBuilder.Create<Led12RealtimeJob>().Build();
            ITrigger led12RealtimeTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInHours(87600)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(led12RealtimeJob, led12RealtimeTrigger);

            IJobDetail led34RealtimeJob = JobBuilder.Create<Led34RealtimeJob>().Build();
            ITrigger led34RealtimeTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInHours(87600)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(led34RealtimeJob, led34RealtimeTrigger);
        }
    }
}
