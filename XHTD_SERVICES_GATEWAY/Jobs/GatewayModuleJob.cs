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

        private IntPtr h21 = IntPtr.Zero;

        private static bool DeviceConnected = false;

        private M221Result PLC_Result;

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
            TCPTrafficLight trafficLight
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _rfidRepository = rfidRepository;
            _categoriesDevicesRepository = categoriesDevicesRepository;
            _categoriesDevicesLogRepository = categoriesDevicesLogRepository;
            _barrier = barrier;
            _trafficLight = trafficLight;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                Console.WriteLine("start gateway service");
                Console.WriteLine("----------------------------");

                log.Info("start gateway service");
                log.Info("----------------------------");

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
             * * 3.6. Cập nhật index (số thứ tự) các đơn hàng
             * * 3.7. Bật đèn xanh giao thông, mở barrier
             * * 3.8. Ghi log thiết bị
             * * 3.9. Bắn tín hiệu thông báo
             * * 3.10. Hiển thị led
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
            Console.Write("start connect to C3-400 ... ");
            log.Info("start connect to C3-400 ... ");

            try
            {
                string str = $"protocol=TCP,ipaddress={c3400?.IpAddress},port={c3400?.PortNumber},timeout=2000,passwd=";
                int ret = 0;
                if (IntPtr.Zero == h21)
                {
                    h21 = Connect(str);
                    if (h21 != IntPtr.Zero)
                    {
                        Console.WriteLine("connected");
                        log.Info("connected");

                        DeviceConnected = true;
                    }
                    else
                    {
                        Console.WriteLine("connected failed");
                        log.Info("connected failed");

                        ret = PullLastError();
                        DeviceConnected = false;
                    }
                }
                return DeviceConnected;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"ConnectGatewayModule : {ex.Message}");
                log.Error($@"ConnectGatewayModule : {ex.StackTrace}");

                return false;
            }
        }

        public async void ReadDataFromC3400()
        {
            Console.WriteLine("start read data from C3-400 ...");
            log.Info("start read data from C3-400 ...");

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

                                Console.WriteLine("----------------------------");
                                Console.WriteLine($"Tag {cardNoCurrent} door {doorCurrent} ... ");

                                log.Info("----------------------------");
                                log.Info($"Tag {cardNoCurrent} door {doorCurrent} ... ");

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
                                        Console.WriteLine($@"1. Tag {cardNoCurrent} da duoc xu ly => Ket thuc.");
                                        log.Info($@"1. Tag {cardNoCurrent} da duoc xu ly => Ket thuc.");

                                        continue;
                                    }
                                }
                                else if (isLuongRa)
                                {
                                    if (tmpCardNoLst_Out.Count > 5) tmpCardNoLst_Out.RemoveRange(0, 4);

                                    if (tmpCardNoLst_Out.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-1)))
                                    {
                                        Console.WriteLine($@"1. Tag {cardNoCurrent} da duoc xu ly => Ket thuc.");
                                        log.Info($@"1. Tag {cardNoCurrent} da duoc xu ly => Ket thuc.");

                                        continue;
                                    }
                                }

                                // 3.3. Kiểm tra cardNoCurrent có hợp lệ hay không
                                Console.Write($"1. Kiem tra tag {cardNoCurrent} hop le: ");
                                log.Info($"1. Kiem tra tag {cardNoCurrent} hop le: ");

                                bool isValid = _rfidRepository.CheckValidCode(cardNoCurrent);

                                if (isValid)
                                {
                                    Console.WriteLine($"CO");
                                    log.Info($"CO");
                                }
                                else
                                {
                                    Console.WriteLine($"KHONG => Ket thuc.");
                                    log.Info($"KHONG => Ket thuc.");

                                    // Cần add các thẻ invalid vào 1 mảng để tránh phải check lại
                                    // Chỉ check lại các invalid tag sau 1 khoảng thời gian: 3 phút

                                    continue;
                                }

                                // 3.4. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
                                Console.Write($"2. Kiem tra tag {cardNoCurrent} co don hang hop le: ");
                                log.Info($"2. Kiem tra tag {cardNoCurrent} co don hang hop le: ");

                                tblStoreOrderOperating orderCurrent = null;
                                if (isLuongVao)
                                {
                                    orderCurrent = _storeOrderOperatingRepository.GetCurrentOrderEntraceGatewayByCardNoReceiving(cardNoCurrent);
                                }
                                else if (isLuongRa){
                                    orderCurrent = _storeOrderOperatingRepository.GetCurrentOrderExitGatewayByCardNoReceiving(cardNoCurrent);
                                }

                                if (orderCurrent == null) {

                                    Console.WriteLine($"KHONG => Ket thuc.");
                                    log.Info($"KHONG => Ket thuc.");

                                    continue; 
                                }
                                else
                                {
                                    Console.WriteLine($"CO. DeliveryCode = {orderCurrent.DeliveryCode}");
                                    log.Info($"CO. DeliveryCode = {orderCurrent.DeliveryCode}");
                                }

                                // 3.5. Cập nhật đơn hàng
                                Console.Write($"3. Tien hanh update don hang: ");
                                log.Info($"3. Tien hanh update don hang: ");

                                var isUpdatedOrder = false;

                                if (isLuongVao)
                                {
                                    Console.WriteLine($"vao cong");
                                    log.Info($"vao cong");

                                    isUpdatedOrder = await _storeOrderOperatingRepository.UpdateOrderEntraceGateway(cardNoCurrent);
                                }
                                else if (isLuongRa)
                                {
                                    Console.WriteLine($"ra cong");
                                    log.Info($"ra cong");

                                    isUpdatedOrder = await _storeOrderOperatingRepository.UpdateOrderExitGateway(cardNoCurrent);
                                }

                                // 3.6. Cập nhật index (số thứ tự) các đơn hàng

                                if (isUpdatedOrder)
                                {
                                    /*
                                     * 3.7. Bật đèn xanh giao thông, mở barrier
                                     * 3.8. Ghi log thiết bị
                                     * 3.9. Bắn tín hiệu thông báo
                                     */

                                    Console.WriteLine($"4. Update don hang thanh cong.");
                                    log.Info($"4. Update don hang thanh cong.");

                                    var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };

                                    if (isLuongVao)
                                    {
                                        tmpCardNoLst_In.Add(newCardNoLog);

                                        // Mở barrier
                                        Console.WriteLine($"5. Mo barrier vao");
                                        log.Info($"5. Mo barrier vao");

                                        OpenBarrier("VAO", "BV.M221.BRE-1", orderCurrent.Vehicle, orderCurrent.DeliveryCode);

                                        // Bật đèn xanh giao thông
                                        Console.WriteLine($"6. Bat den xanh vao");
                                        log.Info($"6. Bat den xanh vao");

                                        OpenTrafficLight("VAO");
                                    }
                                    else if (isLuongRa)
                                    {
                                        tmpCardNoLst_Out.Add(newCardNoLog);

                                        // Mở barrier
                                        Console.WriteLine($"5. Mo barrier ra");
                                        log.Info($"5. Mo barrier ra");

                                        OpenBarrier("RA", "BV.M221.BRE-2", orderCurrent.Vehicle, orderCurrent.DeliveryCode);

                                        // Bật đèn xanh giao thông
                                        Console.WriteLine($"6. Bat den xanh ra");
                                        log.Info($"6. Bat den xanh ra");

                                        OpenTrafficLight("RA");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"4. Update don hang KHONG thanh cong => Ket thuc.");
                                    log.Info($"4. Update don hang KHONG thanh cong => Ket thuc.");
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
                Console.WriteLine("6.1. Open TrafficLight: OK");
                log.Info("5.2. Open TrafficLight: OK");
            }
            else
            {
                Console.WriteLine("6.1. Open TrafficLight: Failed");
                log.Info("5.2. Open TrafficLight: Failed");
            }
        }
    }
}
