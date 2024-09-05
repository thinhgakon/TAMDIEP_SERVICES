using Autofac;
using Quartz;
using SuperSimpleTcp;
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
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_LED.Devices;
using XHTD_SERVICES_LED.Models.Values;

namespace XHTD_SERVICES_LED.Jobs
{
    [DisallowConcurrentExecution]
    public class Led12RealtimeJob : IJob, IDisposable
    {
        private static bool DeviceConnected = false;

        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly MachineRepository _machineRepository;

        protected readonly TroughRepository _troughRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly LedLogger _logger;

        Thread controlPLCThread;

        static SimpleTcpClient client;
        static ASCIIEncoding encoding = new ASCIIEncoding();
        static string MachineResponse = string.Empty;
        static string TroughResponse = string.Empty;

        public const string IP_ADDRESS = "192.168.13.189";
        private const int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 10000;

        private readonly string START_CONNECTION_STR = "hello*mbf*abc123";

        private const string MACHINE_1_CODE = "1";
        private const string MACHINE_2_CODE = "2";

        TimeSpan timeDiffFromLastReceivedScaleSocket = new TimeSpan();

        private const int TIME_TO_RESET = 10;

        public Led12RealtimeJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            MachineRepository machineRepository,
            TroughRepository troughRepository,
            CallToTroughRepository callToTroughRepository,
            SystemParameterRepository systemParameterRepository,
            LedLogger syncTroughLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _machineRepository = machineRepository;
            _troughRepository = troughRepository;
            _callToTroughRepository = callToTroughRepository;
            _systemParameterRepository = systemParameterRepository;
            _logger = syncTroughLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                await AuthenticateScaleStationModuleFromController();
            });
        }

        public async Task AuthenticateScaleStationModuleFromController()
        {
            while (true)
            {
                var isConnected = ConnectScaleStationModuleFromController();

                if (isConnected)
                {

                    await ReadDataFromPlc();
                }

                Thread.Sleep(1000);
            }
        }

        public bool ConnectScaleStationModuleFromController()
        {
            try
            {
                _logger.LogInfo("Thuc hien ket noi scale socket");

                client = new SimpleTcpClient(IP_ADDRESS, PORT_NUMBER);
                client.Keepalive.EnableTcpKeepAlives = true;
                client.Settings.MutuallyAuthenticate = false;
                client.Settings.AcceptInvalidCertificates = true;
                client.Settings.ConnectTimeoutMs = 2000;
                client.Settings.NoDelay = true;

                client.ConnectWithRetries(2000);

                if (client.IsConnected)
                {
                    _logger.LogInfo("Ket noi thanh cong");

                    DeviceConnected = true;
                }

                return DeviceConnected;
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"Ket noi that bai: {ex.Message}");
                return false;
            }
        }

        public async Task ReadDataFromPlc()
        {
            while (true)
            {
                try
                {
                    client.Events.DataReceived += Trough_DataReceived;
                    Thread.Sleep(200);

                    if (TroughResponse == null || TroughResponse.Length == 0)
                    {
                        _logger.LogInfo($"Khong co du lieu tra ve");
                        //continue;
                    }
                    else
                    {
                        var dataStr = TroughResponse;

                        // xử lý LED
                        var result = GetInfo(dataStr.Replace("\0", "").Replace("##", "#"), "MX");

                        var isRunning = result.Item4 == "Run";
                        var deliveryCode = result.Item3;
                        var countQuantity = Double.TryParse(result.Item2, out double i) ? i : 0;
                        var troughCode = result.Item1;

                        var vehicleCode = "BSX-12345";
                        var planQuantity = 100;
                        string typeProduct = "PCB30";

                        if (countQuantity == 0)
                        {
                            continue;
                        }

                        if (isRunning)
                        {
                            var machine = await _machineRepository.GetMachineByTroughCode(troughCode);

                            var order = await _storeOrderOperatingRepository.GetDetail(deliveryCode);
                            if (order != null)
                            {
                                vehicleCode = order.Vehicle;
                                planQuantity = (int)(order.SumNumber * 20);
                                typeProduct = !String.IsNullOrEmpty(order.TypeProduct) ? order.TypeProduct : "---";
                            }

                            var sendCode = $"*[H1][C1]{vehicleCode}[H2][C1][1]{deliveryCode}[2]{typeProduct}[H3][C1][1]DAT[2]{planQuantity}[H4][C1][1]XUAT[2]{countQuantity}[!]";

                            DisplayScreenLed(sendCode, machine.Code);
                        }

                        Program.LastTimeReceivedScaleSocket = DateTime.Now;

                        _logger.LogInfo($"Nhan tin hieu can: {dataStr}");
                    }

                    if (Program.LastTimeReceivedScaleSocket != null)
                    {
                        timeDiffFromLastReceivedScaleSocket = DateTime.Now.Subtract((DateTime)Program.LastTimeReceivedScaleSocket);

                        if (timeDiffFromLastReceivedScaleSocket.TotalSeconds > TIME_TO_RESET)
                        {
                            _logger.LogInfo($"Quá {TIME_TO_RESET}s không nhận được tín hiệu => reconnect: Now {DateTime.Now.ToString()} --- Last: {Program.LastTimeReceivedScaleSocket}");

                            Program.LastTimeReceivedScaleSocket = DateTime.Now;

                            if (client != null)
                            {
                                client.Disconnect();
                                client.Dispose();
                                client = null;
                            }

                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($@"Co loi xay ra khi xu ly du lieu can {ex.StackTrace} {ex.Message} ");

                    if (client != null)
                    {
                        client.Disconnect();
                        client.Dispose();
                        client = null;
                    }

                    break;
                }
                finally
                {
                    TroughResponse = null;
                }

                Thread.Sleep(500);
            }

            await AuthenticateScaleStationModuleFromController();
        }

        static (string, string, string, string) GetInfo(string input, string type)
        {
            string pattern = $@"\*\[Count\]\[{type}\]\[(?<gt1>[^\]]+)\]#(?<gt2>[^#]*)#(?<gt3>[^#]+)#(?<gt4>[^#]+)\[!\]";
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

        public void DisplayScreenLed(string dataCode, string ledCode)
        {
            _logger.LogInfo($"Send led: dataCode = {dataCode}");

            if (DIBootstrapper.Init().Resolve<TCPLedControl>().DisplayScreen(ledCode, dataCode))
            {
                _logger.LogInfo($"LED Máy {ledCode} - OK");
            }
            else
            {
                _logger.LogInfo($"LED Máy {ledCode} - FAILED");
            }
        }

        private void Machine_DataReceived(object sender, DataReceivedEventArgs e)
        {
            MachineResponse = Encoding.UTF8.GetString(e.Data.ToArray());
        }

        private void Trough_DataReceived(object sender, DataReceivedEventArgs e)
        {
            TroughResponse = Encoding.UTF8.GetString(e.Data.ToArray());
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
                _logger.LogInfo($"SyncTroughJob12: Dispose error - {ex.Message} - {ex.StackTrace} - {ex.InnerException}");
            }
        }
    }
}
