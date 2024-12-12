using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;
using RestSharp;
using XHTD_SERVICES_SYNC_ORDER.Models.Response;
using XHTD_SERVICES.Data.Models.Response;
using Newtonsoft.Json;
using XHTD_SERVICES_SYNC_ORDER.Models.Values;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES.Helper.Models.Request;
using System.Threading;
using XHTD_SERVICES.Data.Entities;

namespace XHTD_SERVICES_SYNC_ORDER.Jobs
{
    public class SyncOrderJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly VehicleRepository _vehicleRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly Notification _notification;

        protected readonly SyncOrderLogger _syncOrderLogger;

        private static string strToken;

        protected const string SERVICE_ACTIVE_CODE = "SYNC_ORDER_ACTIVE";

        protected const string SYNC_ORDER_HOURS = "SYNC_ORDER_HOURS";

        private static bool isActiveService = true;

        private static int numberHoursSearchOrder = 48;

        public SyncOrderJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            VehicleRepository vehicleRepository,
            CallToTroughRepository callToTroughRepository,
            SystemParameterRepository systemParameterRepository,
            Notification notification,
            SyncOrderLogger syncOrderLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _vehicleRepository = vehicleRepository;
            _callToTroughRepository = callToTroughRepository;
            _systemParameterRepository = systemParameterRepository;
            _notification = notification;
            _syncOrderLogger = syncOrderLogger;
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
                    _syncOrderLogger.LogInfo("Service dong bo don hang dang TAT");
                    return;
                }

                await SyncOrderProcess();
            });
        }

        public async Task LoadSystemParameters()
        {
            var parameters = await _systemParameterRepository.GetSystemParameters();

            var activeParameter = parameters.FirstOrDefault(x => x.Code == SERVICE_ACTIVE_CODE);
            var numberHoursParameter = parameters.FirstOrDefault(x => x.Code == SYNC_ORDER_HOURS);

            if(activeParameter == null || activeParameter.Value == "0")
            {
                isActiveService = false;
            }
            else
            {
                isActiveService = true;
            }

            if (numberHoursParameter != null)
            {
                numberHoursSearchOrder = Convert.ToInt32(numberHoursParameter.Value);
            }
        }

        public async Task SyncOrderProcess()
        {
            _syncOrderLogger.LogInfo("Start process Sync Order service");

            GetToken();

            List<OrderItemResponse> websaleOrders = GetWebsaleOrder();

            if (websaleOrders == null || websaleOrders.Count == 0)
            {
                return;
            }

            bool isChanged = false;

            foreach (var websaleOrder in websaleOrders)
            {
                // Không đồng bộ các đơn tại sông Thao
                if (websaleOrder.shippointId != "13") { 
                    bool isSynced = await SyncWebsaleOrderToDMS(websaleOrder);

                    if (!isChanged) isChanged = isSynced;
                }
            }
        }

        public void GetToken()
        {
            try
            {
                IRestResponse response = HttpRequest.GetWebsaleToken();

                var content = response.Content;

                var responseData = JsonConvert.DeserializeObject<GetTokenResponse>(content);
                strToken = responseData.access_token;
            }
            catch (Exception ex)
            {
                _syncOrderLogger.LogInfo("getToken error: " + ex.Message);
            }
        }

        public List<OrderItemResponse> GetWebsaleOrder()
        {
            IRestResponse response = HttpRequest.GetWebsaleOrder(strToken, numberHoursSearchOrder);
            var content = response.Content;

            if (response.StatusDescription.Equals("Unauthorized"))
            {
                _syncOrderLogger.LogInfo("Unauthorized GetWebsaleOrder");

                return null;
            }

            var responseData = JsonConvert.DeserializeObject<SearchOrderResponse>(content);

            return responseData.collection.OrderBy(x => x.id).ToList();
        }

        public async Task<bool> SyncWebsaleOrderToDMS(OrderItemResponse websaleOrder)
        {
            bool isSynced = false;

            var stateId = 0;
            switch (websaleOrder.status.ToUpper())
            {
                case "BOOKED":
                    stateId = (int)OrderState.DA_DAT_HANG;
                    break;
                case "VOIDED":
                    stateId = (int)OrderState.DA_HUY_DON;
                    break;
                case "RECEIVING":
                    stateId = (int)OrderState.DANG_LAY_HANG;
                    break;
                case "RECEIVED":
                    stateId = (int)OrderState.DA_XUAT_HANG;
                    break;
            }

            //if (stateId == (int)OrderState.DA_DAT_HANG)
            //{
            //    isSynced = await _storeOrderOperatingRepository.CreateAsync(websaleOrder);

            //    if (isSynced)
            //    {
            //        var vehicleCode = websaleOrder.vehicleCode.Replace("-", "").Replace("  ", "").Replace(" ", "").Replace("/", "").Replace(".", "").ToUpper();
            //        await _vehicleRepository.CreateAsync(vehicleCode);
            //    }
            //}
            //else 
            if (stateId == (int)OrderState.DANG_LAY_HANG)
            {
                if (!_storeOrderOperatingRepository.CheckExist(websaleOrder.id))
                {
                    isSynced = await _storeOrderOperatingRepository.CreateAsync(websaleOrder);
                }
                else 
                { 
                    isSynced = await _storeOrderOperatingRepository.UpdateReceivingOrder(websaleOrder);
                }
            }
            else if (stateId == (int)OrderState.DA_XUAT_HANG)
            {
                // Kiểm tra có deliveryCode và isDone = false trong tblCallToTrough không => nếu có thì set isDone = true
                await _callToTroughRepository.UpdateWhenCanRa(websaleOrder.deliveryCode);

                if (!_storeOrderOperatingRepository.CheckExist(websaleOrder.id))
                {
                    isSynced = await _storeOrderOperatingRepository.CreateAsync(websaleOrder);
                }
                else 
                { 
                    isSynced = await _storeOrderOperatingRepository.UpdateReceivedOrder(websaleOrder);
                }
            }
            else if (stateId == (int)OrderState.DA_HUY_DON)
            {
                // Kiểm tra có deliveryCode và isDone = false trong tblCallToTrough không => nếu có thì set isDone = true
                await _callToTroughRepository.UpdateWhenHuyDon(websaleOrder.deliveryCode);

                isSynced = await _storeOrderOperatingRepository.CancelOrder(websaleOrder.id);
            }

            return isSynced;
        }
    }
}
