using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_SLING_TROUGH_9.Models.Response;
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
using XHTD_SERVICES_SLING_TROUGH_9.Hubs;
using System.Net.NetworkInformation;
using XHTD_SERVICES_SLING_TROUGH_9.Devices;
using XHTD_SERVICES.Helper.Models.Request;
using XHTD_SERVICES_SLING_TROUGH_9.Business;
using System.Data.Entity;
using XHTD_SERVICES.Data.Models.Values;

namespace XHTD_SERVICES_SLING_TROUGH_9.Jobs
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

        private List<CardNoLog> tmpValidCardNoLst = new List<CardNoLog>();

        private List<CardNoLog> tmpInvalidCardNoLst = new List<CardNoLog>();

        private tblCategoriesDevice c3400;

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
        private string PegasusAdr = "192.168.13.242";

        private readonly string TROUGH_CODE = "9";

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
                        _logger.LogInfo("Service nhận diện RFID đang TẮT.");
                        return;
                    }

                    _logger.LogInfo($"--------------- START JOB - IP: {PegasusAdr} ---------------");

                    AuthenticateConfirmModuleFromPegasus();
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

        public void AuthenticateConfirmModuleFromPegasus()
        {
            // 1. Connect Device
            int port = PortHandle;
            var openResult = 1;
            while (openResult != 0)
            {
                try
                {
                    openResult = PegasusStaticClassReader.OpenNetPort(PortHandle, PegasusAdr, ref ComAddr, ref port);
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
                            _logger.LogError($@"Co loi xay ra khi xu ly RFID {ex.StackTrace} {ex.Message}");
                            Program.UHFConnected = false;
                            break;
                        }
                    }
                }
                catch (Exception err)
                {
                    _logger.LogError($@"ReadDataFromPegasus ERROR: {err.StackTrace} {err.Message}");
                    Program.UHFConnected = false;
                    break;
                }
            }

            AuthenticateConfirmModuleFromPegasus();
        }

        private async Task ReadDataProcess(string cardNoCurrent)
        {
            if (Program.IsLockingRfid)
            {
                _logger.LogInfo($"== Đầu đọc RFID đang xử lý => Kết thúc {cardNoCurrent} == ");

                new TroughHub().SendMessage("IS_LOCKING_RFID", "1");
            }
            else
            {
                new TroughHub().SendMessage("IS_LOCKING_RFID", "0");
            }

            // Loại bỏ các tag đã check trước đó
            if (tmpInvalidCardNoLst.Count > 10)
            {
                tmpInvalidCardNoLst.RemoveRange(0, 3);
            }

            if (tmpInvalidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddSeconds(-5)))
            {
                return;
            }

            if (tmpValidCardNoLst.Count > 10)
            {
                tmpValidCardNoLst.RemoveRange(0, 3);
            }

            if (tmpValidCardNoLst.Exists(x => x.CardNo.Equals(cardNoCurrent) && x.DateTime > DateTime.Now.AddSeconds(-5)))
            {
                return;
            }

            _logger.LogInfo("----------------------------");
            _logger.LogInfo($"Tag: {cardNoCurrent}");
            _logger.LogInfo("-----");

            _logger.LogInfo($"2. Kiểm tra tag đã check trước đó");

            var machineCode = string.Empty;
            using (var db = new XHTD_Entities())
            {
                var machineTrough = await db.TblMachineTroughs.FirstOrDefaultAsync(x => x.TroughCode == TROUGH_CODE);
                if (machineTrough == null) return;

                machineCode = machineTrough.MachineCode;
            }

            var machine = await _machineRepository.GetMachineByMachineCode(machineCode);

            // Kiểm tra RFID có hợp lệ hay không
            string vehicleCodeCurrent = _rfidRepository.GetVehicleCodeByCardNo(cardNoCurrent);

            if (!String.IsNullOrEmpty(vehicleCodeCurrent))
            {
                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                tmpValidCardNoLst.Add(newCardNoLog);

                _logger.LogInfo($"3. Tag hợp lệ: vehicle: {vehicleCodeCurrent}");
                SendNotificationHub("SLING", machineCode, TROUGH_CODE, vehicleCodeCurrent);
                SendNotificationAPI("SLING", machineCode, TROUGH_CODE, vehicleCodeCurrent);

                tblStoreOrderOperating currentOrder = null;

                using (var db = new XHTD_Entities())
                {
                    currentOrder = await db.tblStoreOrderOperatings.FirstOrDefaultAsync(x => x.Vehicle == vehicleCodeCurrent &&
                                                                                             x.CatId == OrderCatIdCode.XI_MANG_BAO &&
                                                                                             x.TypeXK == OrderTypeXKCode.SLING &&
                                                                                            (x.Step == (int)OrderStep.DA_CAN_VAO ||
                                                                                             x.Step == (int)OrderStep.DA_LAY_HANG) &&
                                                                                             x.IsVoiced == false);
                }

                if (currentOrder == null)
                {
                    _logger.LogInfo($"3. Tag KHÔNG có đơn hàng hợp lệ hoặc KHÔNG tìm thấy đơn hàng => Kết thúc");
                    return;
                }

                var requestDataList = new List<CallToTroughVehicleUpdateDto>();
                var requestData = new CallToTroughVehicleUpdateDto
                {
                    Id = null,
                    Machine = TROUGH_CODE,
                    IndexTrough = 1,
                    DeliveryCode = currentOrder.DeliveryCode
                };
                requestDataList.Add(requestData);

                var apiResponse = DIBootstrapper.Init().Resolve<MachineApiLib>().AddVehicleInTrough(requestDataList);
                if (apiResponse != null && apiResponse.Status == true && apiResponse.MessageObject.Code == "0103")
                {
                    _logger.LogInfo($"3. Thêm xe vào máng {TROUGH_CODE} thành công!");

                    List<tblStoreOrderOperating> ordersInTrough = new List<tblStoreOrderOperating>();
                    List<tblCallToTrough> callToTroughEntities = new List<tblCallToTrough>();

                    using (var db = new XHTD_Entities())
                    {
                        callToTroughEntities = await db.tblCallToTroughs.Where(x => x.DeliveryCode != currentOrder.DeliveryCode &&
                                                                                    x.Machine == TROUGH_CODE &&
                                                                                    x.IsDone == false).ToListAsync();

                        ordersInTrough = await (from orders in db.tblStoreOrderOperatings
                                                join callToTroughs in db.tblCallToTroughs
                                                on orders.DeliveryCode equals callToTroughs.DeliveryCode
                                                where callToTroughs.Machine == TROUGH_CODE &&
                                                      callToTroughs.IsDone == false &&
                                                      callToTroughs.DeliveryCode != currentOrder.DeliveryCode
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

                else _logger.LogInfo($"3. Thêm xe vào máng {TROUGH_CODE} thất bại! => Trough: {TROUGH_CODE} - Vehicle: {vehicleCodeCurrent} - DeliveryCode: {currentOrder.DeliveryCode}");
            }
            else
            {
                _logger.LogInfo($"3. Tag KHÔNG hợp lệ! => Kết thúc");

                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                tmpInvalidCardNoLst.Add(newCardNoLog);

                return;
            }

            _logger.LogInfo($"10. Giải phóng RFID");

            Program.IsLockingRfid = false;
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
    }
}
