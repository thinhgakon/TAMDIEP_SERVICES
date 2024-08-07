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
using System.Threading;
using XHTD_SERVICES_XB_TROUGH_1.Devices;

namespace XHTD_SERVICES_XB_TROUGH_1.Hubs
{
    public class Trough1Hub : Hub
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Trough1Hub));

        public void SendMessage(string name, string message)
        {
            try
            {
                var broadcast = GlobalHost.ConnectionManager.GetHubContext<Trough1Hub>();
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
                var broadcast = GlobalHost.ConnectionManager.GetHubContext<Trough1Hub>();
                broadcast.Clients.All.SendNotificationConfirmationPoint(name, status, cardNo, message, vehicle);
            }
            catch (Exception ex)
            {

            }
            //Clients.All.SendNotificationHub(status, inout, cardNo, message, deliveryCode);
        }
    }
}
