using Quartz;
using log4net;
using XHTD_SERVICES_CALL_IN_GATEWAY.Jobs;

namespace XHTD_SERVICES_CALL_IN_GATEWAY.Schedules
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

            // Gọi xe vào cổng
            IJobDetail gatewayCallJob = JobBuilder.Create<GatewayCallJob>().Build();
            ITrigger gatewayCallTrigger = TriggerBuilder.Create()
                .WithPriority(1)
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInSeconds(13)
                    .RepeatForever())
                .Build();
            await _scheduler.ScheduleJob(gatewayCallJob, gatewayCallTrigger);
        }
    }
}
