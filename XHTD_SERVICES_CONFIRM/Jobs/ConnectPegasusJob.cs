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
        private readonly byte ComAddr = 0xFF;
        private readonly int PortHandle1 = 6000;
        private readonly int PortHandle2 = 2000;
        private readonly string PegasusAdr1 = "192.168.13.161";
        private readonly string PegasusAdr2 = "192.168.13.162";
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
                    //Console.WriteLine("Connection ok");
                }
                else
                {
                    int port = PortHandle1;
                    byte comAddr1 = ComAddr;
                    var openresult = PegasusStaticClassReader.OpenNetPort(PortHandle1, PegasusAdr1, ref comAddr1, ref port);
                    while (openresult != 0)
                    {
                        openresult = PegasusStaticClassReader.OpenNetPort(PortHandle1, PegasusAdr1, ref comAddr1, ref port);
                        Thread.Sleep(1000);
                    }
                    _logger.LogWarn($"Connect {PegasusAdr1} fail. Start reconnect");
                }

                if (reply.Status == IPStatus.Success)
                {
                    //Console.WriteLine("Connection ok");
                }
                else
                {
                    int port = PortHandle2;
                    byte comAddr2 = ComAddr;
                    var openresult = PegasusStaticClassReader2.OpenNetPort(PortHandle2, PegasusAdr2, ref comAddr2, ref port);
                    while (openresult != 0)
                    {
                        openresult = PegasusStaticClassReader2.OpenNetPort(PortHandle2, PegasusAdr2, ref comAddr2, ref port);
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
