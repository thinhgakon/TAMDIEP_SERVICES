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

namespace XHTD_SERVICES_CALL_IN_GATEWAY.Jobs
{
    public class GatewayCallJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly GatewayCallLogger _gatewayCallLogger;

        string PathAudio = $@"D:/VICEM_TAMDIEP/xuathangtudong/AudioNew";

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
                CallVehicleProcess();
            });
        }

        public void CallVehicleProcess()
        {
            _gatewayCallLogger.LogInfo("==============start process CallVehicleProcess ====================");

            try
            {
                var isWillCall = false;
                var vehiceCode = "";
                using (var db = new XHTD_Entities())
                {
                    var callVehicleItem = new tblCallVehicleStatu();
                    var typeCurrent = Program.roundRobinList.Next();

                    callVehicleItem = GetVehicleToCall();
                    if (callVehicleItem == null || callVehicleItem.Id < 1) return;
                    // check xem trong bảng tblStoreOrderOperating xem đơn hàng có đang yêu cầu gọi vào không
                    var storeOrderOperating = db.tblStoreOrderOperatings.FirstOrDefault(x => x.Id == callVehicleItem.StoreOrderOperatingId);
                    _gatewayCallLogger.LogError($@"======1==call vehicle {storeOrderOperating.Vehicle} ==============");
                    if (storeOrderOperating.CountReindex != null && (int)storeOrderOperating.CountReindex >= 3)
                    {
                        _gatewayCallLogger.LogError($@"======2==call vehicle {storeOrderOperating.Vehicle} quá 3 lần xoay vòng gọi -- hủy lốt==============");
                        if (storeOrderOperating.Step == 1 || storeOrderOperating.Step == 4)
                        {
                            var sql = $@"UPDATE dbo.tblStoreOrderOperating SET IndexOrder = 0, Confirm1 = 0, TimeConfirm1 = NULL, Step = 0, IndexOrder2 = 0, DeliveryCodeParent = NULL, LogProcessOrder = CONCAT(LogProcessOrder, N'#Quá 3 lần xoay vòng lốt mà xe không vào, hủy lốt lúc ', FORMAT(getdate(), 'dd/MM/yyyy HH:mm:ss')) WHERE  Step IN (1,4) AND ISNULL(DriverUserName,'') <> '' AND (DeliveryCode = @DeliveryCode OR DeliveryCodeParent = @DeliveryCode)";
                            db.Database.ExecuteSqlCommand(sql, new SqlParameter("@DeliveryCode", storeOrderOperating.DeliveryCode));
                            var sqlDelete = $@"UPDATE dbo.tblCallVehicleStatus SET IsDone = 1 WHERE StoreOrderOperatingId = @StoreOrderOperatingId";
                            db.Database.ExecuteSqlCommand(sql, new SqlParameter("@StoreOrderOperatingId", storeOrderOperating.Id));
                            return;
                        }
                    }
                    var vehicleWaitingCall = db.tblCallVehicleStatus.FirstOrDefault(x => x.Id == callVehicleItem.Id);
                    if (vehicleWaitingCall == null) return;
                    if (storeOrderOperating == null || storeOrderOperating.Step != 4)
                    {
                        vehicleWaitingCall.ModifiledOn = DateTime.Now;
                        vehicleWaitingCall.IsDone = true;
                        db.SaveChanges();
                    }
                    else
                    {
                        storeOrderOperating.LogProcessOrder = storeOrderOperating.LogProcessOrder + $@" #Gọi xe vào lúc {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}";
                        isWillCall = true;
                        vehiceCode = storeOrderOperating.Vehicle;
                        vehicleWaitingCall.ModifiledOn = DateTime.Now;
                        vehicleWaitingCall.CountTry = vehicleWaitingCall.CountTry + 1;
                        vehicleWaitingCall.LogCall = $@"{vehicleWaitingCall.LogCall} # Gọi xe {vehiceCode} vào lúc {DateTime.Now}";
                        db.SaveChanges();
                    }
                }
                if (isWillCall)
                {
                    // tiến hành gọi xe
                    // CallVoiceVehicle(vehiceCode);
                    CallBySystem(vehiceCode);
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
                    for (int i = 0; i < 10; i++)
                    {
                        var typeCurrent = Program.roundRobinList.Next();
                        callVehicleItem = db.tblCallVehicleStatus.Where(x => x.IsDone == false && x.CountTry < 3 && x.TypeProduct.Equals(typeCurrent)).OrderBy(x => x.Id).FirstOrDefault();
                        if (callVehicleItem != null && callVehicleItem.Id > 0) return callVehicleItem;
                    }
                }
            }
            catch (Exception ex)
            {
                _gatewayCallLogger.LogError(ex.Message);
            }
            return callVehicleItem;
        }

        public void CallBySystem(string vehicle)
        {
            try
            {
                var PathAudioLib = $@"D:/ThuVienGoiLoa/AudioNormal";
                // var PathAudioLib = $@"./AudioNormal";
                string VoiceFileStart = $@"{PathAudioLib}/audio_generer/VicemBegin.wav";
                string VoiceFileInvite = $@"{PathAudioLib}/audio_generer/moixe.wav";
                string VoiceFileInOut = $@"{PathAudioLib}/audio_generer/vaonhanhang.wav";
                string VoiceFileEnd = $@"{PathAudioLib}/audio_generer/VicemEnd.wav";
                WMPLib.WindowsMediaPlayer wplayer = new WMPLib.WindowsMediaPlayer();
                //wplayer.URL = VoiceFileStart;
                //wplayer.settings.volume = 100;
                //wplayer.controls.play();
                //Thread.Sleep(5000);

                wplayer.URL = VoiceFileInvite;
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

                //Thread.Sleep(5000);

                //wplayer.URL = VoiceFileEnd;
                //wplayer.controls.play();

            }
            catch (Exception ex)
            {
                _gatewayCallLogger.LogError(ex.Message);
            }
        }
    }
}
