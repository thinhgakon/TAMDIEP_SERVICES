using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using XHTD_SERVICES_PING.Jobs;
using System.Configuration;

namespace XHTD_SERVICES_PING.Schedules
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

            // Gateway Ping server
            IJobDetail gatewayPingJob = JobBuilder.Create<GatewayPingJob>().Build();
            ITrigger gatewayPingTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(10)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(gatewayPingJob, gatewayPingTrigger);

            // Tram9511 Ping server
            //IJobDetail tram9511PingJob = JobBuilder.Create<Tram9511PingJob>().Build();
            //ITrigger tram9511PingTrigger = TriggerBuilder.Create()
            //    .WithPriority(1)
            //     .StartNow()
            //     .WithSimpleSchedule(x => x
            //         .WithIntervalInSeconds(10)
            //        .RepeatForever())
            //    .Build();
            //await _scheduler.ScheduleJob(tram9511PingJob, tram9511PingTrigger);

            //// Tram9512 Ping server
            //IJobDetail tram9512PingJob = JobBuilder.Create<Tram9512PingJob>().Build();
            //ITrigger tram9512PingTrigger = TriggerBuilder.Create()
            //    .WithPriority(1)
            //     .StartNow()
            //     .WithSimpleSchedule(x => x
            //         .WithIntervalInSeconds(10)
            //        .RepeatForever())
            //    .Build();
            //await _scheduler.ScheduleJob(tram9512PingJob, tram9512PingTrigger);

            //// Tram481 Ping server
            //IJobDetail tram481PingJob = JobBuilder.Create<Tram481PingJob>().Build();
            //ITrigger tram481PingTrigger = TriggerBuilder.Create()
            //    .WithPriority(1)
            //     .StartNow()
            //     .WithSimpleSchedule(x => x
            //         .WithIntervalInSeconds(10)
            //        .RepeatForever())
            //    .Build();
            //await _scheduler.ScheduleJob(tram481PingJob, tram481PingTrigger);
        }
    }
}
