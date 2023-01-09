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
using XHTD_SERVICES_TRAM951_OUT.Devices;
using XHTD_SERVICES_TRAM951_OUT.Business;
using System.Threading;

namespace XHTD_SERVICES_TRAM951_OUT.Hubs
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
            ReadDataScale9511(time, value);
        }

        public void Send9512ScaleInfo(DateTime time, string value)
        {
            // Call the broadcastMessage method to update clients.
            Clients.All.Send9512ScaleInfo(time, value);
            ReadDataScale9512(time, value);
        }

        public void SendClinkerScaleInfo(DateTime time, string value)
        {
            // Call the broadcastMessage method to update clients.
            Clients.All.SendClinkerScaleInfo(time, value);
        }

        /*
        * Cân ra
        * 1. Xác định giá trị cân ổn định
        * 2. Lấy thông tin xe, đơn hàng đang cân
        * 3. Bật đèn đỏ
        * 4. Đóng barrier 2 chiều
        * 5. Gọi iERP API lưu giá trị cân
        * 6. Bật đèn xanh
        * 7. Mở barrier 2 chiều
        * 8. Update giá trị cân ra của đơn hàng
        * 9. Giải phóng cân
        */
        public async void ReadDataScale9511(DateTime time, string value)
        {
            //logger.Info($"Received 951-1 data: time={time}, value={value}");

            int currentScaleValue = Int32.Parse(value);
            if(currentScaleValue == 111)
            {
                Program.IsScalling1 = true;
                logger.Info("IsScalling1 true");
            }
            else if(currentScaleValue == 999)
            {
                Program.IsScalling1 = false;
                logger.Info("IsScalling1 false");
            }

            if (currentScaleValue < ScaleConfig.MIN_WEIGHT_VEHICLE)
            {
                // TODO: giải phóng cân khi xe ra khỏi bàn cân
                // Case này cũng xảy ra khi xe vừa vào bàn cân, lúc này chưa nhận diện dc RFID nên chưa xét IsScalling1
                //Program.IsScalling1 = false;
                //Program.IsLockingScale1 = false;
                Program.scaleValues1.Clear();

                return;
            }

            if (Program.IsScalling1 && !Program.IsLockingScale1)
            {
                Program.scaleValues1.Add(currentScaleValue);

                if (Program.scaleValues1.Count > ScaleConfig.MAX_LENGTH_SCALE_VALUE)
                {
                    Program.scaleValues1.RemoveRange(0, 1);
                }

                var isOnDinh = Calculator.CheckBalanceValues(Program.scaleValues1, ScaleConfig.WEIGHT_SAISO);

                //var scaleText = String.Join(",", Program.scaleValues1);
                //logger.Info("Gia tri can: " + scaleText);

                logger.Info($"Received 951-1 data: time={time}, value={value}");

                if (isOnDinh)
                {
                    Program.IsLockingScale1 = true;

                    // 1. Xác định giá trị cân ổn định
                    logger.Info($"1. Can 1 on dinh: " + currentScaleValue);

                    using (var dbContext = new XHTD_Entities())
                    {
                        // 2. Lấy thông tin xe, đơn hàng đang cân
                        var scaleInfo = dbContext.tblScaleOperatings.FirstOrDefault(x => x.ScaleCode == ScaleCode.CODE_SCALE_1);
                        if(scaleInfo == null)
                        {
                            logger.Info($"Khong co ban ghi trong table Scale voi code = {ScaleCode.CODE_SCALE_1}");
                            return;
                        }
                        logger.Info($"2. Phuong tien dang can 1: Vehicle={scaleInfo.Vehicle} - CardNo={scaleInfo.CardNo} - DeliveryCode={scaleInfo.DeliveryCode}");

                        if ((bool)scaleInfo.IsScaling)
                        {
                            // Đang cân ra
                            if ((bool)scaleInfo.ScaleOut)
                            {
                                // 3. Cập nhật khối lượng không tải của phương tiện
                                //logger.Info($"3. Cap nhat khoi luong khong tai");
                                //await DIBootstrapper.Init().Resolve<UnladenWeightBusiness>().UpdateUnladenWeight(scaleInfo.CardNo, currentScaleValue);

                                // 4. Bật đèn đỏ
                                //logger.Info($"4. Bat den do");
                                //DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnRedTrafficLight(ScaleCode.CODE_SCALE_1);

                                // 5. Đóng barrier
                                logger.Info($"5. Dong barrier IN");
                                DIBootstrapper.Init().Resolve<BarrierControl>().CloseBarrierScaleIn1();
                                logger.Info($"5. Dong barrier OUT");
                                DIBootstrapper.Init().Resolve<BarrierControl>().CloseBarrierScaleOut1();

                                // 6. Gọi iERP API lưu giá trị cân
                                logger.Info($"6. Goi iERP API luu gia tri can");
                                Thread.Sleep(5000);

                                // 7. Bật đèn xanh
                                logger.Info($"7. Bat den xanh");
                                DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnGreenTrafficLight(ScaleCode.CODE_SCALE_1);

                                // 8. Mở barrier
                                logger.Info($"8. Mo barrier IN");
                                DIBootstrapper.Init().Resolve<BarrierControl>().OpenBarrierScaleIn1();
                                logger.Info($"8. Mo barrier OUT");
                                DIBootstrapper.Init().Resolve<BarrierControl>().OpenBarrierScaleOut1();

                                // 9. Update giá trị cân của đơn hàng
                                logger.Info($"9. Update gia tri can ra");
                                await DIBootstrapper.Init().Resolve<WeightBusiness>().UpdateWeightOut(scaleInfo.CardNo, currentScaleValue);

                                // 10. Tiến hành xếp số thứ tự vào máng xuất lấy hàng của xe vừa cân vào xong
                                //logger.Info($"10. Xep so thu tu vao mang xuat");
                                //await DIBootstrapper.Init().Resolve<IndexOrderBusiness>().SetIndexOrder(scaleInfo.DeliveryCode);

                                // 9. Giải phóng cân: Program.IsScalling = false, update table tblScale
                                logger.Info($"11. Giai phong can 1");
                                Program.IsScalling1 = false;
                                Program.IsLockingScale1 = false;
                                Program.scaleValues1.Clear();
                                await DIBootstrapper.Init().Resolve<ScaleBusiness>().ReleaseScale(ScaleCode.CODE_SCALE_1);
                            }
                        }
                    }
                }
            }
            else
            {
                if (Program.scaleValues1.Count > 5)
                {
                    Program.scaleValues1.Clear();
                }
            }
        }

        public async void ReadDataScale9512(DateTime time, string value)
        {
            //logger.Info($"Received 951-2 data: time={time}, value={value}");

            int currentScaleValue = Int32.Parse(value);
            if (currentScaleValue == 111)
            {
                Program.IsScalling2 = true;
                logger.Info("IsScalling2 true");
            }
            else if (currentScaleValue == 999)
            {
                Program.IsScalling2 = false;
                logger.Info("IsScalling2 false");
            }

            if (currentScaleValue < ScaleConfig.MIN_WEIGHT_VEHICLE)
            {
                // TODO: giải phóng cân khi xe ra khỏi bàn cân
                // Case này cũng xảy ra khi xe vừa vào bàn cân, lúc này chưa nhận diện dc RFID nên chưa xét IsScalling2
                //Program.IsScalling2 = false;
                //Program.IsLockingScale2 = false;
                Program.scaleValues2.Clear();

                return;
            }

            if (Program.IsScalling2 && !Program.IsLockingScale2)
            {
                Program.scaleValues2.Add(currentScaleValue);

                if (Program.scaleValues2.Count > ScaleConfig.MAX_LENGTH_SCALE_VALUE)
                {
                    Program.scaleValues2.RemoveRange(0, 1);
                }

                var isOnDinh = Calculator.CheckBalanceValues(Program.scaleValues2, ScaleConfig.WEIGHT_SAISO);

                //var scaleText = String.Join(",", Program.scaleValues2);
                //logger.Info("Gia tri can: " + scaleText);

                logger.Info($"Received 951-2 data: time={time}, value={value}");

                if (isOnDinh)
                {
                    Program.IsLockingScale2 = true;

                    // 1. Xác định giá trị cân ổn định
                    logger.Info($"1. Can 2 on dinh: " + currentScaleValue);

                    using (var dbContext = new XHTD_Entities())
                    {
                        // 2. Lấy thông tin xe, đơn hàng đang cân
                        var scaleInfo = dbContext.tblScaleOperatings.FirstOrDefault(x => x.ScaleCode == ScaleCode.CODE_SCALE_2);
                        if (scaleInfo == null)
                        {
                            logger.Info($"Khong co ban ghi trong table Scale voi code = {ScaleCode.CODE_SCALE_2}");
                            return;
                        }
                        logger.Info($"2. Phuong tien dang can 2: Vehicle={scaleInfo.Vehicle} - CardNo={scaleInfo.CardNo} - DeliveryCode={scaleInfo.DeliveryCode}");

                        if ((bool)scaleInfo.IsScaling)
                        {
                            // Đang cân ra
                            if ((bool)scaleInfo.ScaleOut)
                            {
                                // 3. Cập nhật khối lượng không tải của phương tiện
                                //await DIBootstrapper.Init().Resolve<UnladenWeightBusiness>().UpdateUnladenWeight(scaleInfo.CardNo, currentScaleValue);

                                // 4. Bật đèn đỏ
                                //logger.Info($"4. Bat den do");
                                //DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnRedTrafficLight(ScaleCode.CODE_SCALE_2);

                                // 5. Đóng barrier
                                logger.Info($"5. Dong barrier IN");
                                DIBootstrapper.Init().Resolve<BarrierControl>().CloseBarrierScaleIn2();
                                logger.Info($"5. Dong barrier OUT");
                                DIBootstrapper.Init().Resolve<BarrierControl>().CloseBarrierScaleOut2();

                                // 6. Gọi iERP API lưu giá trị cân
                                logger.Info($"6. Goi iERP API luu gia tri can");
                                Thread.Sleep(10000);

                                // 7. Bật đèn xanh
                                logger.Info($"7. Bat den xanh");
                                DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnGreenTrafficLight(ScaleCode.CODE_SCALE_2);

                                // 8. Mở barrier
                                logger.Info($"8. Mo barrier IN");
                                DIBootstrapper.Init().Resolve<BarrierControl>().OpenBarrierScaleIn2();
                                logger.Info($"8. Mo barrier OUT");
                                DIBootstrapper.Init().Resolve<BarrierControl>().OpenBarrierScaleOut2();

                                // 9. Update giá trị cân của đơn hàng
                                logger.Info($"9. Update gia tri can ra");
                                await DIBootstrapper.Init().Resolve<WeightBusiness>().UpdateWeightOut(scaleInfo.CardNo, currentScaleValue);

                                // 10. Tiến hành xếp số thứ tự vào máng xuất lấy hàng của xe vừa cân vào xong
                                //logger.Info($"10. Xep so thu tu vao mang xuat");
                                //await DIBootstrapper.Init().Resolve<IndexOrderBusiness>().SetIndexOrder(scaleInfo.DeliveryCode);

                                // 9. Giải phóng cân: Program.IsScalling = false, update table tblScale
                                logger.Info($"11. Giai phong can 2");
                                Program.IsScalling2 = false;
                                Program.IsLockingScale2 = false;
                                Program.scaleValues2.Clear();
                                await DIBootstrapper.Init().Resolve<ScaleBusiness>().ReleaseScale(ScaleCode.CODE_SCALE_2);
                            }
                        }
                    }
                }
            }
            else
            {
                if (Program.scaleValues1.Count > 5)
                {
                    Program.scaleValues1.Clear();
                }
            }
        }
    }
}
