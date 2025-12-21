using System;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Data;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Interfaces;

namespace Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Extensions
{
    internal static class LoggerExtensions
    {
        public static void Verbose(this ILogger logger, string message, params object[] args)
        {
            logger.Log(LogLevel.Verbose, message, null, args);
        }

        public static void Debug(this ILogger logger, string message, params object[] args)
        {
            logger.Log(LogLevel.Debug, message, null, args);
        }

        public static void Info(this ILogger logger, string message, params object[] args)
        {
            logger.Log(LogLevel.Info, message, null, args);
        }

        public static void Warn(this ILogger logger, string message, params object[] args)
        {
            logger.Log(LogLevel.Warn, message, null, args);
        }

        public static void Error(this ILogger logger, string message, params object[] args)
        {
            logger.Log(LogLevel.Error, message, null, args);
        }

        public static void Error(this ILogger logger, string message, Exception exception)
        {
            logger.Log(LogLevel.Error, message, exception, null);
        }

        public static void Fatal(this ILogger logger, Exception exception)
        {
            logger.Log(LogLevel.Fatal, string.Empty, exception, null);
        }
    }
}