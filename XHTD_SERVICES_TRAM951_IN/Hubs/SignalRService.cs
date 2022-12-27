﻿using log4net;
using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_TRAM951_IN.Hubs
{
    public partial class SignalRService : IDisposable
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(SignalRService));

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
            //string url = "http://10.0.1.41:8080";
            string url = "http://localhost:8080";

            WebApp.Start(url);

            logger.Info($"Server running on {url}");
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
