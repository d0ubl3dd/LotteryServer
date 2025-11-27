using log4net;
using System;

namespace BusinessLogic.Logging
{
    public class LoggerService
    {
        private readonly ILog _logger;

        public LoggerService()
        {
            _logger = LogManager.GetLogger("WCFServiceLogger");
        }

        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Warn(string message)
        {
            _logger.Warn(message);
        }

        public void Error(string message, Exception ex = null)
        {
            _logger.Error(message, ex);
        }

        public void Fatal(string message, Exception ex = null)
        {
            _logger.Fatal(message, ex);
        }
    }
}
