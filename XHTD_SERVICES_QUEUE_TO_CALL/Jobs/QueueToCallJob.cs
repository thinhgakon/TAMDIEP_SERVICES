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

        public async void QueueToCallProcess()
        {
            _queueToCallLogger.LogInfo("Start process QueueToCallJob");

            // 1. Lay danh sach don hang chua duoc xep vao may xuat
            var orders = await _storeOrderOperatingRepository.GetOrdersAddToQueueToCall();
            if (orders == null || orders.Count == 0)
            {
                return;
            }

            // 2. Voi moi don hang o B1 thi thuc hien
            // 3. Tim may xuat hien tai co it khoi luong don nhat (tuong ung voi type product)
            // 4. Tim STT lon nhat trong may tim duoc o B3: maxIndex
            // 5. Them don hang vao may o B3 voi index = maxIndex + 1
            foreach (var order in orders)
            {
                var orderId = order.Id;
                var deliveryCode = order.DeliveryCode;
                var vehicle = order.Vehicle;
                var sumNumber = (decimal)order.SumNumber;
                var typeProduct = order.TypeProduct;

                var machineCode = await _troughRepository.GetMinQuantityMachine(typeProduct);

                if (!String.IsNullOrEmpty(machineCode)){ 
                    await _callToTroughRepository.AddItem(orderId, deliveryCode, vehicle, machineCode, sumNumber);
                }
            }
        }
    }
}
