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

        public int GetNumberOrderInQueue(string troughCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                return dbContext.tblCallToTroughs.Where(x => x.Trough == troughCode && x.IsDone == false).Count();
            }
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

        public async Task CreateAsync(OrderToCallInTroughResponse order, string troughCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    if (!IsInProgress(order.Id))
                    {
                        var newItem = new tblCallToTrough
                        {
                            OrderId = order.Id,
                            DeliveryCode = order.DeliveryCode,
                            Vehicle = order.Vehicle,
                            Trough = troughCode,
                            CountTry = 0,
                            CallLog = $@"Xe được mời vào lúc {DateTime.Now}.",
                            IsDone = false,
                            CreateDay = DateTime.Now,
                            UpdateDay = DateTime.Now,
                        };

                        dbContext.tblCallToTroughs.Add(newItem);
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    log.Error("CreateAsync vehicle log Error: " + ex.Message); ;
                    Console.WriteLine("CreateAsync vehicle log Error: " + ex.Message);
                }
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
                        itemToCall.CallLog = $@"{itemToCall.CallLog} # Gọi xe {vehiceCode} vào lúc {DateTime.Now}";

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
                                .Where(x => x.Machine == code)
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
                            Machine = machineCode,
                            MachineId = Int32.Parse(machineCode),
                            Trough = machineCode,
                            CountTry = 0,
                            IsDone = false,
                            CreateDay = DateTime.Now,
                            UpdateDay = DateTime.Now,
                            IndexTrough = indexTrough + 1,
                            SumNumber = sumNumber,
                            CallLog = $@"Xe được xếp vào máng lúc {DateTime.Now}.",
                        };

                        dbContext.tblCallToTroughs.Add(newItem);
                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        log.Error("Da ton tai"); 
                        Console.WriteLine("Da ton tai");
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Log Error: " + ex.Message); ;
                    Console.WriteLine("Log Error: " + ex.Message);
                }
            }
        }
    }
}
