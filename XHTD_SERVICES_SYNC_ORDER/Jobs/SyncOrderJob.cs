using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using log4net;
using XHTD_SERVICES.Data.Repositories;
using RestSharp;
using XHTD_SERVICES_SYNC_ORDER.Models.Response;
using XHTD_SERVICES.Data.Models.Response;
using XHTD_SERVICES_SYNC_ORDER.Models.Request;
using Newtonsoft.Json;
using System.Configuration;
using System.Collections.Specialized;
using XHTD_SERVICES_SYNC_ORDER.Models.Values;
using XHTD_SERVICES.Helper;

namespace XHTD_SERVICES_SYNC_ORDER.Jobs
{
    public class SyncOrderJob : IJob
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        private static string strToken;

        public SyncOrderJob(StoreOrderOperatingRepository storeOrderOperatingRepository)
        {
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(async () =>
            {
                await SyncOrderProcess();
            });
        }

        public async Task SyncOrderProcess()
        {
            Console.WriteLine("start process SyncOrderJob");
            log.Info("start process SyncOrderJob");

            GetToken();

            List<OrderItemResponse> websaleOrders = GetWebsaleOrder();

            if(websaleOrders == null || websaleOrders.Count == 0)
            {
                return;
            }

            foreach (var websaleOrder in websaleOrders)
            {
                await SyncWebsaleOrderToDMS(websaleOrder);
            }
        }

        public void GetToken()
        {
            try
            {
                IRestResponse response = HttpRequest.GetWebsaleToken();

                var content = response.Content;

                var responseData = JsonConvert.DeserializeObject<GetTokenResponse>(content);
                strToken = responseData.access_token;
            }
            catch (Exception ex)
            {
                Console.WriteLine("getToken error: " + ex.Message);
                log.Error("getToken error: " + ex.Message);
            }
        }

        public List<OrderItemResponse> GetWebsaleOrder()
        {
            IRestResponse response = HttpRequest.GetWebsaleOrder(strToken);
            var content = response.Content;

            if (response.StatusDescription.Equals("Unauthorized"))
            {
                Console.WriteLine("Unauthorized GetWebsaleOrder");
                log.Error("Unauthorized GetWebsaleOrder");
                return null;
            }

            var responseData = JsonConvert.DeserializeObject<SearchOrderResponse>(content);

            return responseData.collection.OrderBy(x => x.id).ToList();
        }

        public async Task SyncWebsaleOrderToDMS(OrderItemResponse websaleOrder)
        {
            var stateId = 0;
            switch (websaleOrder.status.ToUpper())
            {
                case "BOOKED":
                    switch (websaleOrder.orderPrintStatus.ToUpper())
                    {
                        case "BOOKED":
                        case "APPROVED":
                        case "PENDING":
                            stateId = (int)OrderState.DA_XAC_NHAN;
                            break;
                        case "PRINTED":
                            stateId = (int)OrderState.DA_IN_PHIEU;
                            break;
                    }
                    break;
                case "PRINTED":
                    stateId = (int)OrderState.DA_IN_PHIEU;
                    break;
                case "VOIDED":
                    stateId = (int)OrderState.DA_HUY;
                    break;
                case "RECEIVING":
                    stateId = (int)OrderState.DANG_LAY_HANG;
                    break;
                case "RECEIVED":
                    stateId = (int)OrderState.DA_XUAT_HANG;
                    break;
            }

            if (stateId != (int)OrderState.DA_HUY && stateId != (int)OrderState.DA_XUAT_HANG)
            {
                await _storeOrderOperatingRepository.CreateAsync(websaleOrder);
            }
            else if (stateId == (int)OrderState.DA_HUY){
                await _storeOrderOperatingRepository.CancelOrder(websaleOrder.id);
            }
        }
    }
}
