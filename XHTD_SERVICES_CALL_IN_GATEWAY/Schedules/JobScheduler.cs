﻿using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using XHTD_SERVICES_CALL_IN_GATEWAY.Jobs;
using System.Configuration;

namespace XHTD_SERVICES_CALL_IN_GATEWAY.Schedules
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

            // Gọi loa vào cổng
            IJobDetail gatewayCallJob = JobBuilder.Create<GatewayCallJob>().Build();
            ITrigger gatewayCallTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Sync_Order_Interval_In_Seconds")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(gatewayCallJob, gatewayCallTrigger);
        }
    }
}
