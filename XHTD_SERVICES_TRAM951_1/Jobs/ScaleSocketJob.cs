using Autofac;
using Quartz;
using System;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Common;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES_TRAM951_1.Devices;
using XHTD_SERVICES_TRAM951_1.Hubs;

namespace XHTD_SERVICES_TRAM951_1.Jobs
{
    public class ScaleSocketJob : IJob
    {
        private const int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 2022;
        static ASCIIEncoding encoding = new ASCIIEncoding();
        static TcpClient client = null;
        static Stream stream = null;
        private readonly string START_CONNECTION_STR = "hello*mbf*abc123";
        public const string IP_ADDRESS = "192.168.13.206";

        protected readonly Logger _logger;
        private readonly Notification _notification;

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
            while (true)
            {
                var isConnected = ConnectScaleStationModuleFromController();

                if (isConnected)
                {
                    ReadDataFromController();
                }

                Thread.Sleep(500);
            }
        }

        public bool ConnectScaleStationModuleFromController()
        {
            try
            {
                _logger.LogInfo("Bat dau ket noi.");
                client = new TcpClient();

                // 1. connect
                var isConnected = client.ConnectAsync(IP_ADDRESS, PORT_NUMBER).Wait(2000);

                if (!isConnected)
                {
                    // connection failure
                    Console.WriteLine($@"Khong the connect");
                    return false;
                }

                stream = client.GetStream();
                _logger.LogInfo("Ket noi thanh cong");

                var data = encoding.GetBytes(START_CONNECTION_STR);
                stream.Write(data, 0, data.Length);

                return isConnected;
            }
            catch (Exception ex)
            {
                _logger.LogInfo("Ket noi that bai.");
                //_logger.LogInfo(ex.Message);
                //_logger.LogInfo(ex.StackTrace);
                return false;
            }
        }

        public void ReadDataFromController()
        {
            if (client.Connected)
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

                        if (scaleValue == 0)
                        {
                            if (Program.CountScaleZero < 3)
                            {
                                Program.CountScaleZero++;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            Program.CountScaleZero = 0;
                        }

                        Console.WriteLine($"============= {dateTime} {scaleValue.ToString()}");

                        SendScaleInfoAPI(dateTime, scaleValue.ToString());
                        //new ScaleHub().ReadDataScale(dateTime, scaleValue.ToString());
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError($@"Co loi xay ra khi xu ly du lieu can {ex.StackTrace} {ex.Message} ");
                }
                finally {
                    // 5. Close
                    if (stream != null) { 
                        stream.Close();
                    }

                    if(client != null) { 
                        client.Close();
                    }
                }
            }
            else
            {
                _logger.LogError($@"Khong co ket noi");
            }
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
