using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_XB_TROUGH_1.Models.Response;
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
using XHTD_SERVICES_XB_TROUGH_1.Business;
using XHTD_SERVICES_XB_TROUGH_1.Hubs;
using System.Net.NetworkInformation;
using XHTD_SERVICES_XB_TROUGH_1.Devices;
using PK_UHF_Test;

namespace XHTD_SERVICES_XB_TROUGH_1.Jobs
{
    public class XibaoTrough1Job : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly RfidRepository _rfidRepository;

        protected readonly CategoriesDevicesRepository _categoriesDevicesRepository;

        protected readonly CategoriesDevicesLogRepository _categoriesDevicesLogRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly Notification _notification;

        protected readonly Trough1Logger _trough1Logger;

        private IntPtr h21 = IntPtr.Zero;

        private static bool DeviceConnected = false;

        private List<CardNoLog> tmpValidCardNoLst = new List<CardNoLog>();

        private List<CardNoLog> tmpInvalidCardNoLst = new List<CardNoLog>();

        private tblCategoriesDevice c3400;

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
        private string PegasusAdr = "192.168.13.191";

        public XibaoTrough1Job(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            RfidRepository rfidRepository,
            CategoriesDevicesRepository categoriesDevicesRepository,
            CategoriesDevicesLogRepository categoriesDevicesLogRepository,
            SystemParameterRepository systemParameterRepository,
            Notification notification,
            Trough1Logger trough1Logger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _rfidRepository = rfidRepository;
            _categoriesDevicesRepository = categoriesDevicesRepository;
            _categoriesDevicesLogRepository = categoriesDevicesLogRepository;
            _systemParameterRepository = systemParameterRepository;
            _notification = notification;
            _trough1Logger = trough1Logger;
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
                    _trough1Logger.LogInfo("Service nhận diện RFID máng 1 xi bao đang TẮT.");
                    return;
                }

                _trough1Logger.LogInfo("Start Xibao Trough 1 service");
                _trough1Logger.LogInfo("----------------------------");

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
            _trough1Logger.LogInfo($"Connected Pegasus IP:{PegasusAdr} - Port: {PortHandle}");
            DeviceConnected = true;

            // 2. Đọc dữ liệu từ thiết bị
            ReadDataFromPegasus();
        }

        public async void ReadDataFromPegasus()
        {
            _trough1Logger.LogInfo($"Reading Pegasus...");
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
                        _trough1Logger.LogError($@"Co loi xay ra khi xu ly RFID {ex.StackTrace} {ex.Message} ");
                        continue;
                    }
                }
            }
        }

        private async Task ReadDataProcess(string cardNoCurrent)
        {
            if (Program.IsLockingRfid)
            {
                _trough1Logger.LogInfo($"== Diem xac thuc dang xu ly => Ket thuc {cardNoCurrent} == ");

                new Trough1Hub().SendMessage("IS_LOCKING_RFID", "1");
            }
            else
            {
                new Trough1Hub().SendMessage("IS_LOCKING_RFID", "0");
            }

            // Loại bỏ các tag đã check trước đó
            if (tmpInvalidCardNoLst.Count > 10)
            {
                tmpInvalidCardNoLst.RemoveRange(0, 3);
            }

            if (tmpInvalidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddSeconds(-15)))
            {
                //_trough1Logger.LogInfo($@"2. Tag KHONG HOP LE da duoc check truoc do => Ket thuc.");
                return;
            }

            if (tmpValidCardNoLst.Count > 10)
            {
                tmpValidCardNoLst.RemoveRange(0, 3);
            }

            if (tmpValidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-3)))
            {
                //_trough1Logger.LogInfo($@"2. Tag HOP LE da duoc check truoc do => Ket thuc.");
                return;
            }

            _trough1Logger.LogInfo("----------------------------");
            _trough1Logger.LogInfo($"Tag: {cardNoCurrent}");
            _trough1Logger.LogInfo("-----");

            _trough1Logger.LogInfo($"2. Kiem tra tag da check truoc do");

            // Kiểm tra RFID có hợp lệ hay không
            string vehicleCodeCurrent = _rfidRepository.GetVehicleCodeByCardNo(cardNoCurrent);

            if (!String.IsNullOrEmpty(vehicleCodeCurrent))
            {
                _trough1Logger.LogInfo($"3. Tag hop le: vehicle={vehicleCodeCurrent}");
            }
            else
            {
                _trough1Logger.LogInfo($"3. Tag KHONG hop le => Ket thuc.");

                SendNotificationHub("CONFIRM_VEHICLE", 0, cardNoCurrent, $"RFID {cardNoCurrent} không thuộc hệ thống");
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
                _trough1Logger.LogInfo($"4. Tag KHONG co don hang => Ket thuc.");

                SendNotificationHub("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng");
                SendNotificationAPI("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng");

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                tmpInvalidCardNoLst.Add(newCardNoLog);

                return;
            }

            // Nếu RFID không có đơn hàng hợp lệ
            else if (isValidCardNo == false)
            {
                _trough1Logger.LogInfo($"4. Tag KHONG co don hang hop le => Ket thuc.");

                SendNotificationHub("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng hợp lệ");
                SendNotificationAPI("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng hợp lệ");

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                tmpInvalidCardNoLst.Add(newCardNoLog);

                return;
            }

            // Nếu RFID có đơn hàng hợp lệ
            else
            {
                SendNotificationHub("CONFIRM_VEHICLE", 2, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} có đơn hàng hợp lệ", vehicleCodeCurrent);
                SendNotificationAPI("CONFIRM_VEHICLE", 2, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} có đơn hàng hợp lệ", vehicleCodeCurrent);

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };

                tmpValidCardNoLst.Add(newCardNoLog);

                Program.IsLockingRfid = true;
            }

            var currentDeliveryCode = currentOrder.DeliveryCode;
            _trough1Logger.LogInfo($"4. Tag co don hang hop le DeliveryCode = {currentDeliveryCode}");

            // Xác thực
            bool isConfirmSuccess = this._storeOrderOperatingRepository.UpdateBillOrderConfirm10(vehicleCodeCurrent);

            // Xác thực thành công
            if (isConfirmSuccess)
            {
                SendNotificationHub("CONFIRM_RESULT", 1, cardNoCurrent, $"Xác thực thành công", vehicleCodeCurrent);
                SendNotificationAPI("CONFIRM_RESULT", 1, cardNoCurrent, $"Xác thực thành công", vehicleCodeCurrent);

                // Xếp số
                this._storeOrderOperatingRepository.UpdateIndexOrderForNewConfirm(vehicleCodeCurrent);

                // Chụp ảnh
                //var img = new HikvisionStreamCamera().CaptureStream(CAMERA_IP, CAMERA_USER_NAME, CAMERA_PASSWORD, "CONFIRM", CAMERA_NUMBER, IMG_PATH);
                //if (!string.IsNullOrEmpty(img))
                //{
                //    _storeOrderOperatingRepository.UpdateImgConfirm10(vehicleCodeCurrent, img);
                //}
            }
            else
            {
                SendNotificationHub("CONFIRM_RESULT", 0, cardNoCurrent, $"Xác thực thất bại");
                SendNotificationAPI("CONFIRM_RESULT", 0, cardNoCurrent, $"Xác thực thất bại");

                _trough1Logger.LogError($"Co loi xay ra khi xac thuc rfid: {cardNoCurrent}");
            }

            _trough1Logger.LogInfo($"10. Giai phong RFID IN");

            Program.IsLockingRfid = false;
        }

        public string ByteArrayToString(byte[] b)
        {
            return BitConverter.ToString(b).Replace("-", "");
        }

        private void SendNotificationHub(string name, int status, string cardNo, string message, string vehicle = "")
        {
            new Trough1Hub().SendNotificationConfirmationPoint(name, status, cardNo, message, vehicle);
        }

        public void SendNotificationAPI(string name, int status, string cardNo, string message, string vehicle = "")
        {
            try
            {
                _notification.SendConfirmNotification(name, status, cardNo, message, vehicle);
            }
            catch (Exception ex)
            {
                _trough1Logger.LogInfo($"SendNotificationAPI Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
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
                        _trough1Logger.LogInfo($"Connected to C3-400 {ipAddress}");

                        DeviceConnected = true;
                    }
                    else
                    {
                        _trough1Logger.LogInfo($"Connect to C3-400 {ipAddress} failed");
                        ret = PullLastError();
                        DeviceConnected = false;
                    }
                }
                return DeviceConnected;
            }
            catch (Exception ex)
            {
                _trough1Logger.LogInfo($@"Connect to C3-400 {ipAddress} error: {ex.Message}");
                return false;
            }
        }

        public async void ReadDataFromC3400()
        {
            _trough1Logger.LogInfo("Reading RFID from C3-400 ...");

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
                                        _trough1Logger.LogInfo($"== Diem xac thuc dang xu ly => Ket thuc {cardNoCurrent} == ");

                                        new Trough1Hub().SendMessage("IS_LOCKING_RFID", "1");
                                    }
                                    else
                                    {
                                        new Trough1Hub().SendMessage("IS_LOCKING_RFID", "0");
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

                                    _trough1Logger.LogInfo("----------------------------");
                                    _trough1Logger.LogInfo($"Tag: {cardNoCurrent}, door: {doorCurrent}, time: {timeCurrent}");
                                    _trough1Logger.LogInfo("-----");

                                    _trough1Logger.LogInfo($"2. Kiem tra tag da check truoc do");

                                    // Kiểm tra RFID có hợp lệ hay không
                                    string vehicleCodeCurrent = _rfidRepository.GetVehicleCodeByCardNo(cardNoCurrent);

                                    if (!String.IsNullOrEmpty(vehicleCodeCurrent))
                                    {
                                        _trough1Logger.LogInfo($"3. Tag hop le: vehicle={vehicleCodeCurrent}");
                                    }
                                    else
                                    {
                                        _trough1Logger.LogInfo($"3. Tag KHONG hop le => Ket thuc.");

                                        SendNotificationHub("CONFIRM_VEHICLE", 0, cardNoCurrent, $"RFID {cardNoCurrent} không thuộc hệ thống");
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
                                        _trough1Logger.LogInfo($"4. Tag KHONG co don hang => Ket thuc.");

                                        SendNotificationHub("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng");
                                        SendNotificationAPI("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng");

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                        tmpInvalidCardNoLst.Add(newCardNoLog);

                                        continue;
                                    }

                                    // Nếu RFID không có đơn hàng hợp lệ
                                    else if (isValidCardNo == false)
                                    {
                                        _trough1Logger.LogInfo($"4. Tag KHONG co don hang hop le => Ket thuc.");

                                        SendNotificationHub("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng hợp lệ");
                                        SendNotificationAPI("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng hợp lệ");

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                        tmpInvalidCardNoLst.Add(newCardNoLog);

                                        continue;
                                    }

                                    // Nếu RFID có đơn hàng hợp lệ
                                    else
                                    {
                                        SendNotificationHub("CONFIRM_VEHICLE", 2, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} có đơn hàng hợp lệ", vehicleCodeCurrent);
                                        SendNotificationAPI("CONFIRM_VEHICLE", 2, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} có đơn hàng hợp lệ", vehicleCodeCurrent);

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };

                                        tmpValidCardNoLst.Add(newCardNoLog);

                                        Program.IsLockingRfid = true;
                                    }

                                    var currentDeliveryCode = currentOrder.DeliveryCode;
                                    _trough1Logger.LogInfo($"4. Tag co don hang hop le DeliveryCode = {currentDeliveryCode}");

                                    // Xác thực
                                    bool isConfirmSuccess = this._storeOrderOperatingRepository.UpdateBillOrderConfirm10(vehicleCodeCurrent);

                                    // Xác thực thành công
                                    if (isConfirmSuccess)
                                    {
                                        SendNotificationHub("CONFIRM_RESULT", 1, cardNoCurrent, $"Xác thực thành công", vehicleCodeCurrent);
                                        SendNotificationAPI("CONFIRM_RESULT", 1, cardNoCurrent, $"Xác thực thành công", vehicleCodeCurrent);

                                        // Xếp số
                                        this._storeOrderOperatingRepository.UpdateIndexOrderForNewConfirm(vehicleCodeCurrent);
                                    }
                                    else
                                    {
                                        SendNotificationHub("CONFIRM_RESULT", 0, cardNoCurrent, $"Xác thực thất bại");
                                        SendNotificationAPI("CONFIRM_RESULT", 0, cardNoCurrent, $"Xác thực thất bại");

                                        _trough1Logger.LogError($"Co loi xay ra khi xac thuc rfid: {cardNoCurrent}");
                                    }

                                    _trough1Logger.LogInfo($"10. Giai phong RFID IN");

                                    Program.IsLockingRfid = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                _trough1Logger.LogError($@"Co loi xay ra khi xu ly RFID {ex.StackTrace} {ex.Message} ");
                                continue;
                            }
                        }
                        else
                        {
                            _trough1Logger.LogWarn("No data. Reconnect ...");
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
