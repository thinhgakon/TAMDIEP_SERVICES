using Quartz;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES_CONFIRM.Devices;

namespace XHTD_SERVICES_CONFIRM.Jobs
{
    public class ConnectPegasusJob : IJob
    {
        private byte ComAddr = 0xFF;
        private int PortHandle = 6000;
        private string PegasusAdr1 = "192.168.13.161";
        private string PegasusAdr2 = "192.168.13.162";
        protected readonly ConfirmLogger _logger;

        public ConnectPegasusJob(ConfirmLogger logger)
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
                PingReply reply = pingSender.Send(PegasusAdr1);

                if (reply.Status == IPStatus.Success)
                {
                    Console.WriteLine("Connection ok");
                }
                else
                {
                    int port = PortHandle;
                    var openresult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr1, ref ComAddr, ref port);
                    while (openresult != 0)
                    {
                        openresult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr1, ref ComAddr, ref port);
                        Thread.Sleep(1000);
                    }
                    _logger.LogWarn($"Connect {PegasusAdr1} fail. Start reconnect");
                }

                if (reply.Status == IPStatus.Success)
                {
                    Console.WriteLine("Connection ok");
                }
                else
                {
                    int port = PortHandle;
                    var openresult = PegasusStaticClassReader2.OpenNetPort(PortHandle, PegasusAdr2, ref ComAddr, ref port);
                    while (openresult != 0)
                    {
                        openresult = PegasusStaticClassReader2.OpenNetPort(PortHandle, PegasusAdr2, ref ComAddr, ref port);
                        Thread.Sleep(1000);
                    }
                    _logger.LogWarn($"Connect {PegasusAdr2} fail. Start reconnect");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }
    }
}
