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
        public void SendMsg(SendMsgRequest notification)
        {
            IRestResponse response = HttpRequest.GetDMSToken();

            var content = response.Content;

            var responseData = JsonConvert.DeserializeObject<GetDMSTokenResponse>(content);
            string strToken = responseData.access_token;

            if(strToken != "")
            {
                HttpRequest.SendDMSMsg(strToken, notification);
            }
        }

        public void SendNotification(
            string type,
            string source,
            int status,
            string content,
            int direction,
            string orderId,
            string deliveryCode,
            int rfid,
            string vehicle,
            string driverName,
            string driverUserName
            )
        {
            SendMsgRequest notification = new SendMsgRequest
            {
                Type = type,
                Source = source,
                Status = status,
                Content = content,
                Direction = direction,
                Data = new SendDataMsgRequest
                {
                    Orderid = orderId,
                    DeliveryCode = deliveryCode,
                    Rfid = rfid,
                    Vehicle = vehicle,
                    DriverName = driverName,
                    DriverUserName = driverUserName,
                }
            };

            SendMsg(notification);
        }

        public void SendConfirmNotification(int status, string message, string vehicle, string cardNo)
        {
            SendConfirmNotificationRequest notification = new SendConfirmNotificationRequest
            {
                Status = status,
                Message = message,
                Vehicle = vehicle,
                CardNo = cardNo
            };

            HttpRequest.SendDMSMsgForConfirmation(notification);
        }

        // Gửi thông báo thay đổi trạng thái đơn hàng đến app lái xe
        public void SendInforNotification(string receiver, string message)
        {
            HttpRequest.SendInforNotification(receiver, message);
        }
    }
}
