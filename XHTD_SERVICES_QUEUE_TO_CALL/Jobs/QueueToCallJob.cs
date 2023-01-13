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
            _queueToCallLogger.LogInfo("------------------------------");
            _queueToCallLogger.LogInfo("Start process QueueToCallJob");
            _queueToCallLogger.LogInfo("------------------------------");

            // Lay ra danh sach mang xuat xi mang bao dang hoat dong
            var troughts = await _troughRepository.GetAllTroughCodes();

            if (troughts == null || troughts.Count == 0)
            {
                return;
            }

            // Doc lan luot thong tin tren cac mang
            foreach (var trought in troughts)
            {
                await ReadDataFromTrough(trought);
                _queueToCallLogger.LogInfo("------------------------------");
            }
        }

        public async Task ReadDataFromTrough(string troughCode)
        {
            _queueToCallLogger.LogInfo($"Read data from trough {troughCode}");

            var troughInfo = _troughRepository.GetDetail(troughCode);
            if (troughInfo == null)
            {
                _queueToCallLogger.LogInfo($"1. Khong ton tai mang {troughCode}. Ket thuc");
                return; 
            }
            else if((bool)troughInfo.Working)
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
                    await _callToTroughRepository.CreateAsync(order.Id, troughcode);
                }
            }
            catch(Exception ex)
            {
                _queueToCallLogger.LogInfo($"Errr: {ex.StackTrace} {ex.Message}");
            }
        }
    }
}
