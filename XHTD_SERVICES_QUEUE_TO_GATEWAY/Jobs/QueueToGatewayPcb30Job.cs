﻿using Quartz;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Repositories;

namespace XHTD_SERVICES_QUEUE_TO_GATEWAY.Jobs
{
    public class QueueToGatewayPcb30Job : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly QueueToGatewayLogger _queueToGatewayLogger;

        public QueueToGatewayPcb30Job(
            StoreOrderOperatingRepository storeOrderOperatingRepository,
            QueueToGatewayLogger queueToGatewayLogger
            )
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
            _queueToGatewayLogger = queueToGatewayLogger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                PushToDbCallPcb30Proccesss();
            });
        }

        public void PushToDbCallPcb30Proccesss()
        {
            try
            {
                var LimitVehicle = 5;
                var IsCall = true;

                if (!IsCall) return;

                ProcessPushToDBCall(LimitVehicle);
            }
            catch (Exception ex)
            {
                _queueToGatewayLogger.LogError(ex.Message);
            }
        }

        public void ProcessPushToDBCall(int LimitVehicle)
        {
            try
            {
                //get sl xe trong bãi chờ máng ứng với sp
                var vehicleFrontPcb30Yard = _storeOrderOperatingRepository.CountStoreOrderWaitingIntoTroughByType("PCB30");
                if (vehicleFrontPcb30Yard < LimitVehicle)
                {
                    ProcessUpdateStepIntoPcb30Yard(LimitVehicle - vehicleFrontPcb30Yard);
                }
            }
            catch (Exception ex)
            {
                _queueToGatewayLogger.LogError($@"ProcessPushToDBCall PCB30 error: {ex.Message}");
            }
        }

        public void ProcessUpdateStepIntoPcb30Yard(int topX)
        {
            try
            {
                using (var db = new XHTD_Entities())
                {
                    var orders = db.tblStoreOrderOperatings.Where(x => x.Step == 1 && x.TypeProduct.Equals("PCB30") && x.IndexOrder2 == 0 && (x.DriverUserName ?? "") != "").OrderBy(x => x.IndexOrder).Take(topX).ToList();
                    foreach (var order in orders)
                    {
                        var dateTimeCall = DateTime.Now.AddMinutes(-2);
                        if (order.TimeConfirm1 > dateTimeCall) continue;
                        var sqlUpdate = "UPDATE tblStoreOrderOperating SET Step =  4,  TimeConfirm4 = ISNULL(TimeConfirm4, GETDATE()), LogProcessOrder = CONCAT(LogProcessOrder, N'#Đưa vào hàng đợi mời xe vào lúc ', FORMAT(getdate(), 'dd/MM/yyyy HH:mm:ss')) WHERE OrderId = @OrderId AND ISNULL(Step,0) <> 4";
                        var updateResponse = db.Database.ExecuteSqlCommand(sqlUpdate, new SqlParameter("@OrderId", order.OrderId));
                        if (updateResponse > 0)
                        {
                            // xử lý nghiệp vụ đẩy vào db để xử lý gọi loa
                            var tblCallVehicleStatusDb = db.tblCallVehicleStatus.FirstOrDefault(x => x.StoreOrderOperatingId == order.Id && x.IsDone == false);
                            if (tblCallVehicleStatusDb != null) continue;
                            var logString = $@"Xe được mời vào lúc {DateTime.Now} .";
                            var newTblVehicleStatus = new tblCallVehicleStatu
                            {
                                StoreOrderOperatingId = order.Id,
                                CountTry = 0,
                                TypeProduct = order.TypeProduct,
                                CreatedOn = DateTime.Now,
                                ModifiledOn = DateTime.Now,
                                LogCall = logString,
                                IsDone = false
                            };
                            db.tblCallVehicleStatus.Add(newTblVehicleStatus);
                            db.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _queueToGatewayLogger.LogError($"ProcessUpdateStepIntoPCB30Yard error: " + ex.Message);
            }
        }
    }
}
