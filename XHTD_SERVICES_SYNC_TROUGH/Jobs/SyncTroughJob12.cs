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
    public class SyncTroughJob12 : IJob, IDisposable
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly MachineRepository _machineRepository;

        protected readonly TroughRepository _troughRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly SyncTroughLogger _logger;

        private const string IP_ADDRESS = "192.168.13.189";
        private const int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 11000;
        private int TimeInterVal = 2000;
        private List<string> machineCodes = new List<string>() { "1", "2" };
        private List<string> listTroughInThisDevice = new List<string> { "1", "2", "3", "4" };
        static ASCIIEncoding encoding = new ASCIIEncoding();
        static SimpleTcpClient client;
        static string MachineResponse = string.Empty;
        static string TroughResponse = string.Empty;

        public SyncTroughJob12(
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

            await Task.Run(async () =>
            {
                await SyncTroughProcess();
            });
        }

        public async Task SyncTroughProcess()
        {
            try
            {
                var troughCodes = await _troughRepository.GetActiveXiBaoTroughs();
                troughCodes = troughCodes.Where(x => listTroughInThisDevice.Contains(x)).ToList();
                if (troughCodes == null || troughCodes.Count == 0)
                {
                    _logger.LogInfo($"Trough Job MDB 1|2: Khong tim thay mang xuat --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}");

                    return;
                }

                client = new SimpleTcpClient(IP_ADDRESS, PORT_NUMBER);
                client.Keepalive.EnableTcpKeepAlives = true;
                client.Settings.MutuallyAuthenticate = false;
                client.Settings.AcceptInvalidCertificates = true;
                client.Settings.ConnectTimeoutMs = 2000;
                client.Settings.NoDelay = true;

                client.ConnectWithRetries(2000);

                if (client.IsConnected)
                {
                    _logger.LogInfo($"Trough Job MDB 1|2: Ket noi thanh cong --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}");

                    await ReadDataFromMachine(machineCodes);
                    await ReadDataFromTrough(troughCodes);
                }
                else
                {
                    _logger.LogInfo($"Trough Job MDB 1|2: Ket noi that bai --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"Trough Job MDB 1|2: ERROR --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}: {ex.Message} -- {ex.StackTrace}");
            }
            finally 
            {
                if (client != null)
                {
                    client.Disconnect();
                }
            }
        }

        public async Task ReadDataFromMachine(List<string> machineCodes)
        {
            foreach (var machineCode in machineCodes)
            {
                try
                {
                    _logger.LogInfo($"Đếm đầu máng: {machineCode} ============================================================");

                    var command = $"*[Count][MDB][{machineCode}]#GET[!]";

                    _logger.LogInfo($"1. Gửi lệnh: {command}");
                    client.Send(command);
                    client.Events.DataReceived += Machine_DataReceived;
                    Thread.Sleep(200);

                    if (MachineResponse == null || MachineResponse.Length == 0)
                    {
                        _logger.LogInfo($"2. Không có phản hồi");
                        continue;
                    }
                    else
                    {
                        _logger.LogInfo($"2. Phản hồi: {MachineResponse}");
                    }

                    var machineResult = GetInfo(MachineResponse.Replace("\0", "").Replace("##", "#"), "MDB");

                    var status = machineResult.Item4 == "Run" ? "True" : "False";
                    var firstSensorQuantity = (Double.TryParse(machineResult.Item2, out double j) ? j : 0);
                    var deliveryCode = machineResult.Item3;

                    if (firstSensorQuantity == 0)
                    {
                        continue;
                    }

                    if (status == "False")
                    {
                        continue;
                    }

                    _logger.LogInfo($"3. Cập nhật dữ liệu đầu máng: msgh={deliveryCode} -- firstSensor={firstSensorQuantity}");
                    await _troughRepository.UpdateMachineSensor(deliveryCode, firstSensorQuantity);
                }
                catch (Exception ex)
                {
                    _logger.LogInfo($"ReadDataFromMachine ERROR: Machine {machineCode} -- {ex.Message} --- {ex.StackTrace}");
                }
            }
        }

        public async Task ReadDataFromTrough(List<string> troughCodes)
        {
            foreach (var troughCode in troughCodes)
            {
                try
                {
                    _logger.LogInfo($"Đếm cuối máng: {troughCode} ============================================================");

                    var troughInfo = await _troughRepository.GetDetail(troughCode);
                    if (troughInfo == null)
                    {
                        _logger.LogInfo($"Mang khong ton tai: {troughCode} => Thoat");
                        continue;
                    }

                    var command = $"*[Count][MX][{troughCode}]#GET[!]";

                    _logger.LogInfo($"1. Gửi lệnh: {command}");
                    client.Send(command);

                    client.Send(command);
                    client.Events.DataReceived += Trough_DataReceived;
                    Thread.Sleep(200);

                    if (TroughResponse == null || TroughResponse.Length == 0)
                    {
                        _logger.LogInfo($"2. Không có phản hồi");
                        continue;
                    }
                    else
                    {
                        _logger.LogInfo($"2. Phản hồi: {TroughResponse}");
                    }

                    var result = GetInfo(TroughResponse.Replace("\0", "").Replace("##", "#"), "MX");

                    var status = result.Item4 == "Run" ? "True" : "False";
                    var deliveryCode = result.Item3;
                    var countQuantity = (Double.TryParse(result.Item2, out double i) ? i : 0);
                    if (countQuantity == 0)
                    {
                        continue;
                    }

                    var planQuantity = 100;
                    var troughCodeReturn = result.Item1;
                    if (status == "True")
                    {
                        _logger.LogInfo($"Mang {troughCodeReturn} dang xuat hang deliveryCode {deliveryCode}");

                        _logger.LogInfo($"3. Cập nhật dữ liệu cuối máng: msgh={deliveryCode} -- trough: {troughCodeReturn} -- countQuantity={countQuantity}");
                        await _troughRepository.UpdateTroughSensor(troughCodeReturn, deliveryCode, countQuantity, planQuantity);

                        var trough = await _troughRepository.GetDetail(troughCode);
                        var machine = await _machineRepository.GetMachineByMachineCode(trough.Machine);

                        if (machine.StartStatus == "ON" && machine.StopStatus == "OFF")
                        {
                            await _storeOrderOperatingRepository.UpdateStepInTrough(deliveryCode, (int)OrderStep.DANG_LAY_HANG);
                        }
                    }
                    else
                    {
                        //TODO: xét thêm trường hợp đang xuất dở đơn mà chuyển qua máng khác thì không update được lại trạng thái Đang lấy hàng
                        _logger.LogInfo($"Mang {troughCodeReturn} dang nghi");

                        _logger.LogInfo($"Reset trough troughCode {troughCodeReturn}");
                        //await _troughRepository.ResetTrough(troughCode);
                        await _troughRepository.UpdateTrough(troughCodeReturn, null, 0, 0, 0);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInfo($"ReadDataFromTrough ERROR: Trough {troughCode} -- {ex.Message} --- {ex.StackTrace}");
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
