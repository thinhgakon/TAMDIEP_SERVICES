using Quartz;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES_CANRA_2.Devices;
using log4net;

namespace XHTD_SERVICES_CANRA_2.Jobs
{
    public class ConnectPegasusJob : IJob
    {
        ILog _logger = LogManager.GetLogger("ConnectFileAppender");

        private byte ComAddr = 0xFF;
        private int PortHandle = 6000;
        private string PegasusAdr = "192.168.13.187";

        public ConnectPegasusJob()
        {
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
                    //Console.WriteLine("Connection ok");
                    return;
                }
                else
                {
                    WriteLogInfo("Start reconnect...");

                    int port = PortHandle;
                    var openresult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr, ref ComAddr, ref port);
                    while (openresult != 0)
                    {
                        openresult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr, ref ComAddr, ref port);
                        Thread.Sleep(1000);
                    }

                    WriteLogInfo("Reconnect success");
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo($"PING ERROR: {ex.Message}");
            }
        }

        public void WriteLogInfo(string message)
        {
            Console.WriteLine(message);
            _logger.Info(message);
        }
    }
}
