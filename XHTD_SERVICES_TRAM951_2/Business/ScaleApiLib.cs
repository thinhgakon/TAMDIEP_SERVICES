using Newtonsoft.Json;
using XHTD_SERVICES_TRAM951_2.Models.Response;
using XHTD_SERVICES.Helper;
using log4net;

namespace XHTD_SERVICES_TRAM951_2.Business
{
    public class ScaleApiLib
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(ScaleApiLib));

        public DesicionScaleResponse ScaleIn(string deliveryCode, int weight)
        {
            logger.Info($"ScaleIn API: deliveryCode={deliveryCode} weight={weight}");

            var updateResponse = HttpRequest.UpdateWeightInWebSale(deliveryCode, weight);

            if (updateResponse.StatusDescription.Equals("Unauthorized"))
            {
                var unauthorizedResponse = new DesicionScaleResponse();
                unauthorizedResponse.Code = "02";
                unauthorizedResponse.Message = "Xác thực API cân WebSale không thành công";
                return unauthorizedResponse;
            }

            var updateResponseContent = updateResponse.Content;

            var response = JsonConvert.DeserializeObject<DesicionScaleResponse>(updateResponseContent);

            return response;
        }

        public DesicionScaleResponse ScaleOut(string deliveryCode, int weight)
        {
            logger.Info($"ScaleOut API: deliveryCode={deliveryCode} weight={weight}");

            var updateResponse = HttpRequest.UpdateWeightOutWebSale(deliveryCode, weight);

            if (updateResponse.StatusDescription.Equals("Unauthorized"))
            {
                var unauthorizedResponse = new DesicionScaleResponse();
                unauthorizedResponse.Code = "02";
                unauthorizedResponse.Message = "Xác thực API cân WebSale không thành công";
                return unauthorizedResponse;
            }

            var updateResponseContent = updateResponse.Content;

            var response = JsonConvert.DeserializeObject<DesicionScaleResponse>(updateResponseContent);

            return response;
        }
    }
}
