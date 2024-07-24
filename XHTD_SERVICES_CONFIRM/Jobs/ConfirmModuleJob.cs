using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_CONFIRM.Models.Response;
using XHTD_SERVICES.Data.Models.Response;
using System.Runtime.InteropServices;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Helper;
using Microsoft.AspNet.SignalR.Client;
using System.Threading;
using XHTD_SERVICES.Data.Common;
using Autofac;
using XHTD_SERVICES_CONFIRM.Business;
using XHTD_SERVICES_CONFIRM.Hubs;
using System.Net.NetworkInformation;
using XHTD_SERVICES_CONFIRM.Devices;
using PK_UHF_Test;

namespace XHTD_SERVICES_CONFIRM.Jobs
{
    public class ConfirmModuleJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly RfidRepository _rfidRepository;

        protected readonly CategoriesDevicesRepository _categoriesDevicesRepository;

        protected readonly CategoriesDevicesLogRepository _categoriesDevicesLogRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly PLCBarrier _barrier;

        protected readonly TCPTrafficLight _trafficLight;

        protected readonly Notification _notification;

        protected readonly ConfirmLogger _confirmLogger;

        private IntPtr h21 = IntPtr.Zero;

        private static bool DeviceConnected = false;

        private List<CardNoLog> tmpValidCardNoLst = new List<CardNoLog>();

        private List<CardNoLog> tmpInvalidCardNoLst = new List<CardNoLog>();

        private tblCategoriesDevice c3400, trafficLight;

        protected const string CONFIRM_ACTIVE = "CONFIRM_ACTIVE";

        private static bool isActiveService = true;

        protected const string C3400_CONFIRM_IP_ADDRESS = "10.0.9.1";

        protected const string M221_CONFIRM_IP_ADDRESS = "10.0.9.2";

        protected const string C3400_951_2_IP_ADDRESS = "10.0.9.5";

        private IHubProxy HubProxy { get; set; }

        private string ServerURI = URIConfig.SIGNALR_GATEWAY_SERVICE_URL;

        private HubConnection Connection { get; set; }

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "Connect")]
        public static extern IntPtr Connect(string Parameters);

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "PullLastError")]
        public static extern int PullLastError();

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "GetRTLog")]
        public static extern int GetRTLog(IntPtr h, ref byte buffer, int buffersize);

        private readonly string CAMERA_IP = "192.168.13.163";
        private readonly string CAMERA_USER_NAME = "admin";
        private readonly string CAMERA_PASSWORD = "tamdiep@35";
        private readonly string IMG_PATH = "C:\\IMAGE";
        private readonly int CAMERA_NUMBER = 2;

        private byte ComAddr = 0xFF;
        private int PortHandle = 6000;
        private string PegasusAdr = "192.168.13.168";

        public ConfirmModuleJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            RfidRepository rfidRepository,
            CategoriesDevicesRepository categoriesDevicesRepository,
            CategoriesDevicesLogRepository categoriesDevicesLogRepository,
            SystemParameterRepository systemParameterRepository,
            PLCBarrier barrier,
            TCPTrafficLight trafficLight,
            Notification notification,
            ConfirmLogger confirmLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _rfidRepository = rfidRepository;
            _categoriesDevicesRepository = categoriesDevicesRepository;
            _categoriesDevicesLogRepository = categoriesDevicesLogRepository;
            _systemParameterRepository = systemParameterRepository;
            _barrier = barrier;
            _trafficLight = trafficLight;
            _notification = notification;
            _confirmLogger = confirmLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                // Connect Scale Hub
                ConnectScaleHubAsync();

                // Get System Parameters
                await LoadSystemParameters();

                if (!isActiveService)
                {
                    _confirmLogger.LogInfo("Service điểm xác thực đang TẮT.");
                    return;
                }

                _confirmLogger.LogInfo("Start confirm point service");
                _confirmLogger.LogInfo("----------------------------");

                // Get devices info
                await LoadDevicesInfo();

                AuthenticateConfirmModuleFromPegasus();
            });
        }

        private async void ConnectScaleHubAsync()
        {
            Connection = new HubConnection(ServerURI);
            Connection.Closed += Connection_Closed;
            HubProxy = Connection.CreateHubProxy("ScaleHub");
            try
            {
                await Connection.Start();
                _confirmLogger.LogInfo($"Connected scale hub {ServerURI}");
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                _confirmLogger.LogInfo($"Connect failed scale hub {ServerURI}");
            }
        }

        private void Connection_Closed()
        {
        }

        public async Task LoadSystemParameters()
        {
            var parameters = await _systemParameterRepository.GetSystemParameters();

            var activeParameter = parameters.FirstOrDefault(x => x.Code == CONFIRM_ACTIVE);

            if (activeParameter == null || activeParameter.Value == "0")
            {
                isActiveService = false;
            }
            else
            {
                isActiveService = true;
            }
        }

        public async Task LoadDevicesInfo()
        {
            var devices = await _categoriesDevicesRepository.GetDevices("CONFIRM");

            c3400 = devices.FirstOrDefault(x => x.Code == "CONFIRM.C3-400");

            trafficLight = devices.FirstOrDefault(x => x.Code == "CONFIRM.DGT");
        }

        public void AuthenticateConfirmModule()
        {
            // 1. Connect Device
            while (!DeviceConnected)
            {
                ConnectConfirmationPointModule();
            }

            // 2. Đọc dữ liệu từ thiết bị
            ReadDataFromC3400();
        }

        public void AuthenticateConfirmModuleFromPegasus()
        {
            // 1. Connect Device
            //while (!DeviceConnected)
            //{
            //    ConnectConfirmationPointModuleFromPegasus();
            //}
            DeviceConnected = true;
            // 2. Đọc dữ liệu từ thiết bị
            ReadDataFromPegasus();
        }

        public bool ConnectConfirmationPointModule()
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
                        _confirmLogger.LogInfo($"Connected to C3-400 {ipAddress}");

                        DeviceConnected = true;
                    }
                    else
                    {
                        _confirmLogger.LogInfo($"Connect to C3-400 {ipAddress} failed");
                        ret = PullLastError();
                        DeviceConnected = false;
                    }
                }
                return DeviceConnected;
            }
            catch (Exception ex)
            {
                _confirmLogger.LogInfo($@"Connect to C3-400 {ipAddress} error: {ex.Message}");
                return false;
            }
        }

        public bool ConnectConfirmationPointModuleFromPegasus()
        {
            var ipAddress = c3400?.IpAddress;
            try
            {
                StaticClassReaderB.CloseNetPort(PortHandle);
                var openresult = StaticClassReaderB.OpenNetPort(PortHandle, PegasusAdr, ref ComAddr, ref PortHandle);

                if (openresult == 0)
                {
                    DeviceConnected = true;
                }
                return DeviceConnected;
            }
            catch (Exception ex)
            {
                _confirmLogger.LogInfo($@"Connect to Pegasus {ipAddress} error: {ex.Message}");
                DeviceConnected = false;
                return false;
            }
        }

        public async void ReadDataFromC3400()
        {
            _confirmLogger.LogInfo("Reading RFID from C3-400 ...");

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
                                    var cardNoCurrent = tmp[2]?.ToString(); // RFID
                                    var doorCurrent = tmp[3]?.ToString(); // Điểm xác thực
                                    var timeCurrent = tmp[0]?.ToString(); // Thời gian xác thực

                                    if (Program.IsLockingRfidIn)
                                    {
                                        _confirmLogger.LogInfo($"== Diem xac thuc dang xu ly => Ket thuc {cardNoCurrent} == ");

                                        new ConfirmHub().SendMessage("IS_LOCKING_RFID", "1");
                                    }
                                    else
                                    {
                                        new ConfirmHub().SendMessage("IS_LOCKING_RFID", "0");
                                    }

                                    // Loại bỏ các tag đã check trước đó
                                    if (tmpInvalidCardNoLst.Count > 10)
                                    {
                                        tmpInvalidCardNoLst.RemoveRange(0, 3);
                                    }

                                    if (tmpInvalidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddSeconds(-15)))
                                    {
                                        continue;
                                    }

                                    if (tmpValidCardNoLst.Count > 10)
                                    {
                                        tmpValidCardNoLst.RemoveRange(0, 3);
                                    }

                                    if (tmpValidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-3)))
                                    {
                                        continue;
                                    }

                                    _confirmLogger.LogInfo("----------------------------");
                                    _confirmLogger.LogInfo($"Tag: {cardNoCurrent}, door: {doorCurrent}, time: {timeCurrent}");
                                    _confirmLogger.LogInfo("-----");

                                    _confirmLogger.LogInfo($"2. Kiem tra tag da check truoc do");

                                    // Kiểm tra RFID có hợp lệ hay không
                                    string vehicleCodeCurrent = _rfidRepository.GetVehicleCodeByCardNo(cardNoCurrent);

                                    if (!String.IsNullOrEmpty(vehicleCodeCurrent))
                                    {
                                        _confirmLogger.LogInfo($"3. Tag hop le: vehicle={vehicleCodeCurrent}");
                                    }
                                    else
                                    {
                                        _confirmLogger.LogInfo($"3. Tag KHONG hop le => Ket thuc.");

                                        await SendNotificationHub("CONFIRM_VEHICLE", 0, cardNoCurrent, $"RFID {cardNoCurrent} không thuộc hệ thống");

                                        SendNotificationAPI("CONFIRM_VEHICLE", 0, cardNoCurrent, $"RFID {cardNoCurrent} không thuộc hệ thống");

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                        tmpInvalidCardNoLst.Add(newCardNoLog);

                                        continue;
                                    }

                                    // Nếu RFID hợp lệ
                                    tblStoreOrderOperating currentOrder = null;
                                    var isValidCardNo = false;

                                    currentOrder = await _storeOrderOperatingRepository.GetCurrentOrderConfirmationPoint(vehicleCodeCurrent);

                                    isValidCardNo = OrderValidator.IsValidOrderConfirmationPoint(currentOrder);

                                    // Nếu RFID không có đơn hàng
                                    if (currentOrder == null)
                                    {
                                        _confirmLogger.LogInfo($"4. Tag KHONG co don hang => Ket thuc.");

                                        await SendNotificationHub("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng");

                                        SendNotificationAPI("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng");

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                        tmpInvalidCardNoLst.Add(newCardNoLog);

                                        continue;
                                    }

                                    // Nếu RFID không có đơn hàng hợp lệ
                                    else if (isValidCardNo == false)
                                    {
                                        _confirmLogger.LogInfo($"4. Tag KHONG co don hang hop le => Ket thuc.");

                                        await SendNotificationHub("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng hợp lệ");

                                        SendNotificationAPI("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng hợp lệ");

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                        tmpInvalidCardNoLst.Add(newCardNoLog);

                                        continue;
                                    }

                                    // Nếu RFID có đơn hàng hợp lệ
                                    else
                                    {
                                        await SendNotificationHub("CONFIRM_VEHICLE", 2, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} có đơn hàng hợp lệ", vehicleCodeCurrent);

                                        SendNotificationAPI("CONFIRM_VEHICLE", 2, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} có đơn hàng hợp lệ", vehicleCodeCurrent);

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };

                                        tmpValidCardNoLst.Add(newCardNoLog);

                                        Program.IsLockingRfidIn = true;
                                    }

                                    var currentDeliveryCode = currentOrder.DeliveryCode;
                                    _confirmLogger.LogInfo($"4. Tag co don hang hop le DeliveryCode = {currentDeliveryCode}");

                                    // Xác thực
                                    bool isConfirmSuccess = this._storeOrderOperatingRepository.UpdateBillOrderConfirm10(vehicleCodeCurrent);

                                    // Xác thực thành công
                                    if (isConfirmSuccess)
                                    {
                                        await SendNotificationHub("CONFIRM_RESULT", 1, cardNoCurrent, $"Xác thực thành công", vehicleCodeCurrent);

                                        SendNotificationAPI("CONFIRM_RESULT", 1, cardNoCurrent, $"Xác thực thành công", vehicleCodeCurrent);

                                        // Xếp số
                                        this._storeOrderOperatingRepository.UpdateIndexOrderForNewConfirm(vehicleCodeCurrent);

                                        int statusGreenLight = 0;
                                        string messageGreenLight = "";

                                        _confirmLogger.LogInfo($"7. Bật đèn xanh");
                                        if (TurnOnGreenTrafficLight())
                                        {
                                            statusGreenLight = 1;
                                            messageGreenLight = "Bật đèn xanh thành công";
                                            _confirmLogger.LogInfo($"7.2. Bật đèn xanh thành công");
                                        }
                                        else
                                        {
                                            statusGreenLight = 0;
                                            messageGreenLight = "Bật đèn xanh thất bại";
                                            _confirmLogger.LogInfo($"7.2. Bật đèn xanh thất bại");
                                        }

                                        //var img = new HikvisionStreamCamera().CaptureStream(CAMERA_IP, CAMERA_USER_NAME, CAMERA_PASSWORD, "CONFIRM", CAMERA_NUMBER, IMG_PATH);

                                        //if (!string.IsNullOrEmpty(img))
                                        //{
                                        //    _storeOrderOperatingRepository.UpdateImgConfirm10(vehicleCodeCurrent, img);
                                        //}

                                        //await SendNotificationHub("CONFIRM_RESULT", statusGreenLight, cardNoCurrent, messageGreenLight);

                                        //SendNotificationAPI("CONFIRM_RESULT", statusGreenLight, cardNoCurrent, messageGreenLight);

                                        Thread.Sleep(10000);

                                        int statusRedLight = 0;
                                        string messageRedLight = "";

                                        _confirmLogger.LogInfo($"8. Bật đèn đỏ");
                                        if (TurnOnRedTrafficLight())
                                        {
                                            statusRedLight = 1;
                                            messageRedLight = "Bật đèn đỏ thành công";
                                            _confirmLogger.LogInfo($"8.2. Bật đèn đỏ thành công");
                                        }
                                        else
                                        {
                                            statusRedLight = 0;
                                            messageRedLight = "Bật đèn đỏ thất bại";
                                            _confirmLogger.LogInfo($"8.2. Bật đèn đỏ thất bại");
                                        }

                                        //await SendNotificationHub("CONFIRM_RESULT", statusRedLight, cardNoCurrent, messageRedLight);

                                        //SendNotificationAPI("CONFIRM_RESULT", statusRedLight, cardNoCurrent, messageRedLight);
                                    }
                                    else
                                    {
                                        await SendNotificationHub("CONFIRM_RESULT", 0, cardNoCurrent, $"Xác thực thất bại");

                                        SendNotificationAPI("CONFIRM_RESULT", 0, cardNoCurrent, $"Xác thực thất bại");

                                        _confirmLogger.LogError($"Co loi xay ra khi xac thuc rfid: {cardNoCurrent}");
                                    }

                                    _confirmLogger.LogInfo($"10. Giai phong RFID IN");

                                    Program.IsLockingRfidIn = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                _confirmLogger.LogError($@"Co loi xay ra khi xu ly RFID {ex.StackTrace} {ex.Message} ");
                                continue;
                            }
                        }
                        else
                        {
                            _confirmLogger.LogWarn("No data. Reconnect ...");
                            DeviceConnected = false;
                            h21 = IntPtr.Zero;

                            AuthenticateConfirmModule();
                        }
                    }
                }
            }
            else
            {
                DeviceConnected = false;
                h21 = IntPtr.Zero;

                AuthenticateConfirmModule();
            }
        }

        public async void ReadDataFromPegasus()
        {
            _confirmLogger.LogInfo("Reading RFID from Pegasus ...");

            if (DeviceConnected)
            {
                while (DeviceConnected)
                {
                    int port = 0;
                    var openresult = StaticClassReaderB.OpenNetPort(PortHandle, PegasusAdr, ref ComAddr, ref port);
                    if(openresult == 0)
                    {
                        var data = PegasusReader.Inventory_G2(ref ComAddr, 0, 0, 0, PortHandle);

                        foreach (var item in data)
                        {
                            try
                            {
                                var cardNoCurrent = ByteArrayToString(item);
                                Console.WriteLine($"Nhan the {cardNoCurrent}");
                                if (Program.IsLockingRfidIn)
                                {
                                    _confirmLogger.LogInfo($"== Diem xac thuc dang xu ly => Ket thuc {cardNoCurrent} == ");

                                    new ConfirmHub().SendMessage("IS_LOCKING_RFID", "1");
                                }
                                else
                                {
                                    new ConfirmHub().SendMessage("IS_LOCKING_RFID", "0");
                                }

                                // Loại bỏ các tag đã check trước đó
                                if (tmpInvalidCardNoLst.Count > 10)
                                {
                                    tmpInvalidCardNoLst.RemoveRange(0, 3);
                                }

                                if (tmpInvalidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddSeconds(-15)))
                                {
                                    continue;
                                }

                                if (tmpValidCardNoLst.Count > 10)
                                {
                                    tmpValidCardNoLst.RemoveRange(0, 3);
                                }

                                if (tmpValidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-3)))
                                {
                                    continue;
                                }

                                _confirmLogger.LogInfo("----------------------------");
                                _confirmLogger.LogInfo("-----");

                                _confirmLogger.LogInfo($"2. Kiem tra tag da check truoc do");

                                // Kiểm tra RFID có hợp lệ hay không
                                string vehicleCodeCurrent = _rfidRepository.GetVehicleCodeByCardNo(cardNoCurrent);

                                if (!String.IsNullOrEmpty(vehicleCodeCurrent))
                                {
                                    _confirmLogger.LogInfo($"3. Tag hop le: vehicle={vehicleCodeCurrent}");
                                }
                                else
                                {
                                    _confirmLogger.LogInfo($"3. Tag KHONG hop le => Ket thuc.");

                                    await SendNotificationHub("CONFIRM_VEHICLE", 0, cardNoCurrent, $"RFID {cardNoCurrent} không thuộc hệ thống");

                                    SendNotificationAPI("CONFIRM_VEHICLE", 0, cardNoCurrent, $"RFID {cardNoCurrent} không thuộc hệ thống");

                                    var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                    tmpInvalidCardNoLst.Add(newCardNoLog);

                                    continue;
                                }

                                // Nếu RFID hợp lệ
                                tblStoreOrderOperating currentOrder = null;
                                var isValidCardNo = false;

                                currentOrder = await _storeOrderOperatingRepository.GetCurrentOrderConfirmationPoint(vehicleCodeCurrent);

                                isValidCardNo = OrderValidator.IsValidOrderConfirmationPoint(currentOrder);

                                // Nếu RFID không có đơn hàng
                                if (currentOrder == null)
                                {
                                    _confirmLogger.LogInfo($"4. Tag KHONG co don hang => Ket thuc.");

                                    await SendNotificationHub("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng");

                                    SendNotificationAPI("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng");

                                    var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                    tmpInvalidCardNoLst.Add(newCardNoLog);

                                    continue;
                                }

                                // Nếu RFID không có đơn hàng hợp lệ
                                else if (isValidCardNo == false)
                                {
                                    _confirmLogger.LogInfo($"4. Tag KHONG co don hang hop le => Ket thuc.");

                                    await SendNotificationHub("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng hợp lệ");

                                    SendNotificationAPI("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng hợp lệ");

                                    var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                    tmpInvalidCardNoLst.Add(newCardNoLog);

                                    continue;
                                }

                                // Nếu RFID có đơn hàng hợp lệ
                                else
                                {
                                    await SendNotificationHub("CONFIRM_VEHICLE", 2, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} có đơn hàng hợp lệ", vehicleCodeCurrent);

                                    SendNotificationAPI("CONFIRM_VEHICLE", 2, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} có đơn hàng hợp lệ", vehicleCodeCurrent);

                                    var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };

                                    tmpValidCardNoLst.Add(newCardNoLog);

                                    Program.IsLockingRfidIn = true;
                                }

                                var currentDeliveryCode = currentOrder.DeliveryCode;
                                _confirmLogger.LogInfo($"4. Tag co don hang hop le DeliveryCode = {currentDeliveryCode}");

                                // Xác thực
                                bool isConfirmSuccess = this._storeOrderOperatingRepository.UpdateBillOrderConfirm10(vehicleCodeCurrent);

                                // Xác thực thành công
                                if (isConfirmSuccess)
                                {
                                    await SendNotificationHub("CONFIRM_RESULT", 1, cardNoCurrent, $"Xác thực thành công", vehicleCodeCurrent);

                                    SendNotificationAPI("CONFIRM_RESULT", 1, cardNoCurrent, $"Xác thực thành công", vehicleCodeCurrent);

                                    // Xếp số
                                    this._storeOrderOperatingRepository.UpdateIndexOrderForNewConfirm(vehicleCodeCurrent);

                                    int statusGreenLight = 0;
                                    string messageGreenLight = "";

                                    _confirmLogger.LogInfo($"7. Bật đèn xanh");
                                    if (TurnOnGreenTrafficLight())
                                    {
                                        statusGreenLight = 1;
                                        messageGreenLight = "Bật đèn xanh thành công";
                                        _confirmLogger.LogInfo($"7.2. Bật đèn xanh thành công");
                                    }
                                    else
                                    {
                                        statusGreenLight = 0;
                                        messageGreenLight = "Bật đèn xanh thất bại";
                                        _confirmLogger.LogInfo($"7.2. Bật đèn xanh thất bại");
                                    }

                                    //var img = new HikvisionStreamCamera().CaptureStream(CAMERA_IP, CAMERA_USER_NAME, CAMERA_PASSWORD, "CONFIRM", CAMERA_NUMBER, IMG_PATH);

                                    //if (!string.IsNullOrEmpty(img))
                                    //{
                                    //    _storeOrderOperatingRepository.UpdateImgConfirm10(vehicleCodeCurrent, img);
                                    //}

                                    //await SendNotificationHub("CONFIRM_RESULT", statusGreenLight, cardNoCurrent, messageGreenLight);

                                    //SendNotificationAPI("CONFIRM_RESULT", statusGreenLight, cardNoCurrent, messageGreenLight);

                                    Thread.Sleep(10000);

                                    int statusRedLight = 0;
                                    string messageRedLight = "";

                                    _confirmLogger.LogInfo($"8. Bật đèn đỏ");
                                    if (TurnOnRedTrafficLight())
                                    {
                                        statusRedLight = 1;
                                        messageRedLight = "Bật đèn đỏ thành công";
                                        _confirmLogger.LogInfo($"8.2. Bật đèn đỏ thành công");
                                    }
                                    else
                                    {
                                        statusRedLight = 0;
                                        messageRedLight = "Bật đèn đỏ thất bại";
                                        _confirmLogger.LogInfo($"8.2. Bật đèn đỏ thất bại");
                                    }

                                    //await SendNotificationHub("CONFIRM_RESULT", statusRedLight, cardNoCurrent, messageRedLight);

                                    //SendNotificationAPI("CONFIRM_RESULT", statusRedLight, cardNoCurrent, messageRedLight);
                                }
                                else
                                {
                                    await SendNotificationHub("CONFIRM_RESULT", 0, cardNoCurrent, $"Xác thực thất bại");

                                    SendNotificationAPI("CONFIRM_RESULT", 0, cardNoCurrent, $"Xác thực thất bại");

                                    _confirmLogger.LogError($"Co loi xay ra khi xac thuc rfid: {cardNoCurrent}");
                                }

                                _confirmLogger.LogInfo($"10. Giai phong RFID IN");

                                Program.IsLockingRfidIn = false;
                            }
                            catch (Exception ex)
                            {
                                _confirmLogger.LogError($@"Co loi xay ra khi xu ly RFID {ex.StackTrace} {ex.Message} ");
                                continue;
                            }
                        }
                    }
                    else
                    {
                        _confirmLogger.LogWarn("Disconnected!");
                        Thread.Sleep(2000);
                    }
                   
                    StaticClassReaderB.CloseNetPort(PortHandle);
                }
            }
            else
            {
                DeviceConnected = false;
                h21 = IntPtr.Zero;

                AuthenticateConfirmModuleFromPegasus();
            }
        }

        public string ByteArrayToString(byte[] b)
        {
            return BitConverter.ToString(b).Replace("-", "");
        }
        public string GetTrafficLightIpAddress()
        {
            var ipAddress = "";

            ipAddress = trafficLight?.IpAddress;

            return ipAddress;
        }

        public bool TurnOnGreenTrafficLight()
        {
            var ipAddress = GetTrafficLightIpAddress();

            if (String.IsNullOrEmpty(ipAddress))
            {
                return false;
            }

            _confirmLogger.LogInfo($"7.1. IP đèn: {ipAddress}");

            _trafficLight.Connect(ipAddress);

            return _trafficLight.TurnOnGreenOffRed();
        }

        public bool TurnOnRedTrafficLight()
        {
            var ipAddress = GetTrafficLightIpAddress();

            if (String.IsNullOrEmpty(ipAddress))
            {
                return false;
            }

            _confirmLogger.LogInfo($"8.1. IP đèn: {ipAddress}");

            _trafficLight.Connect(ipAddress);

            return _trafficLight.TurnOffGreenOnRed();
        }

        private async Task SendNotificationHub(string name, int status, string cardNo, string message, string vehicle = "")
        {
            new ConfirmHub().SendNotificationConfirmationPoint(name, status, cardNo, message, vehicle);
        }

        public void SendNotificationAPI(string name, int status, string cardNo, string message, string vehicle = "")
        {
            try
            {
                _notification.SendConfirmNotification(name, status, cardNo, message, vehicle);
            }
            catch (Exception ex)
            {
                _confirmLogger.LogInfo($"SendNotificationAPI Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }
    }
}
