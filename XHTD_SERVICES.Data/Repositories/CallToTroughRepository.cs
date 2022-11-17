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

namespace XHTD_SERVICES.Data.Repositories
{
    public class CallToTroughRepository : BaseRepository <tblCallToTrough>
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public CallToTroughRepository(XHTD_Entities appDbContext) : base(appDbContext)
        {
        }

        public int GetNumberOrderInQueue(string troughCode)
        {
            return _appDbContext.tblCallToTroughs.Where(x => x.Trough == troughCode && x.IsDone == false).Count();
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
    }
}
