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
using XHTD_SERVICES.Data.Models.Response;
using System.Web.UI.WebControls;
using XHTD_SERVICES.Data.Common;

namespace XHTD_SERVICES.Data.Repositories
{
    public partial class StoreOrderOperatingRepository : BaseRepository <tblStoreOrderOperating>
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

        public async Task<tblStoreOrderOperating> GetDetail(string deliveryCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var order = await dbContext.tblStoreOrderOperatings.FirstOrDefaultAsync(x => x.DeliveryCode == deliveryCode);

                return order;
            }
        }

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

                        order.Confirm6 = 1;
                        order.TimeConfirm6 = DateTime.Now;
                        order.LogProcessOrder = order.LogProcessOrder + $@" #Xuất hàng xong lúc {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} ";
                    }
                    else if (step == (int)OrderStep.DANG_LAY_HANG)
                    {
                        if (order.Step == (int)OrderStep.DANG_LAY_HANG)
                        {
                            return true;
                        }
                    }

                    order.Confirm1 = 1;
                    order.Confirm2 = 1;
                    order.Confirm3 = 1;
                    order.Confirm4 = 1;
                    order.Confirm5 = 1;

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

        public async Task<List<tblStoreOrderOperating>> GetOrdersSortByIndex(int quantity)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings.Where(x => x.Step == (int)OrderStep.DA_CAN_VAO && (x.DriverUserName ?? "") != "").OrderBy(x => x.IndexOrder).Take(quantity).ToListAsync();
                return orders;
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

        public async Task<List<tblStoreOrderOperating>> GetCurrentOrdersEntraceGateway(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.CardNo == cardNo
                                                     && (
                                                            (
                                                                (x.CatId == "CLINKER" || x.TypeXK == "JUMBO" || x.TypeXK == "SLING") 
                                                                && 
                                                                x.Step < (int)OrderStep.DA_CAN_VAO
                                                            )
                                                        || 
                                                            (
                                                                (x.DriverUserName ?? "") != ""
                                                                && 
                                                                x.Step < (int)OrderStep.DA_CAN_VAO
                                                            )
                                                        )
                                                     )
                                            .ToListAsync();

                return orders;
            }
        }

        public async Task<tblStoreOrderOperating> GetCurrentOrderEntraceGateway(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var order = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.CardNo == cardNo
                                                     && (
                                                            (
                                                                (x.CatId == "CLINKER" || x.TypeXK == "JUMBO" || x.TypeXK == "SLING")
                                                                &&
                                                                x.Step < (int)OrderStep.DA_CAN_VAO
                                                            )
                                                        ||
                                                            (
                                                                (x.DriverUserName ?? "") != ""
                                                                &&
                                                                x.Step < (int)OrderStep.DA_CAN_VAO
                                                            )
                                                        )
                                                     )
                                            .OrderByDescending(x => x.Step)
                                            .FirstOrDefaultAsync();

                return order;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetCurrentOrdersExitGateway(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.CardNo == cardNo
                                                    && (
                                                            (
                                                                x.CatId == "CLINKER"
                                                                &&
                                                                x.Step == (int)OrderStep.DA_CAN_VAO
                                                            )
                                                        ||
                                                            (
                                                                x.CatId != "CLINKER"
                                                                &&
                                                                x.Step == (int)OrderStep.DA_CAN_RA
                                                            )
                                                        )
                                                     )
                                            .ToListAsync();

                return orders;
            }
        }

        public async Task<tblStoreOrderOperating> GetCurrentOrderExitGateway(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var order = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.CardNo == cardNo
                                                    && (
                                                            (
                                                                x.CatId == "CLINKER"
                                                                &&
                                                                x.Step == (int)OrderStep.DA_CAN_VAO
                                                            )
                                                        ||
                                                            (
                                                                x.CatId != "CLINKER"
                                                                &&
                                                                x.Step == (int)OrderStep.DA_CAN_RA
                                                            )
                                                        )
                                                     )
                                            .OrderByDescending(x => x.Step)
                                            .FirstOrDefaultAsync();

                return order;
            }
        }

        public async Task<tblStoreOrderOperating> GetCurrentOrderScaleStation(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.CardNo == cardNo
                                                        && (
                                                            (
                                                                (x.CatId == "CLINKER" || x.TypeXK == "JUMBO" || x.TypeXK == "SLING")
                                                                &&
                                                                x.Step < (int)OrderStep.DA_CAN_RA
                                                            )
                                                        ||
                                                            (
                                                                // TODO: for test
                                                                //(x.DriverUserName ?? "") != ""
                                                                //&&
                                                                x.Step < (int)OrderStep.DA_CAN_RA
                                                            )
                                                        )
                                                   )
                                            .OrderByDescending(x => x.Step)
                                            .FirstOrDefaultAsync();

                return orders;
            }
        }

        public async Task<tblStoreOrderOperating> GetCurrentOrderEntraceTram951ByCardNo(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                //var order = await dbContext.tblStoreOrderOperatings
                //                            .Where(x => x.CardNo == cardNo && (x.DriverUserName ?? "") != "" && x.Step == (int)OrderStep.DA_VAO_CONG)
                //                            .OrderByDescending(x => x.Id)
                //                            .FirstOrDefaultAsync();

                // TODO for test

                var order = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.CardNo == cardNo && x.Step < (int)OrderStep.DA_CAN_VAO)
                                            .OrderByDescending(x => x.Id)
                                            .FirstOrDefaultAsync();

                return order;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetOrdersEntraceTram951ByCardNoReceiving(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.CardNo == cardNo && (x.DriverUserName ?? "") != "" && x.Step == (int)OrderStep.DA_VAO_CONG)
                                            .ToListAsync();
                return orders;
            }
        }

        public async Task<tblStoreOrderOperating> GetCurrentOrderExitTram951ByCardNo(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                //var order = await dbContext.tblStoreOrderOperatings
                //                            .Where(x => x.CardNo == cardNo && (x.DriverUserName ?? "") != "" && x.Step == (int)OrderStep.DA_LAY_HANG)
                //                            .OrderByDescending(x => x.Id)
                //                            .FirstOrDefaultAsync();

                // TODO for test
                var order = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.CardNo == cardNo && x.Step > (int)OrderStep.DA_CAN_VAO && x.Step < (int)OrderStep.DA_CAN_RA)
                                            .OrderByDescending(x => x.Id)
                                            .FirstOrDefaultAsync();

                return order;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetOrdersExitTram951ByCardNoReceiving(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.CardNo == cardNo && (x.DriverUserName ?? "") != "" && x.Step == (int)OrderStep.DA_LAY_HANG)
                                            .ToListAsync();
                return orders;
            }
        }

        public async Task<bool> UpdateOrderEntraceGateway(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string cancelTime = DateTime.Now.ToString();

                    var orders = await dbContext.tblStoreOrderOperatings
                                                .Where(x => x.CardNo == cardNo && (x.DriverUserName ?? "") != "" && x.Step == (int)OrderStep.DA_NHAN_DON)
                                                .ToListAsync();

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
                        order.CountReindex = 0;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Xác thực vào cổng lúc {cancelTime} ";
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
                    string cancelTime = DateTime.Now.ToString();

                    var orders = await dbContext.tblStoreOrderOperatings
                                                .Where(x => x.CardNo == cardNo && (x.DriverUserName ?? "") != "" && x.Step == (int)OrderStep.DA_CAN_RA)
                                                .ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.Confirm8 = 1;
                        order.TimeConfirm8 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_HOAN_THANH;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Xác thực ra cổng lúc {cancelTime} ";

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

        // Cổng bảo vệ
        // Xác thực ra cổng
        public async Task<bool> UpdateOrderConfirm8(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string currentTime = DateTime.Now.ToString();

                    var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.CardNo == cardNo
                                                    && (
                                                            (
                                                                x.CatId == "CLINKER"
                                                                &&
                                                                x.Step == (int)OrderStep.DA_CAN_VAO
                                                            )
                                                        ||
                                                            (
                                                                x.CatId != "CLINKER"
                                                                &&
                                                                x.Step == (int)OrderStep.DA_CAN_RA
                                                            )
                                                        )
                                                     )
                                            .ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.Confirm8 = 1;
                        order.TimeConfirm8 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_HOAN_THANH;
                        order.IndexOrder = 0;
                        order.CountReindex = 0;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Xác thực ra cổng lúc {currentTime} ";
                    }

                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($@"Xác thực vào cổng {cardNo} error: " + ex.Message);
                    Console.WriteLine($@"Xác thực vào cổng {cardNo} Error: " + ex.Message);
                    return false;
                }
            }
        }

        // Xác thực vào cổng
        public async Task<bool> UpdateOrderConfirm2(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string currentTime = DateTime.Now.ToString();

                    var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.CardNo == cardNo
                                                     && (
                                                            (
                                                                (x.CatId == "CLINKER" || x.TypeXK == "JUMBO" || x.TypeXK == "SLING")
                                                                &&
                                                                x.Step < (int)OrderStep.DA_CAN_VAO
                                                            )
                                                        ||
                                                            (
                                                                (x.DriverUserName ?? "") != ""
                                                                &&
                                                                x.Step < (int)OrderStep.DA_CAN_VAO
                                                            )
                                                        )
                                                     )
                                            .ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.Confirm1 = 1;
                        order.TimeConfirm1 = order.TimeConfirm1 ?? DateTime.Now;
                        order.Confirm2 = 1;
                        order.TimeConfirm2 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_VAO_CONG;
                        order.IndexOrder = 0;
                        order.CountReindex = 0;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Xác thực vào cổng lúc {currentTime} ";
                    }

                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($@"Xác thực vào cổng {cardNo} error: " + ex.Message);
                    Console.WriteLine($@"Xác thực vào cổng {cardNo} Error: " + ex.Message);
                    return false;
                }
            }
        }

        // Trạm 951
        public async Task<bool> UpdateOrderConfirm3(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string cancelTime = DateTime.Now.ToString();

                    var orders = await dbContext.tblStoreOrderOperatings
                                                .Where(x => x.CardNo == cardNo && x.Step == (int)OrderStep.DA_VAO_CONG)
                                                .ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.Confirm2 = 1;
                        order.TimeConfirm2 = order.TimeConfirm2 ?? DateTime.Now;
                        order.Confirm3 = 1;
                        order.TimeConfirm3 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_CAN_VAO;
                        order.IndexOrder = 0;
                        order.CountReindex = 0;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Đã cân vào lúc {cancelTime} ";
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

        public async Task<bool> UpdateOrderConfirm7(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string cancelTime = DateTime.Now.ToString();

                    var orders = await dbContext.tblStoreOrderOperatings
                                                .Where(x => x.CardNo == cardNo && (x.DriverUserName ?? "") != "" && x.Step == (int)OrderStep.DA_LAY_HANG)
                                                .ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.Confirm7 = 1;
                        order.TimeConfirm7 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_CAN_RA;
                        order.IndexOrder = 0;
                        order.CountReindex = 0;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Cân ra lúc {cancelTime} ";
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

        public async Task<bool> UpdateWeightIn(string cardNo, int weightIn)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    var orders = await dbContext.tblStoreOrderOperatings
                                                .Where(x => x.CardNo == cardNo
                                                        && (x.DriverUserName ?? "") != ""
                                                        && x.Step > (int)OrderStep.DA_VAO_CONG && x.Step < (int)OrderStep.DA_HOAN_THANH)
                                                .ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.WeightIn = weightIn;
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

        public async Task<bool> UpdateWeightOut(string cardNo, int weightOut)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    var orders = await dbContext.tblStoreOrderOperatings
                                                .Where(x => x.CardNo == cardNo
                                                        && (x.DriverUserName ?? "") != ""
                                                        && x.Step > (int)OrderStep.DA_VAO_CONG && x.Step < (int)OrderStep.DA_HOAN_THANH)
                                                .ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.WeightOut = weightOut;
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

        public async Task<bool> UpdateOrderEntraceTram951(string cardNo, int weightIn)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string cancelTime = DateTime.Now.ToString();

                    var orders = await dbContext.tblStoreOrderOperatings
                                                .Where(x => x.CardNo == cardNo && (x.DriverUserName ?? "") != "" && x.Step == (int)OrderStep.DA_VAO_CONG)
                                                .ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.Confirm3 = 1;
                        order.TimeConfirm3 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_CAN_VAO;
                        order.WeightIn = weightIn;
                        order.CountReindex = 0;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Đã cân vào lúc {cancelTime} ";
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
                    string cancelTime = DateTime.Now.ToString();

                    var orders = await dbContext.tblStoreOrderOperatings
                                                .Where(x => x.CardNo == cardNo && (x.DriverUserName ?? "") != "" && x.Step == (int)OrderStep.DA_LAY_HANG)
                                                .ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.Confirm7 = 1;
                        order.TimeConfirm7 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_CAN_RA;
                        order.WeightOut = weightOut;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Đã cân ra lúc {cancelTime} ";

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

        public async Task<bool> ReindexToTrough(int orderId)
        {
            // TODO: xếp lại lốt là giá trị lớn nhất cần xét theo type product
            using (var dbContext = new XHTD_Entities())
            {
                bool isUpdated = false;

                try
                {
                    var itemToCall = await dbContext.tblStoreOrderOperatings.FirstOrDefaultAsync(x => x.Id == orderId);
                    if (itemToCall != null)
                    {
                        var oldIndexOrder = itemToCall.IndexOrder;
                        var newIndexOrder = oldIndexOrder + 2;

                        itemToCall.CountReindex++;
                        itemToCall.IndexOrder = newIndexOrder;
                        itemToCall.Step = (int)OrderStep.DA_CAN_VAO;
                        itemToCall.LogProcessOrder = $@"{itemToCall.LogProcessOrder} # Quá 5 phút sau lần gọi cuối cùng mà xe không vào, cập nhật lúc {DateTime.Now}, lốt cũ: {oldIndexOrder}, lốt mới: {newIndexOrder}";

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
                        order.Confirm9 = 1;
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

        public async Task<List<tblStoreOrderOperating>> GetOrdersXiMangRoiIndexed()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                    .Where(x => x.Step == (int)OrderStep.DA_CAN_VAO
                                                && x.CatId == "XI_MANG_XA"
                                                && x.IsVoiced == false
                                                && x.IndexOrder > 0
                                    )
                                    .OrderBy(x => x.TimeConfirm3)
                                    .ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetOrdersXiMangRoiNoIndex()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                    .Where(x => x.Step == (int)OrderStep.DA_CAN_VAO
                                                && x.CatId == "XI_MANG_XA"
                                                && x.IsVoiced == false
                                                //&& x.TimeConfirm3 < DateTime.Now.AddMinutes(-2)
                                                && (x.IndexOrder == null || x.IndexOrder == 0)
                                    )
                                    .OrderBy(x => x.TimeConfirm3)
                                    .ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetOrdersXiMangBaoIndexed()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                    .Where(x => x.Step == (int)OrderStep.DA_CAN_VAO
                                                && x.CatId == "XI_MANG_BAO"
                                                && x.IsVoiced == false
                                                && x.IndexOrder > 0
                                    )
                                    .OrderBy(x => x.TimeConfirm3)
                                    .ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetOrdersXiMangBaoNoIndex()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                    .Where(x => x.Step == (int)OrderStep.DA_CAN_VAO
                                                && x.CatId == "XI_MANG_BAO"
                                                && x.IsVoiced == false
                                                //&& x.TimeConfirm3 < DateTime.Now.AddMinutes(-2)
                                                && (x.IndexOrder == null || x.IndexOrder == 0)
                                    )
                                    .OrderBy(x => x.TimeConfirm3)
                                    .ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblCallToTrough>> GetOrdersLedXiBao()
        {
            using (var dbContext = new XHTD_Entities())
            {
                List<string> listMachine = new List<string>() { 
                    MachineCode.CODE_MACHINE_1,
                    MachineCode.CODE_MACHINE_2,
                    MachineCode.CODE_MACHINE_3,
                    MachineCode.CODE_MACHINE_4,
                };

                var orders = await dbContext.tblCallToTroughs
                                    .Where(x => x.IsDone == false
                                                && listMachine.Contains(x.Machine)
                                    )
                                    .OrderBy(x => x.IndexTrough)
                                    .ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblCallToTrough>> GetOrdersLedXiRoi()
        {
            using (var dbContext = new XHTD_Entities())
            {
                List<string> listMachine = new List<string>() {
                    MachineCode.CODE_MACHINE_9,
                    MachineCode.CODE_MACHINE_10,
                };

                var orders = await dbContext.tblCallToTroughs
                                    .Where(x => x.IsDone == false
                                                && listMachine.Contains(x.Machine)
                                    )
                                    .OrderBy(x => x.IndexTrough)
                                    .ToListAsync();
                return orders;
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

                var timeToAdd = DateTime.Now.AddMinutes(-2);

                var orders = await dbContext.tblStoreOrderOperatings
                                    .Where(x => x.Step == (int)OrderStep.DA_CAN_VAO
                                                && x.CatId == "XI_MANG_BAO"
                                                && x.TypeXK != "JUMBO"
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

                var timeToAdd = DateTime.Now.AddMinutes(-2);

                var orders = await dbContext.tblStoreOrderOperatings
                                    .Where(x => x.Step == (int)OrderStep.DA_CAN_VAO
                                                && (x.CatId == "XI_MANG_XA" || (x.CatId == "XI_MANG_BAO" && x.TypeXK == "JUMBO")) 
                                                && x.IsVoiced == false
                                                && x.TimeConfirm3 < timeToAdd
                                                && !ordersInQueue.Contains(x.DeliveryCode)
                                    )
                                    .OrderBy(x => x.TimeConfirm3)
                                    .ToListAsync();
                return orders;
            }
        }
    }
}
