using Autofac;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES_LED.Devices;
using XHTD_SERVICES_LED.Models.Values;

namespace XHTD_SERVICES_LED.Jobs
{
    public class Led1XiBaoJob : IJob
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly string SCALE_1_LED_IN_CODE = ScaleLedCode.CODE_SCALE_1_LED_IN;

        protected readonly string SCALE_1_LED_OUT_CODE = ScaleLedCode.CODE_SCALE_1_LED_OUT;

        protected readonly string IP_ADDRESS = "192.168.13.210";
        private const int BUFFER_SIZE = 1024;
        protected readonly int PORT_NUMBER = 10000;

        static ASCIIEncoding encoding = new ASCIIEncoding();

        public Led1XiBaoJob()
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
                LEDProcess();
            });
        }

        public void LEDProcess()
        {
            try
            {
                TcpClient client = new TcpClient();
                client.Connect(IP_ADDRESS, PORT_NUMBER);
                Stream stream = client.GetStream();

                while (true)
                {
                    Console.WriteLine($"Connected to PLC {IP_ADDRESS}");
                    Console.WriteLine($"Reading PLC {IP_ADDRESS}...");

                    byte[] data1 = encoding.GetBytes($"*[Count][MX][5]##GET[!]");
                    stream.Write(data1, 0, data1.Length);

                    data1 = new byte[BUFFER_SIZE];
                    stream.Read(data1, 0, BUFFER_SIZE);

                    var response = encoding.GetString(data1).Trim();
                    var responseArr = response.Split(';');

                    if (responseArr == null || responseArr.Length == 0)
                    {
                        Console.WriteLine($"Khong co du lieu tra ve");
                        return;
                    }

                    Match match = Regex.Match(responseArr.First(), @"\[(\d+)\].*#(\d+)#");

                    if (match.Success)
                    {
                        Console.WriteLine($"Máng {match.Groups[1].Value}: {match.Groups[2].Value} bao");
                    }

                    Thread.Sleep(5000);
                }
            }
            catch (Exception ex)
            {
                log.Info($"ERROR: {ex.Message}");
            }
        }

        public void DisplayScreenLed(string dataCode)
        {
            //log.Info($"Send led: dataCode= {dataCode}");

            if (DIBootstrapper.Init().Resolve<TCPLedControl>().DisplayScreen(SCALE_1_LED_IN_CODE, dataCode))
            {
                //log.Info("LED IN 1 Job - OK");
            }
            else
            {
                log.Info($"LED IN 1 Job - FAILED: dataCode={dataCode}");
            }

            Thread.Sleep(500);

            if (DIBootstrapper.Init().Resolve<TCPLedControl>().DisplayScreen(SCALE_1_LED_OUT_CODE, dataCode))
            {
                //log.Info("LED OUT 1 Job - OK");
            }
            else
            {
                log.Info($"LED OUT 1 Job - FAILED: dataCode={dataCode}");
            }
        }
    }
}
