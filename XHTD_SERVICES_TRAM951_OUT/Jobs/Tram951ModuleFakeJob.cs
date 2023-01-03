﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_TRAM951_OUT.Models.Response;
using System.Configuration;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES.Data.Entities;
using Microsoft.AspNet.SignalR.Client;
using XHTD_SERVICES.Helper;
using Newtonsoft.Json;

namespace XHTD_SERVICES_TRAM951_OUT.Jobs
{
    public class Tram951ModuleFakeJob : IJob
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

        protected readonly Tram951Logger _tram951Logger;

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

        private IHubProxy HubProxy { get; set; }

        const string ServerURI = "http://localhost:8083/signalr";

        private HubConnection Connection { get; set; }

        private string RFIDValue;

        private bool IsJustReceivedRFIDData = false;

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "Connect")]
        public static extern IntPtr Connect(string Parameters);

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "PullLastError")]
        public static extern int PullLastError();

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "GetRTLog")]
        public static extern int GetRTLog(IntPtr h, ref byte buffer, int buffersize);

        public Tram951ModuleFakeJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository, 
            RfidRepository rfidRepository,
            CategoriesDevicesRepository categoriesDevicesRepository,
            CategoriesDevicesLogRepository categoriesDevicesLogRepository,
            VehicleRepository vehicleRepository,
            ScaleOperatingRepository scaleOperatingRepository,
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
            _scaleOperatingRepository = scaleOperatingRepository;
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
                // Connect Scale Hub
                ConnectScaleHubAsync();

                _tram951Logger.LogInfo("Start tram951 OUT fake service");
                _tram951Logger.LogInfo("----------------------------");

                // Get devices info
                await LoadDevicesInfo();

                AuthenticateTram951Module();
            });
        }

        private async void ConnectScaleHubAsync()
        {
            Connection = new HubConnection(ServerURI);
            Connection.Closed += Connection_Closed;
            HubProxy = Connection.CreateHubProxy("ScaleHub");

            HubProxy.On<string>("SendFakeRFID", (value) => {
                _tram951Logger.LogInfo("----------------------------");
                _tram951Logger.LogInfo($"Received fake RFID data: value={value}");
                RFIDValue = value;
                IsJustReceivedRFIDData = true;
            }
            );

            try
            {
                await Connection.Start();
                _tram951Logger.LogInfo("Connected scale hub");
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                _tram951Logger.LogInfo("Connect failed scale hub");
            }
        }

        private void Connection_Closed()
        {
        }

        public async Task LoadDevicesInfo()
        {
            var devices = await _categoriesDevicesRepository.GetDevices("951");

            c3400 = devices.FirstOrDefault(x => x.Code == "951-OUT.C3-400");

            rfidIn11 = devices.FirstOrDefault(x => x.Code == "951-OUT.C3-400.RFID-1-1");
            rfidIn12 = devices.FirstOrDefault(x => x.Code == "951-OUT.C3-400.RFID-1-2");
            rfidIn21 = devices.FirstOrDefault(x => x.Code == "951-OUT.C3-400.RFID-2-1");
            rfidIn22 = devices.FirstOrDefault(x => x.Code == "951-OUT.C3-400.RFID-2-2");

            m221 = devices.FirstOrDefault(x => x.Code == "951-OUT.M221");

            //barrierIn1 = devices.FirstOrDefault(x => x.Code == "951-OUT.M221.BRE-1");
            //barrierIn2 = devices.FirstOrDefault(x => x.Code == "951-OUT.M221.BRE-2");
            //barrierOut1 = devices.FirstOrDefault(x => x.Code == "951-OUT.M221.BRE-1");
            //barrierOut2 = devices.FirstOrDefault(x => x.Code == "951-OUT.M221.BRE-2");

            trafficLightIn1 = devices.FirstOrDefault(x => x.Code == "951-OUT.DGT-1");
            trafficLightIn2 = devices.FirstOrDefault(x => x.Code == "951-OUT.DGT-2");

            //sensorIn1 = devices.FirstOrDefault(x => x.Code == "951-OUT.M221.CB-1-1");
            //sensorIn2 = devices.FirstOrDefault(x => x.Code == "951-OUT.M221.CB-1-2");
            //sensorOut1 = devices.FirstOrDefault(x => x.Code == "951-OUT.M221.CB-1-1");
            //sensorOut2 = devices.FirstOrDefault(x => x.Code == "951-OUT.M221.CB-1-2");
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

        public bool ConnectTram951Module()
        {
            _tram951Logger.LogInfo("Connected to C3-400");

            DeviceConnected = true;
                    
            return DeviceConnected;
        }

        public async void ReadDataFromC3400()
        {
            _tram951Logger.LogInfo("start read data from C3-400 ...");

            if (DeviceConnected)
            {
                while (DeviceConnected)
                {
                    string str = "";
                    string[] tmp = null;

                    if (IsJustReceivedRFIDData)
                    {
                        IsJustReceivedRFIDData = false;

                        str = RFIDValue != null ? RFIDValue : "";
                        tmp = str.Split(',');

                        // Bắt đầu xử lý khi nhận diện được RFID
                        if (tmp != null && tmp.Count() > 3 && tmp[2] != "0" && tmp[2] != "")
                        {

                            var cardNoCurrent = tmp[2]?.ToString();
                            var doorCurrent = tmp[3]?.ToString();
                            var timeCurrent = tmp[0]?.ToString();

                            _tram951Logger.LogInfo("----------------------------");
                            _tram951Logger.LogInfo($"Tag: {cardNoCurrent}, door: {doorCurrent}, time: {timeCurrent}");
                            _tram951Logger.LogInfo("-----");

                            // 1. Xác định xe ở cân 1 hay cân 2
                            var isRfidFromScale1 = doorCurrent == rfidIn11.PortNumberDeviceIn.ToString()
                                            || doorCurrent == rfidIn12.PortNumberDeviceIn.ToString();

                            var isRfidFromScale2 = doorCurrent == rfidIn21.PortNumberDeviceIn.ToString()
                                            || doorCurrent == rfidIn22.PortNumberDeviceIn.ToString();

                            if (isRfidFromScale1)
                            {
                                _tram951Logger.LogInfo($"1. RFID tai can 1");
                            }
                            else
                            {
                                _tram951Logger.LogInfo($"1. RFID tai can 2");
                            }

                            // 2. Loại bỏ các tag đã check trước đó
                            // // Nếu đang cân xe khác thì bỏ qua RFID hiện tại
                            if (isRfidFromScale1)
                            {
                                if (Program.IsScalling1)
                                {
                                    _tram951Logger.LogInfo($"2. Can 1 dang hoat dong => Ket thuc.");
                                    continue;
                                }
                            }
                            else if (isRfidFromScale2)
                            {
                                if (Program.IsScalling2)
                                {
                                    _tram951Logger.LogInfo($"2. Can 2 dang hoat dong => Ket thuc.");
                                    continue;
                                }
                            }

                            if (tmpInvalidCardNoLst.Count > 10) tmpInvalidCardNoLst.RemoveRange(0, 3);

                            if (tmpInvalidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-3)))
                            {
                                _tram951Logger.LogInfo($@"2. Tag da duoc check truoc do => Ket thuc.");
                                continue;
                            }

                            if (isRfidFromScale1)
                            {
                                if (tmpCardNoLst_1.Count > 5) tmpCardNoLst_1.RemoveRange(0, 4);

                                if (tmpCardNoLst_1.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-5)))
                                {
                                    _tram951Logger.LogInfo($"2. Tag da duoc check truoc do => Ket thuc.");
                                    continue;
                                }
                            }
                            else if (isRfidFromScale2)
                            {
                                if (tmpCardNoLst_2.Count > 5) tmpCardNoLst_2.RemoveRange(0, 4);

                                if (tmpCardNoLst_2.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-5)))
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

                                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                tmpInvalidCardNoLst.Add(newCardNoLog);

                                continue;
                            }

                            // 4. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
                            var currentOrder = await _storeOrderOperatingRepository.GetCurrentOrderExitTram951ByCardNo(cardNoCurrent);
                            if (currentOrder == null)
                            {
                                _tram951Logger.LogInfo($"4. Tag KHONG co don hang hop le => Ket thuc.");

                                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                tmpInvalidCardNoLst.Add(newCardNoLog);

                                continue;
                            }

                            _tram951Logger.LogInfo($"4. Tag co don hang hop le DeliveryCode = {currentOrder.DeliveryCode}");

                            // 5. Xác thực cân ra
                            if (await _storeOrderOperatingRepository.UpdateOrderConfirm7(cardNoCurrent))
                            {
                                _tram951Logger.LogInfo($@"5. Đã xác thực trạng thái Cân ra");
                                if (isRfidFromScale1)
                                {
                                    // 6. Đánh dấu đang cân
                                    await _scaleOperatingRepository.UpdateWhenConfirmExit("SCALE-1", currentOrder.DeliveryCode, currentOrder.Vehicle, currentOrder.CardNo);
                                    Program.IsScalling1 = true;

                                    _tram951Logger.LogInfo($@"6. Đánh dấu xe đang cân");

                                    tmpCardNoLst_1.Add(new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now });
                                }
                                else if (isRfidFromScale2)
                                {
                                    // 6. Đánh dấu đang cân
                                    await _scaleOperatingRepository.UpdateWhenConfirmExit("SCALE-2", currentOrder.DeliveryCode, currentOrder.Vehicle, currentOrder.CardNo);
                                    Program.IsScalling2 = true;

                                    _tram951Logger.LogInfo($@"6. Đánh dấu xe đang cân");

                                    tmpCardNoLst_2.Add(new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now });
                                }
                            }
                            else
                            {
                                _tram951Logger.LogInfo($@"5. Confirm 3 failed");
                            }
                        }
                    }
                }
            }
        }
    }
}
