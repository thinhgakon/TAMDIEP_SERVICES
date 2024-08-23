using Quartz;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES_XB_TROUGH_8.Devices;

namespace XHTD_SERVICES_XB_TROUGH_8.Jobs
{
    public class ReconnectPegasusJob : IJob
    {
        private byte ComAddr = 0xFF;
        private int PortHandle = 6000;
        private string PegasusAdr = "192.168.13.219";
        protected readonly TroughLogger _logger;

        public ReconnectPegasusJob(TroughLogger logger)
        {
            _logger = logger;
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
                Ping pingSender = new Ping();
                PingReply reply = pingSender.Send(PegasusAdr);

                if (reply.Status == IPStatus.Success)
                {
                    Console.WriteLine("Connection ok");
                    return;
                }
                else
                {
                    int port = PortHandle;
                    var openresult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr, ref ComAddr, ref port);
                    while (openresult != 0)
                    {
                        openresult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr, ref ComAddr, ref port);
                        Thread.Sleep(1000);
                    }
                    _logger.LogWarn("Connect fail. Start reconnect");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarn($"Ping ERROR: {ex.Message}");
            }
        }
    }
}
