using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_XR_TROUGH_1.Models.Response;
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
using XHTD_SERVICES_XR_TROUGH_1.Hubs;
using System.Net.NetworkInformation;
using XHTD_SERVICES_XR_TROUGH_1.Devices;
using XHTD_SERVICES.Helper.Models.Request;
using XHTD_SERVICES_XR_TROUGH_1.Business;
using System.Data.Entity;
using XHTD_SERVICES.Data.Models.Values;

namespace XHTD_SERVICES_XR_TROUGH_1.Jobs
{
    public class TroughJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly RfidRepository _rfidRepository;

        protected readonly CategoriesDevicesRepository _categoriesDevicesRepository;

        protected readonly CategoriesDevicesLogRepository _categoriesDevicesLogRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly MachineRepository _machineRepository;

        protected readonly Notification _notification;

        protected readonly TroughLogger _logger;

        private IntPtr h21 = IntPtr.Zero;

        private static bool DeviceConnected = false;

        private tblCategoriesDevice c3400;

        protected const string CONFIRM_ACTIVE = "AUTO_QUEUE_TO_TROUGH_XIROI_ACTIVE";

        private static bool isActiveService = true;

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "Connect")]
        public static extern IntPtr Connect(string Parameters);

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "PullLastError")]
        public static extern int PullLastError();

        [DllImport(@"C:\\Windows\\System32\\plcommpro.dll", EntryPoint = "GetRTLog")]
        public static extern int GetRTLog(IntPtr h, ref byte buffer, int buffersize);

        private readonly string CAMERA_IP = "192.168.13.231";
        private readonly string CAMERA_USER_NAME = "admin";
        private readonly string CAMERA_PASSWORD = "tamdiep@35";
        private readonly string IMG_PATH = "C:\\IMAGE";
        private readonly int CAMERA_NUMBER = 2;

        private byte ComAddr = 0xFF;
        private int PortHandle = 6000;
        private string PegasusAdr = "192.168.13.230";

        private readonly string MACHINE_CODE = "5";
        private readonly string TROUGH_CODE = "11";

        public TroughJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            RfidRepository rfidRepository,
            CategoriesDevicesRepository categoriesDevicesRepository,
            CategoriesDevicesLogRepository categoriesDevicesLogRepository,
            SystemParameterRepository systemParameterRepository,
            MachineRepository machineRepository,
            CallToTroughRepository callToTroughRepository,
            Notification notification,
            TroughLogger trough1Logger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _rfidRepository = rfidRepository;
            _categoriesDevicesRepository = categoriesDevicesRepository;
            _categoriesDevicesLogRepository = categoriesDevicesLogRepository;
            _systemParameterRepository = systemParameterRepository;
            _machineRepository = machineRepository;
            _callToTroughRepository = callToTroughRepository;
            _notification = notification;
            _logger = trough1Logger;
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
                        _logger.LogInfo("Service nhận diện RFID XI ROI đang TẮT.");
                        return;
                    }

                    _logger.LogInfo($"--------------- START JOB - IP: {PegasusAdr} ---------------");

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

        public void AuthenticateUhfFromPegasus()
        {
            // 1. Connect Device
            int port = PortHandle;
            var openResult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr, ref ComAddr, ref port);
            if (openResult == 0)
            {
                ReadDataFromPegasus();
            }
            PegasusStaticClassReader.CloseNetPort(port);
        }

        public async void ReadDataFromPegasus()
        {
            _logger.LogInfo($"Reading Pegasus...");

            try
            {
                var data = PegasusReader.Inventory_G2(ref ComAddr, 0, 0, 0, PortHandle);

                foreach (var item in data)
                {
                    try
                    {
                        var cardNoCurrent = ByteArrayToString(item);

                        _logger.LogInfo($"====== CardNo : {cardNoCurrent}");
                        SendTroughRfid("XIROI_UHF", cardNoCurrent);

                        await ReadDataProcess(cardNoCurrent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($@"Co loi xay ra khi xu ly RFID {ex.StackTrace} {ex.Message}");
                        break;
                    }
                }
            }
            catch (Exception err)
            {
                _logger.LogError($@"ReadDataFromPegasus ERROR: {err.StackTrace} {err.Message}");
            }
        }

        private async Task ReadDataProcess(string cardNoCurrent)
        {
            // Kiểm tra RFID có hợp lệ hay không
            string vehicleCodeCurrent = _rfidRepository.GetVehicleCodeByCardNo(cardNoCurrent);

            if (!String.IsNullOrEmpty(vehicleCodeCurrent))
            {
                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };

                _logger.LogInfo($"3. Tag hợp lệ: vehicle: {vehicleCodeCurrent}");
                SendNotificationHub("XI_ROI", MACHINE_CODE, TROUGH_CODE, vehicleCodeCurrent);
                SendNotificationAPI("XI_ROI", MACHINE_CODE, TROUGH_CODE, vehicleCodeCurrent);

                tblStoreOrderOperating currentOrder = null;

                using (var db = new XHTD_Entities())
                {
                    currentOrder = await db.tblStoreOrderOperatings.FirstOrDefaultAsync(x => x.Vehicle == vehicleCodeCurrent &&
                                                                                             x.CatId == OrderCatIdCode.XI_MANG_XA &&
                                                                                            (x.Step == (int)OrderStep.DA_CAN_VAO ||
                                                                                             x.Step == (int)OrderStep.DA_LAY_HANG) &&
                                                                                             x.IsVoiced == false);

                    if (currentOrder == null)
                    {
                        _logger.LogInfo($"3. Tag KHÔNG có đơn hàng hợp lệ hoặc KHÔNG tìm thấy đơn hàng => Kết thúc");
                        return;
                    }

                    await _callToTroughRepository.AddItem(currentOrder.Id, currentOrder.DeliveryCode, vehicleCodeCurrent, TROUGH_CODE, currentOrder.SumNumber ?? 0);
                    _logger.LogInfo($"3. Thêm xe vào máng {TROUGH_CODE} thành công!");

                    currentOrder.Step = (int)OrderStep.DANG_LAY_HANG;
                    currentOrder.TimeConfirm5 = DateTime.Now;
                    currentOrder.LogProcessOrder += $"#Xe được tự động xếp vào máng lúc {DateTime.Now}. ";

                    List<tblStoreOrderOperating> ordersInTrough = new List<tblStoreOrderOperating>();
                    List<tblCallToTrough> callToTroughEntities = new List<tblCallToTrough>();

                    callToTroughEntities = await db.tblCallToTroughs.Where(x => x.DeliveryCode != currentOrder.DeliveryCode &&
                                                                                x.Machine == TROUGH_CODE &&
                                                                                x.IsDone == false).ToListAsync();

                    ordersInTrough = await (from orders in db.tblStoreOrderOperatings
                                            join callToTroughs in db.tblCallToTroughs
                                            on orders.DeliveryCode equals callToTroughs.DeliveryCode
                                            where callToTroughs.Machine == TROUGH_CODE &&
                                                  callToTroughs.IsDone == false &&
                                                  callToTroughs.DeliveryCode != currentOrder.DeliveryCode &&
                                                  orders.Step == (int)OrderStep.DANG_LAY_HANG
                                            select orders).ToListAsync();

                    foreach (var callToTroughEntity in callToTroughEntities)
                    {
                        callToTroughEntity.IsDone = true;
                    }

                    foreach (var order in ordersInTrough)
                    {
                        order.Step = (int)OrderStep.DA_LAY_HANG;
                        order.TimeConfirm6 = DateTime.Now;
                        order.LogProcessOrder += $"#Xe lấy hàng lúc {DateTime.Now:dd/MM/yyyy HH:mm:ss} ";
                    }

                    await db.SaveChangesAsync();
                }
            }
            else
            {
                _logger.LogInfo($"3. Tag KHÔNG hợp lệ! => Kết thúc");
                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                return;
            }
        }

        public string ByteArrayToString(byte[] b)
        {
            return BitConverter.ToString(b).Replace("-", "");
        }

        private void SendNotificationHub(string troughType, string machineCode, string troughCode, string vehicle)
        {
            new TroughHub().SendNotificationTrough(troughType, machineCode, troughCode, vehicle);
        }

        public void SendNotificationAPI(string troughType, string machineCode, string troughCode, string vehicle)
        {
            try
            {
                _notification.SendTroughNotification(troughType, machineCode, troughCode, vehicle);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SendNotificationAPI Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }

        public void SendNotificationByRight(string rightCode, string message, string notificationType = null)
        {
            try
            {
                _logger.LogInfo($"Gửi push notification đến các user với quyền {rightCode}, nội dung {message}");
                _notification.SendNotificationByRight(rightCode, message, notificationType);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SendNotificationByRight Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }

        public void SendTroughRfid(string locationCode, string rfid)
        {
            try
            {
                _logger.LogInfo($"Gửi signalR rfid với mã {rfid}");
                _notification.SendTroughRfid(locationCode, rfid);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"SendTroughRfid Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }
    }
}
