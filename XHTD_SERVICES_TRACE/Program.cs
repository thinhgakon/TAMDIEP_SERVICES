using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace XHTD_SERVICES_TRACE
{
    public static class Program
    {
        public static string SignalRUrl = "http://117.4.184.50:7000/SystemTraceRequest";
        public static HubConnection HubConnection = new HubConnectionBuilder()
                  .WithUrl(SignalRUrl)
                  .Build();
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
