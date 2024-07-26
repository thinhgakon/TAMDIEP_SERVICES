using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using XHTD_SERVICES_TRAM951_1.Jobs;
using System.Configuration;

namespace XHTD_SERVICES_TRAM951_1.Schedules
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
            IJobDetail syncOrderJob = JobBuilder.Create<Tram951ModuleJob>().Build();
            ITrigger syncOrderTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInHours(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Tram951_Module_Interval_In_Hours")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(syncOrderJob, syncOrderTrigger);

            IJobDetail syncOrderJob2 = JobBuilder.Create<Tram951ModuleJob2>().Build();
            ITrigger syncOrderTrigger2 = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInHours(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Tram951_Module_Interval_In_Hours")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(syncOrderJob2, syncOrderTrigger2);

            IJobDetail scaleSocketJob = JobBuilder.Create<ScaleSocketJob>().Build();
            ITrigger scaleSocketTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInHours(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Scale_Module_Interval_In_Hours")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(scaleSocketJob, scaleSocketTrigger);

            ////// Reset PLC trạm 951
            //IJobDetail resetPLCJob = JobBuilder.Create<Reset951PLCJob>().Build();
            //ITrigger resetPLCTrigger = TriggerBuilder.Create()
            //    .WithPriority(1)
            //     .StartNow()
            //     .WithSimpleSchedule(x => x
            //         .WithIntervalInSeconds(60)
            //        .RepeatForever())
            //    .Build();
            //await _scheduler.ScheduleJob(resetPLCJob, resetPLCTrigger);

            IJobDetail connectPegasusJob = JobBuilder.Create<ConnectPegasusJob>().Build();
            ITrigger connectPegasusrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(5)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(connectPegasusJob, connectPegasusrigger);
        }
    }
}
