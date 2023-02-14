using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_TRAM951_2.Models.Response;
using System.Runtime.InteropServices;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Common;
using System.Threading;
using XHTD_SERVICES_TRAM951_2.Hubs;
using Autofac;
using XHTD_SERVICES_TRAM951_2.Devices;

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

        protected readonly Tram951Logger _tram951Logger;

        private IntPtr h21 = IntPtr.Zero;

        private static bool DeviceConnected = false;

        private List<CardNoLog> tmpCardNoLst = new List<CardNoLog>();

        private List<CardNoLog> tmpInvalidCardNoLst = new List<CardNoLog>();

        private tblCategoriesDevice
            c3400,
            rfidIn11,
            rfidIn12,
            rfidIn21,
            rfidIn22;

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
            Tram951Logger tram951Logger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _rfidRepository = rfidRepository;
            _categoriesDevicesRepository = categoriesDevicesRepository;
            _categoriesDevicesLogRepository = categoriesDevicesLogRepository;
            _vehicleRepository = vehicleRepository;
            _scaleOperatingRepository = scaleOperatingRepository;
            _tram951Logger = tram951Logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                _tram951Logger.LogInfo("Start tram951 2 service");
                _tram951Logger.LogInfo("----------------------------");

                // Get devices info
                await LoadDevicesInfo();

                AuthenticateTram951Module();
            });
        }

        public async Task LoadDevicesInfo()
        {
            var devices = await _categoriesDevicesRepository.GetDevices("951");

            c3400 = devices.FirstOrDefault(x => x.Code == "951-2.C3-400");

            rfidIn11 = devices.FirstOrDefault(x => x.Code == "951-2.C3-400.RFID-IN-1");
            rfidIn12 = devices.FirstOrDefault(x => x.Code == "951-2.C3-400.RFID-IN-2");
            rfidIn21 = devices.FirstOrDefault(x => x.Code == "951-2.C3-400.RFID-OUT-1");
            rfidIn22 = devices.FirstOrDefault(x => x.Code == "951-2.C3-400.RFID-OUT-2");
        }

        public void AuthenticateTram951Module()
        {
            while (!DeviceConnected)
            {
                ConnectTram951Module();
            }

            ReadDataFromC3400();
        }

        public bool ConnectTram951Module()
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
                        _tram951Logger.LogInfo($"Connected to C3-400 {ipAddress}");

                        DeviceConnected = true;
                    }
                    else
                    {
                        _tram951Logger.LogInfo($"Connect to C3-400 {ipAddress} failed");

                        ret = PullLastError();
                        DeviceConnected = false;
                    }
                }
                return DeviceConnected;
            }
            catch (Exception ex)
            {
                _tram951Logger.LogInfo($@"ConnectTram951Module {ipAddress} error: {ex.Message}");
                return false;
            }
        }

        public async void ReadDataFromC3400()
        {
            _tram951Logger.LogInfo("Reading RFID from C3-400 ...");

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

                                    // 1. Xác định xe vào hay ra
                                    var isLuongVao = doorCurrent == rfidIn11.PortNumberDeviceIn.ToString()
                                                    || doorCurrent == rfidIn12.PortNumberDeviceIn.ToString();

                                    var isLuongRa = doorCurrent == rfidIn21.PortNumberDeviceIn.ToString()
                                                    || doorCurrent == rfidIn22.PortNumberDeviceIn.ToString();

                                    // 2. Loại bỏ các tag đã check trước đó
                                    if (tmpInvalidCardNoLst.Count > 10) tmpInvalidCardNoLst.RemoveRange(0, 3);
                                    if (tmpInvalidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-3)))
                                    {
                                        //_tram951Logger.LogInfo($@"2. Tag da duoc check truoc do => Ket thuc.");
                                        continue;
                                    }

                                    if (tmpCardNoLst.Count > 5) tmpCardNoLst.RemoveRange(0, 4);
                                    if (tmpCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-5)))
                                    {
                                        //_tram951Logger.LogInfo($"2. Tag da duoc check truoc do => Ket thuc.");
                                        continue;
                                    }

                                    _tram951Logger.LogInfo("----------------------------");
                                    _tram951Logger.LogInfo($"Tag: {cardNoCurrent}, door: {doorCurrent}, time: {timeCurrent}");
                                    _tram951Logger.LogInfo("-----");

                                    if (isLuongVao)
                                    {
                                        _tram951Logger.LogInfo($"1. Xe can vao");
                                    }
                                    else
                                    {
                                        _tram951Logger.LogInfo($"1. Xe can ra");
                                    }

                                    _tram951Logger.LogInfo($"2. Kiem tra tag da check truoc do");

                                    // 3. Kiểm tra cardNoCurrent có hợp lệ hay không
                                    bool isValid = _rfidRepository.CheckValidCode(cardNoCurrent);
                                    if (isValid)
                                    {
                                        _tram951Logger.LogInfo($"3. Tag hop le");
                                    }
                                    else
                                    {
                                        _tram951Logger.LogInfo($"3. Tag KHONG hop le => Ket thuc.");

                                        new ScaleHub().SendMessage("Notification", $"Phương tiện RFID {cardNoCurrent} chưa dán thẻ");

                                        new ScaleHub().SendMessage("VEHICLE_2_STATUS", $"RFID {cardNoCurrent} không thuộc hệ thống");

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };

                                        tmpInvalidCardNoLst.Add(newCardNoLog);

                                        continue;
                                    }

                                    // Nếu đang cân xe khác thì bỏ qua RFID hiện tại
                                    if (Program.IsScalling951)
                                    {
                                        var scaleInfo = _scaleOperatingRepository.GetDetail(ScaleCode.CODE_SCALE_2);
                                        if (scaleInfo != null
                                            && (bool)scaleInfo.IsScaling
                                            && !String.IsNullOrEmpty(scaleInfo.DeliveryCode))
                                        {
                                            new ScaleHub().SendMessage("Notification", $"== Can 2 dang hoat dong => Ket thuc {cardNoCurrent} ==");

                                            // TODO: cần kiểm tra đơn hàng DeliveryCode, nếu chưa có weightIn thì mới bỏ qua RFID này
                                            _tram951Logger.LogInfo($"== Can 951 dang hoat dong => Ket thuc ==");
                                            continue;
                                        }
                                    }

                                    // 4. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
                                    tblStoreOrderOperating currentOrder = null;
                                    if (isLuongVao)
                                    {
                                        currentOrder = await _storeOrderOperatingRepository.GetCurrentOrderEntraceTram951ByCardNo(cardNoCurrent);
                                    }
                                    else if (isLuongRa)
                                    {
                                        currentOrder = await _storeOrderOperatingRepository.GetCurrentOrderExitTram951ByCardNo(cardNoCurrent);
                                    }

                                    if (currentOrder == null)
                                    {
                                        _tram951Logger.LogInfo($"4. Tag KHONG co don hang hop le => Ket thuc.");

                                        new ScaleHub().SendMessage("Notification", $"Phương tiện RFID {cardNoCurrent} không có đơn hàng");

                                        new ScaleHub().SendMessage("VEHICLE_2_STATUS", $"RFID {cardNoCurrent} không có đơn hàng");

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };

                                        tmpInvalidCardNoLst.Add(newCardNoLog);

                                        continue;
                                    }

                                    new ScaleHub().SendMessage("Notification", $"Phương tiện RFID {cardNoCurrent} hợp lệ");

                                    new ScaleHub().SendMessage("VEHICLE_2_STATUS", $"RFID {cardNoCurrent} hợp lệ");

                                    _tram951Logger.LogInfo($"4. Tag co don hang hop le DeliveryCode = {currentOrder.DeliveryCode}");

                                    // 5. Xác thực cân vào
                                    if (isLuongVao)
                                    {
                                        //var isUpdatedOrder = await _storeOrderOperatingRepository.UpdateOrderConfirm3(cardNoCurrent);
                                        // TODO for test
                                        var isUpdatedOrder = true;

                                        if (isUpdatedOrder)
                                        {
                                            _tram951Logger.LogInfo($@"5. Đã xác thực trạng thái Cân vào");

                                            // 6. Đánh dấu đang cân
                                            await _scaleOperatingRepository.UpdateWhenConfirmEntrace(ScaleCode.CODE_SCALE_2, currentOrder.DeliveryCode, currentOrder.Vehicle, currentOrder.CardNo);
                                            Program.IsScalling951 = true;

                                            _tram951Logger.LogInfo($@"6. Đánh dấu xe đang cân vao");

                                            tmpCardNoLst.Add(new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now });

                                            // Bat den do
                                            _tram951Logger.LogInfo($@"7. Bat den do can vao");

                                            DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnRedTrafficLight(ScaleCode.CODE_SCALE_2_DGT_IN);
                                            Thread.Sleep(500);
                                        }
                                        else
                                        {
                                            _tram951Logger.LogInfo($@"5. Confirm 3 failed");
                                        }
                                    }

                                    // 5. Xác thực cân ra
                                    else if (isLuongRa)
                                    {
                                        //var isUpdatedOrder = await _storeOrderOperatingRepository.UpdateOrderConfirm7(cardNoCurrent);
                                        // TODO for test
                                        var isUpdatedOrder = true;

                                        if (isUpdatedOrder)
                                        {
                                            _tram951Logger.LogInfo($@"5. Đã xác thực trạng thái Cân ra");

                                            // 6. Đánh dấu đang cân
                                            await _scaleOperatingRepository.UpdateWhenConfirmExit(ScaleCode.CODE_SCALE_2, currentOrder.DeliveryCode, currentOrder.Vehicle, currentOrder.CardNo);
                                            Program.IsScalling951 = true;

                                            _tram951Logger.LogInfo($@"6. Đánh dấu xe đang cân ra");

                                            tmpCardNoLst.Add(new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now });

                                            // Bat den do
                                            _tram951Logger.LogInfo($@"7. Bat den do can ra");
                                            
                                            DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnRedTrafficLight(ScaleCode.CODE_SCALE_2_DGT_OUT);
                                            Thread.Sleep(500);
                                        }
                                        else
                                        {
                                            _tram951Logger.LogInfo($@"5. Confirm 7 failed");
                                        }
                                    }

                                }
                            }
                            catch (Exception ex)
                            {
                                _tram951Logger.LogError($@"Co loi xay ra khi xu ly RFID {ex.StackTrace} {ex.Message} ");
                                continue;
                            }
                        }
                        else
                        {
                            _tram951Logger.LogWarn("No data. Reconnect ...");
                            DeviceConnected = false;
                            h21 = IntPtr.Zero;

                            AuthenticateTram951Module();
                        }
                    }
                }
            }
            else
            {
                _tram951Logger.LogWarn("No data. Reconnect ...");
                DeviceConnected = false;
                h21 = IntPtr.Zero;

                AuthenticateTram951Module();
            }
        }
    }
}
