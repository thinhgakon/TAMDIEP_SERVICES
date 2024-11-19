using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_XB_TROUGH_1.Models.Response;
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
using XHTD_SERVICES_XB_TROUGH_1.Hubs;
using System.Net.NetworkInformation;
using XHTD_SERVICES_XB_TROUGH_1.Devices;
using XHTD_SERVICES_XB_TROUGH_1.Business;
using XHTD_SERVICES.Helper.Models.Request;

namespace XHTD_SERVICES_XB_TROUGH_1.Jobs
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

        protected readonly TroughRepository _troughRepository;

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
        private string PegasusAdr = "192.168.13.191";

        private readonly string TROUGH_CODE = "1";

        public TroughJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            RfidRepository rfidRepository,
            CategoriesDevicesRepository categoriesDevicesRepository,
            CategoriesDevicesLogRepository categoriesDevicesLogRepository,
            SystemParameterRepository systemParameterRepository,
            MachineRepository machineRepository,
            TroughRepository troughRepository,
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
            _troughRepository = troughRepository;
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
                                var pushMessage = $"Xi bao 1: mở kết nối không thành công đến anten {PegasusAdr}. Vui lòng báo kỹ thuật kiểm tra";

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

            AuthenticateUhfFromPegasus();
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

            var machine = await _machineRepository.GetMachineByTroughCode(TROUGH_CODE);

            // Kiểm tra RFID có hợp lệ hay không
            string vehicleCodeCurrent = _rfidRepository.GetVehicleCodeByCardNo(cardNoCurrent);

            // Đơn hàng đầu tiên hiện tại trong máng
            var orderInTrough = _callToTroughRepository.GetCurrentFirstOrderInTrough(TROUGH_CODE);

            var trough = await _troughRepository.GetTroughByTroughCode(TROUGH_CODE);

            if (machine == null)
            {
                _logger.LogInfo($"2. Máy không tồn tại => Kết thúc");
                return;
            }

            if (!String.IsNullOrEmpty(vehicleCodeCurrent))
            {
                var newCardNoLog = new CardNoLog { CardNo = cardNoCurrent, DateTime = DateTime.Now };
                tmpValidCardNoLst.Add(newCardNoLog);

                _logger.LogInfo($"3. Tag hợp lệ: vehicle: {vehicleCodeCurrent}");
                SendNotificationHub("XI_BAO", machine.Code, TROUGH_CODE, vehicleCodeCurrent);
                SendNotificationAPI("XI_BAO", machine.Code, TROUGH_CODE, vehicleCodeCurrent);

                if (trough != null && !string.IsNullOrEmpty(trough.DeliveryCodeCurrent))
                {
                    var oldOrder = await _storeOrderOperatingRepository.GetDetail(trough.DeliveryCodeCurrent);
                    if (oldOrder.Vehicle.ToUpper() != vehicleCodeCurrent.ToUpper())
                    {
                        if (oldOrder.ExportedNumber == oldOrder.SumNumber && machine.StartStatus == "ON" && machine.StopStatus == "OFF")
                        {
                            var response = await _machineRepository.Stop(machine.Code, trough.Code, oldOrder.DeliveryCode);

                            _logger.LogInfo($"Stop Machine Response: Status = {response}");
                        }
                    }
                }

                if (orderInTrough != null && vehicleCodeCurrent.ToUpper() == orderInTrough.Vehicle.ToUpper())
                {
                    var response = await _machineRepository.Start(machine.Code, trough.Code, orderInTrough.DeliveryCode);

                    _logger.LogInfo($"Start Machine Response: Status = {response}");
                }

                else _logger.LogInfo($"3. Phương tiện {vehicleCodeCurrent} không phải là phương tiện đầu tiên trong máng! => Kết thúc");
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
    }
}
