using Quartz;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES_TRAM951_2.Devices;

namespace XHTD_SERVICES_TRAM951_2.Jobs
{
    public class ConnectPegasusJob : IJob
    {
        public static int RefPort1 = 6000;
        public static byte RefComAdr1 = 0xFF;
        public static int RefPort2 = 6000;
        public static byte RefComAdr2 = 0xFF;

        protected readonly Logger _logger;

        public ConnectPegasusJob(Logger logger)
        {
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(() =>
            {
                CheckConnection();
            });
        }

        public void CheckConnection()
        {
            try
            {
                Ping pingSender = new Ping();
                PingReply reply = pingSender.Send(Program.PegasusIP1);

                if (reply.Status == IPStatus.Success)
                {
                    //Console.WriteLine("Connection ok");
                }
                else
                {
                    var openresult = PegasusReader.Connect(RefPort1, Program.PegasusIP1, ref Program.RefComAdr1, ref Program.RefPort1);
                    while (openresult != 0)
                    {
                        PegasusReader2.Close(Program.RefPort1);
                        PegasusReader.Connect(RefPort1, Program.PegasusIP1, ref Program.RefComAdr1, ref Program.RefPort1);
                        Thread.Sleep(1000);
                    }
                    _logger.LogWarn($"Connect {Program.PegasusIP1} fail. Start reconnect");
                }

                if (reply.Status == IPStatus.Success)
                {
                    //Console.WriteLine("Connection ok");
                }
                else
                {
                    var openresult = PegasusReader2.Connect(RefPort2, Program.PegasusIP2, ref Program.RefComAdr2, ref Program.RefPort2);
                    while (openresult != 0)
                    {
                        PegasusReader2.Close(Program.RefPort2);
                        openresult = PegasusReader2.Connect(RefPort2, Program.PegasusIP2, ref Program.RefComAdr2, ref Program.RefPort2);
                        Thread.Sleep(1000);
                    }
                    _logger.LogWarn($"Connect {Program.PegasusIP2} fail. Start reconnect");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }
    }
}
