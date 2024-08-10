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
    public class SyncTroughJob34 : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly TroughRepository _troughRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly SyncTroughLogger _syncTroughLogger;

        protected const string SERVICE_ACTIVE_CODE = "SYNC_TROUGH_ACTIVE";

        private static bool isActiveService = true;

        private const string IP_ADDRESS = "192.168.13.210";
        private const int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 10000;
        private int TimeInterVal = 2000;

        static ASCIIEncoding encoding = new ASCIIEncoding();
        static TcpClient client = new TcpClient();
        static Stream stream = null;

        public SyncTroughJob34(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            TroughRepository troughRepository,
            CallToTroughRepository callToTroughRepository,
            SystemParameterRepository systemParameterRepository,
            SyncTroughLogger syncTroughLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _troughRepository = troughRepository;
            _callToTroughRepository = callToTroughRepository;
            _systemParameterRepository = systemParameterRepository;
            _syncTroughLogger = syncTroughLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
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
            _syncTroughLogger.LogInfo("Thuc hien ket noi.");
            try
            {
                _syncTroughLogger.LogInfo("Bat dau ket noi.");
                client = new TcpClient();
                client.ConnectAsync(IP_ADDRESS, PORT_NUMBER).Wait(2000);
                stream = client.GetStream();
                _syncTroughLogger.LogInfo($"Connected to count machine : 3|4");
                await ReadDataFromTrough();
                client.Close();
                stream.Close();
            }
            catch (Exception ex)
            {
                _syncTroughLogger.LogInfo("Ket noi that bai.");
                _syncTroughLogger.LogInfo(ex.Message);
                _syncTroughLogger.LogInfo(ex.StackTrace);
                Thread.Sleep(2000);
                await SyncTroughProcess();
            }
        }

        public async Task ReadDataFromTrough()
        {
            while (stream.CanRead && stream.CanWrite)
            {
                var troughCodes = await _troughRepository.GetActiveXiBaoTroughs();

                var listTroughInThisDevice = new List<string> { "1", "2", "3", "4" };

                troughCodes = troughCodes.Where(x => listTroughInThisDevice.Contains(x)).ToList();

                if (troughCodes == null || troughCodes.Count == 0)
                {
                    Thread.Sleep(TimeInterVal);
                    continue;
                }

                foreach (var troughCode in troughCodes)
                {
                    try
                    {
                        var troughInfo = await _troughRepository.GetDetail(troughCode);

                        if (troughInfo == null)
                        {
                            Thread.Sleep(TimeInterVal);
                            continue;
                        }

                        _syncTroughLogger.LogInfo($"Read Trough: {troughCode}");
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
                            Thread.Sleep(TimeInterVal);
                            continue;
                        }

                        var result = GetInfo(response);

                        var status = result.Item4 == "Run" ? "True" : "False";
                        var deliveryCode = result.Item3;
                        var countQuantity = Double.Parse(result.Item2);
                        var planQuantity = 100;

                        if (status == "True")
                        {
                            _syncTroughLogger.LogInfo($"Mang {troughCode} dang xuat hang deliveryCode {deliveryCode}");

                            await _troughRepository.UpdateTrough(troughCode, deliveryCode, countQuantity, planQuantity);

                            //await _callToTroughRepository.UpdateWhenIntoTrough(deliveryCode, troughInfo.Machine);

                            await _storeOrderOperatingRepository.UpdateTroughLine(deliveryCode, troughCode);

                            var isAlmostDone = (countQuantity / planQuantity) > 0.98;

                            if (isAlmostDone)
                            {
                                await _storeOrderOperatingRepository.UpdateStepInTrough(deliveryCode, (int)OrderStep.DA_LAY_HANG);
                            }
                            else
                            {
                                await _storeOrderOperatingRepository.UpdateStepInTrough(deliveryCode, (int)OrderStep.DANG_LAY_HANG);
                            }
                            //await _storeOrderOperatingRepository.UpdateStepInTrough(deliveryCode, (int)OrderStep.DANG_LAY_HANG);
                        }
                        else
                        {
                            //TODO: xét thêm trường hợp đang xuất dở đơn mà chuyển qua máng khác thì không update được lại trạng thái Đang lấy hàng

                            _syncTroughLogger.LogInfo($"Mang {troughCode} dang nghi");

                            _syncTroughLogger.LogInfo($"Cap nhat trang thai DA LAY HANG deliveryCode {deliveryCode}");
                            await _storeOrderOperatingRepository.UpdateStepInTrough(deliveryCode, (int)OrderStep.DA_LAY_HANG);

                            _syncTroughLogger.LogInfo($"Reset trough troughCode {troughCode}");
                            await _troughRepository.ResetTrough(troughCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _syncTroughLogger.LogInfo($"Khong the xu ly {troughCode}");
                        _syncTroughLogger.LogError($"{ex.Message}");
                        _syncTroughLogger.LogError($"{ex.StackTrace}");
                    }
                    Thread.Sleep(TimeInterVal);
                }
            }
            await SyncTroughProcess();
        }

        static (string, string, string, string) GetInfo(string input)
        {
            string pattern = @"\[[^\]]+\]\[[^\]]+\]\[(?<info1>[^\]]+)\]#(?<info2>[^#]+)#(?<info3>[^#]+)#(?<info4>[^#]+)\[!\]";
            Match match = Regex.Match(input, pattern);

            if (match.Success)
            {
                return (
                    match.Groups["info1"].Value,
                    match.Groups["info2"].Value,
                    match.Groups["info3"].Value,
                    match.Groups["info4"].Value
                );
            }

            throw new FormatException("Chuỗi không đúng định dạng.");
        }
    }
}
