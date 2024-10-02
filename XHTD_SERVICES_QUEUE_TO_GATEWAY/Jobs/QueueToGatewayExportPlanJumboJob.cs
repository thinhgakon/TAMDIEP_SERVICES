using Quartz;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Models.Values;
using XHTD_SERVICES.Data.Repositories;

namespace XHTD_SERVICES_QUEUE_TO_GATEWAY.Jobs
{
    public class QueueToGatewayExportPlanJumboJob : IJob
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("JumboFileAppender");
        private const string TYPE_PRODUCT = "JUMBO";

        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        public QueueToGatewayExportPlanJumboJob(StoreOrderOperatingRepository storeOrderOperatingRepository)
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            await Task.Run(() =>
            {
                log.Info($@"Start QueueToGatewayExportPlanJumboJob: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");
                QueueToCallProccess();
            });
        }

        public void QueueToCallProccess()
        {
            try
            {
                var IsCall = true;
                using (var db = new XHTD_Entities())
                {
                    var isCallConfig = db.tblSystemParameters.FirstOrDefault(x => x.Code == $"IS_CALL_{TYPE_PRODUCT}");
                    IsCall = isCallConfig.Value == "1" ? true : false;
                }

                if (!IsCall)
                {
                    log.Info($@"Cấu hình gọi loa đang OFF. Kiểm tra IsCall trong tblSystemParameters");
                    return;
                }

                using (var db = new XHTD_Entities())
                {
                    var callConfigs = db.tblCallToGatewayConfigs.Where(x => x.Status == 1).ToList();

                    foreach (var itemConfig in callConfigs)
                    {
                        int sourceDocumentId = (int)itemConfig.SourceDocumentId;
                        int maxVehicleJumbo = (int)itemConfig.MaxVehicleJumbo;
                        ProcessByExportPlan(maxVehicleJumbo, sourceDocumentId);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }
        }

        public void ProcessByExportPlan(int LimitVehicle, int sourceDocumentId)
        {
            log.Info($@"Xử lý đơn hàng với số hiệu hợp đồng: {sourceDocumentId} với cấu hình xe tối đa {LimitVehicle}");
            try
            {
                //get sl xe trong bãi chờ máng ứng với sp
                var vehicleFrontJumboYard = _storeOrderOperatingRepository.CountStoreOrderWaitingIntoTroughByTypeAndExportPlan(TYPE_PRODUCT, sourceDocumentId);

                log.Info($@"1. Số xe trong bãi chờ: {vehicleFrontJumboYard}");

                if (vehicleFrontJumboYard >= LimitVehicle)
                {
                    log.Info($@"2. Số xe đang chờ vượt quá số xe tối đa => Kết thúc");
                    return;
                }

                AddVehicleIntoQueue(LimitVehicle - vehicleFrontJumboYard, sourceDocumentId);
            }
            catch (Exception ex)
            {
                log.Error($@"SyncTrough1 {ex.Message}");
            }
        }

        public void AddVehicleIntoQueue(int topX, int? sourceDocumentId)
        {
            try
            {
                using (var db = new XHTD_Entities())
                {
                    var callConfigs = db.tblCallToGatewayConfigs.Where(x => x.Status == 1 && x.SourceDocumentId != 0).ToList();
                    var sourceDocumentIds = callConfigs.Select(x => x.SourceDocumentId).ToList();

                    var query = db.tblStoreOrderOperatings
                                    .Where(x => x.Step == 1 &&
                                                x.TypeProduct.Equals(TYPE_PRODUCT) &&
                                                x.IndexOrder2 == 0 &&
                                               (x.DriverUserName ?? "") != "" &&
                                                x.IsVoiced == false);

                    if (sourceDocumentId != 0)
                    {
                        query = query.Where(x => x.SourceDocumentId == sourceDocumentId);
                    }
                    else
                    {
                        query = query.Where(x => x.SourceDocumentId == null || 
                                                 x.SourceDocumentId == 0 || 
                                                (x.SourceDocumentId != null && !sourceDocumentIds.Contains(x.SourceDocumentId)));
                    }

                    var orders = query.OrderBy(x => x.TimeConfirm10)
                                      .Take(topX)
                                      .ToList();

                    if (orders == null || orders.Count == 0)
                    {
                        WriteLogInfo($"4. Không tìm thấy xe {TYPE_PRODUCT} nào với số hiệu hợp đồng: {sourceDocumentId} => Kết thúc");
                        return;
                    }
                    else
                    {
                        WriteLogInfo($"4. Các xe phù hợp: {string.Join(", ", orders.Select(order => order.Vehicle))}");
                    }

                    foreach (var order in orders)
                    {
                        WriteLogInfo($"4.1. Tiến hành thêm xe vào hàng đợi: {order.DeliveryCode} --- {order.Vehicle}");

                        var sqlUpdate = $@"UPDATE tblStoreOrderOperating 
                                           SET Step = {(int)OrderStep.CHO_GOI_XE}, 
                                               TimeConfirm11 = ISNULL(TimeConfirm11, GETDATE()), 
                                               LogProcessOrder = CONCAT(LogProcessOrder, N'#Đưa vào hàng đợi mời xe vào lúc ', FORMAT(getdate(), 'dd/MM/yyyy HH:mm:ss')) 
                                           WHERE OrderId = @OrderId 
                                                 AND ISNULL(Step, 0) <> {(int)OrderStep.CHO_GOI_XE}
                                           ";

                        var updateResponse = db.Database.ExecuteSqlCommand(sqlUpdate, new SqlParameter("@OrderId", order.OrderId));
                        if (updateResponse > 0)
                        {
                            // xử lý nghiệp vụ đẩy vào db để xử lý gọi loa
                            var tblCallVehicleStatusDb = db.tblCallVehicleStatus
                                                        .FirstOrDefault(x => x.StoreOrderOperatingId == order.Id
                                                                        && x.IsDone == false
                                                                        && x.CallType == CallType.CONG);

                            if (tblCallVehicleStatusDb != null)
                            {
                                WriteLogInfo($"4.2. Đã tồn tại bản ghi chờ gọi loa");
                                continue;
                            }

                            var logString = $@"Đưa xe vào hàng đợi gọi loa lúc {DateTime.Now}. ";
                            var newTblVehicleStatus = new tblCallVehicleStatu
                            {
                                StoreOrderOperatingId = order.Id,
                                CountTry = 0,
                                TypeProduct = order.TypeProduct,
                                CreatedOn = DateTime.Now,
                                ModifiledOn = DateTime.Now,
                                LogCall = logString,
                                IsDone = false,
                                CallType = CallType.CONG
                            };
                            db.tblCallVehicleStatus.Add(newTblVehicleStatus);
                            db.SaveChanges();

                            WriteLogInfo($"4.2. Thêm thành công");
                        }
                        else
                        {
                            WriteLogInfo($"4.2. Thêm thất bại");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLogInfo($"ERROR ProcessUpdateStepIntoYard: {ex.Message} --- {ex.StackTrace} --- {ex.InnerException} => Kết thúc");
            }
        }

        public void WriteLogInfo(string message)
        {
            Console.WriteLine(message);
            log.Info(message);
        }
    }
}
