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

namespace XHTD_SERVICES_COMPLETE_ORDER.Jobs
{
    public class CompleteOrderJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;
        
        protected readonly CategoriesDevicesRepository _categoriesDevicesRepository;

        protected readonly Notification _notification;

        protected readonly CompleteOrderLogger _sampleLogger;

        const int OVER_TIME_TO_AUTO_COMPLETE = 5; // đơn vị phút

        public CompleteOrderJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            CategoriesDevicesRepository categoriesDevicesRepository,
            Notification notification,
            CompleteOrderLogger callInTroughLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _categoriesDevicesRepository = categoriesDevicesRepository;
            _notification = notification;
            _sampleLogger = callInTroughLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(() =>
            {
                CompleteOrderProcess();
            });
        }

        public async void CompleteOrderProcess()
        {
            _sampleLogger.LogInfo("start process CompleteOrderJob");

            var orders = await _storeOrderOperatingRepository.GetOrdersByStep((int)OrderStep.DA_CAN_RA);

            if (orders == null || orders.Count == 0)
            {
                return;
            }

            bool isChanged = false;

            foreach (var order in orders)
            {
                if (order.TimeConfirm7 == null)
                { 
                    continue; 
                }

                if (((DateTime)order.TimeConfirm7).AddHours(OVER_TIME_TO_AUTO_COMPLETE) < DateTime.Now) {
                    var isCompleted = await _storeOrderOperatingRepository.CompleteOrder(order.Id);

                    if (!isChanged) isChanged = isCompleted;
                }
            }

            if (isChanged)
            {
                _notification.SendNotification("", null, 0, 0, null, 0, null, null, null);
            }
        }
    }
}
