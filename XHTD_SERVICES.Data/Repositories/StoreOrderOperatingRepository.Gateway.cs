using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Models.Response;
using log4net;
using System.Data.Entity;
using XHTD_SERVICES.Data.Models.Values;
using XHTD_SERVICES.Data.Common;

namespace XHTD_SERVICES.Data.Repositories
{
    public partial class StoreOrderOperatingRepository
    {
        // Cổng bảo vệ
        public async Task<tblStoreOrderOperating> GetCurrentOrderEntraceGateway(string vehicleCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var order = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.Vehicle == vehicleCode
                                                     && x.IsVoiced == false
                                                     && (
                                                            (
                                                                (x.CatId == OrderCatIdCode.CLINKER || x.TypeXK == OrderTypeXKCode.JUMBO || x.TypeXK == OrderTypeXKCode.SLING)
                                                                &&
                                                                x.Step < (int)OrderStep.DA_CAN_RA
                                                            )
                                                            ||
                                                            (
                                                                (x.CatId != OrderCatIdCode.CLINKER && x.TypeXK != OrderTypeXKCode.JUMBO && x.TypeXK != OrderTypeXKCode.SLING)
                                                                &&
                                                                x.Step <= (int)OrderStep.DA_CAN_RA
                                                            )
                                                        )
                                                     )
                                            .OrderByDescending(x => x.Step)
                                            .FirstOrDefaultAsync();

                return order;
            }
        }

        public async Task<tblStoreOrderOperating> GetCurrentOrderExitGateway(string vehicleCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var order = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.Vehicle == vehicleCode
                                                     && x.IsVoiced == false
                                                     && (
                                                            (
                                                                (x.CatId == OrderCatIdCode.CLINKER || x.TypeXK == OrderTypeXKCode.JUMBO || x.TypeXK == OrderTypeXKCode.SLING)
                                                                &&
                                                                x.Step <= (int)OrderStep.DA_CAN_RA
                                                            )
                                                            ||
                                                            (
                                                                (x.CatId != OrderCatIdCode.CLINKER && x.TypeXK != OrderTypeXKCode.JUMBO && x.TypeXK != OrderTypeXKCode.SLING)
                                                                &&
                                                                x.Step <= (int)OrderStep.DA_CAN_RA
                                                            )
                                                        )
                                                  )
                                            .OrderByDescending(x => x.Step)
                                            .FirstOrDefaultAsync();

                return order;
            }
        }

        // Xác thực ra cổng
        public async Task<bool> UpdateOrderConfirm8ByVehicleCode(string vehicleCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.Vehicle == vehicleCode
                                                     && x.Step == (int)OrderStep.DA_CAN_RA
                                                    )
                                            .ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.Confirm8 = 1;
                        order.TimeConfirm8 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_HOAN_THANH;
                        order.IndexOrder = 0;
                        order.CountReindex = 0;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Xác thực ra cổng lúc {currentTime} ";
                    }

                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($@"Xác thực ra cổng VehicleCode={vehicleCode} error: " + ex.Message);
                    return false;
                }
            }
        }

        public async Task<bool> UpdateOrderConfirm8ByCardNo(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.CardNo == cardNo
                                                     && x.Step == (int)OrderStep.DA_CAN_RA
                                                    )
                                            .ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.Confirm8 = 1;
                        order.TimeConfirm8 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_HOAN_THANH;
                        order.IndexOrder = 0;
                        order.CountReindex = 0;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Xác thực ra cổng lúc {currentTime} ";
                    }

                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($@"Xác thực ra cổng CardNo={cardNo} error: " + ex.Message);
                    return false;
                }
            }
        }

        // Xác thực vào cổng
        public async Task<bool> UpdateOrderConfirm2ByDeliveryCode(string deliveryCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    var order = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.DeliveryCode == deliveryCode
                                                     && x.Step < (int)OrderStep.DA_VAO_CONG
                                                     )
                                            .FirstOrDefaultAsync();

                    if (order == null)
                    {
                        return false;
                    }

                    order.Confirm2 = (int)ConfirmType.RFID;
                    order.TimeConfirm2 = DateTime.Now;
                    order.Step = (int)OrderStep.DA_VAO_CONG;
                    order.IndexOrder = 0;
                    order.CountReindex = 0;
                    order.LogProcessOrder = $@"{order.LogProcessOrder} #Vào cổng lúc {currentTime} ";

                    await dbContext.SaveChangesAsync();
                    return true;

                }
                catch (Exception ex)
                {
                    log.Error($@"Xác thực vào cổng DeliveryCode={deliveryCode} error: " + ex.Message);
                    return false;
                }
            }
        }
    }
}
