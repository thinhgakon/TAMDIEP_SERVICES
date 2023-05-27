using log4net;
using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHTD_SERVICES.Data.Common;

namespace XHTD_SERVICES_TRAM951_2.Hubs
{
    public partial class SignalRService : IDisposable
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(SignalRService));

        protected readonly string SIGNALR_START_ON_SERVICE_URL = URIConfig.SIGNALR_START_ON_TRAM951_2_SERVICE_URL;

        public SignalRService()
        {
        }

        public void OnStart(string[] args)
        {
            logger.Info("SignalRServiceChat: In OnStart");

            // This will *ONLY* bind to localhost, if you want to bind to all addresses
            // use http://*:8080 to bind to all addresses. 
            // See http://msdn.microsoft.com/library/system.net.httplistener.aspx 
            // for more information.
            try
            {
                WebApp.Start(SIGNALR_START_ON_SERVICE_URL);

                logger.Info($"Server running on {SIGNALR_START_ON_SERVICE_URL}");
            }
            catch (Exception ex)
            {
                logger.Info($"Server running error: {ex.StackTrace} ------------ {ex.InnerException} ------------ {ex.Message}");
            }
        }

        public void OnStop()
        {
            logger.Info("SignalRServiceChat: In OnStop");
        }

        public void Dispose()
        {
        }
    }
}
