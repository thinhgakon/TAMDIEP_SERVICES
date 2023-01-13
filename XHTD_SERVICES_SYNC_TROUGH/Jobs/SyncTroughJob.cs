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

namespace XHTD_SERVICES_SYNC_TROUGH.Jobs
{
    public class SyncTroughJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly VehicleRepository _vehicleRepository;

        protected readonly TroughRepository _troughRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly Notification _notification;

        protected readonly SyncTroughLogger _autoReindexLogger;

        protected const string SYNC_ORDER_ACTIVE = "SYNC_ORDER_ACTIVE";

        protected const string SYNC_ORDER_HOURS = "SYNC_ORDER_HOURS";

        private static bool isActiveService = true;

        private static int numberHoursSearchOrder = 48;

        private const int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 1007;

        static ASCIIEncoding encoding = new ASCIIEncoding();

        public SyncTroughJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            VehicleRepository vehicleRepository,
            TroughRepository troughRepository,
            SystemParameterRepository systemParameterRepository,
            Notification notification,
            SyncTroughLogger autoReindexLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _vehicleRepository = vehicleRepository;
            _troughRepository = troughRepository;
            _systemParameterRepository = systemParameterRepository;
            _notification = notification;
            _autoReindexLogger = autoReindexLogger;
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
                    _autoReindexLogger.LogInfo("Service dong bo don hang dang TAT.");
                    return;
                }

                await AutoReindexProcess();
            });
        }

        public async Task LoadSystemParameters()
        {
            var parameters = await _systemParameterRepository.GetSystemParameters();

            var activeParameter = parameters.FirstOrDefault(x => x.Code == SYNC_ORDER_ACTIVE);
            var numberHoursParameter = parameters.FirstOrDefault(x => x.Code == SYNC_ORDER_HOURS);

            if (activeParameter == null || activeParameter.Value == "0")
            {
                isActiveService = false;
            }

            if (numberHoursParameter != null)
            {
                numberHoursSearchOrder = Convert.ToInt32(numberHoursParameter.Value);
            }
        }

        public async Task AutoReindexProcess()
        {
            _autoReindexLogger.LogInfo("Start process SyncTroughProcess");
            
            TcpClient client = new TcpClient();

            // 1. connect
            client.Connect("10.0.7.40", PORT_NUMBER);
            Stream stream = client.GetStream();

            _autoReindexLogger.LogInfo("Connected to MANG XUAT.");

            var troughCodes = await _troughRepository.GetAllTroughCodes();

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
            _autoReindexLogger.LogInfo($"ReadDataFromTrough: {troughCode}");
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
            var countQuantity = Double.Parse(responseArr[6]);
            var planQuantity = Double.Parse(responseArr[8]);

            if (status == "True")
            {
                await _troughRepository.UpdateTrough(troughCode, deliveryCode, countQuantity, planQuantity);
            }
            else
            {
                await _troughRepository.ResetTrough(troughCode);
            }
        }
    }
}
