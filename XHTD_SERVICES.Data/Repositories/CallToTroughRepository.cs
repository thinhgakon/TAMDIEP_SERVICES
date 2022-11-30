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

        public async Task CreateAsync(int orderId, string troughCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    if (!IsInProgress(orderId))
                    {
                        var newItem = new tblCallToTrough
                        {
                            OrderId = orderId,
                            Trough = troughCode,
                            CountTry = 0,
                            CallLog = $@"Xe được mời vào lúc {DateTime.Now}.",
                            IsDone = false,
                            CreateDay = DateTime.Now,
                            UpdateDay = DateTime.Now,
                        };

                        dbContext.tblCallToTroughs.Add(newItem);
                        await dbContext.SaveChangesAsync();

                        Console.WriteLine("Them order vao hang doi: " + orderId);
                    }
                    else
                    {
                        Console.WriteLine("Da ton tai order trong hang doi: " + orderId);
                    }
                }
                catch (Exception ex)
                {
                    log.Error("CreateAsync vehicle log Error: " + ex.Message); ;
                    Console.WriteLine("CreateAsync vehicle log Error: " + ex.Message);
                }
            }
        }

        public tblCallToTrough GetItemToCall(string troughCode, int maxCountTryCall)
        {
            using (var dbContext = new XHTD_Entities())
            {
                return dbContext.tblCallToTroughs
                        .Where(x => x.Trough == troughCode && x.IsDone == false && x.CountTry < maxCountTryCall)
                        //.Where(x => x.Trough == troughCode && x.IsDone == false)
                        .OrderBy(x => x.Id)
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

        public async Task<bool> UpdateWhenOverCountTry(int id)
        {
            using (var dbContext = new XHTD_Entities())
            {
                bool isUpdated = false;

                try
                {
                    var itemToCall = await dbContext.tblCallToTroughs.FirstOrDefaultAsync(x => x.Id == id);
                    if (itemToCall != null)
                    {
                        itemToCall.IsDone = true;
                        //itemToCall.UpdateDay = DateTime.Now;
                        itemToCall.CallLog = $@"{itemToCall.CallLog} # Quá 5 phút sau gần gọi cuối cùng mà xe không vào, cập nhật lúc {DateTime.Now}";

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

        public async Task<bool> UpdateWhenOverCountReindex(int id)
        {
            using (var dbContext = new XHTD_Entities())
            {
                bool isUpdated = false;

                try
                {
                    var itemToCall = await dbContext.tblCallToTroughs.FirstOrDefaultAsync(x => x.Id == id);
                    if (itemToCall != null)
                    {
                        itemToCall.IsDone = true;
                        itemToCall.UpdateDay = DateTime.Now;
                        itemToCall.CallLog = $@"{itemToCall.CallLog} # Quá 3 lần xoay vòng lốt mà xe không vào, hủy lốt lúc {DateTime.Now}";

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
    }
}
