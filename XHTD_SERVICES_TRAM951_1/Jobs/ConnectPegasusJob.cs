using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES_TRAM951_1.Devices;

namespace XHTD_SERVICES_TRAM951_1.Jobs
{
    public class ConnectPegasusJob : IJob
    {
        private byte ComAddr = 0xFF;
        private int PortHandle = 6000;
        private string PegasusAdr1 = "192.168.13.181";
        private string PegasusAdr2 = "192.168.13.182";
        protected readonly Logger _logger;

        public ConnectPegasusJob(Logger logger)
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
                PingReply reply1 = pingSender.Send(PegasusAdr1);

                if (reply1.Status == IPStatus.Success)
                {
                    //Console.WriteLine("Connection 181 ok");
                }
                else
                {
                    int port = PortHandle;
                    var openresult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr1, ref ComAddr, ref port);
                    while (openresult != 0)
                    {
                        openresult = PegasusStaticClassReader.CloseNetPort(PortHandle);
                        openresult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr1, ref ComAddr, ref port);
                        Thread.Sleep(1000);
                    }
                    _logger.LogWarn($"Connect {PegasusAdr1} fail. Start reconnect");
                }

                PingReply reply2 = pingSender.Send(PegasusAdr2);
                if (reply1.Status == IPStatus.Success)
                {
                    //Console.WriteLine("Connection 182 ok");
                }
                else
                {
                    int port = PortHandle;
                    var openresult = PegasusStaticClassReader2.OpenNetPort(PortHandle, PegasusAdr1, ref ComAddr, ref port);
                    while (openresult != 0)
                    {
                        openresult = PegasusStaticClassReader2.CloseNetPort(PortHandle);
                        openresult = PegasusStaticClassReader2.OpenNetPort(PortHandle, PegasusAdr1, ref ComAddr, ref port);
                        Thread.Sleep(1000);
                    }
                    _logger.LogWarn($"Connect {PegasusAdr2} fail. Start reconnect");
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
