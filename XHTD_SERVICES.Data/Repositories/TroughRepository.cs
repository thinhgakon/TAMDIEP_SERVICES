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
    public class TroughRepository : BaseRepository <tblTrough>
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public TroughRepository(XHTD_Entities appDbContext) : base(appDbContext)
        {
        }

        public async Task<List<string>> GetActiveXiBaoTroughs()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var query = from v in dbContext.tblTroughs
                            join r in dbContext.tblTroughTypeProducts
                            on v.Code equals r.TroughCode
                            where
                                v.State == true 
                                &&  (r.TypeProduct == "PCB30" || r.TypeProduct == "PCB40")
                            select v.Code;

                var troughts = await query.Distinct().ToListAsync();

                return troughts;
            }
        }

        public async Task<List<string>> GetAllTroughCodes()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var trough = await dbContext.tblTroughs
                                    .Where(x => x.State == true)
                                    .OrderBy(x => x.Id)
                                    .Select(x => x.Code)
                                    .ToListAsync();

                return trough;
            }
        }

        public async Task<tblTrough> GetDetail(string code)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var trough = await dbContext.tblTroughs.FirstOrDefaultAsync(x => x.Code == code && x.State == true);

                return trough;
            }
        }

        public async Task UpdateTrough(string troughCode, string deliveryCode, double countQuantity, double planQuantity)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    var itemToCall = await dbContext.tblTroughs.FirstOrDefaultAsync(x => x.Code == troughCode);
                    if (itemToCall != null)
                    {
                        itemToCall.Working = true;
                        itemToCall.DeliveryCodeCurrent = deliveryCode;
                        itemToCall.CountQuantityCurrent = countQuantity;
                        itemToCall.PlanQuantityCurrent = planQuantity;

                        await dbContext.SaveChangesAsync();

                        log.Info($@"UpdateTrough {troughCode} Success");
                        Console.WriteLine($@"UpdateTrough {troughCode} Success");
                    }
                }
                catch (Exception ex)
                {
                    log.Error($@"UpdateTrough Error: " + ex.Message);
                    Console.WriteLine($@"UpdateTrough Error: " + ex.Message);
                }
            }
        }

        public async Task ResetTrough(string troughCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    var itemToCall = await dbContext.tblTroughs.FirstOrDefaultAsync(x => x.Code == troughCode);
                    if (itemToCall != null)
                    {
                        itemToCall.Working = false;
                        itemToCall.DeliveryCodeCurrent = null;
                        itemToCall.CountQuantityCurrent = null;
                        itemToCall.PlanQuantityCurrent = null;

                        await dbContext.SaveChangesAsync();

                        log.Info($@"ResetTrough {troughCode} Success");
                        Console.WriteLine($@"ResetTrough {troughCode} Success");
                    }
                }
                catch (Exception ex)
                {
                    log.Error($@"ResetTrough Error: " + ex.Message);
                    Console.WriteLine($@"ResetTrough Error: " + ex.Message);
                }
            }
        }

        public async Task<string> GetMinQuantityMachine(string typeProduct)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var query = from t in dbContext.tblMachines where t.State == true
                            join ttp in dbContext.tblMachineTypeProducts
                            on t.Code equals ttp.MachineCode into typeProducts
                            join ctt in dbContext.tblCallToTroughs
                            on t.Code equals ctt.Machine into callToTroughs
                            from typeProductItem in typeProducts.DefaultIfEmpty()
                            where typeProductItem.TypeProduct == typeProduct
                            from callToTroughItem in callToTroughs.DefaultIfEmpty()
                            select new {
                                t.Code,
                                callToTroughItem.SumNumber,
                            };

                var records = await query.ToListAsync();

                var record = records.GroupBy(x => x.Code)
                                    .Select(item => new MinQuantityTroughResponse
                                                {
                                                    Code = item.Key,
                                                    SumNumber = (double)item.Sum(m => m.SumNumber)
                                                })
                                    .OrderBy(x => x.SumNumber)
                                    .FirstOrDefault();

                if (record != null)
                {
                    return record.Code;
                }
            }

            return null;
        }
    }
}
