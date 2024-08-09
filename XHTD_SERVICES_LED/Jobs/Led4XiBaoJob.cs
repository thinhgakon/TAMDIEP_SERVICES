using Autofac;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES_LED.Devices;
using XHTD_SERVICES_LED.Models.Values;

namespace XHTD_SERVICES_LED.Jobs
{
    public class Led4XiBaoJob : IJob
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly string SCALE_1_LED_IN_CODE = ScaleLedCode.CODE_SCALE_1_LED_IN;

        protected readonly string SCALE_1_LED_OUT_CODE = ScaleLedCode.CODE_SCALE_1_LED_OUT;

        public Led4XiBaoJob()
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
                if (Program.listScale1 == null || Program.listScale1.Count == 0)
                {
                    if (Program.IsFirstTimeResetLed1)
                    {
                        Program.IsFirstTimeResetLed1 = false;

                        string emptyDataCode = $"*[H1][C1]----[H2][C1]----[H3][C1]----[H4][C1]----[!]";
                        DisplayScreenLed(emptyDataCode);

                        log.Info("Reset led");
                    }
                    else
                    {
                        //log.Info("Khong co xe dang can => return");
                    }

                    return;
                }
                else
                {
                    Program.IsFirstTimeResetLed1 = true;
                }

                var weightText = Program.listScale1.LastOrDefault()?.WeightCurrent;
                var vehicleText = Program.ScalingVehicle1.ToUpper();

                //var sensorCheck = DIBootstrapper.Init().Resolve<BarrierScaleBusiness>().CheckSensorCovered(true);
                var sensorText = false ? "CO" : "KHONG";

                var validCheck = !string.IsNullOrEmpty(vehicleText) && true;
                var validText = validCheck ? "Hop Le" : "Chua Hop Le";

                var colorCode = "C1";
                if (validCheck)
                {
                    colorCode = "C2";
                }

                string dataCode = $"*[H1][C1]{weightText}[H2][C1]{vehicleText}[H3][{colorCode}]{sensorText}[H4][{colorCode}]{validText}[!]";

                DisplayScreenLed(dataCode);
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
