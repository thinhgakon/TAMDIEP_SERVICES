using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Quartz;

namespace XHTD_SYNC_ORDER.Jobs
{
    public class SyncOrderStatusJob : IJob
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(() =>
            {
                log.Info("log in sync order status job");
                Console.WriteLine("sync order status job ... 2s");
            });
        }
    }
}
