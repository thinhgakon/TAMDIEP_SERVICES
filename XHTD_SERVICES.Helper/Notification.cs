using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using XHTD_SERVICES.Helper.Models.Request;
using XHTD_SERVICES.Helper.Models.Response;
using RestSharp;
using Newtonsoft.Json;

namespace XHTD_SERVICES.Helper
{
    public class Notification
    {
        public void SendMsg(string messageContent)
        {
            IRestResponse response = HttpRequest.GetDMSToken();

            var content = response.Content;

            var responseData = JsonConvert.DeserializeObject<GetDMSTokenResponse>(content);
            string strToken = responseData.access_token;

            if(strToken != "")
            {
                HttpRequest.SendDMSMsg(strToken, messageContent);
            }
        }

        public void SendNotification(
            string fromService,
            string fromDevice,
            string vehicle,
            string cardNo,
            string deliveryCode,
            string content
            )
        {
            NotificationRequest notification = new NotificationRequest
            {
                FromService = fromService,
                FromDevice = fromDevice,
                Vehicle = vehicle,
                CardNo = cardNo,
                DeliveryCode = deliveryCode,
                Content = content,
            };

            var messageContent = JsonConvert.SerializeObject(notification);

            SendMsg(messageContent);
        }
    }
}
