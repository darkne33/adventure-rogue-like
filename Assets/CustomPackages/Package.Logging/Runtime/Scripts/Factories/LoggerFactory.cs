using System.Collections.Concurrent;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Configs;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Interfaces;

namespace Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Factories
{
    public static class LoggerFactory
    {
        private static readonly ConcurrentDictionary<string, Logger> _loggers = new();

        public static ILogger CreateLogger(string categoryName)
        {
            var logger = _loggers.GetOrAdd(categoryName, x => new Logger(x, false));
            SetMinLogLevel(logger);

            return logger;
        }

        public static void UpdateMinLogLevels()
        {
            foreach (var logger in _loggers.Values)
            {
                foreach (var config in LoggingConfig.Instance.Data)
                {
                    if (logger.CategoryName != config.CategoryName) continue;
                    logger.MinLogLevel = config.LogLevel;

                    break;
                }
            }
        }

        private static void SetMinLogLevel(ILogger logger)
        {
            var data = LoggingConfig.Instance.Data;

            foreach (var item in data)
            {
                if (logger.CategoryName != item.CategoryName) continue;
                logger.MinLogLevel = item.LogLevel;

                break;
            }
        }
    }
}