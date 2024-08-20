using Quartz;
using System;
using System.Configuration;
using XHTD_SERVICES_GATEWAY_PRINT.Jobs;

namespace XHTD_SERVICES_GATEWAY_PRINT.Schedules
{
    public class JobScheduler
    {
        private readonly IScheduler _scheduler;

        public JobScheduler(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public async void Start()
        {
            await _scheduler.Start();

            // Đồng bộ đơn hàng
            IJobDetail syncInProgressOrderJob = JobBuilder.Create<PrintJob>().Build();
            ITrigger syncInProgressOrderTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Sync_Order_Interval_In_Seconds")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(syncInProgressOrderJob, syncInProgressOrderTrigger);
        }
    }
}
