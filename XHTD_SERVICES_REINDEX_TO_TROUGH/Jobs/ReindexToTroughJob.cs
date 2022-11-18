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

namespace XHTD_SERVICES_REINDEX_TO_TROUGH.Jobs
{
    public class ReindexToTroughJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly VehicleRepository _vehicleRepository;

        protected readonly TroughRepository _troughRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly Notification _notification;

        protected readonly ReindexToTroughLogger _reindexToTroughLogger;

        const int MAX_ORDER_IN_QUEUE_TO_CALL = 2;

        public ReindexToTroughJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            VehicleRepository vehicleRepository,
            TroughRepository troughRepository,
            CallToTroughRepository callToTroughRepository,
            Notification notification,
            ReindexToTroughLogger reindexToTroughLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _vehicleRepository = vehicleRepository;
            _troughRepository = troughRepository;
            _callToTroughRepository = callToTroughRepository;
            _notification = notification;
            _reindexToTroughLogger = reindexToTroughLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(() =>
            {
                ReindexToTroughProcess();
            });
        }

        public async void ReindexToTroughProcess()
        {
            _reindexToTroughLogger.LogInfo("start process ReindexToTroughJob");

            var overCountTryItems = await _callToTroughRepository.GetItemsOverCountTry();

            if (overCountTryItems != null && overCountTryItems.Count > 0)
            {
                foreach (var item in overCountTryItems)
                {
                    var isOverTime = ((DateTime)item.UpdateDay).AddMinutes(5) > DateTime.Now;
                    if (isOverTime)
                    {
                        continue;
                    }

                    // cập nhật trạng thái isDone trong hàng đợi
                    await _callToTroughRepository.UpdateWhenOverCountTry(item.Id);

                    // xếp lại lốt của đơn hàng
                    var order = await _storeOrderOperatingRepository.GetDetail(item.OrderId);
                    if(order == null)
                    {
                        continue;
                    }

                    await _storeOrderOperatingRepository.ReindexToTrough(item.OrderId);
                }
            }
        }
    }
}
