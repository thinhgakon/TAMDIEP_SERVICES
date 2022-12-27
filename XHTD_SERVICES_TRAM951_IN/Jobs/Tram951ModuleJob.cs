using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_TRAM951_IN.Models.Response;
using System.Configuration;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES.Data.Entities;
using Microsoft.AspNetCore.SignalR.Client;
using XHTD_SERVICES.Helper;

namespace XHTD_SERVICES_TRAM951_IN.Jobs
{
    public class Tram951ModuleJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly RfidRepository _rfidRepository;

        protected readonly CategoriesDevicesRepository _categoriesDevicesRepository;

        protected readonly CategoriesDevicesLogRepository _categoriesDevicesLogRepository;

        protected readonly VehicleRepository _vehicleRepository;

        protected readonly PLCBarrier _barrier;

        protected readonly TCPTrafficLight _trafficLight;

        protected readonly Sensor _sensor;

        protected readonly Tram951Logger _tram951Logger;

        private IntPtr h21 = IntPtr.Zero;

        private static bool DeviceConnected = false;

        private List<CardNoLog> tmpCardNoLst_In = new List<CardNoLog>();

        private List<CardNoLog> tmpCardNoLst_Out = new List<CardNoLog>();

        private tblCategoriesDevice 
            c3400, 
            rfidIn11, 
            rfidIn12,
            rfidIn21, 
            rfidIn22, 
            m221,
            barrierIn1, 
            barrierIn2,
            barrierOut1,
            barrierOut2,
            trafficLightIn1, 
            trafficLightIn2,
            sensorIn1, 
            sensorIn2,
            sensorOut1, 
            sensorOut2;

        private List<int> scaleValues = new List<int>();

        private string ScaleHubURL;

        private bool isJustReceivedScaleData = false;

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
            VehicleRepository vehicleRepository,
            PLCBarrier barrier,
            TCPTrafficLight trafficLight,
            Sensor sensor,
            Tram951Logger tram951Logger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _rfidRepository = rfidRepository;
            _categoriesDevicesRepository = categoriesDevicesRepository;
            _categoriesDevicesLogRepository = categoriesDevicesLogRepository;
            _vehicleRepository = vehicleRepository;
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
                _tram951Logger.LogInfo("Start tram951 IN service");
                _tram951Logger.LogInfo("----------------------------");

                // Get devices info
                await LoadDevicesInfo();

                //AuthenticateTram951Module();

                //HandleHubConnection();

                //ReadDataFromScale();
            });
        }

        public void ReadDataFromScale()
        {
            while (true)
            {
                if (isJustReceivedScaleData)
                {
                    Console.Write("Scale Values:");

                    var scaleText = String.Join(",", scaleValues);
                    Console.WriteLine(scaleText);

                    KiemTraCanOnDinh();

                    isJustReceivedScaleData = false;
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
                isJustReceivedScaleData = true;
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

            c3400 = devices.FirstOrDefault(x => x.Code == "951-IN.C3-400");

            rfidIn11 = devices.FirstOrDefault(x => x.Code == "951-IN.C3-400.RFID-1-1");
            rfidIn12 = devices.FirstOrDefault(x => x.Code == "951-IN.C3-400.RFID-1-2");
            rfidIn21 = devices.FirstOrDefault(x => x.Code == "951-IN.C3-400.RFID-2-1");
            rfidIn22 = devices.FirstOrDefault(x => x.Code == "951-IN.C3-400.RFID-2-2");

            m221 = devices.FirstOrDefault(x => x.Code == "951-IN.M221");

            barrierIn1 = devices.FirstOrDefault(x => x.Code == "951-IN.M221.BRE-1");
            barrierIn2 = devices.FirstOrDefault(x => x.Code == "951-IN.M221.BRE-2");
            barrierOut1 = devices.FirstOrDefault(x => x.Code == "951-OUT.M221.BRE-1");
            barrierOut2 = devices.FirstOrDefault(x => x.Code == "951-OUT.M221.BRE-2");

            trafficLightIn1 = devices.FirstOrDefault(x => x.Code == "951-IN.DGT-1");
            trafficLightIn2 = devices.FirstOrDefault(x => x.Code == "951-IN.DGT-2");

            sensorIn1 = devices.FirstOrDefault(x => x.Code == "951-IN.M221.CB-1-1");
            sensorIn2 = devices.FirstOrDefault(x => x.Code == "951-IN.M221.CB-1-2");
            sensorOut1 = devices.FirstOrDefault(x => x.Code == "951-IN.M221.CB-1-1");
            sensorOut2 = devices.FirstOrDefault(x => x.Code == "951-IN.M221.CB-1-2");
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
                                var timeCurrent = tmp[0]?.ToString();

                                _tram951Logger.LogInfo("----------------------------");
                                _tram951Logger.LogInfo($"Tag: {cardNoCurrent}, door: {doorCurrent}, time: {timeCurrent}");
                                _tram951Logger.LogInfo("-----");

                                // 1. Xác định xe ở cân 1 hay cân 2
                                var isCan1 = doorCurrent == rfidIn11.PortNumberDeviceIn.ToString()
                                                || doorCurrent == rfidIn12.PortNumberDeviceIn.ToString();

                                var isCan2 = doorCurrent == rfidIn21.PortNumberDeviceIn.ToString()
                                                || doorCurrent == rfidIn22.PortNumberDeviceIn.ToString();

                                if (isCan1)
                                {
                                    _tram951Logger.LogInfo($"1. Xe can 1");
                                }
                                else
                                {
                                    _tram951Logger.LogInfo($"1. Xe can 2");
                                }

                                // 2. Loại bỏ các tag đã check trước đó
                                if (isCan1)
                                {
                                    if (tmpCardNoLst_In.Count > 5) tmpCardNoLst_In.RemoveRange(0, 4);

                                    if (tmpCardNoLst_In.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-1)))
                                    {
                                        _tram951Logger.LogInfo($"2. Tag da duoc check truoc do => Ket thuc.");
                                        continue;
                                    }
                                }
                                else if (isCan2)
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

                                // 4. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
                                List<tblStoreOrderOperating> currentOrders = null;
                                if (isCan1)
                                {
                                    currentOrders = await _storeOrderOperatingRepository.GetOrdersEntraceTram951ByCardNoReceiving(cardNoCurrent);
                                }
                                else if (isCan2)
                                {
                                    currentOrders = await _storeOrderOperatingRepository.GetOrdersExitTram951ByCardNoReceiving(cardNoCurrent);
                                }

                                if (currentOrders == null || currentOrders.Count == 0)
                                {
                                    _tram951Logger.LogInfo($"4. Tag KHONG co don hang hop le => Ket thuc.");
                                    continue;
                                }

                                var deliveryCodes = String.Join(";", currentOrders.Select(x => x.DeliveryCode).ToArray());

                                _tram951Logger.LogInfo($"4. Tag co cac don hang hop le DeliveryCode = {deliveryCodes}");

                                // 5. Kiểm tra xe có vi phạm cảm biến
                                var isValidSensor = CheckValidSensor();
                                if (!isValidSensor)
                                {
                                    // Vi phạm cảm biến
                                    continue;
                                }
                                else
                                {
                                    _tram951Logger.LogInfo($"5. Khong vi pham cam bien can");
                                }

                                // 6.Kiểm tra trạng thái cân ổn định
                                _tram951Logger.LogInfo($"6. Kiem tra trang thai can on dinh");
                                KiemTraCanOnDinh();

                                // 7. Lấy giá trị cân (giá trị cuối trong mảng cân ổn định)
                                var currentScaleValue = scaleValues.LastOrDefault();
                                _tram951Logger.LogInfo($"7. Gia tri can: {currentScaleValue}");

                                // 8. Bật đèn đỏ
                                // 9. Đóng barrier
                                bool isSuccessTurnOnRedTrafficLight = false;
                                bool isSuccessCloseBarrier = false;
                                if (isCan1)
                                {
                                    isSuccessTurnOnRedTrafficLight = TurnOnRedTrafficLight("IN");
                                    isSuccessCloseBarrier = CloseBarrier("IN");
                                }
                                else if (isCan2)
                                {
                                    isSuccessTurnOnRedTrafficLight = TurnOnRedTrafficLight("OUT");
                                    isSuccessCloseBarrier = CloseBarrier("OUT");
                                }

                                if (isSuccessTurnOnRedTrafficLight)
                                {
                                    _tram951Logger.LogInfo($"8. Bat den do thanh cong");
                                }
                                else
                                {
                                    _tram951Logger.LogInfo($"8. Bat den do KHONG thanh cong");
                                }

                                if (isSuccessCloseBarrier)
                                {
                                    _tram951Logger.LogInfo($"9. Dong barrier thanh cong");
                                }
                                else
                                {
                                    _tram951Logger.LogInfo($"9. Dong barrier KHONG thanh cong");
                                }

                                /*
                                 * 10. Xử lý sau khi da lay duoc gia tri can on dinh
                                 * * Cân vào: 
                                 * * * Gọi api cân để tiến hành cân vào đối với đơn đặt hàng đang xử lý 
                                 * * * Cập nhật khối lượng cân, bước xử lý của đơn hàng trong CSDL
                                 * * * Cập nhật khối lượng không tải của phương tiện
                                 * * Cân ra: 
                                 * * * Gọi api cân để tiến hàng cân ra đối với đơn đặt hàng đang xử lý 
                                 * * * Cập nhật khối lượng cân, bước xử lý của đơn hàng trong CSDL
                                 */
                                var isUpdatedWeightInWebSale = false;
                                var isUpdatedOrder = false;

                                if (isCan1)
                                {
                                    isUpdatedWeightInWebSale = HttpRequest.UpdateWeightInWebSale();
                                    if (isUpdatedWeightInWebSale)
                                    {
                                        isUpdatedOrder = await _storeOrderOperatingRepository.UpdateOrderEntraceTram951(cardNoCurrent, currentScaleValue);

                                        // Cập nhật lại khối lượng không tải của phương tiện
                                        await _vehicleRepository.UpdateUnladenWeight(cardNoCurrent, currentScaleValue);
                                    }
                                }
                                else if (isCan2)
                                {
                                    isUpdatedWeightInWebSale = HttpRequest.UpdateWeightOutWebSale();
                                    if (isUpdatedWeightInWebSale)
                                    {
                                        isUpdatedOrder = await _storeOrderOperatingRepository.UpdateOrderExitTram951(cardNoCurrent, currentScaleValue);
                                    }
                                }

                                if (isUpdatedOrder)
                                {
                                    _tram951Logger.LogInfo($"10. Xu ly don hang sau khi lay duoc gia tri can thanh cong.");

                                    var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };

                                    if (isCan1)
                                    {
                                        tmpCardNoLst_In.Add(newCardNoLog);
                                    }
                                    else if (isCan2)
                                    {
                                        tmpCardNoLst_Out.Add(newCardNoLog);
                                    }
                                }
                                else
                                {
                                    _tram951Logger.LogInfo($"10. Xu ly don hang sau khi lay duoc gia tri can KHONG thanh cong => Ket thuc.");
                                }

                                // 11. Bật đèn xanh
                                // 12. Mở barrier để xe rời bàn cân
                                bool isSuccessTurnOnGreenTrafficLight = false;
                                bool isSuccessOpenBarrier = false;
                                if (isCan1)
                                {
                                    isSuccessTurnOnGreenTrafficLight = TurnOnGreenTrafficLight("IN");
                                    isSuccessOpenBarrier = OpenBarrier("IN");
                                }
                                else if (isCan2)
                                {
                                    isSuccessTurnOnGreenTrafficLight = TurnOnGreenTrafficLight("OUT");
                                    isSuccessOpenBarrier = OpenBarrier("OUT");
                                }

                                if (isSuccessTurnOnGreenTrafficLight)
                                {
                                    _tram951Logger.LogInfo($"11. Bat den xanh thanh cong");
                                }
                                else
                                {
                                    _tram951Logger.LogInfo($"11. Bat den xanh KHONG thanh cong");
                                }

                                if (isSuccessOpenBarrier)
                                {
                                    _tram951Logger.LogInfo($"12. Mo barrier thanh cong");
                                }
                                else
                                {
                                    _tram951Logger.LogInfo($"12. Mo barrier KHONG thanh cong");
                                }

                                /*
                                 * 13. Xử lý sau cân
                                 * * Cân vào:
                                 * * * Tiến hành xếp số thứ tự vào máng xuất lấy hàng của xe vừa cân vào xong;
                                 * * * Gủi thông tin số thứ tự cho lái xe thông qua tin nhắn notification
                                 */

                                foreach (var item in currentOrders)
                                {
                                    var typeProduct = item.TypeProduct;

                                    var maxIndex = _storeOrderOperatingRepository.GetMaxIndexByTypeProduct(typeProduct);

                                    var newIndex = maxIndex + 1;

                                    await _storeOrderOperatingRepository.UpdateIndex(item.Id, newIndex);
                                }
                            }
                        }
                        else
                        {
                            _tram951Logger.LogWarn("No data. Reconnect ...");
                            DeviceConnected = false;
                            h21 = IntPtr.Zero;

                            AuthenticateTram951Module();
                        }
                    }
                }
            }
        }

        public bool OpenBarrier(string luong)
        {
            int portNumberDeviceIn = luong == "IN" ? (int)barrierIn1.PortNumberDeviceIn : (int)barrierIn2.PortNumberDeviceIn;
            int portNumberDeviceOut = luong == "IN" ? (int)barrierIn1.PortNumberDeviceOut : (int)barrierIn2.PortNumberDeviceOut;

            return _barrier.TurnOn(m221.IpAddress, (int)m221.PortNumber, portNumberDeviceIn, portNumberDeviceOut);
        }

        public bool CloseBarrier(string luong)
        {
            int portNumberDeviceIn = luong == "IN" ? (int)barrierIn1.PortNumberDeviceIn : (int)barrierIn2.PortNumberDeviceIn;
            int portNumberDeviceOut = luong == "IN" ? (int)barrierIn1.PortNumberDeviceOut : (int)barrierIn2.PortNumberDeviceOut;

            return _barrier.TurnOff(m221.IpAddress, (int)m221.PortNumber, portNumberDeviceIn, portNumberDeviceOut);
        }

        public bool TurnOnGreenTrafficLight(string luong)
        {
            if (trafficLightIn1 == null || trafficLightIn2 == null)
            {
                return false;
            }

            string ipAddress = luong == "IN" ? trafficLightIn1.IpAddress : trafficLightIn2.IpAddress;

            _trafficLight.Connect($"{ipAddress}");

            return _trafficLight.TurnOnGreenOffRed();
        }

        public bool TurnOnRedTrafficLight(string luong)
        {
            if (trafficLightIn1 == null || trafficLightIn2 == null)
            {
                return false;
            }

            string ipAddress = luong == "IN" ? trafficLightIn1.IpAddress : trafficLightIn2.IpAddress;

            _trafficLight.Connect($"{ipAddress}");

            return _trafficLight.TurnOffGreenOnRed();
        }

        public bool CheckValidSensor()
        {
            int portNumberDeviceIn1 = sensorIn1 != null ? (int)sensorIn1.PortNumberDeviceIn : -1;
            int portNumberDeviceIn2 = sensorIn2 != null ? (int)sensorIn2?.PortNumberDeviceIn : -1;

            List<int> portNumberDeviceIns = new List<int>
            {
                portNumberDeviceIn1,
                portNumberDeviceIn2
            };

            return _sensor.CheckValid(m221.IpAddress, (int)m221.PortNumber, portNumberDeviceIns);
        }
    }
}
