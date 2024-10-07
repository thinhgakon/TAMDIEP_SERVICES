using Quartz;
using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES_CONFIRM.Devices;
using log4net;

namespace XHTD_SERVICES_CONFIRM.Jobs
{
    [DisallowConcurrentExecution]
    public class ReconnectPegasusJob : IJob
    {
        ILog _logger = LogManager.GetLogger("ReconnectFileAppender");

        private byte ComAddr = 0xFF;
        private int PortHandle = 6000;
        private string PegasusAdr = "192.168.13.162";

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

            try
            {
                await Task.Run(() =>
                {
                    WriteLogInfo($"--------------- START JOB - IP: {PegasusAdr} ---------------");

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
                if (Program.LastTimeReceivedUHF != null)
                {
                    WriteLogInfo($"1. Thời điểm gần nhất nhận tín hiệu: {Program.LastTimeReceivedUHF}");

                    timeDiffFromLastReceivedUHF = DateTime.Now.Subtract((DateTime)Program.LastTimeReceivedUHF);

                    if (timeDiffFromLastReceivedUHF.TotalSeconds > TIME_TO_RESET)
                    {
                        WriteLogInfo($"2. Quá {TIME_TO_RESET}s không nhận được UHF => reconnect: Now {DateTime.Now.ToString()} --- Last: {Program.LastTimeReceivedUHF}");

                        PegasusStaticClassReader.CloseNetPort(PortHandle);

                        Program.UHFConnected = false;
                    }
                    else
                    {
                        WriteLogInfo($"2. Chưa vượt quá {TIME_TO_RESET}s");
                    }
                }
                else
                {
                    WriteLogInfo($"Chua dat gia tri cho LastTimeReceivedUHF: null");
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
