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

namespace XHTD_SERVICES_SYNC_TROUGH.Jobs
{
    [DisallowConcurrentExecution]
    public class SyncTroughJob34 : IJob, IDisposable
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly MachineRepository _machineRepository;

        protected readonly TroughRepository _troughRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly SyncTroughLogger _syncTroughLogger;

        protected const string SERVICE_ACTIVE_CODE = "SYNC_TROUGH_ACTIVE";

        private static bool isActiveService = true;

        private const string IP_ADDRESS = "192.168.13.210";
        private const int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 11000;
        private int TimeInterVal = 2000;

        static ASCIIEncoding encoding = new ASCIIEncoding();
        static TcpClient client = new TcpClient();
        static Stream stream = null;

        public SyncTroughJob34(
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
            _syncTroughLogger = syncTroughLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            //while (Program.Machine34Running == true)
            //{
            //}

            Program.SyncTrough34Running = true;
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                // Get System Parameters
                await LoadSystemParameters();

                if (!isActiveService)
                {
                    _syncTroughLogger.LogInfo("Service lay thong tin mang xuat SYNC TROUGH dang TAT.");
                    return;
                }

                await SyncTroughProcess();
            });
            Program.SyncTrough34Running = false;
        }

        public async Task LoadSystemParameters()
        {
            var parameters = await _systemParameterRepository.GetSystemParameters();

            var activeParameter = parameters.FirstOrDefault(x => x.Code == SERVICE_ACTIVE_CODE);

            if (activeParameter == null || activeParameter.Value == "0")
            {
                isActiveService = false;
            }
            else
            {
                isActiveService = true;
            }
        }

        public async Task SyncTroughProcess()
        {
            try
            {
                var troughCodes = await _troughRepository.GetActiveXiBaoTroughs();

                var listTroughInThisDevice = new List<string> { "5", "6", "7", "8" };

                troughCodes = troughCodes.Where(x => listTroughInThisDevice.Contains(x)).ToList();

                if (troughCodes == null || troughCodes.Count == 0)
                {
                    return;
                }

                client = new TcpClient();
                client.ConnectAsync(IP_ADDRESS, PORT_NUMBER).Wait(2000);

                if (client.Connected)
                {
                    _syncTroughLogger.LogInfo($"Trough Job Ket noi thanh cong MDB 3|4 --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}");

                    stream = client.GetStream();

                    await ReadDataFromTrough(troughCodes);
                }
                else
                {
                    _syncTroughLogger.LogInfo($"Trough Job Ket noi that bai MDB 3|4 --- IP: {IP_ADDRESS} --- PORT: {PORT_NUMBER}");
                }

                if (client != null)
                {
                    client.Close();
                }

                if (stream != null)
                {
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                _syncTroughLogger.LogInfo("Ket noi that bai.");
                _syncTroughLogger.LogInfo(ex.Message);
                _syncTroughLogger.LogInfo(ex.StackTrace);
            }
        }

        public async Task ReadDataFromTrough(List<string> troughCodes)
        {
            foreach (var troughCode in troughCodes)
            {
                try
                {
                    var troughInfo = await _troughRepository.GetDetail(troughCode);

                    if (troughInfo == null)
                    {
                        continue;
                    }

                    // Dữ liệu sensor cuối máng

                    // 2. send 1
                    byte[] data = encoding.GetBytes($"*[Count][MX][{troughCode}]#GET[!]");
                    stream.Write(data, 0, data.Length);

                    //*[Count][MX][1]#0#123456#Run[!]
                    // 3. receive 1
                    data = new byte[BUFFER_SIZE];
                    stream.Read(data, 0, BUFFER_SIZE);

                    var response = encoding.GetString(data).Trim();

                    if (response == null || response.Length == 0)
                    {
                        _syncTroughLogger.LogInfo($"Khong co du lieu tra ve");
                        continue;
                    }

                    var result = GetInfo(response.Replace("\0", "").Replace("##", "#"), "MX");

                    var status = result.Item4 == "Run" ? "True" : "False";
                    var deliveryCode = result.Item3;
                    var countQuantity = (Double.TryParse(result.Item2, out double i) ? i : 0);
                    if (countQuantity == 0) continue;
                    var planQuantity = 100;

                    var troughCodeReturn = result.Item1;

                    if (status == "True")
                    {
                        _syncTroughLogger.LogInfo($"Mang {troughCodeReturn} dang xuat hang deliveryCode {deliveryCode}");

                        var machineCode = (troughCode == "5" || troughCode == "6") ? "3" : "4";

                        // Dữ liệu sensor đầu máng

                        byte[] machineData = encoding.GetBytes($"*[Count][MDB][{machineCode}]#GET[!]");
                        stream.Write(machineData, 0, machineData.Length);

                        machineData = new byte[BUFFER_SIZE];
                        stream.Read(machineData, 0, BUFFER_SIZE);

                        var machineResponse = encoding.GetString(machineData).Trim();
                        if (machineResponse == null || machineResponse.Length == 0)
                        {
                            _syncTroughLogger.LogInfo($"Khong co du lieu dau mang tra ve - May {machineCode}");
                            continue;
                        }
                        var machineResult = GetInfo(machineResponse.Replace("\0", "").Replace("##", "#"), "MDB");
                        var firstSensorQuantity = (Double.TryParse(machineResult.Item2, out double j) ? j : 0);

                        await _troughRepository.UpdateTrough(troughCodeReturn, deliveryCode, countQuantity, planQuantity, firstSensorQuantity);

                        //await _callToTroughRepository.UpdateWhenIntoTrough(deliveryCode, troughInfo.Machine);

                        //await _storeOrderOperatingRepository.UpdateTroughLine(deliveryCode, troughCodeReturn);

                        //var isAlmostDone = (countQuantity / planQuantity) > 0.98;

                        //if (isAlmostDone)
                        //{
                        //    await _storeOrderOperatingRepository.UpdateStepInTrough(deliveryCode, (int)OrderStep.DA_LAY_HANG);
                        //}
                        //else
                        //{
                        //    await _storeOrderOperatingRepository.UpdateStepInTrough(deliveryCode, (int)OrderStep.DANG_LAY_HANG);
                        //}
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

                        _syncTroughLogger.LogInfo($"Mang {troughCodeReturn} dang nghi");

                        //_syncTroughLogger.LogInfo($"Cap nhat trang thai DA LAY HANG deliveryCode {deliveryCode}");
                        //await _storeOrderOperatingRepository.UpdateStepInTrough(deliveryCode, (int)OrderStep.DA_LAY_HANG);

                        _syncTroughLogger.LogInfo($"Reset trough troughCode {troughCodeReturn}");
                        //await _troughRepository.ResetTrough(troughCode);
                        await _troughRepository.UpdateTrough(troughCodeReturn, null, 0, 0, 0);

                    }
                }
                catch (Exception ex)
                {
                    _syncTroughLogger.LogInfo($"Khong the xu ly {troughCode}");
                    _syncTroughLogger.LogError($"{ex.Message}");
                    _syncTroughLogger.LogError($"{ex.StackTrace}");
                }
            }
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
