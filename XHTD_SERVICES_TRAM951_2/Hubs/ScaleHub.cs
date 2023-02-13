﻿using System;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Owin;
using Microsoft.Owin.Cors;
using log4net;
using XHTD_SERVICES.Helper;
using System.Linq;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Common;
using Autofac;
using XHTD_SERVICES_TRAM951_2.Devices;
using XHTD_SERVICES_TRAM951_2.Business;
using System.Threading;

namespace XHTD_SERVICES_TRAM951_2.Hubs
{
    public class ScaleHub : Hub
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(ScaleHub));

        public void SendMessage(string name, string message)
        {
            try
            {
                Console.WriteLine($"Send: name {name} message {message}");
                var broadcast = GlobalHost.ConnectionManager.GetHubContext<ScaleHub>();
                broadcast.Clients.All.SendMessage(name, message);
            }
            catch (Exception ex)
            {

            }
        }

        public void SendNotificationCBV(int status, string inout, string cardNo, string message)
        {
            Clients.All.SendNotificationCBV(status, inout, cardNo, message);
        }

        public void SendSensor(string sensorCode, string status)
        {
            try
            {
                Console.WriteLine($"Send: sensorCode {sensorCode} status {status}");
                var broadcast = GlobalHost.ConnectionManager.GetHubContext<ScaleHub>();
                broadcast.Clients.All.SendSensor(sensorCode, status);
            }
            catch (Exception ex)
            {

            }
            //Clients.All.SendSensor(sensorCode, status);
        }

        public void SendFakeRFID(string value)
        {
            Clients.All.SendFakeRFID(value);
        }

        public void Send9511ScaleInfo(DateTime time, string value)
        {
            // Call the broadcastMessage method to update clients.
            Clients.All.Send9511ScaleInfo(time, value);
        }

        public void Send9512ScaleInfo(DateTime time, string value)
        {
            // Call the broadcastMessage method to update clients.
            Clients.All.Send9512ScaleInfo(time, value);
        }

        public void SendClinkerScaleInfo(DateTime time, string value)
        {
            // Call the broadcastMessage method to update clients.
            Clients.All.SendClinkerScaleInfo(time, value);
            ReadDataScale951(time, value);
        }

        /*
        * Cân vào
        * 1. Xác định giá trị cân ổn định
        * 2. Lấy thông tin xe, đơn hàng đang cân
        * 3. Cập nhật khối lượng không tải của phương tiện
        * 4. Bật đèn đỏ
        * 5. Đóng barrier 2 chiều
        * 6. Gọi iERP API lưu giá trị cân
        * 7. Bật đèn xanh
        * 8. Mở barrier 2 chiều
        * 9. Update giá trị cân vào của đơn hàng
        * 10. Xếp STT
        * 11. Giải phóng cân
        */

        public async void ReadDataScale951(DateTime time, string value)
        {
            //logger.Info($"Received 951 data: time={time}, value={value}");

            int currentScaleValue = Int32.Parse(value);

            if (currentScaleValue < ScaleConfig.MIN_WEIGHT_VEHICLE)
            {
                // TODO: giải phóng cân khi xe ra khỏi bàn cân
                //if (Program.IsScalling951) {
                //    logger.Info($"==== Giai phong can 951 khi can khong thanh cong ===");

                //    Program.IsScalling951 = false;
                //    Program.IsLockingScale951 = false;
                //    Program.scaleValues951.Clear();

                //    await DIBootstrapper.Init().Resolve<ScaleBusiness>().ReleaseScale(ScaleCode.CODE_SCALE_951);

                //    // 8. Bật đèn xanh
                //    logger.Info($"=== Bat den xanh khi can khong thanh cong ===");
                //    DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnGreenTrafficLight(ScaleCode.CODE_SCALE_951_DGT_OUT);
                //    Thread.Sleep(500);
                //    DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnGreenTrafficLight(ScaleCode.CODE_SCALE_951_DGT_IN);
                //}

                Program.scaleValues951.Clear();

                return;
            }

            // TODO: kiểm tra vi phạm cảm biến cân
            if (!Program.IsLockingScale951)
            {
                var isInValidSensor951 = DIBootstrapper.Init().Resolve<SensorControl>().IsInValidSensorScale951();
                if (isInValidSensor951)
                {
                    // Send notification signalr
                    logger.Info("Vi pham cam bien can 951");

                    SendSensor(ScaleCode.CODE_SCALE_2, "1");

                    Program.scaleValues951.Clear();

                    return;
                }
                else
                {
                    SendSensor(ScaleCode.CODE_SCALE_2, "0");

                    logger.Info($"Received 951 data: time={time}, value={value}");
                }
            }

            if (Program.IsScalling951 && !Program.IsLockingScale951)
            {
                Program.scaleValues951.Add(currentScaleValue);

                if (Program.scaleValues951.Count > ScaleConfig.MAX_LENGTH_SCALE_VALUE)
                {
                    Program.scaleValues951.RemoveRange(0, 1);
                }

                var isOnDinh = Calculator.CheckBalanceValues(Program.scaleValues951, ScaleConfig.WEIGHT_SAISO);

                //var scaleText = String.Join(",", Program.scaleValues1);
                //logger.Info("Gia tri can 1: " + scaleText);

                logger.Info($"Received 951 data: time={time}, value={value}");

                if (isOnDinh)
                {
                    Program.IsLockingScale951 = true;

                    // 1. Xác định giá trị cân ổn định
                    logger.Info($"1. Can 951 on dinh: " + currentScaleValue);

                    using (var dbContext = new XHTD_Entities())
                    {
                        // 2. Lấy thông tin xe, đơn hàng đang cân
                        var scaleInfo = dbContext.tblScaleOperatings.FirstOrDefault(x => x.ScaleCode == ScaleCode.CODE_SCALE_2);
                        if (scaleInfo == null)
                        {
                            logger.Info($"Khong co ban ghi trong table Scale voi code = {ScaleCode.CODE_SCALE_2}");
                            return;
                        }
                        logger.Info($"2. Phuong tien dang can 951: Vehicle={scaleInfo.Vehicle} - CardNo={scaleInfo.CardNo} - DeliveryCode={scaleInfo.DeliveryCode}");

                        if ((bool)scaleInfo.IsScaling)
                        {
                            // Đang cân vào
                            if ((bool)scaleInfo.ScaleIn)
                            {
                                // 3. Cập nhật khối lượng không tải của phương tiện
                                logger.Info($"3. Cap nhat khoi luong khong tai");
                                //await DIBootstrapper.Init().Resolve<UnladenWeightBusiness>().UpdateUnladenWeight(scaleInfo.CardNo, currentScaleValue);

                                // 4. Bật đèn đỏ
                                //logger.Info($"4. Bat den do");
                                //DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnRedTrafficLight(ScaleCode.CODE_SCALE_951);

                                // 5. Đóng barrier
                                logger.Info($"5. Dong barrier IN");
                                DIBootstrapper.Init().Resolve<BarrierControl>().CloseBarrierScaleIn();
                                Thread.Sleep(1000);
                                logger.Info($"5. Dong barrier OUT");
                                DIBootstrapper.Init().Resolve<BarrierControl>().CloseBarrierScaleOut();

                                // 6. Gọi iERP API lưu giá trị cân
                                logger.Info($"6. Goi iERP API luu gia tri can");
                                Thread.Sleep(7000);

                                // 7. Mở barrier
                                logger.Info($"7. Mo barrier IN");
                                DIBootstrapper.Init().Resolve<BarrierControl>().OpenBarrierScaleIn();
                                Thread.Sleep(1000);
                                logger.Info($"7. Mo barrier OUT");
                                DIBootstrapper.Init().Resolve<BarrierControl>().OpenBarrierScaleOut();

                                Thread.Sleep(3500);

                                // 8. Bật đèn xanh
                                logger.Info($"8. Bat den xanh");
                                DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnGreenTrafficLight(ScaleCode.CODE_SCALE_2_DGT_OUT);
                                Thread.Sleep(500);
                                DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnGreenTrafficLight(ScaleCode.CODE_SCALE_2_DGT_IN);

                                // 9. Update giá trị cân của đơn hàng
                                logger.Info($"9. Update gia tri can vao");
                                await DIBootstrapper.Init().Resolve<WeightBusiness>().UpdateWeightIn(scaleInfo.CardNo, currentScaleValue);

                                // 10. Tiến hành xếp số thứ tự vào máng xuất lấy hàng của xe vừa cân vào xong
                                //logger.Info($"10. Xep so thu tu vao mang xuat");
                                //await DIBootstrapper.Init().Resolve<IndexOrderBusiness>().SetIndexOrder(scaleInfo.DeliveryCode);

                                // 11. Giải phóng cân
                                logger.Info($"11. Giai phong can 951");
                                Program.IsScalling951 = false;
                                Program.IsLockingScale951 = false;
                                Program.scaleValues951.Clear();
                                await DIBootstrapper.Init().Resolve<ScaleBusiness>().ReleaseScale(ScaleCode.CODE_SCALE_2);
                            }
                            // Đang cân ra
                            else if ((bool)scaleInfo.ScaleOut)
                            {
                                // 4. Bật đèn đỏ
                                //logger.Info($"4. Bat den do");
                                //DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnRedTrafficLight(ScaleCode.CODE_SCALE_1);

                                // 5. Đóng barrier
                                logger.Info($"5. Dong barrier IN");
                                DIBootstrapper.Init().Resolve<BarrierControl>().CloseBarrierScaleIn();
                                Thread.Sleep(1000);
                                logger.Info($"5. Dong barrier OUT");
                                DIBootstrapper.Init().Resolve<BarrierControl>().CloseBarrierScaleOut();

                                // 6. Gọi iERP API lưu giá trị cân
                                logger.Info($"6. Goi iERP API luu gia tri can");
                                Thread.Sleep(7000);

                                // 7. Mở barrier
                                logger.Info($"7. Mo barrier IN");
                                DIBootstrapper.Init().Resolve<BarrierControl>().OpenBarrierScaleIn();
                                Thread.Sleep(1000);
                                logger.Info($"7. Mo barrier OUT");
                                DIBootstrapper.Init().Resolve<BarrierControl>().OpenBarrierScaleOut();

                                Thread.Sleep(3500);

                                // 8. Bật đèn xanh
                                logger.Info($"8. Bat den xanh");
                                DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnGreenTrafficLight(ScaleCode.CODE_SCALE_2_DGT_OUT);
                                Thread.Sleep(500);
                                DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnGreenTrafficLight(ScaleCode.CODE_SCALE_2_DGT_IN);

                                // 9. Update giá trị cân của đơn hàng
                                logger.Info($"9. Update gia tri can ra");
                                await DIBootstrapper.Init().Resolve<WeightBusiness>().UpdateWeightOut(scaleInfo.CardNo, currentScaleValue);

                                // 9. Giải phóng cân: Program.IsScalling = false, update table tblScale
                                logger.Info($"11. Giai phong can 951");
                                Program.IsScalling951 = false;
                                Program.IsLockingScale951 = false;
                                Program.scaleValues951.Clear();
                                await DIBootstrapper.Init().Resolve<ScaleBusiness>().ReleaseScale(ScaleCode.CODE_SCALE_2);
                            }
                        }
                    }
                }
            }
            else
            {
                if (Program.scaleValues951.Count > 5)
                {
                    Program.scaleValues951.Clear();
                }
            }
        }

    }
}
