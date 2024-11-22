using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Models.Response;
using log4net;
using System.Data.Entity;
using XHTD_SERVICES.Data.Models.Values;
using XHTD_SERVICES.Data.Common;
using System.Web.Configuration;
using System.Collections.Specialized;
using System.Configuration;
using RestSharp;

namespace XHTD_SERVICES.Data.Repositories
{
    public partial class StoreOrderOperatingRepository
    {
        public async Task<bool> UpdateTroughLine(string deliveryCode, string throughCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                bool isUpdated = false;

                try
                {
                    var order = dbContext.tblStoreOrderOperatings.FirstOrDefault(x => x.DeliveryCode == deliveryCode && x.TroughLineCode != throughCode);
                    if (order != null)
                    {
                        order.TroughLineCode = throughCode;

                        await dbContext.SaveChangesAsync();

                        isUpdated = true;

                        log.Info($@"Update Trough Line {throughCode} cho deliveryCode {deliveryCode} trong bang orderOperatings");
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
                        //order.CountReindex = 0;
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

                var syncTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                try
                {
                    var order = dbContext.tblStoreOrderOperatings.FirstOrDefault(x => x.DeliveryCode == deliveryCode && x.Step < (int)OrderStep.DA_CAN_RA);
                    if (order == null)
                    {
                        return false;
                    }

                    if(step == (int)OrderStep.DA_LAY_HANG)
                    {
                        if(order.Step >= (int)OrderStep.DA_LAY_HANG)
                        {
                            return true;
                        }

                        order.TimeConfirm6 = DateTime.Now;
                        order.LogProcessOrder = order.LogProcessOrder + $@" #Xuất hàng xong lúc {syncTime};";
                    }
                    else if (step == (int)OrderStep.DANG_LAY_HANG)
                    {
                        if (order.Step == (int)OrderStep.DANG_LAY_HANG)
                        {
                            return true;
                        }
                    }

                    order.Step = step;

                    await dbContext.SaveChangesAsync();

                    isUpdated = true;

                    log.Info($@"Cap nhat trang thai don hang deliveryCode {deliveryCode} step {step} thanh cong");

                    return isUpdated;
                }
                catch (Exception ex)
                {
                    log.Error($@"================== UpdateStepInTrough Error: " + ex.Message);
                    Console.WriteLine($@"UpdateStepInTrough Error: " + ex.Message);

                    return isUpdated;
                }
            }
        }

        public async Task<bool> UpdateIndex(int orderId, int index)
        {
            using (var dbContext = new XHTD_Entities())
            {
                bool isUpdated = false;

                try
                {
                    var order = dbContext.tblStoreOrderOperatings.FirstOrDefault(x => x.Id == orderId);
                    if (order != null)
                    {
                        order.IndexOrder = index;

                        await dbContext.SaveChangesAsync();

                        log.Error($"Update Index:  orderId={orderId}, index={index}");
                        Console.WriteLine($"Update Index: orderId={orderId}, index={index}");

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

        public List<string> GetCurrentOrdersToCallInTrough()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var record = dbContext.tblCallToTroughs.Where(x => x.IsDone == false).Select(x => x.DeliveryCode);
                return record.ToList();
            }
        }

        public async Task<List<OrderToCallInTroughResponse>> GetOrdersToCallInTrough(string troughCode, int quantity)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var currentCallInTroughs = GetCurrentOrdersToCallInTrough();

                var timeToCall = DateTime.Now.AddMinutes(-2);

                var query = from v in dbContext.tblStoreOrderOperatings 
                            join r in dbContext.tblTroughTypeProducts 
                            on v.TypeProduct equals r.TypeProduct
                            where 
                                v.Step == (int)OrderStep.DA_CAN_VAO
                                && v.IsVoiced == false
                                && v.IndexOrder > 0
                                && !currentCallInTroughs.Contains(v.DeliveryCode)
                                && v.TimeConfirm3 < timeToCall
                                && r.TroughCode == troughCode
                            orderby v.IndexOrder
                            select new OrderToCallInTroughResponse
                            {
                                Id = v.Id,
                                DeliveryCode = v.DeliveryCode,
                                Vehicle = v.Vehicle,
                            };

                query = query.Take(quantity);

                var data = await query.ToListAsync();

                return data;
            }
        }

        public async Task<bool> UpdateWhenOverCountReindex(int orderId)
        {
            using (var dbContext = new XHTD_Entities())
            {
                bool isUpdated = false;

                try
                {
                    var itemToCall = await dbContext.tblStoreOrderOperatings.FirstOrDefaultAsync(x => x.Id == orderId);
                    if (itemToCall != null)
                    {
                        itemToCall.IndexOrder = 0;
                        itemToCall.Step = (int)OrderStep.DA_VAO_CONG;
                        itemToCall.LogProcessOrder = $@"{itemToCall.LogProcessOrder} # Quá 3 lần xoay vòng lốt mà xe không vào, hủy lốt lúc {DateTime.Now}";

                        await dbContext.SaveChangesAsync();

                        isUpdated = true;
                    }

                    return isUpdated;
                }
                catch (Exception ex)
                {
                    log.Error($@"UpdateWhenOverCountTry Error: " + ex.Message);
                    Console.WriteLine($@"UpdateWhenOverCountTry Error: " + ex.Message);

                    return isUpdated;
                }
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetOrdersOverCountReindex(int maxCountReindex = 3)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                    .Where(x => x.CountReindex >= maxCountReindex 
                                                && (x.Step == (int)OrderStep.DA_CAN_RA || x.Step == (int)OrderStep.DANG_GOI_XE))
                                    .ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetOrdersByStep(int step)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                    .Where(x => x.Step == step)
                                    .ToListAsync();
                return orders;
            }
        }

        public async Task<bool> CompleteOrder(int? orderId)
        {
            using (var dbContext = new XHTD_Entities())
            {
                bool isCompleted = false;

                try
                {
                    string completeTime = DateTime.Now.ToString();

                    var order = dbContext.tblStoreOrderOperatings
                                        .FirstOrDefault(x => x.Id == orderId && x.Step == (int)OrderStep.DA_CAN_RA);

                    if (order != null)
                    {
                        order.Step = (int)OrderStep.DA_GIAO_HANG;
                        order.TimeConfirm9 = DateTime.Now;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Tự động hoàn thành lúc {completeTime} ";

                        await dbContext.SaveChangesAsync();

                        Console.WriteLine($@"Auto Complete Order {orderId}");
                        log.Info($@"Auto Complete Order {orderId}");

                        isCompleted = true;
                    }

                    return isCompleted;
                }
                catch (Exception ex)
                {
                    log.Error($@"Auto Complete Order {orderId} Error: " + ex.Message);
                    Console.WriteLine($@"Auto Complete Order {orderId} Error: " + ex.Message);

                    return isCompleted;
                }
            }
        }

        public int GetMaxIndexByTypeProduct(string typeProduct)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var order = dbContext.tblStoreOrderOperatings
                                .Where(x => x.TypeProduct == typeProduct && x.Step == (int)OrderStep.DA_CAN_VAO)
                                .OrderByDescending(x => x.IndexOrder)
                                .FirstOrDefault();

                if(order != null) { 
                    return (int)order.IndexOrder;
                }

                return 0;
            }
        }

        public int GetMaxIndexByCatId(string catId)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var order = dbContext.tblStoreOrderOperatings
                                .Where(x => x.CatId == catId && x.Step == (int)OrderStep.DA_CAN_VAO && x.IsVoiced == false)
                                .OrderByDescending(x => x.IndexOrder)
                                .FirstOrDefault();

                if (order != null)
                {
                    return (int)order.IndexOrder;
                }

                return 0;
            }
        }

        public async Task SetIndexOrder(string deliveryCode)
        {
            var orderExist = _appDbContext.tblStoreOrderOperatings.FirstOrDefault(x => x.DeliveryCode == deliveryCode);

            if (orderExist != null)
            {
                var typeProduct = orderExist.TypeProduct;

                var maxIndex = GetMaxIndexByTypeProduct(typeProduct);

                var newIndex = maxIndex + 1;

                await UpdateIndex(orderExist.Id, newIndex);
            }
        }

        public async Task ReindexOrder(string typeProduct, string message = null)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var typeProductOrders = await dbContext.tblStoreOrderOperatings
                                                       .Where(x => x.TypeProduct.ToUpper() == typeProduct.ToUpper() &&
                                                                  (x.Step == (int)OrderStep.DA_XAC_THUC ||
                                                                   x.Step == (int)OrderStep.CHO_GOI_XE ||
                                                                   x.Step == (int)OrderStep.DANG_GOI_XE) &&
                                                                   x.IndexOrder != 0 &&
                                                                   x.IsVoiced == false)
                                                       .OrderBy(x => x.TimeConfirm10)
                                                       .ToListAsync();

                var indexOrder = 1;
                foreach (var typeProductOrder in typeProductOrders)
                {
                    if (typeProductOrder.IndexOrder != indexOrder)
                    {
                        typeProductOrder.IndexOrder = indexOrder;
                        typeProductOrder.LogProcessOrder += $"#Đơn hàng được xếp lại lốt: {indexOrder}, lý do: {message} ";

                        var pushMessage = $"Đơn hàng số hiệu {typeProductOrder.DeliveryCode} thay đổi số thứ tự chờ vào cổng lấy hàng: #{typeProductOrder.IndexOrder}";
                        SendPushNotification(typeProductOrder.DriverUserName, pushMessage);
                    }
                    indexOrder++;
                }
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task ReindexOrderToLastIndex(int orderId, string message = null)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var currentOrder = await dbContext.tblStoreOrderOperatings.FirstOrDefaultAsync(x => x.Id == orderId);
                if (currentOrder != null)
                {
                    var typeProductOrders = await dbContext.tblStoreOrderOperatings
                                                           .Where(x => x.TypeProduct.ToUpper() == currentOrder.TypeProduct.ToUpper() &&
                                                                      (x.Step == (int)OrderStep.DA_XAC_THUC ||
                                                                       x.Step == (int)OrderStep.CHO_GOI_XE ||
                                                                       x.Step == (int)OrderStep.DANG_GOI_XE) &&
                                                                       x.IndexOrder != 0 &&
                                                                       x.Id != currentOrder.Id &&
                                                                       x.IsVoiced == false)
                                                           .OrderBy(x => x.TimeConfirm10)
                                                           .ToListAsync();

                    var indexOrder = 1;
                    foreach (var typeProductOrder in typeProductOrders)
                    {
                        if (typeProductOrder.IndexOrder != indexOrder)
                        {
                            typeProductOrder.IndexOrder = indexOrder;
                            typeProductOrder.LogProcessOrder += $"#Đơn hàng được xếp lại lốt: {indexOrder}, lý do: {message} ";

                            var pushMessage = $"Đơn hàng số hiệu {typeProductOrder.DeliveryCode} thay đổi số thứ tự chờ vào cổng lấy hàng: #{typeProductOrder.IndexOrder}";
                            SendPushNotification(typeProductOrder.DriverUserName, pushMessage);
                        }
                        indexOrder++;
                    }

                    currentOrder.IndexOrder = indexOrder;
                    currentOrder.Step = (int)OrderStep.DA_XAC_THUC;
                    currentOrder.LogProcessOrder += $"#Đơn hàng bị xoay lốt vào lúc {DateTime.Now}. ";

                    var pushMessageChanged = $"Đơn hàng số hiệu {currentOrder.DeliveryCode} thay đổi số thứ tự chờ vào cổng lấy hàng: #{currentOrder.IndexOrder}";
                    SendPushNotification(currentOrder.DriverUserName, pushMessageChanged);

                    await dbContext.SaveChangesAsync();
                }
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetXiMangBaoOrdersAddToQueueToCall()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var ordersInQueue = await dbContext.tblCallToTroughs
                                    .Where(x => x.IsDone == false)
                                    .Select(x => x.DeliveryCode)
                                    .ToListAsync();

                var timeToAdd = DateTime.Now.AddMinutes(-1);

                var orders = await dbContext.tblStoreOrderOperatings
                                    .Where(x => x.Step == (int)OrderStep.DA_CAN_VAO
                                                && x.CatId == OrderCatIdCode.XI_MANG_BAO
                                                && x.TypeXK != OrderTypeXKCode.JUMBO
                                                && x.TypeXK != OrderTypeXKCode.SLING
                                                && x.IsVoiced == false
                                                && x.TimeConfirm3 < timeToAdd
                                                && !ordersInQueue.Contains(x.DeliveryCode)
                                    )
                                    .OrderBy(x => x.TimeConfirm3)
                                    .ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetXiMangBaoOrdersAddToQueueToCall(string typeProduct)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var ordersInQueue = await dbContext.tblCallToTroughs
                                    .Where(x => x.IsDone == false)
                                    .Select(x => x.DeliveryCode)
                                    .ToListAsync();

                var timeToAdd = DateTime.Now.AddMinutes(-1);

                var orders = await dbContext.tblStoreOrderOperatings
                                    .Where(x => x.Step == (int)OrderStep.DA_CAN_VAO
                                                && x.TypeProduct == typeProduct
                                                && x.IsVoiced == false
                                                && x.TimeConfirm3 < timeToAdd
                                                && !ordersInQueue.Contains(x.DeliveryCode)
                                    )
                                    .OrderBy(x => x.TimeConfirm3)
                                    .ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetXiMangRoiOrdersAddToQueueToCall()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var ordersInQueue = await dbContext.tblCallToTroughs
                                    .Where(x => x.IsDone == false)
                                    .Select(x => x.DeliveryCode)
                                    .ToListAsync();

                var timeToAdd = DateTime.Now.AddMinutes(-1);

                var orders = await dbContext.tblStoreOrderOperatings
                                    .Where(x => x.Step == (int)OrderStep.DA_CAN_VAO
                                                && x.TypeProduct == OrderTypeProductCode.ROI
                                                && x.IsVoiced == false
                                                && x.TimeConfirm3 < timeToAdd
                                                && !ordersInQueue.Contains(x.DeliveryCode)
                                    )
                                    .OrderBy(x => x.TimeConfirm3)
                                    .ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetClinkerOrdersAddToQueueToCall()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var ordersInQueue = await dbContext.tblCallToTroughs
                                    .Where(x => x.IsDone == false)
                                    .Select(x => x.DeliveryCode)
                                    .ToListAsync();

                var timeToAdd = DateTime.Now.AddMinutes(-1);

                var orders = await dbContext.tblStoreOrderOperatings
                                    .Where(x => x.Step == (int)OrderStep.DA_CAN_VAO
                                                && x.TypeProduct == OrderTypeProductCode.CLINKER
                                                && x.IsVoiced == false
                                                && x.TimeConfirm3 < timeToAdd
                                                && !ordersInQueue.Contains(x.DeliveryCode)
                                    )
                                    .OrderBy(x => x.TimeConfirm3)
                                    .ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetTypeXKOrdersAddToQueueToCall(string typeXK)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var ordersInQueue = await dbContext.tblCallToTroughs
                                    .Where(x => x.IsDone == false)
                                    .Select(x => x.DeliveryCode)
                                    .ToListAsync();

                var timeToAdd = DateTime.Now.AddMinutes(-1);

                var orders = await dbContext.tblStoreOrderOperatings
                                    .Where(x => x.Step == (int)OrderStep.DA_CAN_VAO
                                                && x.TypeProduct == typeXK
                                                && x.IsVoiced == false
                                                && x.TimeConfirm3 < timeToAdd
                                                && !ordersInQueue.Contains(x.DeliveryCode)
                                    )
                                    .OrderBy(x => x.TimeConfirm3)
                                    .ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetTypeOtherOrdersAddToQueueToCall()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var ordersInQueue = await dbContext.tblCallToTroughs
                                    .Where(x => x.IsDone == false)
                                    .Select(x => x.DeliveryCode)
                                    .ToListAsync();

                var timeToAdd = DateTime.Now.AddMinutes(-1);

                var orders = await dbContext.tblStoreOrderOperatings
                                    .Where(x => x.Step == (int)OrderStep.DA_CAN_VAO
                                                && x.TypeProduct == OrderTypeProductCode.OTHER
                                                && x.IsVoiced == false
                                                && x.TimeConfirm3 < timeToAdd
                                                && !ordersInQueue.Contains(x.DeliveryCode)
                                    )
                                    .OrderBy(x => x.TimeConfirm3)
                                    .ToListAsync();
                return orders;
            }
        }

        public async Task<bool> MarkIsSyncedOutSource1(string deliveryCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                bool isUpdated = false;

                try
                {
                    var order = dbContext.tblStoreOrderOperatings.FirstOrDefault(x => x.DeliveryCode == deliveryCode);
                    if (order != null)
                    {
                        order.IsSyncedOutSource1 = true;

                        await dbContext.SaveChangesAsync();

                        isUpdated = true;

                        log.Info($@"Danh dau IsSyncedOutSource1: {deliveryCode}");
                    }

                    return isUpdated;
                }
                catch (Exception ex)
                {
                    log.Error($@"MarkIsSyncedOutSource1 Error: " + ex.Message);
                    Console.WriteLine($@"MarkIsSyncedOutSource1 Error: " + ex.Message);

                    return isUpdated;
                }
            }
        }

        public async Task<bool> MarkIsSyncedOutSource2(string deliveryCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                bool isUpdated = false;

                try
                {
                    var order = dbContext.tblStoreOrderOperatings.FirstOrDefault(x => x.DeliveryCode == deliveryCode);
                    if (order != null)
                    {
                        order.IsSyncedOutSource2 = true;

                        await dbContext.SaveChangesAsync();

                        isUpdated = true;

                        log.Info($@"Danh dau IsSyncedOutSource2: {deliveryCode}");
                    }

                    return isUpdated;
                }
                catch (Exception ex)
                {
                    log.Error($@"MarkIsSyncedOutSource2 Error: " + ex.Message);
                    Console.WriteLine($@"MarkIsSyncedOutSource2 Error: " + ex.Message);

                    return isUpdated;
                }
            }
        }

        public static IRestResponse SendPushNotification(string userName, string message)
        {
            var apiUrl = ConfigurationManager.GetSection("API_DMS/Url") as NameValueCollection;

            var client = new RestClient(apiUrl["SendPushNotification"]);
            var request = new RestRequest();

            request.Method = Method.POST;
            request.AddJsonBody(new
            {
                ContentMessage = message,
                NotificationType = "XHTD",
                SubTitle = "HỆ THỐNG XUẤT HÀNG TỰ ĐỘNG",
                UserNameReceiver = userName,
                UserNameSender = "XHTD"
            });

            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");
            request.RequestFormat = DataFormat.Json;

            IRestResponse response = client.Execute(request);

            return response;
        }
    }
}
