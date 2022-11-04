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
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Data.Models.Values;

namespace XHTD_SERVICES_GATEWAY.Jobs
{
    public class GatewayModuleJob : IJob
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly RfidRepository _rfidRepository;

        protected readonly Barrier _barrier;

        protected readonly TrafficLight _trafficLight;

        private IntPtr h21 = IntPtr.Zero;

        private static bool DeviceConnected = false;

        private M221Result PLC_Result;

        private List<CardNoLog> tmpCardNoLst = new List<CardNoLog>();

        [DllImport(@"C:\Windows\System32\plcommpro.dll", EntryPoint = "Connect")]
        public static extern IntPtr Connect(string Parameters);

        [DllImport(@"C:\Windows\System32\plcommpro.dll", EntryPoint = "PullLastError")]
        public static extern int PullLastError();

        [DllImport(@"C:\Windows\System32\plcommpro.dll", EntryPoint = "GetRTLog")]
        public static extern int GetRTLog(IntPtr h, ref byte buffer, int buffersize);

        public GatewayModuleJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository, 
            RfidRepository rfidRepository, 
            Barrier barrier,
            TrafficLight trafficLight
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _rfidRepository = rfidRepository;
            _barrier = barrier;
            _trafficLight = trafficLight;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(() =>
            {
                log.Info("start gateway service");
                Console.WriteLine("start gateway service");
                Console.WriteLine("----------------------------");
                AuthenticateGatewayModule();
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
            Console.Write("start connect to C3-400 ... ");
            try
            {
                string str = "protocol=TCP,ipaddress=192.168.1.75,port=4370,timeout=2000,passwd=";
                int ret = 0;
                if (IntPtr.Zero == h21)
                {
                    h21 = Connect(str);
                    if (h21 != IntPtr.Zero)
                    {
                        Console.WriteLine("connected");
                        log.Info("--------------connected--------------");
                        DeviceConnected = true;
                    }
                    else
                    {
                        Console.WriteLine("connected failed");
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
            Console.WriteLine("start read data from C3-400 ...");
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

                                Console.WriteLine("----------------------------");
                                Console.WriteLine($"Tag {cardNoCurrent} door {doorCurrent} ... ");

                                if (tmpCardNoLst.Count > 5) tmpCardNoLst.RemoveRange(0, 4);

                                if (tmpCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-1)))
                                {
                                    Console.WriteLine($@"1. Tag {cardNoCurrent} da duoc xu ly => Ket thuc.");
                                    continue;
                                }

                                // 2. Kiểm tra cardNoCurrent có tồn tại trong hệ thống RFID hay ko 
                                Console.Write($"1. Kiem tra tag {cardNoCurrent} hop le: ");

                                bool isValid = _rfidRepository.CheckValidCode(cardNoCurrent);

                                if (isValid)
                                {
                                    Console.WriteLine($"CO");
                                }
                                else
                                {
                                    Console.WriteLine($"KHONG => Ket thuc.");
                                    continue;
                                }

                                // 3. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
                                Console.Write($"2. Kiem tra tag {cardNoCurrent} co don hang hop le: ");

                                var orderCurrent = _storeOrderOperatingRepository.GetCurrentOrderByCardNoReceiving(cardNoCurrent);
                                if (orderCurrent == null) { 

                                    Console.WriteLine($"KHONG => Ket thuc.");
                                    continue; 
                                }
                                else
                                {
                                    Console.WriteLine($"CO. DeliveryCode = {orderCurrent.DeliveryCode}");
                                }

                                /* 4. Cập nhật đơn hàng
                                 * Luồng vào - ra
                                 * Xác định theo doorId của RFID: biết tag do anten nào nhận diện thì biết dc là xe ra hay vào
                                 * Hoặc theo Step của đơn hàng
                                 */

                                Console.Write($"3. Tien hanh update don hang: ");
                                var isUpdatedOrder = false;

                                // Luồng vào
                                if (orderCurrent.Step < (int)OrderStep.DA_LAY_HANG)
                                {
                                    Console.WriteLine($"vao cong");
                                    isUpdatedOrder = await _storeOrderOperatingRepository.UpdateOrderEntraceGateway(cardNoCurrent);
                                }
                                // Luồng ra
                                else
                                {
                                    Console.WriteLine($"ra cong");
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

                                    Console.WriteLine($"4. Update don hang thanh cong.");

                                    tmpCardNoLst.Add(new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now });

                                    // Mở barrier
                                    Console.WriteLine($"5. Mo barrier");
                                    OpenBarrier();

                                    // Bật đèn xanh giao thông
                                    Console.WriteLine($"6. Bat den xanh");
                                    OpenTrafficLight();
                                }
                                else
                                {
                                    Console.WriteLine($"4. Update don hang KHONG thanh cong => Ket thuc.");
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

        public void OpenBarrier()
        {
            PLC_Result = _barrier.Connect("192.168.1.61", 502);

            if (PLC_Result == M221Result.SUCCESS)
            {
                Console.WriteLine($"5.1. Connected to PLC ... {_barrier.GetLastErrorString()}");

                PLC_Result = _barrier.ShuttleOutputPort((byte.Parse("1")));
                if (PLC_Result == M221Result.SUCCESS)
                {
                    Console.WriteLine("5.2. Tat/bat Barrier: OK");
                }
                else
                {
                    Console.WriteLine("5.2. Tat/bat Barrier: ERROR");
                }
            }
            else
            {
                Console.WriteLine($"5.1. Connect failed to PLC ... {_barrier.GetLastErrorString()}");
            }
        }

        public void OpenTrafficLight()
        {
            PLC_Result = _trafficLight.Connect("192.168.1.61", 502);

            if (PLC_Result == M221Result.SUCCESS)
            {
                Console.WriteLine($"6.1. Connected to PLC ... {_trafficLight.GetLastErrorString()}");

                PLC_Result = _trafficLight.ShuttleOutputPort((byte.Parse("5")));
                if (PLC_Result == M221Result.SUCCESS)
                {
                    Console.WriteLine("6.2. Tat/bat Traffic Light: OK");
                }
                else
                {
                    Console.WriteLine("6.2. Tat/bat Traffic Light: ERROR");
                }
            }
            else
            {
                Console.WriteLine($"6.1. Connect failed to PLC {_trafficLight.GetLastErrorString()}");
            }
        }
    }
}
