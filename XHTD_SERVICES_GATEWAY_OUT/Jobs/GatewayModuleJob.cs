using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_GATEWAY_OUT.Models.Response;
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
using XHTD_SERVICES_GATEWAY_OUT.Business;
using XHTD_SERVICES_GATEWAY_OUT.Hubs;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using XHTD_SERVICES_GATEWAY_OUT.Devices;

namespace XHTD_SERVICES_GATEWAY_OUT.Jobs
{
    public class GatewayModuleJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly RfidRepository _rfidRepository;

        protected readonly CategoriesDevicesRepository _categoriesDevicesRepository;

        protected readonly CategoriesDevicesLogRepository _categoriesDevicesLogRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly PLCBarrier _barrier;

        protected readonly Notification _notification;

        protected readonly GatewayLogger _logger;

        private IntPtr h21 = IntPtr.Zero;

        private static bool DeviceConnected = false;

        private List<CardNoLog> tmpCardNoLst_In = new List<CardNoLog>();

        private List<CardNoLog> tmpCardNoLst_Out = new List<CardNoLog>();

        private List<CardNoLog> tmpInvalidCardNoLst = new List<CardNoLog>();

        private tblCategoriesDevice c3400, rfidRa1, rfidRa2, rfidVao1, rfidVao2, m221, barrierVao, barrierRa;

        protected const string CBV_ACTIVE = "CBV_ACTIVE";

        private static bool isActiveService = true;

        protected const string SERVICE_BARRIER_ACTIVE_CODE = "GATEWAY_OUT_BARRIER_ACTIVE";

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
        private string PegasusAdr = "192.168.13.170";

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
            _notification = notification;
            _logger = gatewayLogger;
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
                        _logger.LogInfo("Service cong bao ve dang TAT.");
                        return;
                    }

                    _logger.LogInfo($"--------------- START JOB - IP: {PegasusAdr} ---------------");

                    // Get devices info
                    await LoadDevicesInfo();

                    AuthenticateUhfFromPegasus();
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

            var activeParameter = parameters.FirstOrDefault(x => x.Code == CBV_ACTIVE);
            var barrierActiveParameter = parameters.FirstOrDefault(x => x.Code == SERVICE_BARRIER_ACTIVE_CODE);

            if (activeParameter == null || activeParameter.Value == "0")
            {
                isActiveService = false;
            }
            else
            {
                isActiveService = true;
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
            var devices = await _categoriesDevicesRepository.GetDevices("CBV");

            c3400 = devices.FirstOrDefault(x => x.Code == "CBV.C3-400");

            rfidVao1 = devices.FirstOrDefault(x => x.Code == "CBV.C3-400.RFID-IN-1");
            rfidVao2 = devices.FirstOrDefault(x => x.Code == "CBV.C3-400.RFID-IN-2");
            rfidRa1 = devices.FirstOrDefault(x => x.Code == "CBV.C3-400.RFID-OUT-1");
            rfidRa2 = devices.FirstOrDefault(x => x.Code == "CBV.C3-400.RFID-OUT-1");

            m221 = devices.FirstOrDefault(x => x.Code == "CBV.M221");

            barrierVao = devices.FirstOrDefault(x => x.Code == "CBV.M221.BRE-IN");
            barrierRa = devices.FirstOrDefault(x => x.Code == "CBV.M221.BRE-OUT");
        }

        public void AuthenticateUhfFromPegasus()
        {
            // 1. Connect Device
            int port = PortHandle;
            var openResult = 1;
            while (openResult != 0)
            {
                try
                {
                    #region Check ping anten
                    Ping pingSender = new Ping();
                    PingReply reply = pingSender.Send(PegasusAdr);

                    if (reply.Status != IPStatus.Success)
                    {
                        _logger.LogInfo("Ping fail");

                        Thread.Sleep(3000);

                        continue;
                    }
                    #endregion

                    openResult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr, ref ComAddr, ref port);

                    if (openResult != 0)
                    {
                        _logger.LogInfo($"Open netPort KHONG thanh cong: PegasusAdr={PegasusAdr} -- port={port} --  openResult={openResult}");

                        PegasusStaticClassReader.CloseNetPort(PortHandle);

                        Program.CountToSendFailOpenPort++;

                        _logger.LogInfo($"Open netPort that bai lan thu: {Program.CountToSendFailOpenPort}");

                        if (Program.CountToSendFailOpenPort == 3)
                        {
                            _logger.LogInfo($"Thời điểm gửi cảnh báo gần nhất: {Program.SendFailOpenPortLastTime}");

                            if (Program.SendFailOpenPortLastTime == null || Program.SendFailOpenPortLastTime < DateTime.Now.AddMinutes(-3))
                            {
                                Program.SendFailOpenPortLastTime = DateTime.Now;

                                // gửi thông báo ping thất bại
                                var pushMessage = $"Cổng ra: mở kết nối không thành công đến anten {PegasusAdr}. Vui lòng báo kỹ thuật kiểm tra";

                                _logger.LogInfo($"Gửi cảnh báo: {pushMessage}");

                                //SendNotificationByRight(RightCode.CONFIRM, pushMessage);
                            }

                            Program.CountToSendFailOpenPort = 0;
                        }

                        Thread.Sleep(5000);
                    }
                    else
                    {
                        Program.CountToSendFailOpenPort = 0;

                        _logger.LogInfo($"Open netPort thanh cong: PegasusAdr={PegasusAdr} -- port={port} --  openResult={openResult}");
                    }
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

        public async void ReadDataFromPegasus()
        {
            _logger.LogInfo("Reading Pegasus...");

            while (Program.UHFConnected)
            {
                try
                {
                    var data = PegasusReader.Inventory_G2(ref ComAddr, 0, 0, 0, PortHandle);

                    foreach (var item in data)
                    {
                        try
                        {
                            var cardNoCurrent = ByteArrayToString(item);

                            // Xác định xe cân vào / ra
                            var isLuongVao = false;
                            var isLuongRa = true;

                            Program.LastTimeReceivedUHF = DateTime.Now;

                            _logger.LogInfo($"====== CardNo : {cardNoCurrent}");

                            await ReadDataProcess(cardNoCurrent, isLuongVao, isLuongRa);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($@"Co loi xay ra khi xu ly RFID {ex.StackTrace} {ex.Message} ");
                            Program.UHFConnected = false;
                            continue;
                        }
                    }
                }
                catch (Exception err)
                {
                    _logger.LogError($@"ReadDataFromPegasus ERROR: {err.StackTrace} {err.Message}");
                    Program.UHFConnected = false;
                    break;
                }
            }

            AuthenticateUhfFromPegasus();
        }

        private async Task ReadDataProcess(string cardNoCurrent, bool isLuongVao, bool isLuongRa)
        {
            if (isLuongVao)
            {
                if (Program.IsLockingRfidIn)
                {
                    _logger.LogInfo($"== Cong VAO dang xu ly => Ket thuc {cardNoCurrent} == ");
                    new GatewayHub().SendMessage("IS_LOCKING_RFID_IN", "1");
                    return;
                }
                else
                {
                    new GatewayHub().SendMessage("IS_LOCKING_RFID_IN", "0");
                }
            }

            if (isLuongRa)
            {
                if (Program.IsLockingRfidOut)
                {
                    _logger.LogInfo($"== Cong RA dang xu ly => Ket thuc {cardNoCurrent} == ");
                    new GatewayHub().SendMessage("IS_LOCKING_RFID_OUT", "1");
                    return;
                }
                else
                {
                    new GatewayHub().SendMessage("IS_LOCKING_RFID_OUT", "0");
                }
            }

            // 2. Loại bỏ các tag đã check trước đó
            if (tmpInvalidCardNoLst.Count > 10)
            {
                tmpInvalidCardNoLst.RemoveRange(0, 3);
            }

            if (tmpInvalidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddSeconds(-5)))
            {
                _logger.LogInfo($@"2. Tag KHONG HOP LE da duoc check truoc do => Ket thuc.");
                return;
            }

            if (isLuongVao)
            {
                if (tmpCardNoLst_In.Count > 5)
                {
                    tmpCardNoLst_In.RemoveRange(0, 3);
                }

                if (tmpCardNoLst_In.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-3)))
                {
                    _logger.LogInfo($@"2. Tag HOP LE da duoc check truoc do => Ket thuc.");
                    return;
                }
            }
            else if (isLuongRa)
            {
                if (tmpCardNoLst_Out.Count > 5)
                {
                    tmpCardNoLst_Out.RemoveRange(0, 3);
                }

                if (tmpCardNoLst_Out.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-3)))
                {
                    _logger.LogInfo($@"2. Tag HOP LE da duoc check truoc do => Ket thuc.");
                    return;
                }
            }

            _logger.LogInfo("----------------------------");
            _logger.LogInfo($"Tag: {cardNoCurrent}");
            _logger.LogInfo("-----");

            var inout = "";
            if (isLuongVao)
            {
                inout = "IN";
                _logger.LogInfo($"1. Xe VAO cong");
            }
            else
            {
                inout = "OUT";
                _logger.LogInfo($"1. Xe RA cong");
            }

            _logger.LogInfo($"2. Kiem tra tag da check truoc do");

            // 3. Kiểm tra cardNoCurrent có hợp lệ hay không
            string vehicleCodeCurrent = _rfidRepository.GetVehicleCodeByCardNo(cardNoCurrent);

            if (!String.IsNullOrEmpty(vehicleCodeCurrent))
            {
                _logger.LogInfo($"3. Tag hop le: vehicle={vehicleCodeCurrent}");
            }
            else
            {
                _logger.LogInfo($"3. Tag KHONG hop le => Ket thuc.");

                await SendNotificationCBV(0, inout, cardNoCurrent, $"RFID {cardNoCurrent} không thuộc hệ thống");
                SendNotificationAPI(inout, 0, cardNoCurrent, $"RFID {cardNoCurrent} không thuộc hệ thống");

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                tmpInvalidCardNoLst.Add(newCardNoLog);

                return;
            }

            // 4. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
            tblStoreOrderOperating currentOrder = null;
            var isValidCardNo = false;

            if (isLuongVao)
            {
                currentOrder = await _storeOrderOperatingRepository.GetCurrentOrderEntraceGateway(vehicleCodeCurrent);

                isValidCardNo = OrderValidator.IsValidOrderEntraceGateway(currentOrder);
            }
            else if (isLuongRa)
            {
                currentOrder = await _storeOrderOperatingRepository.GetCurrentOrderExitGateway(vehicleCodeCurrent);

                isValidCardNo = OrderValidator.IsValidOrderExitGateway(currentOrder);
            }

            if (currentOrder == null)
            {
                _logger.LogInfo($"4. Tag KHONG co don hang => Ket thuc.");

                await SendNotificationCBV(0, inout, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng", vehicleCodeCurrent);
                SendNotificationAPI(inout, 0, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng", vehicleCodeCurrent);

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                tmpInvalidCardNoLst.Add(newCardNoLog);

                return;
            }
            else if (isValidCardNo == false)
            {
                _logger.LogInfo($"4. Tag KHONG co don hang hop le => Ket thuc.");

                await SendNotificationCBV(1, inout, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng hợp lệ", vehicleCodeCurrent);
                SendNotificationAPI(inout, 1, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng hợp lệ", vehicleCodeCurrent);

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                tmpInvalidCardNoLst.Add(newCardNoLog);

                return;
            }
            else
            {
                await SendNotificationCBV(2, inout, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} có đơn hàng hợp lệ", vehicleCodeCurrent);
                SendNotificationAPI(inout, 2, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} có đơn hàng hợp lệ", vehicleCodeCurrent);

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };

                if (isLuongVao)
                {
                    tmpCardNoLst_In.Add(newCardNoLog);

                    Program.IsLockingRfidIn = true;
                }
                else if (isLuongRa)
                {
                    tmpCardNoLst_Out.Add(newCardNoLog);

                    Program.IsLockingRfidOut = true;
                }
            }

            var currentDeliveryCode = currentOrder.DeliveryCode;
            _logger.LogInfo($"4. Tag co don hang hop le DeliveryCode = {currentDeliveryCode}");

            /*
            #region Xử lý đơn hàng hợp lệ
            var isUpdatedOrder = false;
            bool isSuccessOpenBarrier = true;

            bool isNormalOrder = true;

            var currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            if (isLuongVao)
            {
                if (isNormalOrder)
                {
                    isUpdatedOrder = await _storeOrderOperatingRepository.UpdateOrderConfirm2ByVehicleCode(vehicleCodeCurrent);

                    if (isUpdatedOrder)
                    {
                        _gatewayLogger.LogInfo($"5. Đã xác thực trạng thái vào cổng");
                    }
                }
                else
                {
                    isUpdatedOrder = true;
                    _gatewayLogger.LogInfo($"5. Đơn hàng nội bộ => Không update trạng thái vào cổng.");
                }

                if (isUpdatedOrder)
                {
                    await SendNotificationCBV(3, inout, null, $"Xác thực vào cổng thành công", null);
                    SendNotificationAPI(inout, 3, null, $"Xác thực vào cổng thành công", null);

                    if (Program.IsBarrierActive)
                    {
                        // 6. Mở barrier
                        _gatewayLogger.LogInfo($"6. Mở barrier");
                        isSuccessOpenBarrier = OpenS7Barrier("IN");
                    }
                    else
                    {
                        _gatewayLogger.LogInfo($"6. Cấu hình barrier đang TẮT");
                    }
                }
                else
                {
                    await SendNotificationCBV(4, inout, null, $"Xác thực vào cổng thất bại", null);
                    SendNotificationAPI(inout, 4, null, $"Xác thực vào cổng thất bại", null);

                    _gatewayLogger.LogInfo($"5. Confirm 2 failed.");
                }
            }
            else if (isLuongRa)
            {
                if (isNormalOrder)
                {
                    isUpdatedOrder = await _storeOrderOperatingRepository.UpdateOrderConfirm8ByVehicleCode(vehicleCodeCurrent);

                    if (isUpdatedOrder)
                    {
                        _gatewayLogger.LogInfo($"5.Đã xác thực trạng thái ra cổng");
                    }
                }
                else
                {
                    isUpdatedOrder = true;
                    _gatewayLogger.LogInfo($"5. Đơn hàng nội bộ => Không update trạng thái ra cổng.");
                }

                if (isUpdatedOrder)
                {
                    await SendNotificationCBV(3, inout, null, $"Xác thực ra cổng thành công", null);
                    SendNotificationAPI(inout, 3, null, $"Xác thực ra cổng thành công", null);

                    if (Program.IsBarrierActive)
                    {
                        // 6. Mở barrier
                        _gatewayLogger.LogInfo($"6. Mở barrier");
                        isSuccessOpenBarrier = OpenS7Barrier("OUT");
                    }
                    else
                    {
                        _gatewayLogger.LogInfo($"6. Cấu hình barrier đang TẮT");
                    }
                }
                else
                {
                    await SendNotificationCBV(4, inout, null, $"Xác thực ra cổng thất bại", null);
                    SendNotificationAPI(inout, 4, null, $"Xác thực ra cổng thất bại", null);

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
                        ActionInfo = $"Mở barrier cho xe {currentOrder.Vehicle} {luongText}, theo đơn hàng {currentDeliveryCode}",
                        ActionDate = DateTime.Now,
                    };

                    await _categoriesDevicesLogRepository.CreateAsync(newLog);
                }
                else
                {
                    _gatewayLogger.LogInfo($"9. Mo barrier KHONG thanh cong");
                }
            }
            #endregion
            */

            if (isLuongVao)
            {
                _logger.LogInfo($"10. Giai phong RFID IN");

                Program.IsLockingRfidIn = false;
            }
            else if (isLuongRa)
            {
                _logger.LogInfo($"10. Giai phong RFID OUT");

                Program.IsLockingRfidOut = false;
            }
        }

        public bool OpenS7Barrier(string luong)
        {
            if (luong == "IN")
            {
                return DIBootstrapper.Init().Resolve<S71200Control>().OpenBarrierIn();
            }
            else 
            { 
                return DIBootstrapper.Init().Resolve<S71200Control>().OpenBarrierOut();
            }
        }

        private async Task SendNotificationCBV(int status, string inout, string cardNo, string message, string vehicle = null)
        {
            new GatewayHub().SendNotificationCBV(status, inout, cardNo, message, vehicle);
            //try
            //{
            //    await StartIfNeededAsync();

            //    HubProxy.Invoke("SendNotificationCBV", status, inout, cardNo, message, deliveryCode).Wait();

            //    _gatewayLogger.LogInfo($"SendNotificationCBV: status={status}, inout={inout}, cardNo={cardNo}, message={message}");
            //}
            //catch (Exception ex)
            //{
            //    _gatewayLogger.LogInfo($"SendNotificationCBV error: {ex.Message}");
            //}
        }

        public void SendNotificationAPI(string inout, int status, string cardNo, string message, string vehicle = null)
        {
            try
            {
                _notification.SendGatewayNotification(inout, status, cardNo, message, vehicle);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SendNotificationAPI Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }

        public void SendNotificationByRight(string rightCode, string message)
        {
            try
            {
                _logger.LogInfo($"Gửi push notification đến các user với quyền {rightCode}, nội dung {message}");
                _notification.SendNotificationByRight(rightCode, message);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SendNotificationByRight Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }

        public string ByteArrayToString(byte[] b)
        {
            return BitConverter.ToString(b).Replace("-", "");
        }

        #region Read RFID by C3-400
        public void AuthenticateGatewayModule()
        {
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
                _logger.LogInfo($@"Connect to C3-400 {ipAddress} error: {ex.Message}");
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


                                    // 1.Xác định xe cân vào / ra
                                    var isLuongVao = doorCurrent == rfidVao1.PortNumberDeviceIn.ToString()
                                                    || doorCurrent == rfidVao2.PortNumberDeviceIn.ToString();

                                    var isLuongRa = doorCurrent == rfidRa1.PortNumberDeviceIn.ToString()
                                                    || doorCurrent == rfidRa2.PortNumberDeviceIn.ToString();

                                    await ReadDataProcess(cardNoCurrent, isLuongVao, isLuongRa);
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

                            AuthenticateGatewayModule();
                        }
                    }
                }
            }
            else
            {
                DeviceConnected = false;
                h21 = IntPtr.Zero;

                AuthenticateGatewayModule();
            }
        }
        #endregion

        #region Read RFID by Controller
        public void AuthenticateGatewayModuleFromController()
        {
            // 1. Connect Device
            while (!DeviceConnected)
            {
                ConnectGatewayModuleFromController();
            }

            // 2. Đọc dữ liệu từ thiết bị
            ReadDataFromController();
        }

        public bool ConnectGatewayModuleFromController()
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

        public async void ReadDataFromController()
        {
            _logger.LogInfo("Reading RFID from Controller ...");

            if (DeviceConnected)
            {
                while (DeviceConnected)
                {
                    try
                    {
                        byte[] data = new byte[BUFFER_SIZE];
                        stream.Read(data, 0, BUFFER_SIZE);
                        //var dataStr = "*[Reader][1]1974716100[!]";
                        var dataStr = encoding.GetString(data);

                        _logger.LogInfo($"Nhan tin hieu: {dataStr}");

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

                        var isLuongVao = doorCurrent == c3400.PortNumberDeviceIn;

                        var isLuongRa = doorCurrent == c3400.PortNumberDeviceOut;

                        await ReadDataProcess(cardNoCurrent, isLuongVao, isLuongRa);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($@"Co loi xay ra khi xu ly RFID {ex.StackTrace} {ex.Message} ");
                        continue;
                    }
                }
            }
            else
            {
                DeviceConnected = false;
                AuthenticateGatewayModuleFromController();
            }
        }
        #endregion
    }
}
