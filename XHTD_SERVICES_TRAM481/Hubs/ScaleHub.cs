using System;
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
using XHTD_SERVICES_TRAM481.Devices;
using XHTD_SERVICES_TRAM481.Business;
using System.Threading;

namespace XHTD_SERVICES_TRAM481.Hubs
{
    public class ScaleHub : Hub
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(ScaleHub));

        public void Send(string name, string message)
        {
            Clients.All.addMessage(name, message);
            Console.WriteLine("send send");
        }

        public void SendNotificationCBV(int status, string inout, string cardNo, string message)
        {
            Clients.All.SendNotificationCBV(status, inout, cardNo, message);
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
            ReadDataScale481(time, value);
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

        public async void ReadDataScale481(DateTime time, string value)
        {
            //logger.Info($"Received 481 data: time={time}, value={value}");

            int currentScaleValue = Int32.Parse(value);
            if (currentScaleValue == 111)
            {
                Program.IsScalling481 = true;
                logger.Info("IsScalling481 true");
            }
            else if (currentScaleValue == 999)
            {
                Program.IsScalling481 = false;
                logger.Info("IsScalling481 false");
            }

            if (currentScaleValue < ScaleConfig.MIN_WEIGHT_VEHICLE)
            {
                // TODO: giải phóng cân khi xe ra khỏi bàn cân
                // Case này cũng xảy ra khi xe vừa vào bàn cân, lúc này chưa nhận diện dc RFID nên chưa xét IsScalling1
                //Program.IsScalling481 = false;
                //Program.IsLockingScale481 = false;
                Program.scaleValues481.Clear();

                return;
            }

            // TODO: kiểm tra vi phạm cảm biến cân
            //var isValidSensor1 = DIBootstrapper.Init().Resolve<SensorControl>().CheckValidSensorScale1();
            //if (isValidSensor1 == false)
            //{
            //    // Send notification signalr
            //    Program.scaleValues1.Clear();

            //    return;
            //}

            if (Program.IsScalling481 && !Program.IsLockingScale481)
            {
                Program.scaleValues481.Add(currentScaleValue);

                if (Program.scaleValues481.Count > ScaleConfig.MAX_LENGTH_SCALE_VALUE)
                {
                    Program.scaleValues481.RemoveRange(0, 1);
                }

                var isOnDinh = Calculator.CheckBalanceValues(Program.scaleValues481, ScaleConfig.WEIGHT_SAISO);

                //var scaleText = String.Join(",", Program.scaleValues1);
                //logger.Info("Gia tri can 1: " + scaleText);

                logger.Info($"Received 481 data: time={time}, value={value}");

                if (isOnDinh)
                {
                    Program.IsLockingScale481 = true;

                    // 1. Xác định giá trị cân ổn định
                    logger.Info($"1. Can 481 on dinh: " + currentScaleValue);

                    using (var dbContext = new XHTD_Entities())
                    {
                        // 2. Lấy thông tin xe, đơn hàng đang cân
                        var scaleInfo = dbContext.tblScaleOperatings.FirstOrDefault(x => x.ScaleCode == ScaleCode.CODE_SCALE_481);
                        if (scaleInfo == null)
                        {
                            logger.Info($"Khong co ban ghi trong table Scale voi code = {ScaleCode.CODE_SCALE_481}");
                            return;
                        }
                        logger.Info($"2. Phuong tien dang can 481: Vehicle={scaleInfo.Vehicle} - CardNo={scaleInfo.CardNo} - DeliveryCode={scaleInfo.DeliveryCode}");

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
                                //DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnRedTrafficLight(ScaleCode.CODE_SCALE_481);

                                // 5. Đóng barrier
                                logger.Info($"5. Dong barrier IN");
                                DIBootstrapper.Init().Resolve<BarrierControl>().CloseBarrierScaleIn();
                                Thread.Sleep(500);
                                logger.Info($"5. Dong barrier OUT");
                                DIBootstrapper.Init().Resolve<BarrierControl>().CloseBarrierScaleOut();

                                // 6. Gọi iERP API lưu giá trị cân
                                logger.Info($"6. Goi iERP API luu gia tri can");
                                Thread.Sleep(10000);

                                // 7. Bật đèn xanh
                                logger.Info($"7. Bat den xanh");
                                DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnGreenTrafficLight(ScaleCode.CODE_SCALE_481);

                                // 8. Mở barrier
                                logger.Info($"8. Mo barrier IN");
                                DIBootstrapper.Init().Resolve<BarrierControl>().OpenBarrierScaleIn();
                                Thread.Sleep(500);
                                logger.Info($"8. Mo barrier OUT");
                                DIBootstrapper.Init().Resolve<BarrierControl>().OpenBarrierScaleOut();

                                // 9. Update giá trị cân của đơn hàng
                                logger.Info($"9. Update gia tri can vao");
                                await DIBootstrapper.Init().Resolve<WeightBusiness>().UpdateWeightIn(scaleInfo.CardNo, currentScaleValue);

                                // 10. Tiến hành xếp số thứ tự vào máng xuất lấy hàng của xe vừa cân vào xong
                                logger.Info($"10. Xep so thu tu vao mang xuat");
                                await DIBootstrapper.Init().Resolve<IndexOrderBusiness>().SetIndexOrder(scaleInfo.DeliveryCode);

                                // 11. Giải phóng cân: Program.IsScalling = false, update table tblScale
                                logger.Info($"11. Giai phong can 481");
                                Program.IsScalling481 = false;
                                Program.IsLockingScale481 = false;
                                Program.scaleValues481.Clear();
                                await DIBootstrapper.Init().Resolve<ScaleBusiness>().ReleaseScale(ScaleCode.CODE_SCALE_481);
                            }
                            // Đang cân ra
                            else if((bool)scaleInfo.ScaleOut)
                            {
                                // 4. Bật đèn đỏ
                                //logger.Info($"4. Bat den do");
                                //DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnRedTrafficLight(ScaleCode.CODE_SCALE_1);

                                // 5. Đóng barrier
                                logger.Info($"5. Dong barrier IN");
                                DIBootstrapper.Init().Resolve<BarrierControl>().CloseBarrierScaleIn();
                                Thread.Sleep(500);
                                logger.Info($"5. Dong barrier OUT");
                                DIBootstrapper.Init().Resolve<BarrierControl>().CloseBarrierScaleOut();

                                // 6. Gọi iERP API lưu giá trị cân
                                logger.Info($"6. Goi iERP API luu gia tri can");
                                Thread.Sleep(5000);

                                // 7. Bật đèn xanh
                                logger.Info($"7. Bat den xanh");
                                DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnGreenTrafficLight(ScaleCode.CODE_SCALE_1);

                                // 8. Mở barrier
                                logger.Info($"8. Mo barrier IN");
                                DIBootstrapper.Init().Resolve<BarrierControl>().OpenBarrierScaleIn();
                                Thread.Sleep(500);
                                logger.Info($"8. Mo barrier OUT");
                                DIBootstrapper.Init().Resolve<BarrierControl>().OpenBarrierScaleOut();

                                // 9. Update giá trị cân của đơn hàng
                                logger.Info($"9. Update gia tri can ra");
                                await DIBootstrapper.Init().Resolve<WeightBusiness>().UpdateWeightOut(scaleInfo.CardNo, currentScaleValue);

                                // 9. Giải phóng cân: Program.IsScalling = false, update table tblScale
                                logger.Info($"11. Giai phong can 481");
                                Program.IsScalling481 = false;
                                Program.IsLockingScale481 = false;
                                Program.scaleValues481.Clear();
                                await DIBootstrapper.Init().Resolve<ScaleBusiness>().ReleaseScale(ScaleCode.CODE_SCALE_1);
                            }
                        }
                    }
                }
            }
            else
            {
                if (Program.scaleValues481.Count > 5)
                {
                    Program.scaleValues481.Clear();
                }
            }
        }
        
    }
}
