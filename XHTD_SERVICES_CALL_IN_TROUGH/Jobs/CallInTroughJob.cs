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

        protected readonly MachineRepository _machineRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly CallInTroughLogger _callInTroughLogger;

        protected const string SERVICE_ACTIVE_CODE = "CALL_IN_TROUGH_ACTIVE";

        protected const string MAX_COUNT_TRY_CALL_CODE = "MAX_COUNT_TRY_CALL";

        protected const string MAX_COUNT_REINDEX_CODE = "MAX_COUNT_REINDEX";

        private static bool isActiveService = true;

        private static int maxCountTryCall = 3;

        private static int maxCountReindex = 3;

        public CallInTroughJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            MachineRepository machineRepository,
            CallToTroughRepository callToTroughRepository,
            SystemParameterRepository systemParameterRepository,
            CallInTroughLogger callInTroughLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _machineRepository = machineRepository;
            _callToTroughRepository = callToTroughRepository;
            _systemParameterRepository = systemParameterRepository;
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

                if (!isActiveService)
                {
                    _callInTroughLogger.LogInfo("Service tu dong goi xe vao mang dang TAT");
                    return;
                }

                CallInTroughProcess();
            });
        }

        public async Task LoadSystemParameters()
        {
            var parameters = await _systemParameterRepository.GetSystemParameters();

            var activeParameter = parameters.FirstOrDefault(x => x.Code == SERVICE_ACTIVE_CODE);
            var maxCountTryCallParameter = parameters.FirstOrDefault(x => x.Code == MAX_COUNT_TRY_CALL_CODE);
            var maxCountReindexParameter = parameters.FirstOrDefault(x => x.Code == MAX_COUNT_REINDEX_CODE);

            if (activeParameter == null || activeParameter.Value == "0")
            {
                isActiveService = false;
            }

            if (maxCountTryCallParameter != null)
            {
                maxCountTryCall = Convert.ToInt32(maxCountTryCallParameter.Value);
            }

            if (maxCountReindexParameter != null)
            {
                maxCountReindex = Convert.ToInt32(maxCountReindexParameter.Value);
            }
        }

        public async void CallInTroughProcess()
        {
            _callInTroughLogger.LogInfo("Start process CallInTrough service");

            var machines = await _machineRepository.GetAllMachineCodes();

            if (machines == null || machines.Count == 0)
            {
                return;
            }

            // Doc lan luot thong tin tren cac mang
            foreach (var machine in machines)
            {
                await CallInTrough(machine);
                Thread.Sleep(5000);
            }
        }

        public async Task CallInTrough(string machineCode)
        {
            _callInTroughLogger.LogInfo($"CallInTrough {machineCode}");

            var isWorkingMachine = await _machineRepository.IsWorkingMachine(machineCode);

            // Khong goi xe vao may dang xuat hang
            if (isWorkingMachine)
            {
                _callInTroughLogger.LogInfo($"May {machineCode} dang xuat hang. Ket thuc");
                return;
            }

            // Tìm đơn hàng sẽ được gọi
            var itemToCall = _callToTroughRepository.GetItemToCall(machineCode, maxCountTryCall);

            // Khong goi 1 xe qua 3 lan
            if (itemToCall == null)
            {
                _callInTroughLogger.LogInfo($"Khong co don hang nao dang cho trong mang {machineCode}");
                return;
            }

            if (itemToCall.CountTry >= maxCountTryCall
                || itemToCall.CountReindex >= maxCountReindex)
            {
                _callInTroughLogger.LogInfo($"Don hang {itemToCall.DeliveryCode} da qua so lan goi CountTry {itemToCall.CountTry} CountReindex {itemToCall.CountReindex}");
                return;
            }

            _callInTroughLogger.LogInfo($"Tien hanh goi loa deliveryCode {itemToCall.DeliveryCode} vehicle {itemToCall.Vehicle} vao mang {machineCode}");

            // Lấy thông tin đơn hàng
            var order = await _storeOrderOperatingRepository.GetDetail(itemToCall.DeliveryCode);

            if(order == null)
            {
                return;
            }

            if(order.Step != (int)OrderStep.DA_GIAO_HANG)
            {
                var vehiceCode = order.Vehicle;

                // update don hang
                var logProcess = $@"#Gọi xe vào lúc {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}";
                await _storeOrderOperatingRepository.UpdateLogProcess(order.DeliveryCode, logProcess);

                // update hang doi: CountTry + 1
                await _callToTroughRepository.UpdateWhenCall(itemToCall.Id, vehiceCode);

                // Thuc hien goi xe
                CallBySystem(vehiceCode, machineCode);
            }
        }

        public void CallBySystem(string vehicle, string troughCode)
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
            Thread.Sleep(1200);

            wplayer.URL = $@"{PathAudioLib}/M.wav"; ;
            wplayer.settings.volume = 100;
            wplayer.controls.play();
            Thread.Sleep(500);

            wplayer.URL = $@"{PathAudioLib}/{troughCode}.wav"; ;
            wplayer.settings.volume = 100;
            wplayer.controls.play();
        }
    }
}
