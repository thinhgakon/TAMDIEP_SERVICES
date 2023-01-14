using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using XHTD_SERVICES_LED.Jobs;
using System.Configuration;

namespace XHTD_SERVICES_LED.Schedules
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

            IJobDetail showLedBaoJob = JobBuilder.Create<LedXiBao>().Build();
            ITrigger showLedBaoTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Led_Interval_In_Seconds")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(showLedBaoJob, showLedBaoTrigger);

            IJobDetail showLedRoiJob = JobBuilder.Create<LedXiRoi>().Build();
            ITrigger showLedRoiTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Led_Interval_In_Seconds")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(showLedRoiJob, showLedRoiTrigger);
        }
    }
}
