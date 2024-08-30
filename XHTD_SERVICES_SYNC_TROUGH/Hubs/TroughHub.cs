using log4net;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_SYNC_TROUGH.Hubs
{
    public class TroughHub : Hub
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(TroughHub));

        public void SendTroughData(string troughType, string deliveryCode, string machineCode, string troughCode, int? firstQuantity, int? lastQuantity)
        {
            try
            {
                var broadcast = GlobalHost.ConnectionManager.GetHubContext<TroughHub>();
                broadcast.Clients.All.SendTroughData(troughType, deliveryCode, machineCode, troughCode, firstQuantity, lastQuantity);
            }
            catch (Exception ex)
            {

            }
        }
    }
}
