using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using XHTD_SERVICES.Data.Repositories;
using XHTD_SERVICES_CONFIRM.Models.Response;
using XHTD_SERVICES.Data.Models.Response;
using System.Runtime.InteropServices;
using XHTD_SERVICES.Device.PLCM221;
using XHTD_SERVICES.Device;
using XHTD_SERVICES.Data.Entities;
using XHTD_SERVICES.Helper;
using Microsoft.AspNet.SignalR.Client;
using System.Threading;
using XHTD_SERVICES.Data.Common;
using Autofac;
using XHTD_SERVICES_CONFIRM.Business;
using XHTD_SERVICES_CONFIRM.Hubs;
using XHTD_SERVICES_CONFIRM.Devices;
using log4net;

namespace XHTD_SERVICES_CONFIRM.Jobs
{
    public class ResetGatewayPLCJob : IJob
    {
        ILog logger = LogManager.GetLogger("SecondFileAppender");

        public ResetGatewayPLCJob(){}

        public async Task Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await Task.Run(() =>
            {
                logger.Info("========= Start reset gateway PLC service =========");

                ResetPLC();
            });                                                                                                                     
        }

        private void ResetPLC()
        {
            DIBootstrapper.Init().Resolve<BarrierControl>().ResetAllOutputPorts();
        }
    }
}
