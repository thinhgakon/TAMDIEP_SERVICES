using System;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Owin;
using Microsoft.Owin.Cors;
using log4net;
using XHTD_SERVICES.Helper;
using System.Linq;
using XHTD_SERVICES.Data.Entities;
using Autofac;
using XHTD_SERVICES_TRAM951_IN.Devices;

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
                Program.scaleValues1.Clear();
                return;
            }

            if (Program.IsScalling1)
            {
                Program.scaleValues1.Add(currentScaleValue);

                if (Program.scaleValues1.Count > 10)
                {
                    Program.scaleValues1.RemoveRange(0, 1);
                }

                var isOnDinh = Calculator.CheckBalanceValues(Program.scaleValues1, 20);

                // var scaleText = String.Join(",", Program.scaleValues1);
                // logger.Info("Gia tri can: " + scaleText);

                if (isOnDinh)
                {
                    Program.IsScalling1 = false;
                    logger.Info($"Can 1 on dinh: " + currentScaleValue);

                    // Thuc hien khi da lay duoc gia tri can on dinh
                    using (var dbContext = new XHTD_Entities())
                    {
                        var scaleInfo = dbContext.tblScaleOperatings.FirstOrDefault(x => x.ScaleCode == "SCALE-1");
                        if ((bool)scaleInfo.IsScaling)
                        {
                            // Đang cân vào
                            if ((bool)scaleInfo.ScaleIn)
                            {
                                // Update giá trị cân không tải của phương tiện

                                // Bật đèn đỏ
                                // Đóng barrier
                                DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnRedTrafficLight("IN");
                                DIBootstrapper.Init().Resolve<BarrierControl>().CloseBarrier("IN");

                                // Gọi iERP API lưu giá trị cân

                                // Bật đèn xanh
                                // Mở barrier
                                DIBootstrapper.Init().Resolve<TrafficLightControl>().TurnOnGreenTrafficLight("IN");
                                DIBootstrapper.Init().Resolve<BarrierControl>().OpenBarrier("IN");

                                // Update giá trị cân của đơn hàng

                                // Giải phóng cân
                                Program.IsScalling1 = false;
                            }

                            // Đang cân ra
                            if ((bool)scaleInfo.ScaleOut)
                            {
                                // Bật đèn đỏ
                                // Đóng barrier

                                // Gọi iERP API lưu giá trị cân

                                // Bật đèn xanh
                                // Mở barrier

                                // Update giá trị cân của đơn hàng

                                // Giải phóng cân
                                Program.IsScalling1 = false;
                            }
                        }
                    }
                }
            }
            else
            {
                if (Program.scaleValues1.Count > 5)
                {
                    Program.scaleValues1.Clear();
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
