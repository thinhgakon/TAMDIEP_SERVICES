using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Quartz;
using XHTD_SERVICES.Data.Common;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Models.Values;
using XHTD_SERVICES.Data.Repositories;

namespace XHTD_SERVICES_QUEUE_TO_TROUGH.Jobs
{
    public class QueueToTroughXibaoJob : IJob
    {
        ILog _logger = LogManager.GetLogger("FileAppender");

        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly TroughRepository _troughRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected const string SERVICE_ACTIVE_CODE = "AUTO_QUEUE_TO_TROUGH_ACTIVE";

        private static bool isActiveService = true;

        public QueueToTroughXibaoJob(
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
                    _logger.Info("Service tự động xếp xe vào máng XI BAO đang TẮT");
                    return;
                }

                await QueueToCallProcess();
            });
        }

        public async Task LoadSystemParameters()
        {
            var parameters = await _systemParameterRepository.GetSystemParameters();

            var activeParameter = parameters.FirstOrDefault(x => x.Code.ToUpper().Trim() == SERVICE_ACTIVE_CODE.ToUpper().Trim());

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
            _logger.Info("Start process QueueToCall XI BAO service");
            
            try { 
                // 1. Lay danh sach don hang chua duoc xep vao may xuat
                var orders = await _storeOrderOperatingRepository.GetXiMangBaoOrdersAddToQueueToCall();
                if (orders == null || orders.Count == 0)
                {
                    return;
                }

                // 2. Voi moi don hang o B1 thi thuc hien
                // 3. Tim may xuat hien tai co it khoi luong don nhat (tuong ung voi type product)
                // 4. Tim STT lon nhat trong may tim duoc o B3: maxIndex
                // 5. Them don hang vao may o B3 voi index = maxIndex + 1
                using (var dbContext = new XHTD_Entities())
                {
                    for (int i = 0; i < 10; i++)
                    {
                        // Tự động
                        var typeProduct = Program.roundRobinList.Next();
                        var typeProductOrders = orders.Where(x => x.TypeProduct.ToUpper() == typeProduct.ToUpper());

                        foreach (var order in typeProductOrders)
                        {
                            var orderId = (int)order.OrderId;
                            var deliveryCode = order.DeliveryCode;
                            var vehicle = order.Vehicle;
                            var sumNumber = (decimal)order.SumNumber;

                            var splitOrders = await dbContext.tblStoreOrderOperatings.Where(x => x.IDDistributorSyn == order.IDDistributorSyn &&
                                                                                                 x.ItemId == order.ItemId &&
                                                                                                 x.Vehicle == order.Vehicle &&
                                                                                                 x.Step == (int)OrderStep.DA_CAN_VAO &&
                                                                                                 x.IsVoiced == false)
                                                                                     .ToListAsync();

                            var machineCode = await _troughRepository.GetMinQuantityTrough(typeProduct, OrderProductCategoryCode.XI_BAO);

                            if (!String.IsNullOrEmpty(machineCode) && machineCode != "0")
                            {
                                var existedTrough = await dbContext.tblCallToTroughs.AnyAsync(x => x.Vehicle == order.Vehicle &&
                                                                                                   x.Machine != machineCode &&
                                                                                                   x.IsDone == false);

                                if (!existedTrough)
                                {
                                    _logger.Info($"Thuc hien them orderId {orderId} deliveryCode {deliveryCode} vao may {machineCode}");
                                    await _callToTroughRepository.AddItem(orderId, deliveryCode, vehicle, machineCode, sumNumber);
                                    order.Step = (int)OrderStep.DANG_LAY_HANG;
                                    order.TimeConfirm5 = DateTime.Now;
                                    order.LogProcessOrder += $"#Xe được tự động xếp vào máng lúc {DateTime.Now}.";

                                    if (splitOrders != null && splitOrders.Count > 0)
                                    {
                                        foreach (var splitOrder in splitOrders)
                                        {
                                            _logger.Info($"Thuc hien them orderId {splitOrder.OrderId} deliveryCode {splitOrder.DeliveryCode} vao may {machineCode}");
                                            await _callToTroughRepository.AddItem((int)splitOrder.OrderId, splitOrder.DeliveryCode, splitOrder.Vehicle, machineCode, (decimal)splitOrder.SumNumber);
                                            splitOrder.Step = (int)OrderStep.DANG_LAY_HANG;
                                            splitOrder.TimeConfirm5 = DateTime.Now;
                                            splitOrder.LogProcessOrder += $"#Xe được tự động xếp vào máng lúc {DateTime.Now}.";
                                        }
                                    }

                                    await dbContext.SaveChangesAsync();
                                }
                            }
                        }
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
