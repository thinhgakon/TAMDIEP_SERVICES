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
using XHTD_SERVICES.Data.Models.Values;

namespace XHTD_SERVICES_QUEUE_TO_CALL.Jobs
{
    public class QueueToCallJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly VehicleRepository _vehicleRepository;

        protected readonly TroughRepository _troughRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly Notification _notification;

        protected readonly QueueToCallLogger _queueToCallLogger;

        const int MAX_ORDER_IN_QUEUE_TO_CALL = 2;

        public QueueToCallJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            VehicleRepository vehicleRepository,
            TroughRepository troughRepository,
            CallToTroughRepository callToTroughRepository,
            Notification notification,
            QueueToCallLogger queueToCallLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _vehicleRepository = vehicleRepository;
            _troughRepository = troughRepository;
            _callToTroughRepository = callToTroughRepository;
            _notification = notification;
            _queueToCallLogger = queueToCallLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(() =>
            {
                QueueToCallProcess();
            });
        }

        public void QueueToCallProcess()
        {
            _queueToCallLogger.LogInfo("start process QueueToCallJob");

            SyncTrough("M1");
        }

        public async void SyncTrough(string troughCode)
        {
            _queueToCallLogger.LogInfo($"SyncTrough {troughCode}");

            // lay thong tin máng
            var troughInfo = _troughRepository.GetDetail(troughCode);
            if (troughInfo == null)
            { 
                return; 
            }

            var currentDeliveryCodeInTrough = troughInfo.DeliveryCodeCurrent;

            // Nếu hiện tại đang có đơn trong máng
            if (!String.IsNullOrEmpty(currentDeliveryCodeInTrough)) 
            {
                // update through line cho don hang
                await _storeOrderOperatingRepository.UpdateTroughLine(currentDeliveryCodeInTrough, troughCode);

                // update step theo % xuat hang cua don hang tai mang
                var isAlmostDone = (troughInfo.CountQuantityCurrent / troughInfo.PlanQuantityCurrent) > 0.8;
                if (isAlmostDone)
                {
                    await _storeOrderOperatingRepository.UpdateStepInTrough(currentDeliveryCodeInTrough, (int)OrderStep.DA_LAY_HANG);
                }
                else
                {
                    await _storeOrderOperatingRepository.UpdateStepInTrough(currentDeliveryCodeInTrough, (int)OrderStep.DANG_LAY_HANG);
                }
            }

            // Đếm số lượng đơn hàng đang chờ gọi của máng
            var numberOrderFrontTrough = _callToTroughRepository.GetNumberOrderInQueue(troughCode);

            if(numberOrderFrontTrough < MAX_ORDER_IN_QUEUE_TO_CALL)
            {
                // goi them xe vao hang doi
                PushOrderToQueue(troughCode, MAX_ORDER_IN_QUEUE_TO_CALL - numberOrderFrontTrough);
            }
        }

        public async void PushOrderToQueue(string troughcode, int quantity)
        {
            var orders = _storeOrderOperatingRepository.GetOrdersSortByIndex(quantity);
            if (orders == null || orders.Count == 0)
            {
                return;
            }

            foreach (var order in orders)
            {
                // Cap nhat trang thai don hang DANG_GOI_XE
                await _storeOrderOperatingRepository.UpdateStepDangGoiXe(order.DeliveryCode);

                // Them ban ghi vao tblCallToTrough: danh sach cho goi xe
                await _callToTroughRepository.CreateAsync(order.Id, troughcode);
            }
        }
    }
}
