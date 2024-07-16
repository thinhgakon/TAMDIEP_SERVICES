using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using XHTD_SERVICES_SYNC_ORDER.Jobs;
using System.Configuration;

namespace XHTD_SERVICES_SYNC_ORDER.Schedules
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
            IJobDetail syncInProgressOrderJob = JobBuilder.Create<SyncInProgressOrderJob>().Build();
            ITrigger syncInProgressOrderTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Sync_Order_Interval_In_Seconds")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(syncInProgressOrderJob, syncInProgressOrderTrigger);

            // Đồng bộ đơn hàng booked
            IJobDetail syncBookedOrderJob = JobBuilder.Create<SyncBookedOrderJob>().Build();
            ITrigger syncBookedOrderTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Sync_Booked_Order_Interval_In_Seconds")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(syncBookedOrderJob, syncBookedOrderTrigger);

            // Đồng bộ đơn hàng changed
            IJobDetail syncChangedOrderJob = JobBuilder.Create<SyncChangedOrderJob>().Build();
            ITrigger syncChangedOrderTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Sync_Booked_Changed_Interval_In_Seconds")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(syncChangedOrderJob, syncChangedOrderTrigger);

            // Đồng bộ đơn hàng từ View Oracle
            IJobDetail syncBookedOrderFromViewJob = JobBuilder.Create<SyncBookedOrderFromViewJob>().Build();
            ITrigger syncBookedOrderFromViewTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                      .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Sync_Booked_Order_From_View_In_Seconds")))
                     .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(syncBookedOrderFromViewJob, syncBookedOrderFromViewTrigger);
        }
    }
}
