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
using System.Data.Entity.Migrations;

namespace XHTD_SERVICES.Data.Repositories
{
    public partial class StoreOrderOperatingRepository
    {
        // Trạm cân
        public async Task<tblStoreOrderOperating> GetCurrentOrderScaleStation(string vehicleCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.Vehicle == vehicleCode
                                                     && x.IsVoiced == false
                                                        && (
                                                            x.Step == (int)OrderStep.CHUA_NHAN_DON
                                                            ||
                                                            x.Step == (int)OrderStep.DA_NHAN_DON
                                                            ||
                                                            x.Step == (int)OrderStep.DA_XAC_THUC
                                                            ||
                                                            x.Step == (int)OrderStep.CHO_GOI_XE
                                                            ||
                                                            x.Step == (int)OrderStep.DANG_GOI_XE
                                                            ||
                                                            x.Step == (int)OrderStep.DA_VAO_CONG
                                                            ||
                                                            x.Step == (int)OrderStep.DA_CAN_VAO
                                                            ||
                                                            x.Step == (int)OrderStep.DANG_LAY_HANG
                                                            ||
                                                            x.Step == (int)OrderStep.DA_LAY_HANG
                                                        )
                                                   )
                                            .OrderByDescending(x => x.Step)
                                            .FirstOrDefaultAsync();

                return orders;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetOrdersScaleStationIn(string vehicleCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.Vehicle == vehicleCode
                                            && x.IsVoiced == false
                                            && (
                                                x.Step == (int)OrderStep.DA_XAC_THUC
                                                ||
                                                x.Step == (int)OrderStep.CHO_GOI_XE
                                                ||
                                                x.Step == (int)OrderStep.DANG_GOI_XE
                                                ||
                                                x.Step == (int)OrderStep.DA_VAO_CONG
                                                )
                                            )
                                            .ToListAsync();
                return orders;
            }
        }

        public async Task<List<tblStoreOrderOperating>> GetOrdersScaleStationOut(string vehicleCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.Vehicle == vehicleCode
                                            && x.IsVoiced == false
                                            && (
                                                x.Step == (int)OrderStep.DA_CAN_VAO
                                                ||
                                                x.Step == (int)OrderStep.DANG_LAY_HANG
                                                ||
                                                x.Step == (int)OrderStep.DA_LAY_HANG
                                                )
                                            )
                                            .ToListAsync();
                return orders;
            }
        }

        public async Task<bool> UpdateOrderConfirm3ByDeliveryCode(string deliveryCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string cancelTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    var order = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.DeliveryCode == deliveryCode
                                                     && (
                                                        x.Step == (int)OrderStep.DA_CAN_VAO
                                                        ||
                                                        x.Step == (int)OrderStep.DA_XAC_THUC
                                                        ||
                                                        x.Step == (int)OrderStep.CHO_GOI_XE
                                                        ||
                                                        x.Step == (int)OrderStep.DANG_GOI_XE
                                                        )
                                                     )
                                            .FirstOrDefaultAsync();

                    if (order == null)
                    {
                        return false;
                    }

                    order.Confirm3 = (int)ConfirmType.RFID;
                    order.TimeConfirm3 = DateTime.Now;
                    order.Step = (int)OrderStep.DA_CAN_VAO;
                    order.IndexOrder = 0;
                    order.CountReindex = 0;
                    order.LogProcessOrder = $@"{order.LogProcessOrder} #Cân vào tự động lúc {cancelTime} ";

                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($@"Cân vào {deliveryCode} Error: " + ex.Message);
                    return false;
                }
            }
        }

        public async Task<bool> UpdateOrderConfirm3ByVehicleCode(string vehicleCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.Vehicle == vehicleCode
                                                     && x.IsVoiced == false
                                                     && (
                                                        x.Step == (int)OrderStep.DA_XAC_THUC
                                                        ||
                                                        x.Step == (int)OrderStep.CHO_GOI_XE
                                                        ||
                                                        x.Step == (int)OrderStep.DANG_GOI_XE
                                                        ||
                                                        x.Step == (int)OrderStep.DA_VAO_CONG
                                                        )
                                                    )
                                            .ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.IsScaleInAuto = true;

                        order.Confirm3 = (int)ConfirmType.RFID;
                        order.TimeConfirm3 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_CAN_VAO;
                        order.IndexOrder = 0;
                        order.CountReindex = 0;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Cân vào tự động lúc {currentTime} ";
                    }

                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($@"Xác thực cân vào {vehicleCode} ERROR: " + ex.Message);
                    return false;
                }
            }
        }

        public async Task<bool> UpdateOrderConfirm3ByCardNo(string cardNo)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.CardNo == cardNo
                                                     && x.IsVoiced == false
                                                     && x.Step < (int)OrderStep.DA_CAN_VAO
                                                     &&
                                                     (
                                                        x.CatId == OrderCatIdCode.CLINKER
                                                        || x.TypeXK == OrderTypeXKCode.JUMBO
                                                        || x.TypeXK == OrderTypeXKCode.SLING
                                                     )
                                                    )
                                            .ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.Confirm3 = (int)ConfirmType.RFID;
                        order.TimeConfirm3 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_CAN_VAO;
                        order.IndexOrder = 0;
                        order.CountReindex = 0;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Đã cân vào lúc {currentTime} ";
                    }

                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($@"Xác thực cân vào {cardNo} error: " + ex.Message);
                    return false;
                }
            }
        }

        public async Task<bool> UpdateOrderConfirm7ByDeliveryCode(string deliveryCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string syncTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    var order = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.DeliveryCode == deliveryCode
                                                     && x.Step < (int)OrderStep.DA_CAN_RA
                                                     )
                                            .FirstOrDefaultAsync();

                    if (order == null)
                    {
                        log.Info($@"Khong ton tai deliveryCode={deliveryCode} voi step < 7");
                        return false;
                    }

                    log.Info($@"UpdateOrderConfirm7 deliveryCode={deliveryCode} with step={order.Step}");

                    order.Confirm7 = (int)ConfirmType.RFID;
                    order.TimeConfirm7 = DateTime.Now;
                    order.Step = (int)OrderStep.DA_CAN_RA;
                    order.IndexOrder = 0;
                    order.CountReindex = 0;
                    order.LogProcessOrder = $@"{order.LogProcessOrder} #Đã cân ra lúc {syncTime};";

                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($@"Cân ra {deliveryCode} Error: " + ex.Message);
                    return false;
                }
            }
        }

        public async Task<bool> UpdateOrderConfirm7ByVehicleCode(string vehicleCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.Vehicle == vehicleCode
                                                     && x.IsVoiced == false
                                                     && (
                                                        x.Step == (int)OrderStep.DA_CAN_VAO
                                                        ||
                                                        x.Step == (int)OrderStep.DANG_LAY_HANG
                                                        ||
                                                        x.Step == (int)OrderStep.DA_LAY_HANG
                                                        )
                                                    )
                                            .ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.IsScaleAuto = true;

                        order.Confirm7 = (int)ConfirmType.RFID;
                        order.TimeConfirm7 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_CAN_RA;
                        order.IndexOrder = 0;
                        order.CountReindex = 0;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Cân ra tự động lúc {currentTime};";
                    }

                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($@"Xác thực cân ra {vehicleCode} ERROR: " + ex.Message);
                    return false;
                }
            }
        }

        public async Task<bool> UpdateWeightIn(string deliveryCode, int weightIn)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    var order = await dbContext.tblStoreOrderOperatings
                                                .Where(x => x.DeliveryCode == deliveryCode
                                                         && x.Step < (int)OrderStep.DA_CAN_RA
                                                         && x.WeightIn == null
                                                      )
                                                .FirstOrDefaultAsync();

                    if (order == null)
                    {
                        return false;
                    }

                    // TODO for test
                    order.WeightInAuto = weightIn;
                    order.WeightInTimeAuto = DateTime.Now;

                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($@"Cân vào deliveryCode={deliveryCode} Error: " + ex.Message);
                    return false;
                }
            }
        }

        public async Task<bool> UpdateWeightInByVehicleCode(string vehicleCode, int weightIn)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string currentTime = DateTime.Now.ToString();

                    var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.Vehicle == vehicleCode
                                                     && x.IsVoiced == false
                                                     && x.Step < (int)OrderStep.DA_CAN_VAO
                                                     && x.WeightIn == null
                                                     &&
                                                     (
                                                        x.CatId == OrderCatIdCode.CLINKER
                                                        || x.TypeXK == OrderTypeXKCode.JUMBO
                                                        || x.TypeXK == OrderTypeXKCode.SLING
                                                     )
                                                    )
                                            .ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        //order.WeightIn = weightIn;

                        // TODO for test
                        order.WeightInAuto = weightIn;
                        order.WeightInTimeAuto = DateTime.Now;

                        //order.IsScaleAuto = true;
                        //order.Step = (int)OrderStep.DA_CAN_VAO;
                    }

                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($@"Cân vào vehicle={vehicleCode} error: " + ex.Message);
                    return false;
                }
            }
        }

        public async Task<bool> UpdateWeightInByCardNo(string cardNo, int weightIn)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string currentTime = DateTime.Now.ToString();

                    var orders = await dbContext.tblStoreOrderOperatings
                                            .Where(x => x.CardNo == cardNo
                                                     && x.IsVoiced == false
                                                     && x.Step < (int)OrderStep.DA_CAN_VAO
                                                     && x.WeightIn == null
                                                     &&
                                                     (
                                                        x.CatId == OrderCatIdCode.CLINKER
                                                        || x.TypeXK == OrderTypeXKCode.JUMBO
                                                        || x.TypeXK == OrderTypeXKCode.SLING
                                                     )
                                                    )
                                            .ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        //order.WeightIn = weightIn;

                        // TODO for test
                        order.WeightInAuto = weightIn;
                        order.WeightInTimeAuto = DateTime.Now;

                        //order.IsScaleAuto = true;
                        //order.Step = (int)OrderStep.DA_CAN_VAO;
                    }

                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($@"Cân vào cardNo={cardNo} error: " + ex.Message);
                    return false;
                }
            }
        }

        public async Task<bool> UpdateWeightOut(string deliveryCode, int weightOut)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    var order = await dbContext.tblStoreOrderOperatings
                                                .Where(x => x.DeliveryCode == deliveryCode
                                                         && x.Step >= (int)OrderStep.DA_CAN_VAO
                                                         && x.Step < (int)OrderStep.DA_HOAN_THANH
                                                         && x.WeightOut == null
                                                      )
                                                .FirstOrDefaultAsync();

                    if (order == null)
                    {
                        return false;
                    }

                    // TODO for test
                    order.WeightOutAuto = weightOut;
                    order.WeightOutTimeAuto = DateTime.Now;

                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($@"Cân ra deliveryCode={deliveryCode} Error: " + ex.Message);
                    return false;
                }
            }
        }

        public async Task<string> UpdateLotNumber(string deliveryCode)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    var order = await dbContext.tblStoreOrderOperatings
                                                .Where(x => x.DeliveryCode == deliveryCode)
                                                .FirstOrDefaultAsync();

                    if (order == null)
                    {
                        return null;
                    }

                    string source = "TAM_DIEP";

                    switch (order.IDDistributorSyn)
                    {
                        case 1065:
                            source = "BIM_SON";
                            break;
                        case 1067:
                            source = "HAI_PHONG";
                            break;
                        case 1072:
                            source = "HOANG_THACH";
                            break;
                        case 1058:
                            source = "BUT_SON";
                            break;
                        case 32537:
                            source = "SONG_THAO";
                            break;
                    }

                    var item = dbContext.Items.FirstOrDefault(x => x.Code == order.ItemId.ToString());

                    if(item == null)
                    {
                        return null;
                    }

                    var lotData = dbContext.TblQualityCertificates
                    .Where(x => x.State == "CHUA_KHOA")
                    .Where(x => x.PartnerId == order.IDDistributorSyn)
                    .Where(x=>x.Source == source)
                    .Where(x=>x.GroupId == item.GroupId)
                    .ToList();

                    if (lotData == null || lotData.Count == 0)
                    {
                        lotData = dbContext.TblQualityCertificates
                            .Where(x => x.State == "CHUA_KHOA")
                            .Where(x => x.PartnerId == null)
                            .Where(x => x.GroupId == item.GroupId)
                            .Where(x => x.Source == source)
                            .ToList();
                    }


                    var lot = lotData.Where(X => X.FromDate.Date <= DateTime.Now.Date)
                    .Where(x => x.ToDate.Date >= DateTime.Now.Date).FirstOrDefault();

                    if (lot == null)
                    {
                        return null;
                    }

                    if (string.IsNullOrEmpty(lot?.Code))
                    {
                        return null;
                    }

                    order.LotNumber = lot.Code;

                    dbContext.tblStoreOrderOperatings.AddOrUpdate(order);

                    await dbContext.SaveChangesAsync();
                    log.Info($"Cập nhật số lô {deliveryCode} - {lot.Code}");
                    return lot.Code;
                }
                catch (Exception ex)
                {
                    log.Error($@"Cập nhật số lô {deliveryCode} Error: " + ex.Message);
                    return null;
                }
            }
        }

        public async Task<bool> UpdateOrderEntraceTram951(string cardNo, int weightIn)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string cancelTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    var orders = await dbContext.tblStoreOrderOperatings
                                                .Where(x => x.CardNo == cardNo && (x.DriverUserName ?? "") != "" && x.Step == (int)OrderStep.DA_VAO_CONG)
                                                .ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.Confirm3 = (int)ConfirmType.RFID;
                        order.TimeConfirm3 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_CAN_VAO;
                        order.WeightIn = weightIn;
                        order.CountReindex = 0;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Đã cân vào lúc {cancelTime} ";
                    }

                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($@"Cân vào {cardNo} Error: " + ex.Message);
                    return false;
                }
            }
        }

        public async Task<bool> UpdateOrderExitTram951(string cardNo, int weightOut)
        {
            using (var dbContext = new XHTD_Entities())
            {
                try
                {
                    string cancelTime = DateTime.Now.ToString();

                    var orders = await dbContext.tblStoreOrderOperatings
                                                .Where(x => x.CardNo == cardNo && (x.DriverUserName ?? "") != "" && x.Step == (int)OrderStep.DA_LAY_HANG)
                                                .ToListAsync();

                    if (orders == null || orders.Count == 0)
                    {
                        return false;
                    }

                    foreach (var order in orders)
                    {
                        order.Confirm7 = (int)ConfirmType.RFID;
                        order.TimeConfirm7 = DateTime.Now;
                        order.Step = (int)OrderStep.DA_CAN_RA;
                        order.WeightOut = weightOut;
                        order.LogProcessOrder = $@"{order.LogProcessOrder} #Đã cân ra lúc {cancelTime} ";

                        log.Info($@"Cân ra {cardNo}");
                    }

                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error($@"Cân ra {cardNo} Error: " + ex.Message);
                    return false;
                }
            }
        }
    }
}
