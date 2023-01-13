using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Models.Response;
using XHTD_SERVICES.Data.Models.Values;

namespace XHTD_SERVICES.Data.Repositories
{
    public partial class StoreOrderOperatingRepository
    {
        public async Task<bool> CreateAsync(OrderItemResponse websaleOrder)
        {
            bool isSynced = false;

            try
            {
                string typeProduct = "";
                string productNameUpper = websaleOrder.productName.ToUpper();
                string itemCategory = websaleOrder.itemCategory;

                if (itemCategory == "XI_MANG_XA")
                {
                    typeProduct = "ROI";
                }
                else if (itemCategory == "CLINKER")
                {
                    typeProduct = "CLINKER";
                }
                else
                {
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
                }

                var vehicleCode = websaleOrder.vehicleCode.Replace("-", "").Replace("  ", "").Replace(" ", "").Replace("/", "").Replace(".", "").ToUpper();
                var rfidItem = _appDbContext.tblRfids.FirstOrDefault(x => x.Vehicle.Contains(vehicleCode));
                var cardNo = rfidItem?.Code ?? null;

                var orderDateString = websaleOrder?.orderDate;

                DateTime orderDate = DateTime.ParseExact(orderDateString, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

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
                        State = websaleOrder.status,
                        IndexOrder = 0,
                        IndexOrder2 = 0,
                        CountReindex = 0,
                        Step = (int)OrderStep.CHUA_NHAN_DON,
                        IsVoiced = false,
                        LogJobAttach = $@"#Sync Order",
                        IsSyncedByNewWS = true
                    };

                    _appDbContext.tblStoreOrderOperatings.Add(newOrderOperating);
                    await _appDbContext.SaveChangesAsync();

                    Console.WriteLine($@"Inserted order {websaleOrder.id}");
                    log.Info($@"Inserted order {websaleOrder.id}");

                    isSynced = true;
                }

                return isSynced;
            }
            catch (Exception ex)
            {
                log.Error("CreateAsync OrderItemResponse Error: " + ex.Message); ;
                Console.WriteLine("CreateAsync OrderItemResponse Error: " + ex.Message);

                return isSynced;
            }
        }

        public async Task<bool> UpdateReceivingOrder(int? orderId, string timeIn)
        {
            bool isSynced = false;

            try
            {
                DateTime timeInDate = DateTime.ParseExact(timeIn, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

                var order = _appDbContext.tblStoreOrderOperatings
                            .FirstOrDefault(x => x.OrderId == orderId
                                                && x.Step < (int)OrderStep.DA_CAN_VAO);
                if (order != null)
                {
                    order.Confirm2 = 1;
                    order.TimeConfirm2 = order.TimeConfirm2 ?? DateTime.Now;
                    order.Confirm3 = 1;
                    order.TimeConfirm3 = timeInDate;
                    order.Step = (int)OrderStep.DA_CAN_VAO;
                    order.IndexOrder = 0;
                    order.CountReindex = 0;
                    order.LogProcessOrder = $@"{order.LogProcessOrder} #Đã cân vào lúc {timeIn}; ";
                    order.LogJobAttach = $@"{order.LogJobAttach} #Đã cân vào lúc {timeIn}; ";

                    await _appDbContext.SaveChangesAsync();

                    Console.WriteLine($@"Update Receiving Order {orderId}");
                    log.Info($@"Update Receiving Order {orderId}");

                    isSynced = true;
                }

                return isSynced;
            }
            catch (Exception ex)
            {
                log.Error($@"Update Receiving Order {orderId} Error: " + ex.Message);
                Console.WriteLine($@"Update Receiving Order {orderId} Error: " + ex.Message);

                return isSynced;
            }
        }

        public async Task<bool> UpdateReceivedOrder(int? orderId, string timeOut)
        {
            bool isSynced = false;

            try
            {
                DateTime timeOutDate = DateTime.ParseExact(timeOut, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

                // TODO: nếu thời gian cân ra > hiện tại 1 tiếng thì step = DA_HOAN_THANH
                if(timeOutDate > DateTime.Now.AddMinutes(-90)) { 
                    var order = _appDbContext.tblStoreOrderOperatings
                                .FirstOrDefault(x => x.OrderId == orderId
                                                    && x.Step < (int)OrderStep.DA_CAN_RA);
                    if (order != null)
                    {
                        order.Confirm7 = 1;
                        order.TimeConfirm7 = timeOutDate;
                        order.Step = (int)OrderStep.DA_CAN_RA;
                        order.IndexOrder = 0;
                        order.CountReindex = 0;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Cân ra lúc {timeOut} ";
                        order.LogJobAttach = $@"{order.LogJobAttach} #Cân ra lúc {timeOut}; ";

                        await _appDbContext.SaveChangesAsync();

                        Console.WriteLine($@"Update Received => DA_CAN_RA Order {orderId}");
                        log.Info($@"Update Received => DA_CAN_RA Order {orderId}");

                        isSynced = true;
                    }
                }
                else
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
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Sync Ra cổng lúc {timeOut} ";
                        order.LogJobAttach = $@"{order.LogJobAttach} #Sync Ra cổng lúc {timeOut}; ";

                        await _appDbContext.SaveChangesAsync();

                        Console.WriteLine($@"Update Received => DA_HOAN_THANH Order {orderId}");
                        log.Info($@"Update Received => DA_HOAN_THANH Order {orderId}");

                        isSynced = true;
                    }
                }

                return isSynced;
            }
            catch (Exception ex)
            {
                log.Error($@"Update Received Order {orderId} Error: " + ex.Message);
                Console.WriteLine($@"Update Received Order {orderId} Error: " + ex.Message);

                return isSynced;
            }
        }

        public async Task<bool> CancelOrder(int? orderId)
        {
            bool isSynced = false;

            try
            {
                string cancelTime = DateTime.Now.ToString();

                var order = _appDbContext.tblStoreOrderOperatings.FirstOrDefault(x => x.OrderId == orderId && x.IsVoiced != true && (x.Step != (int)OrderStep.DA_HOAN_THANH && x.Step != (int)OrderStep.DA_GIAO_HANG));
                if (order != null)
                {
                    order.IsVoiced = true;
                    order.LogJobAttach = $@"{order.LogJobAttach} #Hủy đơn lúc {cancelTime} ";
                    order.LogProcessOrder = $@"{order.LogProcessOrder} #Hủy đơn lúc {cancelTime} ";

                    await _appDbContext.SaveChangesAsync();

                    Console.WriteLine($@"Cancel Order {orderId}");
                    log.Info($@"Cancel Order {orderId}");

                    isSynced = true;
                }

                return isSynced;
            }
            catch (Exception ex)
            {
                log.Error($@"Cancel Order {orderId} Error: " + ex.Message);
                Console.WriteLine($@"Cancel Order {orderId} Error: " + ex.Message);

                return isSynced;
            }
        }
    }
}
