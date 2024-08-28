using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;
using RestSharp;
using XHTD_SERVICES_SYNC_TROUGH.Models.Response;
using XHTD_SERVICES.Data.Models.Response;
using Newtonsoft.Json;
using XHTD_SERVICES_SYNC_TROUGH.Models.Values;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES.Helper.Models.Request;
using System.Threading;
using XHTD_SERVICES.Data.Entities;
using System.Text;
using System.Net.Sockets;
using System.IO;
using XHTD_SERVICES.Data.Models.Values;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices.ComTypes;

namespace XHTD_SERVICES_SYNC_TROUGH.Jobs
{
    [DisallowConcurrentExecution]
    public class SyncTroughJobNew12 : IJob
    {
        private static bool DeviceConnected = false;
        protected readonly SyncTroughLogger _logger;
        private const int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 10000;
        static ASCIIEncoding encoding = new ASCIIEncoding();
        static TcpClient client = new TcpClient();
        static Stream stream = null;
        private readonly Notification _notification;
        private readonly string START_CONNECTION_STR = "hello*mbf*abc123";
        private readonly string SEND_TO_RECEIVED_SCALE_CODE = "ww";

        public const string IP_ADDRESS = "192.168.13.189";

        TimeSpan timeDiffFromLastReceivedScaleSocket = new TimeSpan();

        public SyncTroughJobNew12(SyncTroughLogger logger, Notification notification)
        {
            _logger = logger;
            this._notification = notification;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(() =>
            {
                AuthenticateScaleStationModuleFromController();
            });
        }

        public void AuthenticateScaleStationModuleFromController()
        {
            while (true)
            {
                var isConnected = ConnectScaleStationModuleFromController();

                if (isConnected)
                {
                    ReadDataFromController();
                }

                Thread.Sleep(1000);
            }
        }

        public bool ConnectScaleStationModuleFromController()
        {
            try
            {
                _logger.LogInfo("Thuc hien ket noi scale socket");
                client = new TcpClient();

                // 1. connect
                client.ConnectAsync(IP_ADDRESS, PORT_NUMBER).Wait(2000);
                stream = client.GetStream();
                _logger.LogInfo("Ket noi thanh cong");

                DeviceConnected = true;

                //var data = encoding.GetBytes(START_CONNECTION_STR);
                //stream.Write(data, 0, data.Length);

                return DeviceConnected;
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"Ket noi that bai: {ex.Message}");
                return false;
            }
        }

        public void ReadDataFromController()
        {
            while (true)
            {
                try
                {
                    // send
                    byte[] data = encoding.GetBytes(SEND_TO_RECEIVED_SCALE_CODE);
                    stream.Write(data, 0, data.Length);

                    // receive
                    data = new byte[BUFFER_SIZE];
                    stream.Read(data, 0, BUFFER_SIZE);
                    //stream.ReadAsync(data, 0, BUFFER_SIZE).Wait(1000);

                    var dataStr = encoding.GetString(data);

                    //_logger.LogInfo($"Nhan tin hieu can: {dataStr}");

                    string[] parts = dataStr.Split(new string[] { "tdc" }, StringSplitOptions.None);

                    parts = parts.Where(x => !String.IsNullOrEmpty(x.Trim())).ToArray();

                    parts = parts.Where(x => x.Contains("*")).ToArray();

                    if (parts != null && parts.Count() > 0)
                    {
                        var item = parts.First();
                        int scaleValue;
                        System.DateTime dateTime;
                        try
                        {
                            string[] dt = item.Split('*');

                            // Lấy phần số và ngày tháng giờ
                            string number = dt[1];
                            string dateTimeStr = dt[2];
                            scaleValue = int.TryParse(number, out int i) ? i : 0;
                            dateTime = System.DateTime.Parse(dateTimeStr);
                        }
                        catch (Exception)
                        {
                            continue;
                        }

                        _logger.LogInfo($"dateTime: {dateTime} --- scaleValue: {scaleValue.ToString()}");

                        Program.LastTimeReceivedScaleSocket = DateTime.Now;

                        //_logger.LogInfo($"================= Program.LastTimeReceivedScaleSocket: {Program.LastTimeReceivedScaleSocket}");

                        SendScaleInfoAPI(dateTime, scaleValue.ToString());
                        new ScaleHub().ReadDataScale(dateTime, scaleValue.ToString());
                    }

                    if (Program.LastTimeReceivedScaleSocket != null)
                    {
                        timeDiffFromLastReceivedScaleSocket = DateTime.Now.Subtract((DateTime)Program.LastTimeReceivedScaleSocket);

                        if (timeDiffFromLastReceivedScaleSocket.TotalSeconds > 5)
                        {
                            _logger.LogInfo($"Quá 5s không nhận được tín hiệu cân => tiến hành reconnect: Now {DateTime.Now.ToString()} --- Last: {Program.LastTimeReceivedScaleSocket}");

                            if (stream != null) stream.Close();
                            if (client != null) client.Close();

                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($@"Co loi xay ra khi xu ly du lieu can {ex.StackTrace} {ex.Message} ");
                    if (stream != null) stream.Close();
                    if (client != null) client.Close();

                    break;
                }

                Thread.Sleep(500);
            }

            AuthenticateScaleStationModuleFromController();
        }

        private void SendScaleInfoAPI(DateTime time, string value)
        {
            try
            {
                _notification.SendScale1Info(time, value);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SendScaleInfoAPI Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }
    }
}
