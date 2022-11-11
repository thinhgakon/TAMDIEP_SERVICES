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
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly RfidRepository _rfidRepository;

        protected readonly CategoriesDevicesRepository _categoriesDevicesRepository;

        protected readonly CategoriesDevicesLogRepository _categoriesDevicesLogRepository;

        protected readonly Barrier _barrier;

        protected readonly TCPTrafficLight _trafficLight;

        protected readonly Notification _notification;

        private IntPtr h21 = IntPtr.Zero;

        private static bool DeviceConnected = false;

        private List<CardNoLog> tmpCardNoLst_In = new List<CardNoLog>();

        private List<CardNoLog> tmpCardNoLst_Out = new List<CardNoLog>();

        private tblCategoriesDevice c3400, rfidRa1, rfidRa2, rfidVao1, rfidVao2, m221, barrierVao, barrierRa, trafficLightVao, trafficLightRa;

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
            Barrier barrier,
            TCPTrafficLight trafficLight,
            Notification notification
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _rfidRepository = rfidRepository;
            _categoriesDevicesRepository = categoriesDevicesRepository;
            _categoriesDevicesLogRepository = categoriesDevicesLogRepository;
            _barrier = barrier;
            _trafficLight = trafficLight;
            _notification = notification;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                WriteLineLog("start gateway service");
                WriteLineLog("----------------------------");

                // Get devices info
                await LoadDevicesInfo();

                AuthenticateGatewayModule();
            });
        }

        public void AuthenticateGatewayModule()
        {
            /*
             * == Dùng chung cho cả cổng ra và cổng vào == 
             * 1. Connect Device C3-400
             * 2. Đọc dữ liệu từ thiết bị C3-400
             * 3. Lấy ra cardNo từ dữ liệu đọc được => cardNoCurrent. 
             * * 3.1. Xác định xe vào hay ra cổng theo gia tri door từ C3-400
             * * 3.2. Loại bỏ các cardNoCurrent đã, đang xử lý (đã check trước đó)
             * * 3.3. Kiểm tra cardNoCurrent có hợp lệ hay không
             * * 3.4. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
             * * 3.5. Cập nhật đơn hàng: Step
             * * 3.6. Bật đèn xanh giao thông, mở barrier
             * * 3.7. Ghi log thiết bị
             * * 3.8. Bắn tín hiệu thông báo
             * * 3.9. Hiển thị led
             */

            // 1. Connect Device
            while (!DeviceConnected)
            {
                ConnectGatewayModule();
            }

            // 2. Đọc dữ liệu từ thiết bị
            ReadDataFromC3400();

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

        public bool ConnectGatewayModule()
        {
            WriteLog("start connect to C3-400 ... ");

            try
            {
                string str = $"protocol=TCP,ipaddress={c3400?.IpAddress},port={c3400?.PortNumber},timeout=2000,passwd=";
                int ret = 0;
                if (IntPtr.Zero == h21)
                {
                    h21 = Connect(str);
                    if (h21 != IntPtr.Zero)
                    {
                        WriteLineLog("connected");

                        DeviceConnected = true;
                    }
                    else
                    {
                        WriteLineLog("connected failed");

                        ret = PullLastError();
                        DeviceConnected = false;
                    }
                }
                return DeviceConnected;
            }
            catch (Exception ex)
            {
                WriteLineLog($@"ConnectGatewayModule : {ex.Message}");

                return false;
            }
        }

        public async void ReadDataFromC3400()
        {
            WriteLineLog("start read data from C3-400 ...");

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

                                // 3. Lấy ra cardNo từ dữ liệu đọc được => cardNoCurrent
                                var cardNoCurrent = tmp[2]?.ToString();
                                var doorCurrent = tmp[3]?.ToString();

                                WriteLineLog("----------------------------");
                                WriteLineLog($"Tag {cardNoCurrent} door {doorCurrent} ... ");

                                // 3.1.Xác định xe vào hay ra cổng theo gia tri door từ C3-400
                                var isLuongVao = doorCurrent == rfidVao1.PortNumberDeviceIn.ToString()
                                                || doorCurrent == rfidVao2.PortNumberDeviceIn.ToString();

                                var isLuongRa = doorCurrent == rfidRa1.PortNumberDeviceIn.ToString()
                                                || doorCurrent == rfidRa2.PortNumberDeviceIn.ToString();

                                // 3.2.Loại bỏ các cardNoCurrent đã, đang xử lý(đã check trước đó)
                                if (isLuongVao) {
                                    if (tmpCardNoLst_In.Count > 5) tmpCardNoLst_In.RemoveRange(0, 4);

                                    if (tmpCardNoLst_In.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-1)))
                                    {
                                        WriteLineLog($@"1. Tag {cardNoCurrent} da duoc xu ly => Ket thuc.");

                                        continue;
                                    }
                                }
                                else if (isLuongRa)
                                {
                                    if (tmpCardNoLst_Out.Count > 5) tmpCardNoLst_Out.RemoveRange(0, 4);

                                    if (tmpCardNoLst_Out.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-1)))
                                    {
                                        WriteLineLog($@"1. Tag {cardNoCurrent} da duoc xu ly => Ket thuc.");

                                        continue;
                                    }
                                }

                                // 3.3. Kiểm tra cardNoCurrent có hợp lệ hay không
                                WriteLog($"1. Kiem tra tag {cardNoCurrent} hop le: ");

                                bool isValid = _rfidRepository.CheckValidCode(cardNoCurrent);

                                if (isValid)
                                {
                                    WriteLineLog($"CO");
                                }
                                else
                                {
                                    WriteLineLog($"KHONG => Ket thuc.");

                                    _notification.SendNotification("GETWAY", null, null, cardNoCurrent, null, "Không xác định phương tiện");

                                    // Cần add các thẻ invalid vào 1 mảng để tránh phải check lại
                                    // Chỉ check lại các invalid tag sau 1 khoảng thời gian: 3 phút

                                    continue;
                                }

                                // 3.4. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
                                WriteLog($"2. Kiem tra tag {cardNoCurrent} co don hang hop le: ");

                                List <tblStoreOrderOperating> currentOrders = null;
                                if (isLuongVao)
                                {
                                    currentOrders = await _storeOrderOperatingRepository.GetCurrentOrdersEntraceGatewayByCardNoReceiving(cardNoCurrent);
                                }
                                else if (isLuongRa){
                                    currentOrders = await _storeOrderOperatingRepository.GetCurrentOrdersExitGatewayByCardNoReceiving(cardNoCurrent);
                                }

                                if (currentOrders == null || currentOrders.Count == 0) {

                                    WriteLineLog($"KHONG => Ket thuc.");

                                    _notification.SendNotification("GETWAY", null, null, cardNoCurrent, null, "Không xác định đơn hàng hợp lệ");

                                    continue; 
                                }

                                var currentOrder = currentOrders.FirstOrDefault();
                                var deliveryCodes = String.Join(";", currentOrders.Select(x => x.DeliveryCode).ToArray());

                                WriteLineLog($"CO. DeliveryCode = {deliveryCodes}");

                                // 3.5. Cập nhật đơn hàng
                                WriteLog($"3. Tien hanh update don hang: ");

                                var isUpdatedOrder = false;

                                if (isLuongVao)
                                {
                                    WriteLineLog($"vao cong");

                                    isUpdatedOrder = await _storeOrderOperatingRepository.UpdateOrderEntraceGateway(cardNoCurrent);
                                }
                                else if (isLuongRa)
                                {
                                    WriteLineLog($"ra cong");

                                    isUpdatedOrder = await _storeOrderOperatingRepository.UpdateOrderExitGateway(cardNoCurrent);
                                }

                                if (isUpdatedOrder)
                                {
                                    /*
                                     * 3.6. Bật đèn xanh giao thông, mở barrier
                                     * 3.7. Ghi log thiết bị
                                     * 3.8. Bắn tín hiệu thông báo
                                     */

                                    WriteLineLog($"4. Update don hang thanh cong.");

                                    var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };

                                    if (isLuongVao)
                                    {
                                        tmpCardNoLst_In.Add(newCardNoLog);

                                        // Mở barrier
                                        WriteLineLog($"5. Mo barrier vao");

                                        OpenBarrier("VAO", "BV.M221.BRE-1", currentOrder.Vehicle, deliveryCodes);

                                        // Bật đèn xanh giao thông
                                        WriteLineLog($"6. Bat den xanh vao");

                                        OpenTrafficLight("VAO");
                                    }
                                    else if (isLuongRa)
                                    {
                                        tmpCardNoLst_Out.Add(newCardNoLog);

                                        // Mở barrier
                                        WriteLineLog($"5. Mo barrier ra");

                                        OpenBarrier("RA", "BV.M221.BRE-2", currentOrder.Vehicle, deliveryCodes);

                                        // Bật đèn xanh giao thông
                                        WriteLineLog($"6. Bat den xanh ra");

                                        OpenTrafficLight("RA");
                                    }
                                }
                                else
                                {
                                    WriteLineLog($"4. Update don hang KHONG thanh cong => Ket thuc.");
                                }
                            }
                        }
                        else
                        {
                            log.Warn("Lỗi không đọc được dữ liệu, có thể do mất kết nối");
                            DeviceConnected = false;
                            h21 = IntPtr.Zero;

                            AuthenticateGatewayModule();
                        }
                    }
                }
            }
        }

        public async void OpenBarrier(string luong, string code, string vehicle, string deliveryCode)
        {
            string luongText = luong == "VAO" ? "vào" : "ra";
            int portNumberDeviceIn = luong == "VAO" ? (int)barrierVao.PortNumberDeviceIn : (int)barrierRa.PortNumberDeviceIn;
            int portNumberDeviceOut = luong == "VAO" ? (int)barrierVao.PortNumberDeviceOut : (int)barrierRa.PortNumberDeviceOut;

            var newLog = new CategoriesDevicesLogItemResponse
            {
                Code = code,
                ActionType = 1,
                ActionInfo = $"Mở barrier cho xe {vehicle} {luongText}, theo đơn hàng {deliveryCode}",
                ActionDate = DateTime.Now,
            };

            var isOpenSuccess = _barrier.TurnOn(m221.IpAddress, (int)m221.PortNumber, portNumberDeviceIn, portNumberDeviceOut);
            if (isOpenSuccess)
            {
                await _categoriesDevicesLogRepository.CreateAsync(newLog);
            }
        }

        public void OpenTrafficLight(string luong)
        {
            string ipAddress = luong == "VAO" ? trafficLightVao.IpAddress : trafficLightRa.IpAddress;

            _trafficLight.Connect($"{ipAddress}");
            var isSuccess = _trafficLight.TurnOnGreenOffRed();
            if (isSuccess)
            {
                WriteLineLog("6.1. Open TrafficLight: OK");
            }
            else
            {
                WriteLineLog("6.1. Open TrafficLight: Failed");
            }
        }

        public void WriteLineLog(string message)
        {
            Console.WriteLine($"{message}");
            log.Info($"{message}");
        }

        public void WriteLog(string message)
        {
            Console.Write($"{message}");
            log.Info($"{message}");
        }
    }
}
