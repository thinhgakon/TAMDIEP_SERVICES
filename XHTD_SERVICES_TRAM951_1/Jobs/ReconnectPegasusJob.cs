using Quartz;
using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES_TRAM951_1.Devices;
using log4net;

namespace XHTD_SERVICES_TRAM951_1.Jobs
{
    public class ReconnectPegasusJob : IJob
    {
        ILog _logger = LogManager.GetLogger("ConnectFileAppender");

        private byte ComAddr = 0xFF;
        private int PortHandle = 6000;
        private string PegasusAdr = "192.168.13.181";

        protected const int TIME_TO_RESET = 10;

        TimeSpan timeDiffFromLastReceivedUHF = new TimeSpan();

        public ReconnectPegasusJob()
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
                if (Program.LastTimeReceivedUHF != null)
                {
                    timeDiffFromLastReceivedUHF = DateTime.Now.Subtract((DateTime)Program.LastTimeReceivedUHF);

                    if (timeDiffFromLastReceivedUHF.TotalSeconds > TIME_TO_RESET)
                    {
                        WriteLogInfo($"Quá 5s không nhận được UHF => reconnect: Now {DateTime.Now.ToString()} --- Last: {Program.LastTimeReceivedUHF}");

                        PegasusStaticClassReader.CloseNetPort(PortHandle);

                        Program.UHFConnected = false;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo($"RECONNECT ERROR: {ex.Message}");
            }
        }

        public void WriteLogInfo(string message)
        {
            Console.WriteLine(message);
            _logger.Info(message);
        }
    }
}
