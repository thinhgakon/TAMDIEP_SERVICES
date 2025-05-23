﻿using System;
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

        public async Task<tblTrough> GetTroughByTroughCode(string troughCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var trough = await dbContext.tblTroughs.FirstOrDefaultAsync(x => x.Code.ToUpper() == troughCode.ToUpper());
                if (trough == null) return null;
                return trough;
            }
        }

        public async Task<tblTrough> GetTroughByDeliveryCode(string deliveryCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var trough = await dbContext.tblTroughs.FirstOrDefaultAsync(x => x.DeliveryCodeCurrent.ToUpper() == deliveryCode.ToUpper());
                if (trough == null) return null;
                return trough;
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

        public async Task<List<string>> GetActiveTroughInMachine(string machineCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var machineTroughs = await dbContext.TblMachineTroughs
                                                    .Where(x => x.MachineCode.ToUpper() == machineCode.ToUpper() &&
                                                                x.Status == true)
                                                    .ToListAsync();

                if (machineTroughs == null || machineTroughs.Count == 0)
                {
                    return null;
                }

                return machineTroughs.Select(x => x.TroughCode).ToList();
            }
        }

        public async Task<bool> IsTroughActiveInAnyMachine(string troughCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var machineTroughs = await dbContext.TblMachineTroughs
                                                    .Where(x => x.TroughCode.ToUpper() ==  troughCode.ToUpper() &&
                                                                x.Status == true)
                                                    .ToListAsync();

                if (machineTroughs == null || machineTroughs.Count == 0)
                {
                    return false;
                }

                return true;
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

        public async Task UpdateMachineSensor(string deliveryCode, double firstSensorQuantity, DateTime firstCountFirstSensor, DateTime lastCountFirstSensor)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    var order = await dbContext.tblStoreOrderOperatings.FirstOrDefaultAsync(x => x.DeliveryCode == deliveryCode);
                    if (order == null) return;

                    var callToTrough = await dbContext.tblCallToTroughs.FirstOrDefaultAsync(x => x.DeliveryCode == deliveryCode && x.IsDone == false);
                    if (callToTrough == null) return;
                    
                    var trough = await dbContext.tblTroughs.FirstOrDefaultAsync(x => x.Code == callToTrough.Machine);
                    if (trough != null)
                    {
                        trough.Working = true;
                        trough.DeliveryCodeCurrent = deliveryCode;
                        trough.FirstSensorQuantityCurrent = firstSensorQuantity;
                        trough.LastCountFirstSensor = lastCountFirstSensor;

                        if (trough.FirstCountFirstSensor == null)
                        {
                            trough.FirstCountFirstSensor = firstCountFirstSensor;
                        }

                        double? orderNetWeight = 50;
                        if (order.NetWeight != null && order.NetWeight != 0)
                        {
                            orderNetWeight = order.NetWeight;
                        }

                        order.MachineExportedNumber = (decimal?)(firstSensorQuantity / 1000 * orderNetWeight);

                        await dbContext.SaveChangesAsync();

                        log.Info($@"Update Machine Sensor Trough {trough.Code} success");
                        Console.WriteLine($@"Update Machine Sensor Trough {trough.Code} Success");
                    }
                }
                catch (Exception ex)
                {
                    log.Error($@"=================== Update Machine Sensor Trough Error: " + ex.Message);
                    Console.WriteLine($@"Update Machine Sensor Trough Error: " + ex.Message);
                }
            }
        }

        public async Task UpdateTroughSensor(string troughCode, string deliveryCode, double countQuantity, double planQuantity, DateTime firstCountLastSensor, DateTime lastCountLastSensor)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    var order = await dbContext.tblStoreOrderOperatings.FirstOrDefaultAsync(x => x.DeliveryCode == deliveryCode);
                    if (order == null) return;

                    var trough = await dbContext.tblTroughs.FirstOrDefaultAsync(x => x.Code == troughCode);
                    if (trough != null)
                    {
                        trough.Working = true;
                        trough.DeliveryCodeCurrent = deliveryCode;
                        trough.CountQuantityCurrent = countQuantity;
                        trough.PlanQuantityCurrent = planQuantity;
                        trough.LastCountLastSensor = lastCountLastSensor;

                        if (trough.FirstCountLastSensor == null)
                        {
                            trough.FirstCountLastSensor = firstCountLastSensor;
                        }

                        double? orderNetWeight = 50;
                        if (order.NetWeight != null && order.NetWeight != 0)
                        {
                            orderNetWeight = order.NetWeight;
                        }

                        order.ExportedNumber = (decimal?)(countQuantity / 1000 * orderNetWeight);

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

        public async Task<string> GetMinQuantityTrough(string typeProduct, string productCategory)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var query = await (from trough in dbContext.tblTroughs

                                   join machineTrough in dbContext.TblMachineTroughs.Where(x => x.Status == true)
                                   on trough.Code equals machineTrough.TroughCode
                                   into machineTroughs

                                   from machineTrough in machineTroughs

                                   join machine in dbContext.tblMachines.Where(x => x.State == true)
                                   on machineTrough.MachineCode equals machine.Code

                                   join machineTypeProduct in dbContext.tblMachineTypeProducts
                                   on machine.Code equals machineTypeProduct.MachineCode
                                   into machineTypeProducts

                                   join callToTrough in dbContext.tblCallToTroughs.Where(x => x.IsDone == null || x.IsDone == false)
                                   on trough.Code equals callToTrough.Machine
                                   into callToTroughs

                                   where machine.ProductCategory == productCategory &&
                                         machineTypeProducts.Any(mtp => mtp.TypeProduct == typeProduct) &&
                                         trough.State == true

                                   select new
                                   {
                                       trough.Code,
                                       callToTroughs
                                   })
                                   .ToListAsync();

                var record = query.Select(x => new
                {
                    x.Code,
                    SumNumber = x.callToTroughs.Sum(y => y.SumNumber ?? 0)
                })
                .OrderBy(t => t.SumNumber)
                .FirstOrDefault();

                return record.Code;
            }
        }
    }
}
