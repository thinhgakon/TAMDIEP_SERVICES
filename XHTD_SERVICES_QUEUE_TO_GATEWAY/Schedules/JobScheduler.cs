using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using XHTD_SERVICES_QUEUE_TO_GATEWAY.Jobs;
using System.Configuration;

namespace XHTD_SERVICES_QUEUE_TO_GATEWAY.Schedules
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

            // Đưa Clinker vào hàng đợi gọi loa
            IJobDetail queueToGatewayClinkerJob = JobBuilder.Create<QueueToGatewayExportPlanClinkerJob>().Build();
            ITrigger queueToGatewayClinkerTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(5)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(queueToGatewayClinkerJob, queueToGatewayClinkerTrigger);

            // Đưa Rời vào hàng đợi gọi loa
            IJobDetail queueToGatewayRoiJob = JobBuilder.Create<QueueToGatewayExportPlanRoiJob>().Build();
            ITrigger queueToGatewayRoiTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(5)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(queueToGatewayRoiJob, queueToGatewayRoiTrigger);

            // Đưa PCB40 vào hàng đợi gọi loa
            IJobDetail queueToGatewayPcb40Job = JobBuilder.Create<QueueToGatewayExportPlanPcb40Job>().Build();
            ITrigger queueToGatewayPcb40Trigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(5)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(queueToGatewayPcb40Job, queueToGatewayPcb40Trigger);

            // Đưa PCB30 vào hàng đợi gọi loa
            IJobDetail queueToGatewayPcb30Job = JobBuilder.Create<QueueToGatewayExportPlanPcb30Job>().Build();
            ITrigger queueToGatewayPcb30Trigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(5)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(queueToGatewayPcb30Job, queueToGatewayPcb30Trigger);

            // Đưa C91 vào hàng đợi gọi loa
            IJobDetail queueToGatewayC91Job = JobBuilder.Create<QueueToGatewayExportPlanC91Job>().Build();
            ITrigger queueToGatewayC91Trigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(5)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(queueToGatewayC91Job, queueToGatewayC91Trigger);

            // Đưa Jumbo vào hàng đợi gọi loa
            IJobDetail queueToGatewayJumboJob = JobBuilder.Create<QueueToGatewayExportPlanJumboJob>().Build();
            ITrigger queueToGatewayJumboTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(5)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(queueToGatewayJumboJob, queueToGatewayJumboTrigger);

            // Đưa Sling vào hàng đợi gọi loa
            IJobDetail queueToGatewaySlingJob = JobBuilder.Create<QueueToGatewayExportPlanSlingJob>().Build();
            ITrigger queueToGatewaySlingTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(5)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(queueToGatewaySlingJob, queueToGatewaySlingTrigger);

            // Đưa Other vào hàng đợi gọi loa
            IJobDetail queueToGatewayOtherJob = JobBuilder.Create<QueueToGatewayExportPlanOtherJob>().Build();
            ITrigger queueToGatewayOtherTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(5)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(queueToGatewayOtherJob, queueToGatewayOtherTrigger);
        }
    }
}
