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

namespace XHTD_SERVICES_SYNC_TROUGH.Jobs
{
    public class SyncTroughJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly TroughRepository _troughRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly SyncTroughLogger _syncTroughLogger;

        protected const string SERVICE_ACTIVE_CODE = "SYNC_TROUGH_ACTIVE";

        private static bool isActiveService = true;

        private const string IP_ADDRESS = "10.0.7.40";
        private const int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 1007;

        static ASCIIEncoding encoding = new ASCIIEncoding();

        public SyncTroughJob(
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
            _syncTroughLogger.LogInfo("Start process Sync Trough service");

            TcpClient client = new TcpClient();

            // 1. connect
            client.Connect(IP_ADDRESS, PORT_NUMBER);
            Stream stream = client.GetStream();

            _syncTroughLogger.LogInfo($"Connected to MANG XUAT {IP_ADDRESS}");

            var troughCodes = await _troughRepository.GetActiveXiBaoTroughs();

            if (troughCodes == null || troughCodes.Count == 0)
            {
                return;
            }

            foreach (var troughCode in troughCodes)
            {
                await ReadDataFromTrough(troughCode, stream);
            }

            // 5. Close
            stream.Close();
            client.Close();
        }

        public async Task ReadDataFromTrough(string troughCode, Stream stream)
        {
            var troughInfo = await _troughRepository.GetDetail(troughCode);

            if(troughInfo == null)
            {
                return;
            }

            _syncTroughLogger.LogInfo($"Read Trough: {troughCode}");
            // 2. send 1
            byte[] data1 = encoding.GetBytes($"SendTroughInfo_{troughCode}");
            stream.Write(data1, 0, data1.Length);

            // 3. receive 1
            data1 = new byte[BUFFER_SIZE];
            stream.Read(data1, 0, BUFFER_SIZE);

            var response = encoding.GetString(data1).Trim();
            var responseArr = response.Split(';');

            var status = responseArr[1];
            var deliveryCode = responseArr[5].Replace("'", "");
            var countQuantity = Double.Parse(responseArr[9]);
            var planQuantity = Double.Parse(responseArr[7]);

            if (status == "True")
            {
                _syncTroughLogger.LogInfo($"Mang {troughCode} dang xuat hang deliveryCode {deliveryCode}");

                await _troughRepository.UpdateTrough(troughCode, deliveryCode, countQuantity, planQuantity);

                await _callToTroughRepository.UpdateWhenIntoTrough(deliveryCode, troughInfo.Machine);

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
    }
}
