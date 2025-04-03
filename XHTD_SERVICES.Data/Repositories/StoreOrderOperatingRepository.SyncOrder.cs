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
using RestSharp;
using System.Collections.Specialized;
using System.Configuration;

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

                var productId = !string.IsNullOrEmpty(websaleOrder.productId) ? Int32.Parse(websaleOrder.productId) : 0;
                var itemConfig = _appDbContext.tblItemConfigs.FirstOrDefault(x => x.ItemIdSyn == productId);
                typeProduct = itemConfig?.TypeProductCode ?? OrderTypeProductCode.OTHER;

                if (typeProduct == OrderTypeProductCode.JUMBO)
                {
                    typeXK = OrderTypeXKCode.JUMBO;
                }
                else if (typeProduct == OrderTypeProductCode.SLING)
                {
                    typeXK = OrderTypeXKCode.SLING;
                }

                #region Old: Set type product
                //if (itemCategory == OrderCatIdCode.CLINKER)
                //{
                //    typeProduct = OrderTypeProductCode.CLINKER;
                //}
                //else if (itemCategory == OrderCatIdCode.XI_MANG_XA)
                //{
                //    typeProduct = OrderTypeProductCode.ROI;
                //}
                //else
                //{
                //    // Type XK
                //    if (productNameUpper.Contains(OrderTypeXKCode.JUMBO))
                //    {
                //        typeXK = OrderTypeXKCode.JUMBO;
                //        typeProduct = OrderTypeProductCode.JUMBO;
                //    }
                //    else if (productNameUpper.Contains(OrderTypeXKCode.SLING)
                //        || productNameUpper.Contains(OrderTypeXKCode.SILING))
                //    {
                //        typeXK = OrderTypeXKCode.SLING;
                //        typeProduct = OrderTypeProductCode.SLING;
                //    }
                //    else if (productNameUpper.Contains("PCB30")
                //        || productNameUpper.Contains("PCB 30")
                //        || productNameUpper.Contains("MAX PRO")
                //        )
                //    {
                //        typeProduct = OrderTypeProductCode.PCB30;
                //    }
                //    else if (productNameUpper.Contains("PCB40")
                //        || productNameUpper.Contains("PCB 40")
                //        || productNameUpper.Contains("PC40")
                //        )
                //    {
                //        typeProduct = OrderTypeProductCode.PCB40;
                //    }
                //    else if (productNameUpper.Contains("C91")
                //        || productNameUpper.Contains("XÂY TRÁT")
                //        )
                //    {
                //        typeProduct = OrderTypeProductCode.C91;
                //    }
                //    else
                //    {
                //        typeProduct = OrderTypeProductCode.OTHER;
                //    }
                //}
                #endregion

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
                        SealCount = !string.IsNullOrEmpty(websaleOrder.topSealCount) ? int.Parse(websaleOrder.topSealCount) : 0,
                        SealDes = websaleOrder.topSealDes,
                        DeliveryCodeTgc = websaleOrder.deliveryCodeTgc,
                        DocNum = websaleOrder.docnum,
                        RealNumber = (decimal?)websaleOrder.orderQuantity,
                        Type = websaleOrder.type,
                        AreaId  = !string.IsNullOrEmpty(websaleOrder.areaId) ? int.Parse(websaleOrder.areaId) : 0,
                        AreaCode = websaleOrder.areaCode,
                        AreaName = websaleOrder.areaName,
                        SourceDocumentName = websaleOrder.sourceDocumentName
                    };

                    _appDbContext.tblStoreOrderOperatings.Add(newOrderOperating);

                    var newHistory = new tblStoreOrderOperatingHistory
                    {
                        DeliveryCode = newOrderOperating.DeliveryCode,
                        Vehicle = newOrderOperating.Vehicle,
                        TypeProduct = newOrderOperating.TypeProduct,
                        SumNumber = newOrderOperating.SumNumber,
                        NameDistributor = newOrderOperating.NameDistributor,
                        OrderDate = newOrderOperating.OrderDate,
                        LogChange = $"Đơn hàng được tạo lúc {DateTime.Now} ",
                        TimeChange = DateTime.Now
                    };
                    _appDbContext.tblStoreOrderOperatingHistories.Add(newHistory);

                    await _appDbContext.SaveChangesAsync();

                    Console.WriteLine($@"Inserted order orderId={websaleOrder.id} createDate={websaleOrder.createDate} lúc {syncTime}");
                    log.Info($@"Inserted order orderId={websaleOrder.id} createDate={websaleOrder.createDate} lúc {syncTime}");

                    SendOrderHistory(newHistory);

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

                var productId = !string.IsNullOrEmpty(websaleOrder.productId) ? Int32.Parse(websaleOrder.productId) : 0;
                var itemConfig = _appDbContext.tblItemConfigs.FirstOrDefault(x => x.ItemIdSyn == productId);
                typeProduct = itemConfig?.TypeProductCode ?? OrderTypeProductCode.OTHER;

                if (typeProduct == OrderTypeProductCode.JUMBO)
                {
                    typeXK = OrderTypeXKCode.JUMBO;
                }
                else if (typeProduct == OrderTypeProductCode.SLING)
                {
                    typeXK = OrderTypeXKCode.SLING;
                }

                #region Old: Set type product
                //if (itemCategory == OrderCatIdCode.CLINKER)
                //{
                //    typeProduct = OrderTypeProductCode.CLINKER;
                //}
                //else if (itemCategory == OrderCatIdCode.XI_MANG_XA)
                //{
                //    typeProduct = OrderTypeProductCode.ROI;
                //}
                //else
                //{
                //    // Type XK
                //    if (productNameUpper.Contains(OrderTypeXKCode.JUMBO))
                //    {
                //        typeXK = OrderTypeXKCode.JUMBO;
                //        typeProduct = OrderTypeProductCode.JUMBO;
                //    }
                //    else if (productNameUpper.Contains(OrderTypeXKCode.SLING)
                //        || productNameUpper.Contains(OrderTypeXKCode.SILING))
                //    {
                //        typeXK = OrderTypeXKCode.SLING;
                //        typeProduct = OrderTypeProductCode.SLING;
                //    }
                //    else if (productNameUpper.Contains("PCB30") 
                //        || productNameUpper.Contains("PCB 30")
                //        || productNameUpper.Contains("MAX PRO")
                //        )
                //    {
                //        typeProduct = OrderTypeProductCode.PCB30;
                //    }
                //    else if (productNameUpper.Contains("PCB40") 
                //        || productNameUpper.Contains("PCB 40")
                //        || productNameUpper.Contains("PC40")
                //        )
                //    {
                //        typeProduct = OrderTypeProductCode.PCB40;
                //    }
                //    else if (productNameUpper.Contains("C91") 
                //        || productNameUpper.Contains("XÂY TRÁT")
                //        )
                //    {
                //        typeProduct = OrderTypeProductCode.C91;
                //    }
                //    else
                //    {
                //        typeProduct = OrderTypeProductCode.OTHER;
                //    }
                //}
                #endregion

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

                            if (order.Step < (int)OrderStep.DA_CAN_VAO)
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

                            order.DocNum = string.IsNullOrEmpty(websaleOrder.docnum) ? order.DocNum : websaleOrder.docnum;
                            order.RealNumber = (decimal?)websaleOrder.orderQuantity;

                            order.SealCount = !string.IsNullOrEmpty(websaleOrder.topSealCount) ? int.Parse(websaleOrder.topSealCount) : 0;
                            order.SealDes = websaleOrder.topSealDes;
                            order.MoocCode = websaleOrder.moocCode;

                            order.DeliveryCodeTgc = websaleOrder.deliveryCodeTgc;

                            order.UpdateDay = lastUpdatedDate;

                            order.LogProcessOrder = $@"{order.LogProcessOrder} #Sync Update lúc {syncTime}; ";
                            order.LogJobAttach = $@"{order.LogJobAttach} #Sync Update lúc {syncTime}; ";

                            if (double.TryParse(websaleOrder.loadweightnull, out double weightIn))
                            {
                                if (weightIn > 0)
                                {
                                    order.WeightIn = Convert.ToInt32((weightIn * 1000));

                                    if (DateTime.TryParseExact(websaleOrder.timeIn, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                                    {
                                        order.WeightInTime = d;
                                    }
                                }
                            }

                            if (double.TryParse(websaleOrder.loadweightfull, out double weightOut))
                            {
                                if (weightOut > 0)
                                {
                                    order.WeightOut = Convert.ToInt32((weightOut * 1000));

                                    if (DateTime.TryParseExact(websaleOrder.timeOut, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                                    {
                                        order.WeightOutTime = d;
                                    }
                                }
                            }

                            var newHistory = new tblStoreOrderOperatingHistory
                            {
                                DeliveryCode = order.DeliveryCode,
                                Vehicle = order.Vehicle,
                                TypeProduct = order.TypeProduct,
                                SumNumber = order.SumNumber,
                                NameDistributor = order.NameDistributor,
                                OrderDate = order.OrderDate,
                                LogChange = $"Đơn hàng thay đổi lúc {DateTime.Now} ",
                                TimeChange = DateTime.Now
                            };
                            _appDbContext.tblStoreOrderOperatingHistories.Add(newHistory);

                            await _appDbContext.SaveChangesAsync();

                            log.Info($@"Sync Update after orderId={websaleOrder.id} Vehicle={vehicleCode} DriverName={websaleOrder.driverName} CardNo={cardNo} SumNumber={websaleOrder.bookQuantity}");

                            SendOrderHistory(newHistory);

                            isSynced = true;
                        }
                    }
                    else
                    {
                        order = _appDbContext.tblStoreOrderOperatings
                              .FirstOrDefault(x => x.OrderId == websaleOrder.id);

                        if (double.TryParse(websaleOrder.loadweightnull, out double weightIn))
                        {
                            if (weightIn > 0)
                            {
                                order.WeightIn = Convert.ToInt32((weightIn * 1000));

                                if (DateTime.TryParseExact(websaleOrder.timeIn, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                                {
                                    order.WeightInTime = d;
                                }
                            }
                        }

                        if (double.TryParse(websaleOrder.loadweightfull, out double weightOut))
                        {
                            if (weightOut > 0)
                            {
                                order.WeightOut = Convert.ToInt32((weightOut * 1000));

                                if (DateTime.TryParseExact(websaleOrder.timeOut, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                                {
                                    order.WeightOutTime = d;
                                }
                            }
                        }

                        await _appDbContext.SaveChangesAsync();
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

        public async Task<bool> UpdateReceivingOrder(OrderItemResponse websaleOrder)
        {
            bool isSynced = false;

            var syncTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            var weightIn = !string.IsNullOrEmpty(websaleOrder.loadweightnull) ? Double.Parse(websaleOrder.loadweightnull) : 0.0;

            try
            {
                DateTime timeInDate = !string.IsNullOrEmpty(websaleOrder.timeIn) ?
                                       DateTime.ParseExact(websaleOrder.timeIn, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) :
                                       DateTime.MinValue;

                //var order = _appDbContext.tblStoreOrderOperatings
                //            .FirstOrDefault(x => x.OrderId == websaleOrder.id
                //                                &&
                //                                (
                //                                    x.Step < (int)OrderStep.DA_CAN_VAO
                //                                    ||
                //                                    x.Step == (int)OrderStep.DA_XAC_THUC
                //                                    ||
                //                                    x.Step == (int)OrderStep.CHO_GOI_XE
                //                                    ||
                //                                    x.Step == (int)OrderStep.DANG_GOI_XE
                //                                    ||
                //                                    x.WeightIn == null
                //                                    ||
                //                                    x.WeightIn == 0
                //                                )
                //                            );

                var order = _appDbContext.tblStoreOrderOperatings
                            .FirstOrDefault(x => (x.OrderId == websaleOrder.id &&
                                                  x.Step != (int)OrderStep.DA_CAN_VAO &&
                                                  x.Step != (int)OrderStep.DANG_LAY_HANG &&
                                                  x.Step != (int)OrderStep.DA_LAY_HANG) || 
                                                  x.WeightIn == null ||
                                                  x.WeightIn == 0);

                if (order != null)
                {
                    log.Info($@"===== Update Receiving Order {websaleOrder.id} timeIn={timeInDate} lúc {syncTime}: WeightIn {order.WeightInAuto} ==>> {weightIn * 1000}");

                    order.TimeConfirm3 = timeInDate > DateTime.MinValue ? timeInDate : DateTime.Now;
                    order.WeightInTime = timeInDate > DateTime.MinValue ? timeInDate : DateTime.Now;

                    if (order.Step != (int)OrderStep.DA_CAN_VAO && 
                        order.Step != (int)OrderStep.DANG_LAY_HANG && 
                        order.Step != (int)OrderStep.DA_LAY_HANG)
                    {
                        order.Step = (int)OrderStep.DA_CAN_VAO;
                    }

                    order.IndexOrder = 0;
                    order.CountReindex = 0;
                    order.WeightIn = Convert.ToInt32((weightIn * 1000));
                    order.SealCount = !string.IsNullOrEmpty(websaleOrder.topSealCount) ? int.Parse(websaleOrder.topSealCount) : 0;
                    order.SealDes = websaleOrder.topSealDes;
                    order.DocNum = string.IsNullOrEmpty(websaleOrder.docnum) ? order.DocNum : websaleOrder.docnum;
                    order.RealNumber = websaleOrder.orderQuantity;
                    order.MoocCode = websaleOrder.moocCode;
                    order.LogProcessOrder = $@"{order.LogProcessOrder} #Sync Cân vào lúc {syncTime}; ";
                    order.LogJobAttach = $@"{order.LogJobAttach} #Sync Cân vào lúc {syncTime}; ";

                    if (order.TimeConfirm2 == null)
                    {
                        order.TimeConfirm2 = DateTime.Now.AddMinutes(-1);
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Đặt lại time vào cổng lúc {syncTime}; ";
                        order.LogJobAttach = $@"{order.LogJobAttach} #Đặt lại time vào cổng lúc {syncTime}; ";
                    }

                    if (order.WeightOut != null || order.WeightOutTime != null)
                    {
                        order.WeightOut = null;
                        order.WeightOutTime = null;
                    }

                    if (weightIn > 0)
                    {
                        order.WeightIn = Convert.ToInt32((weightIn * 1000));

                        if (DateTime.TryParseExact(websaleOrder.timeIn, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                        {
                            order.WeightInTime = d;
                        }
                    }

                    if (double.TryParse(websaleOrder.loadweightfull, out double weightOut))
                    {
                        if (weightOut > 0)
                        {
                            order.WeightOut = Convert.ToInt32((weightOut * 1000));

                            if (DateTime.TryParseExact(websaleOrder.timeOut, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                            {
                                order.WeightOutTime = d;
                            }
                        }
                    }

                    // Xếp lại lốt
                    var message = $"Đơn hàng số hiệu {order.DeliveryCode} cân vào lúc {order.WeightInTime}";
                    await ReindexOrder(order.TypeProduct, message);

                    //var newHistory = new tblStoreOrderOperatingHistory
                    //{
                    //    DeliveryCode = order.DeliveryCode,
                    //    Vehicle = order.Vehicle,
                    //    TypeProduct = order.TypeProduct,
                    //    SumNumber = order.SumNumber,
                    //    NameDistributor = order.NameDistributor,
                    //    OrderDate = order.OrderDate,
                    //    LogChange = $"Đơn hàng cân vào lúc {DateTime.Now} ",
                    //    TimeChange = DateTime.Now
                    //};
                    //_appDbContext.tblStoreOrderOperatingHistories.Add(newHistory);

                    await _appDbContext.SaveChangesAsync();

                    Console.WriteLine($@"Update Receiving Order {websaleOrder.id}");
                    log.Info($@"Update Receiving Order {websaleOrder.id}");

                    //SendOrderHistory(newHistory);

                    isSynced = true;
                }
                else
                {
                    order = _appDbContext.tblStoreOrderOperatings
                          .FirstOrDefault(x => x.OrderId == websaleOrder.id);

                    if (weightIn > 0)
                    {
                        order.WeightIn = Convert.ToInt32((weightIn * 1000));

                        if (DateTime.TryParseExact(websaleOrder.timeIn, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                        {
                            order.WeightInTime = d;
                        }
                    }

                    if (double.TryParse(websaleOrder.loadweightfull, out double weightOut))
                    {
                        if (weightOut > 0)
                        {
                            order.WeightOut = Convert.ToInt32((weightOut * 1000));

                            if (DateTime.TryParseExact(websaleOrder.timeOut, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                            {
                                order.WeightOutTime = d;
                            }
                        }
                    }

                    await _appDbContext.SaveChangesAsync();
                }

                return isSynced;
            }
            catch (Exception ex)
            {
                log.Error($@"=========================== Update Receiving Order {websaleOrder.id} Error: " + ex.Message + " ====== " + ex.StackTrace + "==============" + ex.InnerException);
                Console.WriteLine($@"Update Receiving Order {websaleOrder.id} Error: " + ex.Message);

                return isSynced;
            }
        }

        public async Task<bool> UpdateReceivedOrder(OrderItemResponse websaleOrder)
        {
            bool isSynced = false;

            var syncTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            var weightOut = !string.IsNullOrEmpty(websaleOrder.loadweightfull) ? Double.Parse(websaleOrder.loadweightfull) : 0.0;

            try
            {
                DateTime timeOutDate = !string.IsNullOrEmpty(websaleOrder.timeOut) ?
                                        DateTime.ParseExact(websaleOrder.timeOut, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) :
                                        DateTime.MinValue;

                // TODO: nếu thời gian cân ra > hiện tại 1 tiếng thì step = DA_HOAN_THANH
                if (timeOutDate > DateTime.Now.AddMinutes(-30))
                {
                    var order = _appDbContext.tblStoreOrderOperatings
                                .FirstOrDefault(x => x.OrderId == websaleOrder.id
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
                                                    //||
                                                    //x.DocNum == null
                                                    )
                                               );

                    if (order != null)
                    {
                        log.Info($@"===== Update Received Order {websaleOrder.id} timeOut={timeOutDate} lúc {syncTime}: WeightOut {order.WeightOutAuto} ==>> {weightOut * 1000}");

                        order.TimeConfirm7 = timeOutDate > DateTime.MinValue ? timeOutDate : DateTime.Now;
                        order.WeightOutTime = timeOutDate > DateTime.MinValue ? timeOutDate : DateTime.Now;

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
                        order.WeightOut = Convert.ToInt32((weightOut * 1000));
                        order.SealCount = !string.IsNullOrEmpty(websaleOrder.topSealCount) ? int.Parse(websaleOrder.topSealCount) : 0;
                        order.SealDes = websaleOrder.topSealDes;
                        order.DocNum = string.IsNullOrEmpty(websaleOrder.docnum) ? order.DocNum : websaleOrder.docnum;
                        order.RealNumber = websaleOrder.orderQuantity;
                        order.MoocCode = websaleOrder.moocCode;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Sync Cân ra lúc {syncTime} ";
                        order.LogJobAttach = $@"{order.LogJobAttach} #Sync Cân ra lúc {syncTime}; ";

                        if (double.TryParse(websaleOrder.loadweightnull, out double weightIn))
                        {
                            if (weightIn > 0)
                            {
                                order.WeightIn = Convert.ToInt32((weightIn * 1000));

                                if (DateTime.TryParseExact(websaleOrder.timeIn, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                                {
                                    order.WeightInTime = d;
                                }
                            }
                        }

                        if (weightOut > 0)
                        {
                            order.WeightOut = Convert.ToInt32((weightOut * 1000));

                            if (DateTime.TryParseExact(websaleOrder.timeOut, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                            {
                                order.WeightOutTime = d;
                            }
                        }

                        //var newHistory = new tblStoreOrderOperatingHistory
                        //{
                        //    DeliveryCode = order.DeliveryCode,
                        //    Vehicle = order.Vehicle,
                        //    TypeProduct = order.TypeProduct,
                        //    SumNumber = order.SumNumber,
                        //    NameDistributor = order.NameDistributor,
                        //    OrderDate = order.OrderDate,
                        //    LogChange = $"Đơn hàng cân ra lúc {DateTime.Now} ",
                        //    TimeChange = DateTime.Now
                        //};
                        //_appDbContext.tblStoreOrderOperatingHistories.Add(newHistory);

                        await _appDbContext.SaveChangesAsync();

                        Console.WriteLine($@"Sync Update Received => DA_CAN_RA Order {websaleOrder.id}");
                        log.Info($@"Sync Update Received => DA_CAN_RA Order {websaleOrder.id}");

                        //SendOrderHistory(newHistory);

                        isSynced = true;
                    }
                }
                else if (timeOutDate > DateTime.Now.AddMinutes(-60))
                {
                    var order = _appDbContext.tblStoreOrderOperatings
                                .FirstOrDefault(x => x.OrderId == websaleOrder.id
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
                        order.SealCount = !string.IsNullOrEmpty(websaleOrder.topSealCount) ? int.Parse(websaleOrder.topSealCount) : 0;
                        order.SealDes = websaleOrder.topSealDes;
                        order.DocNum = string.IsNullOrEmpty(websaleOrder.docnum) ? order.DocNum : websaleOrder.docnum;
                        order.RealNumber = websaleOrder.orderQuantity;
                        order.MoocCode = websaleOrder.moocCode;

                        if (double.TryParse(websaleOrder.loadweightnull, out double weightIn))
                        {
                            if (weightIn > 0)
                            {
                                order.WeightIn = Convert.ToInt32((weightIn * 1000));

                                if (DateTime.TryParseExact(websaleOrder.timeIn, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                                {
                                    order.WeightInTime = d;
                                }
                            }
                        }

                        if (weightOut > 0)
                        {
                            order.WeightOut = Convert.ToInt32((weightOut * 1000));

                            if (DateTime.TryParseExact(websaleOrder.timeOut, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                            {
                                order.WeightOutTime = d;
                            }
                        }

                        //var newHistory = new tblStoreOrderOperatingHistory
                        //{
                        //    DeliveryCode = order.DeliveryCode,
                        //    Vehicle = order.Vehicle,
                        //    TypeProduct = order.TypeProduct,
                        //    SumNumber = order.SumNumber,
                        //    NameDistributor = order.NameDistributor,
                        //    OrderDate = order.OrderDate,
                        //    LogChange = $"Đơn hàng ra cổng lúc {DateTime.Now} ",
                        //    TimeChange = DateTime.Now
                        //};
                        //_appDbContext.tblStoreOrderOperatingHistories.Add(newHistory);

                        await _appDbContext.SaveChangesAsync();

                        Console.WriteLine($@"Sync Update Received => DA_HOAN_THANH Order {websaleOrder.id}");
                        log.Info($@"Sync Update Received => DA_HOAN_THANH Order {websaleOrder.id}");

                        //SendOrderHistory(newHistory);

                        isSynced = true;
                    }
                }
                else
                {
                    var order = _appDbContext.tblStoreOrderOperatings
                                .FirstOrDefault(x => x.OrderId == websaleOrder.id
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
                        order.SealCount = !string.IsNullOrEmpty(websaleOrder.topSealCount) ? int.Parse(websaleOrder.topSealCount) : 0;
                        order.SealDes = websaleOrder.topSealDes;
                        order.DocNum = string.IsNullOrEmpty(websaleOrder.docnum) ? order.DocNum : websaleOrder.docnum;
                        order.RealNumber = websaleOrder.orderQuantity;
                        order.MoocCode = websaleOrder.moocCode;

                        if (double.TryParse(websaleOrder.loadweightnull, out double weightIn))
                        {
                            if (weightIn > 0)
                            {
                                order.WeightIn = Convert.ToInt32((weightIn * 1000));

                                if (DateTime.TryParseExact(websaleOrder.timeIn, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                                {
                                    order.WeightInTime = d;
                                }
                            }
                        }

                        if (weightOut > 0)
                        {
                            order.WeightOut = Convert.ToInt32((weightOut * 1000));

                            if (DateTime.TryParseExact(websaleOrder.timeOut, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                            {
                                order.WeightOutTime = d;
                            }
                        }

                        //var newHistory = new tblStoreOrderOperatingHistory
                        //{
                        //    DeliveryCode = order.DeliveryCode,
                        //    Vehicle = order.Vehicle,
                        //    TypeProduct = order.TypeProduct,
                        //    SumNumber = order.SumNumber,
                        //    NameDistributor = order.NameDistributor,
                        //    OrderDate = order.OrderDate,
                        //    LogChange = $"Đơn hàng được giao lúc {DateTime.Now} ",
                        //    TimeChange = DateTime.Now
                        //};
                        //_appDbContext.tblStoreOrderOperatingHistories.Add(newHistory);

                        await _appDbContext.SaveChangesAsync();

                        Console.WriteLine($@"Update Received => DA_GIAO_HANG Order {websaleOrder.id}");
                        log.Info($@"Update Received => DA_GIAO_HANG Order {websaleOrder.id}");

                        //SendOrderHistory(newHistory);

                        isSynced = true;
                    }

                    else
                    {
                        order = _appDbContext.tblStoreOrderOperatings
                              .FirstOrDefault(x => x.OrderId == websaleOrder.id);

                        if (double.TryParse(websaleOrder.loadweightnull, out double weightIn))
                        {
                            if (weightIn > 0)
                            {
                                order.WeightIn = Convert.ToInt32((weightIn * 1000));

                                if (DateTime.TryParseExact(websaleOrder.timeIn, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                                {
                                    order.WeightInTime = d;
                                }
                            }
                        }

                        if (weightOut > 0)
                        {
                            order.WeightOut = Convert.ToInt32((weightOut * 1000));

                            if (DateTime.TryParseExact(websaleOrder.timeOut, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                            {
                                order.WeightOutTime = d;
                            }
                        }

                        await _appDbContext.SaveChangesAsync();
                    }
                }

                return isSynced;
            }
            catch (Exception ex)
            {
                log.Error($@"=========================== Update Received Order {websaleOrder.id} Error: " + ex.Message + " ============ " + ex.StackTrace + " ==== " + ex.InnerException);
                Console.WriteLine($@"Update Received Order {websaleOrder.id} Error: " + ex.Message);

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
                                                                //&& x.Step != (int)OrderStep.DA_HOAN_THANH
                                                                //&& x.Step != (int)OrderStep.DA_GIAO_HANG
                                                                );
                if (order != null)
                {
                    order.IsVoiced = true;
                    order.LogJobAttach = $@"{order.LogJobAttach} #Sync Hủy đơn lúc {syncTime} ";
                    order.LogProcessOrder = $@"{order.LogProcessOrder} #Sync Hủy đơn lúc {syncTime} ";

                    var newHistory = new tblStoreOrderOperatingHistory
                    {
                        DeliveryCode = order.DeliveryCode,
                        Vehicle = order.Vehicle,
                        TypeProduct = order.TypeProduct,
                        SumNumber = order.SumNumber,
                        NameDistributor = order.NameDistributor,
                        OrderDate = order.OrderDate,
                        LogChange = $"Đơn hàng bị hủy lúc {DateTime.Now} ",
                        TimeChange = DateTime.Now
                    };
                    _appDbContext.tblStoreOrderOperatingHistories.Add(newHistory);

                    await _appDbContext.SaveChangesAsync();

                    Console.WriteLine($@"Cancel Order {orderId}");
                    log.Info($@"Cancel Order {orderId}");

                    SendOrderHistory(newHistory);

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

        public static IRestResponse SendOrderHistory(tblStoreOrderOperatingHistory orderHistory)
        {
            var apiUrl = ConfigurationManager.GetSection("API_DMS/Url") as NameValueCollection;

            var client = new RestClient(apiUrl["SendOrderHistory"]);
            var request = new RestRequest();

            var requestData = new
            {
                DeliveryCode = orderHistory.DeliveryCode,
                Vehicle = orderHistory.Vehicle,
                TypeProduct = orderHistory.TypeProduct,
                SumNumber = orderHistory.SumNumber,
                NameDistributor = orderHistory.NameDistributor,
                OrderDate = orderHistory.OrderDate,
                LogChange = orderHistory.LogChange,
                TimeChange = orderHistory.TimeChange
            };

            request.Method = Method.POST;
            request.AddJsonBody(requestData);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");
            request.RequestFormat = DataFormat.Json;

            IRestResponse response = client.Execute(request);

            return response;
        }
    }
}
