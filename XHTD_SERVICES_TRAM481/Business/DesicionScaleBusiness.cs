using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES.Device;
using XHTD_SERVICES_TRAM481.Models.Response;
using XHTD_SERVICES_TRAM481.Hubs;
using log4net;

namespace XHTD_SERVICES_TRAM481.Business
{
    public class DesicionScaleBusiness
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(DesicionScaleBusiness));

        protected readonly ScaleOperatingRepository _scaleOperatingRepository;

        protected readonly StoreOrderOperatingRepository _storeOrderOperatingRepository;

        public DesicionScaleBusiness(
            ScaleOperatingRepository scaleOperatingRepository,
            StoreOrderOperatingRepository storeOrderOperatingRepository
            )
        {
            _scaleOperatingRepository = scaleOperatingRepository;
            _storeOrderOperatingRepository = storeOrderOperatingRepository;
        }

        public DesicionScaleResponse MakeDecisionScaleIn(string deliveryCode, int weight)
        {
            var resultResponse = new DesicionScaleResponse
            {
                Code = "02",
                Message = "Cân thất bại"
            };

            var response = DIBootstrapper.Init().Resolve<ScaleApiLib>().ScaleIn(deliveryCode, weight);

            if (response.Code == "01")
            {
                // Gọi API lưu thành công
                logger.Info($"Scale_Send_Successed: {response.Message}");

                new ScaleHub().SendMessage("Scale_Send_Successed", $"{response.Message}");
            }
            else
            {
                // Gọi API lưu thất bại
                logger.Info($"Scale_Send_Failed: {response.Message}");

                new ScaleHub().SendMessage("Scale_Send_Failed", $"{response.Message}");
            }

            resultResponse.Code = response.Code;
            resultResponse.Message = response.Message;

            return resultResponse;
        }

        public async Task<DesicionScaleResponse> MakeDecisionScaleOut(string deliveryCode, int weight)
        {
            var resultResponse = new DesicionScaleResponse
            {
                Code = "02",
                Message = "Cân thất bại"
            };

            var order = await _storeOrderOperatingRepository.GetDetail(deliveryCode);

            if (CheckToleranceLimit(order, weight))
            {
                // vi phạm độ lệch khối lượng
                logger.Info($"Scale_Send_Failed: Vượt quá 1% dung sai cho phép");

                new ScaleHub().SendMessage("Scale_Send_Failed", $"Vượt quá 1% dung sai cho phép");

                return new DesicionScaleResponse
                {
                    Code = "02",
                    Message = "Vượt quá 1% dung sai cho phép"
                };
            }

            var response = DIBootstrapper.Init().Resolve<ScaleApiLib>().ScaleOut(deliveryCode, weight);

            if (response.Code == "01")
            {
                // Gọi API lưu thành công
                logger.Info($"Scale_Send_Successed: {response.Message}");

                new ScaleHub().SendMessage("Scale_Send_Successed", $"{response.Message}");
            }
            else
            {
                // Gọi API lưu thất bại
                logger.Info($"Scale_Send_Failed: {response.Message}");

                new ScaleHub().SendMessage("Scale_Send_Failed", $"{response.Message}");
            }

            resultResponse.Code = response.Code;
            resultResponse.Message = response.Message;

            return resultResponse;
        }

        public bool CheckToleranceLimit(tblStoreOrderOperating order, int weight)
        {
            bool isCheck = false;
            try
            {
                var tolerance = (weight - order.WeightIn - order.SumNumber * 1000) / (order.SumNumber * 1000);
                tolerance = tolerance < 0 ? (-1) * tolerance : tolerance;
                isCheck = (double)tolerance > 0.01 ? true : false;
            }
            catch (Exception ex)
            {
                // TODO: log here
            }
            return isCheck;
        }
    }
}
