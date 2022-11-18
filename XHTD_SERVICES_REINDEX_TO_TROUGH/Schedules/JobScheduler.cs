using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using XHTD_SERVICES_REINDEX_TO_TROUGH.Jobs;
using System.Configuration;

namespace XHTD_SERVICES_REINDEX_TO_TROUGH.Schedules
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
            IJobDetail syncOrderJob = JobBuilder.Create<ReindexToTroughJob>().Build();
            ITrigger syncOrderTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Sync_Order_Interval_In_Seconds")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(syncOrderJob, syncOrderTrigger);

            // Đồng bộ trạng thái đơn hàng
            /*
            IJobDetail smsJob = JobBuilder.Create<SyncOrderStatusJob>().Build();
            ITrigger smsTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(2)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(smsJob, smsTrigger);
            */
        }
    }
}
