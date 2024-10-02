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

namespace XHTD_SERVICES_REINDEX_TO_GATEWAY.Jobs
{
    public class ReindexToGatewayJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly ReindexToTroughLogger _reindexToTroughLogger;

        protected const string SERVICE_ACTIVE_CODE = "REINDEX_TO_TROUGH_ACTIVE";

        protected const string MAX_COUNT_TRY_CALL_CODE = "MAX_COUNT_TRY_CALL";

        protected const string MAX_COUNT_REINDEX_CODE = "MAX_COUNT_REINDEX";

        protected const string OVER_TIME_TO_REINDEX_CODE = "OVER_TIME_TO_REINDEX";

        private static bool isActiveService = true;

        private static int maxCountTryCall = 3;

        private static int maxCountReindex = 3;

        private static int overTimeToReindex = 5;

        public ReindexToGatewayJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            CallToTroughRepository callToTroughRepository,
            SystemParameterRepository systemParameterRepository,
            ReindexToTroughLogger reindexToTroughLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _callToTroughRepository = callToTroughRepository;
            _systemParameterRepository = systemParameterRepository;
            _reindexToTroughLogger = reindexToTroughLogger;
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
                    _reindexToTroughLogger.LogInfo("Service tu dong quay vong lot xe vao mang dang TAT");
                    return;
                }

                ReindexToTroughProcess();
            });
        }

        public async Task LoadSystemParameters()
        {
            var parameters = await _systemParameterRepository.GetSystemParameters();

            var activeParameter = parameters.FirstOrDefault(x => x.Code == SERVICE_ACTIVE_CODE);
            var maxCountTryCallParameter = parameters.FirstOrDefault(x => x.Code == MAX_COUNT_TRY_CALL_CODE);
            var maxCountReindexParameter = parameters.FirstOrDefault(x => x.Code == MAX_COUNT_REINDEX_CODE);
            var overTimeToReindexParameter = parameters.FirstOrDefault(x => x.Code == OVER_TIME_TO_REINDEX_CODE);

            if (activeParameter == null || activeParameter.Value == "0")
            {
                isActiveService = false;
            }
            else
            {
                isActiveService = true;
            }

            if (maxCountTryCallParameter != null)
            {
                maxCountTryCall = Convert.ToInt32(maxCountTryCallParameter.Value);
            }

            if (maxCountReindexParameter != null)
            {
                maxCountReindex = Convert.ToInt32(maxCountReindexParameter.Value);
            }

            if (overTimeToReindexParameter != null)
            {
                overTimeToReindex = Convert.ToInt32(overTimeToReindexParameter.Value);
            }
        }

        public async void ReindexToTroughProcess()
        {
            _reindexToTroughLogger.LogInfo("Start process ReindexToTrough service");

            // Xử lý các order đã quá 3 lần gọi loa mà ko vào máng
            var overCountTryItems = await _callToTroughRepository.GetItemsOverCountTry(maxCountTryCall);
            if (overCountTryItems != null && overCountTryItems.Count > 0)
            {
                foreach (var item in overCountTryItems)
                {
                    var isOverTime = ((DateTime)item.UpdateDay).AddMinutes(overTimeToReindex) > DateTime.Now;
                    if (isOverTime)
                    {
                        continue;
                    }

                    // cập nhật trạng thái isDone trong hàng đợi
                    await _callToTroughRepository.UpdateWhenOverCountTry(item.Id);
                }
            }

            // Xử lý các order quá 3 lần xoay vòng lốt mà ko vào máng
            //var overCountReindexItems = await _callToTroughRepository.GetItemsOverCountReindex(maxCountReindex);
            //if (overCountReindexItems != null && overCountReindexItems.Count > 0)
            //{
            //    foreach (var item in overCountReindexItems)
            //    {
            //        var isOverTime = ((DateTime)item.UpdateDay).AddMinutes(overTimeToReindex) > DateTime.Now;
            //        if (isOverTime)
            //        {
            //            continue;
            //        }

            //        // cập nhật trạng thái isDone trong hàng đợi
            //        await _callToTroughRepository.UpdateWhenOverCountReindex(item.Id);

            //        await _storeOrderOperatingRepository.UpdateWhenOverCountReindex(item.OrderId);
            //    }
            //}
        }
    }
}
