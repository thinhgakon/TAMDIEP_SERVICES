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
using XHTD_SERVICES_CONFIRM.Business;
using System.Threading;
using XHTD_SERVICES_CONFIRM.Devices;

namespace XHTD_SERVICES_CONFIRM.Hubs
{
    public class ConfirmHub : Hub
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ConfirmHub));

        public void SendMessage(string name, string message)
        {
            try
            {
                var broadcast = GlobalHost.ConnectionManager.GetHubContext<ConfirmHub>();
                broadcast.Clients.All.SendMessage(name, message);
            }
            catch (Exception ex)
            {

            }
        }

        public void SendNotificationConfirmationPoint(string name, int status, string cardNo, string message, string vehicle = "")
        {
            try
            {
                var broadcast = GlobalHost.ConnectionManager.GetHubContext<ConfirmHub>();
                broadcast.Clients.All.SendNotificationConfirmationPoint(name, status, cardNo, message, vehicle);
            }
            catch (Exception ex)
            {

            }
            //Clients.All.SendNotificationHub(status, inout, cardNo, message, deliveryCode);
        }
    }
}
