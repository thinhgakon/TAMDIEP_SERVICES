﻿using System;
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

        private tblCategoriesDevice c3400, rfidRa1, rfidRa2, rfidVao1, rfidVao2, m221, barrierVao, barrierRa, trafficLightIn, trafficLightOut;

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
                    _confirmLogger.LogInfo("Service cong bao ve dang TAT.");
                    return;
                }

                _confirmLogger.LogInfo("Start gateway service");
                _confirmLogger.LogInfo("----------------------------");

                // Get devices info
                await LoadDevicesInfo();

                AuthenticateConfirmModule();
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
                            try {
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

                                        await SendNotificationHub(0, cardNoCurrent, $"RFID {cardNoCurrent} không thuộc hệ thống");

                                        SendNotificationAPI(0, $"RFID {cardNoCurrent} không thuộc hệ thống", null, null);

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

                                        await SendNotificationHub(1, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng");

                                        SendNotificationAPI(1, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng", vehicleCodeCurrent, cardNoCurrent);

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                        tmpInvalidCardNoLst.Add(newCardNoLog);

                                        continue;
                                    }

                                    // Nếu RFID không có đơn hàng hợp lệ
                                    else if (isValidCardNo == false)
                                    {
                                        _confirmLogger.LogInfo($"4. Tag KHONG co don hang hop le => Ket thuc.");

                                        await SendNotificationHub(1, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng hợp lệ", currentOrder.DeliveryCode);

                                        SendNotificationAPI(1, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng hợp lệ", vehicleCodeCurrent, cardNoCurrent);

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                        tmpInvalidCardNoLst.Add(newCardNoLog);

                                        continue;
                                    }

                                    // Nếu RFID có đơn hàng hợp lệ
                                    else
                                    {
                                        await SendNotificationHub(2, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} có đơn hàng hợp lệ", currentOrder.DeliveryCode);

                                        SendNotificationAPI(2, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} có đơn hàng hợp lệ", vehicleCodeCurrent, cardNoCurrent);

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };

                                        tmpValidCardNoLst.Add(newCardNoLog);

                                        Program.IsLockingRfidIn = true;
                                    }

                                    var currentDeliveryCode = currentOrder.DeliveryCode;
                                    _confirmLogger.LogInfo($"4. Tag co don hang hop le DeliveryCode = {currentDeliveryCode}");

                                    // Xác thực
                                    bool isConfirmSuccess = this._storeOrderOperatingRepository.UpdateBillOrderConfirm10(cardNoCurrent);

                                    // Xác thực thành công
                                    if (isConfirmSuccess)
                                    {
                                        // Xếp số
                                        this._storeOrderOperatingRepository.UpdateIndexOrderForNewConfirm(cardNoCurrent);

                                        _confirmLogger.LogInfo($"7. Bật đèn xanh");
                                        if (TurnOnGreenTrafficLight())
                                        {
                                            _confirmLogger.LogInfo($"7.2. Bật đèn xanh thành công");
                                        }
                                        else
                                        {
                                            _confirmLogger.LogInfo($"7.2. Bật đèn xanh thất bại");
                                        }

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
                                        _confirmLogger.LogError($"Co loi xay ra khi xac thuc rfid: {cardNoCurrent}" );
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

        public string GetTrafficLightIpAddress()
        {
            var ipAddress = "";

            ipAddress = trafficLightIn?.IpAddress;

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

        private async Task SendNotificationHub(int status, string cardNo, string message, string deliveryCode = "")
        {
            new ConfirmHub().SendNotificationConfirmationPoint(status, cardNo, message, deliveryCode);
        }

        public void SendNotificationAPI(int status, string message, string vehicle, string cardNo)
        {
            try
            {
                _notification.SendConfirmNotification(status, message, vehicle, cardNo);
            }
            catch (Exception ex)
            {
                _confirmLogger.LogInfo($"SendNotificationAPI Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }
    }
}
