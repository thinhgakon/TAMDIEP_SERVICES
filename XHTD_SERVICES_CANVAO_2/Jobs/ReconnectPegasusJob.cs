﻿using Quartz;
using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES_CANVAO_2.Devices;
using log4net;

namespace XHTD_SERVICES_CANVAO_2.Jobs
{
    public class ReconnectPegasusJob : IJob
    {
        ILog _logger = LogManager.GetLogger("ConnectFileAppender");

        private byte ComAddr = 0xFF;
        private int PortHandle = 6000;
        private string PegasusAdr = "192.168.13.185";

        protected const int TIME_TO_RESET = 30;

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
                WriteLogInfo($"=================== Start JOB - IP: {PegasusAdr} ===================");

                CheckConnection();
            });
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
