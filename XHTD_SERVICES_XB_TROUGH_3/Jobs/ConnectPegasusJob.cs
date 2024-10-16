using log4net;
using Quartz;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES_XB_TROUGH_3.Devices;

namespace XHTD_SERVICES_XB_TROUGH_3.Jobs
{
    public class ConnectPegasusJob : IJob
    {
        ILog _logger = LogManager.GetLogger("ConnectFileAppender");

        protected readonly Notification _notification;

        private byte ComAddr = 0xFF;
        private int PortHandle = 6000;
        private string PegasusAdr = "192.168.13.196";

        public ConnectPegasusJob(Notification notification)
        {
            _notification = notification;
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
                Ping pingSender = new Ping();
                PingReply reply = pingSender.Send(PegasusAdr);

                if (reply.Status == IPStatus.Success)
                {
                    WriteLogInfo("Ping ok");
                    return;
                }
                else
                {
                    WriteLogInfo("Ping fail");

                    int port = PortHandle;
                    var openresult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr, ref ComAddr, ref port);

                    if (openresult != 0)
                    {
                        WriteLogInfo($"Open netPort KHONG thanh cong: PegasusAdr={PegasusAdr} -- port={port} --  openResult={openresult}");
                    }
                    else
                    {
                        WriteLogInfo($"Open netPort thanh cong: PegasusAdr={PegasusAdr} -- port={port} --  openResult={openresult}");
                    }

                    WriteLogInfo("Connect fail. Start reconnect");
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo($"Ping ERROR: {ex.Message}");
            }
        }

        public void WriteLogInfo(string message)
        {
            Console.WriteLine(message);
            _logger.Info(message);
        }

        public void SendNotificationByRight(string rightCode, string message)
        {
            try
            {
                WriteLogInfo($"Gửi push notification đến các user với quyền {rightCode}, nội dung {message}");
                _notification.SendNotificationByRight(rightCode, message);
            }
            catch (Exception ex)
            {
                WriteLogInfo($"SendNotificationByRight Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }
    }
}
