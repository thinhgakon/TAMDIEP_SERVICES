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

            // Đồng bộ đơn hàng từ View Oracle
            IJobDetail syncBookedOrderFromViewJob = JobBuilder.Create<SyncBookedOrderFromViewJob>().Build();
            ITrigger syncBookedOrderFromViewTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                      .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Sync_Booked_Order_Interval_In_Seconds")))
                     .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(syncBookedOrderFromViewJob, syncBookedOrderFromViewTrigger);

            IJobDetail syncInProgressOrderFromViewJob = JobBuilder.Create<SyncInProgressOrderFromViewJob>().Build();
            ITrigger syncInProgressOrderFromViewTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                      .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Sync_Order_Interval_In_Seconds")))
                     .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(syncInProgressOrderFromViewJob, syncInProgressOrderFromViewTrigger);

            IJobDetail syncChangedOrderFromViewJob = JobBuilder.Create<SyncChangedOrderFromViewJob>().Build();
            ITrigger syncChangedOrderFromViewTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                      .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Sync_Booked_Changed_Interval_In_Seconds")))
                     .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(syncChangedOrderFromViewJob, syncChangedOrderFromViewTrigger);
        }
    }
}
