﻿using Newtonsoft.Json;
using Quartz;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Models.Response;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES_SYNC_ORDER.Models.Values;
using XHTD_SERVICES_SYNC_ORDER.Models.Response;

namespace XHTD_SERVICES_SYNC_ORDER.Jobs
{
    public class SyncChangedOrderFromViewJob : IJob
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

        private static int numberHoursSearchOrder = 2;

        public SyncChangedOrderFromViewJob(
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
                    _syncOrderLogger.LogInfo("Service dong bo don hang thay doi dang TAT");
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
            _syncOrderLogger.LogInfo($"Start Sync Changed Order From View: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} Updated");

            List<OrderItemResponse> websaleOrders = GetWebsaleOrderFromView();

            if (websaleOrders == null || websaleOrders.Count == 0)
            {
                _syncOrderLogger.LogInfo($"Sync Changed Order From View: Khong co don hang");
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
        }

        public List<OrderItemResponse> GetWebsaleOrderFromView()
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["TAMDIEP_ORACLE"].ConnectionString.ToString();

            OracleHelper oracleHelper = new OracleHelper(connectionString);

            string query = $@"SELECT ORDER_ID, DELIVERY_CODE, TIMEIN, TIMEOUT, LOADWEIGHTNULL, STATUS, LOADWEIGHTFULL, PRODUCT_NAME, VEHICLE_CODE, 
                            DRIVER_NAME, CUSTOMER_NAME, BOOK_QUANTITY, ORDER_DATE, MOOC_CODE, LOCATION_CODE, TRANSPORT_METHOD_ID, LAST_UPDATE_DATE, ITEM_CATEGORY, DOC_NUM 
                            FROM apps.dev_sales_orders_mbf_v 
                            WHERE LAST_UPDATE_DATE >= SYSTIMESTAMP - INTERVAL '{numberHoursSearchOrder}' HOUR";

            OrderItemResponse mapFunc(IDataReader reader) => new OrderItemResponse
            {
                id = Convert.ToInt32(reader["ORDER_ID"]),
                deliveryCode = reader["DELIVERY_CODE"]?.ToString(),
                timeIn = reader["TIMEIN"] == DBNull.Value ? null : reader.GetDateTime(2).ToString("yyyy-MM-ddTHH:mm:ss"),
                timeOut = reader["TIMEOUT"] == DBNull.Value ? null : reader.GetDateTime(3).ToString("yyyy-MM-ddTHH:mm:ss"),
                loadweightnull = reader["LOADWEIGHTNULL"]?.ToString(),
                loadweightfull = reader["LOADWEIGHTFULL"]?.ToString(),
                status = reader["STATUS"]?.ToString(),
                productName = reader["PRODUCT_NAME"]?.ToString(),
                vehicleCode = reader["VEHICLE_CODE"]?.ToString(),
                driverName = reader["DRIVER_NAME"]?.ToString(),
                customerName = reader["CUSTOMER_NAME"]?.ToString(),
                bookQuantity = decimal.TryParse(reader["BOOK_QUANTITY"]?.ToString(), out decimal bq) ? bq : default,
                orderDate = reader["ORDER_DATE"] == DBNull.Value ? null : reader.GetDateTime(12).ToString("yyyy-MM-ddTHH:mm:ss"),
                moocCode = reader["MOOC_CODE"]?.ToString(),
                locationCode = reader["LOCATION_CODE"]?.ToString(),
                transportMethodId = int.TryParse(reader["TRANSPORT_METHOD_ID"]?.ToString(), out int i) ? i : default,
                lastUpdatedDate = reader["LAST_UPDATE_DATE"] == DBNull.Value ? null : reader.GetDateTime(16).ToString("yyyy-MM-ddTHH:mm:ss"),
                itemCategory = reader["ITEM_CATEGORY"].ToString(),
                docnum = reader["DOC_NUM"].ToString(),
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

            if (stateId == (int)OrderState.DA_DAT_HANG)
            {
                isSynced = await _storeOrderOperatingRepository.ChangedAsync(websaleOrder);
            }

            return isSynced;
        }
    }
}
