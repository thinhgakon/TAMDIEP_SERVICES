using Quartz;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES_CANVAO_2.Devices;
using log4net;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES.Data.Common;

namespace XHTD_SERVICES_CANVAO_2.Jobs
{
    public class ConnectPegasusJob : IJob
    {
        ILog _logger = LogManager.GetLogger("ConnectFileAppender");

        protected readonly Notification _notification;

        private byte ComAddr = 0xFF;
        private int PortHandle = 6000;
        private string PegasusAdr = "192.168.13.182";

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
                    WriteLogInfo($"=================== Start JOB - IP: {PegasusAdr} ===================");

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
                    WriteLogInfo("Ping success");

                    Program.CountToSendFailPing = 0;

                    return;
                }
                else
                {
                    WriteLogInfo("Ping fail");

                    Program.CountToSendFailPing++;

                    WriteLogInfo($"Lần thứ: {Program.CountToSendFailPing}");

                    if (Program.CountToSendFailPing == 3)
                    {
                        WriteLogInfo($"Thời điểm gửi cảnh báo gần nhất: {Program.SendFailPingLastTime}");

                        if (Program.SendFailPingLastTime == null || Program.SendFailPingLastTime < DateTime.Now.AddMinutes(-3))
                        {
                            Program.SendFailPingLastTime = DateTime.Now;

                            // gửi thông báo ping thất bại
                            var pushMessage = $"Cân vào 2: mất kết nối đến anten {PegasusAdr}. Vui lòng báo kỹ thuật kiểm tra";

                            WriteLogInfo($"Gửi cảnh báo: {pushMessage}");

                            SendNotificationByRight(RightCode.SCALE, pushMessage);
                        }

                        Program.CountToSendFailPing = 0;
                    }
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
