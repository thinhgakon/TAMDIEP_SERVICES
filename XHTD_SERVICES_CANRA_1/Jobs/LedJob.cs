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

        protected readonly string SCALE_2_LED_IP = "192.168.13.186";

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
                if (Program.scaleValuesForResetLight == null || Program.scaleValuesForResetLight.Count == 0)
                {
                    if (Program.IsFirstTimeResetLed)
                    {
                        Program.IsFirstTimeResetLed = false;

                        string emptyDataCode = $"*[H1][C1]HE THONG CAN TU DONG[H2][C1]---[H3][C1]---[H4][Cy]---[!]";
                        DisplayScreenLed(emptyDataCode);

                        log.Info("Reset led");
                    }
                    else
                    {
                        log.Info("Khong co xe dang can => Ket thuc");
                    }

                    return;
                }
                else
                {
                    Program.IsFirstTimeResetLed = true;
                }

                var weightText = Program.scaleValuesForResetLight.LastOrDefault();
                var vehicleText = Program.InProgressVehicleCode != null ? Program.InProgressVehicleCode.ToUpper() : "HE THONG CAN TU DONG";

                string dataCode = $"*[H1][C1]{vehicleText}[H2][C1]{weightText}[H3][C1]---[H4][Cy]---[!]";
                DisplayScreenLed(dataCode);
            }
            catch (Exception ex)
            {
                log.Info($"ERROR: {ex.Message}");
            }
        }

        public void DisplayScreenLed(string dataCode)
        {
            log.Info($"Send led: dataCode= {dataCode}");

            if (DIBootstrapper.Init().Resolve<TCPLedControl>().DisplayScreen(SCALE_2_LED_IP, dataCode))
            {
                log.Info("LED CAN RA - OK");
            }
            else
            {
                log.Info($"LED CAN RA - FAILED: dataCode={dataCode}");
            }
        }
    }
}
