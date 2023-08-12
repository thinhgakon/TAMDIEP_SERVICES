using System;
using System.Threading.Tasks;
using Quartz;
using System.Net.NetworkInformation;
using log4net;

namespace XHTD_SERVICES_GATEWAY.Jobs
{
    public class PingJob : IJob
    {
        ILog logger = LogManager.GetLogger("SecondFileAppender");

        protected const string IP_ADDRESS = "10.0.9.1";

        public PingJob(
            )
        {
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(() =>
            {
                logger.Info("-------- Start ping server --------");

                PingServer();
            });                                                                                                                     
        }

        private void PingServer()
        {
            Ping myPing = new Ping();
            PingReply reply = myPing.Send(IP_ADDRESS, 2000);

            if (reply != null)
            {
                Console.WriteLine("Address: " + reply.Address + " - Status:  " + reply.Status + " - Time : " + reply.RoundtripTime.ToString());
                logger.Info("Address: " + reply.Address + " - Status:  " + reply.Status + " - Time : " + reply.RoundtripTime.ToString());
            }
            else
            {
                Console.WriteLine("Khong nhan duoc tin hieu ping");
                logger.Info("Khong nhan duoc tin hieu ping");
            }
        }
    }
}
