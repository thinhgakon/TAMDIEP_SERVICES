using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_CONFIRM.Models.Response;
using XHTD_SERVICES.Data.Models.Response;
using System.Runtime.InteropServices;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Helper;
using Microsoft.AspNet.SignalR.Client;
using System.Threading;
using XHTD_SERVICES.Data.Common;
using Autofac;
using XHTD_SERVICES_CONFIRM.Business;
using XHTD_SERVICES_CONFIRM.Hubs;
using System.Net.NetworkInformation;
using XHTD_SERVICES_CONFIRM.Devices;
using PK_UHF_Test;
using XHTD_SERVICES.Data.Models.Values;
using System.Data.Entity;

namespace XHTD_SERVICES_CONFIRM.Jobs
{
    public class ConfirmModuleJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly RfidRepository _rfidRepository;

        protected readonly CategoriesDevicesRepository _categoriesDevicesRepository;

        protected readonly CategoriesDevicesLogRepository _categoriesDevicesLogRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly TCPTrafficLight _trafficLight;

        protected readonly Notification _notification;

        protected readonly ConfirmLogger _logger;

        private IntPtr h21 = IntPtr.Zero;

        private static bool DeviceConnected = false;

        private List<CardNoLog> tmpValidCardNoLst = new List<CardNoLog>();

        private List<CardNoLog> tmpInvalidCardNoLst = new List<CardNoLog>();

        private tblCategoriesDevice c3400, trafficLight;

        protected const string CONFIRM_ACTIVE = "CONFIRM_ACTIVE";

        private static bool isActiveService = true;

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "Connect")]
        public static extern IntPtr Connect(string Parameters);

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "PullLastError")]
        public static extern int PullLastError();

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "GetRTLog")]
        public static extern int GetRTLog(IntPtr h, ref byte buffer, int buffersize);

        private readonly string CAMERA_IP = "192.168.13.163";
        private readonly string CAMERA_USER_NAME = "admin";
        private readonly string CAMERA_PASSWORD = "tamdiep@35";
        private readonly string IMG_PATH = "C:\\IMAGE";
        private readonly int CAMERA_NUMBER = 2;

        private byte ComAddr = 0xFF;
        private int PortHandle = 6000;
        private string PegasusAdr = "192.168.13.162";

        public ConfirmModuleJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            RfidRepository rfidRepository,
            CategoriesDevicesRepository categoriesDevicesRepository,
            CategoriesDevicesLogRepository categoriesDevicesLogRepository,
            SystemParameterRepository systemParameterRepository,
            TCPTrafficLight trafficLight,
            Notification notification,
            ConfirmLogger confirmLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _rfidRepository = rfidRepository;
            _categoriesDevicesRepository = categoriesDevicesRepository;
            _categoriesDevicesLogRepository = categoriesDevicesLogRepository;
            _systemParameterRepository = systemParameterRepository;
            _trafficLight = trafficLight;
            _notification = notification;
            _logger = confirmLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                await Task.Run(async () =>
                {
                    // Get System Parameters
                    await LoadSystemParameters();

                    if (!isActiveService)
                    {
                        _logger.LogInfo("Service điểm xác thực đang TẮT.");
                        return;
                    }

                    _logger.LogInfo($"--------------- START JOB - IP: {PegasusAdr} ---------------");

                    // Get devices info
                    await LoadDevicesInfo();

                    AuthenticateUhfFromPegasus();
                });
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"RUN JOB ERROR: {ex.Message} --- {ex.StackTrace} --- {ex.InnerException}");

                // do you want the job to refire?
                throw new JobExecutionException(msg: "", refireImmediately: true, cause: ex);
            }
        }

        public async Task LoadSystemParameters()
        {
            var parameters = await _systemParameterRepository.GetSystemParameters();

            var activeParameter = parameters.FirstOrDefault(x => x.Code == CONFIRM_ACTIVE);

            if (activeParameter == null || activeParameter.Value == "0")
            {
                isActiveService = false;
            }
            else
            {
                isActiveService = true;
            }
        }

        public async Task LoadDevicesInfo()
        {
            var devices = await _categoriesDevicesRepository.GetDevices("CONFIRM");

            c3400 = devices.FirstOrDefault(x => x.Code == "CONFIRM.C3-400");

            trafficLight = devices.FirstOrDefault(x => x.Code == "CONFIRM.DGT");
        }

        public void AuthenticateUhfFromPegasus()
        {
            // 1. Connect Device
            int port = PortHandle;
            var openResult = 1;
            while (openResult != 0)
            {
                try
                {
                    #region Check ping anten
                    Ping pingSender = new Ping();
                    PingReply reply = pingSender.Send(PegasusAdr);

                    if (reply.Status != IPStatus.Success)
                    {
                        _logger.LogInfo("Ping fail");

                        Thread.Sleep(3000);

                        continue;
                    }
                    #endregion

                    openResult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr, ref ComAddr, ref port);

                    if (openResult != 0)
                    {
                        _logger.LogInfo($"Open netPort KHONG thanh cong: PegasusAdr={PegasusAdr} -- port={port} --  openResult={openResult}");

                        PegasusStaticClassReader.CloseNetPort(PortHandle);

                        Program.CountToSendFailOpenPort++;

                        _logger.LogInfo($"Open netPort that bai lan thu: {Program.CountToSendFailOpenPort}");

                        if (Program.CountToSendFailOpenPort == 3)
                        {
                            _logger.LogInfo($"Thời điểm gửi cảnh báo gần nhất: {Program.SendFailOpenPortLastTime}");

                            if (Program.SendFailOpenPortLastTime == null || Program.SendFailOpenPortLastTime < DateTime.Now.AddMinutes(-3))
                            {
                                Program.SendFailOpenPortLastTime = DateTime.Now;

                                // gửi thông báo ping thất bại
                                var pushMessage = $"Điểm xác thực: mở kết nối không thành công đến anten {PegasusAdr}. Vui lòng báo kỹ thuật kiểm tra";

                                _logger.LogInfo($"Gửi cảnh báo: {pushMessage}");

                                SendNotificationByRight(RightCode.CONFIRM, pushMessage);
                            }

                            Program.CountToSendFailOpenPort = 0;
                        }

                        Thread.Sleep(5000);
                    }
                    else
                    {
                        Program.CountToSendFailOpenPort = 0;

                        _logger.LogInfo($"Open netPort thanh cong: PegasusAdr={PegasusAdr} -- port={port} --  openResult={openResult}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInfo($"OpenNetPort ERROR:{ex.StackTrace} --- {ex.Message}");
                }
            }

            _logger.LogInfo($"Connected Pegasus IP:{PegasusAdr} - Port: {PortHandle}");

            Program.UHFConnected = true;

            // 2. Đọc dữ liệu từ thiết bị
            ReadDataFromPegasus();
        }

        public async void ReadDataFromPegasus()
        {
            _logger.LogInfo($"Reading Pegasus...");

            while (Program.UHFConnected)
            {
                try
                {
                    var data = PegasusReader.Inventory_G2(ref ComAddr, 0, 0, 0, PortHandle);

                    foreach (var item in data)
                    {
                        try
                        {
                            var cardNoCurrent = ByteArrayToString(item);

                            Program.LastTimeReceivedUHF = DateTime.Now;

                            _logger.LogInfo($"====== CardNo : {cardNoCurrent}");

                            await ReadDataProcess(cardNoCurrent);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($@"Co loi xay ra khi xu ly RFID {ex.StackTrace} {ex.Message} ");
                            Program.UHFConnected = false;
                            break;
                        }
                    }
                }
                catch(Exception err) 
                {
                    _logger.LogError($@"ReadDataFromPegasus ERROR: {err.StackTrace} {err.Message}");
                    Program.UHFConnected = false;
                    break;
                }
            }

            AuthenticateUhfFromPegasus();
        }

        private async Task ReadDataProcess(string cardNoCurrent)
        {
            if (Program.IsLockingRfid)
            {
                _logger.LogInfo($"== Diem xac thuc dang xu ly => Ket thuc {cardNoCurrent} == ");

                new ConfirmHub().SendMessage("IS_LOCKING_RFID", "1");
            }
            else
            {
                new ConfirmHub().SendMessage("IS_LOCKING_RFID", "0");
            }

            // Loại bỏ các tag đã check trước đó
            if (tmpInvalidCardNoLst.Count > 10)
            {
                tmpInvalidCardNoLst.RemoveRange(0, 3);
            }

            if (tmpInvalidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddSeconds(-15)))
            {
                //_confirmLogger.LogInfo($@"2. Tag KHONG HOP LE da duoc check truoc do => Ket thuc.");
                return;
            }

            if (tmpValidCardNoLst.Count > 10)
            {
                tmpValidCardNoLst.RemoveRange(0, 3);
            }

            if (tmpValidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-3)))
            {
                //_confirmLogger.LogInfo($@"2. Tag HOP LE da duoc check truoc do => Ket thuc.");
                return;
            }

            _logger.LogInfo("----------------------------");
            _logger.LogInfo($"Tag: {cardNoCurrent}");
            _logger.LogInfo("-----");

            _logger.LogInfo($"2. Kiem tra tag da check truoc do");

            // Kiểm tra RFID có hợp lệ hay không
            string vehicleCodeCurrent = _rfidRepository.GetVehicleCodeByCardNo(cardNoCurrent);

            if (!String.IsNullOrEmpty(vehicleCodeCurrent))
            {
                _logger.LogInfo($"3. Tag hop le: vehicle={vehicleCodeCurrent}");
            }
            else
            {
                _logger.LogInfo($"3. Tag KHONG hop le => Ket thuc.");

                SendNotificationHub("CONFIRM_VEHICLE", 0, cardNoCurrent, $"RFID {cardNoCurrent} không thuộc hệ thống");
                SendNotificationAPI("CONFIRM_VEHICLE", 0, cardNoCurrent, $"RFID {cardNoCurrent} không thuộc hệ thống");

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                tmpInvalidCardNoLst.Add(newCardNoLog);

                return;
            }

            // Nếu RFID hợp lệ
            tblStoreOrderOperating currentOrder = null;
            //var isValidCardNo = false;

            currentOrder = await _storeOrderOperatingRepository.GetCurrentOrderConfirmationPoint(vehicleCodeCurrent);

            //isValidCardNo = OrderValidator.IsValidOrderConfirmationPoint(currentOrder);

            var checkValidCardNoResult = OrderValidator.CheckValidOrderConfirmationPoint(currentOrder);

            // Nếu RFID không có đơn hàng
            if (checkValidCardNoResult == CheckValidRfidResultCode.CHUA_CO_DON)
            {
                _logger.LogInfo($"4. Tag KHONG co don hang => Ket thuc.");

                #region Gọi loa thông báo
                using (var db = new XHTD_Entities())
                {
                    try
                    {
                        var timeToAdd = DateTime.Now.AddMinutes(-10);

                        var checkExist = db.tblCallVehicleStatus
                                            .FirstOrDefault(x => x.Vehicle == vehicleCodeCurrent 
                                                            && x.CallType == CallType.CHUA_CO_DON
                                                            //&& x.IsDone == false
                                                            && x.CreatedOn != null
                                                            && x.CreatedOn > timeToAdd
                                                            );

                        if (checkExist == null) {
                            var newTblVehicleStatus = new tblCallVehicleStatu
                            {
                                Vehicle = vehicleCodeCurrent,
                                CountTry = 0,
                                CreatedOn = DateTime.Now,
                                ModifiledOn = DateTime.Now,
                                LogCall = $@"Đưa xe thông báo chưa có đơn lúc {DateTime.Now}. ",
                                IsDone = false,
                                CallType = CallType.CHUA_CO_DON
                            };

                            db.tblCallVehicleStatus.Add(newTblVehicleStatus);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInfo($"ERROR CHUA_CO_DON: {ex.Message} -- {ex.StackTrace} -- {ex.InnerException}");
                    }
                }
                #endregion

                SendNotificationHub("CONFIRM_VEHICLE", 1, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} chưa có đơn hàng");
                SendNotificationAPI("CONFIRM_VEHICLE", 1, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} chưa có đơn hàng");

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                tmpInvalidCardNoLst.Add(newCardNoLog);

                return;
            }
            // Nếu RFID không có đơn hàng hợp lệ: chưa nhập đơn
            else if (checkValidCardNoResult == CheckValidRfidResultCode.CHUA_NHAN_DON)
            {
                _logger.LogInfo($"4. Tag KHONG co don hang hop le: chưa nhận đơn => Ket thuc.");

                #region Gọi loa thông báo
                using (var db = new XHTD_Entities())
                {
                    try
                    {
                        var timeToAdd = DateTime.Now.AddMinutes(-10);

                        var checkExist = db.tblCallVehicleStatus
                                            .FirstOrDefault(x => x.Vehicle == vehicleCodeCurrent
                                                            && x.CallType == CallType.CHUA_NHAN_DON
                                                            //&& x.IsDone == false
                                                            && x.CreatedOn != null
                                                            && x.CreatedOn > timeToAdd
                                                            );

                        if (checkExist == null)
                        {
                            var newTblVehicleStatus = new tblCallVehicleStatu
                            {
                                Vehicle = vehicleCodeCurrent,
                                CountTry = 0,
                                CreatedOn = DateTime.Now,
                                ModifiledOn = DateTime.Now,
                                LogCall = $@"Đưa xe thông báo chưa nhận đơn lúc {DateTime.Now}. ",
                                IsDone = false,
                                CallType = CallType.CHUA_NHAN_DON
                            };

                            db.tblCallVehicleStatus.Add(newTblVehicleStatus);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInfo($"ERROR CHUA_NHAN_DON: {ex.Message} -- {ex.StackTrace} -- {ex.InnerException}");
                    }
                }
                #endregion

                SendNotificationHub("CONFIRM_VEHICLE", 1, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} lái xe chưa nhận đơn hàng");
                SendNotificationAPI("CONFIRM_VEHICLE", 1, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} lái xe chưa nhận đơn hàng");

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                tmpInvalidCardNoLst.Add(newCardNoLog);

                return;
            }
            // Nếu RFID không có đơn hàng hợp lệ: đã xác thực
            else if (checkValidCardNoResult == CheckValidRfidResultCode.DA_XAC_THUC)
            {
                _logger.LogInfo($"4. Tag KHONG co don hang hop le: đã xác thực => Ket thuc.");

                SendNotificationHub("CONFIRM_VEHICLE", 1, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} đã xác thực thành công");
                SendNotificationAPI("CONFIRM_VEHICLE", 1, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} đã xác thực thành công");

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                tmpInvalidCardNoLst.Add(newCardNoLog);

                return;
            }
            // Nếu RFID có đơn hàng hợp lệ
            else
            {
                SendNotificationHub("CONFIRM_VEHICLE", 2, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} có đơn hàng hợp lệ", vehicleCodeCurrent);
                SendNotificationAPI("CONFIRM_VEHICLE", 2, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} có đơn hàng hợp lệ", vehicleCodeCurrent);

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };

                tmpValidCardNoLst.Add(newCardNoLog);

                Program.IsLockingRfid = true;
            }

            var currentDeliveryCode = currentOrder.DeliveryCode;
            _logger.LogInfo($"4. Tag co don hang hop le DeliveryCode = {currentDeliveryCode}");

            // Gọi API ERP kiểm tra điều kiện xác thực
            var orders = await _storeOrderOperatingRepository.GetOrdersConfirmationPoint(vehicleCodeCurrent);
            var currentDeliveryCodes = String.Empty;
            if (orders != null && orders.Count != 0)
            {
                currentDeliveryCodes = string.Join(";", orders.Select(x => x.DeliveryCode).Distinct().ToList());
            }
            
            var erpValidateResponse = DIBootstrapper.Init().Resolve<SaleOrdersApiLib>().CheckOrderValidate(currentDeliveryCodes);
            if (erpValidateResponse.Code == "02")
            {
                SendNotificationHub("CONFIRM_RESULT", 0, cardNoCurrent, $"Xác thực thất bại: {erpValidateResponse.Message}");
                SendNotificationAPI("CONFIRM_RESULT", 0, cardNoCurrent, $"Xác thực thất bại: {erpValidateResponse.Message}");

                var pushMessage = $"Phương tiện {vehicleCodeCurrent} xác thực xếp số tự động thất bại, lái xe vui lòng liên hệ bộ phận điều hành để được hỗ trợ, trân trọng! Chi tiết: {erpValidateResponse.Message}";
                SendNotificationByRight(RightCode.CONFIRM, pushMessage);

                var driverUserName = currentOrder.DriverUserName;
                if (driverUserName != null)
                {
                    SendPushNotification(currentOrder.DriverUserName, pushMessage);
                }

                _logger.LogInfo($"Phương tiện {vehicleCodeCurrent} xác thực xếp số tự động thất bại, {erpValidateResponse.Message} - DeliveryCode: {currentDeliveryCodes}!");

                return;
            }

            // Xác thực
            bool isConfirmSuccess = await this._storeOrderOperatingRepository.UpdateBillOrderConfirm10(vehicleCodeCurrent);

            // Xác thực thành công
            if (isConfirmSuccess)
            {
                // Xếp số
                this._storeOrderOperatingRepository.UpdateIndexOrderForNewConfirm(vehicleCodeCurrent);

                SendNotificationHub("CONFIRM_RESULT", 1, cardNoCurrent, $"Xác thực thành công", vehicleCodeCurrent);
                SendNotificationAPI("CONFIRM_RESULT", 1, cardNoCurrent, $"Xác thực thành công", vehicleCodeCurrent);

                #region Điều hướng gọi loa
                _logger.LogInfo($"Dieu huong goi loa vao cong hoac bai cho");

                var typeProduct = currentOrder.TypeProduct.ToUpper();
                var sourceDocumentId = currentOrder.SourceDocumentId ?? 0;

                var currentNumberWaitingVehicleInFactory = 0;
                int? maxVehicle = 0;

                using (var db = new XHTD_Entities())
                {
                    try
                    {
                        var config = await db.tblCallToGatewayConfigs.FirstOrDefaultAsync(x => x.SourceDocumentId == sourceDocumentId && x.Status == 1);
                        if (config == null)
                        {
                            _logger.LogInfo($"Don hang thuoc cau hinh ke hoach chung");

                            config = await db.tblCallToGatewayConfigs.FirstOrDefaultAsync(x => x.SourceDocumentId == 0);
                            currentNumberWaitingVehicleInFactory = _storeOrderOperatingRepository.CountStoreOrderWaitingIntoTroughByTypeAndExportPlan(typeProduct, 0);
                        }
                        else
                        {
                            _logger.LogInfo($"Don hang co cau hinh ke hoach rieng");

                            currentNumberWaitingVehicleInFactory = _storeOrderOperatingRepository.CountStoreOrderWaitingIntoTroughByTypeAndExportPlan(typeProduct, sourceDocumentId);
                        }

                        _logger.LogInfo($"So xe {typeProduct} hien tai: {currentNumberWaitingVehicleInFactory}");

                        maxVehicle = GetMaxVehicle(config, typeProduct);

                        _logger.LogInfo($"So xe {typeProduct} cau hinh toi da: {maxVehicle}");

                        if (currentNumberWaitingVehicleInFactory >= maxVehicle)
                        {
                            _logger.LogInfo($"Them vao hang doi goi vao BAI CHO");

                            var newTblVehicleStatus = new tblCallVehicleStatu
                            {
                                StoreOrderOperatingId = currentOrder.Id,
                                CountTry = 0,
                                TypeProduct = currentOrder.TypeProduct,
                                CreatedOn = DateTime.Now,
                                ModifiledOn = DateTime.Now,
                                LogCall = $@"Đưa xe vào bãi chờ lúc {DateTime.Now}. ",
                                IsDone = false,
                                CallType = CallType.BAI_CHO
                            };

                            db.tblCallVehicleStatus.Add(newTblVehicleStatus);
                            db.SaveChanges();

                            _logger.LogInfo($"Them vao hang doi goi vao BAI CHO thanh cong");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInfo($"ERROR BAI CHO: {ex.Message} -- {ex.StackTrace} -- {ex.InnerException}");
                    }
                }
                #endregion

                #region Gửi thông báo notification
                var pushMessage = currentNumberWaitingVehicleInFactory < maxVehicle ?
                                  $"Đơn hàng {currentDeliveryCode} phương tiện {vehicleCodeCurrent} xác thực xếp số tự động thành công, lái xe vui lòng di chuyển vào cổng lấy hàng, trân trọng!" :
                                  $"Đơn hàng {currentDeliveryCode} phương tiện {vehicleCodeCurrent} xác thực xếp số tự động thành công, lái xe vui lòng di chuyển vào bãi chờ, trân trọng!";
                SendNotificationByRight(RightCode.CONFIRM, pushMessage);

                var driverUserName = currentOrder.DriverUserName;
                if (driverUserName != null)
                {
                    SendPushNotification(driverUserName, pushMessage);
                }
                #endregion

                // Bật đèn xanh - đỏ
                TurnOnTrafficLight();

                #region Cập nhật trạng thái in phiếu
                var erpUpdateStatusResponse = DIBootstrapper.Init().Resolve<SaleOrdersApiLib>().UpdateOrderStatus(currentDeliveryCodes);
                if (erpUpdateStatusResponse.Code == "01")
                {
                    var pushMessagePrintStatus = $"Đơn hàng {currentDeliveryCodes} phương tiện {vehicleCodeCurrent} cập nhật trạng thái in phiếu thành công!";
                    SendNotificationByRight(RightCode.CONFIRM, pushMessagePrintStatus);

                    _logger.LogInfo($"{pushMessagePrintStatus}");
                }
                else if (erpUpdateStatusResponse.Code == "02")
                {
                    var pushMessagePrintStatus = $"Đơn hàng {currentDeliveryCodes} phương tiện {vehicleCodeCurrent} cập nhật trạng thái in phiếu thất bại! Chi tiết: {erpUpdateStatusResponse.Message}!";
                    SendNotificationByRight(RightCode.CONFIRM, pushMessagePrintStatus);

                    _logger.LogInfo($"{pushMessagePrintStatus}");
                }
                #endregion
            }
            else
            {
                SendNotificationHub("CONFIRM_RESULT", 0, cardNoCurrent, $"Xác thực thất bại");
                SendNotificationAPI("CONFIRM_RESULT", 0, cardNoCurrent, $"Xác thực thất bại");

                var pushMessage = $"Đơn hàng {currentDeliveryCode} phương tiện {vehicleCodeCurrent} xác thực xếp số tự động thất bại, lái xe vui lòng liên hệ bộ phận điều hành để được hỗ trợ, trân trọng!";
                SendNotificationByRight(RightCode.CONFIRM, pushMessage);

                var driverUserName = currentOrder.DriverUserName;
                if (driverUserName != null)
                {
                    SendPushNotification(driverUserName, pushMessage);
                }

                _logger.LogError($"Co loi xay ra khi xac thuc rfid: {cardNoCurrent}");
            }

            _logger.LogInfo($"10. Giai phong RFID IN");

            Program.IsLockingRfid = false;
        }

        public string ByteArrayToString(byte[] b)
        {
            return BitConverter.ToString(b).Replace("-", "");
        }

        public string GetTrafficLightIpAddress()
        {
            var ipAddress = "";

            ipAddress = trafficLight?.IpAddress;

            return ipAddress;
        }

        public void TurnOnTrafficLight()
        {
            _logger.LogInfo($"7. Bật đèn xanh");
            if (TurnOnGreenTrafficLight())
            {
                _logger.LogInfo($"7.2. Bật đèn xanh thành công");
            }
            else
            {
                _logger.LogInfo($"7.2. Bật đèn xanh thất bại");
            }

            Thread.Sleep(20000);

            _logger.LogInfo($"8. Bật đèn đỏ");
            if (TurnOnRedTrafficLight())
            {
                _logger.LogInfo($"8.2. Bật đèn đỏ thành công");
            }
            else
            {
                _logger.LogInfo($"8.2. Bật đèn đỏ thất bại");
            }
        }

        public bool TurnOnGreenTrafficLight()
        {
            var ipAddress = GetTrafficLightIpAddress();

            if (String.IsNullOrEmpty(ipAddress))
            {
                return false;
            }

            _logger.LogInfo($"7.1. IP đèn: {ipAddress}");

            _trafficLight.Connect(ipAddress);

            return _trafficLight.TurnOnGreenOffRed();
        }

        public bool TurnOnRedTrafficLight()
        {
            var ipAddress = GetTrafficLightIpAddress();

            if (String.IsNullOrEmpty(ipAddress))
            {
                return false;
            }

            _logger.LogInfo($"8.1. IP đèn: {ipAddress}");

            _trafficLight.Connect(ipAddress);

            return _trafficLight.TurnOffGreenOnRed();
        }

        private void SendNotificationHub(string name, int status, string cardNo, string message, string vehicle = "")
        {
            new ConfirmHub().SendNotificationConfirmationPoint(name, status, cardNo, message, vehicle);
        }

        public void SendNotificationAPI(string name, int status, string cardNo, string message, string vehicle = "")
        {
            try
            {
                _notification.SendConfirmNotification(name, status, cardNo, message, vehicle);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SendNotificationAPI Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }

        public void SendPushNotification(string userNameReceiver, string message)
        {
            try
            {
                _logger.LogInfo($"Gửi push notification đến {userNameReceiver}, nội dung {message}");
                _notification.SendPushNotification(userNameReceiver, message);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SendPushNotification Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }

        public void SendNotificationByRight(string rightCode, string message)
        {
            try
            {
                _logger.LogInfo($"Gửi push notification đến các user với quyền {rightCode}, nội dung {message}");
                _notification.SendNotificationByRight(rightCode, message);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SendNotificationByRight Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }

        #region Read RFID by C3-400
        public void AuthenticateConfirmModule()
        {
            // 1. Connect Device
            while (!DeviceConnected)
            {
                ConnectConfirmationPointModule();
            }

            // 2. Đọc dữ liệu từ thiết bị
            ReadDataFromC3400();
        }

        public bool ConnectConfirmationPointModule()
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
                        _logger.LogInfo($"Connected to C3-400 {ipAddress}");

                        DeviceConnected = true;
                    }
                    else
                    {
                        _logger.LogInfo($"Connect to C3-400 {ipAddress} failed");
                        ret = PullLastError();
                        DeviceConnected = false;
                    }
                }
                return DeviceConnected;
            }
            catch (Exception ex)
            {
                _logger.LogInfo($@"Connect to C3-400 {ipAddress} error: {ex.Message}");
                return false;
            }
        }

        public async void ReadDataFromC3400()
        {
            _logger.LogInfo("Reading RFID from C3-400 ...");

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
                                    var cardNoCurrent = tmp[2]?.ToString(); // RFID
                                    var doorCurrent = tmp[3]?.ToString(); // Điểm xác thực
                                    var timeCurrent = tmp[0]?.ToString(); // Thời gian xác thực

                                    if (Program.IsLockingRfid)
                                    {
                                        _logger.LogInfo($"== Diem xac thuc dang xu ly => Ket thuc {cardNoCurrent} == ");

                                        new ConfirmHub().SendMessage("IS_LOCKING_RFID", "1");
                                    }
                                    else
                                    {
                                        new ConfirmHub().SendMessage("IS_LOCKING_RFID", "0");
                                    }

                                    // Loại bỏ các tag đã check trước đó
                                    if (tmpInvalidCardNoLst.Count > 10)
                                    {
                                        tmpInvalidCardNoLst.RemoveRange(0, 3);
                                    }

                                    if (tmpInvalidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddSeconds(-15)))
                                    {
                                        continue;
                                    }

                                    if (tmpValidCardNoLst.Count > 10)
                                    {
                                        tmpValidCardNoLst.RemoveRange(0, 3);
                                    }

                                    if (tmpValidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddMinutes(-3)))
                                    {
                                        continue;
                                    }

                                    _logger.LogInfo("----------------------------");
                                    _logger.LogInfo($"Tag: {cardNoCurrent}, door: {doorCurrent}, time: {timeCurrent}");
                                    _logger.LogInfo("-----");

                                    _logger.LogInfo($"2. Kiem tra tag da check truoc do");

                                    // Kiểm tra RFID có hợp lệ hay không
                                    string vehicleCodeCurrent = _rfidRepository.GetVehicleCodeByCardNo(cardNoCurrent);

                                    if (!String.IsNullOrEmpty(vehicleCodeCurrent))
                                    {
                                        _logger.LogInfo($"3. Tag hop le: vehicle={vehicleCodeCurrent}");
                                    }
                                    else
                                    {
                                        _logger.LogInfo($"3. Tag KHONG hop le => Ket thuc.");

                                        SendNotificationHub("CONFIRM_VEHICLE", 0, cardNoCurrent, $"RFID {cardNoCurrent} không thuộc hệ thống");
                                        SendNotificationAPI("CONFIRM_VEHICLE", 0, cardNoCurrent, $"RFID {cardNoCurrent} không thuộc hệ thống");

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                        tmpInvalidCardNoLst.Add(newCardNoLog);

                                        continue;
                                    }

                                    // Nếu RFID hợp lệ
                                    tblStoreOrderOperating currentOrder = null;
                                    var isValidCardNo = false;

                                    currentOrder = await _storeOrderOperatingRepository.GetCurrentOrderConfirmationPoint(vehicleCodeCurrent);

                                    isValidCardNo = OrderValidator.IsValidOrderConfirmationPoint(currentOrder);

                                    // Nếu RFID không có đơn hàng
                                    if (currentOrder == null)
                                    {
                                        _logger.LogInfo($"4. Tag KHONG co don hang => Ket thuc.");

                                        SendNotificationHub("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng");
                                        SendNotificationAPI("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng");

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                        tmpInvalidCardNoLst.Add(newCardNoLog);

                                        continue;
                                    }

                                    // Nếu RFID không có đơn hàng hợp lệ
                                    else if (isValidCardNo == false)
                                    {
                                        _logger.LogInfo($"4. Tag KHONG co don hang hop le => Ket thuc.");

                                        SendNotificationHub("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng hợp lệ");
                                        SendNotificationAPI("CONFIRM_VEHICLE", 1, cardNoCurrent, $"Phương tiện {vehicleCodeCurrent} - RFID {cardNoCurrent} không có đơn hàng hợp lệ");

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                                        tmpInvalidCardNoLst.Add(newCardNoLog);

                                        continue;
                                    }

                                    // Nếu RFID có đơn hàng hợp lệ
                                    else
                                    {
                                        SendNotificationHub("CONFIRM_VEHICLE", 2, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} có đơn hàng hợp lệ", vehicleCodeCurrent);
                                        SendNotificationAPI("CONFIRM_VEHICLE", 2, cardNoCurrent, $"{vehicleCodeCurrent} - RFID {cardNoCurrent} có đơn hàng hợp lệ", vehicleCodeCurrent);

                                        var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };

                                        tmpValidCardNoLst.Add(newCardNoLog);

                                        Program.IsLockingRfid = true;
                                    }

                                    var currentDeliveryCode = currentOrder.DeliveryCode;
                                    _logger.LogInfo($"4. Tag co don hang hop le DeliveryCode = {currentDeliveryCode}");

                                    // Xác thực
                                    bool isConfirmSuccess = await this._storeOrderOperatingRepository.UpdateBillOrderConfirm10(vehicleCodeCurrent);

                                    // Xác thực thành công
                                    if (isConfirmSuccess)
                                    {
                                        SendNotificationHub("CONFIRM_RESULT", 1, cardNoCurrent, $"Xác thực thành công", vehicleCodeCurrent);
                                        SendNotificationAPI("CONFIRM_RESULT", 1, cardNoCurrent, $"Xác thực thành công", vehicleCodeCurrent);

                                        // Xếp số
                                        this._storeOrderOperatingRepository.UpdateIndexOrderForNewConfirm(vehicleCodeCurrent);

                                        int statusGreenLight = 0;
                                        string messageGreenLight = "";

                                        _logger.LogInfo($"7. Bật đèn xanh");
                                        if (TurnOnGreenTrafficLight())
                                        {
                                            statusGreenLight = 1;
                                            messageGreenLight = "Bật đèn xanh thành công";
                                            _logger.LogInfo($"7.2. Bật đèn xanh thành công");
                                        }
                                        else
                                        {
                                            statusGreenLight = 0;
                                            messageGreenLight = "Bật đèn xanh thất bại";
                                            _logger.LogInfo($"7.2. Bật đèn xanh thất bại");
                                        }

                                        //var img = new HikvisionStreamCamera().CaptureStream(CAMERA_IP, CAMERA_USER_NAME, CAMERA_PASSWORD, "CONFIRM", CAMERA_NUMBER, IMG_PATH);

                                        //if (!string.IsNullOrEmpty(img))
                                        //{
                                        //    _storeOrderOperatingRepository.UpdateImgConfirm10(vehicleCodeCurrent, img);
                                        //}

                                        //await SendNotificationHub("CONFIRM_RESULT", statusGreenLight, cardNoCurrent, messageGreenLight);

                                        //SendNotificationAPI("CONFIRM_RESULT", statusGreenLight, cardNoCurrent, messageGreenLight);

                                        Thread.Sleep(10000);

                                        int statusRedLight = 0;
                                        string messageRedLight = "";

                                        _logger.LogInfo($"8. Bật đèn đỏ");
                                        if (TurnOnRedTrafficLight())
                                        {
                                            statusRedLight = 1;
                                            messageRedLight = "Bật đèn đỏ thành công";
                                            _logger.LogInfo($"8.2. Bật đèn đỏ thành công");
                                        }
                                        else
                                        {
                                            statusRedLight = 0;
                                            messageRedLight = "Bật đèn đỏ thất bại";
                                            _logger.LogInfo($"8.2. Bật đèn đỏ thất bại");
                                        }

                                        //await SendNotificationHub("CONFIRM_RESULT", statusRedLight, cardNoCurrent, messageRedLight);

                                        //SendNotificationAPI("CONFIRM_RESULT", statusRedLight, cardNoCurrent, messageRedLight);
                                    }
                                    else
                                    {
                                        SendNotificationHub("CONFIRM_RESULT", 0, cardNoCurrent, $"Xác thực thất bại");
                                        SendNotificationAPI("CONFIRM_RESULT", 0, cardNoCurrent, $"Xác thực thất bại");

                                        _logger.LogError($"Co loi xay ra khi xac thuc rfid: {cardNoCurrent}");
                                    }

                                    _logger.LogInfo($"10. Giai phong RFID IN");

                                    Program.IsLockingRfid = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($@"Co loi xay ra khi xu ly RFID {ex.StackTrace} {ex.Message} ");
                                continue;
                            }
                        }
                        else
                        {
                            _logger.LogWarn("No data. Reconnect ...");
                            DeviceConnected = false;
                            h21 = IntPtr.Zero;

                            AuthenticateConfirmModule();
                        }
                    }
                }
            }
            else
            {
                DeviceConnected = false;
                h21 = IntPtr.Zero;

                AuthenticateConfirmModule();
            }
        }
        #endregion

        public int? GetMaxVehicle(tblCallToGatewayConfig config, string typeProduct)
        {
            switch (typeProduct.ToUpper())
            {
                case "PCB30":
                    return config.MaxVehiclePcb30;
                case "PCB40":
                    return config.MaxVehiclePcb40;
                case "CLINKER":
                    return config.MaxVehicleClinker;
                case "ROI":
                    return config.MaxVehicleRoi;
                case "C91":
                    return config.MaxVehicleC91;
                case "JUMBO":
                    return config.MaxVehicleJumbo;
                case "SLING":
                    return config.MaxVehicleSling;
                case "OTHER":
                    return config.MaxVehicleOther;
                default:
                    return 0;
            }
        }
    }
}
