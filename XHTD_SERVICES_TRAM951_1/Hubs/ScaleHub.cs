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
using System.Threading.Tasks;

namespace XHTD_SERVICES_TRAM951_1.Hubs
{
    public class ScaleHub : Hub
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ScaleHub));

        protected readonly string SCALE_CODE = ScaleCode.CODE_SCALE_1;

        protected readonly string SCALE_DGT_IN_CODE = ScaleCode.CODE_SCALE_1_DGT_IN;

        protected readonly string SCALE_DGT_OUT_CODE = ScaleCode.CODE_SCALE_1_DGT_OUT;

        protected readonly string SCALE_STATUS = "SCALE_1_STATUS";

        protected readonly string SCALE_BALANCE = "SCALE_1_BALANCE";

        protected readonly string SCALE_DELIVERY_CODE = "TRAM951_1_DELIVERY_CODE";

        protected readonly string VEHICLE_STATUS = "VEHICLE_1_STATUS";

        protected readonly string ENABLED_RFID_STATUS = "ENABLED_RFID_1_STATUS";

        protected readonly string ENABLED_RFID_TIME = "ENABLED_RFID_1_TIME";

        protected readonly string LOCKING_RFID_STATUS = "LOCKING_RFID_1_STATUS";

        private readonly string SCALE_IS_LOCKING_RFID = "SCALE_1_IS_LOCKING_RFID";

        protected readonly int TIME_TO_READ_RFID = 30;

        protected readonly int TIME_TO_RELEASE_SCALE = 5000;

        protected Notification _notification = new Notification();

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

        public void SendNotificationCBV(int status, string inout, string cardNo, string message, string deliveryCode = "")
        {
            Clients.All.SendNotificationCBV(status, inout, cardNo, message, deliveryCode);
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
            try
            {
                int currentScaleValue = Int32.Parse(value);

                // Check lock RFID
                if (currentScaleValue > ScaleConfig.MIN_WEIGHT_TO_SCALE && Program.IsLockingRfid == false && Program.IsEnabledRfid == false)
                {
                    Program.IsEnabledRfid = true;
                    Program.EnabledRfidTime = time;
                }

                if (currentScaleValue < ScaleConfig.MIN_WEIGHT_TO_SCALE || Program.IsLockingRfid == true)
                {
                    Program.IsEnabledRfid = false;
                }

                if (currentScaleValue < ScaleConfig.MIN_WEIGHT_TO_SCALE)
                {
                    Program.IsLockingRfid = false;
                    Program.EnabledRfidTime = null;
                }

                if (Program.IsEnabledRfid && Program.EnabledRfidTime != null && Program.EnabledRfidTime < time.AddSeconds(-1 * TIME_TO_READ_RFID))
                {
                    Program.IsLockingRfid = true;
                }

                SendMessage($"{ENABLED_RFID_STATUS}", $"{Program.IsEnabledRfid}");
                SendScale1Message($"{ENABLED_RFID_STATUS}", $"{Program.IsEnabledRfid}");

                SendMessage($"{ENABLED_RFID_TIME}", $"{Program.EnabledRfidTime}");
                SendScale1Message($"{ENABLED_RFID_TIME}", $"{Program.EnabledRfidTime}");

                SendMessage($"{LOCKING_RFID_STATUS}", $"{Program.IsLockingRfid}");
                SendScale1Message($"{LOCKING_RFID_STATUS}", $"{Program.IsLockingRfid}");
                // End Check lock RFID

                if (currentScaleValue < ScaleConfig.MIN_WEIGHT_VEHICLE)
                {
                    // TODO: giải phóng cân khi xe ra khỏi bàn cân
                    // Hàm kiểm tra xe đang ra khỏi bàn cân: khối lượng giảm dần

                    SendMessage($"{SCALE_STATUS}", $"Cân đang nghỉ");
                    SendMessage($"{SCALE_BALANCE}", "");

                    SendScale1Message($"{SCALE_STATUS}", $"Cân đang nghỉ");
                    SendScale1Message($"{SCALE_BALANCE}", "   ");
                    SendScale1Message($"{VEHICLE_STATUS}", "    ");
                    SendScale1Message($"{SCALE_DELIVERY_CODE}", "  ");
                    SendScale1Message("Notification", "    ");
                    SendScale1Message($"{SCALE_IS_LOCKING_RFID}", "  ");

                    Program.scaleValues.Clear();
                    return;
                }

                if (Program.IsScalling)
                {
                    SendMessage($"{SCALE_STATUS}", $"Đang cân tự động");
                    SendMessage($"{VEHICLE_STATUS}", $"{Program.InProgressVehicleCode}");
                    SendMessage($"{SCALE_DELIVERY_CODE}", $"{Program.InProgressDeliveryCode}");

                    SendScale1Message($"{SCALE_STATUS}", $"Đang cân tự động");
                    SendScale1Message($"{VEHICLE_STATUS}", $"{Program.InProgressVehicleCode}");
                    SendScale1Message($"{SCALE_DELIVERY_CODE}", $"{Program.InProgressDeliveryCode}");
                }
                else
                {
                    SendMessage($"{SCALE_STATUS}", $"Đang cân thủ công");
                    SendMessage($"{SCALE_BALANCE}", "  ");
                    SendMessage("Notification", "  ");

                    SendScale1Message($"{SCALE_STATUS}", $"Đang cân thủ công");
                    SendScale1Message($"{SCALE_BALANCE}", "");
                    SendScale1Message("Notification", "  ");
                }

                // TODO: kiểm tra vi phạm cảm biến cân
                if (Program.IsSensorActive)
                {
                    if (!Program.IsLockingScale)
                    {
                        var isInValidSensor = DIBootstrapper.Init().Resolve<SensorControl>().IsInValidSensorScale();
                        if (isInValidSensor)
                        {
                            SendSensor(SCALE_CODE, "1");
                            SendScale1Sensor(SCALE_CODE, "1");

                            Program.scaleValues.Clear();

                            return;
                        }
                        else
                        {
                            SendSensor(SCALE_CODE, "0");
                            SendScale1Sensor(SCALE_CODE, "0");
                        }
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

                    _logger.Info($"Received {SCALE_CODE} data: time={time}, value={value}");

                    if (isOnDinh)
                    {
                        Program.IsLockingScale = true;

                        // 1. Xác định giá trị cân ổn định
                        _logger.Info($"1. Can {SCALE_CODE} on dinh: " + currentScaleValue);

                        SendMessage($"{SCALE_BALANCE}", $"{currentScaleValue}");
                        SendScale1Message($"{SCALE_BALANCE}", $"{currentScaleValue}");

                        using (var dbContext = new XHTD_Entities())
                        {
                            // 2. Lấy thông tin xe, đơn hàng đang cân
                            var scaleInfo = dbContext.tblScaleOperatings.FirstOrDefault(x => x.ScaleCode == SCALE_CODE && (bool)x.IsScaling);
                            if (scaleInfo == null)
                            {
                                _logger.Info($"2. Khong co thong tin xe dang can trong table Scale voi code = {SCALE_CODE}");

                                SendMessage("Notification", $"Không có thông tin xe đang cân. Vui lòng xử lý thủ công!");
                                SendScale1Message("Notification", $"Không có thông tin xe đang cân. Vui lòng xử lý thủ công!");

                                Thread.Sleep(TIME_TO_RELEASE_SCALE);
                                await ReleaseScale();
                                return;
                            }

                            var currentOrder = await DIBootstrapper.Init().Resolve<OrderBusiness>().GetDetail(scaleInfo.DeliveryCode);
                            if (currentOrder == null)
                            {
                                _logger.Info($"2. Khong co thong tin don hang dang can voi code = {scaleInfo.DeliveryCode}");

                                SendMessage("Notification", $"Không có thông tin đơn hàng {scaleInfo.DeliveryCode} đang cân. Vui lòng xử lý thủ công!");
                                SendScale1Message("Notification", $"Không có thông tin đơn hàng {scaleInfo.DeliveryCode} đang cân. Vui lòng xử lý thủ công!");

                                Thread.Sleep(TIME_TO_RELEASE_SCALE);
                                await ReleaseScale();
                                return;
                            }

                            _logger.Info($"2. Phuong tien dang can {SCALE_CODE}: Vehicle={scaleInfo.Vehicle} - CardNo={scaleInfo.CardNo} - DeliveryCode={scaleInfo.DeliveryCode}");

                            var currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                            var unladenWeight = DIBootstrapper.Init().Resolve<UnladenWeightBusiness>().GetUnladenWeight(scaleInfo.Vehicle);

                            var ladenWeight = unladenWeight + currentOrder.SumNumber * 1000;

                            // Đang cân vào
                            if ((bool)scaleInfo.ScaleIn)
                            {
                                // Độ lệch khối lượng không tải trung bình và giá trị cân bì hiện tại
                                var unladenWeightSaiSo = Math.Abs(unladenWeight - currentScaleValue);

                                _logger.Info($"2.1. Khoi luong khong tai trung binh: {unladenWeight}");
                                _logger.Info($"2.2. Sai so khoi luong khong tai: {unladenWeightSaiSo}");

                                if (unladenWeight > 0 && unladenWeightSaiSo > ScaleConfig.UNLADEN_WEIGHT_SAISO)
                                {
                                    _logger.Info($"2.3. Sai so vuot qua {ScaleConfig.UNLADEN_WEIGHT_SAISO}. Nghi ngờ cân nhầm xe. Vui lòng xử lý thủ công!");

                                    SendMessage("Notification", $"Phát hiện khối lượng cân không hợp lệ, sai số vượt quá {ScaleConfig.UNLADEN_WEIGHT_SAISO}. Vui lòng xử lý thủ công!");
                                    SendScale1Message("Notification", $"Phát hiện khối lượng cân không hợp lệ, sai số vượt quá {ScaleConfig.UNLADEN_WEIGHT_SAISO}. Vui lòng xử lý thủ công!");

                                    Thread.Sleep(TIME_TO_RELEASE_SCALE);
                                    await ReleaseScale();
                                    return;
                                }

                                // 3. Cập nhật khối lượng không tải của phương tiện
                                _logger.Info($"3. Cap nhat khoi luong khong tai cua phuong tien");
                                await DIBootstrapper.Init().Resolve<UnladenWeightBusiness>().UpdateUnladenWeight(scaleInfo.CardNo, currentScaleValue);

                                // 5. Gọi iERP API lưu giá trị cân
                                _logger.Info($"5. Goi iERP API luu gia tri can");
                                var scaleInfoResult = DIBootstrapper.Init().Resolve<DesicionScaleBusiness>().MakeDecisionScaleIn(scaleInfo.DeliveryCode, currentScaleValue);

                                if (scaleInfoResult.Code == "01")
                                {
                                    // Lưu giá trị cân thành công
                                    SendMessage("Notification", $"{scaleInfoResult.Message}");
                                    SendScale1Message("Notification", $"{scaleInfoResult.Message}");

                                    _logger.Info($"5.1. Lưu giá trị cân thành công");

                                    // 6. Update gia tri can va trang thai Can vao
                                    _logger.Info($"6. Update gia tri can va trang thai Can vao");

                                    if (currentOrder.CatId == OrderCatIdCode.CLINKER
                                     || currentOrder.TypeXK == OrderTypeXKCode.JUMBO
                                     || currentOrder.TypeXK == OrderTypeXKCode.SLING)
                                    {
                                        _logger.Info($"6.1. Don hang CLINKER hoac XK: CatId = {currentOrder.CatId}, TypeXK = {currentOrder.TypeXK}");

                                        _logger.Info($"6.2. Update gia tri can vao toan bo don hang theo vehicle code");
                                        await DIBootstrapper.Init().Resolve<WeightBusiness>().UpdateWeightInByVehicleCode(scaleInfo.Vehicle, currentScaleValue);

                                        _logger.Info($"6.3. Update trạng thái cân vào toan bo don hang theo vehicle code");
                                        await DIBootstrapper.Init().Resolve<StepBusiness>().UpdateOrderConfirm3ByVehicleCode(scaleInfo.Vehicle);
                                    }
                                    else
                                    {
                                        _logger.Info($"6.1. Don hang thong thuong: CatId = {currentOrder.CatId}, TypeXK = {currentOrder.TypeXK}");

                                        _logger.Info($"6.2. Update gia tri can vao");
                                        await DIBootstrapper.Init().Resolve<WeightBusiness>().UpdateWeightIn(scaleInfo.DeliveryCode, currentScaleValue);

                                        _logger.Info($"6.3. Update trạng thái cân vào");
                                        await DIBootstrapper.Init().Resolve<StepBusiness>().UpdateOrderConfirm3(scaleInfo.DeliveryCode);

                                        //DIBootstrapper.Init().Resolve<Notification>().SendInforNotification($"{currentOrder.DriverUserName}", $"{scaleInfo.DeliveryCode} cân vào tự động lúc {currentTime}");
                                    }

                                    Thread.Sleep(3000);

                                    // 8. Bật đèn xanh
                                    _logger.Info($"8. Bat den xanh");
                                    TurnOnGreenTrafficLight();
                                }
                                else
                                {
                                    // Lưu giá trị cân thất bại
                                    SendMessage("Notification", $"{scaleInfoResult.Message}. Vui lòng xử lý thủ công!");
                                    SendScale1Message("Notification", $"{scaleInfoResult.Message}. Vui lòng xử lý thủ công!");

                                    _logger.Info($"5.1. Lưu giá trị cân thất bại: Code={scaleInfoResult.Code} Message={scaleInfoResult.Message}");

                                    Thread.Sleep(TIME_TO_RELEASE_SCALE);
                                }

                                // 9. Giải phóng cân
                                _logger.Info($"9. Giai phong can {SCALE_CODE}");
                                await ReleaseScale();
                            }
                            // Đang cân ra
                            else if ((bool)scaleInfo.ScaleOut)
                            {
                                // Độ lệch khối lượng hiện tại và khối lượng có tải dự kiến (không tải trung bình + số lượng đặt hàng)
                                var ladenWeightSaiSo = Math.Abs((int)ladenWeight - currentScaleValue);

                                _logger.Info($"2.1. Khoi luong khong tai trung binh: {unladenWeight}");
                                _logger.Info($"2.2. Khoi luong đặt hàng: {currentOrder.SumNumber}");
                                _logger.Info($"2.3. Khoi luong có tải dự kiến: {ladenWeight}");
                                _logger.Info($"2.4. Sai so khoi luong có tải: {ladenWeightSaiSo}");

                                if (unladenWeight > 0 && ladenWeightSaiSo > ScaleConfig.LADEN_WEIGHT_SAISO)
                                {
                                    _logger.Info($"2.3. Sai so vuot qua {ScaleConfig.LADEN_WEIGHT_SAISO}. Nghi ngờ cân nhầm xe. Vui lòng xử lý thủ công!");

                                    SendMessage("Notification", $"Phát hiện khối lượng cân không hợp lệ, sai số vượt quá {ScaleConfig.LADEN_WEIGHT_SAISO}. Vui lòng xử lý thủ công!");

                                    Thread.Sleep(TIME_TO_RELEASE_SCALE);
                                    await ReleaseScale();
                                    return;
                                }

                                // 4. Gọi iERP API lưu giá trị cân
                                _logger.Info($"4. Goi iERP API luu gia tri can");
                                var scaleInfoResult = await DIBootstrapper.Init().Resolve<DesicionScaleBusiness>().MakeDecisionScaleOut(scaleInfo.DeliveryCode, currentScaleValue);

                                if (scaleInfoResult.Code == "01")
                                {
                                    // Lưu giá trị cân thành công
                                    SendMessage("Notification", $"{scaleInfoResult.Message}");

                                    _logger.Info($"4.1. Lưu giá trị cân thành công");

                                    // 5. Update gia tri can va trang thai Can ra
                                    _logger.Info($"5. Update gia tri can va trang thai Can ra");
                                    _logger.Info($"5.1. Update gia tri can ra");
                                    await DIBootstrapper.Init().Resolve<WeightBusiness>().UpdateWeightOut(scaleInfo.DeliveryCode, currentScaleValue);

                                    _logger.Info($"5.2. Update trạng thái cân ra");
                                    await DIBootstrapper.Init().Resolve<StepBusiness>().UpdateOrderConfirm7(scaleInfo.DeliveryCode);

                                    //DIBootstrapper.Init().Resolve<Notification>().SendInforNotification($"{currentOrder.DriverUserName}", $"{scaleInfo.DeliveryCode} cân ra tự động lúc {currentTime}");

                                    Thread.Sleep(3000);

                                    // 7. Bật đèn xanh
                                    _logger.Info($"7. Bat den xanh");
                                    TurnOnGreenTrafficLight();
                                }
                                else
                                {
                                    // Lưu giá trị cân thất bại
                                    SendMessage("Notification", $"{scaleInfoResult.Message}. Vui lòng xử lý thủ công!");

                                    _logger.Info($"4.1. Lưu giá trị cân thất bại: Code={scaleInfoResult.Code} Message={scaleInfoResult.Message}");

                                    Thread.Sleep(TIME_TO_RELEASE_SCALE);
                                }

                                // 8. Giải phóng cân: Program.IsScalling = false, update table tblScale
                                _logger.Info($"8. Giai phong can {SCALE_CODE}");
                                await ReleaseScale();
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
            catch (Exception ex)
            {
                _logger.Error($@"Co loi khi xu ly scale data: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }

        public async Task ReleaseScale()
        {
            Program.IsScalling = false;
            Program.IsLockingScale = false;
            Program.scaleValues.Clear();
            await DIBootstrapper.Init().Resolve<ScaleBusiness>().ReleaseScale(SCALE_CODE);
        }

        public void TurnOnGreenTrafficLight(bool isHasNotification = false)
        {
            _logger.Info($@"Bật den xanh chieu vao");
            if (DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnGreenTrafficLight(SCALE_DGT_IN_CODE))
            {
                if (isHasNotification)
                {
                    SendMessage("Notification", $"Bật đèn xanh chiều vào thành công");
                    SendScale1Message("Notification", $"Bật đèn xanh chiều vào thành công");
                }
                _logger.Info($@"Bật thành công");
            }
            else
            {
                if (isHasNotification)
                {
                    SendMessage("Notification", $"Bật đèn xanh chiều vào thất bại");
                    SendScale1Message("Notification", $"Bật đèn xanh chiều vào thất bại");
                }
                _logger.Info($@"Bật thất bại");
            }

            Thread.Sleep(500);

            _logger.Info($@"Bật den xanh chieu ra");
            if (DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnGreenTrafficLight(SCALE_DGT_OUT_CODE))
            {
                if (isHasNotification)
                {
                    SendMessage("Notification", $"Bật đèn xanh chiều ra thành công");
                    SendScale1Message("Notification", $"Bật đèn xanh chiều ra thành công");
                }
                _logger.Info($@"Bật thành công");
            }
            else
            {
                if (isHasNotification)
                {
                    SendMessage("Notification", $"Bật đèn xanh chiều ra thất bại");
                    SendScale1Message("Notification", $"Bật đèn xanh chiều ra thất bại");
                }
                _logger.Info($@"Bật thất bại");
            }
        }

        private void SendScale1Message(string name, string message)
        {
            try
            {
                _notification.SendScale1Message(name, message);
            }
            catch (Exception ex)
            {
                _logger.Info($"SendScale1Message Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }

        private void SendScale1Sensor(string sensorCode, string status)
        {
            try
            {
                _notification.SendScale1Sensor(sensorCode, status);
            }
            catch (Exception ex)
            {
                _logger.Info($"SendScale1Message Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }
    }
}
