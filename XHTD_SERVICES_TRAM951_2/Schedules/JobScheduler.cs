using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using XHTD_SERVICES_TRAM951_2.Jobs;
using System.Configuration;

namespace XHTD_SERVICES_TRAM951_2.Schedules
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

            // Trạm cân 481
            IJobDetail syncOrderJob = JobBuilder.Create<Tram951ModuleJob>().Build();
            ITrigger syncOrderTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInHours(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Tram951_Module_Interval_In_Hours")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(syncOrderJob, syncOrderTrigger);
        }
    }
}
