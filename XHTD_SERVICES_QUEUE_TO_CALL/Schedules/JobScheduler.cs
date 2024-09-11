using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using XHTD_SERVICES_QUEUE_TO_TROUGH.Jobs;
using System.Configuration;

namespace XHTD_SERVICES_QUEUE_TO_TROUGH.Schedules
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

            // Xếp số vào máng xi bao
            IJobDetail queueToCallXibaoJob = JobBuilder.Create<QueueToCallXibaoJob>().Build();
            ITrigger queueToCallXibaoTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Queue_To_Call_Interval_In_Seconds")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(queueToCallXibaoJob, queueToCallXibaoTrigger);

            // Xếp số vào máng xi rời
            IJobDetail queueToCallRoiJob = JobBuilder.Create<QueueToCallRoiJob>().Build();
            ITrigger queueToCallRoiTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Queue_To_Call_Interval_In_Seconds")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(queueToCallRoiJob, queueToCallRoiTrigger);

            // Xếp số vào máng clinker
            IJobDetail queueToCallClinkerJob = JobBuilder.Create<QueueToCallClinkerJob>().Build();
            ITrigger queueToCallClinkerTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Queue_To_Call_Interval_In_Seconds")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(queueToCallClinkerJob, queueToCallClinkerTrigger);

            // Xếp số vào máng jumbo
            IJobDetail queueToCallJumboJob = JobBuilder.Create<QueueToCallJumboJob>().Build();
            ITrigger queueToCallJumboTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Queue_To_Call_Interval_In_Seconds")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(queueToCallJumboJob, queueToCallJumboTrigger);
        }
    }
}
