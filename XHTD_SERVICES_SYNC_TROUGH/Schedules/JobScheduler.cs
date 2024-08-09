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

            // Đồng bộ đơn hàng
            //IJobDetail syncOrderJob = JobBuilder.Create<SyncTroughJob>().Build();
            //ITrigger syncOrderTrigger = TriggerBuilder.Create()
            //    .WithPriority(1)
            //     .StartNow()
            //     .WithSimpleSchedule(x => x
            //         .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Sync_Order_Interval_In_Seconds")))
            //        .RepeatForever())
            //    .Build();
            //await _scheduler.ScheduleJob(syncOrderJob, syncOrderTrigger);

            IJobDetail machineJob12 = JobBuilder.Create<MachineJob12>().Build();
            ITrigger machineJob12Trigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInHours(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Machine_Job_Interval")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(machineJob12, machineJob12Trigger);

            IJobDetail machineJob34 = JobBuilder.Create<MachineJob34>().Build();
            ITrigger machineJob34Trigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInHours(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Machine_Job_Interval")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(machineJob12, machineJob12Trigger);
        }
    }
}
