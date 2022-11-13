using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Repository.Hierarchy;

namespace XHTD_SERVICES.Helper
{
    public abstract class BaseLogger<T> where T : class
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(T));

        public void LogDebug(string message)
        {
            logger.Debug(message);
            Console.WriteLine(message);
        }

        public void LogError(string message)
        {
            logger.Error(message);
            Console.WriteLine(message);
        }

        public void LogInfo(string message)
        {
            logger.Info(message);
            Console.WriteLine(message);
        }

        public void LogWarn(string message)
        {
            logger.Warn(message);
            Console.WriteLine(message);
        }

        public void LogConsole(string message)
        {
            Console.WriteLine(message);
        }
    }
}
