using log4net.Repository.Hierarchy;
using Quartz;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES_TRAM951_2;
using XHTD_SERVICES_TRAM951_2.Devices;

namespace XHTD_SERVICES_TRAM951_2.Jobs
{
    public class ConnectPegasusJob : IJob
    {
        private byte ComAddr = 0xFF;
        private int PortHandle = 6000;
        private string PegasusAdr = Program.PegasusIP1;
        protected readonly Logger _logger;

        public ConnectPegasusJob(Logger gatewayLogger)
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
                var openresult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr, ref Program.RefComAdr1, ref port);
                while (openresult != 0)
                {
                    openresult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr, ref Program.RefComAdr1, ref port);
                    Thread.Sleep(1000);
                }
                _logger.LogWarn("Connect fail. Start reconnect");
            }
        }
    }
}
