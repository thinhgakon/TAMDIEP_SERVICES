﻿using System;
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
using System.Data.Entity;

namespace XHTD_SERVICES_CALL_IN_GATEWAY.Jobs
{
    [DisallowConcurrentExecution]
    public class GatewayCallJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

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
                var type = CallType.CONG;
                var vehiceCode = "";
                using (var db = new XHTD_Entities())
                {
                    var callVehicleItem = new tblCallVehicleStatu();
                    callVehicleItem = GetVehicleToCall();
                    if (callVehicleItem == null || callVehicleItem.Id < 1) return;

                    // Nếu callVehicleItem có CallType là CHUA_CO_DON hoặc CHUA_NHAN_DON
                    if (callVehicleItem.StoreOrderOperatingId == null)
                    {
                        isWillCall = true;
                        type = callVehicleItem.CallType;
                        vehiceCode = callVehicleItem.Vehicle;

                        var vehicleWaitingCall = db.tblCallVehicleStatus.FirstOrDefault(x => x.Id == callVehicleItem.Id);
                        if (vehicleWaitingCall == null) return;

                        vehicleWaitingCall.ModifiledOn = DateTime.Now;
                        vehicleWaitingCall.CountTry = vehicleWaitingCall.CountTry + 1;
                        vehicleWaitingCall.LogCall = $@"{vehicleWaitingCall.LogCall} # Gọi xe {vehiceCode} vào lúc {DateTime.Now}";

                        if (vehicleWaitingCall.CountTry == 1)
                        {
                            vehicleWaitingCall.IsDone = true;
                        }

                        await db.SaveChangesAsync();
                    }

                    // check xem trong bảng tblStoreOrderOperating xem đơn hàng có đang yêu cầu gọi vào không
                    else
                    {
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

                        // Nếu trạm cân nhận thêm đơn, xác thực thủ công khi xe đang đứng ở cân thì không gọi loa
                        var isVehicleInFactory = await db.tblStoreOrderOperatings
                                                            .AnyAsync(x => x.Vehicle == storeOrderOperating.Vehicle 
                                                                        && x.IsVoiced == false 
                                                                        && (
                                                                            x.Step == (int)OrderStep.DA_VAO_CONG 
                                                                            ||
                                                                            x.Step == (int)OrderStep.DA_CAN_VAO 
                                                                            ||
                                                                            x.Step == (int)OrderStep.DANG_LAY_HANG 
                                                                            ||
                                                                            x.Step == (int)OrderStep.DA_LAY_HANG)
                                                                            );

                        if (isVehicleInFactory)
                        {
                            _gatewayCallLogger.LogInfo($"======== Phương tiện {storeOrderOperating.Vehicle} - đơn hàng {storeOrderOperating.DeliveryCode} có 1 đơn hàng đang ở trong nhà máy. Trường hợp nhận thêm lệnh thủ công => Kết thúc ========");

                            vehicleWaitingCall.ModifiledOn = DateTime.Now;
                            vehicleWaitingCall.IsDone = true;
                            vehicleWaitingCall.LogCall = $@"{vehicleWaitingCall.LogCall} # Hủy gọi xe khi có đơn trong nhà máy, lúc {DateTime.Now}";

                            await db.SaveChangesAsync();

                            return;
                        }

                        // Nếu đơn đã vào cổng
                        if ((callVehicleItem.CallType == CallType.CONG ||
                             callVehicleItem.CallType == "MANUAL") &&
                             storeOrderOperating.Step != (int)OrderStep.DA_XAC_THUC &&
                             storeOrderOperating.Step != (int)OrderStep.CHO_GOI_XE &&
                             storeOrderOperating.Step != (int)OrderStep.DANG_GOI_XE)
                        {
                            _gatewayCallLogger.LogInfo($"======== Phương tiện {storeOrderOperating.Vehicle} - đơn hàng {storeOrderOperating.DeliveryCode} đã vào cổng ========");

                            vehicleWaitingCall.ModifiledOn = DateTime.Now;
                            vehicleWaitingCall.IsDone = true;
                            await db.SaveChangesAsync();
                        }

                        // Nếu đơn bị xoay lốt
                        else if ((callVehicleItem.CallType == CallType.CONG ||
                                  callVehicleItem.CallType == "MANUAL") && 
                                  storeOrderOperating.Step == (int)OrderStep.DA_XAC_THUC)
                        {
                            _gatewayCallLogger.LogInfo($"======== Đơn hàng {storeOrderOperating.DeliveryCode} bị xoay lốt => Chưa đến lượt gọi, bỏ qua ========");
                            return;
                        }

                        // Gọi bình thường
                        else
                        {
                            _gatewayCallLogger.LogInfo($"======== Gọi phương tiện {storeOrderOperating.Vehicle} - đơn hàng {storeOrderOperating.DeliveryCode} lần thứ {vehicleWaitingCall.CountTry + 1} ========");

                            isWillCall = true;
                            type = callVehicleItem.CallType;
                            vehiceCode = storeOrderOperating.Vehicle;

                            if (type == CallType.CONG)
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

                            if (type == CallType.BAI_CHO && vehicleWaitingCall.CountTry == 3)
                            {
                                vehicleWaitingCall.IsDone = true;
                            }

                            await db.SaveChangesAsync();
                        }
                    }
                }

                if (isWillCall)
                {
                    // tiến hành gọi xe
                    switch (type)
                    {
                        case CallType.CONG:
                            CallInGatewayBySystem(vehiceCode);
                            break;

                        case CallType.BAI_CHO:
                            CallInYardBySystem(vehiceCode);
                            break;

                        case CallType.CHUA_CO_DON:
                            OrderNotExistBySystem(vehiceCode);
                            break;

                        case CallType.CHUA_NHAN_DON:
                            OrderNotReceiveBySystem(vehiceCode);
                            break;
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

                    // Xe chưa có đơn
                    callVehicleItem = db.tblCallVehicleStatus.Where(x => x.IsDone == false && x.CountTry < 1 && x.CallType.ToUpper() == CallType.CHUA_CO_DON).OrderBy(x => x.Id).FirstOrDefault();
                    if (callVehicleItem != null && callVehicleItem.Id > 0) return callVehicleItem;

                    // Lái xe chưa nhận đơn
                    callVehicleItem = db.tblCallVehicleStatus.Where(x => x.IsDone == false && x.CountTry < 1 && x.CallType.ToUpper() == CallType.CHUA_NHAN_DON).OrderBy(x => x.Id).FirstOrDefault();
                    if (callVehicleItem != null && callVehicleItem.Id > 0) return callVehicleItem;

                    for (int i = 0; i < 10; i++)
                    {
                        // Tự động
                        var typeCurrent = Program.roundRobinList.Next();
                        callVehicleItem = db.tblCallVehicleStatus.Where(x => x.IsDone == false && x.CountTry < 3 && x.TypeProduct.Equals(typeCurrent) && (x.CallType.ToUpper() == CallType.CONG || string.IsNullOrEmpty(x.CallType))).OrderByDescending(x => x.Id).FirstOrDefault();
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

        // gọi vào cổng
        public void CallInGatewayBySystem(string vehicle)
        {
            try
            {
                var PathAudioLib = $@"D:/ThuVienGoiLoa/AudioNormal";

                string VoiceFileDing = $@"{PathAudioLib}/audio_generer/ding.wav";
                string VoiceFileStart = $@"{PathAudioLib}/audio_generer/VicemBegin.wav";
                string VoiceFileInvite = $@"{PathAudioLib}/audio_generer/moixe.wav";
                string VoiceFileInOut = $@"{PathAudioLib}/audio_generer/vaonhanhang.wav";
                string VoiceFileEnd = $@"{PathAudioLib}/audio_generer/VicemEnd.wav";
                WMPLib.WindowsMediaPlayer wplayer = new WMPLib.WindowsMediaPlayer();

                wplayer.URL = VoiceFileDing;
                wplayer.settings.volume = 30;
                wplayer.controls.play();
                Thread.Sleep(1200);

                wplayer.URL = VoiceFileDing;
                wplayer.settings.volume = 70;
                wplayer.controls.play();
                Thread.Sleep(1200);

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
                        Thread.Sleep(1200);
                    }
                    else
                    {
                        Thread.Sleep(700);
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

        // gọi vào bãi chờ
        public void CallInYardBySystem(string vehicle)
        {
            try
            {
                var PathAudioLib = $@"D:/ThuVienGoiLoa/AudioNormal";

                string VoiceFileDing = $@"{PathAudioLib}/audio_generer/ding.wav";
                string VoiceFileStart = $@"{PathAudioLib}/audio_generer/chuadenluot.wav";
                string VoiceFileInvite = $@"{PathAudioLib}/audio_generer/moixe.wav";
                string VoiceFileInOut = $@"{PathAudioLib}/audio_generer/vaobaicho.wav";
                string VoiceFileEnd = $@"{PathAudioLib}/audio_generer/VicemEnd.wav";
                WMPLib.WindowsMediaPlayer wplayer = new WMPLib.WindowsMediaPlayer();

                wplayer.URL = VoiceFileDing;
                wplayer.settings.volume = 30;
                wplayer.controls.play();
                Thread.Sleep(1200);

                wplayer.URL = VoiceFileDing;
                wplayer.settings.volume = 70;
                wplayer.controls.play();
                Thread.Sleep(1200);

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
                        Thread.Sleep(1200);
                    }
                    else
                    {
                        Thread.Sleep(700);
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

        // chưa có đơn hàng
        public void OrderNotExistBySystem(string vehicle)
        {
            try
            {
                var PathAudioLib = $@"D:/ThuVienGoiLoa/AudioNormal";

                string VoiceFileDing = $@"{PathAudioLib}/audio_generer/ding.wav";
                string VoiceFileStart = $@"{PathAudioLib}/audio_generer/xe.wav";
                string VoiceFileInOut = $@"{PathAudioLib}/audio_generer/chuacodonhang.wav";
                WMPLib.WindowsMediaPlayer wplayer = new WMPLib.WindowsMediaPlayer();

                wplayer.URL = VoiceFileDing;
                wplayer.settings.volume = 30;
                wplayer.controls.play();
                Thread.Sleep(1200);

                wplayer.URL = VoiceFileDing;
                wplayer.settings.volume = 70;
                wplayer.controls.play();
                Thread.Sleep(1200);

                wplayer.URL = VoiceFileStart;
                wplayer.settings.volume = 100;
                wplayer.controls.play();
                Thread.Sleep(1500);

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
                        Thread.Sleep(1200);
                    }
                    else
                    {
                        Thread.Sleep(700);
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

        // chưa nhận đơn hàng
        public void OrderNotReceiveBySystem(string vehicle)
        {
            try
            {
                var PathAudioLib = $@"D:/ThuVienGoiLoa/AudioNormal";

                string VoiceFileDing = $@"{PathAudioLib}/audio_generer/ding.wav";
                string VoiceFileStart = $@"{PathAudioLib}/audio_generer/laixe.wav";
                string VoiceFileInOut = $@"{PathAudioLib}/audio_generer/chuanhandonhang.wav";
                WMPLib.WindowsMediaPlayer wplayer = new WMPLib.WindowsMediaPlayer();

                wplayer.URL = VoiceFileDing;
                wplayer.settings.volume = 30;
                wplayer.controls.play();
                Thread.Sleep(1200);

                wplayer.URL = VoiceFileDing;
                wplayer.settings.volume = 70;
                wplayer.controls.play();
                Thread.Sleep(1200);

                wplayer.URL = VoiceFileStart;
                wplayer.settings.volume = 100;
                wplayer.controls.play();
                Thread.Sleep(1500);

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
                        Thread.Sleep(1200);
                    }
                    else
                    {
                        Thread.Sleep(700);
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
