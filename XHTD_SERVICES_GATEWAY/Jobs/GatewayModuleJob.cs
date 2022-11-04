using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;
using RestSharp;
using XHTD_SERVICES_GATEWAY.Models.Response;
using XHTD_SERVICES.Data.Models.Response;
using XHTD_SERVICES_GATEWAY.Models.Request;
using Newtonsoft.Json;
using System.Configuration;
using System.Collections.Specialized;
using XHTD_SERVICES_GATEWAY.Models.Values;
using System.Runtime.InteropServices;
using NDTan;
using XHTD_SERVICES.Device.PLCM221;

namespace XHTD_SERVICES_GATEWAY.Jobs
{
    public class GatewayModuleJob : IJob
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly RfidRepository _rfidRepository;

        protected readonly Barrier _barrier;

        private IntPtr h21 = IntPtr.Zero;

        private static bool DeviceConnected = false;

        private Result PLC_Result;

        [DllImport(@"C:\Windows\System32\plcommpro.dll", EntryPoint = "Connect")]
        public static extern IntPtr Connect(string Parameters);

        [DllImport(@"C:\Windows\System32\plcommpro.dll", EntryPoint = "PullLastError")]
        public static extern int PullLastError();

        [DllImport(@"C:\Windows\System32\plcommpro.dll", EntryPoint = "GetRTLog")]
        public static extern int GetRTLog(IntPtr h, ref byte buffer, int buffersize);

        public GatewayModuleJob(StoreOrderOperatingRepository storeOrderOperatingRepository, RfidRepository rfidRepository, Barrier barrier)
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _rfidRepository = rfidRepository;
            _barrier = barrier;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(() =>
            {
                log.Info("start GatewayModule Job");
                Console.WriteLine("start GatewayModule Job");
                AuthenticateGatewayModule();

                TestBarrier();
            });
        }

        public void AuthenticateGatewayModule()
        {
            /*
             * 1. Connect Device
             * 2. Đọc dữ liệu từ thiết bị
             * 3. Lấy ra cardNo từ dữ liệu đọc được => cardNoCurrent
             * 4. Kiểm tra cardNoCurrent có tồn tại trong hệ thống RFID hay ko 
             * (do 1 xe có thể có nhiều tag ngoài hệ thống)
             * 5. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
             * 6. Xác định xe đang vào hay ra cổng qua field Step của đơn hàng (<6 => vào cổng)
             * 7. Cập nhật đơn hàng: Step
             * 8. Cập nhật index (số thứ tự) các đơn hàng
             * 9. Ghi log
             * 10. Bắn tín hiệu thông báo
             * 11. Bật đèn xanh giao thông, mở barrier
             * 12. Hiển thị led
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
            Console.WriteLine(" call f ConnectGatewayModule");
            try
            {
                string str = "protocol=TCP,ipaddress=192.168.1.75,port=4370,timeout=2000,passwd=";
                int ret = 0;
                if (IntPtr.Zero == h21)
                {
                    h21 = Connect(str);
                    if (h21 != IntPtr.Zero)
                    {
                        log.Info("--------------connected--------------");
                        Console.WriteLine("--------------connected--------------");
                        DeviceConnected = true;
                    }
                    else
                    {
                        Console.WriteLine("--------------connected failed--------------");
                        log.Info("--------------connected failed--------------");
                        ret = PullLastError();
                        DeviceConnected = false;
                    }
                }
                return DeviceConnected;
            }
            catch (Exception ex)
            {
                log.Error($@"ConnectGatewayModule : {ex.StackTrace}");
                Console.WriteLine($@"ConnectGatewayModule : {ex.Message}");
                return false;
            }
        }

        public async void ReadDataFromC3400()
        {
            Console.WriteLine(" call f ReadDataFromC3400");
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

                                // 1. Lấy ra cardNo từ dữ liệu đọc được => cardNoCurrent
                                var cardNoCurrent = tmp[2]?.ToString();
                                var doorCurrent = tmp[3]?.ToString();

                                Console.WriteLine($"Phat hien tag {cardNoCurrent} tai door {doorCurrent}");

                                // 2. Kiểm tra cardNoCurrent có tồn tại trong hệ thống RFID hay ko 
                                bool isValid = _rfidRepository.CheckValidCode(cardNoCurrent);

                                if (isValid)
                                {
                                    Console.WriteLine($"Tag {cardNoCurrent} hop le.");
                                }
                                else
                                {
                                    Console.WriteLine($"Tag {cardNoCurrent} khong hop le => Bo qua.");
                                    continue;
                                }

                                // 3. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
                                var orderCurrent = _storeOrderOperatingRepository.GetCurrentOrderByCardNoReceiving(cardNoCurrent);
                                if (orderCurrent == null) {
                                    Console.WriteLine($"Tag {cardNoCurrent} khong co don hang hop le");
                                    continue; 
                                }

                                /* 4. Cập nhật đơn hàng
                                 * Luồng vào - ra
                                 * Xác định theo doorId của RFID: biết tag do anten nào nhận diện thì biết dc là xe ra hay vào
                                 * Hoặc theo Step của đơn hàng
                                 */

                                var isUpdatedOrder = false;

                                // Luồng vào
                                if (orderCurrent.Step < 6)
                                {
                                    isUpdatedOrder = await _storeOrderOperatingRepository.UpdateOrderEntraceGateway(cardNoCurrent);
                                }
                                // Luồng ra
                                else
                                {
                                    isUpdatedOrder = await _storeOrderOperatingRepository.UpdateOrderExitGateway(cardNoCurrent);
                                }

                                if (isUpdatedOrder)
                                {
                                    /*
                                     * Tắt đèn đỏ
                                     * Bật đèn xanh
                                     * Mở barrier
                                     * Ghi log thiết bị
                                     */

                                    // OpenBarrier();
                                }
                            }
                        }
                        else
                        {
                            log.Warn("Lỗi không đọc được dữ liệu, có thể do mất kết nối");
                            DeviceConnected = false;
                            h21 = IntPtr.Zero;
                        }
                    }
                }
            }
        }

        public void OpenBarrier()
        {
            Console.WriteLine("--------------open barrier--------------");
            log.Info("--------------open barrier--------------");
        }

        public void TestBarrier()
        {
            PLC_Result = _barrier.Connect("192.168.1.61", 502);

            if (PLC_Result == Result.SUCCESS)
            {
                Console.WriteLine("Connect to PLC at 192.168.1.61 ok - " + _barrier.GetLastErrorString());

                PLC_Result = _barrier.ShuttleOutputPort((byte.Parse("1")));
                if (PLC_Result == Result.SUCCESS)
                {
                    Console.WriteLine("Tat/bat OK");
                }
            }
            else
            {
                Console.WriteLine("Connect to PLC at 192.168.1.61 not ok - " + _barrier.GetLastErrorString());
            }
        }
    }
}
