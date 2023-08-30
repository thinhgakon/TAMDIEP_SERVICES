using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using Autofac;
using System.Runtime.InteropServices;
using XHTD_SERVICES.Data.Common;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Data.Models.Values;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_TRAM951_1.Models.Response;
using XHTD_SERVICES_TRAM951_1.Hubs;
using XHTD_SERVICES_TRAM951_1.Devices;
using XHTD_SERVICES_TRAM951_1.Business;
using XHTD_SERVICES.Helper;
using XHTD_SERVICES.Device.PLCM221;
using log4net;

namespace XHTD_SERVICES_TRAM951_1.Jobs
{
    public class Reset951PLCJob : IJob
    {
        ILog logger = LogManager.GetLogger("SecondFileAppender");

        public Reset951PLCJob(){}

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(() =>
            {
                logger.Info("========= Start reset 951 PLC service =========");

                ResetPLC();
            });
        }
        private void ResetPLC()
        {
            DIBootstrapper.Init().Resolve<BarrierControl>().ResetAllOutputPorts();
        }
    }
}
