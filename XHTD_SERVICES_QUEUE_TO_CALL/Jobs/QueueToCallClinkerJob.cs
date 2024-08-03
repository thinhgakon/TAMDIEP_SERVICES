using System;
using System.Threading.Tasks;
using Quartz;
using XHTD_SERVICES.Data.Repositories;

namespace XHTD_SERVICES_QUEUE_TO_CALL.Jobs
{
    public class QueueToCallClinkerJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly TroughRepository _troughRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly QueueToCallLogger _queueToCallLogger;

        public QueueToCallClinkerJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            TroughRepository troughRepository,
            CallToTroughRepository callToTroughRepository,
            QueueToCallLogger queueToCallLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _troughRepository = troughRepository;
            _callToTroughRepository = callToTroughRepository;
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
            _queueToCallLogger.LogInfo("Start process QueueToCall CLINKER service");

            try
            {
                // 1. Lay danh sach don hang chua duoc xep vao may xuat
                var orders = await _storeOrderOperatingRepository.GetClinkerOrdersAddToQueueToCall();
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
                    var orderId = (int)order.OrderId;
                    var deliveryCode = order.DeliveryCode;
                    var vehicle = order.Vehicle;
                    var sumNumber = (decimal)order.SumNumber;
                    var typeProduct = order.TypeProduct;

                    var machineCode = await _troughRepository.GetMinQuantityMachine(typeProduct);

                    _queueToCallLogger.LogInfo($"Thuc hien them orderId {orderId} deliveryCode {deliveryCode} vao may {machineCode}");

                    if (!String.IsNullOrEmpty(machineCode) && machineCode != "0")
                    {
                        await _callToTroughRepository.AddItem(orderId, deliveryCode, vehicle, machineCode, sumNumber);
                    }
                }
            }
            catch (Exception ex)
            {
                _queueToCallLogger.LogInfo($"Errrrorrr: {ex.Message} ==== {ex.StackTrace} ===== {ex.InnerException}");
            }
        }
    }
}
