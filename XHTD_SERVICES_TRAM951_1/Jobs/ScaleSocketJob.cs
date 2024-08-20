using Autofac;
using Quartz;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
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
        private static bool DeviceConnected = false;
        protected readonly Logger _logger;
        private const int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 2022;
        static ASCIIEncoding encoding = new ASCIIEncoding();
        static TcpClient client = new TcpClient();
        static Stream stream = null;
        private readonly Notification _notification;
        private readonly string START_CONNECTION_STR = "hello*mbf*abc123";

        public const string IP_ADDRESS = "192.168.13.206";

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
            //_logger.LogInfo("Thuc hien ket noi.");
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
                //_logger.LogInfo(ex.Message);
                //_logger.LogInfo(ex.StackTrace);
                return false;
            }
        }

        public void ReadDataFromController()
        {
            while (true)
            {
                try
                {
                    byte[] data = new byte[BUFFER_SIZE];
                    stream.ReadAsync(data, 0, BUFFER_SIZE).Wait(1000);
                    var dataStr = encoding.GetString(data);

                    _logger.LogInfo($"Nhan tin hieu can: {dataStr}");

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

                        Console.WriteLine($"dateTime: {dateTime} --- scaleValue: {scaleValue.ToString()}");

                        Program.LastTimeReceivedScaleSocket = DateTime.Now;

                        Console.WriteLine($"================= Program.LastTimeReceivedScaleSocket: {Program.LastTimeReceivedScaleSocket}");

                        SendScaleInfoAPI(dateTime, scaleValue.ToString());
                        new ScaleHub().ReadDataScale(dateTime, scaleValue.ToString());
                    }

                    TimeSpan span = DateTime.Now.Subtract((DateTime)Program.LastTimeReceivedScaleSocket);

                    Console.WriteLine("Time Difference (seconds): " + span.TotalSeconds);

                    if(span.TotalSeconds > 5)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($@"Co loi xay ra khi xu ly du lieu can {ex.StackTrace} {ex.Message} ");
                    break;
                }
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
