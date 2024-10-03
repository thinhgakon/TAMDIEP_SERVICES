using Newtonsoft.Json;
using XHTD_SERVICES_GATEWAY.Models.Response;
using XHTD_SERVICES.Helper;

namespace XHTD_SERVICES_GATEWAY.Business
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

        public SaleOrdersResponse UpdateOrderStatus(string deliveryCodes)
        {
            var updateResponse = HttpRequest.UpdateOrderStatus(deliveryCodes);
            var updateResponseContent = updateResponse.Content;
            var response = JsonConvert.DeserializeObject<SaleOrdersResponse>(updateResponseContent);

            var resultResponse = new SaleOrdersResponse
            {
                Code = "02",
                Message = "Cập nhật trạng thái in phiếu thất bại"
            };
            resultResponse.Code = response.Code;
            resultResponse.Message = response.Message;

            return resultResponse;
        }
    }
}
