using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Models.Response;
using XHTD_SERVICES.Data.Models.Values;
using XHTD_SERVICES.Data.Common;

namespace XHTD_SERVICES.Data.Repositories
{
    public partial class StoreOrderOperatingRepository
    {
        public async Task<bool> CreateAsync(OrderItemResponse websaleOrder)
        {
            bool isSynced = false;

            var syncTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            try
            {
                string typeProduct = "";
                string typeXK = null;
                string productNameUpper = websaleOrder.productName.ToUpper();
                string itemCategory = websaleOrder.itemCategory;

                if (itemCategory == OrderCatIdCode.XI_MANG_XA)
                {
                    typeProduct = "ROI";
                }
                else if (itemCategory == OrderCatIdCode.CLINKER)
                {
                    typeProduct = OrderCatIdCode.CLINKER;
                }
                else
                {
                    // Type Product
                    if (productNameUpper.Contains("PCB30") || productNameUpper.Contains("MAX PRO"))
                    {
                        typeProduct = "PCB30";
                    }
                    else if (productNameUpper.Contains("PC30"))
                    {
                        typeProduct = "PC30";
                    }
                    else if (productNameUpper.Contains("PCB40"))
                    {
                        typeProduct = "PCB40";
                    }
                    else if (productNameUpper.Contains("PC40"))
                    {
                        typeProduct = "PC40";
                    }

                    // Type XK
                    if (productNameUpper.Contains(OrderTypeXKCode.JUMBO))
                    {
                        typeXK = OrderTypeXKCode.JUMBO;
                    }
                    else if (productNameUpper.Contains(OrderTypeXKCode.SLING))
                    {
                        typeXK = OrderTypeXKCode.SLING;
                    }
                }

                var vehicleCode = websaleOrder.vehicleCode.Replace("-", "").Replace("  ", "").Replace(" ", "").Replace("/", "").Replace(".", "").ToUpper();
                var rfidItem = _appDbContext.tblRfids.FirstOrDefault(x => x.Vehicle.Contains(vehicleCode));
                var cardNo = rfidItem?.Code ?? null;

                var orderDateString = websaleOrder?.orderDate;
                DateTime orderDate = DateTime.ParseExact(orderDateString, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

                var lastUpdatedDateString = websaleOrder?.lastUpdatedDate;
                DateTime lastUpdatedDate = DateTime.ParseExact(lastUpdatedDateString, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

                if (!CheckExist(websaleOrder.id))
                {
                    var newOrderOperating = new tblStoreOrderOperating
                    {
                        Vehicle = vehicleCode,
                        DriverName = websaleOrder.driverName,
                        NameDistributor = websaleOrder.customerName,
                        //ItemId = websaleOrder.INVENTORY_ITEM_ID,
                        NameProduct = websaleOrder.productName,
                        CatId = websaleOrder.itemCategory,
                        SumNumber = (decimal?)websaleOrder.bookQuantity,
                        CardNo = cardNo,
                        OrderId = websaleOrder.id,
                        DeliveryCode = websaleOrder.deliveryCode,
                        OrderDate = orderDate,
                        TypeProduct = typeProduct,
                        TypeXK = typeXK,
                        Confirm1 = 0,
                        Confirm2 = 0,
                        Confirm3 = 0,
                        Confirm4 = 0,
                        Confirm5 = 0,
                        Confirm6 = 0,
                        Confirm7 = 0,
                        Confirm8 = 0,
                        MoocCode = websaleOrder.moocCode,
                        LocationCode = websaleOrder.locationCode,
                        TransportMethodId = websaleOrder.transportMethodId,
                        TransportMethodName = websaleOrder.transportMethodName,
                        State = websaleOrder.status,
                        IndexOrder = 0,
                        IndexOrder2 = 0,
                        CountReindex = 0,
                        Step = (int)OrderStep.CHUA_NHAN_DON,
                        IsVoiced = false,
                        UpdateDay = lastUpdatedDate > DateTime.MinValue ? lastUpdatedDate : DateTime.Now,
                        LogProcessOrder = $@"#Sync Tạo đơn lúc {syncTime}",
                        LogJobAttach = $@"#Sync Tạo đơn lúc {syncTime}",
                        IsSyncedByNewWS = true
                    };

                    _appDbContext.tblStoreOrderOperatings.Add(newOrderOperating);
                    await _appDbContext.SaveChangesAsync();

                    Console.WriteLine($@"Inserted order orderId={websaleOrder.id} createDate={websaleOrder.createDate} lúc {syncTime}");
                    log.Info($@"Inserted order orderId={websaleOrder.id} createDate={websaleOrder.createDate} lúc {syncTime}");

                    isSynced = true;
                }
                else
                {
                    var order = _appDbContext.tblStoreOrderOperatings
                            .FirstOrDefault(x => x.OrderId == websaleOrder.id
                                                && x.IsVoiced == false
                                                && x.Step < (int)OrderStep.DA_CAN_VAO
                                                );
                    if (order != null)
                    {
                        if (lastUpdatedDate == null || lastUpdatedDate <= DateTime.MinValue)
                        {
                            return false;
                        }

                        if (order.UpdateDay == null || order.UpdateDay < lastUpdatedDate)
                        {
                            log.Info($@"Sync Update before orderId={order.OrderId} Vehicle={order.Vehicle} DriverName={order.DriverName} CardNo={order.CardNo} SumNumber={order.SumNumber}");

                            order.Vehicle = vehicleCode;
                            order.DriverName = websaleOrder.driverName;
                            order.CardNo = cardNo;
                            order.SumNumber = (decimal?)websaleOrder.bookQuantity;
                            order.UpdateDay = lastUpdatedDate;

                            order.LogProcessOrder = $@"{order.LogProcessOrder} #Sync Update lúc {syncTime}; ";
                            order.LogJobAttach = $@"{order.LogJobAttach} #Sync Update lúc {syncTime}; ";

                            await _appDbContext.SaveChangesAsync();

                            log.Info($@"Sync Update after orderId={websaleOrder.id} Vehicle={vehicleCode} DriverName={websaleOrder.driverName} CardNo={cardNo} SumNumber={websaleOrder.bookQuantity}");
                        }
                    }
                }

                return isSynced;
            }
            catch (Exception ex)
            {
                log.Error("=========================== CreateAsync Error: " + ex.Message + " ========== " + ex.StackTrace + " === " + ex.InnerException); ;
                Console.WriteLine("CreateAsync Error: " + ex.Message);

                return isSynced;
            }
        }

        public async Task<bool> UpdateReceivingOrder(int? orderId, string timeIn, string loadweightnull)
        {
            bool isSynced = false;

            var syncTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            var weightIn = Double.Parse(loadweightnull);

            try
            {
                DateTime timeInDate = DateTime.ParseExact(timeIn, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

                var order = _appDbContext.tblStoreOrderOperatings
                            .FirstOrDefault(x => x.OrderId == orderId
                                                &&
                                                (
                                                    x.Step < (int)OrderStep.DA_CAN_VAO
                                                    ||
                                                    x.WeightIn == null
                                                )
                                            );

                if (order != null)
                {
                    log.Info($@"===== Update Receiving Order {orderId} timeIn={timeIn} lúc {syncTime}: WeightIn {order.WeightInAuto} ==>> {weightIn * 1000}");

                    order.Confirm2 = 1;
                    order.TimeConfirm2 = order.TimeConfirm2 ?? DateTime.Now;
                    order.Confirm3 = 1;
                    order.TimeConfirm3 = timeInDate > DateTime.MinValue ? timeInDate : DateTime.Now;

                    if(order.Step < (int)OrderStep.DA_CAN_VAO) { 
                        order.Step = (int)OrderStep.DA_CAN_VAO;
                    }

                    order.IndexOrder = 0;
                    order.CountReindex = 0;

                    order.WeightIn = (int)(weightIn * 1000);
                    order.WeightInTime = timeInDate > DateTime.MinValue ? timeInDate : DateTime.Now;

                    order.LogProcessOrder = $@"{order.LogProcessOrder} #Sync Cân vào lúc {syncTime}; ";
                    order.LogJobAttach = $@"{order.LogJobAttach} #Sync Cân vào lúc {syncTime}; ";

                    await _appDbContext.SaveChangesAsync();

                    Console.WriteLine($@"Update Receiving Order {orderId}");
                    log.Info($@"Update Receiving Order {orderId}");

                    isSynced = true;
                }

                return isSynced;
            }
            catch (Exception ex)
            {
                log.Error($@"=========================== Update Receiving Order {orderId} Error: " + ex.Message + " ====== " + ex.StackTrace + "==============" + ex.InnerException);
                Console.WriteLine($@"Update Receiving Order {orderId} Error: " + ex.Message);

                return isSynced;
            }
        }

        public async Task<bool> UpdateReceivedOrder(int? orderId, string timeOut, string loadweightfull)
        {
            bool isSynced = false;

            var syncTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            var weightOut = Double.Parse(loadweightfull);

            try
            {
                DateTime timeOutDate = DateTime.ParseExact(timeOut, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

                // TODO: nếu thời gian cân ra > hiện tại 1 tiếng thì step = DA_HOAN_THANH
                if (timeOutDate > DateTime.Now.AddMinutes(-30))
                {
                    var order = _appDbContext.tblStoreOrderOperatings
                                .FirstOrDefault(x => x.OrderId == orderId
                                                    &&
                                                    (
                                                        x.Step < (int)OrderStep.DA_CAN_RA
                                                        ||
                                                        x.WeightOut == null
                                                    )
                                               );

                    if (order != null)
                    {
                        log.Info($@"===== Update Received Order {orderId} timeOut={timeOut} lúc {syncTime}: WeightOut {order.WeightOutAuto} ==>> {weightOut * 1000}");

                        order.Confirm7 = 1;
                        order.TimeConfirm7 = timeOutDate > DateTime.MinValue ? timeOutDate : DateTime.Now;

                        if (order.Step < (int)OrderStep.DA_CAN_RA)
                        {
                            order.Step = (int)OrderStep.DA_CAN_RA;
                        }

                        order.IndexOrder = 0;
                        order.CountReindex = 0;

                        order.WeightOut = (int)(weightOut * 1000);
                        order.WeightOutTime = timeOutDate > DateTime.MinValue ? timeOutDate : DateTime.Now;

                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Sync Cân ra lúc {syncTime} ";
                        order.LogJobAttach = $@"{order.LogJobAttach} #Sync Cân ra lúc {syncTime}; ";

                        await _appDbContext.SaveChangesAsync();

                        Console.WriteLine($@"Sync Update Received => DA_CAN_RA Order {orderId}");
                        log.Info($@"Sync Update Received => DA_CAN_RA Order {orderId}");

                        isSynced = true;
                    }
                }
                else if (timeOutDate > DateTime.Now.AddMinutes(-60))
                {
                    var order = _appDbContext.tblStoreOrderOperatings
                                .FirstOrDefault(x => x.OrderId == orderId
                                                    && x.Step < (int)OrderStep.DA_HOAN_THANH);
                    if (order != null)
                    {
                        order.Confirm8 = 1;
                        order.TimeConfirm8 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_HOAN_THANH;
                        order.IndexOrder = 0;
                        order.CountReindex = 0;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Sync Ra cổng lúc {syncTime};";
                        order.LogJobAttach = $@"{order.LogJobAttach} #Sync Ra cổng lúc {syncTime};";

                        await _appDbContext.SaveChangesAsync();

                        Console.WriteLine($@"Sync Update Received => DA_HOAN_THANH Order {orderId}");
                        log.Info($@"Sync Update Received => DA_HOAN_THANH Order {orderId}");

                        isSynced = true;
                    }
                }
                else
                {
                    var order = _appDbContext.tblStoreOrderOperatings
                                .FirstOrDefault(x => x.OrderId == orderId
                                                    && x.Step < (int)OrderStep.DA_GIAO_HANG);
                    if (order != null)
                    {
                        order.Confirm9 = 1;
                        order.TimeConfirm9 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_GIAO_HANG;
                        order.IndexOrder = 0;
                        order.CountReindex = 0;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Sync Đã giao hàng lúc {syncTime};";
                        order.LogJobAttach = $@"{order.LogJobAttach} #Sync Đã giao hàng lúc {syncTime};";

                        await _appDbContext.SaveChangesAsync();

                        Console.WriteLine($@"Update Received => DA_GIAO_HANG Order {orderId}");
                        log.Info($@"Update Received => DA_GIAO_HANG Order {orderId}");

                        isSynced = true;
                    }
                }

                return isSynced;
            }
            catch (Exception ex)
            {
                log.Error($@"=========================== Update Received Order {orderId} Error: " + ex.Message + " ============ " + ex.StackTrace + " ==== " + ex.InnerException);
                Console.WriteLine($@"Update Received Order {orderId} Error: " + ex.Message);

                return isSynced;
            }
        }

        public async Task<bool> CancelOrder(int? orderId)
        {
            bool isSynced = false;

            try
            {
                string syncTime = DateTime.Now.ToString();

                var order = _appDbContext.tblStoreOrderOperatings.FirstOrDefault(x => x.OrderId == orderId && x.IsVoiced != true && x.Step < (int)OrderStep.DA_HOAN_THANH);
                if (order != null)
                {
                    order.IsVoiced = true;
                    order.LogJobAttach = $@"{order.LogJobAttach} #Sync Hủy đơn lúc {syncTime} ";
                    order.LogProcessOrder = $@"{order.LogProcessOrder} #Sync Hủy đơn lúc {syncTime} ";

                    await _appDbContext.SaveChangesAsync();

                    Console.WriteLine($@"Cancel Order {orderId}");
                    log.Info($@"Cancel Order {orderId}");

                    isSynced = true;
                }

                return isSynced;
            }
            catch (Exception ex)
            {
                log.Error($@"=========================== Cancel Order {orderId} Error: " + ex.Message);
                Console.WriteLine($@"Cancel Order {orderId} Error: " + ex.Message);

                return isSynced;
            }
        }
    }
}
