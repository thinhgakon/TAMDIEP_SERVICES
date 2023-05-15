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

namespace XHTD_SERVICES.Helper
{
    public static class HttpRequest
    {
        public static IRestResponse GetWebsaleToken()
        {
            var apiUrl = ConfigurationManager.GetSection("API_WebSale/Url") as NameValueCollection;
            var account = ConfigurationManager.GetSection("API_WebSale/Account") as NameValueCollection;

            var requestData = new GetTokenRequest
            {
                grant_type = account["grant_type"].ToString(),
                client_secret = account["client_secret"].ToString(),
                username = account["username"].ToString(),
                password = account["password"].ToString(),
                client_id = account["client_id"].ToString(),
            };

            var client = new RestClient(apiUrl["GetToken"]);
            var request = new RestRequest();

            request.Method = Method.POST;
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "multipart/form-data");
            request.Parameters.Clear();
            request.AddParameter("grant_type", requestData.grant_type);
            request.AddParameter("client_secret", requestData.client_secret);
            request.AddParameter("username", requestData.username);
            request.AddParameter("password", requestData.password);
            request.AddParameter("client_id", requestData.client_id);

            IRestResponse response = client.Execute(request);

            return response;
        }

        public static IRestResponse GetWebsaleOrder(string token, int numberHoursSearchOrder)
        {
            var apiUrl = ConfigurationManager.GetSection("API_WebSale/Url") as NameValueCollection;

            var requestData = new SearchOrderRequest
            {
                from = DateTime.Now.AddHours(-1 * numberHoursSearchOrder).ToString("dd/MM/yyyy"),
                to = DateTime.Now.ToString("dd/MM/yyyy"),
            };

            var client = new RestClient(apiUrl["SearchOrder"]);
            var request = new RestRequest();

            request.Method = Method.POST;
            request.AddJsonBody(requestData);
            request.AddHeader("Authorization", "Bearer " + token);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");
            request.RequestFormat = DataFormat.Json;

            IRestResponse response = client.Execute(request);

            return response;
        }

        public static IRestResponse GetDMSToken()
        {
            var apiUrl = ConfigurationManager.GetSection("API_DMS/Url") as NameValueCollection;
            var account = ConfigurationManager.GetSection("API_DMS/Account") as NameValueCollection;

            var requestData = new GetDMSTokenRequest
            {
                grant_type = account["grant_type"].ToString(),
                username = account["username"].ToString(),
                password = account["password"].ToString(),
            };

            var client = new RestClient(apiUrl["GetToken"]);
            var request = new RestRequest();

            request.Method = Method.POST;

            request.AddJsonBody(requestData);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");
            request.RequestFormat = DataFormat.Json;

            IRestResponse response = client.Execute(request);

            return response;
        }

        public static IRestResponse SendDMSMsg(string token, SendMsgRequest messenge)
        {
            var apiUrl = ConfigurationManager.GetSection("API_DMS/Url") as NameValueCollection;

            var requestData = new SendMsgRequest
            {
                Type = messenge.Type,
                Source = messenge.Source,
                Status = messenge.Status,
                Direction = messenge.Direction,
                Content = messenge.Content,
                Data = messenge.Data
            };

            var client = new RestClient(apiUrl["SendMsg"]);
            var request = new RestRequest();

            request.Method = Method.POST;
            request.AddJsonBody(requestData);
            request.AddHeader("Authorization", "Bearer " + token);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");
            request.RequestFormat = DataFormat.Json;

            IRestResponse response = client.Execute(request);

            return response;
        }

        public static IRestResponse SendInforNotification(string sender, string receiver, string message)
        {
            var apiUrl = ConfigurationManager.GetSection("API_DMS/Url") as NameValueCollection;

            var requestData = new SendInforNotificationRequest
            {
                UserNameSender = sender,
                UserNameReceiver = receiver,
                ContentMessage = message,
            };

            var client = new RestClient(apiUrl["SendInforNotification"]);
            var request = new RestRequest();

            request.Method = Method.POST;
            request.AddJsonBody(requestData);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");
            request.RequestFormat = DataFormat.Json;

            IRestResponse response = client.Execute(request);

            return response;
        }

        public static bool UpdateWeightInWebSale()
        {
            return true;
        }

        public static bool UpdateWeightOutWebSale()
        {
            return true;
        }
    }
}
