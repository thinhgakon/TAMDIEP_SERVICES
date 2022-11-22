using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_TRAM951.Models.Response;
using System.Configuration;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES.Data.Entities;
using Microsoft.AspNetCore.SignalR.Client;
using XHTD_SERVICES.Helper;

namespace XHTD_SERVICES_TRAM951.Jobs
{
    public class Tram951ModuleJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly RfidRepository _rfidRepository;

        protected readonly CategoriesDevicesRepository _categoriesDevicesRepository;

        protected readonly CategoriesDevicesLogRepository _categoriesDevicesLogRepository;

        protected readonly Barrier _barrier;

        protected readonly TCPTrafficLight _trafficLight;

        protected readonly Sensor _sensor;

        protected readonly Tram951Logger _tram951Logger;

        private IntPtr h21 = IntPtr.Zero;

        private static bool DeviceConnected = false;

        private List<CardNoLog> tmpCardNoLst_In = new List<CardNoLog>();

        private List<CardNoLog> tmpCardNoLst_Out = new List<CardNoLog>();

        private tblCategoriesDevice c3400, rfidRa1, rfidRa2, rfidVao1, rfidVao2, m221, barrierVao, barrierRa, trafficLightVao, trafficLightRa, sensor1, sensor2;

        private List<int> scaleValues = new List<int>();

        private string ScaleHubURL;

        private bool IsJustReceivedScaleData = false;

        private HubConnection Connection { get; set; }

        [DllImport(@"C:\Windows\System32\plcommpro.dll", EntryPoint = "Connect")]
        public static extern IntPtr Connect(string Parameters);

        [DllImport(@"C:\Windows\System32\plcommpro.dll", EntryPoint = "PullLastError")]
        public static extern int PullLastError();

        [DllImport(@"C:\Windows\System32\plcommpro.dll", EntryPoint = "GetRTLog")]
        public static extern int GetRTLog(IntPtr h, ref byte buffer, int buffersize);

        public Tram951ModuleJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository, 
            RfidRepository rfidRepository,
            CategoriesDevicesRepository categoriesDevicesRepository,
            CategoriesDevicesLogRepository categoriesDevicesLogRepository,
            Barrier barrier,
            TCPTrafficLight trafficLight,
            Sensor sensor,
            Tram951Logger tram951Logger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _rfidRepository = rfidRepository;
            _categoriesDevicesRepository = categoriesDevicesRepository;
            _categoriesDevicesLogRepository = categoriesDevicesLogRepository;
            _barrier = barrier;
            _trafficLight = trafficLight;
            _sensor = sensor;
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
                _tram951Logger.LogInfo("start tram951 service");
                _tram951Logger.LogInfo("----------------------------");

                // Get devices info
                await LoadDevicesInfo();

                AuthenticateTram951Module();

                HandleHubConnection();

                ReadDataFromScale();
            });
        }

        public void ReadDataFromScale()
        {
            while (true)
            {
                if (IsJustReceivedScaleData)
                {
                    Console.Write("Scale Values:");

                    var scaleText = String.Join(",", scaleValues);
                    Console.WriteLine(scaleText);

                    KiemTraCanOnDinh();

                    IsJustReceivedScaleData = false;
                }
            }
        }

        public void KiemTraCanOnDinh()
        {
            while (true) {
                var tbc = Calculator.TrungBinhCong(scaleValues);
                var isOnDinh = Calculator.CheckBalanceValues(scaleValues, 1);

                Console.WriteLine("tbc: " + tbc);

                if (isOnDinh)
                {
                    Console.WriteLine("can on dinh");
                    Console.WriteLine("Gia tri can hien tai: " + scaleValues.LastOrDefault().ToString() );
                    break;
                }
                else
                {
                    Console.WriteLine("can chua on dinh ...");
                }
            }
        }

        public async void HandleHubConnection()
        {
            var apiUrl = ConfigurationManager.GetSection("API_DMS/Url") as NameValueCollection;
            ScaleHubURL = apiUrl["ScaleHub"];

            var reconnectSeconds = new List<TimeSpan> { TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(5) };

            var i = 5;
            while (i <= 7200)
            {
                reconnectSeconds.Add(TimeSpan.FromSeconds(i));
                i++;
            }

            Connection = new HubConnectionBuilder()
                .WithUrl($"{ScaleHubURL}")
                //.WithAutomaticReconnect()
                .Build();

            Connection.On<string>("SendOffersToUser", data =>
            {
                IsJustReceivedScaleData = true;
                int result = Int32.Parse(data);

                //todo, adding updates tolist for example
                scaleValues.Add(result);

                if (scaleValues.Count > 5) { 
                    scaleValues.RemoveRange(0, 1); 
                }
            });

            try
            {
                await Connection.StartAsync();
                Console.WriteLine("Connected!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Disconnect!");
            }

            Connection.Reconnecting += connectionId =>
            {
                Console.WriteLine("Reconnecting....");
                return Task.CompletedTask;
            };

            Connection.Reconnected += connectionId =>
            {
                Console.WriteLine("Connected!");
                return Task.CompletedTask;
            };

            Connection.Closed += async (error) =>
            {
                Console.WriteLine("Closed!");

                await Task.Delay(new Random().Next(0, 5) * 1000);
                await Connection.StartAsync();
            };
        }

        public void AuthenticateTram951Module()
        {
            /*
             * 1. Xác định xe cân vào hay cân ra theo gia tri door từ C3-400
             * 2. Loại bỏ các cardNoCurrent đã, đang xử lý (đã check trước đó)
             * 3. Kiểm tra cardNoCurrent có hợp lệ hay không
             * 4. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
             * 5. Kiểm tra xe có vi phạm cảm biến
             * 6. Kiểm tra trạng thái cân ổn định
             * 7. Lấy giá trị cân (giá trị cuối trong mảng cân ổn định)
             * 8. Bật đèn đỏ
             * 9. Đóng barrier
             * 10. Xử lý đơn hàng
             * * Cân vào: 
             * * * Gọi api cân để tiến hàng cân vào đối với đơn đặt hàng đang xử lý, 
             * * * Cập nhật khối lượng cân, bước xử lý của đơn hàng trong CSDL,
             * * * Cập nhật khối lượng không tải của phương tiện;
             * * Cân ra: 
             * * * Gọi api cân để tiến hàng cân ra đối với đơn đặt hàng đang xử lý, 
             * * * Cập nhật khối lượng cân, bước xử lý của đơn hàng trong CSDL;
             * 11. Bật đèn xanh
             * 12. Mở barrier để xe rời bàn cân
             * 13. Xử lý sau cân
             * * Cân vào:
             * * * Tiến hành xếp số thứ tự vào máng xuất lấy hàng của xe vừa cân vào xong;
             */

            // 1. Connect Device
            while (!DeviceConnected)
            {
                ConnectTram951Module();
            }

            // 2. Đọc dữ liệu từ thiết bị
            ReadDataFromC3400();

        }

        public async Task LoadDevicesInfo()
        {
            var devices = await _categoriesDevicesRepository.GetDevices("951");

            c3400 = devices.FirstOrDefault(x => x.Code == "951.C3-400-1");
            rfidRa1 = devices.FirstOrDefault(x => x.Code == "951.C3-400-1.RFID.RA-1");
            rfidRa2 = devices.FirstOrDefault(x => x.Code == "951.C3-400-1.RFID.RA-2");
            rfidVao1 = devices.FirstOrDefault(x => x.Code == "951.C3-400-1.RFID.VAO-1");
            rfidVao2 = devices.FirstOrDefault(x => x.Code == "951.C3-400-1.RFID.VAO-2");

            m221 = devices.FirstOrDefault(x => x.Code == "951.M221");
            barrierVao = devices.FirstOrDefault(x => x.Code == "951.M221.BRE-1-VAO");
            barrierRa = devices.FirstOrDefault(x => x.Code == "951.M221.BRE-1-RA");
            trafficLightVao = devices.FirstOrDefault(x => x.Code == "951.M221.DGT-1");
            trafficLightRa = devices.FirstOrDefault(x => x.Code == "951.M221.DGT-2");
            sensor1 = devices.FirstOrDefault(x => x.Code == "951.M221.SENSOR-1");
            sensor2 = devices.FirstOrDefault(x => x.Code == "951.M221.SENSOR-2");
        }

        public bool ConnectTram951Module()
        {
            _tram951Logger.LogInfo("start connect to C3-400 ... ");

            try
            {
                string str = $"protocol=TCP,ipaddress={c3400?.IpAddress},port={c3400?.PortNumber},timeout=2000,passwd=";
                int ret = 0;
                if (IntPtr.Zero == h21)
                {
                    h21 = Connect(str);
                    if (h21 != IntPtr.Zero)
                    {
                        _tram951Logger.LogInfo("connected");

                        DeviceConnected = true;
                    }
                    else
                    {
                        _tram951Logger.LogInfo("connected failed");

                        ret = PullLastError();
                        DeviceConnected = false;
                    }
                }
                return DeviceConnected;
            }
            catch (Exception ex)
            {
                _tram951Logger.LogInfo($@"ConnectTram951Module : {ex.Message}");

                return false;
            }
        }

        public async void ReadDataFromC3400()
        {
            _tram951Logger.LogInfo("start read data from C3-400 ...");

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

                                _tram951Logger.LogInfo("----------------------------");
                                _tram951Logger.LogInfo($"Tag: {cardNoCurrent}, door: {doorCurrent}");
                                _tram951Logger.LogInfo("-----");

                                // 1.Xác định xe cân vào / ra
                                var isLuongVao = doorCurrent == rfidVao1.PortNumberDeviceIn.ToString()
                                                || doorCurrent == rfidVao2.PortNumberDeviceIn.ToString();

                                var isLuongRa = doorCurrent == rfidRa1.PortNumberDeviceIn.ToString()
                                                || doorCurrent == rfidRa2.PortNumberDeviceIn.ToString();

                                if (isLuongVao)
                                {
                                    _tram951Logger.LogInfo($"1. Xe can vao");
                                }
                                else
                                {
                                    _tram951Logger.LogInfo($"1. Xe can ra");
                                }

                                // 2. Loại bỏ các tag đã check trước đó
                                if (isLuongVao)
                                {
                                    if (tmpCardNoLst_In.Count > 5) tmpCardNoLst_In.RemoveRange(0, 4);

                                    if (tmpCardNoLst_In.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-1)))
                                    {
                                        _tram951Logger.LogInfo($"2. Tag da duoc check truoc do => Ket thuc.");
                                        continue;
                                    }
                                }
                                else if (isLuongRa)
                                {
                                    if (tmpCardNoLst_Out.Count > 5) tmpCardNoLst_Out.RemoveRange(0, 4);

                                    if (tmpCardNoLst_Out.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-1)))
                                    {
                                        _tram951Logger.LogInfo($"2. Tag da duoc check truoc do => Ket thuc.");
                                        continue;
                                    }
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

                                    // Cần add các thẻ invalid vào 1 mảng để tránh phải check lại
                                    // Chỉ check lại các invalid tag sau 1 khoảng thời gian: 3 phút

                                    continue;
                                }

                                // 3.4. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
                                _tram951Logger.LogInfo($"2. Kiem tra tag {cardNoCurrent} co don hang hop le: ");

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

                                    _tram951Logger.LogInfo($"KHONG => Ket thuc.");

                                    continue;
                                }

                                var currentOrder = currentOrders.FirstOrDefault();
                                var deliveryCodes = String.Join(";", currentOrders.Select(x => x.DeliveryCode).ToArray());

                                _tram951Logger.LogInfo($"CO. DeliveryCode = {deliveryCodes}");

                                // 3.5. Kiểm tra xe có vi phạm cảm biến
                                var isValidSensor = CheckValidSensor();
                                if (!isValidSensor)
                                {
                                    // Vi phạm cảm biến
                                    continue;
                                }

                                // 3.6.Kiểm tra trạng thái cân ổn định
                                KiemTraCanOnDinh();

                                // 3.7. Lấy giá trị cân (giá trị cuối trong mảng cân ổn định)
                                var currentScaleValue = scaleValues.LastOrDefault();

                                // 3.8. Bật đèn đỏ
                                // 3.9. Đóng barrier
                                if (isLuongVao)
                                {
                                    TurnOnRedTrafficLight("VAO");
                                    CloseBarrier("VAO");
                                }
                                else if (isLuongRa)
                                {
                                    TurnOnRedTrafficLight("RA");
                                    CloseBarrier("RA");
                                }

                                /*
                                 * 3.10. Xử lý đơn hàng
                                 * * Cân vào: 
                                 * * * Gọi api cân để tiến hành cân vào đối với đơn đặt hàng đang xử lý, 
                                 * * * cập nhật khối lượng cân, bước xử lý của đơn hàng trong CSDL,
                                 * * * vào khối lượng không tải của phương tiện;
                                 * * Cân ra: 
                                 * * * Gọi api cân để tiến hàng cân ra đối với đơn đặt hàng đang xử lý, 
                                 * * * cập nhật khối lượng cân, bước xử lý của đơn hàng trong CSDL;
                                 */

                                // 3.11. Bật đèn xanh
                                // 3.12. Mở barrier để xe rời bàn cân
                                if (isLuongVao)
                                {
                                    TurnOnGreenTrafficLight("VAO");
                                    OpenBarrier("VAO");
                                }
                                else if (isLuongRa)
                                {
                                    TurnOnGreenTrafficLight("RA");
                                    OpenBarrier("RA");
                                }

                                /*
                                 * 3.13. Xử lý sau cân
                                 * * Cân vào:
                                 * * * Tiến hành xếp số thứ tự vào máng xuất lấy hàng của xe vừa cân vào xong;
                                 * * Cân ra:
                                 * * * Đánh dấu trạng thái đơn hàng (step = 7) và gửi thông tin ra cổng bảo vệ;
                                 */

                                _tram951Logger.LogInfo($"3. Tien hanh update don hang: ");

                                var isUpdatedOrder = false;

                                // Luồng vào
                                if (isLuongVao)
                                {
                                    _tram951Logger.LogInfo($"vao cong");

                                    isUpdatedOrder = await _storeOrderOperatingRepository.UpdateOrderEntraceGateway(cardNoCurrent);
                                }
                                // Luồng ra
                                else if (isLuongRa)
                                {
                                    _tram951Logger.LogInfo($"ra cong");

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

                                    _tram951Logger.LogInfo($"4. Update don hang thanh cong.");

                                    var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };

                                    if (isLuongVao)
                                    {
                                        tmpCardNoLst_In.Add(newCardNoLog);
                                    }
                                    else if (isLuongRa)
                                    {
                                        tmpCardNoLst_Out.Add(newCardNoLog);
                                    }
                                }
                                else
                                {
                                    _tram951Logger.LogInfo($"4. Update don hang KHONG thanh cong => Ket thuc.");
                                }
                            }
                        }
                        else
                        {
                            _tram951Logger.LogWarn("Lỗi không đọc được dữ liệu, có thể do mất kết nối");
                            DeviceConnected = false;
                            h21 = IntPtr.Zero;

                            AuthenticateTram951Module();
                        }
                    }
                }
            }
        }

        public void OpenBarrier(string luong)
        {
            int portNumberDeviceIn = luong == "VAO" ? (int)barrierVao.PortNumberDeviceIn : (int)barrierRa.PortNumberDeviceIn;
            int portNumberDeviceOut = luong == "VAO" ? (int)barrierVao.PortNumberDeviceOut : (int)barrierRa.PortNumberDeviceOut;

            _barrier.TurnOn(m221.IpAddress, (int)m221.PortNumber, portNumberDeviceIn, portNumberDeviceOut);
        }

        public void CloseBarrier(string luong)
        {
            int portNumberDeviceIn = luong == "VAO" ? (int)barrierVao.PortNumberDeviceIn : (int)barrierRa.PortNumberDeviceIn;
            int portNumberDeviceOut = luong == "VAO" ? (int)barrierVao.PortNumberDeviceOut : (int)barrierRa.PortNumberDeviceOut;

            _barrier.TurnOff(m221.IpAddress, (int)m221.PortNumber, portNumberDeviceIn, portNumberDeviceOut);
        }

        public void TurnOnGreenTrafficLight(string luong)
        {
            string ipAddress = luong == "VAO" ? trafficLightVao.IpAddress : trafficLightRa.IpAddress;

            _trafficLight.Connect($"{ipAddress}");

            _trafficLight.TurnOnGreenOffRed();
        }

        public void TurnOnRedTrafficLight(string luong)
        {
            string ipAddress = luong == "VAO" ? trafficLightVao.IpAddress : trafficLightRa.IpAddress;

            _trafficLight.Connect($"{ipAddress}");

            _trafficLight.TurnOffGreenOnRed();
        }

        public bool CheckValidSensor()
        {
            int portNumberDeviceIn1 = (int)sensor1.PortNumberDeviceIn;
            int portNumberDeviceIn2 = (int)sensor2.PortNumberDeviceIn;

            List<int> portNumberDeviceIns = new List<int>
            {
                portNumberDeviceIn1,
                portNumberDeviceIn2
            };

            return _sensor.CheckValid(m221.IpAddress, (int)m221.PortNumber, portNumberDeviceIns);
        }
    }
}
