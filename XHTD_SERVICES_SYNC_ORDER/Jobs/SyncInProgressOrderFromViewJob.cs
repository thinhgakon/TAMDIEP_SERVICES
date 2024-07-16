using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using XHTD_SERVICES.Data.Repositories;
using RestSharp;
using XHTD_SERVICES_SYNC_ORDER.Models.Response;
using XHTD_SERVICES.Data.Models.Response;
using Newtonsoft.Json;
using XHTD_SERVICES_SYNC_ORDER.Models.Values;
using XHTD_SERVICES.Helper;
using System.Data;
using System.Globalization;

namespace XHTD_SERVICES_SYNC_ORDER.Jobs
{
    public class SyncInProgressOrderFromViewJob : IJob
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

        public SyncInProgressOrderFromViewJob(
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

            if (activeParameter == null || activeParameter.Value == "0")
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
            _syncOrderLogger.LogInfo($"Start Sync In Progress Order From View: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");

            //GetToken();

            List<OrderItemResponse> websaleOrders = GetWebsaleOrderFromView();

            if (websaleOrders == null || websaleOrders.Count == 0)
            {
                return;
            }

            bool isChanged = false;

            foreach (var websaleOrder in websaleOrders)
            {
                // Không đồng bộ các đơn tại sông Thao
                if (websaleOrder.shippointId != "13")
                {
                    bool isSynced = await SyncWebsaleOrderToDMS(websaleOrder);

                    if (!isChanged) isChanged = isSynced;
                }
            }

            //_syncOrderLogger.LogInfo($"Done Sync In Progress Order: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");
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
            IRestResponse response = HttpRequest.GetWebsaleOrderByUpdated(strToken, numberHoursSearchOrder);
            var content = response.Content;

            if (response.StatusDescription.Equals("Unauthorized"))
            {
                _syncOrderLogger.LogInfo("Unauthorized GetWebsaleOrder");

                return null;
            }

            var responseData = JsonConvert.DeserializeObject<SearchOrderResponse>(content);

            return responseData.collection.OrderBy(x => x.id).ToList();
        }

        public List<OrderItemResponse> GetWebsaleOrderFromView()
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["TAMDIEP_ORACLE"].ConnectionString.ToString();

            OracleHelper oracleHelper = new OracleHelper(connectionString);

            string query = $"SELECT ORDER_ID, DELIVERY_CODE, TIMEIN, TIMEOUT, LOADWEIGHTNULL, STATUS, LOADWEIGHTFULL, PRODUCT_NAME, VEHICLE_CODE, " +
                           $"DRIVER_NAME, CUSTOMER_NAME, BOOK_QUANTITY, ORDER_DATE, MOOC_CODE, LOCATION_CODE, TRANSPORT_METHOD_ID, LAST_UPDATE_DATE " +
                           $"FROM apps.dev_sales_orders_mbf_v " +
                           $"WHERE ORDER_DATE >= SYSTIMESTAMP - INTERVAL '{numberHoursSearchOrder}' HOUR";

            OrderItemResponse mapFunc(IDataReader reader) => new OrderItemResponse
            {
                id = Convert.ToInt32(reader["ORDER_ID"]),
                deliveryCode = reader["DELIVERY_CODE"]?.ToString(),
                timeIn = string.IsNullOrEmpty(reader["TIMEIN"].ToString()) ? null : DateTime.ParseExact(reader["TIMEIN"].ToString(), "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture).ToString(),
                timeOut = string.IsNullOrEmpty(reader["TIMEOUT"].ToString()) ? null : DateTime.ParseExact(reader["TIMEOUT"].ToString(), "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture).ToString(),
                loadweightnull = reader["LOADWEIGHTNULL"].ToString(),
                loadweightfull = reader["LOADWEIGHTFULL"].ToString(),
                status = reader["STATUS"].ToString(),
                productName = reader["PRODUCT_NAME"].ToString(),
                vehicleCode = reader["VEHICLE_CODE"].ToString(),
                driverName = reader["DRIVER_NAME"].ToString(),
                customerName = reader["CUSTOMER_NAME"].ToString(),
                bookQuantity = decimal.Parse(reader["BOOK_QUANTITY"].ToString()),
                orderDate = string.IsNullOrEmpty(reader["ORDER_DATE"].ToString()) ? null : DateTime.ParseExact(reader["ORDER_DATE"].ToString(), "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture).ToString(),
                moocCode = reader["MOOC_CODE"].ToString(),
                locationCode = reader["LOCATION_CODE"].ToString(),
                transportMethodId = int.Parse(reader["TRANSPORT_METHOD_ID"].ToString()),
                lastUpdatedDate = string.IsNullOrEmpty(reader["LAST_UPDATE_DATE"].ToString()) ? null : DateTime.ParseExact(reader["LAST_UPDATE_DATE"].ToString(), "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture).ToString()
            };

            List<OrderItemResponse> result = oracleHelper.GetDataFromOracle(query, mapFunc);
            return result;
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

            if (stateId == (int)OrderState.DANG_LAY_HANG)
            {
                if (!_storeOrderOperatingRepository.CheckExist(websaleOrder.id))
                {
                    isSynced = await _storeOrderOperatingRepository.CreateAsync(websaleOrder);
                }
                else
                {
                    isSynced = await _storeOrderOperatingRepository.UpdateReceivingOrder(websaleOrder.id, websaleOrder.timeIn, websaleOrder.loadweightnull);
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
                    isSynced = await _storeOrderOperatingRepository.UpdateReceivedOrder(websaleOrder.id, websaleOrder.timeOut, websaleOrder.loadweightfull);
                }
            }
            else if (stateId == (int)OrderState.DA_HUY_DON)
            {
                // Kiểm tra có deliveryCode và isDone = false trong tblCallToTrough không => nếu có thì set isDone = true
                await _callToTroughRepository.UpdateWhenHuyDon(websaleOrder.deliveryCode);

                isSynced = await _storeOrderOperatingRepository.CancelOrder(websaleOrder.id);

                if (isSynced)
                {
                    // Gửi notification đơn bị hủy đến app lái xe
                    var canceledOrder = await _storeOrderOperatingRepository.GetDetail(websaleOrder.deliveryCode);
                    if (canceledOrder != null && !String.IsNullOrEmpty(canceledOrder.DriverUserName))
                    {
                        var currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                        //SendInfoNotification("khoanv", $"{websaleOrder.deliveryCode} {canceledOrder.DriverUserName} đã bị hủy lúc {currentTime}");
                    }
                }
            }

            return isSynced;
        }

        public void SendInfoNotification(string receiver, string message)
        {
            try
            {
                _notification.SendInforNotification(receiver, message);

                _syncOrderLogger.LogInfo($"SendInforNotification success: {message}");
            }
            catch (Exception ex)
            {
                _syncOrderLogger.LogInfo($"SendInfoNotification Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }
    }
}
