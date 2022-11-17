using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Models.Response;
using log4net;
using System.Data.Entity;
using XHTD_SERVICES.Data.Models.Values;

namespace XHTD_SERVICES.Data.Repositories
{
    public class StoreOrderOperatingRepository : BaseRepository <tblStoreOrderOperating>
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public StoreOrderOperatingRepository(XHTD_Entities appDbContext) : base(appDbContext)
        {
        }

        public bool CheckExist(int? orderId)
        {
            var orderExist = _appDbContext.tblStoreOrderOperatings.FirstOrDefault(x => x.OrderId == orderId);
            if (orderExist != null)
            {
                return true;
            }
            return false;
        }

        public async Task<tblStoreOrderOperating> GetDetail(int orderId)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var order = await dbContext.tblStoreOrderOperatings.FirstOrDefaultAsync(x => x.Id == orderId);

                return order;
            }
        }

        public async Task<bool> CreateAsync(OrderItemResponse websaleOrder)
        {
            bool isSynced = false;

            try {
                string typeProduct = "";
                string productNameUpper = websaleOrder.productName.ToUpper();

                if (productNameUpper.Contains("RỜI"))
                {
                    typeProduct = "ROI";
                }
                else if (productNameUpper.Contains("PCB30") || productNameUpper.Contains("MAX PRO"))
                {
                    typeProduct = "PCB30";
                }
                else if (productNameUpper.Contains("PCB40"))
                {
                    typeProduct = "PCB40";
                }
                else if (productNameUpper.Contains("CLINKER"))
                {
                    typeProduct = "CLINKER";
                }

                var vehicleCode = websaleOrder.vehicleCode.Replace("-", "").Replace("  ", "").Replace(" ", "").Replace("/", "").Replace(".", "").ToUpper();
                var rfidItem = _appDbContext.tblRfids.FirstOrDefault(x => x.Vehicle.Contains(vehicleCode));
                var cardNo = rfidItem?.Code ?? null;

                var orderDateString = websaleOrder?.orderDate;

                DateTime orderDate = DateTime.ParseExact(orderDateString, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

                if (!CheckExist(websaleOrder.id)) {
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
            catch(Exception ex)
            {
                log.Error("CreateAsync OrderItemResponse Error: " + ex.Message); ;
                Console.WriteLine("CreateAsync OrderItemResponse Error: " + ex.Message);

                return isSynced;
            }
        }

        public async Task<bool> CancelOrder(int? orderId)
        {
            bool isSynced = false;

            try
            {
                string calcelTime = DateTime.Now.ToString();

                var order = _appDbContext.tblStoreOrderOperatings.FirstOrDefault(x => x.OrderId == orderId && x.IsVoiced != true && (x.Step != (int)OrderStep.DA_HOAN_THANH && x.Step != (int)OrderStep.DA_GIAO_HANG));
                if (order != null)
                {
                    order.IsVoiced = true;
                    order.LogJobAttach = $@"{order.LogJobAttach} #Hủy đơn lúc {calcelTime} ";
                    order.LogProcessOrder = $@"{order.LogProcessOrder} #Hủy đơn lúc {calcelTime} ";

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

        public async Task<bool> UpdateTroughLine(string deliveryCode, string throughCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                bool isUpdated = false;

                try
                {
                    var order = dbContext.tblStoreOrderOperatings.FirstOrDefault(x => x.DeliveryCode == deliveryCode);
                    if (order != null)
                    {
                        order.TroughLineCode = throughCode;

                        await dbContext.SaveChangesAsync();

                        isUpdated = true;
                    }

                    return isUpdated;
                }
                catch (Exception ex)
                {
                    log.Error($@"UpdateLineTrough Error: " + ex.Message);
                    Console.WriteLine($@"UpdateLineTrough Error: " + ex.Message);

                    return isUpdated;
                }
            }
        }

        public async Task<bool> UpdateLogProcess(string deliveryCode, string logProcess)
        {
            using (var dbContext = new XHTD_Entities())
            {
                bool isUpdated = false;

                try
                {
                    var order = dbContext.tblStoreOrderOperatings.FirstOrDefault(x => x.DeliveryCode == deliveryCode);
                    if (order != null)
                    {
                        order.LogProcessOrder = order.LogProcessOrder + $@" {logProcess}";

                        await dbContext.SaveChangesAsync();

                        isUpdated = true;
                    }

                    return isUpdated;
                }
                catch (Exception ex)
                {
                    log.Error($@"UpdateLineTrough Error: " + ex.Message);
                    Console.WriteLine($@"UpdateLineTrough Error: " + ex.Message);

                    return isUpdated;
                }
            }
        }

        public async Task<bool> UpdateStepDangGoiXe(string deliveryCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                bool isUpdated = false;

                try
                {
                    var order = dbContext.tblStoreOrderOperatings.FirstOrDefault(x => x.DeliveryCode == deliveryCode);
                    if (order != null)
                    {
                        order.Step = (int)OrderStep.DANG_GOI_XE;
                        order.Confirm4 = 1;
                        order.TimeConfirm4 = DateTime.Now;
                        order.LogProcessOrder = order.LogProcessOrder + $@" #Đưa vào hàng đợi mời xe vào lúc {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} ";

                        await dbContext.SaveChangesAsync();

                        isUpdated = true;
                    }

                    return isUpdated;
                }
                catch (Exception ex)
                {
                    log.Error($@"UpdateStepDangGoiXe Error: " + ex.Message);
                    Console.WriteLine($@"UpdateStepDangGoiXe Error: " + ex.Message);

                    return isUpdated;
                }
            }
        }

        public async Task<bool> UpdateStepInTrough(string deliveryCode, int step)
        {
            using (var dbContext = new XHTD_Entities())
            {
                bool isUpdated = false;

                try
                {
                    var order = dbContext.tblStoreOrderOperatings.FirstOrDefault(x => x.DeliveryCode == deliveryCode);
                    if (order == null)
                    {
                        return false;
                    }

                    if(step == (int)OrderStep.DA_LAY_HANG)
                    {
                        if(order.Step == (int)OrderStep.DA_LAY_HANG)
                        {
                            return true;
                        }

                        order.Confirm6 = 1;
                        order.TimeConfirm6 = DateTime.Now;
                        order.LogProcessOrder = order.LogProcessOrder + $@" #xuất hàng xong lúc {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} ";
                    }
                    else if (step == (int)OrderStep.DANG_LAY_HANG)
                    {
                        if (order.Step == (int)OrderStep.DANG_LAY_HANG)
                        {
                            return true;
                        }

                        order.LogProcessOrder = order.LogProcessOrder + $@" #xuất hàng lúc {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} ";
                    }

                    order.IndexOrder = 0;
                    order.Confirm1 = 1;
                    order.Confirm2 = 1;
                    order.Confirm3 = 1;
                    order.Confirm4 = 1;
                    order.Confirm5 = 1;

                    order.Step = step;

                    await dbContext.SaveChangesAsync();

                    isUpdated = true;

                    return isUpdated;
                }
                catch (Exception ex)
                {
                    log.Error($@"UpdateStepInTrough Error: " + ex.Message);
                    Console.WriteLine($@"UpdateStepInTrough Error: " + ex.Message);

                    return isUpdated;
                }
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetOrdersSortByIndex(int quantity)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings.Where(x => x.Step == (int)OrderStep.DA_CAN_VAO && (x.DriverUserName ?? "") != "").OrderBy(x => x.IndexOrder).Take(quantity).ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetCurrentOrdersEntraceGatewayByCardNoReceiving(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings.Where(x => x.CardNo == cardNo && (x.DriverUserName ?? "") != "" && x.Step < (int)OrderStep.DA_VAO_CONG).ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetCurrentOrdersExitGatewayByCardNoReceiving(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings.Where(x => x.CardNo == cardNo && (x.DriverUserName ?? "") != "" && x.Step == (int)OrderStep.DA_CAN_RA).ToListAsync();
                return orders;
            }
        }

        public async Task<bool> UpdateOrderEntraceGateway(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string calcelTime = DateTime.Now.ToString();

                    var orders = await dbContext.tblStoreOrderOperatings.Where(x => x.CardNo == cardNo && x.Step < (int)OrderStep.DA_VAO_CONG).ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.Confirm1 = 1;
                        order.Confirm2 = 1;
                        order.TimeConfirm2 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_VAO_CONG;
                        order.IndexOrder = 0;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Xác thực vào cổng lúc {calcelTime} ";
                    }

                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($@"Xác thực vào cổng {cardNo} Error: " + ex.Message);
                    Console.WriteLine($@"Xác thực vào cổng {cardNo} Error: " + ex.Message);
                    return false;
                }
            }
        }

        public async Task<bool> UpdateOrderExitGateway(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string calcelTime = DateTime.Now.ToString();

                    var orders = await dbContext.tblStoreOrderOperatings.Where(x => x.CardNo == cardNo && x.Step == (int)OrderStep.DA_CAN_RA).ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.Confirm8 = 1;
                        order.TimeConfirm8 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_HOAN_THANH;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Xác thực ra cổng lúc {calcelTime} ";

                        Console.WriteLine($@"Xác thực ra cổng {cardNo}");
                        log.Info($@"Xác thực ra cổng {cardNo}");
                    }

                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($@"Xác thực ra cổng {cardNo} Error: " + ex.Message);
                    Console.WriteLine($@"Xác thực ra cổng {cardNo} Error: " + ex.Message);
                    return false;
                }
            }
        }

        public async Task<bool> UpdateOrderEntraceTram951(string cardNo, int weightIn)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string calcelTime = DateTime.Now.ToString();

                    var orders = await dbContext.tblStoreOrderOperatings.Where(x => x.CardNo == cardNo && x.Step == (int)OrderStep.DA_VAO_CONG).ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.Step = (int)OrderStep.DA_CAN_VAO;
                        order.WeightIn = weightIn;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Đã cân vào lúc {calcelTime} ";
                    }

                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($@"Cân vào {cardNo} Error: " + ex.Message);
                    Console.WriteLine($@"Cân vào {cardNo} Error: " + ex.Message);
                    return false;
                }
            }
        }

        public async Task<bool> UpdateOrderExitTram951(string cardNo, int weightOut)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string calcelTime = DateTime.Now.ToString();

                    var orders = await dbContext.tblStoreOrderOperatings.Where(x => x.CardNo == cardNo && x.Step == (int)OrderStep.DA_LAY_HANG).ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.Step = (int)OrderStep.DA_CAN_RA;
                        order.WeightOut = weightOut;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Đã cân ra lúc {calcelTime} ";

                        Console.WriteLine($@"Cân ra {cardNo}");
                        log.Info($@"Cân ra {cardNo}");
                    }

                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($@"Cân ra {cardNo} Error: " + ex.Message);
                    Console.WriteLine($@"Cân ra {cardNo} Error: " + ex.Message);
                    return false;
                }
            }
        }
    }
}
