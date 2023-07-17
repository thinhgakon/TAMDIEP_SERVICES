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

namespace XHTD_SERVICES_TRAM951_1.Jobs
{
    public class Reset951PLCJob : IJob
    {
        protected readonly PLCBarrier _barrier;

        protected readonly Logger _logger;

        public Reset951PLCJob(
            PLCBarrier barrier,
            Logger logger
            )
        {
            _barrier = barrier;
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
                ResetPLC();
            });
        }
        private void ResetPLC()
        {
            DIBootstrapper.Init().Resolve<BarrierControl>().ResetAllOutputPorts();
        }
    }
}
