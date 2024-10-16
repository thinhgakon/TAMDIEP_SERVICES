using log4net;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Common;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Models.Values;

namespace XHTD_SERVICES.Data.Repositories
{
    public partial class StoreOrderOperatingRepository : BaseRepository<tblStoreOrderOperating>
    {
        public async Task<bool> UpdateBillOrderConfirm10(string vehicleCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.Vehicle == vehicleCode
                                                     && x.Step == (int)OrderStep.DA_NHAN_DON)
                                            .ToListAsync();

                    if (orders == null)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.Confirm10 = (int)ConfirmType.RFID;
                        order.TimeConfirm10 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_XAC_THUC;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Xác thực tự động lúc {currentTime} ";
                    }

                    await dbContext.SaveChangesAsync();
                    return true;

                }
                catch (Exception ex)
                {
                    log.Error($@"Xác thực VehicleCode={vehicleCode} error: " + ex.Message);
                    return false;
                }
            }
        }

        public void UpdateImgConfirm10(string vehicleCode,string img)
        {
            try
            {
                using (var db = new XHTD_Entities())
                {
                    var orders = db.tblStoreOrderOperatings.Where(x => x.Vehicle == vehicleCode && x.Step == (int)OrderStep.DA_XAC_THUC).ToList();
                    if (orders == null || orders.Count < 1) return;

                    var currentOrder = orders.FirstOrDefault();
                    currentOrder.ImgConfirm10 = img;
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                log.Error($@"UpdateIndexOrderForNewConfirm with vehicle {vehicleCode}, {ex.Message}");
            }
        }


        public void UpdateIndexOrderForNewConfirm(string vehicleCode)
        {
            try
            {
                var logProccess = "";
                using (var db = new XHTD_Entities())
                {
                    var orders = db.tblStoreOrderOperatings.Where(x => x.Vehicle == vehicleCode && x.Step == (int)OrderStep.DA_XAC_THUC && (x.IsVoiced == null || x.IsVoiced == false)).ToList();
                    if (orders == null || orders.Count < 1) return;

                    foreach (var currentOrder in orders)
                    {
                        if (currentOrder == null || currentOrder.IndexOrder > 0) return;
                        logProccess += $@"Don dang xu ly: {currentOrder.Id} loai sp: {currentOrder.TypeProduct}";

                        var orderIndexMax = db.tblStoreOrderOperatings.Where(x => (x.Step == (int)OrderStep.DA_XAC_THUC || x.Step == (int)OrderStep.CHO_GOI_XE || x.Step == (int)OrderStep.DANG_GOI_XE) && x.TypeProduct.Equals(currentOrder.TypeProduct) && (x.IsVoiced == null || x.IsVoiced == false)).Max(x => x.IndexOrder) ?? 0;
                        var indexOrderSet = orderIndexMax + 1;
                        logProccess += $@", xep lot cho xe {indexOrderSet}";

                        currentOrder.IndexOrder = indexOrderSet;
                        currentOrder.LogHistory = currentOrder.LogHistory + $@" #IndexOrder: {indexOrderSet}";
                        currentOrder.LogProcessOrder = currentOrder.LogProcessOrder + $@" #Xếp lốt : {indexOrderSet}";
                    }

                    db.SaveChanges();
                    log.Info(logProccess);
                }
            }
            catch (Exception ex)
            {
                log.Error($@"UpdateIndexOrderForNewConfirm with vehicle {vehicleCode}, {ex.Message}");
            }
        }

        public async Task<tblStoreOrderOperating> GetCurrentOrderConfirmationPoint(string vehicleCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var order = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.Vehicle == vehicleCode
                                                     && x.IsVoiced == false
                                                     && (x.Step == (int)OrderStep.DA_XAC_THUC
                                                     || x.Step == (int)OrderStep.DA_NHAN_DON
                                                     || x.Step == (int)OrderStep.CHUA_NHAN_DON)
                                                     )
                                            .OrderByDescending(x => x.Step)
                                            .FirstOrDefaultAsync();

                return order;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetOrdersConfirmationPoint(string vehicleCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.Vehicle == vehicleCode
                                                     && x.IsVoiced == false
                                                     && x.Step == (int)OrderStep.DA_NHAN_DON
                                                     && (x.DriverUserName ?? "") != ""
                                                     )
                                            .ToListAsync();
                return orders;
            }
        }
    }
}
