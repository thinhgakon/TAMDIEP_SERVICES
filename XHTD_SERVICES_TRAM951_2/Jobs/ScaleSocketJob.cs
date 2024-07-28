﻿using Quartz;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES_TRAM951_2;
using XHTD_SERVICES_TRAM951_2.Hubs;

namespace XHTD_SERVICES_TRAM951_2.Jobs
{
    public class ScaleSocketJob : IJob
    {
        private static bool DeviceConnected = false;
        protected readonly Logger _logger;
        private const int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 2022;
        static ASCIIEncoding encoding = new ASCIIEncoding();
        static TcpClient client = new TcpClient();
        static Stream stream = null;
        private readonly Notification _notification;
        private readonly string START_CONNECTION_STR = "hello*mbf*abc123";

        public const string IP_ADDRESS = "192.168.13.203";

        public ScaleSocketJob(Logger logger, Notification notification)
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
            while (!client.Connected)
            {
                    ConnectScaleStationModuleFromController();
            }
            ReadDataFromController();
        }

        public bool ConnectScaleStationModuleFromController()
        {
            _logger.LogInfo("Thuc hien ket noi.");
            try
            {
                _logger.LogInfo("Bat dau ket noi.");
                client = new TcpClient();

                // 1. connect
                client.ConnectAsync(IP_ADDRESS, PORT_NUMBER).Wait(2000);
                stream = client.GetStream();
                _logger.LogInfo("Connected to controller");

                DeviceConnected = true;

                var data = encoding.GetBytes(START_CONNECTION_STR);
                stream.Write(data, 0, data.Length);

                return DeviceConnected;
            }
            catch (Exception ex)
            {
                _logger.LogInfo("Ket noi that bai.");
                _logger.LogInfo(ex.Message);
                _logger.LogInfo(ex.StackTrace);
                return false;
            }
        }

        public void ReadDataFromController()
        {
            if (client.Connected)
            {
                while (client.Connected)
                {
                    try
                    {
                        byte[] data = new byte[BUFFER_SIZE];
                        stream.Read(data, 0, BUFFER_SIZE);
                        var dataStr = encoding.GetString(data);

                        //_logger.LogInfo($"Nhan tin hieu can: {dataStr}");

                        string[] parts = dataStr.Split(new string[] { "tdc" }, StringSplitOptions.None);
                        
                        foreach (var item in parts)
                        {
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

                            if(scaleValue == 0)
                            {
                                continue;
                            }

                            SendScaleInfoAPI(dateTime, scaleValue.ToString());
                            new ScaleHub().ReadDataScale(dateTime, scaleValue.ToString());
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($@"Co loi xay ra khi xu ly du lieu can {ex.StackTrace} {ex.Message} ");
                        continue;
                    }
                }
                AuthenticateScaleStationModuleFromController();
            }
            else
            {
                DeviceConnected = false;
                AuthenticateScaleStationModuleFromController();
            }
        }

        private void SendScaleInfoAPI(DateTime time, string value)
        {
            try
            {
                _notification.SendScale2Info(time, value);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SendScaleInfoAPI Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }
    }
}
