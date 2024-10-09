﻿using System;
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

                if (itemCategory == OrderCatIdCode.CLINKER)
                {
                    typeProduct = OrderTypeProductCode.CLINKER;
                }
                else if (itemCategory == OrderCatIdCode.XI_MANG_XA)
                {
                    typeProduct = OrderTypeProductCode.ROI;
                }
                else
                {
                    // Type XK
                    if (productNameUpper.Contains(OrderTypeXKCode.JUMBO))
                    {
                        typeXK = OrderTypeXKCode.JUMBO;
                        typeProduct = OrderTypeProductCode.JUMBO;
                    }
                    else if (productNameUpper.Contains(OrderTypeXKCode.SLING)
                        || productNameUpper.Contains(OrderTypeXKCode.SILING))
                    {
                        typeXK = OrderTypeXKCode.SLING;
                        typeProduct = OrderTypeProductCode.SLING;
                    }
                    else if (productNameUpper.Contains("PCB30") 
                        || productNameUpper.Contains("PCB 30")
                        || productNameUpper.Contains("MAX PRO")
                        )
                    {
                        typeProduct = OrderTypeProductCode.PCB30;
                    }
                    else if (productNameUpper.Contains("PCB40") 
                        || productNameUpper.Contains("PCB 40")
                        || productNameUpper.Contains("PC40")
                        )
                    {
                        typeProduct = OrderTypeProductCode.PCB40;
                    }
                    else if (productNameUpper.Contains("C91") 
                        || productNameUpper.Contains("XÂY TRÁT")
                        )
                    {
                        typeProduct = OrderTypeProductCode.C91;
                    }
                    else
                    {
                        typeProduct = OrderTypeProductCode.OTHER;
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
                    int? sourceDocumentId = null;
                    if (!string.IsNullOrEmpty(websaleOrder.sourceDocumentId))
                    {
                        sourceDocumentId = int.Parse(websaleOrder.sourceDocumentId);
                    }

                    var newOrderOperating = new tblStoreOrderOperating
                    {
                        Vehicle = vehicleCode,
                        DriverName = websaleOrder.driverName,
                        NameDistributor = websaleOrder.customerName,
                        ItemId = !string.IsNullOrEmpty(websaleOrder.productId) ? Double.Parse(websaleOrder.productId) : 0,
                        IDDistributorSyn = !string.IsNullOrEmpty(websaleOrder.customerId) ? int.Parse(websaleOrder.customerId) : 0,
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
                        Confirm9 = 0,
                        MoocCode = websaleOrder.moocCode,
                        LocationCode = websaleOrder.locationCode,
                        LocationCodeTgc = websaleOrder.locationCodeTgc,
                        TransportMethodId = websaleOrder.transportMethodId,
                        TransportMethodName = websaleOrder.transportMethodName,
                        State = websaleOrder.status,
                        IndexOrder = 0,
                        IndexOrder1 = 0,
                        IndexOrder2 = 0,
                        CountReindex = 0,
                        Step = (int)OrderStep.CHUA_NHAN_DON,
                        IsVoiced = false,
                        UpdateDay = lastUpdatedDate > DateTime.MinValue ? lastUpdatedDate : DateTime.Now,
                        LogProcessOrder = $@"#Sync Tạo đơn lúc {syncTime}",
                        LogJobAttach = $@"#Sync Tạo đơn lúc {syncTime}",
                        IsSyncedByNewWS = true,
                        SourceDocumentId = sourceDocumentId,
                        ItemAlias = websaleOrder.itemalias,
                        NetWeight = !string.IsNullOrEmpty(websaleOrder.netweight) ? Double.Parse(websaleOrder.netweight) : 0,
                    };

                    _appDbContext.tblStoreOrderOperatings.Add(newOrderOperating);
                    await _appDbContext.SaveChangesAsync();

                    Console.WriteLine($@"Inserted order orderId={websaleOrder.id} createDate={websaleOrder.createDate} lúc {syncTime}");
                    log.Info($@"Inserted order orderId={websaleOrder.id} createDate={websaleOrder.createDate} lúc {syncTime}");

                    isSynced = true;
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

        public async Task<bool> ChangedAsync(OrderItemResponse websaleOrder)
        {
            bool isSynced = false;

            var syncTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            try
            {
                string typeProduct = "";
                string typeXK = null;
                string productNameUpper = websaleOrder.productName.ToUpper();
                string itemCategory = websaleOrder.itemCategory;

                if (itemCategory == OrderCatIdCode.CLINKER)
                {
                    typeProduct = OrderTypeProductCode.CLINKER;
                }
                else if (itemCategory == OrderCatIdCode.XI_MANG_XA)
                {
                    typeProduct = OrderTypeProductCode.ROI;
                }
                else
                {
                    // Type XK
                    if (productNameUpper.Contains(OrderTypeXKCode.JUMBO))
                    {
                        typeXK = OrderTypeXKCode.JUMBO;
                        typeProduct = OrderTypeProductCode.JUMBO;
                    }
                    else if (productNameUpper.Contains(OrderTypeXKCode.SLING)
                        || productNameUpper.Contains(OrderTypeXKCode.SILING))
                    {
                        typeXK = OrderTypeXKCode.SLING;
                        typeProduct = OrderTypeProductCode.SLING;
                    }
                    else if (productNameUpper.Contains("PCB30") 
                        || productNameUpper.Contains("PCB 30")
                        || productNameUpper.Contains("MAX PRO")
                        )
                    {
                        typeProduct = OrderTypeProductCode.PCB30;
                    }
                    else if (productNameUpper.Contains("PCB40") 
                        || productNameUpper.Contains("PCB 40")
                        || productNameUpper.Contains("PC40")
                        )
                    {
                        typeProduct = OrderTypeProductCode.PCB40;
                    }
                    else if (productNameUpper.Contains("C91") 
                        || productNameUpper.Contains("XÂY TRÁT")
                        )
                    {
                        typeProduct = OrderTypeProductCode.C91;
                    }
                    else
                    {
                        typeProduct = OrderTypeProductCode.OTHER;
                    }
                }

                var vehicleCode = websaleOrder.vehicleCode.Replace("-", "").Replace("  ", "").Replace(" ", "").Replace("/", "").Replace(".", "").ToUpper();
                var rfidItem = _appDbContext.tblRfids.FirstOrDefault(x => x.Vehicle.Contains(vehicleCode));
                var cardNo = rfidItem?.Code ?? null;

                var orderDateString = websaleOrder?.orderDate;
                DateTime orderDate = DateTime.ParseExact(orderDateString, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

                var lastUpdatedDateString = websaleOrder?.lastUpdatedDate;
                DateTime lastUpdatedDate = DateTime.ParseExact(lastUpdatedDateString, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

                if (CheckExist(websaleOrder.id))
                {
                    var order = _appDbContext.tblStoreOrderOperatings
                            .FirstOrDefault(x => x.OrderId == websaleOrder.id
                                                && x.IsVoiced == false
                                                //&& x.Step < (int)OrderStep.DA_CAN_VAO
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

                            if(order.Step < (int)OrderStep.DA_CAN_VAO)
                            {
                                order.Vehicle = vehicleCode;
                                order.DriverName = websaleOrder.driverName;
                                order.CardNo = cardNo;

                                order.CatId = websaleOrder.itemCategory;
                                order.NameProduct = websaleOrder.productName;
                                order.ItemId = !string.IsNullOrEmpty(websaleOrder.productId) ? Double.Parse(websaleOrder.productId) : 0;
                                order.TypeProduct = typeProduct;
                                order.TypeXK = typeXK;
                                order.LocationCode = websaleOrder.locationCode;
                                order.LocationCodeTgc = websaleOrder.locationCodeTgc;
                                order.TransportMethodId = websaleOrder.transportMethodId;
                                order.TransportMethodName = websaleOrder.transportMethodName;
                            }

                            order.ItemAlias = websaleOrder.itemalias;
                            order.NetWeight = !string.IsNullOrEmpty(websaleOrder.netweight) ? Double.Parse(websaleOrder.netweight) : 0;

                            order.SumNumber = (decimal?)websaleOrder.bookQuantity;
                            order.OrderDate = orderDate;

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
                log.Error("=========================== ChangedAsync Error: " + websaleOrder.deliveryCode + " ========== " + ex.Message + " ========== " + ex.StackTrace + " === " + ex.InnerException);
                Console.WriteLine("ChangedAsync Error: " + websaleOrder.deliveryCode + " ========== " + ex.Message + " ========== " + ex.StackTrace + " === " + ex.InnerException);

                return isSynced;
            }
        }

        public async Task<bool> UpdateReceivingOrder(int? orderId, string timeIn, string loadweightnull)
        {
            bool isSynced = false;

            var syncTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            var weightIn = !string.IsNullOrEmpty(loadweightnull) ? Double.Parse(loadweightnull) : 0.0;

            try
            {
                DateTime timeInDate = !string.IsNullOrEmpty(timeIn) ? 
                                       DateTime.ParseExact(timeIn, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) :
                                       DateTime.MinValue;

                var order = _appDbContext.tblStoreOrderOperatings
                            .FirstOrDefault(x => x.OrderId == orderId
                                                &&
                                                (
                                                    x.Step < (int)OrderStep.DA_CAN_VAO
                                                    ||
                                                    x.Step == (int)OrderStep.DA_XAC_THUC
                                                    ||
                                                    x.Step == (int)OrderStep.CHO_GOI_XE
                                                    ||
                                                    x.Step == (int)OrderStep.DANG_GOI_XE
                                                    ||
                                                    x.WeightIn == null
                                                    ||
                                                    x.WeightIn == 0
                                                )
                                            );

                if (order != null)
                {
                    log.Info($@"===== Update Receiving Order {orderId} timeIn={timeInDate} lúc {syncTime}: WeightIn {order.WeightInAuto} ==>> {weightIn * 1000}");

                    order.TimeConfirm3 = timeInDate > DateTime.MinValue ? timeInDate : DateTime.Now;

                    if(order.Step < (int)OrderStep.DA_CAN_VAO 
                      || 
                      order.Step == (int)OrderStep.DA_XAC_THUC
                      ||
                      order.Step == (int)OrderStep.CHO_GOI_XE
                      ||
                      order.Step == (int)OrderStep.DANG_GOI_XE
                      ) 
                    { 
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

        public async Task<bool> UpdateReceivedOrder(int? orderId, string timeOut, string loadweightfull, string docnum = null)
        {
            bool isSynced = false;

            var syncTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            var weightOut = !string.IsNullOrEmpty(loadweightfull) ? Double.Parse(loadweightfull) : 0.0;

            try
            {
                DateTime timeOutDate = !string.IsNullOrEmpty(timeOut) ?
                                        DateTime.ParseExact(timeOut, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) :
                                        DateTime.MinValue;

                // TODO: nếu thời gian cân ra > hiện tại 1 tiếng thì step = DA_HOAN_THANH
                if (timeOutDate > DateTime.Now.AddMinutes(-30))
                {
                    var order = _appDbContext.tblStoreOrderOperatings
                                .FirstOrDefault(x => x.OrderId == orderId
                                                    &&
                                                    (
                                                        x.Step < (int)OrderStep.DA_CAN_RA
                                                        ||
                                                        x.Step == (int)OrderStep.DA_XAC_THUC
                                                        ||
                                                        x.Step == (int)OrderStep.CHO_GOI_XE
                                                        ||
                                                        x.Step == (int)OrderStep.DANG_GOI_XE
                                                        ||
                                                        x.WeightOut == null
                                                        ||
                                                        x.WeightOut == 0
                                                        ||
                                                        x.DocNum == null
                                                    )
                                               );

                    if (order != null)
                    {
                        log.Info($@"===== Update Received Order {orderId} timeOut={timeOutDate} lúc {syncTime}: WeightOut {order.WeightOutAuto} ==>> {weightOut * 1000}");

                        order.TimeConfirm7 = timeOutDate > DateTime.MinValue ? timeOutDate : DateTime.Now;

                        if (order.Step < (int)OrderStep.DA_CAN_RA 
                           || 
                           order.Step == (int)OrderStep.DA_XAC_THUC
                           ||
                           order.Step == (int)OrderStep.CHO_GOI_XE
                           ||
                           order.Step == (int)OrderStep.DANG_GOI_XE
                           )
                        {
                            order.Step = (int)OrderStep.DA_CAN_RA;
                        }

                        order.IndexOrder = 0;
                        order.CountReindex = 0;

                        order.WeightOut = (int)(weightOut * 1000);
                        order.WeightOutTime = timeOutDate > DateTime.MinValue ? timeOutDate : DateTime.Now;

                        order.DocNum = docnum;

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
                                                    &&
                                                    (
                                                        x.Step < (int)OrderStep.DA_HOAN_THANH
                                                        ||
                                                        x.Step == (int)OrderStep.DA_XAC_THUC
                                                        ||
                                                        x.Step == (int)OrderStep.CHO_GOI_XE
                                                        ||
                                                        x.Step == (int)OrderStep.DANG_GOI_XE
                                                    )
                                                );

                    if (order != null)
                    {
                        order.TimeConfirm8 = DateTime.Now;

                        if (order.Step < (int)OrderStep.DA_HOAN_THANH 
                           || 
                           order.Step == (int)OrderStep.DA_XAC_THUC
                           ||
                           order.Step == (int)OrderStep.CHO_GOI_XE
                           ||
                           order.Step == (int)OrderStep.DANG_GOI_XE
                           )
                        {
                            order.Step = (int)OrderStep.DA_HOAN_THANH;
                        }

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
                                                    && 
                                                    (
                                                        x.Step < (int)OrderStep.DA_GIAO_HANG
                                                        ||
                                                        x.Step == (int)OrderStep.DA_XAC_THUC
                                                        ||
                                                        x.Step == (int)OrderStep.CHO_GOI_XE
                                                        ||
                                                        x.Step == (int)OrderStep.DANG_GOI_XE
                                                    )
                                                );

                    if (order != null)
                    {
                        order.TimeConfirm9 = DateTime.Now;

                        if (order.Step < (int)OrderStep.DA_GIAO_HANG
                           ||
                           order.Step == (int)OrderStep.DA_XAC_THUC
                           ||
                           order.Step == (int)OrderStep.CHO_GOI_XE
                           ||
                           order.Step == (int)OrderStep.DANG_GOI_XE
                           )
                        {
                            order.Step = (int)OrderStep.DA_GIAO_HANG;
                        }
                        
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

                var order = _appDbContext.tblStoreOrderOperatings
                                            .FirstOrDefault(x => x.OrderId == orderId 
                                                                && x.IsVoiced != true 
                                                                && x.Step != (int)OrderStep.DA_HOAN_THANH
                                                                && x.Step != (int)OrderStep.DA_GIAO_HANG
                                                                );
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
