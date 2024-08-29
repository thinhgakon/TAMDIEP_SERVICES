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
using SuperSimpleTcp;

namespace XHTD_SERVICES_SYNC_TROUGH.Jobs
{
    [DisallowConcurrentExecution]
    public class SyncTroughJobNew12 : IJob, IDisposable
    {
        private static bool DeviceConnected = false;
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly MachineRepository _machineRepository;

        protected readonly TroughRepository _troughRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly SyncTroughLogger _logger;

        Thread controlPLCThread;

        protected const string SERVICE_ACTIVE_CODE = "SYNC_TROUGH_ACTIVE";

        private static bool isActiveService = true;

        private int TimeInterVal = 2000;

        private const int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 10000;
        static ASCIIEncoding encoding = new ASCIIEncoding();
        static SimpleTcpClient client;
        static string MachineResponse = string.Empty;
        static string TroughResponse = string.Empty;
        static Stream stream = null;
        private readonly Notification _notification;
        private readonly string START_CONNECTION_STR = "hello*mbf*abc123";
        private readonly string SEND_TO_RECEIVED_SCALE_CODE = "ww";

        public const string IP_ADDRESS = "192.168.13.210";

        TimeSpan timeDiffFromLastReceivedScaleSocket = new TimeSpan();

        public SyncTroughJobNew12(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            MachineRepository machineRepository,
            TroughRepository troughRepository,
            CallToTroughRepository callToTroughRepository,
            SystemParameterRepository systemParameterRepository,
            SyncTroughLogger syncTroughLogger
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

                    controlPLCThread = new Thread(() =>
                    {
                        ProcessPendingStatusPlc();
                    });
                    controlPLCThread.IsBackground = true;
                    controlPLCThread.Start();

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
                //client = new TcpClient();

                //// 1. connect
                //client.ConnectAsync(IP_ADDRESS, PORT_NUMBER).Wait(2000);
                //stream = client.GetStream();

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

                    var dataStr = encoding.GetString(data);

                    _logger.LogInfo($"Nhan tin hieu can: {dataStr}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($@"Co loi xay ra khi xu ly du lieu can {ex.StackTrace} {ex.Message} ");
                    if (stream != null) stream.Close();
                    if (client != null) client.Disconnect();

                    break;
                }

                Thread.Sleep(500);
            }

            AuthenticateScaleStationModuleFromController();
        }

        public async Task ProcessPendingStatusPlc()
        {
            while (true)
            {
                Console.WriteLine("process pending plc");

                var machines = await _machineRepository.GetPendingMachine();

                if (machines == null || machines.Count == 0)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                await MachineJobProcess(machines);

                Thread.Sleep(1000);
            }
        }

        private async Task MachineJobProcess(List<tblMachine> machines)
        {
            machines = machines.Where(x => x.Code == "3" || x.Code == "4").ToList();

            foreach (var machine in machines)
            {
                try
                {
                    if (machine.StartStatus == "PENDING" && !string.IsNullOrEmpty(machine.CurrentDeliveryCode))
                    {
                        _logger.LogInfo($"Start machine: {machine.Code}");

                        var command = (machine.StartCountingFrom == null || machine.StartCountingFrom == 0) ?
                                      $"*[Start][MDB][{machine.Code}]##{machine.CurrentDeliveryCode}[!]" :
                                      $"*[Start][MDB][{machine.Code}]##{machine.CurrentDeliveryCode}[N]{machine.StartCountingFrom}[!]";

                        // 2. send 1
                        //byte[] data = encoding.GetBytes(command);
                        //stream.Write(data, 0, data.Length);

                        //// 3. receive 1
                        //data = new byte[BUFFER_SIZE];
                        //stream.Read(data, 0, BUFFER_SIZE);

                        //var response = encoding.GetString(data).Trim();

                        client.Send(command);
                        client.Events.DataReceived += Machine_DataReceived;
                        Thread.Sleep(200);

                        if (MachineResponse == null || MachineResponse.Length == 0)
                        {
                            _logger.LogInfo($"Khong co du lieu tra ve");
                            continue;
                        }
                        _logger.LogInfo($"Du lieu tra ve: {MachineResponse}");

                        if (MachineResponse.Contains($"*[Start][MDB][{machine.Code}]#OK#"))
                        {
                            machine.StartStatus = "ON";
                            machine.StopStatus = "OFF";

                            await _machineRepository.UpdateMachine(machine);
                            _logger.LogInfo($"Start machine {machine.Code} thanh cong!");
                        }
                        else
                        {
                            _logger.LogInfo($"Tin hieu phan hoi khong thanh cong");
                            continue;
                        }
                    }

                    if (machine.StopStatus == "PENDING")
                    {
                        _logger.LogInfo($"Stop machine: {machine.Code}");

                        byte[] data = encoding.GetBytes($"*[Stop][MDB][{machine.Code}][!]");
                        stream.Write(data, 0, data.Length);

                        data = new byte[BUFFER_SIZE];
                        stream.Read(data, 0, BUFFER_SIZE);

                        var response = encoding.GetString(data).Trim();

                        if (response == null || response.Length == 0)
                        {
                            _logger.LogInfo($"Khong co du lieu tra ve");
                            continue;
                        }
                        _logger.LogInfo($"Du lieu tra ve: {response}");

                        if (response.Contains($"*[Stop][MDB][{machine.Code}]#OK#"))
                        {
                            machine.StartStatus = "OFF";
                            machine.StopStatus = "ON";
                            machine.CurrentDeliveryCode = null;

                            await _machineRepository.UpdateMachine(machine);
                            _logger.LogInfo($"Stop machine {machine.Code} thanh cong!");
                        }
                        else
                        {
                            _logger.LogInfo($"Tin hieu phan hoi khong thanh cong");
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInfo($"MachineJobProcess ERROR: Code={machine.Code} --- {ex.Message} --- {ex.StackTrace}");
                }
            }
        }

        private void Machine_DataReceived(object sender, DataReceivedEventArgs e)
        {
            MachineResponse = Encoding.UTF8.GetString(e.Data.ToArray());
        }

        public void Dispose()
        {
            try
            {
                if (client != null)
                {
                    client.Dispose();
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SyncTroughJob12: Dispose error - {ex.Message} - {ex.StackTrace} - {ex.InnerException}");
            }
        }
    }
}
