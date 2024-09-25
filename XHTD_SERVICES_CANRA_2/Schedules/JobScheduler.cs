using Quartz.Impl;
using Quartz;
using System;
using log4net;
using XHTD_SERVICES_CANRA_2.Jobs;
using System.Configuration;

namespace XHTD_SERVICES_CANRA_2.Schedules
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

            // Trạm cân 951
            IJobDetail autoScaleJob = JobBuilder.Create<Tram951ModuleJob>().Build();
            ITrigger autoScaleTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInHours(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Tram951_Module_Interval_In_Hours")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(autoScaleJob, autoScaleTrigger);

            IJobDetail reConnectPegasusJob = JobBuilder.Create<ReconnectPegasusJob>().Build();
            ITrigger reConnectPegasusrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(15)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(reConnectPegasusJob, reConnectPegasusrigger);

            //IJobDetail connectPegasusJob = JobBuilder.Create<ConnectPegasusJob>().Build();
            //ITrigger connectPegasusrigger = TriggerBuilder.Create()
            //    .WithPriority(1)
            //     .StartNow()
            //     .WithSimpleSchedule(x => x
            //         .WithIntervalInSeconds(5)
            //        .RepeatForever())
            //    .Build();
            //await _scheduler.ScheduleJob(connectPegasusJob, connectPegasusrigger);

            IJobDetail scaleSocketJob = JobBuilder.Create<ScaleSocketJob>().Build();
            ITrigger scaleSocketTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInHours(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Scale_Module_Interval_In_Hours")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(scaleSocketJob, scaleSocketTrigger);

            //IJobDetail trafficLightJob = JobBuilder.Create<TrafficLightJob>().Build();
            //ITrigger trafficLightTrigger = TriggerBuilder.Create()
            //    .WithPriority(1)
            //     .StartNow()
            //     .WithSimpleSchedule(x => x
            //         .WithIntervalInSeconds(5)
            //        .RepeatForever())
            //    .Build();
            //await _scheduler.ScheduleJob(trafficLightJob, trafficLightTrigger);

            //IJobDetail resetTrafficLightJob = JobBuilder.Create<ResetTrafficLightJob>().Build();
            //ITrigger resetTrafficLightTrigger = TriggerBuilder.Create()
            //    .WithPriority(1)
            //     .StartNow()
            //     .WithSimpleSchedule(x => x
            //         .WithIntervalInSeconds(5)
            //        .RepeatForever())
            //    .Build();
            //await _scheduler.ScheduleJob(resetTrafficLightJob, resetTrafficLightTrigger);

            //IJobDetail ledJob = JobBuilder.Create<LedJob>().Build();
            //ITrigger ledTrigger = TriggerBuilder.Create()
            //    .WithPriority(1)
            //     .StartNow()
            //     .WithSimpleSchedule(x => x
            //         .WithIntervalInSeconds(1)
            //        .RepeatForever())
            //    .Build();
            //await _scheduler.ScheduleJob(ledJob, ledTrigger);
        }
    }
}
