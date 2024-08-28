using Autofac;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_LED.Devices;
using XHTD_SERVICES_LED.Models.Values;

namespace XHTD_SERVICES_LED.Jobs
{
    [DisallowConcurrentExecution]
    public class Led1XiBaoNewJob : IJob, IDisposable
    {
        private static bool DeviceConnected = false;

        protected readonly LedLogger _logger;

        static ASCIIEncoding encoding = new ASCIIEncoding();
        static TcpClient client = new TcpClient();
        static Stream stream = null;

        public const string PLC_IP_ADDRESS = "192.168.13.210";
        private const int PLC_PORT_NUMBER = 10000;
        private const int BUFFER_SIZE = 1024;

        protected readonly string MACHINE_CODE = MachineCode.MACHINE_XI_BAO_1;

        TimeSpan timeDiffFromLastReceivedScaleSocket = new TimeSpan();

        public Led1XiBaoNewJob(
            LedLogger syncTroughLogger
            )
        {
            _logger = syncTroughLogger;
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
                client.ConnectAsync(PLC_IP_ADDRESS, PLC_PORT_NUMBER).Wait(2000);
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
                    //byte[] data = encoding.GetBytes(SEND_TO_RECEIVED_SCALE_CODE);
                    //stream.Write(data, 0, data.Length);

                    // receive
                    byte[] data = new byte[BUFFER_SIZE];
                    stream.Read(data, 0, BUFFER_SIZE);
                    //stream.ReadAsync(data, 0, BUFFER_SIZE).Wait(1000);

                    var response = encoding.GetString(data);

                    _logger.LogInfo($"Nhan tin hieu can: {response}");

                    if (response == null || response.Length == 0)
                    {
                        _logger.LogInfo($"Khong co du lieu tra ve");
                        return;
                    }

                    var result = GetInfo(response.Replace("\0", "").Replace("##", "#"));

                    var isRunning = result.Item4 == "Run";
                    var deliveryCode = result.Item3;
                    var countQuantity = Double.TryParse(result.Item2, out double i) ? i : 0;

                    var troughCode = result.Item1;

                    _logger.LogInfo($"Trough: {troughCode} -- Count: {countQuantity} -- DeliveryCode: {deliveryCode} -- Runing: {isRunning}");
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

        static (string, string, string, string) GetInfo(string input)
        {
            string pattern = @"\*\[Count\]\[MX\]\[(?<gt1>[^\]]+)\]#(?<gt2>[^#]*)#(?<gt3>[^#]+)#(?<gt4>[^#]+)\[!\]";
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

        public void DisplayScreenLed(string dataCode)
        {
            _logger.LogInfo($"Send led: dataCode = {dataCode}");

            if (DIBootstrapper.Init().Resolve<TCPLedControl>().DisplayScreen(MACHINE_CODE, dataCode))
            {
                _logger.LogInfo($"LED Máy {MACHINE_CODE} - OK");
            }
            else
            {
                _logger.LogInfo($"LED Máy {MACHINE_CODE} - FAILED");
            }
        }

        public void Dispose()
        {
            try
            {
                if (client != null && client?.Connected == true)
                {
                    client.Close();
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
