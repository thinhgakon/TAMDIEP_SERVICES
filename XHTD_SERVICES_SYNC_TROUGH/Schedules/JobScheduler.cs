using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using XHTD_SERVICES_SYNC_TROUGH.Jobs;
using System.Configuration;

namespace XHTD_SERVICES_SYNC_TROUGH.Schedules
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

            IJobDetail syncOrderJob = JobBuilder.Create<SyncTroughJob12>().Build();
            ITrigger syncOrderTrigger = TriggerBuilder.Create()
                .StartNow()
                .WithPriority(2)
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(1)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(syncOrderJob, syncOrderTrigger);

            IJobDetail syncOrderJob34 = JobBuilder.Create<SyncTroughJob34>().Build();
            ITrigger syncOrderTrigger34 = TriggerBuilder.Create()
                .WithPriority(2)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(1)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(syncOrderJob34, syncOrderTrigger34);

            IJobDetail machineJob12 = JobBuilder.Create<MachineJob12>().Build();
            ITrigger machineJob12Trigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(5)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(machineJob12, machineJob12Trigger);

            IJobDetail machineJob34 = JobBuilder.Create<MachineJob34>().Build();
            ITrigger machineJob34Trigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(5)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(machineJob34, machineJob34Trigger);

            //IJobDetail controlPlc12AndRealtimeJob = JobBuilder.Create<ControlPlc12AndRealtime>().Build();
            //ITrigger controlPlc12AndRealtimeTrigger = TriggerBuilder.Create()
            //    .WithPriority(1)
            //     .StartNow()
            //     .WithSimpleSchedule(x => x
            //         .WithIntervalInHours(87600)
            //        .RepeatForever())
            //    .Build();
            //await _scheduler.ScheduleJob(controlPlc12AndRealtimeJob, controlPlc12AndRealtimeTrigger);

            //IJobDetail controlPlc34AndRealtimeJob = JobBuilder.Create<ControlPlc34AndRealtime>().Build();
            //ITrigger controlPlc34AndRealtimeTrigger = TriggerBuilder.Create()
            //    .WithPriority(1)
            //     .StartNow()
            //     .WithSimpleSchedule(x => x
            //         .WithIntervalInHours(87600)
            //        .RepeatForever())
            //    .Build();
            //await _scheduler.ScheduleJob(controlPlc34AndRealtimeJob, controlPlc34AndRealtimeTrigger);
        }
    }
}
