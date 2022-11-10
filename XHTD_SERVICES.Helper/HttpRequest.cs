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

        public static IRestResponse GetWebsaleOrder(string token)
        {
            // strToken = "eyJhbGciOiJSUzI1NiIsImtpZCI6IkZGMTVFOEFFMDI0Q0MyMkNGMzhBMURFOEU0RTREQjg1RTcxQTZGNTkiLCJ0eXAiOiJhdCtqd3QiLCJ4NXQiOiJfeFhvcmdKTXdpenppaDNvNU9UYmhlY2FiMWsifQ.eyJuYmYiOjE2NjY5NDg2NzgsImV4cCI6MTY2Njk3NzQ3OCwiaXNzIjoiaHR0cDovLzEwLjAuMS40MDo2MDAxIiwiYXVkIjoid2Vic2FsZSIsImNsaWVudF9pZCI6IndlYnNhbGUtYXBpLWhhaXBob25nIiwic3ViIjoiMjA5MCIsImF1dGhfdGltZSI6MTY2Njk0ODY3OCwiaWRwIjoibG9jYWwiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3VzZXJkYXRhIjoiMjA5MCIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJJRVJQLVRVTkdOVCIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6Ik9yZGVyTWFuYWdlbWVudCIsIkFjdGlvbiI6WyJTdXBlckRlY2lzaW9uIiwiRWRpdERlY2lzaW9uIiwiTWFuYWdlckFyZWEiLCJQcm9tb09yZGVyIiwiTm9Db250cmFjdE9yZGVyIiwiVXBkYXRlT3JkZXIiLCJQcmludERlbGl2ZXJ5QmlsbHMiLCJSZXBvcnQiLCJFZGl0VGlja2l0IiwiQXBwcm92YWxPcmRlciIsIk5ld0NvdW50cnlzaWRlT3JkZXIiLCJWaWV3RGVjaXNpb24iLCJXZWlnaHRNYW5hZ2VtZW50IiwiRWRpdE9yZGVyRGF0ZSIsIkVkaXRCaWxsIiwiR2V0QWxsQ3VzdG9tZXIiLCJHZXRBbGxTaGlwcG9pbnQiLCJSZWFkR3VhcmFudGVlIiwiQ3JlYXRlR3VhcmFudGVlIiwiUmVhZFBheW1lbnQiLCJDcmVhdGVQYXltZW50IiwiUmVwb3J0QWNjb3VudCIsIlJlcG9ydEFsbCIsIlVzZXJNYW5hZ2VtZW50IiwiUmVhZFZlaGljbGVzIiwiQ3JlYXRlVmVoaWNsZXMiLCJWZWhpY2xlUHJvaGliaXRlZCIsIkNvbnRyYWN0T3JkZXIiXSwic2NvcGUiOlsib3BlbmlkIiwicHJvZmlsZSIsIndlYnNhbGUiLCJvZmZsaW5lX2FjY2VzcyJdLCJhbXIiOlsicHdkIl19.ol6RM8fyuB0JQNq0KiD-SnnkNqrfywDmf7TL3niITGsoXu9LcvIaTmLFiotIwb5RK0pOADUOV7b5uha4pDaBh4AKi28hgaNKB56btbTJTRLh0aRwZobu7CFhJhsC568RRDi3TKU-nke75nD560f98U7PORFNuJnurlbyxFrtB37Wor2loZNT8lzhobUk9l7uE5o-vMua7RNXaS8_0TJGEWSHRoiGUjBzW9RJUXg3PxnSQkMZljvOa8qu30JO0kkvDbGyL-qzFCX4PGg7wHCQPVs019S18nzny_C9DIhLWs-aoAZtQhrpCqvvlfTvOKQXoLSg3XBIJKuVgJhc53805g";
            var apiUrl = ConfigurationManager.GetSection("API_WebSale/Url") as NameValueCollection;

            var requestData = new SearchOrderRequest
            {
                from = DateTime.Now.AddHours(-48).ToString("dd/MM/yyyy"),
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
    }
}
