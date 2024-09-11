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
            IJobDetail queueToTroughXibaoJob = JobBuilder.Create<QueueToTroughXibaoJob>().Build();
            ITrigger queueToTroughXibaoTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Queue_To_Trough_Interval_In_Seconds")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(queueToTroughXibaoJob, queueToTroughXibaoTrigger);

            // Xếp số vào máng xi rời
            IJobDetail queueToTroughRoiJob = JobBuilder.Create<QueueToTroughRoiJob>().Build();
            ITrigger queueToTroughRoiTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Queue_To_Trough_Interval_In_Seconds")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(queueToTroughRoiJob, queueToTroughRoiTrigger);

            // Xếp số vào máng clinker
            IJobDetail queueToTroughClinkerJob = JobBuilder.Create<QueueToTroughClinkerJob>().Build();
            ITrigger queueToTroughClinkerTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Queue_To_Trough_Interval_In_Seconds")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(queueToTroughClinkerJob, queueToTroughClinkerTrigger);

            // Xếp số vào máng jumbo
            IJobDetail queueToTroughJumboJob = JobBuilder.Create<QueueToTroughJumboJob>().Build();
            ITrigger queueToTroughJumboTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(Convert.ToInt32(ConfigurationManager.AppSettings.Get("Queue_To_Trough_Interval_In_Seconds")))
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(queueToTroughJumboJob, queueToTroughJumboTrigger);
        }
    }
}
