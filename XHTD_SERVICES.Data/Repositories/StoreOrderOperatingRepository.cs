using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Models.Response;
using log4net;

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

        public async Task CreateAsync(OrderItemResponse websaleOrder)
        {
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
                        Step = 0,
                        IsVoiced = false,
                        LogJobAttach = $@"#Sync Order",
                        IsSyncedByNewWS = true
                    };

                    _appDbContext.tblStoreOrderOperatings.Add(newOrderOperating);
                    await _appDbContext.SaveChangesAsync();

                    Console.WriteLine($@"Inserted order {websaleOrder.id}");
                    log.Info($@"Inserted order {websaleOrder.id}");
                }
            }
            catch(Exception ex)
            {
                log.Error("CreateAsync OrderItemResponse Error: " + ex.Message); ;
                Console.WriteLine("CreateAsync OrderItemResponse Error: " + ex.Message);
            }
        }

        public async Task CancelOrder(int? orderId)
        {
            try
            {
                string calcelTime = DateTime.Now.ToString();

                var order = _appDbContext.tblStoreOrderOperatings.FirstOrDefault(x => x.OrderId == orderId && x.IsVoiced != true && (x.Step != 8 && x.Step != 9));
                if (order != null)
                {
                    order.IsVoiced = true;
                    order.LogJobAttach = $@"{order.LogJobAttach} #Hủy đơn lúc {calcelTime} ";
                    order.LogProcessOrder = $@"{order.LogProcessOrder} #Hủy đơn lúc {calcelTime} ";

                    await _appDbContext.SaveChangesAsync();

                    Console.WriteLine($@"Cancel Order {orderId}");
                    log.Info($@"Cancel Order {orderId}");
                }
            }
            catch (Exception ex)
            {
                log.Error($@"Cancel Order {orderId} Error: " + ex.Message);
                Console.WriteLine($@"Cancel Order {orderId} Error: " + ex.Message);
            }
        }
    }
}
