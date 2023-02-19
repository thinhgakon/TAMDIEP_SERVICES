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
using XHTD_SERVICES_TRAM951_1.Devices;
using XHTD_SERVICES_TRAM951_1.Business;
using System.Threading;

namespace XHTD_SERVICES_TRAM951_1.Hubs
{
    public class ScaleHub : Hub
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(ScaleHub));

        protected readonly string SCALE_CODE = ScaleCode.CODE_SCALE_1;

        protected readonly string SCALE_DGT_IN_CODE = ScaleCode.CODE_SCALE_1_DGT_IN;

        protected readonly string SCALE_DGT_OUT_CODE = ScaleCode.CODE_SCALE_1_DGT_OUT;

        protected readonly string SCALE_STATUS = "SCALE_1_STATUS";

        protected readonly string SCALE_BALANCE = "SCALE_1_BALANCE";

        public void SendMessage(string name, string message)
        {
            try
            {
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
                var broadcast = GlobalHost.ConnectionManager.GetHubContext<ScaleHub>();
                broadcast.Clients.All.SendSensor(sensorCode, status);
            }
            catch (Exception ex)
            {

            }
        }

        public void SendFakeRFID(string value)
        {
            Clients.All.SendFakeRFID(value);
        }

        public void Send9511ScaleInfo(DateTime time, string value)
        {
            // Call the broadcastMessage method to update clients.
            Clients.All.Send9511ScaleInfo(time, value);
            ReadDataScale(time, value);
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
        }

        public async void ReadDataScale(DateTime time, string value)
        {
            int currentScaleValue = Int32.Parse(value);

            if (currentScaleValue < ScaleConfig.MIN_WEIGHT_VEHICLE)
            {
                // TODO: giải phóng cân khi xe ra khỏi bàn cân
                // Hàm kiểm tra xe đang ra khỏi bàn cân: khối lượng giảm dần

                SendMessage($"{SCALE_STATUS}", $"Cân đang nghỉ");
                SendMessage($"{SCALE_BALANCE}", "");

                Program.scaleValues.Clear();

                return;
            }

            if (Program.IsScalling)
            {
                SendMessage($"{SCALE_STATUS}", $"Cân tự động");
            }
            else
            {
                SendMessage($"{SCALE_STATUS}", $"Cân thủ công");
                SendMessage($"{SCALE_BALANCE}", "");
            }

            // TODO: kiểm tra vi phạm cảm biến cân
            if (!Program.IsLockingScale)
            {
                var isInValidSensor951 = DIBootstrapper.Init().Resolve<SensorControl>().IsInValidSensorScale951();
                if (isInValidSensor951)
                {
                    SendSensor(SCALE_CODE, "1");

                    Program.scaleValues.Clear();

                    return;
                }
                else
                {
                    SendSensor(SCALE_CODE, "0");
                }
            }

            if (Program.IsScalling && !Program.IsLockingScale)
            {
                Program.scaleValues.Add(currentScaleValue);

                if (Program.scaleValues.Count > ScaleConfig.MAX_LENGTH_SCALE_VALUE)
                {
                    Program.scaleValues.RemoveRange(0, 1);
                }

                var isOnDinh = Calculator.CheckBalanceValues(Program.scaleValues, ScaleConfig.WEIGHT_SAISO);

                //var scaleText = String.Join(",", Program.scaleValues1);
                //logger.Info("Gia tri can 1: " + scaleText);

                logger.Info($"Received 951 data: time={time}, value={value}");

                if (isOnDinh)
                {
                    Program.IsLockingScale = true;

                    // 1. Xác định giá trị cân ổn định
                    logger.Info($"1. Can 951 on dinh: " + currentScaleValue);

                    SendMessage($"{SCALE_BALANCE}", $"{currentScaleValue}");

                    using (var dbContext = new XHTD_Entities())
                    {
                        // 2. Lấy thông tin xe, đơn hàng đang cân
                        var scaleInfo = dbContext.tblScaleOperatings.FirstOrDefault(x => x.ScaleCode == SCALE_CODE);
                        if (scaleInfo == null)
                        {
                            logger.Info($"Khong co ban ghi trong table Scale voi code = {SCALE_CODE}");
                            return;
                        }
                        logger.Info($"2. Phuong tien dang can 951: Vehicle={scaleInfo.Vehicle} - CardNo={scaleInfo.CardNo} - DeliveryCode={scaleInfo.DeliveryCode}");

                        if ((bool)scaleInfo.IsScaling)
                        {
                            // Đang cân vào
                            if ((bool)scaleInfo.ScaleIn)
                            {
                                // 3. Cập nhật khối lượng không tải của phương tiện
                                logger.Info($"3. Cap nhat khoi luong khong tai cua phuong tien");
                                //await DIBootstrapper.Init().Resolve<UnladenWeightBusiness>().UpdateUnladenWeight(scaleInfo.CardNo, currentScaleValue);

                                // 4. Đóng barrier
                                //logger.Info($"4.1. Dong barrier IN");
                                //DIBootstrapper.Init().Resolve<BarrierControl>().CloseBarrierScaleIn();
                                //Thread.Sleep(1000);
                                //logger.Info($"4.2. Dong barrier OUT");
                                //DIBootstrapper.Init().Resolve<BarrierControl>().CloseBarrierScaleOut();

                                // 5. Gọi iERP API lưu giá trị cân
                                logger.Info($"5. Goi iERP API luu gia tri can");
                                var scaleInfoResult = DIBootstrapper.Init().Resolve<DesicionScaleBusiness>().MakeDecisionScaleIn(scaleInfo.DeliveryCode, currentScaleValue);

                                if (scaleInfoResult.Code == "01")
                                {
                                    // Lưu giá trị cân thành công
                                    logger.Info($"Lưu giá trị cân thành công");

                                    Thread.Sleep(7000);

                                    // 6. Mở barrier
                                    //logger.Info($"6.1. Mo barrier IN");
                                    //DIBootstrapper.Init().Resolve<BarrierControl>().OpenBarrierScaleIn();
                                    //Thread.Sleep(1000);
                                    //logger.Info($"6.2. Mo barrier OUT");
                                    //DIBootstrapper.Init().Resolve<BarrierControl>().OpenBarrierScaleOut();

                                    Thread.Sleep(3500);

                                    // 8. Update giá trị cân của đơn hàng
                                    logger.Info($"8. Update gia tri can vao");
                                    //await DIBootstrapper.Init().Resolve<WeightBusiness>().UpdateWeightIn(scaleInfo.CardNo, currentScaleValue);
                                }
                                else
                                {
                                    // Lưu giá trị cân thất bại
                                    logger.Info($"Lưu giá trị cân thất bại");
                                }

                                // 7. Bật đèn xanh
                                logger.Info($"7.1. Bat den xanh chieu vao");
                                DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnGreenTrafficLight(SCALE_DGT_IN_CODE);
                                Thread.Sleep(500);
                                logger.Info($"7.2. Bat den xanh chieu ra");
                                DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnGreenTrafficLight(SCALE_DGT_OUT_CODE);

                                // 9. Giải phóng cân
                                logger.Info($"9. Giai phong can 951");
                                Program.IsScalling = false;
                                Program.IsLockingScale = false;
                                Program.scaleValues.Clear();
                                await DIBootstrapper.Init().Resolve<ScaleBusiness>().ReleaseScale(SCALE_CODE);
                            }
                            // Đang cân ra
                            else if ((bool)scaleInfo.ScaleOut)
                            {
                                // 3. Đóng barrier
                                //logger.Info($"3.1. Dong barrier IN");
                                //DIBootstrapper.Init().Resolve<BarrierControl>().CloseBarrierScaleIn();
                                //Thread.Sleep(1000);
                                //logger.Info($"3.2. Dong barrier OUT");
                                //DIBootstrapper.Init().Resolve<BarrierControl>().CloseBarrierScaleOut();

                                // 4. Gọi iERP API lưu giá trị cân
                                logger.Info($"4. Goi iERP API luu gia tri can");
                                var scaleInfoResult = await DIBootstrapper.Init().Resolve<DesicionScaleBusiness>().MakeDecisionScaleOut(scaleInfo.DeliveryCode, currentScaleValue);

                                if (scaleInfoResult.Code == "01")
                                {
                                    // Lưu giá trị cân thành công
                                    logger.Info($"Lưu giá trị cân thành công");

                                    Thread.Sleep(7000);

                                    // 5. Mở barrier
                                    //logger.Info($"5.1. Mo barrier IN");
                                    //DIBootstrapper.Init().Resolve<BarrierControl>().OpenBarrierScaleIn();
                                    //Thread.Sleep(1000);
                                    //logger.Info($"5.2. Mo barrier OUT");
                                    //DIBootstrapper.Init().Resolve<BarrierControl>().OpenBarrierScaleOut();

                                    Thread.Sleep(3500);

                                    // 7. Update giá trị cân của đơn hàng
                                    logger.Info($"7. Update gia tri can ra");
                                    //await DIBootstrapper.Init().Resolve<WeightBusiness>().UpdateWeightOut(scaleInfo.CardNo, currentScaleValue);
                                }
                                else
                                {
                                    // Lưu giá trị cân thất bại
                                    logger.Info($"Lưu giá trị cân thất bại: {scaleInfoResult.Message}");
                                }

                                // 6. Bật đèn xanh
                                logger.Info($"6.1. Bat den xanh chieu vao");
                                DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnGreenTrafficLight(SCALE_DGT_IN_CODE);
                                Thread.Sleep(500);
                                logger.Info($"6.2. Bat den xanh chieu ra");
                                DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnGreenTrafficLight(SCALE_DGT_OUT_CODE);

                                // 8. Giải phóng cân: Program.IsScalling = false, update table tblScale
                                logger.Info($"8. Giai phong can 951");
                                Program.IsScalling = false;
                                Program.IsLockingScale = false;
                                Program.scaleValues.Clear();
                                await DIBootstrapper.Init().Resolve<ScaleBusiness>().ReleaseScale(SCALE_CODE);
                            }
                        }
                    }
                }
            }
            else
            {
                if (Program.scaleValues.Count > 5)
                {
                    Program.scaleValues.Clear();
                }
            }
        }
    }
}
