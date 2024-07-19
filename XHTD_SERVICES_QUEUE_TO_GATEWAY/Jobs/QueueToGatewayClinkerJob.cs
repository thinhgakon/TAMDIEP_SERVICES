using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;
using RestSharp;
using XHTD_SERVICES_QUEUE_TO_GATEWAY.Models.Response;
using XHTD_SERVICES.Data.Models.Response;
using Newtonsoft.Json;
using XHTD_SERVICES_QUEUE_TO_GATEWAY.Models.Values;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES.Helper.Models.Request;
using System.Threading;
using XHTD_SERVICES.Data.Entities;
using System.Data.SqlClient;

namespace XHTD_SERVICES_QUEUE_TO_GATEWAY.Jobs
{
    public class QueueToGatewayClinkerJob : IJob
    {
        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        protected readonly QueueToGatewayLogger _queueToGatewayLogger;

        public QueueToGatewayClinkerJob(
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
                _queueToGatewayLogger.LogInfo($"Start Queue To Gateway CLINKER: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");

                PushToDbCallClinkerProccesss();
            });
        }

        public void PushToDbCallClinkerProccesss()
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
                var vehicleFrontClinkerYard = _storeOrderOperatingRepository.CountStoreOrderWaitingIntoTroughByType("CLINKER");
                if (vehicleFrontClinkerYard < LimitVehicle)
                {
                    ProcessUpdateStepIntoClinkerYard(LimitVehicle - vehicleFrontClinkerYard);
                }
            }
            catch (Exception ex)
            {
                _queueToGatewayLogger.LogError($@"ProcessPushToDBCall CLINKER error: {ex.Message}");
            }
        }
        public void ProcessUpdateStepIntoClinkerYard(int topX)
        {
            try
            {
                using (var db = new XHTD_Entities())
                {
                    var orders = db.tblStoreOrderOperatings.Where(x => x.Step == 10 && x.TypeProduct.Equals("CLINKER") && x.IndexOrder2 == 0 && (x.DriverUserName ?? "") != "").OrderBy(x => x.IndexOrder).Take(topX).ToList();
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
                _queueToGatewayLogger.LogError($"ProcessUpdateStepIntoClinkerYard error: " + ex.Message);
            }
        }
    }
}
