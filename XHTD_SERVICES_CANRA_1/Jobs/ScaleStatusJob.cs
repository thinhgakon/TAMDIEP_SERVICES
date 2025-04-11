using Quartz;
using System;
using System.Threading.Tasks;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES_CANRA_1.Hubs;

namespace XHTD_SERVICES_CANRA_1.Jobs
{
    [DisallowConcurrentExecution]
    public class ScaleStatusJob : IJob
    {
        protected readonly Notification _notification;
        protected readonly Logger _logger;
        protected readonly string SCALE_ENV = "SCALE_2_ENV";

        public ScaleStatusJob(Notification notification, Logger logger)
        {
            _notification = notification;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                await Task.Run(() =>
                {
                    SendScaleStatus();
                });
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"RUN JOB ERROR: {ex.Message} --- {ex.StackTrace} --- {ex.InnerException}");

                throw new JobExecutionException(msg: "", refireImmediately: true, cause: ex);
            }
        }

        public void SendScaleStatus()
        {
            var currentScaleIn = Environment.GetEnvironmentVariable("SCALEOUT");
            SendNotificationHub(SCALE_ENV, currentScaleIn);
            SendNotificationAPI(SCALE_ENV, currentScaleIn);
        }

        private void SendNotificationHub(string name, string message)
        {
            new ScaleHub().SendMessage(name, message);
        }

        private void SendNotificationAPI(string name, string message)
        {
            try
            {
                _notification.SendScale2Message(name, message);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SendNotificationAPI ERR: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }
    }
}
