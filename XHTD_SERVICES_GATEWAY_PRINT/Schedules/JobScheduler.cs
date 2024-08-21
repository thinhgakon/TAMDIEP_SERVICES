using Quartz;
using log4net;
using XHTD_SERVICES_GATEWAY_PRINT.Jobs;

namespace XHTD_SERVICES_GATEWAY_PRINT.Schedules
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

            IJobDetail syncOrderJob = JobBuilder.Create<PrintJob>().Build();
            ITrigger syncOrderTrigger = TriggerBuilder.Create()
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(10)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(syncOrderJob, syncOrderTrigger);
        }
    }
}
