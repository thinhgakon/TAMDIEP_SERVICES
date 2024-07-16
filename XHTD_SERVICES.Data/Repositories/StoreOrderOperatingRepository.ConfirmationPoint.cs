using log4net;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;

namespace XHTD_SERVICES.Data.Repositories
{
    public partial class StoreOrderOperatingRepository : BaseRepository<tblStoreOrderOperating>
    {
        public bool UpdateBillOrderConfirm10(string cardNo)
        {
            bool res = false;
            try
            {
                using (var db = this._appDbContext)
                {
                    var orders = db.tblStoreOrderOperatings.Where(x => x.CardNo == cardNo && (x.Step ?? 0) == 0 && (x.IndexOrder2 ?? 0) == 0 && (x.DriverUserName ?? "") != "").ToList();
                    if (orders.Count < 1) return false;
                    
                    var ordersFist = new tblStoreOrderOperating();

                    var sqlUpdateIndexOrder2 = $@"UPDATE tblStoreOrderOperating
                                                SET Confirm10 = 1 ,
                                                    TimeConfirm10 = GETDATE() ,
                                                    Step = 1 ,
                                                    IndexOrder2 = 1 ,
                                                    DeliveryCodeParent = @DeliveryCode ,
                                                    CountReindex = 0 ,
                                                    TimeConfirmHistory = GETDATE() ,
                                                    LogHistory = CONCAT(LogHistory, '#confirm by rfid at ', GETDATE()) ,
                                                    LogProcessOrder = CONCAT(LogProcessOrder, N'#Xác thực bước 1 lúc ',
                                                                                FORMAT(GETDATE(), 'dd/MM/yyyy HH:mm:ss'))
                                                WHERE Vehicle = @Vehicle
                                                      AND ISNULL(Step, 0) = 0
                                                      AND ISNULL(DriverUserName, '') != ''";

                    res = db.Database.ExecuteSqlCommand(sqlUpdateIndexOrder2, new SqlParameter("@DeliveryCode", ordersFist.DeliveryCode), new SqlParameter("@Vehicle", ordersFist.Vehicle)) > 0;
                }
            }
            catch (Exception ex)
            {
                log.Error($@"UpdateBillOrderConfirm10, card no {cardNo}, {ex.Message}");
            }
            return res;
        }

        public void UpdateIndexOrderForNewConfirm(string cardNo)
        {
            try
            {
                var logProccess = "";
                using (var db = this._appDbContext)
                {
                    var currentOrder = db.tblStoreOrderOperatings.Where(x => x.CardNo == cardNo && x.Step == 1 && (x.IndexOrder2 ?? 0) == 0).FirstOrDefault();
                    logProccess += $@"Don dang xu ly: {currentOrder.Id} loai sp: {currentOrder.TypeProduct}";

                    if (currentOrder == null || currentOrder.IndexOrder > 0) return;

                    var orderIndexMax = db.tblStoreOrderOperatings.Where(x => (x.Step == 1 || x.Step == 4) && (x.IndexOrder2 ?? 0) == 0 && x.TypeProduct.Equals(currentOrder.TypeProduct)).Max(x => x.IndexOrder) ?? 0;
                    // log thêm các đơn cùng loại đã được xếp lốt
                    var orderReceivings = db.tblStoreOrderOperatings.Where(x => (x.Step == 1 || x.Step == 4) && (x.IndexOrder2 ?? 0) == 0 && x.TypeProduct.Equals(currentOrder.TypeProduct)).ToList();
                    logProccess += $@", Cac don duoc xep lot truoc do: ";
                    foreach (var orderReceiving in orderReceivings)
                    {
                        logProccess += $@"Order {orderReceiving.Id} - lot hien tai: {orderReceiving.IndexOrder} - loai sp: {orderReceiving.TypeProduct} - step: {orderReceiving.Step},";
                    }

                    var indexOrderSet = orderIndexMax + 1;
                    logProccess += $@", xep lot cho xe {indexOrderSet}";

                    currentOrder.IndexOrder = indexOrderSet;
                    currentOrder.IndexOrderTemp = indexOrderSet;
                    currentOrder.IndexOrder1 = indexOrderSet;
                    currentOrder.LogHistory = currentOrder.LogHistory + $@" #IndexOrder: {indexOrderSet}";
                    currentOrder.LogProcessOrder = currentOrder.LogProcessOrder + $@" #Xếp lốt : {indexOrderSet}";

                    db.SaveChanges();
                    log.Info(logProccess);
                }
            }
            catch (Exception ex)
            {
                log.Error($@"UpdateIndexOrderForNewConfirm with cardno {cardNo}, {ex.Message}");
            }
        }
    }
}
