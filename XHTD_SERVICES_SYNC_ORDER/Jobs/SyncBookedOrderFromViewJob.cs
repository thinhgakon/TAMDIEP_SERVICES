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
            DataTable orderTable = new DataTable();

            string strConString = System.Configuration.ConfigurationManager.ConnectionStrings["TAMDIEP_ORACLE"].ConnectionString.ToString();
            string sqlQuery = @"SELECT * 
                                FROM APPS.DEV_SALES_ORDERS_MBF_V
                                WHERE CREATION_DATE BETWEEN TO_DATE(:startDate,'dd/MM/yyyy') AND TO_DATE(:endDate,'dd/MM/yyyy') 
                                ORDER BY ORDER_ID DESC";

            try
            {
                using (OracleConnection sqlCon = new OracleConnection(strConString))
                {
                    sqlCon.Open();

                    using (OracleCommand sqlCmd = new OracleCommand(sqlQuery, sqlCon))
                    {
                        var startDate = DateTime.Now.AddHours(-1 * numberHoursSearchOrder).ToString("dd/MM/yyyy");
                        var endDate = DateTime.Now.ToString("dd/MM/yyyy");

                        sqlCmd.Parameters.Add(new OracleParameter("startDate", startDate));
                        sqlCmd.Parameters.Add(new OracleParameter("endDate", endDate));

                        using (OracleDataAdapter sqlAdpt = new OracleDataAdapter(sqlCmd))
                        {
                            sqlAdpt.Fill(orderTable);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _syncOrderLogger.LogInfo("GetWebsaleViewOrder error: " + ex.Message);
            }

            List<OrderItemResponse> orderList = new List<OrderItemResponse>();

            foreach (DataRow orderRow in orderTable.Rows)
            {
                var orderDate = DateTime.ParseExact(orderRow["ORDER_DATE"].ToString(), "dd-MMM-yy HH:mm:ss", CultureInfo.InvariantCulture);
                var orderDateString = orderDate.ToString("yyyy-MM-ddTHH:mm:ss");

                var lastUpdateDate = DateTime.ParseExact(orderRow["LAST_UPDATE_DATE"].ToString(), "dd-MMM-yy HH:mm:ss", CultureInfo.InvariantCulture);
                var lastUpdateDateString = lastUpdateDate.ToString("yyyy-MM-ddTHH:mm:ss");

                var order = new OrderItemResponse()
                {
                    vehicleCode = orderRow["VEHICLE_CODE"].ToString(),
                    driverName = orderRow["DRIVER_NAME"].ToString(),
                    customerName = orderRow["CUSTOMER_NAME"].ToString(),
                    productName = orderRow["PRODUCT_NAME"].ToString(),
                    bookQuantity = decimal.Parse(orderRow["BOOK_QUANTITY"].ToString()),
                    id = int.Parse(orderRow["ORDER_ID"].ToString()),
                    deliveryCode = orderRow["DELIVERY_CODE"].ToString(),
                    orderDate = orderDateString,
                    moocCode = orderRow["MOOC_CODE"].ToString(),
                    locationCode = orderRow["LOCATION_CODE"].ToString(),
                    transportMethodId = int.Parse(orderRow["TRANSPORT_METHOD_ID"].ToString()),
                    status = orderRow["STATUS"].ToString(),
                    lastUpdatedDate = lastUpdateDateString
                };

                orderList.Add(order);
            }

            return orderList;
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
