using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using XHTD_SERVICES_GATEWAY_OUT.Jobs;
using System.Configuration;
using XHTD_SERVICES_GATEWAY.Jobs;

namespace XHTD_SERVICES_GATEWAY_OUT.Schedules
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

            // Xác thực cổng bảo vệ
            IJobDetail syncOrderJob = JobBuilder.Create<GatewayModuleJob>().Build();
            ITrigger syncOrderTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInHours(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Gateway_Module_Interval_In_Hours")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(syncOrderJob, syncOrderTrigger);

            IJobDetail reConnectPegasusJob = JobBuilder.Create<ReconnectPegasusJob>().Build();
            ITrigger reConnectPegasusrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(5)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(reConnectPegasusJob, reConnectPegasusrigger);

            // Reset PLC cổng bảo vệ
            IJobDetail resetPLCJob = JobBuilder.Create<ResetGatewayPLCJob>().Build();
            ITrigger resetPLCTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(10)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(resetPLCJob, resetPLCTrigger);

            IJobDetail captureJob = JobBuilder.Create<CaptureInOutJob>().Build();
            ITrigger captureTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(5)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(captureJob, captureTrigger);

            IJobDetail reconnectJob = JobBuilder.Create<ConnectPegasusJob>().Build();
            ITrigger reconnectTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(5)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(reconnectJob, reconnectTrigger);
        }
    }
}
