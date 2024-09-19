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
using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.Globalization;

namespace XHTD_SERVICES_SYNC_ORDER.Jobs
{
    public class SyncBookedOrderFromViewJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly VehicleRepository _vehicleRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly Notification _notification;

        protected readonly SyncOrderLogger _syncOrderLogger;

        protected const string SERVICE_ACTIVE_CODE = "SYNC_ORDER_ACTIVE";

        protected const string SYNC_ORDER_HOURS = "SYNC_BOOKED_ORDER_MINUTES";

        private static bool isActiveService = true;

        private static int numberHoursSearchOrder = 48;

        public SyncBookedOrderFromViewJob(
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
            _syncOrderLogger.LogInfo($"Start Sync Booked Order From View: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");

            var orderList = GetWebsaleViewOrder();

            foreach (var order in orderList)
            {
                await SyncWebsaleOrderToDMS(order);
            }
        }

        public List<OrderItemResponse> GetWebsaleViewOrder()
        {
            string strConString = System.Configuration.ConfigurationManager.ConnectionStrings["TAMDIEP_ORACLE"].ConnectionString.ToString();
            OracleHelper oracleHelper = new OracleHelper(strConString);
            string sqlQuery = @"SELECT VEHICLE_CODE, DRIVER_NAME, CUSTOMER_NAME, PRODUCT_NAME, BOOK_QUANTITY, ORDER_ID, DELIVERY_CODE, ORDER_DATE, MOOC_CODE, LOCATION_CODE, LOCATION_CODE_TGC, TRANSPORT_METHOD_ID, STATUS, LAST_UPDATE_DATE, ITEM_CATEGORY
                                FROM APPS.DEV_SALES_ORDERS_MBF_V
                                WHERE CREATION_DATE BETWEEN :startDate AND :endDate
                                ORDER BY STATUS ASC";

            var startDate = DateTime.Now.AddHours(-1 * numberHoursSearchOrder);
            var endDate = DateTime.Now.AddDays(1);

            OrderItemResponse mapFunc(IDataReader reader) => new OrderItemResponse
            {
                vehicleCode = reader["VEHICLE_CODE"].ToString(),
                driverName = reader["DRIVER_NAME"].ToString(),
                customerName = reader["CUSTOMER_NAME"].ToString(),
                productName = reader["PRODUCT_NAME"].ToString(),
                bookQuantity = decimal.TryParse(reader["BOOK_QUANTITY"].ToString(), out decimal d) ? d : default ,
                id = int.TryParse(reader["ORDER_ID"]?.ToString(), out int i) ? i : default,
                deliveryCode = reader["DELIVERY_CODE"].ToString(),
                orderDate = reader["ORDER_DATE"]?.ToString() == null ? null : reader.GetDateTime(7).ToString("yyyy-MM-ddTHH:mm:ss"),
                moocCode = reader["MOOC_CODE"].ToString(),
                locationCode = reader["LOCATION_CODE"].ToString(),
                locationCodeTgc = reader["LOCATION_CODE_TGC"].ToString(),
                transportMethodId = int.TryParse(reader["TRANSPORT_METHOD_ID"]?.ToString(),out int t) ? t : default,
                status = reader["STATUS"].ToString(),
                lastUpdatedDate = reader["LAST_UPDATE_DATE"]?.ToString() == null ? null : reader.GetDateTime(12).ToString("yyyy-MM-ddTHH:mm:ss"),
                itemCategory = reader["ITEM_CATEGORY"].ToString()
            };

            List<OrderItemResponse> result = oracleHelper.GetDataFromOracle(sqlQuery, mapFunc, new[] { new OracleParameter("startDate", startDate), new OracleParameter("endDate", endDate) });
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

            if (stateId == (int)OrderState.DA_DAT_HANG)
            {
                isSynced = await _storeOrderOperatingRepository.CreateAsync(websaleOrder);

                if (isSynced)
                {
                    var vehicleCode = websaleOrder.vehicleCode.Replace("-", "").Replace("  ", "").Replace(" ", "").Replace("/", "").Replace(".", "").ToUpper();
                    await _vehicleRepository.CreateAsync(vehicleCode);
                }
            }

            return isSynced;
        }
    }
}
