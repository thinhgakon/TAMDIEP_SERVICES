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
using XHTD_SERVICES_GATEWAY_OUT.Business;
using System.Threading;
using XHTD_SERVICES_GATEWAY_OUT.Devices;

namespace XHTD_SERVICES_GATEWAY_OUT.Hubs
{
    public class GatewayHub : Hub
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(GatewayHub));

        public void SendMessage(string name, string message)
        {
            try
            {
                _logger.Info($"name: {name} - message: {message}");

                //var broadcast = GlobalHost.ConnectionManager.GetHubContext<GatewayHub>();
                //broadcast.Clients.All.SendMessage(name, message);
            }
            catch (Exception ex)
            {

            }
        }

        public void SendNotificationCBV(int status, string inout, string cardNo, string message, string vehicle = null)
        {
            try
            {
                var broadcast = GlobalHost.ConnectionManager.GetHubContext<GatewayHub>();
                broadcast.Clients.All.SendNotificationCBV(status, inout, cardNo, message, vehicle);
            }
            catch (Exception ex)
            {

            }
            //Clients.All.SendNotificationCBV(status, inout, cardNo, message, deliveryCode);
        }

        public void OpenManualBarrierIn(string name)
        {
            _logger.Info("Open Manual Barrier In");
            bool isOpened = DIBootstrapper.Init().Resolve<BarrierControl>().OpenBarrierScaleIn();
            if (isOpened)
            {
                _logger.Info("Mở thành công");
            }
            else
            {
                _logger.Info("Mở thất bại");
            }
        }

        public void OpenManualBarrierOut(string name)
        {
            _logger.Info("Open Manual Barrier Out");
            bool isOpened = DIBootstrapper.Init().Resolve<BarrierControl>().OpenBarrierScaleOut();
            if (isOpened)
            {
                _logger.Info("Mở thành công");
            }
            else
            {
                _logger.Info("Mở thất bại");
            }
        }
    }
}
