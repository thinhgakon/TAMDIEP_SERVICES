﻿using System;
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
using System.Xml.Linq;

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

        public void SendConfirmNotification(string name, int status, string cardNo, string message, string vehicle = "")
        {
            SendConfirmNotificationRequest notification = new SendConfirmNotificationRequest
            {
                Name = name,
                Status = status,
                CardNo = cardNo,
                Message = message,
                Vehicle = vehicle
            };

            HttpRequest.SendDMSMsgForConfirmation(notification);
        }

        public void SendGatewayNotification(string inout, int status, string cardNo, string message, string vehicle = null)
        {
            SendGatewayNotificationRequest notification = new SendGatewayNotificationRequest
            {
                InOut = inout,
                Status = status,
                CardNo = cardNo,
                Message = message,
                Vehicle = vehicle
            };

            HttpRequest.SendDMSMsgForGatewayNotification(notification);
        }

        public void SendTroughNotification(string troughType, string machineCode, string troughCode, string vehicle)
        {
            SendTroughNotificationRequest notification = new SendTroughNotificationRequest
            {
                TroughType = troughType,
                MachineCode = machineCode,
                TroughCode = troughCode,
                Vehicle = vehicle
            };

            HttpRequest.SendDMSMsgForTroughNotification(notification);
        }

        public void SendTroughData(string troughType, string deliveryCode, string machineCode, string troughCode, int? firstQuantity, int? lastQuantity)
        {
            SendTroughDataRequest data = new SendTroughDataRequest
            {
                TroughType = troughType,
                DeliveryCode = deliveryCode,
                MachineCode = machineCode,
                TroughCode = troughCode,
                FirstQuantity = firstQuantity,
                LastQuantity = lastQuantity
            };

            HttpRequest.SendDMSMsgForTroughData(data);
        }

        public void SendOrderSendOrderToleranceWarning(string deliveryCode, string vehicle, decimal? sumNumber, int? weightIn, int? weightOut, double? tolerance)
        {
            SendOrderToleranceWarningRequest warning = new SendOrderToleranceWarningRequest
            {
                DeliveryCode = deliveryCode,
                Vehicle = vehicle,
                SumNumber = sumNumber,
                WeightIn = weightIn,
                WeightOut = weightOut,
                Tolerance = tolerance
            };

            HttpRequest.SendDMSOrderToleranceWarning(warning);
        }

        // Gửi thông báo thay đổi trạng thái đơn hàng đến app lái xe
        public void SendInforNotification(string receiver, string message)
        {
            HttpRequest.SendInforNotification(receiver, message);
        }

        public void SendScale1Sensor(string sensorCode, string status)
        {
            HttpRequest.SendScale1Sensor(sensorCode,status);
        }

        public void SendScale1Info(DateTime time, string value)
        {
            HttpRequest.SendScale1Info(time, value);
        }

        public void SendScale1Message(string name, string message)
        {
            HttpRequest.SendScale1Message(name, message);
        }

        public void SendScale2Sensor(string sensorCode, string status)
        {
            HttpRequest.SendScale2Sensor(sensorCode, status);
        }

        public void SendScale2Info(DateTime time, string value)
        {
            HttpRequest.SendScale2Info(time, value);
        }

        public void SendScale2Message(string name, string message)
        {
            HttpRequest.SendScale2Message(name, message);
        }

        public void SendPushNotification(string userName, string message)
        {
            HttpRequest.SendPushNotification(userName, message);

        }
    }
}
