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
    public class CallToTroughRepository : BaseRepository <tblCallToTrough>
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        const int MAX_COUNT_TRY = 3;

        public CallToTroughRepository(XHTD_Entities appDbContext) : base(appDbContext)
        {
        }

        public bool IsInProgress(int orderId)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var record = dbContext.tblCallToTroughs.FirstOrDefault(x => x.OrderId == orderId && x.IsDone == false);
                if (record != null)
                {
                    return true;
                }
                return false;
            }
        }

        public tblCallToTrough GetItemToCall(string machineCode, int maxCountTryCall)
        {
            using (var dbContext = new XHTD_Entities())
            {
                return dbContext.tblCallToTroughs
                        //.Where(x => x.Machine == machineCode && x.IsDone == false && x.CountTry < maxCountTryCall)
                        .Where(x => x.Machine == machineCode && x.IsDone == false)
                        .OrderBy(x => x.IndexTrough)
                        .FirstOrDefault();
            }
        }

        public async Task<bool> UpdateWhenCall(int calLId, string vehiceCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                bool isUpdated = false;

                try
                {
                    var itemToCall = await dbContext.tblCallToTroughs.FirstOrDefaultAsync(x => x.Id == calLId);
                    if (itemToCall != null)
                    {
                        itemToCall.CountTry = itemToCall.CountTry + 1;
                        itemToCall.UpdateDay = DateTime.Now;
                        itemToCall.CallLog = $@"{itemToCall.CallLog} #Gọi xe {vehiceCode} vào lúc {DateTime.Now}";

                        await dbContext.SaveChangesAsync();

                        isUpdated = true;
                    }

                    return isUpdated;
                }
                catch (Exception ex)
                {
                    log.Error($@"UpdateWhenCall Error: " + ex.Message);
                    Console.WriteLine($@"UpdateWhenCall Error: " + ex.Message);

                    return isUpdated;
                }
            }
        }

        public async Task UpdateWhenIntoTrough(string deliveryCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    var itemToCall = await dbContext.tblCallToTroughs.FirstOrDefaultAsync(x => x.DeliveryCode == deliveryCode && x.IsDone == false);
                    if (itemToCall != null)
                    {
                        itemToCall.IsDone = true;
                        itemToCall.UpdateDay = DateTime.Now;
                        itemToCall.CallLog = $@"{itemToCall.CallLog} #Xe vào máng lúc {DateTime.Now}";

                        await dbContext.SaveChangesAsync();

                        log.Info($@"Dat isDone = true voi deliveryCode {deliveryCode} trong callToTrough");
                    }
                }
                catch (Exception ex)
                {
                    log.Error($@"====================== UpdateWhenIntoTrough Error: " + ex.Message);
                    Console.WriteLine($@"UpdateWhenIntoTrough Error: " + ex.Message);
                }
            }
        }

        public async Task UpdateWhenCanRa(string deliveryCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    var itemToCall = await dbContext.tblCallToTroughs.FirstOrDefaultAsync(x => x.DeliveryCode == deliveryCode && x.IsDone == false);
                    if (itemToCall != null)
                    {
                        var machineCode = itemToCall.Machine; 

                        itemToCall.IsDone = true;
                        itemToCall.UpdateDay = DateTime.Now;
                        itemToCall.CallLog = $@"{itemToCall.CallLog} #Xe cân ra lúc {DateTime.Now}";

                        await dbContext.SaveChangesAsync();

                        log.Info($@"Dat isDone = true voi deliveryCode {deliveryCode} da can ra");

                        // Xep lai STT với các đơn khác
                        var items = await dbContext.tblCallToTroughs
                                        .Where(x => x.Machine == machineCode && x.IsDone == false)
                                        .ToListAsync();

                        if (items != null && items.Count > 0)
                        {
                            int i = 1;
                            foreach (var item in items)
                            {
                                await UpdateIndex(item.DeliveryCode, i);
                                i++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error($@"====================== UpdateWhenCanRa Error: " + ex.Message);
                    Console.WriteLine($@"UpdateWhenCanRa Error: " + ex.Message);
                }
            }
        }

        public async Task UpdateWhenHuyDon(string deliveryCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    var itemToCall = await dbContext.tblCallToTroughs.FirstOrDefaultAsync(x => x.DeliveryCode == deliveryCode && x.IsDone == false);
                    if (itemToCall != null)
                    {
                        var machineCode = itemToCall.Machine;

                        itemToCall.IsDone = true;
                        itemToCall.UpdateDay = DateTime.Now;
                        itemToCall.CallLog = $@"{itemToCall.CallLog} #Huy don lúc {DateTime.Now}";

                        await dbContext.SaveChangesAsync();

                        log.Info($@"Dat isDone = true voi deliveryCode {deliveryCode} bi huy don");

                        // Xep lai STT với các đơn khác
                        var items = await dbContext.tblCallToTroughs
                                        .Where(x => x.Machine == machineCode && x.IsDone == false)
                                        .ToListAsync();

                        if (items != null && items.Count > 0)
                        {
                            int i = 1;
                            foreach (var item in items)
                            {
                                await UpdateIndex(item.DeliveryCode, i);
                                i++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error($@"====================== UpdateWhenHuyDon Error: " + ex.Message);
                    Console.WriteLine($@"UpdateWhenHuyDon Error: " + ex.Message);
                }
            }
        }

        public async Task UpdateWhenOverCountTry(int id)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    var overCountTryItem = await dbContext.tblCallToTroughs.FirstOrDefaultAsync(x => x.Id == id);

                    if (overCountTryItem == null) {
                        return;
                    }

                    var countReindex = overCountTryItem.CountReindex;
                    var indexTrough = overCountTryItem.IndexTrough;

                    var impactedItem = await dbContext.tblCallToTroughs.FirstOrDefaultAsync(x => x.Machine == overCountTryItem.Machine && x.IsDone == false && x.CountTry <= 3 && x.IndexTrough == indexTrough + 1);

                    if (impactedItem != null)
                    { 
                        overCountTryItem.IndexTrough = indexTrough + 1;

                        impactedItem.IndexTrough = indexTrough;
                        impactedItem.CallLog = $@"{impactedItem.CallLog} #Dịch lốt sau khi xe trước gọi không vào lúc {DateTime.Now}";
                    }

                    overCountTryItem.CountTry = 0;
                    overCountTryItem.CountReindex = countReindex + 1;
                    overCountTryItem.UpdateDay = DateTime.Now;
                    overCountTryItem.CallLog = $@"{overCountTryItem.CallLog} #Quá 5 phút sau gần gọi cuối cùng mà xe không vào, cập nhật lúc {DateTime.Now}";

                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    log.Error($@"UpdateWhenOverCountTry Error: " + ex.Message);
                    Console.WriteLine($@"UpdateWhenOverCountTry Error: " + ex.Message);
                }
            }
        }

        public async Task UpdateWhenOverCountReindex(int id)
        {
            //TODO: xếp lại STT của toàn bộ đơn hàng đang chờ trong máng
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    var itemToCall = await dbContext.tblCallToTroughs.FirstOrDefaultAsync(x => x.Id == id);

                    if(itemToCall == null)
                    {
                        return;
                    }
                    
                    itemToCall.IsDone = true;
                    itemToCall.UpdateDay = DateTime.Now;
                    itemToCall.CallLog = $@"{itemToCall.CallLog} # Quá 3 lần xoay vòng lốt mà xe không vào, hủy lốt lúc {DateTime.Now}";

                    await dbContext.SaveChangesAsync();

                    // Đặt lại STT cho các order khác
                    var impactedItems = await dbContext.tblCallToTroughs
                                                .Where(x => x.IsDone == false && x.Machine == itemToCall.Machine)
                                                .ToListAsync();

                    if (impactedItems != null && impactedItems.Count > 0)
                    {
                        int i = 1;
                        foreach (var impactedItem in impactedItems)
                        {
                            impactedItem.IndexTrough = i;
                            i++;
                        }
                    }

                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    log.Error($@"UpdateWhenOverCountTry Error: " + ex.Message);
                    Console.WriteLine($@"UpdateWhenOverCountTry Error: " + ex.Message);
                }
            }
        }

        public async Task<List<tblCallToTrough>> GetItemsOverCountTry(int maxCountTryCall = 3)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblCallToTroughs
                                            .Where(x => x.IsDone == false && x.CountTry >= maxCountTryCall)
                                            .ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblCallToTrough>> GetItemsOverCountReindex(int maxCountReindex = 3)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblCallToTroughs
                                            .Where(x => x.IsDone == false && x.CountReindex >= maxCountReindex)
                                            .ToListAsync();
                return orders;
            }
        }

        public async Task<int> GetMaxIndexByCode(string code)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var order = await dbContext.tblCallToTroughs
                                .Where(x => x.Machine == code && x.IsDone == false)
                                .OrderByDescending(x => x.IndexTrough)
                                .FirstOrDefaultAsync();

                if (order != null)
                {
                    return (int)order.IndexTrough;
                }

                return 0;
            }
        }

        public async Task AddItem(int orderId, string deliveryCode, string vehicle, string machineCode, decimal sumNumber)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    if (!IsInProgress(orderId))
                    {
                        var indexTrough = await GetMaxIndexByCode(machineCode);

                        var newItem = new tblCallToTrough
                        {
                            OrderId = orderId,
                            DeliveryCode = deliveryCode,
                            Vehicle = vehicle,
                            SumNumber = sumNumber,
                            Machine = machineCode,
                            IndexTrough = indexTrough + 1,
                            CountTry = 0,
                            CountReindex = 0,
                            IsDone = false,
                            CallLog = $@"#Xe được xếp vào máng lúc {DateTime.Now}.",
                            CreateDay = DateTime.Now,
                            UpdateDay = DateTime.Now,
                        };

                        dbContext.tblCallToTroughs.Add(newItem);

                        await dbContext.SaveChangesAsync();

                        log.Info($@"Them THANH CONG orderId {orderId} deliveryCode {deliveryCode} vao mang {machineCode}");
                    }
                    else
                    {
                        log.Error($"Da ton tai ban ghi orderId {orderId} deliveryCode {deliveryCode} trong mang {machineCode}"); 
                        Console.WriteLine("Da ton tai");
                    }
                }
                catch (Exception ex)
                {
                    log.Error("============================ AddItem Error: " + ex.Message); ;
                    Console.WriteLine("Log Error: " + ex.Message);
                }
            }
        }

        public async Task<bool> UpdateIndex(string deliveryCode, int index)
        {
            using (var dbContext = new XHTD_Entities())
            {
                bool isUpdated = false;

                try
                {
                    var order = dbContext.tblCallToTroughs.FirstOrDefault(x => x.DeliveryCode == deliveryCode && x.IsDone == false);
                    if (order != null)
                    {
                        order.IndexTrough = index;
                        order.CallLog = $@"#Reindex lúc {DateTime.Now}.";
                        order.UpdateDay = DateTime.Now;

                        await dbContext.SaveChangesAsync();

                        log.Info($"Update Index Trough:  deliveryCode={deliveryCode}, index={index}");
                        Console.WriteLine($"Update Index Trough: deliveryCode={deliveryCode}, index={index}");

                        isUpdated = true;
                    }

                    return isUpdated;
                }
                catch (Exception ex)
                {
                    log.Error($@"UpdateIndex Trough Error: " + ex.Message);
                    Console.WriteLine($@"UpdateIndex Trough Error: " + ex.Message);

                    return isUpdated;
                }
            }
        }
    }
}
