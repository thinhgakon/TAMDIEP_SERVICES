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
    public class ControlPlc12AndRealtime : IJob, IDisposable
    {
        private static bool DeviceConnected = false;

        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly MachineRepository _machineRepository;

        protected readonly TroughRepository _troughRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly SyncTroughLogger _logger;

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

        public ControlPlc12AndRealtime(
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

                client = new SimpleTcpClient(IP_ADDRESS, PORT_NUMBER);
                client.Keepalive.EnableTcpKeepAlives = true;
                client.Settings.MutuallyAuthenticate = false;
                client.Settings.AcceptInvalidCertificates = true;
                client.Settings.ConnectTimeoutMs = 2000;
                client.Settings.NoDelay = true;

                client.ConnectWithRetries(2000);

                //var data = encoding.GetBytes(START_CONNECTION_STR);
                //stream.Write(data, 0, data.Length);

                client.Send(START_CONNECTION_STR);

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

        public void ReadDataFromController()
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

                        Program.LastTimeReceivedScaleSocket = DateTime.Now;

                        _logger.LogInfo($"Nhan tin hieu can: {dataStr}");
                    }

                    if (Program.LastTimeReceivedScaleSocket != null)
                    {
                        timeDiffFromLastReceivedScaleSocket = DateTime.Now.Subtract((DateTime)Program.LastTimeReceivedScaleSocket);

                        if (timeDiffFromLastReceivedScaleSocket.TotalSeconds > 5)
                        {
                            _logger.LogInfo($"Quá 5s không nhận được tín hiệu cân => tiến hành reconnect: Now {DateTime.Now.ToString()} --- Last: {Program.LastTimeReceivedScaleSocket}");
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
                    }

                    break;
                }
                finally 
                {
                    TroughResponse = null;
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
            machines = machines.Where(x => x.Code == MACHINE_1_CODE || x.Code == MACHINE_2_CODE).ToList();

            foreach (var machine in machines)
            {
                try
                {
                    if (machine.StartStatus == "PENDING" && !string.IsNullOrEmpty(machine.CurrentDeliveryCode))
                    {
                        _logger.LogInfo($"Start machine code: {machine.Code} - msgh: {machine.CurrentDeliveryCode}============================================");

                        var command = (machine.StartCountingFrom == null || machine.StartCountingFrom == 0) ?
                                      $"*[Start][MDB][{machine.Code}]##{machine.CurrentDeliveryCode}[!]" :
                                      $"*[Start][MDB][{machine.Code}]##{machine.CurrentDeliveryCode}[N]{machine.StartCountingFrom}[!]";

                        _logger.LogInfo($"1. Gửi lệnh: {command}");
                        client.Send(command);
                        client.Events.DataReceived += Machine_DataReceived;
                        Thread.Sleep(200);

                        if (MachineResponse == null || MachineResponse.Length == 0)
                        {
                            _logger.LogInfo($"2. Không có phản hồi");
                            continue;
                        }
                        _logger.LogInfo($"2. Phản hồi: {MachineResponse}");

                        if (MachineResponse.Contains($"*[Start][MDB][{machine.Code}]#OK#"))
                        {
                            machine.StartStatus = "ON";
                            machine.StopStatus = "OFF";

                            await _machineRepository.UpdateMachine(machine);

                            _logger.LogInfo($"2.1. Start thành công");
                        }
                        else
                        {
                            _logger.LogInfo($"2.1. Start thất bại");
                            continue;
                        }
                    }

                    if (machine.StopStatus == "PENDING")
                    {
                        _logger.LogInfo($"Stop machine code: {machine.Code} ============================================");

                        var command = $"*[Stop][MDB][{machine.Code}][!]";

                        _logger.LogInfo($"1. Gửi lệnh: {command}");
                        client.Send(command);
                        client.Events.DataReceived += Machine_DataReceived;
                        Thread.Sleep(200);

                        if (MachineResponse == null || MachineResponse.Length == 0)
                        {
                            _logger.LogInfo($"2. Không có phản hồi");
                            continue;
                        }
                        _logger.LogInfo($"2. Phản hồi: {MachineResponse}");

                        if (MachineResponse.Contains($"*[Stop][MDB][{machine.Code}]#OK#"))
                        {
                            machine.StartStatus = "OFF";
                            machine.StopStatus = "ON";
                            machine.CurrentDeliveryCode = null;

                            await _machineRepository.UpdateMachine(machine);

                            _logger.LogInfo($"2.1. Stop thành công");
                        }
                        else
                        {
                            _logger.LogInfo($"2.1. Stop thất bại");
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
