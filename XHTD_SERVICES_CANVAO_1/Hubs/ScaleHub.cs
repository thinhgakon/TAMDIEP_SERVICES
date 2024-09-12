using System;
using Microsoft.AspNet.SignalR;
using log4net;
using XHTD_SERVICES.Helper;
using System.Linq;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Common;
using Autofac;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES_CANVAO_1.Devices;
using XHTD_SERVICES_CANVAO_1.Business;
using XHTD_SERVICES.Data.Repositories;

namespace XHTD_SERVICES_CANVAO_1.Hubs
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

                    Program.IsLockingScale = false;
                }

                if (Program.IsEnabledRfid && Program.EnabledRfidTime != null && Program.EnabledRfidTime < time.AddSeconds(-1 * TIME_TO_READ_RFID))
                {
                    Program.IsLockingRfid = true;
                }

                SendMessage($"{ENABLED_RFID_STATUS}", $"{Program.IsEnabledRfid}");
                SendMessageAPI($"{ENABLED_RFID_STATUS}", $"{Program.IsEnabledRfid}");

                SendMessage($"{ENABLED_RFID_TIME}", $"{Program.EnabledRfidTime}");
                SendMessageAPI($"{ENABLED_RFID_TIME}", $"{Program.EnabledRfidTime}");

                SendMessage($"{LOCKING_RFID_STATUS}", $"{Program.IsLockingRfid}");
                SendMessageAPI($"{LOCKING_RFID_STATUS}", $"{Program.IsLockingRfid}");
                // End Check lock RFID

                if (currentScaleValue < ScaleConfig.MIN_WEIGHT_VEHICLE)
                {
                    // TODO: giải phóng cân khi xe ra khỏi bàn cân
                    // Hàm kiểm tra xe đang ra khỏi bàn cân: khối lượng giảm dần

                    SendMessage($"{SCALE_STATUS}", $"Cân đang nghỉ");
                    SendMessageAPI($"{SCALE_STATUS}", $"Cân đang nghỉ");

                    SendMessage($"{SCALE_BALANCE}", "");
                    SendMessageAPI($"{SCALE_BALANCE}", "   ");

                    SendMessageAPI($"{VEHICLE_STATUS}", "    ");
                    SendMessageAPI($"{SCALE_DELIVERY_CODE}", "  ");
                    SendMessageAPI("Notification", "    ");
                    SendMessageAPI("WarningNotification ", "    ");
                    SendMessageAPI($"{SCALE_IS_LOCKING_RFID}", "  ");

                    Program.scaleValues.Clear();
                    Program.scaleValuesForResetLight.Clear();

                    Program.InProgressDeliveryCode = null;
                    Program.InProgressVehicleCode = null;

                    return;
                }
                else
                {
                    Program.scaleValuesForResetLight.Add(currentScaleValue);
                }

                if (Program.IsScalling)
                {
                    SendMessage($"{SCALE_STATUS}", $"Đang cân tự động");
                    SendMessageAPI($"{SCALE_STATUS}", $"Đang cân tự động");

                    SendMessage($"{VEHICLE_STATUS}", $"{Program.InProgressVehicleCode}");
                    SendMessageAPI($"{VEHICLE_STATUS}", $"{Program.InProgressVehicleCode}");

                    SendMessage($"{SCALE_DELIVERY_CODE}", $"{Program.InProgressDeliveryCode}");
                    SendMessageAPI($"{SCALE_DELIVERY_CODE}", $"{Program.InProgressDeliveryCode}");
                }
                else
                {
                    SendMessage($"{SCALE_STATUS}", $"Đang cân thủ công");
                    SendMessageAPI($"{SCALE_STATUS}", $"Đang cân thủ công");
                }

                // TODO: kiểm tra vi phạm cảm biến cân
                if (Program.IsSensorActive)
                {
                    if (!Program.IsLockingScale)
                    {
                        var isInValidSensor = DIBootstrapper.Init().Resolve<S7SensorControl>().IsInValidSensorScale();
                        if (isInValidSensor)
                        {
                            SendSensor(SCALE_CODE, "1");
                            SendSensorAPI(SCALE_CODE, "1");

                            Program.scaleValues.Clear();

                            return;
                        }
                        else
                        {
                            SendSensor(SCALE_CODE, "0");
                            SendSensorAPI(SCALE_CODE, "0");
                        }
                    }
                }

                WriteLogInfo($"IsScalling: {Program.IsScalling} ----- IsLockingScale: {Program.IsLockingScale}");

                if (Program.IsScalling && !Program.IsLockingScale)
                {
                    WriteLogInfo($"Received {SCALE_CODE} data: time={time}, value={value}");

                    Program.scaleValues.Add(currentScaleValue);

                    if (Program.scaleValues.Count > ScaleConfig.MAX_LENGTH_SCALE_VALUE)
                    {
                        Program.scaleValues.RemoveRange(0, 1);
                    }

                    var isOnDinh = Calculator.CheckBalanceValues(Program.scaleValues, ScaleConfig.WEIGHT_SAISO);

                    if (isOnDinh)
                    {
                        Program.IsLockingScale = true;

                        // 1. Xác định giá trị cân ổn định
                        WriteLogInfo($"1. Can {SCALE_CODE} on dinh: " + currentScaleValue);

                        SendMessage($"{SCALE_BALANCE}", $"{currentScaleValue}");
                        SendMessageAPI($"{SCALE_BALANCE}", $"{currentScaleValue}");

                        using (var dbContext = new XHTD_Entities())
                        {
                            // 2. Lấy thông tin xe, đơn hàng đang cân
                            var scaleInfo = dbContext.tblScaleOperatings.FirstOrDefault(x => x.ScaleCode == SCALE_CODE && (bool)x.IsScaling);
                            if (scaleInfo == null)
                            {
                                WriteLogInfo($"2. Khong co thong tin xe dang can trong table Scale voi code = {SCALE_CODE}");

                                SendMessage("WarningNotification ", $"Không có thông tin xe đang cân. Vui lòng xử lý thủ công!");
                                SendMessageAPI("WarningNotification ", $"Không có thông tin xe đang cân. Vui lòng xử lý thủ công!");

                                Thread.Sleep(TIME_TO_RELEASE_SCALE);
                                await ReleaseScale();
                                return;
                            }

                            var currentOrder = await DIBootstrapper.Init().Resolve<OrderBusiness>().GetDetail(scaleInfo.DeliveryCode);
                            if (currentOrder == null)
                            {
                                WriteLogInfo($"2. Khong co thong tin don hang dang can voi code = {scaleInfo.DeliveryCode}");

                                SendMessage("WarningNotification", $"Không có thông tin đơn hàng {scaleInfo.DeliveryCode} đang cân. Vui lòng xử lý thủ công!");
                                SendMessageAPI("WarningNotification", $"Không có thông tin đơn hàng {scaleInfo.DeliveryCode} đang cân. Vui lòng xử lý thủ công!");

                                Thread.Sleep(TIME_TO_RELEASE_SCALE);
                                await ReleaseScale();
                                return;
                            }

                            WriteLogInfo($"2. Phuong tien dang can {SCALE_CODE}: Vehicle={scaleInfo.Vehicle} - CardNo={scaleInfo.CardNo} - DeliveryCode={scaleInfo.DeliveryCode}");

                            var currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                            var unladenWeight = DIBootstrapper.Init().Resolve<UnladenWeightBusiness>().GetUnladenWeight(scaleInfo.Vehicle);

                            var ladenWeight = unladenWeight + currentOrder.SumNumber * 1000;

                            // Đang cân vào
                            if ((bool)scaleInfo.ScaleIn)
                            {
                                // Độ lệch khối lượng không tải trung bình và giá trị cân bì hiện tại
                                var unladenWeightSaiSo = Math.Abs(unladenWeight - currentScaleValue);

                                WriteLogInfo($"2.1. Khoi luong khong tai trung binh: {unladenWeight}");
                                WriteLogInfo($"2.2. Sai so khoi luong khong tai: {unladenWeightSaiSo}");

                                if (unladenWeight > 0 && unladenWeightSaiSo > ScaleConfig.UNLADEN_WEIGHT_SAISO)
                                {
                                    WriteLogInfo($"2.3. Sai so vuot qua {ScaleConfig.UNLADEN_WEIGHT_SAISO}. Nghi ngờ cân nhầm xe. Vui lòng xử lý thủ công!");

                                    SendMessage("WarningNotification", $"Phát hiện khối lượng cân không hợp lệ, sai số vượt quá {ScaleConfig.UNLADEN_WEIGHT_SAISO}. Vui lòng xử lý thủ công!");
                                    SendMessageAPI("WarningNotification", $"Phát hiện khối lượng cân không hợp lệ, sai số vượt quá {ScaleConfig.UNLADEN_WEIGHT_SAISO}. Vui lòng xử lý thủ công!");

                                    Thread.Sleep(TIME_TO_RELEASE_SCALE);
                                    await ReleaseScale();
                                    return;
                                }

                                // 3. Cập nhật khối lượng không tải của phương tiện
                                WriteLogInfo($"3. Cap nhat khoi luong khong tai cua phuong tien");
                                await DIBootstrapper.Init().Resolve<UnladenWeightBusiness>().UpdateUnladenWeight(scaleInfo.CardNo, currentScaleValue);

                                // 5. Gọi iERP API lưu giá trị cân
                                WriteLogInfo($"5. Goi iERP API luu gia tri can");
                                var orders = await DIBootstrapper.Init().Resolve<StoreOrderOperatingRepository>().GetOrdersScaleStationIn(scaleInfo.Vehicle);

                                var deliveryCodes = scaleInfo.DeliveryCode;

                                if (orders != null && orders.Count != 0)
                                {
                                    deliveryCodes = string.Join(";", orders.Select(x => x.DeliveryCode).Distinct().ToList());
                                }

                                var scaleInfoResult = DIBootstrapper.Init().Resolve<DesicionScaleBusiness>().MakeDecisionScaleIn(deliveryCodes, currentScaleValue);

                                if (scaleInfoResult.Code == "01")
                                {
                                    // Lưu giá trị cân thành công
                                    SendMessage("Notification", $"{scaleInfoResult.Message}");
                                    SendMessageAPI("Notification", $"{scaleInfoResult.Message}");

                                    var pushMessage = $"Đơn hàng {deliveryCodes} phương tiện {currentOrder.Vehicle} cân vào tự động thành công, khối lượng {currentScaleValue} kg, vui lòng di chuyển đến bãi chờ lấy hàng, trân trọng!";
                                    SendPushNotification("adminNPP", pushMessage);

                                    //var driverUserName = orders.FirstOrDefault()?.DriverUserName;
                                    //if (driverUserName != null)
                                    //{
                                    //    _notification.SendPushNotification(driverUserName, $"Đơn hàng số hiệu {deliveryCodes} cân vào thành công lúc {currentTime}. Khối lượng {currentScaleValue}, vui lòng mở ứng dụng VICEM để xem chi tiết, trân trọng!");
                                    //}

                                    WriteLogInfo($"5.1. Lưu giá trị cân thành công");

                                    // 6. Update gia tri can va trang thai Can vao
                                    WriteLogInfo($"6. Update gia tri can va trang thai Can vao");

                                    if (currentOrder.CatId == OrderCatIdCode.CLINKER
                                     || currentOrder.TypeXK == OrderTypeXKCode.JUMBO
                                     || currentOrder.TypeXK == OrderTypeXKCode.SLING)
                                    {
                                        WriteLogInfo($"6.1. Don hang CLINKER hoac XK: CatId = {currentOrder.CatId}, TypeXK = {currentOrder.TypeXK}");

                                        WriteLogInfo($"6.2. Update gia tri can vao");
                                        await DIBootstrapper.Init().Resolve<WeightBusiness>().UpdateWeightIn(scaleInfo.DeliveryCode, currentScaleValue);

                                        //WriteLogInfo($"6.3. Update trạng thái cân vào");
                                        //await DIBootstrapper.Init().Resolve<StepBusiness>().UpdateOrderConfirm3(scaleInfo.DeliveryCode);

                                        //WriteLogInfo($"6.2. Update gia tri can vao toan bo don hang theo vehicle code");
                                        //await DIBootstrapper.Init().Resolve<WeightBusiness>().UpdateWeightInByVehicleCode(scaleInfo.Vehicle, currentScaleValue);

                                        WriteLogInfo($"6.3. Update trạng thái cân vào toan bo don hang theo vehicle code");
                                        await DIBootstrapper.Init().Resolve<StepBusiness>().UpdateOrderConfirm3ByVehicleCode(scaleInfo.Vehicle);
                                    }
                                    else
                                    {
                                        WriteLogInfo($"6.1. Don hang thong thuong: CatId = {currentOrder.CatId}, TypeXK = {currentOrder.TypeXK}");

                                        WriteLogInfo($"6.2. Update gia tri can vao");
                                        await DIBootstrapper.Init().Resolve<WeightBusiness>().UpdateWeightIn(scaleInfo.DeliveryCode, currentScaleValue);

                                        //WriteLogInfo($"6.3. Update trạng thái cân vào");
                                        //await DIBootstrapper.Init().Resolve<StepBusiness>().UpdateOrderConfirm3(scaleInfo.DeliveryCode);

                                        WriteLogInfo($"6.3. Update trạng thái cân vào toan bo don hang theo vehicle code");
                                        await DIBootstrapper.Init().Resolve<StepBusiness>().UpdateOrderConfirm3ByVehicleCode(scaleInfo.Vehicle);

                                        //DIBootstrapper.Init().Resolve<Notification>().SendInforNotification($"{currentOrder.DriverUserName}", $"{scaleInfo.DeliveryCode} cân vào tự động lúc {currentTime}");
                                    }

                                    // 8. Bật đèn xanh
                                    WriteLogInfo($"8. Bật đèn xanh");
                                    TurnOnGreenTrafficLight();

                                    //Thread.Sleep(15000);

                                    //WriteLogInfo($"9. Tắt đèn");
                                    //TurnOffTrafficLight();
                                }
                                else
                                {
                                    // Lưu giá trị cân thất bại
                                    SendMessage("WarningNotification", $"{scaleInfoResult.Message}. Vui lòng xử lý thủ công!");
                                    SendMessageAPI("WarningNotification", $"{scaleInfoResult.Message}. Vui lòng xử lý thủ công!");

                                    var pushMessage = $"Đơn hàng {deliveryCodes} phương tiện {currentOrder.Vehicle} cân vào tự động thất bại , khối lượng {currentScaleValue} kg, vui lòng cân thủ công, trân trọng! Chi tiết: {scaleInfoResult.Message}";
                                    SendPushNotification("adminNPP", pushMessage);

                                    WriteLogInfo($"5.1. Lưu giá trị cân thất bại: Code={scaleInfoResult.Code} Message={scaleInfoResult.Message}");

                                    Thread.Sleep(TIME_TO_RELEASE_SCALE);
                                }

                                // 9. Giải phóng cân
                                WriteLogInfo($"9. Giai phong can {SCALE_CODE}");
                                await ReleaseScale();
                            }
                            // Đang cân ra
                            else if ((bool)scaleInfo.ScaleOut)
                            {
                                // Độ lệch khối lượng hiện tại và khối lượng có tải dự kiến (không tải trung bình + số lượng đặt hàng)
                                var ladenWeightSaiSo = Math.Abs((int)ladenWeight - currentScaleValue);

                                WriteLogInfo($"2.1. Khoi luong khong tai trung binh: {unladenWeight}");
                                WriteLogInfo($"2.2. Khoi luong đặt hàng: {currentOrder.SumNumber}");
                                WriteLogInfo($"2.3. Khoi luong có tải dự kiến: {ladenWeight}");
                                WriteLogInfo($"2.4. Sai so khoi luong có tải: {ladenWeightSaiSo}");

                                if (unladenWeight > 0 && ladenWeightSaiSo > ScaleConfig.LADEN_WEIGHT_SAISO)
                                {
                                    WriteLogInfo($"2.3. Sai so vuot qua {ScaleConfig.LADEN_WEIGHT_SAISO}. Nghi ngờ cân nhầm xe. Vui lòng xử lý thủ công!");

                                    SendMessage("WarningNotification", $"Phát hiện khối lượng cân không hợp lệ, sai số vượt quá {ScaleConfig.LADEN_WEIGHT_SAISO}. Vui lòng xử lý thủ công!");

                                    Thread.Sleep(TIME_TO_RELEASE_SCALE);
                                    await ReleaseScale();
                                    return;
                                }

                                // 4. Gọi iERP API lưu giá trị cân
                                WriteLogInfo($"4. Goi iERP API luu gia tri can");

                                var orders = await DIBootstrapper.Init().Resolve<StoreOrderOperatingRepository>().GetOrdersScaleStationOut(scaleInfo.Vehicle);

                                var deliveryCodes = scaleInfo.DeliveryCode;

                                if (orders != null && orders.Count != 0)
                                {
                                    deliveryCodes = string.Join(";", orders.Select(x => x.DeliveryCode).Distinct().ToList());
                                }

                                var scaleInfoResult = await DIBootstrapper.Init().Resolve<DesicionScaleBusiness>().MakeDecisionScaleOut(deliveryCodes, currentScaleValue);

                                if (scaleInfoResult.Code == "01")
                                {
                                    // Lưu giá trị cân thành công
                                    SendMessage("Notification", $"{scaleInfoResult.Message}");
                                    SendMessageAPI("Notification", $"{scaleInfoResult.Message}");

                                    var pushMessage = $"Đơn hàng {deliveryCodes} phương tiện {currentOrder.Vehicle} cân ra tự động thành công, khối lượng {currentScaleValue} kg, vui lòng di chuyển ra cổng bảo vệ, trân trọng!";
                                    SendPushNotification("adminNPP", pushMessage);

                                    //var driverUserName = orders.FirstOrDefault()?.DriverUserName;
                                    //if (driverUserName != null)
                                    //{
                                    //    _notification.SendPushNotification(driverUserName, $"Đơn hàng số hiệu {deliveryCodes} cân vào thành công lúc {currentTime}. Khối lượng {currentScaleValue}, vui lòng mở ứng dụng VICEM để xem chi tiết, trân trọng!");
                                    //}

                                    WriteLogInfo($"4.1. Lưu giá trị cân thành công");

                                    // 5. Update gia tri can va trang thai Can ra
                                    WriteLogInfo($"5. Update gia tri can va trang thai Can ra");
                                    WriteLogInfo($"5.1. Update gia tri can ra");
                                    await DIBootstrapper.Init().Resolve<WeightBusiness>().UpdateWeightOut(scaleInfo.DeliveryCode, currentScaleValue);

                                    WriteLogInfo($"5.2. Update trạng thái cân ra");
                                    //await DIBootstrapper.Init().Resolve<StepBusiness>().UpdateOrderConfirm7(scaleInfo.DeliveryCode);
                                    await DIBootstrapper.Init().Resolve<StepBusiness>().UpdateOrderConfirm7ByVehicleCode(scaleInfo.Vehicle);

                                    //DIBootstrapper.Init().Resolve<Notification>().SendInforNotification($"{currentOrder.DriverUserName}", $"{scaleInfo.DeliveryCode} cân ra tự động lúc {currentTime}");

                                    //Thread.Sleep(3000);

                                    // 7. Bật đèn xanh
                                    WriteLogInfo($"7. Bat den xanh");
                                    TurnOnGreenTrafficLight();

                                    //Thread.Sleep(15000);

                                    //WriteLogInfo($"8. Tắt đèn");
                                    //TurnOffTrafficLight();
                                }
                                else
                                {
                                    // Lưu giá trị cân thất bại
                                    SendMessage("WarningNotification", $"{scaleInfoResult.Message}. Vui lòng xử lý thủ công!");
                                    SendMessageAPI("WarningNotification", $"{scaleInfoResult.Message}. Vui lòng xử lý thủ công!");

                                    var pushMessage = $"Đơn hàng {deliveryCodes} phương tiện {currentOrder.Vehicle} cân ra tự động thất bại , khối lượng {currentScaleValue} kg, vui lòng cân thủ công, trân trọng! Chi tiết: {scaleInfoResult.Message}";
                                    SendPushNotification("adminNPP", pushMessage);

                                    WriteLogInfo($"4.1. Lưu giá trị cân thất bại: Code={scaleInfoResult.Code} Message={scaleInfoResult.Message}");

                                    Thread.Sleep(TIME_TO_RELEASE_SCALE);
                                }

                                // 8. Giải phóng cân: Program.IsScalling = false, update table tblScale
                                WriteLogInfo($"8. Giai phong can {SCALE_CODE}");
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
                WriteLogInfo($@"Co loi khi xu ly scale data: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
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
            WriteLogInfo($@"Bật đèn xanh chiều vào");
            if (DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnGreenTrafficLight(SCALE_DGT_IN_CODE))
            {
                if (isHasNotification)
                {
                    SendMessage("Notification", $"Bật đèn xanh chiều vào thành công");
                    SendMessageAPI("Notification", $"Bật đèn xanh chiều vào thành công");
                }
                WriteLogInfo($@"Bật thành công");
            }
            else
            {
                if (isHasNotification)
                {
                    SendMessage("Notification", $"Bật đèn xanh chiều vào thất bại");
                    SendMessageAPI("Notification", $"Bật đèn xanh chiều vào thất bại");
                }
                WriteLogInfo($@"Bật thất bại");
            }

            Thread.Sleep(500);

            WriteLogInfo($@"Bật đèn xanh chiều ra");
            if (DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnGreenTrafficLight(SCALE_DGT_OUT_CODE))
            {
                if (isHasNotification)
                {
                    SendMessage("Notification", $"Bật đèn xanh chiều ra thành công");
                    SendMessageAPI("Notification", $"Bật đèn xanh chiều ra thành công");
                }
                WriteLogInfo($@"Bật thành công");
            }
            else
            {
                if (isHasNotification)
                {
                    SendMessage("Notification", $"Bật đèn xanh chiều ra thất bại");
                    SendMessageAPI("Notification", $"Bật đèn xanh chiều ra thất bại");
                }
                WriteLogInfo($@"Bật thất bại");
            }
        }

        public void TurnOnRedTrafficLight(bool isHasNotification = false)
        {
            WriteLogInfo($@"Bật đèn đỏ chiều vào");
            if (DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnRedTrafficLight(SCALE_DGT_IN_CODE))
            {
                if (isHasNotification)
                {
                    SendMessage("Notification", $"Bật đèn đỏ chiều vào thành công");
                    SendMessageAPI("Notification", $"Bật đèn đỏ chiều vào thành công");
                }
                WriteLogInfo($@"Bật thành công");
            }
            else
            {
                if (isHasNotification)
                {
                    SendMessage("Notification", $"Bật đèn đỏ chiều vào thất bại");
                    SendMessageAPI("Notification", $"Bật đèn đỏ chiều vào thất bại");
                }
                WriteLogInfo($@"Bật thất bại");
            }

            Thread.Sleep(500);

            WriteLogInfo($@"Bật đèn đỏ chiều ra");
            if (DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnRedTrafficLight(SCALE_DGT_OUT_CODE))
            {
                if (isHasNotification)
                {
                    SendMessage("Notification", $"Bật đèn đỏ chiều ra thành công");
                    SendMessageAPI("Notification", $"Bật đèn đỏ chiều ra thành công");
                }
                WriteLogInfo($@"Bật thành công");
            }
            else
            {
                if (isHasNotification)
                {
                    SendMessage("Notification", $"Bật đèn đỏ chiều ra thất bại");
                    SendMessageAPI("Notification", $"Bật đèn đỏ chiều ra thất bại");
                }
                WriteLogInfo($@"Bật thất bại");
            }
        }

        public void TurnOffTrafficLight(bool isHasNotification = false)
        {
            WriteLogInfo($@"Tắt đèn chiều vào");
            if (DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOffTrafficLight(SCALE_DGT_IN_CODE))
            {
                if (isHasNotification)
                {
                    SendMessage("Notification", $"Tắt đèn chiều vào thành công");
                    SendMessageAPI("Notification", $"Tắt đèn chiều vào thành công");
                }
                WriteLogInfo($@"Bật thành công");
            }
            else
            {
                if (isHasNotification)
                {
                    SendMessage("Notification", $"Tắt đèn chiều vào thất bại");
                    SendMessageAPI("Notification", $"Tắt đèn chiều vào thất bại");
                }
                WriteLogInfo($@"Bật thất bại");
            }

            Thread.Sleep(500);

            WriteLogInfo($@"Tắt đèn chiều ra");
            if (DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOffTrafficLight(SCALE_DGT_OUT_CODE))
            {
                if (isHasNotification)
                {
                    SendMessage("Notification", $"Tắt đèn chiều ra thành công");
                    SendMessageAPI("Notification", $"BTắt đèn chiều ra thành công");
                }
                WriteLogInfo($@"Bật thành công");
            }
            else
            {
                if (isHasNotification)
                {
                    SendMessage("Notification", $"Tắt đèn chiều ra thất bại");
                    SendMessageAPI("Notification", $"Tắt đèn chiều ra thất bại");
                }
                WriteLogInfo($@"Bật thất bại");
            }
        }

        private void SendMessageAPI(string name, string message)
        {
            try
            {
                _notification.SendScale1Message(name, message);
            }
            catch (Exception ex)
            {
                WriteLogInfo($"SendMessageAPI ERROR: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }

        public void SendPushNotification(string userNameReceiver, string message)
        {
            try
            {
                WriteLogInfo($"Gửi push notificaiton đến {userNameReceiver}, nội dung {message}");
                _notification.SendPushNotification(userNameReceiver, message);
            }
            catch (Exception ex)
            {
                WriteLogInfo($"SendPushNotification Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }

        public void SendSensorAPI(string sensorCode, string status)
        {
            try
            {
                _notification.SendScale1Sensor(sensorCode, status);
            }
            catch (Exception ex)
            {
                WriteLogInfo($"SendSensorAPI ERROR: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }

        public void WriteLogInfo(string message)
        {
            Console.WriteLine(message);
            _logger.Info(message);
        }
    }
}
