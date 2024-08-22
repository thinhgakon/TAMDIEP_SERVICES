using Quartz;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES_GATEWAY_OUT;
using XHTD_SERVICES_GATEWAY_OUT.Devices;

namespace XHTD_SERVICES_GATEWAY.Jobs
{
    public class ReconnectPegasusJob : IJob
    {
        private byte ComAddr = 0xFF;
        private int PortHandle = 6000;
        private string PegasusAdr = "192.168.13.170";
        protected readonly GatewayLogger _logger;

        protected const int TIME_TO_RESET = 10;

        TimeSpan timeDiffFromLastReceivedUHF = new TimeSpan();

        public ReconnectPegasusJob(GatewayLogger gatewayLogger)
        {
            _logger = gatewayLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(() =>
            {
                CheckConnection();
            });
        }

        public void CheckConnection()
        {
            try
            {
                if (Program.LastTimeReceivedUHF != null)
                {
                    timeDiffFromLastReceivedUHF = DateTime.Now.Subtract((DateTime)Program.LastTimeReceivedUHF);

                    if (timeDiffFromLastReceivedUHF.TotalSeconds > TIME_TO_RESET)
                    {
                        _logger.LogInfo($"Quá 5s không nhận được UHF => reconnect: Now {DateTime.Now.ToString()} --- Last: {Program.LastTimeReceivedUHF}");

                        PegasusStaticClassReader.CloseNetPort(PortHandle);

                        Program.UHFConnected = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarn($"RECONNECT ERROR: {ex.Message}");
            }
        }
    }
}
