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
            try
            {
                var currentScaleOut = Environment.GetEnvironmentVariable("SCALEOUT", EnvironmentVariableTarget.Machine);
                _logger.LogInfo($"ENV ========= SCALEOUT ========= {currentScaleOut}");
                SendNotificationAPI(SCALE_ENV, currentScaleOut);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"ENV ========= SCALEOUT ========= {ex.Message} --- {ex.StackTrace} --- {ex.InnerException}");
            }
        }

        private void SendNotificationAPI(string name, string message)
        {
            try
            {
                _notification.SendScale2Message(name, message);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SendNotificationAPI SCALE_ENV ERR: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }
    }
}
