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

        protected readonly Notification _notification;

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
            ReindexToGatewayLogger reindexToGatewayLogger,
            Notification notification
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _callToTroughRepository = callToTroughRepository;
            _systemParameterRepository = systemParameterRepository;
            _reindexToGatewayLogger = reindexToGatewayLogger;
            _notification = notification;
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
            try
            {
                _reindexToGatewayLogger.LogInfo("Start process ReindexToGateway service");

                // Xử lý các order đã quá 3 lần gọi loa mà ko vào cổng
                using (var db = new XHTD_Entities())
                {
                    var last5Min = DateTime.Now.AddMinutes(-5);

                    // Xếp lại số
                    var callVehicleStatusReindex = await db.tblCallVehicleStatus
                                                           .Where(x => x.CallType == CallType.CONG &&
                                                                       x.CountTry == 3 &&
                                                                      (x.CountReindex == null || x.CountReindex < 3) &&
                                                                       x.ModifiledOn <= last5Min &&
                                                                       x.IsDone == false)
                                                           .ToListAsync();

                    if (callVehicleStatusReindex == null || callVehicleStatusReindex.Count == 0)
                    {
                        _reindexToGatewayLogger.LogInfo("1. Không có xe nào vượt quá 3 lần gọi => Bỏ qua");
                    }
                    else
                    {
                        _reindexToGatewayLogger.LogInfo($"1. Tìm thấy các đơn vượt quá 3 lần gọi => {string.Join(",", callVehicleStatusReindex.Select(x => x.StoreOrderOperatingId))}");

                        foreach (var callVehicleStatus in callVehicleStatusReindex)
                        {
                            callVehicleStatus.CountReindex = callVehicleStatus.CountReindex == null ? 1 : callVehicleStatus.CountReindex + 1;
                            callVehicleStatus.CountTry = 0;

                            var currentRetryOrder = await db.tblStoreOrderOperatings.FirstOrDefaultAsync(x => x.Id == callVehicleStatus.StoreOrderOperatingId);
                            if (currentRetryOrder != null)
                            {
                                currentRetryOrder.Step = (int)OrderStep.DA_XAC_THUC;
                                currentRetryOrder.LogProcessOrder += $"Đơn hàng bị xoay lốt vào lúc {DateTime.Now} ";
                                await _storeOrderOperatingRepository.ReindexOrderToLastIndex(currentRetryOrder.Id, $"Đơn hàng số hiệu {currentRetryOrder.DeliveryCode} xoay lốt #{currentRetryOrder.IndexOrder}");
                            }
                        }
                        await db.SaveChangesAsync();
                    }

                    // Tăng số lần CountToCancel
                    var callVehicleStatusRetry = await db.tblCallVehicleStatus
                                                         .Where(x => x.CallType == CallType.CONG &&
                                                                     x.CountReindex == 3 &&
                                                                    (x.CountToCancel == null || x.CountToCancel < 3) &&
                                                                     x.ModifiledOn <= last5Min &&
                                                                     x.IsDone == false)
                                                         .ToListAsync();

                    if (callVehicleStatusRetry == null || callVehicleStatusRetry.Count == 0)
                    {
                        _reindexToGatewayLogger.LogInfo("2. Không có xe nào vượt quá 3 lần gọi và vượt quá 3 lần đếm hủy => Bỏ qua");
                    }
                    else
                    {
                        _reindexToGatewayLogger.LogInfo($"2. Tìm thấy các đơn vượt quá 3 lần gọi và vượt quá 3 lần đếm hủy => {string.Join(",", callVehicleStatusRetry.Select(x => x.StoreOrderOperatingId))}");

                        foreach (var callVehicleStatus in callVehicleStatusRetry)
                        {
                            callVehicleStatus.CountToCancel = callVehicleStatus.CountToCancel == null ? 1 : callVehicleStatus.CountToCancel + 1;
                            callVehicleStatus.CountTry = 0;
                            callVehicleStatus.CountReindex = 0;
                        }
                        await db.SaveChangesAsync();
                    }

                    // Hủy xác thực các đơn hàng vượt quá số lần gọi
                    var ordersToCancel = await (from orders in db.tblStoreOrderOperatings
                                                join callVehicleStatus in db.tblCallVehicleStatus
                                                on orders.Id equals callVehicleStatus.StoreOrderOperatingId
                                                where callVehicleStatus.CallType == CallType.CONG &&
                                                      callVehicleStatus.CountToCancel == 3 &&
                                                      callVehicleStatus.ModifiledOn <= last5Min &&
                                                      callVehicleStatus.IsDone == false
                                                select orders).ToListAsync();

                    if (ordersToCancel == null || ordersToCancel.Count == 0)
                    {
                        _reindexToGatewayLogger.LogInfo("3. Không có đơn nào đủ đk hủy xác thực => Bỏ qua");
                    }
                    else
                    {
                        _reindexToGatewayLogger.LogInfo($"3. Hủy xác thực các đơn {string.Join(",", ordersToCancel.Select(x => x.DeliveryCode))}");

                        foreach (var order in ordersToCancel)
                        {
                            order.Confirm10 = 0;
                            order.TimeConfirm10 = null;
                            order.Step = (int)OrderStep.DA_NHAN_DON;
                            order.IndexOrder = 0;
                            order.CountReindex = 0;
                            order.LogProcessOrder += $"#Hủy xác thực do vượt quá số lần gọi loa lúc {DateTime.Now}";
                        }
                        await db.SaveChangesAsync();

                        // Xếp lại lốt
                        var typeProductList = ordersToCancel.Select(x => x.TypeProduct).Distinct().ToList();
                        var reason = $"Đơn hàng số hiệu {string.Join(", ", ordersToCancel.Select(x => x.DeliveryCode))} bị hủy xác thực lúc {DateTime.Now} do vượt quá số lần gọi loa";
                        foreach (var typeProduct in typeProductList)
                        {
                            await _storeOrderOperatingRepository.ReindexOrder(typeProduct, reason);
                        }

                        foreach (var order in ordersToCancel)
                        {
                            var pushMessage = $"Đơn hàng số hiệu {order.DeliveryCode} đã bị hủy lốt do quá thời gian chờ. Xin mời lái xe xác thực lại";
                            SendPushNotification(order.DriverUserName, pushMessage);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _reindexToGatewayLogger.LogInfo($"{ex.Message}");
            }
        }

        public void SendPushNotification(string userNameReceiver, string message)
        {
            try
            {
                _reindexToGatewayLogger.LogInfo($"Gửi push notification đến {userNameReceiver}, nội dung {message}");
                _notification.SendPushNotification(userNameReceiver, message);
            }
            catch (Exception ex)
            {
                _reindexToGatewayLogger.LogInfo($"SendPushNotification Ex: {ex.Message} == {ex.StackTrace} == {ex.InnerException}");
            }
        }
    }
}
