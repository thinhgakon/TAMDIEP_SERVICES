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
using XHTD_SERVICES_CANVAO_1.Models.Response;
using XHTD_SERVICES_CANVAO_1.Hubs;
using XHTD_SERVICES_CANVAO_1.Devices;
using XHTD_SERVICES_CANVAO_1.Business;
using XHTD_SERVICES.Helper;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using log4net;

namespace XHTD_SERVICES_CANVAO_1.Jobs
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

        ILog _rfidlogger = LogManager.GetLogger("RfidFileAppender");

        protected readonly string SCALE_CODE = ScaleCode.CODE_SCALE_1;

        protected readonly string SCALE_DGT_IN_CODE = ScaleCode.CODE_SCALE_1_DGT_IN;

        protected readonly string SCALE_DGT_OUT_CODE = ScaleCode.CODE_SCALE_1_DGT_OUT;

        protected readonly string SCALE_SIGNALR_RFID_CODE = "TRAM951_1_RFID";

        protected readonly string SCALE_DELIVERY_CODE = "TRAM951_1_DELIVERY_CODE";

        protected readonly string SCALE_IS_LOCKING_RFID = "SCALE_1_IS_LOCKING_RFID";

        protected readonly string VEHICLE_STATUS = "VEHICLE_1_STATUS";

        protected const string SERVICE_ACTIVE_CODE = "TRAM951_1_ACTIVE";

        protected const string SERVICE_SENSOR_ACTIVE_CODE = "TRAM951_1_SENSOR_ACTIVE";

        protected const string SERVICE_BARRIER_ACTIVE_CODE = "TRAM951_1_BARRIER_ACTIVE";

        protected readonly string SCALE_CURRENT_RFID = "SCALE_1_CURRENT_RFID";

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

        private const int BUFFER_SIZE = 1024;
        private const int PORT_NUMBER = 10000;

        static ASCIIEncoding encoding = new ASCIIEncoding();

        static TcpClient client = new TcpClient();
        static Stream stream = null;

        private byte ComAddr = 0xFF;
        private int PortHandle = 6000;
        private string PegasusAdr = "192.168.13.181";

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

            try
            {
                await Task.Run(async () =>
                {
                    // Get System Parameters
                    await LoadSystemParameters();

                    if (!isActiveService)
                    {
                        _logger.LogInfo("Service cân vào đang tắt");
                        return;
                    }

                    _logger.LogInfo($"==================================== START JOB - IP: {PegasusAdr} ====================================");

                    // Get devices info
                    await LoadDevicesInfo();

                    AuthenticateGatewayModuleFromPegasus();
                });
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"RUN JOB ERROR: {ex.Message} --- {ex.StackTrace} --- {ex.InnerException}");

                // do you want the job to refire?
                throw new JobExecutionException(msg: "", refireImmediately: true, cause: ex);
            }
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

            c3400 = devices.FirstOrDefault(x => x.Code == "951-1.C3-400");
        }

        public void AuthenticateGatewayModuleFromPegasus()
        {
            // 1. Connect Device
            int port = PortHandle;
            var openResult = 1;
            while (openResult != 0)
            {
                try
                {
                    openResult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr, ref ComAddr, ref port);
                }
                catch (Exception ex)
                {
                    _logger.LogInfo($"OpenNetPort ERROR:{ex.StackTrace} --- {ex.Message}");
                }
            }

            _logger.LogInfo($"Connected Pegasus IP:{PegasusAdr} - Port: {PortHandle}");

            Program.UHFConnected = true;

            // 2. Đọc dữ liệu từ thiết bị
            ReadDataFromPegasus();
        }

        public void ReadDataFromPegasus()
        {
            _logger.LogInfo($"Reading Pegasus...");

            while (Program.UHFConnected)
            {
                try
                {
                    var data = PegasusReader.Inventory_G2(ref Program.RefComAdr1, 0, 0, 0, Program.RefPort2);

                    foreach (var item in data)
                    {
                        try
                        {
                            var cardNoCurrent = ByteArrayToString(item);

                            Program.LastTimeReceivedUHF = DateTime.Now;

                            _rfidlogger.Info($"======================= CardNo: {cardNoCurrent}");

                            ReadDataProcess(cardNoCurrent);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($@"PROCESS RFID ERROR: {ex.StackTrace} -- {ex.Message} -- {ex.InnerException}");
                            Program.UHFConnected = false;
                            break;
                        }
                    }
                }
                catch(Exception err) 
                {
                    _logger.LogError($@"ReadDataFromPegasus ERROR: {err.StackTrace} -- {err.Message} -- {err.InnerException}");
                    Program.UHFConnected = false;
                    break;
                }
            }

            AuthenticateGatewayModuleFromPegasus();
        }

        public async void ReadDataProcess(string cardNoCurrent)
        {
            SendNotificationHub($"{SCALE_IS_LOCKING_RFID}", $"{cardNoCurrent}");
            SendNotificationAPI($"{SCALE_IS_LOCKING_RFID}", $"{cardNoCurrent}");

            if (Program.IsEnabledRfid == false)
            {
                _rfidlogger.Info($"1. Đang khóa nhận diện IsEnabledRfid={Program.IsEnabledRfid} => Kết thúc");
                _rfidlogger.Info($"2. Chi tiết khóa nhận diện IsLockingRfid={Program.IsLockingRfid} --  scaleValue={Program.scaleValuesForResetLight.LastOrDefault()} -- EnabledRfidTime={Program.EnabledRfidTime} => Kết thúc");

                return;
            }

            // Loại bỏ các tag đã check trước đó
            if (tmpInvalidCardNoLst.Count > 10)
            {
                tmpInvalidCardNoLst.RemoveRange(0, 3);
            }

            if (tmpInvalidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddSeconds(-30)))
            {
                _rfidlogger.Info($@"1. Tag KHONG HOP LE da duoc check truoc do => Ket thuc.");
                return;
            }

            if (tmpCardNoLst.Count > 5)
            {
                tmpCardNoLst.RemoveRange(0, 3);
            }

            if (tmpCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-7)))
            {
                _rfidlogger.Info($"1. Tag HOP LE da duoc check truoc do => Ket thuc.");
                return;
            }

            _rfidlogger.Info($"1. Tiến hành xử lý rfid => Xem main log");

            SendNotificationHub(SCALE_CURRENT_RFID, cardNoCurrent);
            SendNotificationAPI(SCALE_CURRENT_RFID, cardNoCurrent);

            _logger.LogInfo("--------------------------------------------------------");
            _logger.LogInfo($"Tag: {cardNoCurrent}");
            _logger.LogInfo("--------------------------------------------------------");

            // Nếu đang cân xe khác thì bỏ qua RFID hiện tại
            var scaleInfo = _scaleOperatingRepository.GetDetail(SCALE_CODE);
            
            if (Program.IsScalling)
            {
                var timeToRelease = DateTime.Now.AddMinutes(-5);

                if (scaleInfo != null
                    && (bool)scaleInfo.IsScaling
                    && !String.IsNullOrEmpty(scaleInfo.DeliveryCode)
                    && scaleInfo.TimeIn > timeToRelease
                    )
                {
                    SendNotificationHub("Notification", $"== Can {SCALE_CODE} dang hoat dong => Ket thuc {cardNoCurrent} ==");
                    SendNotificationAPI("Notification", $"== Can {SCALE_CODE} dang hoat dong => Ket thuc {cardNoCurrent} ==");

                    // TODO: cần kiểm tra đơn hàng DeliveryCode, nếu chưa có weightIn thì mới bỏ qua RFID này
                    _logger.LogInfo($"== Can {SCALE_CODE} dang hoat dong => Ket thuc ==");
                    return;
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

            #region Kiểm tra đang có dữ liệu đơn đang cân không
            if (scaleInfo != null
                    && (bool)scaleInfo.IsScaling
                    && !String.IsNullOrEmpty(scaleInfo.DeliveryCode)
                    )
            {
                _logger.LogInfo($"=== Đang cân MSGH: {scaleInfo.DeliveryCode} --- TimeIn: {scaleInfo.TimeIn} == => Kết thúc");

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                tmpInvalidCardNoLst.Add(newCardNoLog);

                return;
            }
            #endregion

            // 1. Kiểm tra cardNoCurrent hợp lệ
            string vehicleCodeCurrent = _rfidRepository.GetVehicleCodeByCardNo(cardNoCurrent);
            if (!String.IsNullOrEmpty(vehicleCodeCurrent))
            {
                _logger.LogInfo($"1. Tag hop le: vehicle={vehicleCodeCurrent}");
            }
            else
            {
                _logger.LogInfo($"1. Tag KHONG hop le => Ket thuc");

                SendNotificationHub($"{VEHICLE_STATUS}", $"RFID {cardNoCurrent} không thuộc hệ thống");
                SendNotificationAPI($"{VEHICLE_STATUS}", $"RFID {cardNoCurrent} không thuộc hệ thống");

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                tmpInvalidCardNoLst.Add(newCardNoLog);

                return;
            }

            // 2. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
            var currentOrder = await _storeOrderOperatingRepository.GetCurrentOrderScaleStation(vehicleCodeCurrent);
            var isValidCardNo = OrderValidator.IsValidOrderScaleStation(currentOrder);

            if (currentOrder == null)
            {
                _logger.LogInfo($"2. Tag KHONG co don hang => Ket thuc");

                SendNotificationHub($"{VEHICLE_STATUS}", $"{vehicleCodeCurrent} không có đơn hàng");
                SendNotificationAPI($"{VEHICLE_STATUS}", $"{vehicleCodeCurrent} không có đơn hàng");

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                tmpInvalidCardNoLst.Add(newCardNoLog);

                return;
            }
            else if (isValidCardNo == false)
            {
                _logger.LogInfo($"2. Tag KHONG co don hang hop le => Ket thuc");

                SendNotificationHub($"{VEHICLE_STATUS}", $"{vehicleCodeCurrent} không có đơn hàng hợp lệ");
                SendNotificationAPI($"{VEHICLE_STATUS}", $"{vehicleCodeCurrent} không có đơn hàng hợp lệ");

                SendNotificationHub($"{SCALE_DELIVERY_CODE}", $"{currentOrder.DeliveryCode}");
                SendNotificationAPI($"{SCALE_DELIVERY_CODE}", $"{currentOrder.DeliveryCode}");

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                tmpInvalidCardNoLst.Add(newCardNoLog);

                return;
            }
            else
            {
                Program.IsLockingRfid = true;

                SendNotificationHub($"{VEHICLE_STATUS}", $"{vehicleCodeCurrent} có đơn hàng hợp lệ");
                SendNotificationAPI($"{VEHICLE_STATUS}", $"{vehicleCodeCurrent} có đơn hàng hợp lệ");

                SendNotificationHub($"{SCALE_DELIVERY_CODE}", $"{currentOrder.DeliveryCode}");
                SendNotificationAPI($"{SCALE_DELIVERY_CODE}", $"{currentOrder.DeliveryCode}");

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                tmpCardNoLst.Add(newCardNoLog);

                _logger.LogInfo($"2. Tag co don hang hop le DeliveryCode = {currentOrder.DeliveryCode}");
            }

            // 3. Xác định xe vào hay ra
            var isLuongVao = true;

            if (currentOrder.Step < (int)OrderStep.DA_CAN_VAO 
                || currentOrder.Step == (int)OrderStep.DA_XAC_THUC 
                || currentOrder.Step == (int)OrderStep.CHO_GOI_XE
                || currentOrder.Step == (int)OrderStep.DANG_GOI_XE
                )
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
                    TurnOnRedTrafficLight();

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
                    TurnOnRedTrafficLight();

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

        public void TurnOnRedTrafficLight()
        {
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
        }

        private void SendNotificationHub(string name, string message)
        {
            new ScaleHub().SendMessage(name, message);
        }

        private void SendNotificationAPI(string name, string message)
        {
            try
            {
                _notification.SendScale1Message(name, message);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SendNotificationAPI ERR: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }

        public string ByteArrayToString(byte[] b)
        {
            return BitConverter.ToString(b).Replace("-", "");
        }

        #region Read RFID by C3-400
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

        public void ReadDataFromC3400()
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

                                    ReadDataProcess(cardNoCurrent);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($@"Co loi xay ra khi xu ly RFID: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
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
        #endregion

        #region Read RFID by Controller
        public void AuthenticateScaleStationModuleFromController()
        {
            while (!client.Connected)
            {
                ConnectScaleStationModuleFromController();
            }
            ReadDataFromController();
        }

        public bool ConnectScaleStationModuleFromController()
        {
            _logger.LogInfo("Thuc hien ket noi.");
            try
            {
                _logger.LogInfo("Bat dau ket noi.");
                client = new TcpClient();

                // 1. connect
                client.ConnectAsync(c3400.IpAddress, c3400.PortNumber ?? 0).Wait(2000);
                stream = client.GetStream();

                _logger.LogInfo("Connected to controller");

                DeviceConnected = true;
                return DeviceConnected;
            }
            catch (Exception ex)
            {
                _logger.LogInfo("Ket noi that bai.");
                _logger.LogInfo(ex.Message);
                _logger.LogInfo(ex.StackTrace);
                return false;
            }
        }

        public void ReadDataFromController()
        {
            if (client.Connected)
            {
                while (client.Connected)
                {
                    try
                    {
                        if (Program.IsEnabledRfid == false)
                            continue;
                        _logger.LogInfo("Reading RFID from Controller ...");
                        byte[] data = new byte[BUFFER_SIZE];
                        stream.Read(data, 0, BUFFER_SIZE);
                        var dataStr = encoding.GetString(data);

                        if (Program.IsLockingRfid == true)
                        {
                            _logger.LogInfo($"Nhan tin hieu: {dataStr}");
                        }

                        string pattern = @"\*\[Reader\]\[(\d+)\](.*?)\[!\]";
                        Match match = Regex.Match(dataStr, pattern);

                        string xValue = string.Empty;
                        string cardNoCurrent = string.Empty;

                        if (match.Success)
                        {
                            xValue = match.Groups[1].Value;
                            cardNoCurrent = match.Groups[2].Value;
                        }
                        else
                        {
                            _logger.LogInfo("Tin hieu nhan vao khong dung dinh dang");
                            continue;
                        }

                        if (!int.TryParse(xValue, out int doorCurrent))
                        {
                            _logger.LogInfo("XValue is not valid");
                            continue;
                        }

                        if (doorCurrent != 1 && doorCurrent != 2) continue;

                        ReadDataProcess(cardNoCurrent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($@"Co loi xay ra khi xu ly RFID {ex.StackTrace} {ex.Message} ");
                        continue;
                    }
                }
                AuthenticateScaleStationModuleFromController();
            }
            else
            {
                AuthenticateScaleStationModuleFromController();
            }
        }
        #endregion
    }
}
