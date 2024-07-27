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
        private int PortHandle = 2000;
        private string PegasusAdr = "192.168.13.162";

        public ConfirmModuleJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            RfidRepository rfidRepository,
            CategoriesDevicesRepository categoriesDevicesRepository,
            CategoriesDevicesLogRepository categoriesDevicesLogRepository,
            SystemParameterRepository systemParameterRepository,
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

        public void AuthenticateConfirmModuleFromPegasus()
        {
            // 1. Connect Device
            int port = PortHandle;
            var openResult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr, ref ComAddr, ref port);
            while (openResult != 0)
            {
                openResult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr, ref ComAddr, ref port);
            }
            _confirmLogger.LogInfo("Connected Pegasus");
            DeviceConnected = true;
            // 2. Đọc dữ liệu từ thiết bị
            ReadDataFromPegasus();
        }

        public async void ReadDataFromPegasus()
        {
            _confirmLogger.LogInfo($"Reading Pegasus...");
            while (DeviceConnected)
            {
                var data = PegasusReader.Inventory_G2(ref ComAddr, 0, 0, 0, PortHandle);

                foreach (var item in data)
                {
                    try
                    {
                        var cardNoCurrent = ByteArrayToString(item);

                        await ReadDataProcess(cardNoCurrent);
                    }
                    catch (Exception ex)
                    {
                        _confirmLogger.LogError($@"Co loi xay ra khi xu ly RFID {ex.StackTrace} {ex.Message} ");
                        continue;
                    }
                }
            }
        }

        private async Task ReadDataProcess(string cardNoCurrent)
        {
            if (Program.IsLockingRfid)
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
                //_confirmLogger.LogInfo($@"2. Tag KHONG HOP LE da duoc check truoc do => Ket thuc.");
                return;
            }

            if (tmpValidCardNoLst.Count > 10)
            {
                tmpValidCardNoLst.RemoveRange(0, 3);
            }

            if (tmpValidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-3)))
            {
                //_confirmLogger.LogInfo($@"2. Tag HOP LE da duoc check truoc do => Ket thuc.");
                return;
            }

            _confirmLogger.LogInfo("----------------------------");
            _confirmLogger.LogInfo($"Tag: {cardNoCurrent}");
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

                return;
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

                return;
            }

            // Nếu RFID không có đơn hàng hợp lệ
            else if (isValidCardNo == false)
            {
                _confirmLogger.LogInfo($"4. Tag KHONG co don hang hop le => Ket thuc.");

                await SendNotificationHub("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng hợp lệ");

                SendNotificationAPI("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng hợp lệ");

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                tmpInvalidCardNoLst.Add(newCardNoLog);

                return;
            }

            // Nếu RFID có đơn hàng hợp lệ
            else
            {
                await SendNotificationHub("CONFIRM_VEHICLE", 2, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} có đơn hàng hợp lệ", vehicleCodeCurrent);

                SendNotificationAPI("CONFIRM_VEHICLE", 2, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} có đơn hàng hợp lệ", vehicleCodeCurrent);

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };

                tmpValidCardNoLst.Add(newCardNoLog);

                Program.IsLockingRfid = true;
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

                _confirmLogger.LogInfo($"7. Bật đèn xanh");
                if (TurnOnGreenTrafficLight())
                {
                    _confirmLogger.LogInfo($"7.2. Bật đèn xanh thành công");
                }
                else
                {
                    _confirmLogger.LogInfo($"7.2. Bật đèn xanh thất bại");
                }

                //var img = new HikvisionStreamCamera().CaptureStream(CAMERA_IP, CAMERA_USER_NAME, CAMERA_PASSWORD, "CONFIRM", CAMERA_NUMBER, IMG_PATH);
                //if (!string.IsNullOrEmpty(img))
                //{
                //    _storeOrderOperatingRepository.UpdateImgConfirm10(vehicleCodeCurrent, img);
                //}

                Thread.Sleep(10000);

                _confirmLogger.LogInfo($"8. Bật đèn đỏ");
                if (TurnOnRedTrafficLight())
                {
                    _confirmLogger.LogInfo($"8.2. Bật đèn đỏ thành công");
                }
                else
                {
                    _confirmLogger.LogInfo($"8.2. Bật đèn đỏ thất bại");
                }
            }
            else
            {
                await SendNotificationHub("CONFIRM_RESULT", 0, cardNoCurrent, $"Xác thực thất bại");
                SendNotificationAPI("CONFIRM_RESULT", 0, cardNoCurrent, $"Xác thực thất bại");

                _confirmLogger.LogError($"Co loi xay ra khi xac thuc rfid: {cardNoCurrent}");
            }

            _confirmLogger.LogInfo($"10. Giai phong RFID IN");

            Program.IsLockingRfid = false;
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

        #region Read RFID by C3-400
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

                                    if (Program.IsLockingRfid)
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

                                        Program.IsLockingRfid = true;
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

                                    Program.IsLockingRfid = false;
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
        #endregion
    }
}
