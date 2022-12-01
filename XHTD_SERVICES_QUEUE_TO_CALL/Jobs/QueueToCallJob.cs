using System;
using System.Threading.Tasks;
using Quartz;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES.Helper;
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

        const int MAX_ORDER_IN_QUEUE_TO_CALL = 1;

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
            _queueToCallLogger.LogInfo("------------------------------");
            _queueToCallLogger.LogInfo("Start process QueueToCallJob");
            _queueToCallLogger.LogInfo("------------------------------");

            ReadDataFromTrough("M1");
        }

        public async void ReadDataFromTrough(string troughCode)
        {
            _queueToCallLogger.LogInfo($"Read data from trough {troughCode}");

            var troughInfo = _troughRepository.GetDetail(troughCode);
            if (troughInfo == null)
            { 
                return; 
            }

            var currentDeliveryCodeInTrough = troughInfo.DeliveryCodeCurrent;

            // Cập nhật đơn hàng đang ở trong máng
            // troughLine
            // step DANG va DA_LAY_HANG phụ thuộc tình trạng xuất tại máng
            if (!String.IsNullOrEmpty(currentDeliveryCodeInTrough)) 
            {
                _queueToCallLogger.LogInfo($"1. Cập nhật đơn hàng đang ở trong máng {currentDeliveryCodeInTrough}");
                await _storeOrderOperatingRepository.UpdateTroughLine(currentDeliveryCodeInTrough, troughCode);

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
            else
            {
                _queueToCallLogger.LogInfo($"1. Không có đơn nào đang ở trong máng");
            }

            // Đếm số lượng đơn trong hàng chờ gọi của máng
            // Thêm đơn vào hàng chờ gọi
            var numberOrderFrontTrough = _callToTroughRepository.GetNumberOrderInQueue(troughCode);
            if(numberOrderFrontTrough < MAX_ORDER_IN_QUEUE_TO_CALL)
            {
                PushOrderToQueue(troughCode, MAX_ORDER_IN_QUEUE_TO_CALL - numberOrderFrontTrough);
            }
        }

        public async void PushOrderToQueue(string troughcode, int quantity)
        {
            _queueToCallLogger.LogInfo($"2. Đang còn {quantity} chỗ trống trong hàng chờ gọi của máng {troughcode}");

            var orders = await _storeOrderOperatingRepository.GetOrdersToCallInTrough(troughcode, quantity);

            if (orders == null || orders.Count == 0)
            {
                _queueToCallLogger.LogInfo($"2.1. Không còn đơn vừa cân vào để thêm vào hàng chờ gọi");

                return;
            }

            _queueToCallLogger.LogInfo($"2.1. Có {orders.Count} đơn sẽ được thêm vào hàng chờ");

            foreach (var order in orders)
            {
                _queueToCallLogger.LogInfo($"2.1.*. Tiến hành thêm {order.Id} với code {order.DeliveryCode}");

                // Cap nhat trang thai don hang DANG_GOI_XE
                await _storeOrderOperatingRepository.UpdateStepDangGoiXe(order.DeliveryCode);

                // Them ban ghi vao tblCallToTrough: danh sach cho goi xe
                await _callToTroughRepository.CreateAsync(order.Id, troughcode);
            }
        }
    }
}
