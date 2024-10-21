using log4net;
using Microsoft.AspNet.SignalR.Messaging;
using Quartz;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Common;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES_CONFIRM.Devices;

namespace XHTD_SERVICES_CONFIRM.Jobs
{
    [DisallowConcurrentExecution]
    public class ConnectPegasusJob : IJob
    {
        ILog _logger = LogManager.GetLogger("ConnectFileAppender");

        protected readonly Notification _notification;

        private const string DXT_UHF_2 = "DXT_UHF_2";
        private const string DXT_CAM = "DXT_CAM";
        private const string DXT_DTH = "DXT_DTH";

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
                    WriteLogInfo("--------------- START JOB ---------------");

                    CheckConnection(DXT_UHF_2, DeviceCode.CONFIRM.GetIpAddress(DXT_UHF_2));

                    CheckConnection(DXT_CAM, DeviceCode.CONFIRM.GetIpAddress(DXT_CAM));

                    CheckConnection(DXT_DTH, DeviceCode.CONFIRM.GetIpAddress(DXT_DTH));
                });
            }
            catch (Exception ex)
            {
                WriteLogInfo($"RUN JOB ERROR: {ex.Message} --- {ex.StackTrace} --- {ex.InnerException}");

                // do you want the job to refire?
                throw new JobExecutionException(msg: "", refireImmediately: true, cause: ex);
            }
        }

        public void CheckConnection(string deviceCode, string ipAddress)
        {
            try
            {
                Ping pingSender = new Ping();
                PingReply reply = pingSender.Send(ipAddress);

                if (reply.Status == IPStatus.Success)
                {
                    WriteLogInfo("Ping success");

                    Program.CountToSendFailPing = 0;

                    SendNotificationHub(deviceCode, "OK");

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
                            var pushMessage = $"Điểm xác thực: mất kết nối đến thiết bị {ipAddress}. Vui lòng báo kỹ thuật kiểm tra";

                            WriteLogInfo($"Gửi cảnh báo: {pushMessage}");

                            SendNotificationByRight(RightCode.CONFIRM, pushMessage);
                            SendNotificationHub(deviceCode, "FAILED");
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

        public void SendNotificationHub(string deviceCode, string status)
        {
            try
            {
                WriteLogInfo($"Gửi signalR tín hiệu thiết bị {deviceCode} - trạng thái {status}");
                _notification.SendDeviceStatus(deviceCode, status);
            }
            catch (Exception ex)
            {
                WriteLogInfo($"SendNotificationHub Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }
    }
}
