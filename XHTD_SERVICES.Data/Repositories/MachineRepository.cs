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
using System.Data.Entity.Migrations;
using log4net.Repository.Hierarchy;
using System.Web.Configuration;
using XHTD_SERVICES.Data.Common;

namespace XHTD_SERVICES.Data.Repositories
{
    public class MachineRepository : BaseRepository<tblMachine>
    {
        private readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MachineRepository(XHTD_Entities appDbContext) : base(appDbContext)
        {
        }

        public async Task<List<string>> GetActiveXiBaoMachines()
        {
            using (var dbContext = new XHTD_Entities())
            {
                var query = from v in dbContext.tblMachines
                            join r in dbContext.tblMachineTypeProducts
                            on v.Code equals r.MachineCode
                            where
                                v.State == true
                                && (r.TypeProduct == "PCB30" || r.TypeProduct == "PCB40")
                            //orderby v.Id ascending
                            select v.Code;

                var troughts = await query.Distinct().ToListAsync();

                return troughts;
            }
        }

        public async Task<tblMachine> GetMachineByMachineCode(string machineCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                return await dbContext.tblMachines.FirstOrDefaultAsync(x => x.Code == machineCode);
            }
        }

        public async Task<tblMachine> GetMachineByTroughCode(string troughCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var machineTrough = await dbContext.TblMachineTroughs.Where(x => x.TroughCode == troughCode && 
                                                                                 x.Status != null && 
                                                                                 x.Status == true)
                                                                     .FirstOrDefaultAsync();
                if (machineTrough != null)
                {
                    var machine = await dbContext.tblMachines.FirstOrDefaultAsync(x => x.Code.ToUpper() == machineTrough.MachineCode.ToUpper());
                    if (machine != null) return machine;
                    return null;
                }

                return null;
            }
        }

        public async Task<bool> IsWorkingMachine(string machineCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var trough = await dbContext.tblTroughs.FirstOrDefaultAsync(x => x.Machine == machineCode && x.Working == true);
                if (trough != null)
                {
                    return true;
                }

                return false;
            }
        }

        public async Task<List<tblMachine>> GetPendingMachine()
        {
            try
            {
                using (var dbContext = new XHTD_Entities())
                {
                    return await dbContext.tblMachines
                        .Where(x => x.StartStatus == "PENDING" || x.StopStatus == "PENDING")
                        .Where(x=>x.ProductCategory == "XI_BAO")
                        .ToListAsync();
                }
            }
            catch (Exception)
            {
                return null;
            }
           
        }

        public async Task<bool> UpdateMachine(tblMachine machine)
        {
            try
            {
                _appDbContext.tblMachines.AddOrUpdate(machine);
                await _appDbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<string> Stop(string machineCode, string troughCode, string deliveryCode)
        {
            try
            {
               using (var dbContext = new XHTD_Entities())
                {
                    var machine = await dbContext.tblMachines.FirstOrDefaultAsync(x => x.Code == machineCode);
                    if (machine == null)
                    {
                        return "Không tìm thấy máy xuất";
                    }

                    if (!string.IsNullOrEmpty(deliveryCode))
                    {
                        var trough = !string.IsNullOrEmpty(troughCode) ?
                                     await dbContext.tblTroughs.FirstOrDefaultAsync(x => x.Code == troughCode) :
                                     await dbContext.tblTroughs.FirstOrDefaultAsync(x => x.DeliveryCodeCurrent == machine.CurrentDeliveryCode);

                        var order = await dbContext.tblStoreOrderOperatings.Where(x => x.DeliveryCode == deliveryCode).FirstOrDefaultAsync();

                        List<tblStoreOrderOperating> splitOrders = await dbContext.tblStoreOrderOperatings
                                                                                  .Where(x => x.DeliveryCode != order.DeliveryCode &&
                                                                                              x.Vehicle == order.Vehicle &&
                                                                                              x.TypeProduct == order.TypeProduct &&
                                                                                              x.NameProduct.ToUpper() == order.NameProduct.ToUpper() &&
                                                                                             (x.Step == (int)OrderStep.DA_CAN_VAO ||
                                                                                              x.Step == (int)OrderStep.DANG_LAY_HANG ||
                                                                                              x.Step == (int)OrderStep.DA_LAY_HANG) &&
                                                                                              x.IsVoiced == false)
                                                                                  .ToListAsync();

                        if (splitOrders == null || splitOrders.Count == 0)
                        {
                            order.ExportedNumber = (trough != null && order.DeliveryCode == trough.DeliveryCodeCurrent) ?
                                                   (decimal?)(trough.CountQuantityCurrent / 20) : 0;
                            order.MachineExportedNumber = (decimal?)(trough.FirstSensorQuantityCurrent / 20);
                        }

                        else
                        {
                            var totalExported = (trough != null && order.DeliveryCode == trough.DeliveryCodeCurrent) ?
                                                (decimal?)(trough.CountQuantityCurrent / 20) : 0;
                            var machineTotalExported = (decimal?)(trough.FirstSensorQuantityCurrent / 20);

                            order.ExportedNumber = (totalExported - order.SumNumber) >= 0 ? order.SumNumber : totalExported;
                            order.MachineExportedNumber = (machineTotalExported - order.SumNumber) >= 0 ? order.SumNumber : machineTotalExported;

                            totalExported -= order.SumNumber;
                            machineTotalExported -= order.SumNumber;

                            foreach (var splitOrder in splitOrders)
                            {
                                totalExported = totalExported >= 0 ? totalExported : 0;
                                machineTotalExported = machineTotalExported >= 0 ? machineTotalExported : 0;

                                splitOrder.ExportedNumber = (totalExported - splitOrder.SumNumber) >= 0 ? splitOrder.SumNumber : (splitOrder.ExportedNumber + totalExported);
                                splitOrder.MachineExportedNumber = (machineTotalExported - splitOrder.SumNumber) >= 0 ? splitOrder.SumNumber : (splitOrder.MachineExportedNumber + machineTotalExported);
                                splitOrder.Step = (int)OrderStep.DA_LAY_HANG;
                                splitOrder.TimeConfirm6 = DateTime.Now;
                                splitOrder.LogProcessOrder += $"#Xe lấy hàng lúc {DateTime.Now:dd/MM/yyyy HH:mm:ss} ";

                                totalExported -= splitOrder.SumNumber;
                                machineTotalExported -= splitOrder.SumNumber;
                            }

                            var callToTroughsList = await dbContext.tblCallToTroughs.ToListAsync();
                            var splitOrdersCallToTroughs = (from callToTroughs in callToTroughsList
                                                            join orders in splitOrders
                                                            on callToTroughs.DeliveryCode equals orders.DeliveryCode
                                                            where callToTroughs.IsDone == false
                                                            select callToTroughs)
                                                           .ToList();

                            foreach (var splitOrderCallToTrough in splitOrdersCallToTroughs)
                            {
                                splitOrderCallToTrough.IsDone = true;
                            }
                        }

                        order.Step = (int)OrderStep.DA_LAY_HANG;
                        order.TimeConfirm6 = DateTime.Now;
                        order.LogProcessOrder += $"#Xe lấy hàng lúc {DateTime.Now:dd/MM/yyyy HH:mm:ss} ";

                        var currentExportHistory = await dbContext.tblExportHistories.FirstOrDefaultAsync(x => x.DeliveryCode == order.DeliveryCode &&
                                                                                                                x.MachineCode == machineCode &&
                                                                                                                x.TroughCode == troughCode &&
                                                                                                                x.CountQuantityEnd == null &&
                                                                                                                x.TimeEnd == null);

                        if (currentExportHistory != null)
                        {
                            currentExportHistory.CountQuantityEnd = order.ExportedNumber != null ? (double?)(order.ExportedNumber * 20) : 0;
                            currentExportHistory.TimeEnd = DateTime.Now;
                            currentExportHistory.MachineExportedNumber = order.MachineExportedNumber;
                            currentExportHistory.FirstSensorCountQuantityEnd = trough.FirstSensorQuantityCurrent;
                            currentExportHistory.TimeGetOutTrough = DateTime.Now;
                        }

                        var callToTroughEntity = await dbContext.tblCallToTroughs
                                                        .Where(x => x.DeliveryCode == deliveryCode &&
                                                                   (x.IsDone == null || x.IsDone == false))
                                                        .FirstOrDefaultAsync();
                        if (callToTroughEntity != null)
                        {
                            callToTroughEntity.IsDone = true;
                        }

                        List<tblCallToTrough> callToTroughRunning = await dbContext.tblCallToTroughs
                                                                                   .Where(x => x.Machine == callToTroughEntity.Machine &&
                                                                                               x.IndexTrough > callToTroughEntity.IndexTrough &&
                                                                                              (x.IsDone == null || x.IsDone == false))
                                                                                   .ToListAsync();

                        if (callToTroughRunning != null && callToTroughRunning.Count > 0)
                        {
                            foreach (var callToTrough in callToTroughRunning)
                            {
                                if (callToTrough.IndexTrough > 1)
                                {
                                    callToTrough.IndexTrough--;
                                }
                            }
                        }

                        if (machine.CurrentDeliveryCode == deliveryCode)
                        {
                            if (machine.ProductCategory.ToUpper() == OrderProductCategoryCode.XI_BAO)
                            {
                                machine.StopStatus = MachineStatus.PENDING.ToString();
                            }

                            else if (machine.ProductCategory.ToUpper() == OrderProductCategoryCode.XI_ROI)
                            {
                                machine.StartStatus = MachineStatus.OFF.ToString();
                                machine.StopStatus = MachineStatus.ON.ToString();
                            }

                            machine.CurrentDeliveryCode = null;
                            machine.StartCountingFrom = 0;
                        }

                        var currentTrough = await dbContext.tblTroughs.Where(x => x.DeliveryCodeCurrent == deliveryCode).FirstOrDefaultAsync();
                        if (currentTrough != null)
                        {
                            currentTrough.DeliveryCodeCurrent = null;
                            currentTrough.PlanQuantityCurrent = null;
                            currentTrough.CountQuantityCurrent = null;
                            currentTrough.FirstSensorQuantityCurrent = null;
                            currentTrough.FirstCountFirstSensor = null;
                            currentTrough.LastCountFirstSensor = null;
                            currentTrough.FirstCountLastSensor = null;
                            currentTrough.LastCountLastSensor = null;
                        }
                    }

                    else
                    {
                        if (machine.ProductCategory.ToUpper() == OrderProductCategoryCode.XI_BAO)
                        {
                            machine.StopStatus = MachineStatus.PENDING.ToString();
                        }

                        else if (machine.ProductCategory.ToUpper() == OrderProductCategoryCode.XI_ROI)
                        {
                            machine.StartStatus = MachineStatus.OFF.ToString();
                            machine.StopStatus = MachineStatus.ON.ToString();
                        }

                        machine.CurrentDeliveryCode = null;
                        machine.StartCountingFrom = 0;
                    }

                    await dbContext.SaveChangesAsync();
                    return "OK";
                }
            }
            catch (Exception ex)
            {
                return $"Lỗi - {ex.Message} - {ex.InnerException} - {ex.StackTrace}";
            }
        }
    }
}
