﻿using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using XHTD_SERVICES_XR_TROUGH_1.Jobs;
using System.Configuration;

namespace XHTD_SERVICES_XR_TROUGH_1.Schedules
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

            IJobDetail xibaoTroughJob = JobBuilder.Create<TroughJob>().Build();
            ITrigger xibaoTroughTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(2)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(xibaoTroughJob, xibaoTroughTrigger);

            IJobDetail ledJob = JobBuilder.Create<LedJob>().Build();
            ITrigger ledTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(10)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(ledJob, ledTrigger);
        }
    }
}
