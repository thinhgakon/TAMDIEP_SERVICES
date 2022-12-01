using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_GATEWAY.Models.Response;
using XHTD_SERVICES.Data.Models.Response;
using System.Runtime.InteropServices;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES.Data.Entities;
using Newtonsoft.Json;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES.Helper.Models.Request;

namespace XHTD_SERVICES_GATEWAY.Jobs
{
    public class GatewayModuleJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly RfidRepository _rfidRepository;

        protected readonly CategoriesDevicesRepository _categoriesDevicesRepository;

        protected readonly CategoriesDevicesLogRepository _categoriesDevicesLogRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly Barrier _barrier;

        protected readonly TCPTrafficLight _trafficLight;

        protected readonly Notification _notification;

        protected readonly GatewayLogger _gatewayLogger;

        private IntPtr h21 = IntPtr.Zero;

        private static bool DeviceConnected = false;

        private List<CardNoLog> tmpCardNoLst_In = new List<CardNoLog>();

        private List<CardNoLog> tmpCardNoLst_Out = new List<CardNoLog>();

        private List<CardNoLog> tmpInvalidCardNoLst = new List<CardNoLog>();

        private tblCategoriesDevice c3400, rfidRa1, rfidRa2, rfidVao1, rfidVao2, m221, barrierVao, barrierRa, trafficLightVao, trafficLightRa;

        protected const string CBV_ACTIVE = "CBV_ACTIVE";

        private static bool isActiveService = true;

        [DllImport(@"C:\Windows\System32\plcommpro.dll", EntryPoint = "Connect")]
        public static extern IntPtr Connect(string Parameters);

        [DllImport(@"C:\Windows\System32\plcommpro.dll", EntryPoint = "PullLastError")]
        public static extern int PullLastError();

        [DllImport(@"C:\Windows\System32\plcommpro.dll", EntryPoint = "GetRTLog")]
        public static extern int GetRTLog(IntPtr h, ref byte buffer, int buffersize);

        public GatewayModuleJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository, 
            RfidRepository rfidRepository,
            CategoriesDevicesRepository categoriesDevicesRepository,
            CategoriesDevicesLogRepository categoriesDevicesLogRepository,
            SystemParameterRepository systemParameterRepository,
            Barrier barrier,
            TCPTrafficLight trafficLight,
            Notification notification,
            GatewayLogger gatewayLogger
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
            _gatewayLogger = gatewayLogger;
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
                    _gatewayLogger.LogInfo("Service cong bao ve dang TAT.");
                    return;
                }

                _gatewayLogger.LogInfo("Start gateway service");
                _gatewayLogger.LogInfo("----------------------------");

                // Get devices info
                await LoadDevicesInfo();

                AuthenticateGatewayModule();
            });                                                                                                                     
        }

        public async Task LoadSystemParameters()
        {
            var parameters = await _systemParameterRepository.GetSystemParameters();

            var activeParameter = parameters.FirstOrDefault(x => x.Code == CBV_ACTIVE);

            if (activeParameter == null || activeParameter.Value == "0")
            {
                isActiveService = false;
            }
        }

        public async Task LoadDevicesInfo()
        {
            var devices = await _categoriesDevicesRepository.GetDevices("BV");

            c3400 = devices.FirstOrDefault(x => x.Code == "BV.C3-400");
            rfidRa1 = devices.FirstOrDefault(x => x.Code == "BV.C3-400.RFID.RA-1");
            rfidRa2 = devices.FirstOrDefault(x => x.Code == "BV.C3-400.RFID.RA-2");
            rfidVao1 = devices.FirstOrDefault(x => x.Code == "BV.C3-400.RFID.VAO-1");
            rfidVao2 = devices.FirstOrDefault(x => x.Code == "BV.C3-400.RFID.VAO-2");

            m221 = devices.FirstOrDefault(x => x.Code == "BV.M221");
            barrierVao = devices.FirstOrDefault(x => x.Code == "BV.M221.BRE-1");
            barrierRa = devices.FirstOrDefault(x => x.Code == "BV.M221.BRE-2");
            trafficLightVao = devices.FirstOrDefault(x => x.Code == "BV.DGT-1");
            trafficLightRa = devices.FirstOrDefault(x => x.Code == "BV.DGT-2");
        }

        public void AuthenticateGatewayModule()
        {
            /*
             * 1. Xác định xe vào hay ra cổng theo gia tri door từ C3-400
             * 2. Loại bỏ các cardNoCurrent đã, đang xử lý (đã check trước đó)
             * 3. Kiểm tra cardNoCurrent có hợp lệ hay không
             * 4. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
             * 5. Cập nhật đơn hàng: Step
             * 6. Bật đèn xanh giao thông
             * 7. Mở barrier
             * 8. Ghi log thiết bị
             * 9. Bắn tín hiệu thông báo
             */

            // 1. Connect Device
            while (!DeviceConnected)
            {
                ConnectGatewayModule();
            }

            // 2. Đọc dữ liệu từ thiết bị
            ReadDataFromC3400();

        }

        public bool ConnectGatewayModule()
        {
            try
            {
                string str = $"protocol=TCP,ipaddress={c3400?.IpAddress},port={c3400?.PortNumber},timeout=2000,passwd=";
                int ret = 0;
                if (IntPtr.Zero == h21)
                {
                    h21 = Connect(str);
                    if (h21 != IntPtr.Zero)
                    {
                        _gatewayLogger.LogInfo("Connected to C3-400");

                        DeviceConnected = true;
                    }
                    else
                    {
                        _gatewayLogger.LogInfo("Connect to C3-400 failed");

                        ret = PullLastError();
                        DeviceConnected = false;
                    }
                }
                return DeviceConnected;
            }
            catch (Exception ex)
            {
                _gatewayLogger.LogInfo($@"ConnectGateway Exception: {ex.Message}");

                return false;
            }
        }

        public async void ReadDataFromC3400()
        {
            _gatewayLogger.LogInfo("Read data from C3-400");

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
                            str = Encoding.Default.GetString(buffer);
                            tmp = str.Split(',');

                            // Trường hợp bắt được tag RFID
                            if (tmp[2] != "0" && tmp[2] != "") {

                                var cardNoCurrent = tmp[2]?.ToString();
                                var doorCurrent = tmp[3]?.ToString();

                                _gatewayLogger.LogInfo("----------------------------");
                                _gatewayLogger.LogInfo($"Tag: {cardNoCurrent}, door: {doorCurrent}");
                                _gatewayLogger.LogInfo("-----");

                                // 1.Xác định xe cân vào / ra
                                var isLuongVao = doorCurrent == rfidVao1.PortNumberDeviceIn.ToString()
                                                || doorCurrent == rfidVao2.PortNumberDeviceIn.ToString();

                                var isLuongRa = doorCurrent == rfidRa1.PortNumberDeviceIn.ToString()
                                                || doorCurrent == rfidRa2.PortNumberDeviceIn.ToString();

                                var direction = 0;

                                if (isLuongVao)
                                {
                                    direction = 1;
                                    _gatewayLogger.LogInfo($"1. Xe can vao");
                                }
                                else
                                {
                                    direction = 2;
                                    _gatewayLogger.LogInfo($"1. Xe can ra");
                                }

                                // 2. Loại bỏ các tag đã check trước đó
                                if (tmpInvalidCardNoLst.Count > 5) tmpInvalidCardNoLst.RemoveRange(0, 3);

                                if (tmpInvalidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-2)))
                                {
                                    _gatewayLogger.LogInfo($@"2. Tag da duoc check truoc do => Ket thuc.");

                                    continue;
                                }

                                if (isLuongVao)
                                {
                                    if (tmpCardNoLst_In.Count > 5) tmpCardNoLst_In.RemoveRange(0, 3);

                                    if (tmpCardNoLst_In.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-1)))
                                    {
                                        _gatewayLogger.LogInfo($@"2. Tag da duoc check truoc do => Ket thuc.");

                                        continue;
                                    }
                                }
                                else if (isLuongRa)
                                {
                                    if (tmpCardNoLst_Out.Count > 5) tmpCardNoLst_Out.RemoveRange(0, 3);

                                    if (tmpCardNoLst_Out.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-1)))
                                    {
                                        _gatewayLogger.LogInfo($@"2. Tag da duoc check truoc do => Ket thuc.");

                                        continue;
                                    }
                                }

                                _gatewayLogger.LogInfo($"2. Kiem tra tag da check truoc do");

                                // 3. Kiểm tra cardNoCurrent có hợp lệ hay không
                                bool isValid = _rfidRepository.CheckValidCode(cardNoCurrent);

                                if (isValid)
                                {
                                    _gatewayLogger.LogInfo($"3. Tag hop le");
                                }
                                else
                                {
                                    _gatewayLogger.LogInfo($"3. Tag KHONG hop le => Ket thuc.");

                                    _notification.SendNotification(
                                        "CBV",
                                        null,
                                        0,
                                        "RFID không thuộc hệ thống",
                                        direction,
                                        null,
                                        null,
                                        Convert.ToInt32(cardNoCurrent),
                                        null,
                                        null,
                                        null
                                    );

                                    // Cần add các thẻ invalid vào 1 mảng để tránh phải check lại
                                    // Chỉ check lại các invalid tag sau 1 khoảng thời gian: 3 phút
                                    var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                    tmpInvalidCardNoLst.Add(newCardNoLog);

                                    continue;
                                }

                                // 4. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
                                List<tblStoreOrderOperating> currentOrders = null;
                                if (isLuongVao)
                                {
                                    currentOrders = await _storeOrderOperatingRepository.GetCurrentOrdersEntraceGatewayByCardNoReceiving(cardNoCurrent);
                                }
                                else if (isLuongRa)
                                {
                                    currentOrders = await _storeOrderOperatingRepository.GetCurrentOrdersExitGatewayByCardNoReceiving(cardNoCurrent);
                                }

                                if (currentOrders == null || currentOrders.Count == 0)
                                {

                                    _gatewayLogger.LogInfo($"4. Tag KHONG co don hang hop le => Ket thuc.");

                                    _notification.SendNotification(
                                        "CBV",
                                        null,
                                        0,
                                        "RFID không có đơn hàng hợp lệ",
                                        direction,
                                        null,
                                        null,
                                        Convert.ToInt32(cardNoCurrent),
                                        null,
                                        null,
                                        null
                                    );

                                    // Cần add các thẻ invalid vào 1 mảng để tránh phải check lại
                                    // Chỉ check lại các invalid tag sau 1 khoảng thời gian: 3 phút
                                    var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                    tmpInvalidCardNoLst.Add(newCardNoLog);

                                    continue;
                                }

                                var currentOrder = currentOrders.FirstOrDefault();
                                var deliveryCodes = String.Join(";", currentOrders.Select(x => x.DeliveryCode).ToArray());

                                _gatewayLogger.LogInfo($"4. Tag co cac don hang hop le DeliveryCode = {deliveryCodes}");

                                _notification.SendNotification(
                                    "CBV",
                                    null,
                                    1,
                                    "RFID có đơn hàng hợp lệ",
                                    direction,
                                    null,
                                    null,
                                    Convert.ToInt32(cardNoCurrent),
                                    null,
                                    null,
                                    null
                                );

                                // 5. Cập nhật đơn hàng
                                var isUpdatedOrder = false;

                                if (isLuongVao)
                                {
                                    isUpdatedOrder = await _storeOrderOperatingRepository.UpdateOrderEntraceGateway(cardNoCurrent);
                                }
                                else if (isLuongRa)
                                {
                                    isUpdatedOrder = await _storeOrderOperatingRepository.UpdateOrderExitGateway(cardNoCurrent);
                                }

                                if (isUpdatedOrder)
                                {
                                    _gatewayLogger.LogInfo($"5. Update don hang thanh cong.");

                                    var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };

                                    /*
                                     * 6. Bật đèn xanh giao thông, 
                                     * 7. Mở barrier
                                     * 8. Ghi log thiết bị
                                     * 9. Bắn tín hiệu thông báo
                                     */
                                    bool isSuccessTurnOnGreenTrafficLight = false;
                                    bool isSuccessOpenBarrier = false;

                                    if (isLuongVao)
                                    {
                                        tmpCardNoLst_In.Add(newCardNoLog);

                                        isSuccessOpenBarrier = OpenBarrier("IN");

                                        isSuccessTurnOnGreenTrafficLight = TurnOnGreenTrafficLight("IN");
                                    }
                                    else if (isLuongRa)
                                    {
                                        tmpCardNoLst_Out.Add(newCardNoLog);

                                        isSuccessOpenBarrier = OpenBarrier("OUT");

                                        isSuccessTurnOnGreenTrafficLight = TurnOnGreenTrafficLight("OUT");
                                    }

                                    if (isSuccessTurnOnGreenTrafficLight)
                                    {
                                        _gatewayLogger.LogInfo($"6. Bat den xanh thanh cong");
                                    }
                                    else
                                    {
                                        _gatewayLogger.LogInfo($"6. Bat den xanh KHONG thanh cong");
                                    }

                                    if (isSuccessOpenBarrier)
                                    {
                                        _gatewayLogger.LogInfo($"7. Mo barrier thanh cong");
                                        _gatewayLogger.LogInfo($"8. Ghi log thiet bi mo barrier");

                                        string luongText = isLuongVao ? "vào" : "ra";
                                        string deviceCode = isLuongVao ? "BV.M221.BRE-1" : "BV.M221.BRE-2";
                                        var newLog = new CategoriesDevicesLogItemResponse
                                        {
                                            Code = deviceCode,
                                            ActionType = 1,
                                            ActionInfo = $"Mở barrier cho xe {currentOrder.Vehicle} {luongText}, theo đơn hàng {deliveryCodes}",
                                            ActionDate = DateTime.Now,
                                        };

                                        await _categoriesDevicesLogRepository.CreateAsync(newLog);
                                    }
                                    else
                                    {
                                        _gatewayLogger.LogInfo($"7. Mo barrier KHONG thanh cong");
                                    }

                                    _gatewayLogger.LogInfo($"Ket thuc.");
                                }
                                else
                                {
                                    _gatewayLogger.LogInfo($"5. Update don hang KHONG thanh cong => Ket thuc.");
                                }
                            }
                        }
                        else
                        {
                            _gatewayLogger.LogWarn("Lỗi không đọc được dữ liệu, có thể do mất kết nối");
                            DeviceConnected = false;
                            h21 = IntPtr.Zero;

                            AuthenticateGatewayModule();
                        }
                    }
                }
            }
        }

        public bool OpenBarrier(string luong)
        {
            int portNumberDeviceIn = luong == "IN" ? (int)barrierVao.PortNumberDeviceIn : (int)barrierRa.PortNumberDeviceIn;
            int portNumberDeviceOut = luong == "IN" ? (int)barrierVao.PortNumberDeviceOut : (int)barrierRa.PortNumberDeviceOut;

            return _barrier.TurnOn(m221.IpAddress, (int)m221.PortNumber, portNumberDeviceIn, portNumberDeviceOut);
        }

        public bool TurnOnGreenTrafficLight(string luong)
        {
            if (trafficLightVao == null || trafficLightRa == null)
            {
                return false;
            }

            string ipAddress = luong == "IN" ? trafficLightVao.IpAddress : trafficLightRa.IpAddress;

            _trafficLight.Connect($"{ipAddress}");

            return _trafficLight.TurnOnGreenOffRed();
        }
    }
}
