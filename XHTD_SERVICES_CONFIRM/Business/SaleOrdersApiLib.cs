using Newtonsoft.Json;
using XHTD_SERVICES_CONFIRM.Models.Response;
using XHTD_SERVICES.Helper;

namespace XHTD_SERVICES_CONFIRM.Business
{
    public class SaleOrdersApiLib
    {
        public SaleOrdersResponse CheckOrderValidate(string deliveryCodes)
        {
            var updateResponse = HttpRequest.CheckOrderValidate(deliveryCodes);
            var updateResponseContent = updateResponse.Content;
            var response = JsonConvert.DeserializeObject<SaleOrdersResponse>(updateResponseContent);

            var resultResponse = new SaleOrdersResponse
            {
                Code = "02",
                Message = "Xác thực thất bại"
            };
            resultResponse.Code = response.Code;
            resultResponse.Message = response.Message;

            return resultResponse;
        }
    }
}
