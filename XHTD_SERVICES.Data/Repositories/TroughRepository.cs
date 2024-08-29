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
                                 (r.TypeProduct == "PCB30" || r.TypeProduct == "PCB40")
                                //orderby v.Id ascending
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
                                    //.OrderBy(x => x.Id)
                                    .Select(x => x.Code)
                                    .ToListAsync();

                return trough;
            }
        }

        public async Task<tblTrough> GetDetail(string code)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var trough = await dbContext.tblTroughs.FirstOrDefaultAsync(x => x.Code == code);

                return trough;
            }
        }

        public async Task UpdateTrough(string troughCode, string deliveryCode, double countQuantity, double planQuantity, double firstSensorQuantity)
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
                        itemToCall.FirstSensorQuantityCurrent = firstSensorQuantity;

                        await dbContext.SaveChangesAsync();

                        log.Info($@"Update Trough {troughCode} success");
                        Console.WriteLine($@"UpdateTrough {troughCode} Success");
                    }
                }
                catch (Exception ex)
                {
                    log.Error($@"=================== UpdateTrough Error: " + ex.Message);
                    Console.WriteLine($@"UpdateTrough Error: " + ex.Message);
                }
            }
        }

        public async Task UpdateMachineSensor(string deliveryCode, double firstSensorQuantity)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    var callToTrough = await dbContext.tblCallToTroughs.FirstOrDefaultAsync(x => x.DeliveryCode == deliveryCode && x.IsDone == false);
                    if (callToTrough == null) return;
                    
                    var itemToCall = await dbContext.tblTroughs.FirstOrDefaultAsync(x => x.Code == callToTrough.Machine);
                    if (itemToCall != null)
                    {
                        itemToCall.Working = true;
                        itemToCall.DeliveryCodeCurrent = deliveryCode;
                        itemToCall.FirstSensorQuantityCurrent = firstSensorQuantity;

                        await dbContext.SaveChangesAsync();

                        log.Info($@"Update Machine Sensor Trough {itemToCall.Code} success");
                        Console.WriteLine($@"Update Machine Sensor Trough {itemToCall.Code} Success");
                    }
                }
                catch (Exception ex)
                {
                    log.Error($@"=================== Update Machine Sensor Trough Error: " + ex.Message);
                    Console.WriteLine($@"Update Machine Sensor Trough Error: " + ex.Message);
                }
            }
        }

        public async Task UpdateTroughSensor(string troughCode, string deliveryCode, double countQuantity, double planQuantity)
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

                        log.Info($@"Update Trough Sensor Trough {troughCode} success");
                        Console.WriteLine($@"Update Trough Sensor Trough {troughCode} Success");
                    }
                }
                catch (Exception ex)
                {
                    log.Error($@"=================== Update Trough Sensor Trough Error: " + ex.Message);
                    Console.WriteLine($@"Update Trough Sensor Trough Error: " + ex.Message);
                }
            }
        }

        public async Task ResetTrough(string troughCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    var itemToCall = await dbContext.tblTroughs.FirstOrDefaultAsync(x => x.Code == troughCode && x.Working == true);
                    if (itemToCall != null)
                    {
                        itemToCall.Working = false;
                        itemToCall.DeliveryCodeCurrent = null;
                        itemToCall.CountQuantityCurrent = null;
                        itemToCall.PlanQuantityCurrent = null;
                        itemToCall.FirstSensorQuantityCurrent = null;

                        await dbContext.SaveChangesAsync();

                        log.Info($@"Reset Trough {troughCode} success");
                        Console.WriteLine($@"ResetTrough {troughCode} Success");
                    }
                }
                catch (Exception ex)
                {
                    log.Error($@"===================== ResetTrough Error: " + ex.Message);
                    Console.WriteLine($@"ResetTrough Error: " + ex.Message);
                }
            }
        }

        public async Task<string> GetMinQuantityMachine(string typeProduct, string productCategory)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var query = from t in dbContext.tblTroughs where t.State == true && t.ProductCategory == productCategory
                            join ttp in dbContext.tblTroughTypeProducts
                            on t.Code equals ttp.TroughCode into typeProducts

                            from typeProductItem in typeProducts.DefaultIfEmpty()
                            where typeProductItem.TypeProduct == typeProduct

                            join ctt in dbContext.tblCallToTroughs
                            on t.Code equals ctt.Machine into callToTroughs

                            from callToTroughItem in callToTroughs.DefaultIfEmpty()
                            //where callToTroughItem.IsDone == false

                            select new {
                                t.Code,
                                callToTroughItem.IsDone,
                                callToTroughItem.SumNumber,
                            };

                var records = await query.ToListAsync();

                var record = records.GroupBy(x => x.Code)
                                    .Select(item => new MinQuantityTroughResponse
                                    {
                                        Code = item.Key,
                                        SumNumber = (double)item.Where(i => i.IsDone == false).Sum(sm => sm.SumNumber)
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
