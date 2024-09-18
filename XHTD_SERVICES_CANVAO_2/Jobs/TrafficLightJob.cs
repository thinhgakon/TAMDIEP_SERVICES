using log4net;
using Quartz;
using SuperSimpleTcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Common;
using XHTD_SERVICES.Helper;

namespace XHTD_SERVICES_CANVAO_2.Jobs
{
    [DisallowConcurrentExecution]
    public class TrafficLightJob : IJob, IDisposable
    {
        ILog _logger = LogManager.GetLogger("TrafficLightFileAppender");

        private readonly Notification _notification;

        static SimpleTcpClient client;
        static ASCIIEncoding encoding = new ASCIIEncoding();
        static string TrafficLightResponse = string.Empty;

        private const string IP_ADDRESS = "192.168.13.185";
        private const int PORT_NUMBER = 10000;
        private const int BUFFER_SIZE = 1024;

        private readonly string SCALE_DGT_CODE = ScaleCode.CODE_SCALE_1_DGT_OUT;

        public TrafficLightJob(Notification notification)
        {
            _notification = notification;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(() =>
            {
                ConnectTrafficLight();
            });
        }

        public void ConnectTrafficLight()
        {
            try
            {
                client = new SimpleTcpClient(IP_ADDRESS, PORT_NUMBER);
                client.Keepalive.EnableTcpKeepAlives = true;
                client.Settings.MutuallyAuthenticate = false;
                client.Settings.AcceptInvalidCertificates = true;
                client.Settings.ConnectTimeoutMs = 2000;
                client.Settings.NoDelay = true;

                client.ConnectWithRetries(2000);

                if (client.IsConnected)
                {
                    WriteLogInfo($"ConnectTrafficLight thanh cong --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}");

                    ReadDataFromTrafficLight();
                }
                else
                {
                    WriteLogInfo($"ConnectTrafficLight that bai --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}");
                }

                if (client != null)
                {
                    client.Dispose();
                    Thread.Sleep(2000);
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo($"ConnectTrafficLight ERROR IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}: {ex.Message} -- {ex.StackTrace}");
            }
        }

        public void ReadDataFromTrafficLight()
        {
            try
            {
                var command = $"*[Output]Check[!]";

                WriteLogInfo($"1. Gửi lệnh: {command}");
                client.Send(command);
                client.Events.DataReceived += TrafficLight_DataReceived;
                Thread.Sleep(200);

                if (TrafficLightResponse == null || TrafficLightResponse.Length == 0)
                {
                    WriteLogInfo($"2. Không có phản hồi");
                    return;
                }
                WriteLogInfo($"2. Phản hồi: {TrafficLightResponse}");

                var result = GetStringValue(TrafficLightResponse.Replace("\0", ""));
                var green = result.Item1;
                var red = result.Item2;

                _notification.SendScale1TrafficLight(SCALE_DGT_CODE, red, green);
            }
            catch (Exception ex)
            {
                WriteLogInfo($"ReadDataFromTrafficLight ERROR IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}: {ex.Message} -- {ex.StackTrace}");
            }
        }

        public void TrafficLight_DataReceived(object sender, DataReceivedEventArgs e)
        {
            TrafficLightResponse = Encoding.UTF8.GetString(e.Data.ToArray());
        }

        static (string, string, string, string) GetStringValue(string input)
        {
            string pattern = $@"\[1\](?<gt1>ON|OFF)\[2\](?<gt2>ON|OFF)\[3\](?<gt3>ON|OFF)\[4\](?<gt4>ON|OFF)\[!\]";
            Match match = Regex.Match(input, pattern);

            if (match.Success)
            {
                return (
                    match.Groups["gt1"].Value,
                    match.Groups["gt2"].Value,
                    match.Groups["gt3"].Value,
                    match.Groups["gt4"].Value
                );
            }

            return (string.Empty, string.Empty, string.Empty, string.Empty);
        }

        public void WriteLogInfo(string message)
        {
            Console.WriteLine(message);
            _logger.Info(message);
        }

        public void Dispose()
        {
            try
            {
                if (client != null)
                {
                    client.Dispose();
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo($"TrafficLightJob: Dispose error - {ex.Message} - {ex.StackTrace} - {ex.InnerException}");
            }
        }
    }
}
