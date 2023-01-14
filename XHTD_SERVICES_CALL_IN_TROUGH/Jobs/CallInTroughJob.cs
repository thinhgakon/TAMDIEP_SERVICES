using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES.Data.Models.Response;
using Newtonsoft.Json;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES.Helper.Models.Request;
using XHTD_SERVICES.Data.Models.Values;
using System.Threading;
using WMPLib;

namespace XHTD_SERVICES_CALL_IN_TROUGH.Jobs
{
    public class CallInTroughJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly VehicleRepository _vehicleRepository;

        protected readonly TroughRepository _troughRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly Notification _notification;

        protected readonly CallInTroughLogger _callInTroughLogger;

        protected const string MAX_COUNT_TRY_CALL = "MAX_COUNT_TRY_CALL";

        private static int maxCountTryCall = 3;

        public CallInTroughJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            VehicleRepository vehicleRepository,
            TroughRepository troughRepository,
            CallToTroughRepository callToTroughRepository,
            SystemParameterRepository systemParameterRepository,
            Notification notification,
            CallInTroughLogger callInTroughLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _vehicleRepository = vehicleRepository;
            _troughRepository = troughRepository;
            _callToTroughRepository = callToTroughRepository;
            _systemParameterRepository = systemParameterRepository;
            _notification = notification;
            _callInTroughLogger = callInTroughLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                // Get System Parameters
                await LoadSystemParameters();

                CallInTroughProcess();
            });
        }

        public async Task LoadSystemParameters()
        {
            var parameters = await _systemParameterRepository.GetSystemParameters();

            var maxCountTryCallParameter = parameters.FirstOrDefault(x => x.Code == MAX_COUNT_TRY_CALL);

            if (maxCountTryCallParameter != null)
            {
                maxCountTryCall = Convert.ToInt32(maxCountTryCallParameter.Value);
            }
        }

        public async void CallInTroughProcess()
        {
            _callInTroughLogger.LogInfo("start process CallInTroughJob");

            // TODO: Lay ra danh sach mang xuat xi mang bao dang hoat dong
            // Goi xe vao tung mang: tham khao service QueueToCall

            var troughts = await _troughRepository.GetAllTroughCodes();

            if (troughts == null || troughts.Count == 0)
            {
                return;
            }

            // Doc lan luot thong tin tren cac mang
            foreach (var trought in troughts)
            {
                await CallInTrough(trought);
                Thread.Sleep(5000);
            }
        }

        public async Task CallInTrough(string troughCode) 
        {
            _callInTroughLogger.LogInfo($"CallInTrough {troughCode}");

            // Tìm đơn hàng sẽ được gọi
            var itemToCall = _callToTroughRepository.GetItemToCall(troughCode, maxCountTryCall);

            if (itemToCall == null)
            {
                return;
            }

            // Lấy thông tin đơn hàng
            var order = await _storeOrderOperatingRepository.GetDetail(itemToCall.OrderId);

            if(order == null)
            {
                return;
            }

            if(order.Step == (int)OrderStep.DANG_GOI_XE)
            {
                var vehiceCode = order.Vehicle;

                // update don hang
                var logProcess = $@"#Gọi xe vào lúc {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}";
                await _storeOrderOperatingRepository.UpdateLogProcess(order.DeliveryCode, logProcess);

                // update hang doi: CountTry + 1
                await _callToTroughRepository.UpdateWhenCall(itemToCall.Id, vehiceCode);

                // Thuc hien goi xe
                CallBySystem(vehiceCode);
            }
        }

        public void CallBySystem(string vehicle)
        {
            var PathAudioLib = $@"D:/ThuVienGoiLoa/AudioNormal";

            string VoiceFileInvite = $@"{PathAudioLib}/audio_generer/moixe.wav";
            string VoiceFileInOut = $@"{PathAudioLib}/audio_generer/vaonhanhang.wav";
            
            WindowsMediaPlayer wplayer = new WindowsMediaPlayer();

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
        }
    }
}
