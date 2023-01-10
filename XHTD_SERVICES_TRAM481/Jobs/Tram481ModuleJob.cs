using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_TRAM481.Models.Response;
using System.Configuration;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES.Data.Entities;
using Microsoft.AspNetCore.SignalR.Client;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES.Data.Common;

namespace XHTD_SERVICES_TRAM481.Jobs
{
    public class Tram481ModuleJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly RfidRepository _rfidRepository;

        protected readonly CategoriesDevicesRepository _categoriesDevicesRepository;

        protected readonly CategoriesDevicesLogRepository _categoriesDevicesLogRepository;

        protected readonly VehicleRepository _vehicleRepository;

        protected readonly ScaleOperatingRepository _scaleOperatingRepository;

        protected readonly PLCBarrier _barrier;

        protected readonly TCPTrafficLight _trafficLight;

        protected readonly Sensor _sensor;

        protected readonly Tram481Logger _tram481Logger;

        private IntPtr h21 = IntPtr.Zero;

        private static bool DeviceConnected = false;

        private List<CardNoLog> tmpCardNoLst_1 = new List<CardNoLog>();

        private List<CardNoLog> tmpCardNoLst_2 = new List<CardNoLog>();

        private List<CardNoLog> tmpInvalidCardNoLst = new List<CardNoLog>();

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

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "Connect")]
        public static extern IntPtr Connect(string Parameters);

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "PullLastError")]
        public static extern int PullLastError();

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "GetRTLog")]
        public static extern int GetRTLog(IntPtr h, ref byte buffer, int buffersize);

        public Tram481ModuleJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            RfidRepository rfidRepository,
            CategoriesDevicesRepository categoriesDevicesRepository,
            CategoriesDevicesLogRepository categoriesDevicesLogRepository,
            VehicleRepository vehicleRepository,
            ScaleOperatingRepository scaleOperatingRepository,
            PLCBarrier barrier,
            TCPTrafficLight trafficLight,
            Sensor sensor,
            Tram481Logger tram481Logger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _rfidRepository = rfidRepository;
            _categoriesDevicesRepository = categoriesDevicesRepository;
            _categoriesDevicesLogRepository = categoriesDevicesLogRepository;
            _vehicleRepository = vehicleRepository;
            _scaleOperatingRepository = scaleOperatingRepository;
            _barrier = barrier;
            _trafficLight = trafficLight;
            _sensor = sensor;
            _tram481Logger = tram481Logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                _tram481Logger.LogInfo("Start tram481 IN service");
                _tram481Logger.LogInfo("----------------------------");

                // Get devices info
                await LoadDevicesInfo();

                AuthenticateTram481Module();
            });
        }

        public async Task LoadDevicesInfo()
        {
            var devices = await _categoriesDevicesRepository.GetDevices("CLK");

            c3400 = devices.FirstOrDefault(x => x.Code == "CLK.C3-400");

            rfidIn11 = devices.FirstOrDefault(x => x.Code == "CLK.C3-400.RFID-IN-1");
            rfidIn12 = devices.FirstOrDefault(x => x.Code == "CLK.C3-400.RFID-IN-2");
            rfidIn21 = devices.FirstOrDefault(x => x.Code == "CLK.C3-400.RFID-OUT-1");
            rfidIn22 = devices.FirstOrDefault(x => x.Code == "CLK.C3-400.RFID-OUT-2");

            m221 = devices.FirstOrDefault(x => x.Code == "CLK.M221");

            //barrierIn1 = devices.FirstOrDefault(x => x.Code == "951-IN.M221.BRE-1");
            //barrierIn2 = devices.FirstOrDefault(x => x.Code == "951-IN.M221.BRE-2");
            //barrierOut1 = devices.FirstOrDefault(x => x.Code == "951-OUT.M221.BRE-1");
            //barrierOut2 = devices.FirstOrDefault(x => x.Code == "951-OUT.M221.BRE-2");

            trafficLightIn1 = devices.FirstOrDefault(x => x.Code == "CLK.DGT-IN");
            trafficLightIn2 = devices.FirstOrDefault(x => x.Code == "CLK.DGT-OUT");

            //sensorIn1 = devices.FirstOrDefault(x => x.Code == "951-IN.M221.CB-1-1");
            //sensorIn2 = devices.FirstOrDefault(x => x.Code == "951-IN.M221.CB-1-2");
            //sensorOut1 = devices.FirstOrDefault(x => x.Code == "951-IN.M221.CB-1-1");
            //sensorOut2 = devices.FirstOrDefault(x => x.Code == "951-IN.M221.CB-1-2");
        }

        public void AuthenticateTram481Module()
        {
            /*
             * 1. Xác định xe vao can 1 hay can 2 theo gia tri door từ C3-400
             * 2. Loại bỏ các cardNoCurrent đã, đang xử lý (đã check trước đó) hoặc khi đang cân xe khác
             * 3. Kiểm tra cardNoCurrent có hợp lệ hay không
             * 4. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
             * 5. Xác thực cân vào: update step, confirm
             * 6. Đánh dấu đang cân
             * * *  Lưu vào bảng tblScale xe đang cân vào
             * * *  Program.IsScalling = true;
             */

            // 1. Connect Device
            while (!DeviceConnected)
            {
                ConnectTram481Module();
            }

            // 2. Đọc dữ liệu từ thiết bị
            ReadDataFromC3400();
        }

        public bool ConnectTram481Module()
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
                        _tram481Logger.LogInfo($"Connected to C3-400 {ipAddress}");

                        DeviceConnected = true;
                    }
                    else
                    {
                        _tram481Logger.LogInfo($"Connect to C3-400 {ipAddress} failed");

                        ret = PullLastError();
                        DeviceConnected = false;
                    }
                }
                return DeviceConnected;
            }
            catch (Exception ex)
            {
                _tram481Logger.LogInfo($@"ConnectTram481Module {ipAddress} error: {ex.Message}");
                return false;
            }
        }

        public async void ReadDataFromC3400()
        {
            _tram481Logger.LogInfo("Reading RFID from C3-400 ...");

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

                                    // 1. Xác định xe ở cân 1 hay cân 2
                                    var isLuongVao = doorCurrent == rfidIn11.PortNumberDeviceIn.ToString()
                                                    || doorCurrent == rfidIn12.PortNumberDeviceIn.ToString();

                                    var isLuongRa = doorCurrent == rfidIn21.PortNumberDeviceIn.ToString()
                                                    || doorCurrent == rfidIn22.PortNumberDeviceIn.ToString();

                                    // 2. Loại bỏ các tag đã check trước đó
                                    if (tmpInvalidCardNoLst.Count > 10) tmpInvalidCardNoLst.RemoveRange(0, 3);
                                    if (tmpInvalidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-3)))
                                    {
                                        //_tram481Logger.LogInfo($@"2. Tag da duoc check truoc do => Ket thuc.");
                                        continue;
                                    }

                                    if (isLuongVao)
                                    {
                                        if (tmpCardNoLst_1.Count > 5) tmpCardNoLst_1.RemoveRange(0, 4);
                                        if (tmpCardNoLst_1.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-5)))
                                        {
                                            //_tram481Logger.LogInfo($"2. Tag da duoc check truoc do => Ket thuc.");
                                            continue;
                                        }
                                    }
                                    else if (isLuongRa)
                                    {
                                        if (tmpCardNoLst_2.Count > 5) tmpCardNoLst_2.RemoveRange(0, 4);
                                        if (tmpCardNoLst_2.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-5)))
                                        {
                                            //_tram481Logger.LogInfo($"2. Tag da duoc check truoc do => Ket thuc.");
                                            continue;
                                        }
                                    }

                                    _tram481Logger.LogInfo("----------------------------");
                                    _tram481Logger.LogInfo($"Tag: {cardNoCurrent}, door: {doorCurrent}, time: {timeCurrent}");
                                    _tram481Logger.LogInfo("-----");

                                    if (isLuongVao)
                                    {
                                        _tram481Logger.LogInfo($"1. RFID tai can 1");
                                    }
                                    else
                                    {
                                        _tram481Logger.LogInfo($"1. RFID tai can 2");
                                    }

                                    _tram481Logger.LogInfo($"2. Kiem tra tag da check truoc do");

                                    // 3. Kiểm tra cardNoCurrent có hợp lệ hay không
                                    bool isValid = _rfidRepository.CheckValidCode(cardNoCurrent);
                                    if (isValid)
                                    {
                                        _tram481Logger.LogInfo($"3. Tag hop le");
                                    }
                                    else
                                    {
                                        _tram481Logger.LogInfo($"3. Tag KHONG hop le => Ket thuc.");

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                        tmpInvalidCardNoLst.Add(newCardNoLog);

                                        continue;
                                    }

                                    // Nếu đang cân xe khác thì bỏ qua RFID hiện tại
                                    if (isLuongVao)
                                    {
                                        if (Program.IsScalling1)
                                        {
                                            var scaleInfo = _scaleOperatingRepository.GetDetail(ScaleCode.CODE_SCALE_1);
                                            if (scaleInfo != null
                                                && (bool)scaleInfo.IsScaling
                                                && (bool)scaleInfo.ScaleIn
                                                && !String.IsNullOrEmpty(scaleInfo.DeliveryCode))
                                            {
                                                // TODO: cần kiểm tra đơn hàng DeliveryCode, nếu chưa có weightIn thì mới bỏ qua RFID này
                                                _tram481Logger.LogInfo($"== Can 1 dang hoat dong => Ket thuc ==");
                                                continue;
                                            }
                                        }
                                    }
                                    else if (isLuongRa)
                                    {
                                        if (Program.IsScalling2)
                                        {
                                            var scaleInfo = _scaleOperatingRepository.GetDetail(ScaleCode.CODE_SCALE_2);
                                            if (scaleInfo != null
                                                && (bool)scaleInfo.IsScaling
                                                && (bool)scaleInfo.ScaleIn
                                                && !String.IsNullOrEmpty(scaleInfo.DeliveryCode))
                                            {
                                                // TODO: cần kiểm tra đơn hàng DeliveryCode, nếu chưa có weightIn thì mới bỏ qua RFID này
                                                _tram481Logger.LogInfo($"== Can 2 dang hoat dong => Ket thuc ==");
                                                continue;
                                            }
                                        }
                                    }

                                    // 4. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
                                    tblStoreOrderOperating currentOrder = null;
                                    if (isLuongVao)
                                    {
                                        currentOrder = await _storeOrderOperatingRepository.GetCurrentOrderEntraceTram951ByCardNo(cardNoCurrent);
                                    }
                                    else if (isLuongRa)
                                    {
                                        currentOrder = await _storeOrderOperatingRepository.GetCurrentOrderExitTram951ByCardNo(cardNoCurrent);
                                    }
                                    
                                    if (currentOrder == null)
                                    {
                                        _tram481Logger.LogInfo($"4. Tag KHONG co don hang hop le => Ket thuc.");

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                        tmpInvalidCardNoLst.Add(newCardNoLog);

                                        continue;
                                    }

                                    _tram481Logger.LogInfo($"4. Tag co don hang hop le DeliveryCode = {currentOrder.DeliveryCode}");

                                    // 5. Xác thực cân vào
                                    if (isLuongVao)
                                    {
                                        if (await _storeOrderOperatingRepository.UpdateOrderConfirm3(cardNoCurrent))
                                        {
                                            _tram481Logger.LogInfo($@"5. Đã xác thực trạng thái Cân vào");
                                        
                                                // 6. Đánh dấu đang cân
                                                await _scaleOperatingRepository.UpdateWhenConfirmEntrace(ScaleCode.CODE_SCALE_1, currentOrder.DeliveryCode, currentOrder.Vehicle, currentOrder.CardNo);
                                                Program.IsScalling1 = true;

                                                _tram481Logger.LogInfo($@"6. Đánh dấu xe đang cân");

                                                tmpCardNoLst_1.Add(new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now });

                                                // Bat den do
                                                _tram481Logger.LogInfo($@"7. Bat den do");
                                                TurnOnRedTrafficLight(ScaleCode.CODE_SCALE_1);
                                        }
                                        else
                                        {
                                            _tram481Logger.LogInfo($@"5. Confirm 3 failed");
                                        }
                                    }

                                    // 5. Xác thực cân ra
                                    else if (isLuongRa)
                                    {
                                        if (await _storeOrderOperatingRepository.UpdateOrderConfirm7(cardNoCurrent))
                                        {
                                            _tram481Logger.LogInfo($@"5. Đã xác thực trạng thái Cân ra");
                                        
                                                // 6. Đánh dấu đang cân
                                                await _scaleOperatingRepository.UpdateWhenConfirmExit(ScaleCode.CODE_SCALE_1, currentOrder.DeliveryCode, currentOrder.Vehicle, currentOrder.CardNo);
                                                Program.IsScalling1 = true;

                                                _tram481Logger.LogInfo($@"6. Đánh dấu xe đang cân");

                                                tmpCardNoLst_1.Add(new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now });

                                                // Bat den do
                                                _tram481Logger.LogInfo($@"7. Bat den do");
                                                TurnOnRedTrafficLight(ScaleCode.CODE_SCALE_1);
                                        }
                                        else
                                        {
                                            _tram481Logger.LogInfo($@"5. Confirm 7 failed");
                                        }
                                    }

                                }
                            }
                            catch (Exception ex)
                            {
                                _tram481Logger.LogError($@"Co loi xay ra khi xu ly RFID {ex.StackTrace} {ex.Message} ");
                                continue;
                            }
                        }
                        else
                        {
                            _tram481Logger.LogWarn("No data. Reconnect ...");
                            DeviceConnected = false;
                            h21 = IntPtr.Zero;

                            AuthenticateTram481Module();
                        }
                    }
                }
            }
            else
            {
                _tram481Logger.LogWarn("No data. Reconnect ...");
                DeviceConnected = false;
                h21 = IntPtr.Zero;

                AuthenticateTram481Module();
            }
        }

        public string GetTrafficLightIpAddress(string code)
        {
            var ipAddress = "";

            if (code == ScaleCode.CODE_SCALE_1)
            {
                ipAddress = trafficLightIn1?.IpAddress;
            }
            else if (code == ScaleCode.CODE_SCALE_2)
            {
                ipAddress = trafficLightIn2?.IpAddress;
            }

            return ipAddress;
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
    }
}
