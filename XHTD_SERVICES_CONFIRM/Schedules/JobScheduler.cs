﻿using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using XHTD_SERVICES_CONFIRM.Jobs;
using System.Configuration;

namespace XHTD_SERVICES_CONFIRM.Schedules
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

            // Xác thực cổng bảo vệ
            IJobDetail syncOrderJob = JobBuilder.Create<ConfirmModuleJob>().Build();
            ITrigger syncOrderTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInHours(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Gateway_Module_Interval_In_Hours")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(syncOrderJob, syncOrderTrigger);

            IJobDetail trafficLightJob = JobBuilder.Create<TrafficLightJob>().Build();
            ITrigger trafficLightTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(5)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(trafficLightJob, trafficLightTrigger);

            IJobDetail reConnectPegasusJob = JobBuilder.Create<ReconnectPegasusJob>().Build();
            ITrigger reConnectPegasusrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(10)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(reConnectPegasusJob, reConnectPegasusrigger);

            IJobDetail ConnectPegasusJob = JobBuilder.Create<ConnectPegasusJob>().Build();
            ITrigger ConnectPegasusTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(5)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(ConnectPegasusJob, ConnectPegasusTrigger);
        }
    }
}
