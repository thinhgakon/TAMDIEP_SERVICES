﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Models.Response;
using log4net;
using System.Data.Entity;
using XHTD_SERVICES.Data.Models.Values;
using XHTD_SERVICES.Data.Common;
using System.Data.SqlClient;

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
                                                     && (x.Step == (int)OrderStep.DA_XAC_THUC
                                                     || x.Step == (int)OrderStep.DANG_GOI_XE
                                                     || x.Step == (int)OrderStep.CHO_GOI_XE
                                                     || x.Step == (int)OrderStep.DA_NHAN_DON
                                                     || x.Step == (int)OrderStep.CHUA_NHAN_DON)
                                                     )
                                            .OrderByDescending(x => x.Step)
                                            .FirstOrDefaultAsync();

                return order;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetCurrentOrdersEntraceGateway(string vehicleCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.Vehicle == vehicleCode
                                                     && x.IsVoiced == false
                                                     && (x.Step == (int)OrderStep.DA_XAC_THUC
                                                     || x.Step == (int)OrderStep.DANG_GOI_XE
                                                     || x.Step == (int)OrderStep.CHO_GOI_XE
                                                     || x.Step == (int)OrderStep.DA_NHAN_DON
                                                     || x.Step == (int)OrderStep.CHUA_NHAN_DON)
                                                     )
                                            .OrderByDescending(x => x.Step)
                                            .ToListAsync();

                return orders;
            }
        }

        public async Task<tblStoreOrderOperating> GetCurrentOrderExitGateway(string vehicleCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var order = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.Vehicle == vehicleCode
                                                     && x.IsVoiced == false
                                                     && (x.Step == (int)OrderStep.DA_CAN_VAO
                                                     || x.Step == (int)OrderStep.DANG_LAY_HANG
                                                     || x.Step == (int)OrderStep.DA_LAY_HANG
                                                     || x.Step == (int)OrderStep.DA_CAN_RA)
                                                  )
                                            .OrderByDescending(x => x.Step)
                                            .FirstOrDefaultAsync();

                return order;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetCurrentOrdersExitGateway(string vehicleCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.Vehicle == vehicleCode
                                                     && x.IsVoiced == false
                                                     && (x.Step == (int)OrderStep.DA_CAN_VAO
                                                     || x.Step == (int)OrderStep.DANG_LAY_HANG
                                                     || x.Step == (int)OrderStep.DA_LAY_HANG
                                                     || x.Step == (int)OrderStep.DA_CAN_RA)
                                                  )
                                            .OrderByDescending(x => x.Step)
                                            .ToListAsync();

                return orders;
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
                        order.Confirm8 = (int)ConfirmType.RFID;
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
                        order.Confirm8 = (int)ConfirmType.RFID;
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
                                                     && x.Step == (int)OrderStep.DA_XAC_THUC)
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

        // Xác thực vào cổng
        public async Task<bool> UpdateOrderConfirm2ByVehicleCode(string vehicleCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.Vehicle == vehicleCode
                                                     && (
                                                         x.Step == (int)OrderStep.DA_XAC_THUC 
                                                         || 
                                                         x.Step == (int)OrderStep.CHO_GOI_XE
                                                         ||
                                                         x.Step == (int)OrderStep.DANG_GOI_XE
                                                         )
                                                     )
                                            .ToListAsync();

                    if (orders == null)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.Confirm2 = (int)ConfirmType.RFID;
                        order.TimeConfirm2 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_VAO_CONG;
                        order.IndexOrder = 0;
                        order.CountReindex = 0;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Vào cổng tự động lúc {currentTime} ";
                    }

                    await dbContext.SaveChangesAsync();
                    return true;

                }
                catch (Exception ex)
                {
                    log.Error($@"Xác thực vào cổng VehicleCode={vehicleCode} error: " + ex.Message);
                    return false;
                }
            }
        }

        // Lấy số lượng xe trong bãi chờ
        public int CountStoreOrderWaitingIntoTroughByType(string typeProduct)
        {
            var validStep = new[] { 
                                    OrderStep.DA_VAO_CONG, 
                                    OrderStep.DA_CAN_VAO, 
                                    OrderStep.CHO_GOI_XE,
                                    OrderStep.DANG_GOI_XE, 
                                    OrderStep.DANG_LAY_HANG, 
                                    OrderStep.DA_LAY_HANG
                                  };

            var validStepSql = string.Join(",", validStep.Select(s => (int)s));

            using (var db = new XHTD_Entities())
            {
                var sqlCount = $"SELECT COUNT(DISTINCT Vehicle) FROM dbo.tblStoreOrderOperating WHERE Step IN ({validStepSql}) AND IsVoiced = 0 AND TypeProduct = @TypeProduct";
                var count = db.Database.SqlQuery<int>(sqlCount, new SqlParameter("@TypeProduct", typeProduct)).Single();
                return count;
            }
        }
    }
}
