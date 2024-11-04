using System;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Quartz;
using XHTD_SERVICES.Data.Common;
using XHTD_SERVICES.Data.Repositories;

namespace XHTD_SERVICES_QUEUE_TO_TROUGH.Jobs
{
    public class QueueToTroughOtherJob : IJob
    {
        ILog _logger = LogManager.GetLogger("OtherFileAppender");

        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly TroughRepository _troughRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected const string SERVICE_ACTIVE_CODE = "AUTO_QUEUE_TO_TROUGH_ACTIVE";

        private static bool isActiveService = true;

        public QueueToTroughOtherJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            TroughRepository troughRepository,
            CallToTroughRepository callToTroughRepository,
            SystemParameterRepository systemParameterRepository
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _troughRepository = troughRepository;
            _callToTroughRepository = callToTroughRepository;
            _systemParameterRepository = systemParameterRepository;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                await LoadSystemParameters();

                if (!isActiveService)
                {
                    _logger.Info("Service tự động xếp xe vào máng OTHER đang TẮT");
                    return;
                }

                await QueueToCallProcess();
            });
        }

        public async Task LoadSystemParameters()
        {
            var parameters = await _systemParameterRepository.GetSystemParameters();

            var activeParameter = parameters.FirstOrDefault(x => x.Code == SERVICE_ACTIVE_CODE);

            if (activeParameter == null || activeParameter.Value == "0")
            {
                isActiveService = false;
            }
            else
            {
                isActiveService = true;
            }
        }

        public async Task QueueToCallProcess()
        {
            _logger.Info("Start process QueueToCall OTHER service");

            try
            {
                // 1. Lay danh sach don hang chua duoc xep vao may xuat
                var orders = await _storeOrderOperatingRepository.GetTypeOtherOrdersAddToQueueToCall();
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

                    var machineCode = await _troughRepository.GetMinQuantityTrough(OrderTypeProductCode.OTHER, OrderProductCategoryCode.XI_BAO);

                    _logger.Info($"Thuc hien them orderId {orderId} deliveryCode {deliveryCode} vao may {machineCode}");

                    if (!String.IsNullOrEmpty(machineCode) && machineCode != "0")
                    {
                        await _callToTroughRepository.AddItem(orderId, deliveryCode, vehicle, machineCode, sumNumber);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Info($"Errrrorrr: {ex.Message} ==== {ex.StackTrace} ===== {ex.InnerException}");
            }
        }
    }
}
