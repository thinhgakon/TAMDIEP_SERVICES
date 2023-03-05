using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_TRAM951_2.Models.Response;
using System.Configuration;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES.Data.Entities;
using Microsoft.AspNet.SignalR.Client;
using XHTD_SERVICES.Helper;
using Newtonsoft.Json;
using XHTD_SERVICES.Data.Common;
using System.Threading;
using XHTD_SERVICES_TRAM951_2.Hubs;
using Autofac;
using XHTD_SERVICES_TRAM951_2.Business;
using XHTD_SERVICES_TRAM951_2.Devices;
using XHTD_SERVICES.Data.Models.Values;

namespace XHTD_SERVICES_TRAM951_2.Jobs
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

        protected readonly Logger _logger;

        protected readonly string SCALE_CODE = ScaleCode.CODE_SCALE_2;

        protected readonly string SCALE_DGT_IN_CODE = ScaleCode.CODE_SCALE_2_DGT_IN;

        protected readonly string SCALE_DGT_OUT_CODE = ScaleCode.CODE_SCALE_2_DGT_OUT;

        protected readonly string VEHICLE_STATUS = "VEHICLE_2_STATUS";

        private IntPtr h21 = IntPtr.Zero;

        private static bool DeviceConnected = false;

        private List<CardNoLog> tmpCardNoLst = new List<CardNoLog>();

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

        private string ServerURI = URIConfig.SIGNALR_GATEWAY_SERVICE_URL;

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
            Logger logger
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
            _logger = logger;
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

                _logger.LogInfo("Start tram951 fake service");
                _logger.LogInfo("----------------------------");

                // Get devices info
                await LoadDevicesInfo();

                AuthenticateScaleStationModule();
            });
        }

        private async void ConnectScaleHubAsync()
        {
            Connection = new HubConnection(ServerURI);
            Connection.Closed += Connection_Closed;
            HubProxy = Connection.CreateHubProxy("ScaleHub");

            HubProxy.On<string>("SendFakeRFID", (value) =>
            {
                //_logger.LogInfo("----------------------------");
                //_logger.LogInfo($"Received fake RFID data: value={value}");
                RFIDValue = value;
                IsJustReceivedRFIDData = true;
            }
            );

            try
            {
                await Connection.Start();
                _logger.LogInfo("Connected scale hub");
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                _logger.LogInfo("Connect failed scale hub");
            }
        }

        private void Connection_Closed()
        {
        }

        public async Task LoadDevicesInfo()
        {
            var devices = await _categoriesDevicesRepository.GetDevices("951");

            c3400 = devices.FirstOrDefault(x => x.Code == "951-2.C3-400");

            rfidIn11 = devices.FirstOrDefault(x => x.Code == "951-2.C3-400.RFID-IN-1");
            rfidIn12 = devices.FirstOrDefault(x => x.Code == "951-2.C3-400.RFID-IN-2");
            rfidIn21 = devices.FirstOrDefault(x => x.Code == "951-2.C3-400.RFID-OUT-1");
            rfidIn22 = devices.FirstOrDefault(x => x.Code == "951-2.C3-400.RFID-OUT-2");
        }

        public void AuthenticateScaleStationModule()
        {
            while (!DeviceConnected)
            {
                ConnectScaleStationModule();
            }

            ReadDataFromC3400();
        }

        public bool ConnectScaleStationModule()
        {
            _logger.LogInfo("Connected to C3-400");

            DeviceConnected = true;

            return DeviceConnected;
        }

        public async void ReadDataFromC3400()
        {
            _logger.LogInfo("Reading RFID from C3-400 ...");

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

                            // Loại bỏ các tag đã check trước đó
                            if (tmpInvalidCardNoLst.Count > 10)
                            {
                                tmpInvalidCardNoLst.RemoveRange(0, 3);
                            }

                            if (tmpInvalidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-3)))
                            {
                                //_logger.LogInfo($@"1. Tag KHONG HOP LE da duoc check truoc do => Ket thuc.");
                                continue;
                            }

                            if (tmpCardNoLst.Count > 5)
                            {
                                tmpCardNoLst.RemoveRange(0, 3);
                            }

                            if (tmpCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-5)))
                            {
                                //_logger.LogInfo($"1. Tag HOP LE da duoc check truoc do => Ket thuc.");
                                continue;
                            }

                            _logger.LogInfo("----------------------------");
                            _logger.LogInfo($"Tag: {cardNoCurrent}, door: {doorCurrent}, time: {timeCurrent}");
                            _logger.LogInfo("-----");

                            // Nếu đang cân xe khác thì bỏ qua RFID hiện tại
                            if (Program.IsScalling)
                            {
                                var timeToRelease = DateTime.Now.AddMinutes(-5);

                                var scaleInfo = _scaleOperatingRepository.GetDetail(SCALE_CODE);
                                if (scaleInfo != null
                                    && (bool)scaleInfo.IsScaling
                                    && !String.IsNullOrEmpty(scaleInfo.DeliveryCode)
                                    && scaleInfo.TimeIn > timeToRelease
                                    )
                                {
                                    new ScaleHub().SendMessage("Notification", $"== Can {SCALE_CODE} dang hoat dong => Ket thuc {cardNoCurrent} ==");

                                    // TODO: cần kiểm tra đơn hàng DeliveryCode, nếu chưa có weightIn thì mới bỏ qua RFID này
                                    _logger.LogInfo($"== Can {SCALE_CODE} dang hoat dong => Ket thuc ==");
                                    continue;
                                }
                                else
                                {
                                    // Giải phóng cân khi bị giữ quá 5 phút
                                    _logger.LogInfo($"== Giai phong can {SCALE_CODE} khi bi giu qua 5 phut ==");

                                    await DIBootstrapper.Init().Resolve<ScaleBusiness>().ReleaseScale(SCALE_CODE);

                                    Program.IsScalling = false;
                                }
                            }

                            // 1. Kiểm tra cardNoCurrent hợp lệ
                            bool isValid = _rfidRepository.CheckValidCode(cardNoCurrent);
                            if (isValid)
                            {
                                _logger.LogInfo($"1. Tag hop le");
                            }
                            else
                            {
                                _logger.LogInfo($"1. Tag KHONG hop le => Ket thuc");

                                new ScaleHub().SendMessage("Notification", $"RFID {cardNoCurrent} không thuộc hệ thống");
                                new ScaleHub().SendMessage($"{VEHICLE_STATUS}", $"RFID {cardNoCurrent} không thuộc hệ thống");

                                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                tmpInvalidCardNoLst.Add(newCardNoLog);

                                continue;
                            }

                            // 2. Kiểm tra cardNoCurrent có đang chứa đơn hàng hợp lệ không
                            var currentOrder = await _storeOrderOperatingRepository.GetCurrentOrderScaleStation(cardNoCurrent);
                            var isValidCardNo = IsValidOrderScaleStation(currentOrder);

                            if (isValidCardNo == false)
                            {
                                _logger.LogInfo($"2. Tag KHONG co don hang hop le => Ket thuc");

                                new ScaleHub().SendMessage("Notification", $"RFID {cardNoCurrent} không có đơn hàng hợp lệ");
                                new ScaleHub().SendMessage($"{VEHICLE_STATUS}", $"RFID {cardNoCurrent} không có đơn hàng hợp lệ");

                                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                tmpInvalidCardNoLst.Add(newCardNoLog);

                                continue;
                            }
                            else
                            {
                                new ScaleHub().SendMessage("Notification", $"RFID {cardNoCurrent} có đơn hàng hợp lệ");
                                new ScaleHub().SendMessage($"{VEHICLE_STATUS}", $"RFID {cardNoCurrent} có đơn hàng hợp lệ");

                                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                tmpCardNoLst.Add(newCardNoLog);

                                _logger.LogInfo($"2. Tag co don hang hop le DeliveryCode = {currentOrder.DeliveryCode}");
                            }

                            // 3. Xác định xe vào hay ra
                            var isLuongVao = true;

                            if (currentOrder.Step < (int)OrderStep.DA_CAN_VAO)
                            {
                                isLuongVao = true;
                                _logger.LogInfo($"3. Xe can VAO");
                            }
                            else
                            {
                                isLuongVao = false;
                                _logger.LogInfo($"3. Xe can RA");
                            }

                            if (isLuongVao)
                            {
                                // 4. Lưu thông tin xe đang cân
                                var isUpdatedOrder = await _scaleOperatingRepository.UpdateWhenConfirmEntrace(SCALE_CODE, currentOrder.DeliveryCode, currentOrder.Vehicle, currentOrder.CardNo);
                                if (isUpdatedOrder)
                                {
                                    _logger.LogInfo($"4. Lưu thông tin xe đang cân thành công");

                                    // 5. Bat den do
                                    _logger.LogInfo($@"5.1. Bat den do chieu vao");
                                    DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnRedTrafficLight(SCALE_DGT_IN_CODE);
                                    Thread.Sleep(500);
                                    _logger.LogInfo($@"5.2. Bat den do chieu ra");
                                    DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnRedTrafficLight(SCALE_DGT_OUT_CODE);

                                    // 6. Đánh dấu trạng thái đang cân
                                    _logger.LogInfo($@"6. Đánh dấu CAN đang hoạt động: IsScalling = true");
                                    Program.IsScalling = true;
                                }
                                else
                                {
                                    _logger.LogInfo($"4. Lưu thông tin xe đang cân THẤT BẠI");
                                }
                            }
                            else
                            {
                                // 4. Lưu thông tin xe đang cân
                                var isUpdatedOrder = await _scaleOperatingRepository.UpdateWhenConfirmExit(SCALE_CODE, currentOrder.DeliveryCode, currentOrder.Vehicle, currentOrder.CardNo);
                                if (isUpdatedOrder)
                                {
                                    _logger.LogInfo($"4. Lưu thông tin xe đang cân thành công");

                                    // 5. Bat den do
                                    _logger.LogInfo($@"5.1. Bat den do chieu vao");
                                    DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnRedTrafficLight(SCALE_DGT_IN_CODE);
                                    Thread.Sleep(500);
                                    _logger.LogInfo($@"5.2. Bat den do chieu ra");
                                    DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnRedTrafficLight(SCALE_DGT_OUT_CODE);

                                    // 6. Đánh dấu trạng thái đang cân
                                    _logger.LogInfo($@"6. Đánh dấu CAN đang hoạt động: IsScalling = true");
                                    Program.IsScalling = true;
                                }
                                else
                                {
                                    _logger.LogInfo($@"4. Lưu thông tin xe đang cân THẤT BẠI");
                                }
                            }

                        }
                    }
                }
            }
        }

        public bool IsValidOrderScaleStation(tblStoreOrderOperating order)
        {
            if (order == null)
            {
                _logger.LogInfo($"4.0. Don hang tai can: order = null");
                return false;
            }

            _logger.LogInfo($"4.0. Kiem tra don hang tai can: CatId = {order.CatId}, TypeXK = {order.TypeXK}, Step = {order.Step}, DriverUserName = {order.DriverUserName}");

            if (order.CatId == "CLINKER")
            {
                if (order.Step < (int)OrderStep.DA_CAN_RA)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (order.TypeXK == "JUMBO" || order.TypeXK == "SLING")
            {
                if (order.Step < (int)OrderStep.DA_CAN_RA)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (
                    order.Step >= (int)OrderStep.DA_NHAN_DON
                    &&
                    order.Step < (int)OrderStep.DA_CAN_RA
                    &&
                    (order.DriverUserName ?? "") != ""
                  )
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
