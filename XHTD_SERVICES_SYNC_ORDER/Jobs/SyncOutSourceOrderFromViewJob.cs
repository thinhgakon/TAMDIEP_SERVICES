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
using XHTD_SERVICES.Helper.Models.Request;
using Autofac;
using XHTD_SERVICES_SYNC_ORDER.Business;
using XHTD_SERVICES_SYNC_ORDER.ws.hoangthach;
using XHTD_SERVICES_SYNC_ORDER.Models.Request;

namespace XHTD_SERVICES_SYNC_ORDER.Jobs
{
    public class SyncOutSourceOrderFromViewJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly VehicleRepository _vehicleRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly MachineRepository _machineRepository;

        protected readonly TroughRepository _troughRepository;

        protected readonly Notification _notification;

        protected readonly SyncOrderLogger _syncOrderLogger;

        private static string strToken;

        protected const string SERVICE_ACTIVE_CODE = "SYNC_ORDER_ACTIVE";

        protected const string SYNC_ORDER_HOURS = "SYNC_ORDER_HOURS";

        private static bool isActiveService = true;

        private static int numberHoursSearchOrder = 48;

        protected const string BIMSON_CUSTOMER_NUMBER = "44";

        protected const string HOANGTHACH_CUSTOMER_NUMBER = "56";

        public SyncOutSourceOrderFromViewJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            VehicleRepository vehicleRepository,
            CallToTroughRepository callToTroughRepository,
            SystemParameterRepository systemParameterRepository,
            MachineRepository machineRepository,
            TroughRepository troughRepository,
            Notification notification,
            SyncOrderLogger syncOrderLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _vehicleRepository = vehicleRepository;
            _callToTroughRepository = callToTroughRepository;
            _systemParameterRepository = systemParameterRepository;
            _machineRepository = machineRepository;
            _troughRepository = troughRepository;
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
            _syncOrderLogger.LogInfo($"Start Sync OutSource Order From View: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");

            List<OrderItemResponse> websaleOrders = GetWebsaleOrderFromView();

            if (websaleOrders == null || websaleOrders.Count == 0)
            {
                _syncOrderLogger.LogInfo($"Sync OutSource Order From View: Khong co don hang");
                return;
            }

            foreach (var websaleOrder in websaleOrders)
            {
                // Hoang Thach
                if (websaleOrder.customerNumber == HOANGTHACH_CUSTOMER_NUMBER)
                {
                    bool isSynced = await SyncWebsaleOrderToHoangThach(websaleOrder);
                }
                // Bim Son
                else if (websaleOrder.customerNumber == BIMSON_CUSTOMER_NUMBER)
                {
                    bool isSynced = await SyncWebsaleOrderToBimSon(websaleOrder);
                }
            }
        }

        public List<OrderItemResponse> GetWebsaleOrderFromView()
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["TAMDIEP_ORACLE"].ConnectionString.ToString();

            OracleHelper oracleHelper = new OracleHelper(connectionString);

            string query = $@"SELECT DELIVERY_CODE, DELIVERY_CODE_TGC, VEHICLE_CODE, TIMEIN, TIMEOUT, ORDER_DATE, LOADWEIGHTNULL, LOADWEIGHTFULL, STATUS, PRINT_STATUS,
                                     ORDER_QUANTITY, LAST_UPDATE_DATE, DOC_NUM, CUSTOMER_ID, CUSTOMER_NUMBER, CUSTOMER_NAME
                            FROM apps.dev_sales_orders_mbf_v 
                            WHERE DELIVERY_CODE_TGC IS NOT NULL AND LAST_UPDATE_DATE >= SYSTIMESTAMP - INTERVAL '{numberHoursSearchOrder}' HOUR";

            OrderItemResponse mapFunc(IDataReader reader) => new OrderItemResponse
            {
                deliveryCode = reader["DELIVERY_CODE"]?.ToString(),
                deliveryCodeTgc = reader["DELIVERY_CODE_TGC"]?.ToString(),
                vehicleCode = reader["VEHICLE_CODE"]?.ToString(),
                timeIn = reader["TIMEIN"] == DBNull.Value ? null : reader.GetDateTime(3).ToString("yyyy-MM-ddTHH:mm:ss"),
                timeOut = reader["TIMEOUT"] == DBNull.Value ? null : reader.GetDateTime(4).ToString("yyyy-MM-ddTHH:mm:ss"),
                orderDate = reader["ORDER_DATE"] == DBNull.Value ? null : reader.GetDateTime(5).ToString("yyyy-MM-ddTHH:mm:ss"),
                loadweightnull = reader["LOADWEIGHTNULL"]?.ToString(),
                loadweightfull = reader["LOADWEIGHTFULL"]?.ToString(),
                status = reader["STATUS"]?.ToString(),
                orderPrintStatus = reader["PRINT_STATUS"]?.ToString(),
                bookQuantity = decimal.TryParse(reader["ORDER_QUANTITY"]?.ToString(), out decimal bq) ? bq : default,
                lastUpdatedDate = reader["LAST_UPDATE_DATE"] == DBNull.Value ? null : reader.GetDateTime(11).ToString("yyyy-MM-ddTHH:mm:ss"),
                docnum = reader["DOC_NUM"].ToString(),
                customerId = reader["CUSTOMER_ID"].ToString(),
                customerNumber = reader["CUSTOMER_NUMBER"].ToString(),
                customerName = reader["CUSTOMER_NAME"].ToString(),
            };

            List<OrderItemResponse> result = oracleHelper.GetDataFromOracle(query, mapFunc);
            return result;
        }

        public async Task<bool> SyncWebsaleOrderToHoangThach(OrderItemResponse websaleOrder)
        {
            bool isSynced = false;

            var stateId = 0;

            var type = 1;
            var userName = "vicemtamdiep";
            var password = "77889911";
            var data = "";

            if (websaleOrder.status.ToUpper() == "BOOKED" && websaleOrder.orderPrintStatus.ToUpper() == "PRINTED")
            {
                stateId = (int)OrderState.DANG_LAY_HANG;
            }
            else if (websaleOrder.status.ToUpper() == "RECEIVED" && !String.IsNullOrEmpty(websaleOrder.docnum))
            {
                stateId = (int)OrderState.DA_XUAT_HANG;
            }

            if (stateId == (int)OrderState.DANG_LAY_HANG)
            {
                var obj = new HTOrderRequest
                {
                    DELIVERY_CODE_TD = websaleOrder.deliveryCode,
                    DELIVERY_CODE_HT = websaleOrder.deliveryCodeTgc,
                    VEHICLE_CODE = websaleOrder.vehicleCode,
                    TIME_IN = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    TIME_OUT = null,
                    ORDER_DATE = websaleOrder.orderDate,
                    LOADWEIGHTNULL = 0.0,
                    LOADWEIGHTFULL = 0.0,
                    SO_STATUS = "RECEIVING",
                    //ORDER_QUANTITY = 0.0,
                };

                data = JsonConvert.SerializeObject(obj);

                // kiểm tra MSGH đã được đồng bộ 
                var isSyncedOutSource1 = _storeOrderOperatingRepository.CheckIsSyncedOutSource1(websaleOrder.deliveryCode);
                if (isSyncedOutSource1)
                {
                    _syncOrderLogger.LogInfo($"Đơn hàng đã được đồng bộ lần 1 => Kết thúc");
                    return isSynced;
                }

                var ws_ht = new DongBoGiaCongHoangThachTamDiep();
                var wsResult = ws_ht.ReciverData(1, data, userName, password);

                if (wsResult == "ReceivedData")
                {
                    // Đánh dấu MSGH này đã được đồng bộ sang HT
                    await _storeOrderOperatingRepository.MarkIsSyncedOutSource1(websaleOrder.deliveryCode);
                }

            }
            else if (stateId == (int)OrderState.DA_XUAT_HANG)
            {
                var orderDateString = websaleOrder?.orderDate;
                DateTime orderDate = DateTime.ParseExact(orderDateString, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

                DateTime timeInDate = !string.IsNullOrEmpty(websaleOrder?.timeIn) ?
                                        DateTime.ParseExact(websaleOrder?.timeIn, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) :
                                        DateTime.MinValue;

                DateTime timeOutDate = !string.IsNullOrEmpty(websaleOrder?.timeOut) ?
                                        DateTime.ParseExact(websaleOrder?.timeOut, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) :
                                        DateTime.MinValue;

                // gui du lieu lan 2 khi can ra va co pkx
                var obj = new HTOrderRequest
                {
                    DELIVERY_CODE_TD = websaleOrder.deliveryCode,
                    DELIVERY_CODE_HT = websaleOrder.deliveryCodeTgc,
                    VEHICLE_CODE = websaleOrder.vehicleCode,
                    TIME_IN = !string.IsNullOrEmpty(websaleOrder.timeIn) ? timeInDate.ToString("dd/MM/yyyy HH:mm:ss") : null,
                    TIME_OUT = !string.IsNullOrEmpty(websaleOrder.timeOut) ? timeOutDate.ToString("dd/MM/yyyy HH:mm:ss") : null,
                    ORDER_DATE = orderDate.ToString("dd/MM/yyyy HH:mm:ss"),
                    LOADWEIGHTNULL = !string.IsNullOrEmpty(websaleOrder.loadweightnull) ? Double.Parse(websaleOrder.loadweightnull) : 0.0,
                    LOADWEIGHTFULL = !string.IsNullOrEmpty(websaleOrder.loadweightfull) ? Double.Parse(websaleOrder.loadweightfull) : 0.0,
                    SO_STATUS = "RECEIVED",
                    ORDER_QUANTITY = (double)websaleOrder.bookQuantity,
                };

                data = JsonConvert.SerializeObject(obj);

                // kiểm tra MSGH đã được đồng bộ 
                var isSyncedOutSource2 = _storeOrderOperatingRepository.CheckIsSyncedOutSource2(websaleOrder.deliveryCode);
                if (isSyncedOutSource2)
                {
                    _syncOrderLogger.LogInfo($"Đơn hàng đã được đồng bộ lần 2 => Kết thúc");
                    return isSynced;
                }

                var ws_ht = new DongBoGiaCongHoangThachTamDiep();
                var wsResult = ws_ht.ReciverData(1, data, userName, password);

                if (wsResult == "ReceivedData")
                {
                    // Đánh dấu MSGH này đã được đồng bộ sang HT
                    await _storeOrderOperatingRepository.MarkIsSyncedOutSource2(websaleOrder.deliveryCode);
                }
            }

            return isSynced;
        }

        public async Task<bool> SyncWebsaleOrderToBimSon(OrderItemResponse websaleOrder)
        {
            bool isSynced = false;

            var stateId = 0;

            if(websaleOrder.status.ToUpper() == "BOOKED" && websaleOrder.orderPrintStatus.ToUpper() == "PRINTED")
            {
                stateId = (int)OrderState.DANG_LAY_HANG;
            }
            else if (websaleOrder.status.ToUpper() == "RECEIVED" && !String.IsNullOrEmpty(websaleOrder.docnum))
            {
                stateId = (int)OrderState.DA_XUAT_HANG;
            }

            if (stateId == (int)OrderState.DANG_LAY_HANG)
            {
                // gui du lieu lan 1 khi in phieu thanh cong

            }
            else if (stateId == (int)OrderState.DA_XUAT_HANG)
            {
                // gui du lieu lan 2 khi can ra va co pkx

            }

            return isSynced;
        }
    }
}
