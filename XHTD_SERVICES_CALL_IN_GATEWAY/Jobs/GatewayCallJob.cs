using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;
using RestSharp;
using XHTD_SERVICES.Data.Models.Response;
using Newtonsoft.Json;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES.Helper.Models.Request;
using System.Threading;
using XHTD_SERVICES.Data.Entities;
using System.Data.SqlClient;
using XHTD_SERVICES.Data.Models.Values;

namespace XHTD_SERVICES_CALL_IN_GATEWAY.Jobs
{
    [DisallowConcurrentExecution]
    public class GatewayCallJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly string CONG = CallType.CONG;
        protected readonly string BAI_CHO = CallType.BAI_CHO;

        protected readonly GatewayCallLogger _gatewayCallLogger;

        public GatewayCallJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            GatewayCallLogger gatewayCallLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _gatewayCallLogger = gatewayCallLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                await CallVehicleProcess();
            });
        }

        public async Task CallVehicleProcess()
        {
            _gatewayCallLogger.LogInfo("================= Bắt đầu service gọi loa ====================");

            try
            {
                var isWillCall = false;
                var type = CONG;
                var vehiceCode = "";
                using (var db = new XHTD_Entities())
                {
                    var callVehicleItem = new tblCallVehicleStatu();
                    callVehicleItem = GetVehicleToCall();
                    if (callVehicleItem == null || callVehicleItem.Id < 1) return;

                    // check xem trong bảng tblStoreOrderOperating xem đơn hàng có đang yêu cầu gọi vào không
                    var storeOrderOperating = db.tblStoreOrderOperatings.FirstOrDefault(x => x.Id == callVehicleItem.StoreOrderOperatingId);
                    if (storeOrderOperating == null) return;

                    _gatewayCallLogger.LogInfo($@"======== Phương tiện {storeOrderOperating.Vehicle} - Đơn hàng {storeOrderOperating.DeliveryCode} - Loại {storeOrderOperating.TypeProduct} đang trong hàng đợi =========");
                    if (storeOrderOperating.CountReindex != null && (int)storeOrderOperating.CountReindex >= 3)
                    {
                        _gatewayCallLogger.LogInfo($@"======== Phương tiện {storeOrderOperating.Vehicle} quá 3 lần xoay vòng gọi => Hủy lốt ========");
                    }
                    var vehicleWaitingCall = db.tblCallVehicleStatus.FirstOrDefault(x => x.Id == callVehicleItem.Id);
                    if (vehicleWaitingCall == null) return;

                    if (storeOrderOperating == null)
                    {
                        _gatewayCallLogger.LogInfo($"Không tìm thấy đơn hàng => Kết thúc");
                        return;
                    }

                    if (storeOrderOperating.Step != (int)OrderStep.DA_XAC_THUC &&
                        storeOrderOperating.Step != (int)OrderStep.CHO_GOI_XE &&
                        storeOrderOperating.Step != (int)OrderStep.DANG_GOI_XE)
                    {
                        _gatewayCallLogger.LogInfo($"======== Phương tiện {storeOrderOperating.Vehicle} - đơn hàng {storeOrderOperating.DeliveryCode} đã vào cổng ========");

                        vehicleWaitingCall.ModifiledOn = DateTime.Now;
                        vehicleWaitingCall.IsDone = true;
                        await db.SaveChangesAsync();
                    }
                    else
                    {
                        _gatewayCallLogger.LogInfo($"======== Gọi phương tiện {storeOrderOperating.Vehicle} - đơn hàng {storeOrderOperating.DeliveryCode} lần thứ {vehicleWaitingCall.CountTry + 1} ========");

                        isWillCall = true;
                        type = callVehicleItem.CallType;
                        vehiceCode = storeOrderOperating.Vehicle;
                        
                        if (type == CONG)
                        {
                            storeOrderOperating.LogProcessOrder = storeOrderOperating.LogProcessOrder + $@" #Gọi xe vào cổng lúc {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}";
                            storeOrderOperating.Step = (int)OrderStep.DANG_GOI_XE;
                            storeOrderOperating.TimeConfirm4 = DateTime.Now;
                        }
                        else
                        {
                            storeOrderOperating.LogProcessOrder = storeOrderOperating.LogProcessOrder + $@" #Gọi xe vào bãi chờ lúc {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}";
                        }

                        vehicleWaitingCall.ModifiledOn = DateTime.Now;
                        vehicleWaitingCall.CountTry = vehicleWaitingCall.CountTry + 1;
                        vehicleWaitingCall.LogCall = $@"{vehicleWaitingCall.LogCall} # Gọi xe {vehiceCode} vào lúc {DateTime.Now}";

                        if (type == BAI_CHO && vehicleWaitingCall.CountTry == 3)
                        {
                            vehicleWaitingCall.IsDone = true;
                        }

                        await db.SaveChangesAsync();
                    }
                }

                if (isWillCall)
                {
                    // tiến hành gọi xe
                    if (type == CONG)
                    {
                        CallInGatewayBySystem(vehiceCode);
                    }

                    else if (type == BAI_CHO)
                    {
                        CallInYardBySystem(vehiceCode);
                    }
                }
            }
            catch (Exception ex)
            {
                _gatewayCallLogger.LogError(ex.Message);
            }
        }

        public tblCallVehicleStatu GetVehicleToCall()
        {
            var callVehicleItem = new tblCallVehicleStatu();
            try
            {
                using (var db = new XHTD_Entities())
                {
                    // Gọi xe được thêm vào hàng đợi thủ công vào trước
                    callVehicleItem = db.tblCallVehicleStatus.Where(x => x.IsDone == false && x.CountTry < 3 && x.TypeProduct.ToUpper() == "MANUAL").OrderBy(x => x.Id).FirstOrDefault();
                    if (callVehicleItem != null && callVehicleItem.Id > 0) return callVehicleItem;

                    // Mời xe ra bãi chờ trước
                    callVehicleItem = db.tblCallVehicleStatus.Where(x => x.IsDone == false && x.CountTry < 3 && x.CallType.ToUpper() == CallType.BAI_CHO).OrderBy(x => x.Id).FirstOrDefault();
                    if (callVehicleItem != null && callVehicleItem.Id > 0) return callVehicleItem;

                    for (int i = 0; i < 10; i++)
                    {
                        // Tự động
                        var typeCurrent = Program.roundRobinList.Next();
                        callVehicleItem = db.tblCallVehicleStatus.Where(x => x.IsDone == false && x.CountTry < 3 && x.TypeProduct.Equals(typeCurrent) && (x.CallType.ToUpper() == CallType.CONG || string.IsNullOrEmpty(x.CallType))).OrderBy(x => x.Id).FirstOrDefault();
                        if (callVehicleItem != null && callVehicleItem.Id > 0) return callVehicleItem;
                    }
                }
            }
            catch (Exception ex)
            {
                _gatewayCallLogger.LogError($"Có lỗi khi lấy dữ liệu từ bảng tblCallVehicleStatus: {ex.Message}");
            }
            return callVehicleItem;
        }

        public void CallInGatewayBySystem(string vehicle)
        {
            try
            {
                var PathAudioLib = $@"D:/ThuVienGoiLoa/AudioNormal";
                string VoiceFileStart = $@"{PathAudioLib}/audio_generer/VicemBegin.wav";
                string VoiceFileInvite = $@"{PathAudioLib}/audio_generer/moixe.wav";
                string VoiceFileInOut = $@"{PathAudioLib}/audio_generer/vaonhanhang.wav";
                string VoiceFileEnd = $@"{PathAudioLib}/audio_generer/VicemEnd.wav";
                WMPLib.WindowsMediaPlayer wplayer = new WMPLib.WindowsMediaPlayer();

                wplayer.URL = VoiceFileInvite;
                wplayer.settings.volume = 100;
                wplayer.controls.play();
                Thread.Sleep(1200);
                var count = 0;
                foreach (char c in vehicle)
                {
                    count++;
                    wplayer.URL = $@"{PathAudioLib}/{c}.wav";
                    wplayer.settings.volume = 100;
                    wplayer.controls.play();
                    if (count < 3)
                    {
                        Thread.Sleep(700);
                    }
                    else if (count == 3)
                    {
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        Thread.Sleep(600);
                    }
                }

                wplayer.URL = VoiceFileInOut;
                wplayer.settings.volume = 100;
                wplayer.controls.play();
            }
            catch (Exception ex)
            {
                _gatewayCallLogger.LogError(ex.Message);
            }
        }

        public void CallInYardBySystem(string vehicle)
        {
            try
            {
                var PathAudioLib = $@"D:/ThuVienGoiLoa/AudioNormal";
                string VoiceFileStart = $@"{PathAudioLib}/audio_generer/chuadenluot.wav";
                string VoiceFileInvite = $@"{PathAudioLib}/audio_generer/moixe.wav";
                string VoiceFileInOut = $@"{PathAudioLib}/audio_generer/vaobaicho.wav";
                string VoiceFileEnd = $@"{PathAudioLib}/audio_generer/VicemEnd.wav";
                WMPLib.WindowsMediaPlayer wplayer = new WMPLib.WindowsMediaPlayer();

                wplayer.URL = VoiceFileStart;
                wplayer.settings.volume = 100;
                wplayer.controls.play();
                Thread.Sleep(1500);

                wplayer.URL = VoiceFileInvite;
                wplayer.settings.volume = 100;
                wplayer.controls.play();
                Thread.Sleep(1200);
                var count = 0;
                foreach (char c in vehicle)
                {
                    count++;
                    wplayer.URL = $@"{PathAudioLib}/{c}.wav";
                    wplayer.settings.volume = 100;
                    wplayer.controls.play();
                    if (count < 3)
                    {
                        Thread.Sleep(700);
                    }
                    else if (count == 3)
                    {
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        Thread.Sleep(600);
                    }
                }

                wplayer.URL = VoiceFileInOut;
                wplayer.settings.volume = 100;
                wplayer.controls.play();

                Thread.Sleep(3000);
            }
            catch (Exception ex)
            {
                _gatewayCallLogger.LogError(ex.Message);
            }
        }
    }
}
