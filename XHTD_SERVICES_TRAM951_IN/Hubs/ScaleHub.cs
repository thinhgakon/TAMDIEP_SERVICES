using System;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Owin;
using Microsoft.Owin.Cors;

namespace XHTD_SERVICES_TRAM951_IN.Hubs
{
    public class ScaleHub : Hub
    {
        public void Send(string name, string message)
        {
            Clients.All.addMessage(name, message);
            Console.WriteLine("send send");
        }
    }
}
