using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using Autofac;
using System.Runtime.InteropServices;
using XHTD_SERVICES.Data.Common;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Models.Values;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_TRAM951_2.Models.Response;
using XHTD_SERVICES_TRAM951_2.Hubs;
using XHTD_SERVICES_TRAM951_2.Devices;
using XHTD_SERVICES_TRAM951_2.Business;
using XHTD_SERVICES.Helper;

namespace XHTD_SERVICES_TRAM951_2.Jobs
{
    public class Tram951ModuleJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly RfidRepository _rfidRepository;

        protected readonly CategoriesDevicesRepository _categoriesDevicesRepository;

        protected readonly CategoriesDevicesLogRepository _categoriesDevicesLogRepository;

        protected readonly VehicleRepository _vehicleRepository;

        protected readonly ScaleOperatingRepository _scaleOperatingRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly Notification _notification;

        protected readonly Logger _logger;

        protected readonly string SCALE_CODE = ScaleCode.CODE_SCALE_2;

        protected readonly string SCALE_DGT_IN_CODE = ScaleCode.CODE_SCALE_2_DGT_IN;

        protected readonly string SCALE_DGT_OUT_CODE = ScaleCode.CODE_SCALE_2_DGT_OUT;

        protected readonly string SCALE_SIGNALR_RFID_CODE = "TRAM951_2_RFID";

        protected readonly string SCALE_DELIVERY_CODE = "TRAM951_2_DELIVERY_CODE";

        protected readonly string SCALE_IS_LOCKING_RFID = "TRAM951_2_IS_LOCKING_RFID";

        protected readonly string VEHICLE_STATUS = "VEHICLE_2_STATUS";

        protected const string SERVICE_ACTIVE_CODE = "TRAM951_2_ACTIVE";

        protected const string SERVICE_SENSOR_ACTIVE_CODE = "TRAM951_2_SENSOR_ACTIVE";

        protected const string SERVICE_BARRIER_ACTIVE_CODE = "TRAM951_2_BARRIER_ACTIVE";

        private static bool isActiveService = true;

        private IntPtr h21 = IntPtr.Zero;

        private static bool DeviceConnected = false;

        private List<CardNoLog> tmpCardNoLst = new List<CardNoLog>();

        private List<CardNoLog> tmpInvalidCardNoLst = new List<CardNoLog>();

        private tblCategoriesDevice c3400;

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "Connect")]
        public static extern IntPtr Connect(string Parameters);

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "PullLastError")]
        public static extern int PullLastError();

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "GetRTLog")]
        public static extern int GetRTLog(IntPtr h, ref byte buffer, int buffersize);

        public Tram951ModuleJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            RfidRepository rfidRepository,
            CategoriesDevicesRepository categoriesDevicesRepository,
            CategoriesDevicesLogRepository categoriesDevicesLogRepository,
            VehicleRepository vehicleRepository,
            ScaleOperatingRepository scaleOperatingRepository,
            SystemParameterRepository systemParameterRepository,
            Notification notification,
            Logger logger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _rfidRepository = rfidRepository;
            _categoriesDevicesRepository = categoriesDevicesRepository;
            _categoriesDevicesLogRepository = categoriesDevicesLogRepository;
            _vehicleRepository = vehicleRepository;
            _scaleOperatingRepository = scaleOperatingRepository;
            _systemParameterRepository = systemParameterRepository;
            _notification = notification;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                // Get System Parameters
                await LoadSystemParameters();

                if (!isActiveService)
                {
                    _logger.LogInfo("Service đang tắt");
                    return;
                }

                _logger.LogInfo("Start tram951 2 service");
                _logger.LogInfo("----------------------------");

                // Get devices info
                await LoadDevicesInfo();

                AuthenticateScaleStationModule();
            });
        }

        public async Task LoadSystemParameters()
        {
            var parameters = await _systemParameterRepository.GetSystemParameters();

            var activeParameter = parameters.FirstOrDefault(x => x.Code == SERVICE_ACTIVE_CODE);
            var sensorActiveParameter = parameters.FirstOrDefault(x => x.Code == SERVICE_SENSOR_ACTIVE_CODE);
            var barrierActiveParameter = parameters.FirstOrDefault(x => x.Code == SERVICE_BARRIER_ACTIVE_CODE);

            if (activeParameter == null || activeParameter.Value == "0")
            {
                isActiveService = false;
            }
            else
            {
                isActiveService = true;
            }

            if (sensorActiveParameter == null || sensorActiveParameter.Value == "0")
            {
                Program.IsSensorActive = false;
            }
            else
            {
                Program.IsSensorActive = true;
            }

            if (barrierActiveParameter == null || barrierActiveParameter.Value == "0")
            {
                Program.IsBarrierActive = false;
            }
            else
            {
                Program.IsBarrierActive = true;
            }
        }

        public async Task LoadDevicesInfo()
        {
            var devices = await _categoriesDevicesRepository.GetDevices("951");

            c3400 = devices.FirstOrDefault(x => x.Code == "951-2.C3-400");
        }

        public void AuthenticateScaleStationModule()
        {
            while (!DeviceConnected)
            {
                ConnectScaleStationModule();
            }

            ReadDataFromC3400();
        }

        public bool ConnectScaleStationModule()
        {
            var ipAddress = c3400?.IpAddress;
            try
            {
                string str = $"protocol=TCP,ipaddress={ipAddress},port={c3400?.PortNumber},timeout=2000,passwd=";
                int ret = 0;
                if (IntPtr.Zero == h21)
                {
                    h21 = Connect(str);
                    if (h21 != IntPtr.Zero)
                    {
                        _logger.LogInfo($"Connected to C3-400 {ipAddress}");

                        DeviceConnected = true;
                    }
                    else
                    {
                        _logger.LogInfo($"Connect to C3-400 {ipAddress} failed");

                        ret = PullLastError();
                        DeviceConnected = false;
                    }
                }
                return DeviceConnected;
            }
            catch (Exception ex)
            {
                _logger.LogInfo($@"ConnectScaleStationModule {ipAddress} error: {ex.Message}");
                return false;
            }
        }

        public async void ReadDataFromC3400()
        {
            _logger.LogInfo("Reading RFID from C3-400 ...");

            if (DeviceConnected)
            {
                while (DeviceConnected)
                {
                    int ret = 0, buffersize = 256;
                    string str = "";
                    string[] tmp = null;
                    byte[] buffer = new byte[256];

                    if (IntPtr.Zero != h21)
                    {
                        ret = GetRTLog(h21, ref buffer[0], buffersize);
                        if (ret >= 0)
                        {
                            try
                            {
                                str = Encoding.Default.GetString(buffer);
                                tmp = str.Split(',');

                                // Bắt đầu xử lý khi nhận diện được RFID
                                if (tmp[2] != "0" && tmp[2] != "")
                                {
                                    var cardNoCurrent = tmp[2]?.ToString();
                                    var doorCurrent = tmp[3]?.ToString();
                                    var timeCurrent = tmp[0]?.ToString();

                                    new ScaleHub().SendMessage($"{SCALE_IS_LOCKING_RFID}", $"{cardNoCurrent}");
                                    if (Program.IsEnabledRfid == false)
                                    {
                                        continue;
                                    }

                                    // Gửi signalr thông tin RFID cho chức năng nhận diện RFID trên app mobile
                                    SendRFIDInfo(cardNoCurrent, doorCurrent);

                                    // Loại bỏ các tag đã check trước đó
                                    if (tmpInvalidCardNoLst.Count > 10)
                                    {
                                        tmpInvalidCardNoLst.RemoveRange(0, 3);
                                    }

                                    if (tmpInvalidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-3)))
                                    {
                                        //_logger.LogInfo($@"1. Tag KHONG HOP LE da duoc check truoc do => Ket thuc.");
                                        continue;
                                    }

                                    if (tmpCardNoLst.Count > 5)
                                    {
                                        tmpCardNoLst.RemoveRange(0, 3);
                                    }

                                    if (tmpCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-7)))
                                    {
                                        //_logger.LogInfo($"1. Tag HOP LE da duoc check truoc do => Ket thuc.");
                                        continue;
                                    }

                                    _logger.LogInfo("----------------------------");
                                    _logger.LogInfo($"Tag: {cardNoCurrent}, door: {doorCurrent}, time: {timeCurrent}");
                                    _logger.LogInfo("-----");

                                    // Nếu đang cân xe khác thì bỏ qua RFID hiện tại
                                    if (Program.IsScalling)
                                    {
                                        var timeToRelease = DateTime.Now.AddMinutes(-5);

                                        var scaleInfo = _scaleOperatingRepository.GetDetail(SCALE_CODE);
                                        if (scaleInfo != null
                                            && (bool)scaleInfo.IsScaling
                                            && !String.IsNullOrEmpty(scaleInfo.DeliveryCode)
                                            && scaleInfo.TimeIn > timeToRelease
                                            )
                                        {
                                            new ScaleHub().SendMessage("Notification", $"== Can {SCALE_CODE} dang hoat dong => Ket thuc {cardNoCurrent} ==");

                                            // TODO: cần kiểm tra đơn hàng DeliveryCode, nếu chưa có weightIn thì mới bỏ qua RFID này
                                            _logger.LogInfo($"== Can {SCALE_CODE} dang hoat dong => Ket thuc ==");
                                            continue;
                                        }
                                        else
                                        {
                                            // Giải phóng cân khi bị giữ quá 5 phút
                                            _logger.LogInfo($"== Giai phong can {SCALE_CODE} khi bi giu qua 5 phut ==");

                                            await DIBootstrapper.Init().Resolve<ScaleBusiness>().ReleaseScale(SCALE_CODE);

                                            Program.IsScalling = false;
                                            Program.InProgressDeliveryCode = null;
                                            Program.InProgressVehicleCode = null;
                                        }
                                    }

                                    // 1. Kiểm tra cardNoCurrent hợp lệ
                                    string vehicleCodeCurrent = _rfidRepository.GetVehicleCodeByCardNo(cardNoCurrent);
                                    if (!String.IsNullOrEmpty(vehicleCodeCurrent))
                                    {
                                        _logger.LogInfo($"1. Tag hop le: vehicle={vehicleCodeCurrent}");
                                    }
                                    else
                                    {
                                        _logger.LogInfo($"1. Tag KHONG hop le => Ket thuc");

                                        new ScaleHub().SendMessage($"{VEHICLE_STATUS}", $"RFID {cardNoCurrent} không thuộc hệ thống");

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                        tmpInvalidCardNoLst.Add(newCardNoLog);

                                        continue;
                                    }

                                    // 2. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
                                    var currentOrder = await _storeOrderOperatingRepository.GetCurrentOrderScaleStation(vehicleCodeCurrent);
                                    var isValidCardNo = OrderValidator.IsValidOrderScaleStation(currentOrder);

                                    if (currentOrder == null)
                                    {
                                        _logger.LogInfo($"2. Tag KHONG co don hang => Ket thuc");

                                        new ScaleHub().SendMessage($"{VEHICLE_STATUS}", $"{vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng");

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                        tmpInvalidCardNoLst.Add(newCardNoLog);

                                        continue;
                                    }
                                    else if (isValidCardNo == false)
                                    {
                                        _logger.LogInfo($"2. Tag KHONG co don hang hop le => Ket thuc");

                                        new ScaleHub().SendMessage($"{VEHICLE_STATUS}", $"{vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng hợp lệ");
                                        new ScaleHub().SendMessage($"{SCALE_DELIVERY_CODE}", $"{currentOrder.DeliveryCode}");

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                        tmpInvalidCardNoLst.Add(newCardNoLog);

                                        continue;
                                    }
                                    else
                                    {
                                        Program.IsLockingRfid = true;

                                        new ScaleHub().SendMessage($"{VEHICLE_STATUS}", $"{vehicleCodeCurrent} - RFID {cardNoCurrent} có đơn hàng hợp lệ");
                                        new ScaleHub().SendMessage($"{SCALE_DELIVERY_CODE}", $"{currentOrder.DeliveryCode}");

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                        tmpCardNoLst.Add(newCardNoLog);

                                        _logger.LogInfo($"2. Tag co don hang hop le DeliveryCode = {currentOrder.DeliveryCode}");
                                    }

                                    // 3. Xác định xe vào hay ra
                                    var isLuongVao = true;

                                    if (currentOrder.Step < (int)OrderStep.DA_CAN_VAO)
                                    {
                                        isLuongVao = true;
                                        _logger.LogInfo($"3. Xe can VAO");
                                    }
                                    else
                                    {
                                        isLuongVao = false;
                                        _logger.LogInfo($"3. Xe can RA");
                                    }

                                    if (isLuongVao)
                                    {
                                        // 4. Lưu thông tin xe đang cân
                                        var isUpdatedOrder = await _scaleOperatingRepository.UpdateWhenConfirmEntrace(SCALE_CODE, currentOrder.DeliveryCode, currentOrder.Vehicle, currentOrder.CardNo);
                                        if (isUpdatedOrder)
                                        {
                                            _logger.LogInfo($"4. Lưu thông tin xe đang cân thành công");

                                            // 5. Bat den do
                                            _logger.LogInfo($@"5.1. Bật đèn ĐỎ chiều VÀO");
                                            if (DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnRedTrafficLight(SCALE_DGT_IN_CODE))
                                            {
                                                _logger.LogInfo($@"Bật đèn thành công");
                                            }
                                            else
                                            {
                                                _logger.LogInfo($@"Bật đèn thất bại");
                                            }

                                            Thread.Sleep(500);

                                            _logger.LogInfo($@"5.2. Bật đèn ĐỎ chiều RA");
                                            if (DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnRedTrafficLight(SCALE_DGT_OUT_CODE))
                                            {
                                                _logger.LogInfo($@"Bật đèn thành công");
                                            }
                                            else
                                            {
                                                _logger.LogInfo($@"Bật đèn thất bại");
                                            }

                                            // 6. Đánh dấu trạng thái đang cân
                                            _logger.LogInfo($@"6. Đánh dấu CAN đang hoạt động: IsScalling = true");
                                            Program.IsScalling = true;
                                            Program.InProgressDeliveryCode = currentOrder.DeliveryCode;
                                            Program.InProgressVehicleCode = currentOrder.Vehicle;
                                        }
                                        else
                                        {
                                            _logger.LogInfo($"4. Lưu thông tin xe đang cân THẤT BẠI");
                                        }
                                    }
                                    else
                                    {
                                        // 4. Lưu thông tin xe đang cân
                                        var isUpdatedOrder = await _scaleOperatingRepository.UpdateWhenConfirmExit(SCALE_CODE, currentOrder.DeliveryCode, currentOrder.Vehicle, currentOrder.CardNo);
                                        if (isUpdatedOrder)
                                        {
                                            _logger.LogInfo($"4. Lưu thông tin xe đang cân thành công");

                                            // 5. Bat den do
                                            _logger.LogInfo($@"5.1. Bật đèn ĐỎ chiều VÀO");
                                            if (DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnRedTrafficLight(SCALE_DGT_IN_CODE))
                                            {
                                                _logger.LogInfo($@"Bật đèn thành công");
                                            }
                                            else
                                            {
                                                _logger.LogInfo($@"Bật đèn thất bại");
                                            }

                                            Thread.Sleep(500);

                                            _logger.LogInfo($@"5.2. Bật đèn ĐỎ chiều RA");
                                            if (DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnRedTrafficLight(SCALE_DGT_OUT_CODE))
                                            {
                                                _logger.LogInfo($@"Bật đèn thành công");
                                            }
                                            else
                                            {
                                                _logger.LogInfo($@"Bật đèn thất bại");
                                            }

                                            // 6. Đánh dấu trạng thái đang cân
                                            _logger.LogInfo($@"6. Đánh dấu CAN đang hoạt động: IsScalling = true");
                                            Program.IsScalling = true;
                                            Program.InProgressDeliveryCode = currentOrder.DeliveryCode;
                                            Program.InProgressVehicleCode = currentOrder.Vehicle;
                                        }
                                        else
                                        {
                                            _logger.LogInfo($@"4. Lưu thông tin xe đang cân THẤT BẠI");
                                        }
                                    }

                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($@"Co loi xay ra khi xu ly RFID {ex.StackTrace} {ex.Message} ");
                                continue;
                            }
                        }
                        else
                        {
                            _logger.LogWarn("No data. Reconnect ...");
                            DeviceConnected = false;
                            h21 = IntPtr.Zero;

                            AuthenticateScaleStationModule();
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarn("No data. Reconnect ...");
                DeviceConnected = false;
                h21 = IntPtr.Zero;

                AuthenticateScaleStationModule();
            }
        }

        public void SendRFIDInfo(string cardNo, string door)
        {
            try
            {
                _notification.SendNotification(
                    SCALE_SIGNALR_RFID_CODE,
                    null,
                    1,
                    cardNo,
                    Int32.Parse(door),
                    null,
                    null,
                    0,
                    null,
                    null,
                    null
                );

                //_logger.LogInfo($"Sent  RFID to app: {cardNo}");
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SendNotification Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }
    }
}
