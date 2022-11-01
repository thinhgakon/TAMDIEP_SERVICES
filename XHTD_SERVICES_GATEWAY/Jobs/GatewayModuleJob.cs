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

namespace XHTD_SERVICES_GATEWAY.Jobs
{
    public class GatewayModuleJob : IJob
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly RfidRepository _rfidRepository;

        private IntPtr h21 = IntPtr.Zero;

        private static bool DeviceConnected = false;

        [DllImport(@"C:\Windows\System32\plcommpro.dll", EntryPoint = "Connect")]
        public static extern IntPtr Connect(string Parameters);

        [DllImport(@"C:\Windows\System32\plcommpro.dll", EntryPoint = "PullLastError")]
        public static extern int PullLastError();

        [DllImport(@"C:\Windows\System32\plcommpro.dll", EntryPoint = "GetRTLog")]
        public static extern int GetRTLog(IntPtr h, ref byte buffer, int buffersize);

        public GatewayModuleJob(StoreOrderOperatingRepository storeOrderOperatingRepository, RfidRepository rfidRepository)
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _rfidRepository = rfidRepository;
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
            });
        }

        public async void AuthenticateGatewayModule()
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
            ConnectGatewayModule();

            // 2. Đọc dữ liệu từ thiết bị
            var str = GetDataFromDevice();

            // 3. Lấy ra cardNo từ dữ liệu đọc được => cardNoCurrent
            string[] tmp = str?.Split(',');

            var cardNoCurrent = tmp?[2]?.ToString();

            // 4. Kiểm tra cardNoCurrent có tồn tại trong hệ thống RFID hay ko 
            bool isValid = _rfidRepository.CheckValidCode(cardNoCurrent);

            // 5. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
            var orderCurrent = _storeOrderOperatingRepository.GetCurrentOrderByCardNoReceiving(cardNoCurrent);

            // 6, 7, 8, 9
            if (orderCurrent.Step < 6)
            {
                await _storeOrderOperatingRepository.UpdateOrderEntraceGateway(cardNoCurrent);
            }
            else
            {
                await _storeOrderOperatingRepository.UpdateOrderExitGateway(cardNoCurrent);
            }

            // 10, 11, 12
            // OpenBarrier();
        }
        public bool ConnectGatewayModule()
        {
            try
            {
                string str = "protocol=TCP,ipaddress=10.15.15.86,port=2681,timeout=2000,passwd=";
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

        public string GetDataFromDevice()
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
                    return str;
                }
                else
                {
                    log.Warn("Lỗi không đọc được dữ liệu, có thể do mất kết nối");
                    DeviceConnected = false;
                    h21 = IntPtr.Zero;
                    AuthenticateGatewayModule();
                }
            }
            return null;
        }

        public void OpenBarrier()
        {
            Console.WriteLine("--------------open barrier--------------");
            log.Info("--------------open barrier--------------");
        }
    }
}
