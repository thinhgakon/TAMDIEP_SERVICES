using log4net;
using Quartz;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES_XB_TROUGH_6.Devices;

namespace XHTD_SERVICES_XB_TROUGH_6.Jobs
{
    public class ConnectPegasusJob : IJob
    {
        ILog _logger = LogManager.GetLogger("ConnectFileAppender");

        private byte ComAddr = 0xFF;
        private int PortHandle = 6000;
        private string PegasusAdr = "192.168.13.214";

        public ConnectPegasusJob()
        {
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
                    WriteLogInfo("--------------- START JOB ---------------");

                    CheckConnection();
                });
            }
            catch (Exception ex)
            {
                WriteLogInfo($"RUN JOB ERROR: {ex.Message} --- {ex.StackTrace} --- {ex.InnerException}");

                // do you want the job to refire?
                throw new JobExecutionException(msg: "", refireImmediately: true, cause: ex);
            }
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
