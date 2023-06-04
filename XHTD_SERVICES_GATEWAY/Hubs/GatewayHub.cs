using System;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Owin;
using Microsoft.Owin.Cors;
using log4net;
using XHTD_SERVICES.Helper;
using System.Linq;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Common;
using Autofac;
using XHTD_SERVICES_GATEWAY.Business;
using System.Threading;

namespace XHTD_SERVICES_GATEWAY.Hubs
{
    public class GatewayHub : Hub
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(GatewayHub));

        public void SendMessage(string name, string message)
        {
            try
            {
                var broadcast = GlobalHost.ConnectionManager.GetHubContext<GatewayHub>();
                broadcast.Clients.All.SendMessage(name, message);
            }
            catch (Exception ex)
            {

            }
        }

        public void SendNotificationCBV(int status, string inout, string cardNo, string message, string deliveryCode = "")
        {
            try
            {
                var broadcast = GlobalHost.ConnectionManager.GetHubContext<GatewayHub>();
                broadcast.Clients.All.SendNotificationCBV(status, inout, cardNo, message, deliveryCode);
            }
            catch (Exception ex)
            {

            }
            //Clients.All.SendNotificationCBV(status, inout, cardNo, message, deliveryCode);
        }
    }
}
