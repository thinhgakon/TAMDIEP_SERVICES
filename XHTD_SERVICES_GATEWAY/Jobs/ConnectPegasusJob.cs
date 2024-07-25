using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES_GATEWAY.Devices;

namespace XHTD_SERVICES_GATEWAY.Jobs
{
    public class ConnectPegasusJob : IJob
    {
        private byte ComAddr = 0xFF;
        private int PortHandle = 6000;
        private string PegasusAdr = "192.168.13.168";

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run( () =>
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
                var openresult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr, ref ComAddr, ref port);
                while(openresult != 0)
                {
                    openresult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr, ref ComAddr, ref port);
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
