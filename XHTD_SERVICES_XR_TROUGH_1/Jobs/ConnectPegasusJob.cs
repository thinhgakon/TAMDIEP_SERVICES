using log4net;
using Quartz;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Common;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES_XR_TROUGH_1.Devices;

namespace XHTD_SERVICES_XR_TROUGH_1.Jobs
{
    [DisallowConcurrentExecution]
    public class ConnectPegasusJob : IJob
    {
        ILog _logger = LogManager.GetLogger("ConnectFileAppender");

        protected readonly Notification _notification;

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

                    if (DeviceCode.CodeDict.TryGetValue(LocationCode.TROUGH_XI_ROI, out var devices))
                    {
                        foreach (var (deviceCode, deviceIp) in devices)
                        {
                            CheckConnection(deviceCode, deviceIp, LocationCode.TROUGH_XI_ROI);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                WriteLogInfo($"RUN JOB ERROR: {ex.Message} --- {ex.StackTrace} --- {ex.InnerException}");

                // do you want the job to refire?
                throw new JobExecutionException(msg: "", refireImmediately: true, cause: ex);
            }
        }

        public void CheckConnection(string deviceCode, string ipAddress, string deviceLocation)
        {
            try
            {
                if (!Program.DeviceLastFailPingTime.ContainsKey(deviceCode))
                {
                    Program.DeviceFailCount[deviceCode] = 0;
                    Program.DeviceLastFailPingTime[deviceCode] = null;
                }

                Ping pingSender = new Ping();
                PingReply reply = pingSender.Send(ipAddress);

                if (reply.Status == IPStatus.Success)
                {
                    WriteLogInfo("Ping success");

                    Program.DeviceFailCount[deviceCode] = 0;

                    SendNotificationHub(deviceCode, "OK");

                    return;
                }
                else
                {
                    WriteLogInfo("Ping fail");

                    SendNotificationHub(deviceCode, "FAILED");

                    Program.DeviceFailCount[deviceCode]++;

                    WriteLogInfo($"Thiết bị: {deviceCode} - IP: {ipAddress} - KHÔNG ping được lần thứ: {Program.DeviceFailCount[deviceCode]}");

                    if (Program.DeviceFailCount[deviceCode] == 3)
                    {
                        WriteLogInfo($"Thiết bị {deviceCode} - Thời điểm gửi cảnh báo gần nhất: {Program.DeviceLastFailPingTime[deviceCode]}");

                        if (Program.DeviceLastFailPingTime[deviceCode] == null || Program.DeviceLastFailPingTime[deviceCode] < DateTime.Now.AddMinutes(-3))
                        {
                            Program.DeviceLastFailPingTime[deviceCode] = DateTime.Now;

                            // gửi thông báo ping thất bại
                            var pushMessage = $"{deviceLocation}: mất kết nối đến thiết bị: {deviceCode} - IP: {ipAddress}. Vui lòng báo kỹ thuật kiểm tra";

                            WriteLogInfo($"Gửi cảnh báo: {pushMessage}");

                            SendNotificationByRight(RightCode.TROUGH_XI_ROI, pushMessage, "SYSTEM");
                        }

                        Program.DeviceFailCount[deviceCode] = 0;
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

        public void SendNotificationByRight(string rightCode, string message, string notificationType)
        {
            try
            {
                WriteLogInfo($"Gửi push notification đến các user với quyền {rightCode}, nội dung {message}");
                _notification.SendNotificationByRight(rightCode, message, notificationType);
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
