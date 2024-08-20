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

            IJobDetail showLed1XiBaoJob = JobBuilder.Create<Led1XiBaoJob>().Build();
            ITrigger showLed1XiBaoTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(2000)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(showLed1XiBaoJob, showLed1XiBaoTrigger);

            IJobDetail showLed2XiBaoJob = JobBuilder.Create<Led2XiBaoJob>().Build();
            ITrigger showLed2XiBaoTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(2)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(showLed2XiBaoJob, showLed2XiBaoTrigger);

            IJobDetail showLed3XiBaoJob = JobBuilder.Create<Led3XiBaoJob>().Build();
            ITrigger showLed3XiBaoTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(2)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(showLed3XiBaoJob, showLed3XiBaoTrigger);

            IJobDetail showLed4XiBaoJob = JobBuilder.Create<Led4XiBaoJob>().Build();
            ITrigger showLed4XiBaoTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(2)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(showLed4XiBaoJob, showLed4XiBaoTrigger);
        }
    }
}
