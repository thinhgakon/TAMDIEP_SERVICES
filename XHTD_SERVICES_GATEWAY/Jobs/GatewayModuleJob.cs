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
using Microsoft.AspNet.SignalR.Client;
using System.Threading;
using XHTD_SERVICES.Data.Common;

namespace XHTD_SERVICES_GATEWAY.Jobs
{
    public class GatewayModuleJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly RfidRepository _rfidRepository;

        protected readonly CategoriesDevicesRepository _categoriesDevicesRepository;

        protected readonly CategoriesDevicesLogRepository _categoriesDevicesLogRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly PLCBarrier _barrier;

        protected readonly TCPTrafficLight _trafficLight;

        protected readonly Notification _notification;

        protected readonly GatewayLogger _gatewayLogger;

        private IntPtr h21 = IntPtr.Zero;

        private static bool DeviceConnected = false;

        private List<CardNoLog> tmpCardNoLst_In = new List<CardNoLog>();

        private List<CardNoLog> tmpCardNoLst_Out = new List<CardNoLog>();

        private List<CardNoLog> tmpInvalidCardNoLst = new List<CardNoLog>();

        private tblCategoriesDevice c3400, rfidRa1, rfidRa2, rfidVao1, rfidVao2, m221, barrierVao, barrierRa, trafficLightIn, trafficLightOut;

        protected const string CBV_ACTIVE = "CBV_ACTIVE";

        private static bool isActiveService = true;

        private IHubProxy HubProxy { get; set; }

        private string ServerURI = URIConfig.SIGNALR_GATEWAY_SERVICE_URL;

        private HubConnection Connection { get; set; }

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "Connect")]
        public static extern IntPtr Connect(string Parameters);

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "PullLastError")]
        public static extern int PullLastError();

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "GetRTLog")]
        public static extern int GetRTLog(IntPtr h, ref byte buffer, int buffersize);

        public GatewayModuleJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository, 
            RfidRepository rfidRepository,
            CategoriesDevicesRepository categoriesDevicesRepository,
            CategoriesDevicesLogRepository categoriesDevicesLogRepository,
            SystemParameterRepository systemParameterRepository,
            PLCBarrier barrier,
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
                // Connect Scale Hub
                ConnectScaleHubAsync();

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

        private async void ConnectScaleHubAsync()
        {
            Connection = new HubConnection(ServerURI);
            Connection.Closed += Connection_Closed;
            HubProxy = Connection.CreateHubProxy("ScaleHub");
            try
            {
                await Connection.Start();
                _gatewayLogger.LogInfo("Connected scale hub");
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                _gatewayLogger.LogInfo("Connect failed scale hub");
            }
        }

        private void Connection_Closed()
        {
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
            var devices = await _categoriesDevicesRepository.GetDevices("CBV");

            c3400 = devices.FirstOrDefault(x => x.Code == "CBV.C3-400");

            rfidVao1 = devices.FirstOrDefault(x => x.Code == "CBV.C3-400.RFID-IN-1");
            rfidVao2 = devices.FirstOrDefault(x => x.Code == "CBV.C3-400.RFID-IN-2");
            rfidRa1 = devices.FirstOrDefault(x => x.Code == "CBV.C3-400.RFID-OUT-1");
            rfidRa2 = devices.FirstOrDefault(x => x.Code == "CBV.C3-400.RFID-OUT-1");

            m221 = devices.FirstOrDefault(x => x.Code == "CBV.M221");

            barrierVao = devices.FirstOrDefault(x => x.Code == "CBV.M221.BRE-IN");
            barrierRa = devices.FirstOrDefault(x => x.Code == "CBV.M221.BRE-OUT");

            trafficLightIn = devices.FirstOrDefault(x => x.Code == "CBV.DGT-IN");
            trafficLightOut = devices.FirstOrDefault(x => x.Code == "CBV.DGT-OUT");
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
                        _gatewayLogger.LogInfo($"Connected to C3-400 {ipAddress}");

                        DeviceConnected = true;
                    }
                    else
                    {
                        _gatewayLogger.LogInfo($"Connect to C3-400 {ipAddress} failed");

                        ret = PullLastError();
                        DeviceConnected = false;
                    }
                }
                return DeviceConnected;
            }
            catch (Exception ex)
            {
                _gatewayLogger.LogInfo($@"ConnectGateway {ipAddress} error: {ex.Message}");
                return false;
            }
        }

        public async void ReadDataFromC3400()
        {
            _gatewayLogger.LogInfo("Reading RFID from C3-400 ...");

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
                            try {
                                str = Encoding.Default.GetString(buffer);
                                tmp = str.Split(',');

                                // Bắt đầu xử lý khi nhận diện được RFID
                                if (tmp[2] != "0" && tmp[2] != "")
                                {
                                    var cardNoCurrent = tmp[2]?.ToString();
                                    var doorCurrent = tmp[3]?.ToString();
                                    var timeCurrent = tmp[0]?.ToString();

                                    _gatewayLogger.LogInfo("----------------------------");
                                    _gatewayLogger.LogInfo($"Tag: {cardNoCurrent}, door: {doorCurrent}, time: {timeCurrent}");
                                    _gatewayLogger.LogInfo("-----");

                                    // 1.Xác định xe cân vào / ra
                                    var isLuongVao = doorCurrent == rfidVao1.PortNumberDeviceIn.ToString()
                                                    || doorCurrent == rfidVao2.PortNumberDeviceIn.ToString();

                                    var isLuongRa = doorCurrent == rfidRa1.PortNumberDeviceIn.ToString()
                                                    || doorCurrent == rfidRa2.PortNumberDeviceIn.ToString();

                                    var inout = "";
                                    if (isLuongVao)
                                    {
                                        inout = "IN";
                                        _gatewayLogger.LogInfo($"1. Xe vao cong");
                                    }
                                    else
                                    {
                                        inout = "OUT";
                                        _gatewayLogger.LogInfo($"1. Xe ra cong");
                                    }

                                    // 2. Loại bỏ các tag đã check trước đó
                                    if (tmpInvalidCardNoLst.Count > 10) tmpInvalidCardNoLst.RemoveRange(0, 3);
                                    if (tmpInvalidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-3)))
                                    {
                                        _gatewayLogger.LogInfo($@"2. Tag da duoc check truoc do => Ket thuc.");
                                        continue;
                                    }

                                    if (isLuongVao)
                                    {
                                        if (tmpCardNoLst_In.Count > 5) tmpCardNoLst_In.RemoveRange(0, 3);
                                        if (tmpCardNoLst_In.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-5)))
                                        {
                                            _gatewayLogger.LogInfo($@"2. Tag da duoc check truoc do => Ket thuc.");
                                            continue;
                                        }
                                    }
                                    else if (isLuongRa)
                                    {
                                        if (tmpCardNoLst_Out.Count > 5) tmpCardNoLst_Out.RemoveRange(0, 3);
                                        if (tmpCardNoLst_Out.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-5)))
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

                                        SendNotificationCBV(0, inout, cardNoCurrent, "Không thuộc hệ thống");

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

                                        SendNotificationCBV(0, inout, cardNoCurrent, "Không có đơn hàng");

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                        tmpInvalidCardNoLst.Add(newCardNoLog);

                                        continue;
                                    }

                                    var currentOrder = currentOrders.FirstOrDefault();
                                    var deliveryCodes = String.Join(";", currentOrders.Select(x => x.DeliveryCode).ToArray());

                                    _gatewayLogger.LogInfo($"4. Tag co cac don hang hop le DeliveryCode = {deliveryCodes}");

                                    SendNotificationCBV(1, inout, cardNoCurrent, "Phương tiện hợp lệ");

                                    // 5. Xác thực vào / ra cổng
                                    // 6. Bật đèn xanh giao thông, 
                                    // 7. Mở barrier
                                    // 8. Ghi log thiết bị
                                    // 9. Bắn tín hiệu thông báo

                                    var isUpdatedOrder = false;
                                    bool isSuccessOpenBarrier = false;

                                    if (isLuongVao)
                                    {
                                        isUpdatedOrder = await _storeOrderOperatingRepository.UpdateOrderConfirm2(cardNoCurrent);
                                        if (isUpdatedOrder)
                                        {
                                            _gatewayLogger.LogInfo($"5. Đã xác thực trạng thái vào cổng.");

                                            var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                            tmpCardNoLst_In.Add(newCardNoLog);

                                            _gatewayLogger.LogInfo($"6. Mở barrier");
                                            isSuccessOpenBarrier = OpenBarrier("IN");

                                            _gatewayLogger.LogInfo($"7. Bật đèn xanh");
                                            TurnOnGreenTrafficLight("IN");

                                            Thread.Sleep(10000);

                                            _gatewayLogger.LogInfo($"8. Bật đèn đỏ");
                                            TurnOnRedTrafficLight("IN");
                                        }
                                        else
                                        {
                                            _gatewayLogger.LogInfo($"5. Confirm 2 failed.");
                                        }
                                    }
                                    else if (isLuongRa)
                                    {
                                        isUpdatedOrder = await _storeOrderOperatingRepository.UpdateOrderConfirm8(cardNoCurrent);
                                        if (isUpdatedOrder)
                                        {
                                            _gatewayLogger.LogInfo($"5. Đã xác thực trạng thái ra cổng.");

                                            var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                            tmpCardNoLst_Out.Add(newCardNoLog);

                                            _gatewayLogger.LogInfo($"7. Mở barrier");
                                            isSuccessOpenBarrier = OpenBarrier("OUT");

                                            _gatewayLogger.LogInfo($"6. Bật đèn xanh");
                                            TurnOnGreenTrafficLight("OUT");

                                            Thread.Sleep(10000);

                                            _gatewayLogger.LogInfo($"8. Bật đèn đỏ");
                                            TurnOnRedTrafficLight("OUT");
                                        }
                                        else
                                        {
                                            _gatewayLogger.LogInfo($"5. Confirm 8 failed.");
                                        }
                                    }

                                    if (isUpdatedOrder)
                                    {
                                        if (isSuccessOpenBarrier)
                                        {
                                            _gatewayLogger.LogInfo($"9. Ghi log thiet bi mo barrier");

                                            string luongText = isLuongVao ? "vào" : "ra";
                                            string deviceCode = isLuongVao ? "CBV.M221.BRE-IN" : "CBV.M221.BRE-OUT";
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
                                            _gatewayLogger.LogInfo($"8. Mo barrier KHONG thanh cong");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _gatewayLogger.LogError($@"Co loi xay ra khi xu ly RFID {ex.StackTrace} {ex.Message} ");
                                continue;
                            }
                        }
                        else
                        {
                            _gatewayLogger.LogWarn("No data. Reconnect ...");
                            DeviceConnected = false;
                            h21 = IntPtr.Zero;

                            AuthenticateGatewayModule();
                        }
                    }
                }
            }
            else
            {
                _gatewayLogger.LogWarn("No data. Reconnect ...");
                DeviceConnected = false;
                h21 = IntPtr.Zero;

                AuthenticateGatewayModule();
            }
        }

        public bool OpenBarrier(string luong)
        {
            //return true;
            try { 
                int portNumberDeviceIn = luong == "IN" ? (int)barrierVao.PortNumberDeviceIn : (int)barrierRa.PortNumberDeviceIn;
                int portNumberDeviceOut = luong == "IN" ? (int)barrierVao.PortNumberDeviceOut : (int)barrierRa.PortNumberDeviceOut;

                _barrier.ConnectPLC(m221.IpAddress);

                _barrier.ShuttleOutputPort(byte.Parse(portNumberDeviceIn.ToString()));

                Thread.Sleep(1000);

                _barrier.ShuttleOutputPort(byte.Parse(portNumberDeviceIn.ToString()));

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

            //return _barrier.TurnOn(m221.IpAddress, (int)m221.PortNumber, portNumberDeviceIn, portNumberDeviceOut);
        }

        public string GetTrafficLightIpAddress(string code)
        {
            var ipAddress = "";

            if (code == "IN")
            {
                ipAddress = trafficLightIn?.IpAddress;
            }
            else if (code == "OUT")
            {
                ipAddress = trafficLightOut?.IpAddress;
            }

            return ipAddress;
        }

        public bool TurnOnGreenTrafficLight(string code)
        {
            var ipAddress = GetTrafficLightIpAddress(code);

            if (String.IsNullOrEmpty(ipAddress))
            {
                return false;
            }

            _trafficLight.Connect(ipAddress);

            return _trafficLight.TurnOnGreenOffRed();
        }

        public bool TurnOnRedTrafficLight(string code)
        {
            var ipAddress = GetTrafficLightIpAddress(code);

            if (String.IsNullOrEmpty(ipAddress))
            {
                return false;
            }

            _trafficLight.Connect(ipAddress);

            return _trafficLight.TurnOffGreenOnRed();
        }

        private void SendNotificationCBV(int status, string inout, string cardNo, string message)
        {
            try
            {
                HubProxy.Invoke("SendNotificationCBV", status, inout, cardNo, message).Wait();

                _gatewayLogger.LogInfo($"SendNotificationCBV: status={status}, inout={inout}, cardNo={cardNo}, message={message}");
            }
            catch (Exception ex)
            {
                _gatewayLogger.LogInfo($"SendNotificationCBV error: {ex.Message}");
            }
        }
    }
}
