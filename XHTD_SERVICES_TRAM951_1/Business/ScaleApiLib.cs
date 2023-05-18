using Newtonsoft.Json;
using XHTD_SERVICES_TRAM951_1.Models.Response;
using XHTD_SERVICES.Helper;

namespace XHTD_SERVICES_TRAM951_1.Business
{
    public class ScaleApiLib
    {
        public DesicionScaleResponse ScaleIn(string deliveryCode, int weight)
        {
            var strToken = HttpRequest.GetScaleToken();

            var updateResponse = HttpRequest.UpdateWeightInWebSale(strToken, deliveryCode, weight);

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
            var strToken = HttpRequest.GetScaleToken();

            var updateResponse = HttpRequest.UpdateWeightOutWebSale(strToken, deliveryCode, weight);

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
