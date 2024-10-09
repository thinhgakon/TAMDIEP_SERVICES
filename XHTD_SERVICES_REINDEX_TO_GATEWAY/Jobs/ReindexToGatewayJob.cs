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
using XHTD_SERVICES.Data.Entities;
using System.Data.Entity;

namespace XHTD_SERVICES_REINDEX_TO_GATEWAY.Jobs
{
    public class ReindexToGatewayJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly CallToTroughRepository _callToTroughRepository;

        protected readonly SystemParameterRepository _systemParameterRepository;

        protected readonly ReindexToGatewayLogger _reindexToGatewayLogger;

        protected const string SERVICE_ACTIVE_CODE = "REINDEX_TO_TROUGH_ACTIVE";

        protected const string MAX_COUNT_TRY_CALL_CODE = "MAX_COUNT_TRY_CALL";

        protected const string MAX_COUNT_REINDEX_CODE = "MAX_COUNT_REINDEX";

        protected const string OVER_TIME_TO_REINDEX_CODE = "OVER_TIME_TO_REINDEX";

        private static bool isActiveService = true;

        private static int maxCountTryCall = 3;

        private static int maxCountReindex = 3;

        private static int overTimeToReindex = 5;

        public ReindexToGatewayJob(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            CallToTroughRepository callToTroughRepository,
            SystemParameterRepository systemParameterRepository,
            ReindexToGatewayLogger reindexToGatewayLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _callToTroughRepository = callToTroughRepository;
            _systemParameterRepository = systemParameterRepository;
            _reindexToGatewayLogger = reindexToGatewayLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                // Get System Parameters
                await LoadSystemParameters();

                if (!isActiveService)
                {
                    _reindexToGatewayLogger.LogInfo("Service tu dong quay vong lot xe vao mang dang TAT");
                    return;
                }

                ReindexToGatewayProcess();
            });
        }

        public async Task LoadSystemParameters()
        {
            var parameters = await _systemParameterRepository.GetSystemParameters();

            var activeParameter = parameters.FirstOrDefault(x => x.Code == SERVICE_ACTIVE_CODE);
            var maxCountTryCallParameter = parameters.FirstOrDefault(x => x.Code == MAX_COUNT_TRY_CALL_CODE);
            var maxCountReindexParameter = parameters.FirstOrDefault(x => x.Code == MAX_COUNT_REINDEX_CODE);
            var overTimeToReindexParameter = parameters.FirstOrDefault(x => x.Code == OVER_TIME_TO_REINDEX_CODE);

            if (activeParameter == null || activeParameter.Value == "0")
            {
                isActiveService = false;
            }
            else
            {
                isActiveService = true;
            }

            if (maxCountTryCallParameter != null)
            {
                maxCountTryCall = Convert.ToInt32(maxCountTryCallParameter.Value);
            }

            if (maxCountReindexParameter != null)
            {
                maxCountReindex = Convert.ToInt32(maxCountReindexParameter.Value);
            }

            if (overTimeToReindexParameter != null)
            {
                overTimeToReindex = Convert.ToInt32(overTimeToReindexParameter.Value);
            }
        }

        public async void ReindexToGatewayProcess()
        {
            _reindexToGatewayLogger.LogInfo("Start process ReindexToGateway service");

            // Xử lý các order đã quá 3 lần gọi loa mà ko vào cổng
            using (var db = new XHTD_Entities())
            {
                var last5Min = DateTime.Now.AddMinutes(-5);

                // Xếp lại số
                var callVehicleStatusReindex = await db.tblCallVehicleStatus
                                                       .Where(x => x.CountTry == 3 &&
                                                                   x.CountReindex < 3 &&
                                                                   x.ModifiledOn <= last5Min)
                                                       .ToListAsync();

                if (callVehicleStatusReindex == null || callVehicleStatusReindex.Count == 0)
                {
                    _reindexToGatewayLogger.LogInfo("Không có xe nào vượt quá 3 lần gọi => Bỏ qua");
                }

                else
                {
                    foreach (var callVehicleStatus in callVehicleStatusReindex)
                    {
                        callVehicleStatus.CountReindex++;
                        callVehicleStatus.CountTry = 0;
                    }
                    await db.SaveChangesAsync();
                }

                // Tăng số lần CountToCancel
                var callVehicleStatusRetry = await db.tblCallVehicleStatus
                                                     .Where(x => x.CountTry == 3 &&
                                                                 x.CountToCancel < 3 &&
                                                                 x.ModifiledOn <= last5Min)
                                                     .ToListAsync();

                if (callVehicleStatusRetry == null || callVehicleStatusRetry.Count == 0)
                {
                    _reindexToGatewayLogger.LogInfo("Không có xe nào vượt quá 3 lần gọi và vượt quá 3 lần đếm hủy => Bỏ qua");
                }

                else
                {
                    foreach (var callVehicleStatus in callVehicleStatusRetry)
                    {
                        callVehicleStatus.CountToCancel++;
                        callVehicleStatus.CountTry = 0;
                        callVehicleStatus.CountReindex = 0;
                    }
                    await db.SaveChangesAsync();
                }

                // Hủy xác thực các đơn hàng vượt quá số lần gọi
                var ordersToCancel = await (from orders in db.tblStoreOrderOperatings
                                            join callVehicleStatus in db.tblCallVehicleStatus
                                            on orders.Id equals callVehicleStatus.StoreOrderOperatingId
                                            where callVehicleStatus.CountToCancel == 3 && callVehicleStatus.ModifiledOn <= last5Min
                                            select orders).ToListAsync();

                foreach (var order in ordersToCancel)
                {
                    order.Confirm10 = 0;
                    order.TimeConfirm10 = null;
                    order.Step = (int)OrderStep.DA_NHAN_DON;
                    order.LogProcessOrder += $"#Hủy xác thực do vượt quá số lần gọi loa lúc {DateTime.Now}";
                }
                await db.SaveChangesAsync();
            }
        }
    }
}
