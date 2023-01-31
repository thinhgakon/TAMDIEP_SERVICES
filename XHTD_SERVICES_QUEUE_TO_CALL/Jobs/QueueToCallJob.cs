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
            var orders = await _storeOrderOperatingRepository.GetOrdersXiMangBaoNoIndex();
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

                var troughCode = await _troughRepository.GetMinQuantityTrough(typeProduct);

                if (!String.IsNullOrEmpty(troughCode)){ 
                    await _callToTroughRepository.AddItem(orderId, deliveryCode, vehicle, troughCode, sumNumber);
                }
            }
        }

        public async Task ReadDataFromTrough(string troughCode)
        {
            _queueToCallLogger.LogInfo($"Read data from trough {troughCode}");

            var troughInfo = _troughRepository.GetDetail(troughCode);

            if((bool)troughInfo.Working)
            {
                _queueToCallLogger.LogInfo($"1. Mang {troughCode} dang xuat hang. Ket thuc");
                return;
            }

            // Đếm số lượng đơn trong hàng chờ gọi của máng
            // Thêm đơn vào hàng chờ gọi
            var numberOrderFrontTrough = _callToTroughRepository.GetNumberOrderInQueue(troughCode);

            _queueToCallLogger.LogInfo($"3. Co {numberOrderFrontTrough} don hang trong hang cho goi vao mang {troughCode}");

            if (numberOrderFrontTrough < MAX_ORDER_IN_QUEUE_TO_CALL)
            {
                await PushOrderToQueue(troughCode, MAX_ORDER_IN_QUEUE_TO_CALL - numberOrderFrontTrough);
            }
        }

        public async Task PushOrderToQueue(string troughcode, int quantity)
        {
            try { 
                _queueToCallLogger.LogInfo($"4. Them {quantity} don vao hang doi goi loa vao mang {troughcode}");

                var orders = await _storeOrderOperatingRepository.GetOrdersToCallInTrough(troughcode, quantity);

                if (orders == null || orders.Count == 0)
                {
                    _queueToCallLogger.LogInfo($"5. Ko con don vua can vao hop le de them vao hang cho goi. Ket thuc");

                    return;
                }

                _queueToCallLogger.LogInfo($"5. Co {orders.Count} don hang hop le de the vao hang doi");

                foreach (var order in orders)
                {
                    _queueToCallLogger.LogInfo($"5.1. Tien hanh them {order.Id} voi code {order.DeliveryCode}");

                    // Cap nhat trang thai don hang DANG_GOI_XE
                    await _storeOrderOperatingRepository.UpdateStepDangGoiXe(order.DeliveryCode);

                    // Them ban ghi vao tblCallToTrough: danh sach cho goi xe
                    await _callToTroughRepository.CreateAsync(order, troughcode);
                }
            }
            catch(Exception ex)
            {
                _queueToCallLogger.LogInfo($"Errr: {ex.StackTrace} {ex.Message}");
            }
        }
    }
}
