using Autofac;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XHTD_SERVICES_CANRA_1.Devices;

namespace XHTD_SERVICES_CANRA_1.Jobs
{
    public class LedJob : IJob
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly string SCALE_2_LED_IN_IP = "192.168.13.180";

        protected readonly string SCALE_2_LED_OUT_IP = "192.168.13.186";

        public LedJob()
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
                if (Program.scaleValues == null || Program.scaleValues.Count == 0)
                {
                    if (Program.IsFirstTimeResetLed)
                    {
                        Program.IsFirstTimeResetLed = false;

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
                    Program.IsFirstTimeResetLed = true;
                }

                var weightText = Program.scaleValues.LastOrDefault();
                var vehicleText = Program.InProgressVehicleCode.ToUpper();

                var sensorCheck = true;//DIBootstrapper.Init().Resolve<BarrierScaleBusiness>().CheckSensorCovered(true);
                var sensorText = sensorCheck ? "CO" : "KHONG";

                var validCheck = !string.IsNullOrEmpty(vehicleText) && !sensorCheck;
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

            if (DIBootstrapper.Init().Resolve<TCPLedControl>().DisplayScreen(SCALE_2_LED_IN_IP, dataCode))
            {
                log.Info("LED IN Job - OK");
            }
            else
            {
                log.Info($"LED IN Job - FAILED: dataCode={dataCode}");
            }

            Thread.Sleep(500);

            if (DIBootstrapper.Init().Resolve<TCPLedControl>().DisplayScreen(SCALE_2_LED_OUT_IP, dataCode))
            {
                log.Info("LED OUT Job - OK");
            }
            else
            {
                log.Info($"LED OUT Job - FAILED: dataCode={dataCode}");
            }
        }
    }
}
