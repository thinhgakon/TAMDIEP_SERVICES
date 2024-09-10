using Autofac;
using Quartz;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Common;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES_CANVAO_1.Devices;
using XHTD_SERVICES_CANVAO_1.Hubs;
using log4net;

namespace XHTD_SERVICES_CANVAO_1.Jobs
{
    public class ScaleSocketJob : IJob
    {
        ILog _logger = LogManager.GetLogger("ScaleSocketFileAppender");

        private static bool DeviceConnected = false;
        private const int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 2022;
        static ASCIIEncoding encoding = new ASCIIEncoding();
        static TcpClient client = new TcpClient();
        static Stream stream = null;
        private readonly Notification _notification;
        private readonly string START_CONNECTION_STR = "hello*mbf*abc123";
        private readonly string SEND_TO_RECEIVED_SCALE_CODE = "ww";

        public const string IP_ADDRESS = "192.168.13.206";

        private readonly bool IsWillSendScaleInfoAPI = true;

        TimeSpan timeDiffFromLastReceivedScaleSocket = new TimeSpan();

        public ScaleSocketJob(Notification notification)
        {
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
                WriteLogInfo("Thuc hien ket noi scale socket");
                client = new TcpClient();

                // 1. connect
                client.ConnectAsync(IP_ADDRESS, PORT_NUMBER).Wait(2000);
                stream = client.GetStream();
                WriteLogInfo("Ket noi thanh cong");

                DeviceConnected = true;

                //var data = encoding.GetBytes(START_CONNECTION_STR);
                //stream.Write(data, 0, data.Length);

                return DeviceConnected;
            }
            catch (Exception ex)
            {
                WriteLogInfo($"Ket noi that bai: {ex.Message}");
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

                        WriteLogInfo($"dateTime: {dateTime} --- scaleValue: {scaleValue.ToString()}");

                        Program.LastTimeReceivedScaleSocket = DateTime.Now;

                        if (IsWillSendScaleInfoAPI)
                        {
                            SendScaleInfoAPI(dateTime, scaleValue.ToString());
                        }

                        new ScaleHub().ReadDataScale(dateTime, scaleValue.ToString());
                    }

                    if(Program.LastTimeReceivedScaleSocket != null)
                    { 
                        timeDiffFromLastReceivedScaleSocket = DateTime.Now.Subtract((DateTime)Program.LastTimeReceivedScaleSocket);

                        if(timeDiffFromLastReceivedScaleSocket.TotalSeconds > 5)
                        {
                            WriteLogInfo($"Quá 5s không nhận được tín hiệu cân => tiến hành reconnect: Now {DateTime.Now.ToString()} --- Last: {Program.LastTimeReceivedScaleSocket}");

                            if (stream != null) stream.Close();
                            if (client != null) client.Close();

                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLogInfo($@"Co loi xay ra khi xu ly du lieu can {ex.StackTrace} {ex.Message} ");
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
                WriteLogInfo($"SendScaleInfoAPI Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }

        public void WriteLogInfo(string message)
        {
            Console.WriteLine(message);
            _logger.Info(message);
        }
    }
}
