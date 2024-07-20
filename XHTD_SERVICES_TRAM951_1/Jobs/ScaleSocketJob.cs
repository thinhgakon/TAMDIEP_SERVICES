using Quartz;
using System;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES_TRAM951_1.Hubs;

namespace XHTD_SERVICES_TRAM951_1.Jobs
{
    public class ScaleSocketJob : IJob
    {
        private static bool DeviceConnected = false;
        protected readonly Logger _logger;
        private const int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 10000;
        static ASCIIEncoding encoding = new ASCIIEncoding();
        static TcpClient client = new TcpClient();
        static Stream stream = null;
        private readonly Notification _notification;

        public const string IP_ADDRESS = "127.0.0.2";

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
            while (!DeviceConnected)
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
            _logger.LogInfo("Reading RFID from Controller ...");

            if (DeviceConnected)
            {
                while (DeviceConnected)
                {
                    try
                    {
                        byte[] data = new byte[BUFFER_SIZE];
                        stream.Read(data, 0, BUFFER_SIZE);
                        var dataStr = encoding.GetString(data);

                        _logger.LogInfo($"Nhan tin hieu can: {dataStr}");

                        string pattern = @"\[Reader\]\[(\d+)\](\d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2})\[!\]";
                        Match match = Regex.Match(dataStr, pattern);

                        int scaleValue; 
                        DateTime dateTime;

                        if (match.Success)
                        {
                            scaleValue = int.TryParse(match.Groups[1].Value, out int i) ? i : 0;
                            dateTime = DateTime.ParseExact(match.Groups[2].Value, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            _logger.LogInfo("Tin hieu can khong dung dinh dang");
                            continue;
                        }

                        SendScale1Info(dateTime, scaleValue.ToString());
                        new ScaleHub().ReadDataScale(dateTime, scaleValue.ToString());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($@"Co loi xay ra khi xu ly du lieu can {ex.StackTrace} {ex.Message} ");
                        continue;
                    }
                }
            }
            else
            {
                DeviceConnected = false;
                AuthenticateScaleStationModuleFromController();
            }
        }
        private void SendScale1Info(DateTime time, string value)
        {
            try
            {
                _notification.SendScale1Info(time, value);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SendScale1Message Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }
    }
}
