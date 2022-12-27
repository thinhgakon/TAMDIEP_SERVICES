using System;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Owin;
using Microsoft.Owin.Cors;
using log4net;
using XHTD_SERVICES.Helper;
using System.Linq;

namespace XHTD_SERVICES_TRAM951_IN.Hubs
{
    public class ScaleHub : Hub
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(ScaleHub));

        public void Send(string name, string message)
        {
            Clients.All.addMessage(name, message);
            Console.WriteLine("send send");
        }

        public void Send9511ScaleInfo(DateTime time, string value)
        {
            // Call the broadcastMessage method to update clients.
            Clients.All.Send9511ScaleInfo(time, value);
            ReadDataScale9511(time, value);
        }

        public void Send9512ScaleInfo(DateTime time, string value)
        {
            // Call the broadcastMessage method to update clients.
            Clients.All.Send9512ScaleInfo(time, value);
            ReadDataScale9512(time, value);
        }

        public void SendClinkerScaleInfo(DateTime time, string value)
        {
            // Call the broadcastMessage method to update clients.
            Clients.All.SendClinkerScaleInfo(time, value);
        }

        public void ReadDataScale9511(DateTime time, string value)
        {
            logger.Info($"Received 951-1 data: time={time}, value={value}");

            int currentScaleValue = Int32.Parse(value);
            if (currentScaleValue < 1000)
            {
                Program.IsScalling1 = false;
                Program.scaleValues1.Clear();
                return;
            }
            else
            {
                Program.IsScalling1 = true;
            }

            if (Program.IsScalling1) {

                Program.scaleValues1.Add(currentScaleValue);

                var isOnDinh = Calculator.CheckBalanceValues(Program.scaleValues1, 20);

                if (Program.scaleValues1.Count > 10)
                {
                    Program.scaleValues1.RemoveRange(0, 1);
                }

                var scaleText = String.Join(",", Program.scaleValues1);
                logger.Info("Gia tri can: " + scaleText);

                if (isOnDinh)
                {
                    Program.IsScalling1 = false;
                    logger.Info($"Can on dinh: " + Program.scaleValues1.LastOrDefault().ToString());
                }
            }
        }

        public void ReadDataScale9512(DateTime time, string value)
        {
            logger.Info($"Received 951-2 data: time={time}, value={value}");

            int currentScaleValue = Int32.Parse(value);
            if (currentScaleValue < 1000)
            {
                Program.IsScalling2 = false;
                Program.scaleValues2.Clear();
                return;
            }
            else
            {
                Program.IsScalling2 = true;
            }

            if (Program.IsScalling2)
            {

                Program.scaleValues2.Add(currentScaleValue);

                var isOnDinh = Calculator.CheckBalanceValues(Program.scaleValues2, 20);

                if (Program.scaleValues2.Count > 10)
                {
                    Program.scaleValues2.RemoveRange(0, 1);
                }

                var scaleText = String.Join(",", Program.scaleValues2);
                logger.Info("Gia tri can: " + scaleText);

                if (isOnDinh)
                {
                    Program.IsScalling1 = false;
                    logger.Info($"Can on dinh: " + Program.scaleValues2.LastOrDefault().ToString());
                }
            }
        }
    }
}
