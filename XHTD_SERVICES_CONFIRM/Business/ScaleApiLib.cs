using Newtonsoft.Json;
using XHTD_SERVICES_CONFIRM.Models.Response;
using XHTD_SERVICES.Helper;

namespace XHTD_SERVICES_CONFIRM.Business
{
    public class ScaleApiLib
    {
        public DesicionScaleResponse SaleOrder(string deliveryCode)
        {
            var strToken = HttpRequest.GetScaleToken();

            var updateResponse = HttpRequest.SaleOrderWebSale(strToken, deliveryCode);

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
