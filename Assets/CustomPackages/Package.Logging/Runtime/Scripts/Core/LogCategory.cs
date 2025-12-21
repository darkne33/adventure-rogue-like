using System;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Data;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Extensions;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Factories;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Interfaces;

namespace Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core
{
    public abstract class LogCategory<T> where T : class
    {
        private static readonly ILogger _logger;
        private static string CategoryName => typeof(T).Name;
        public static LogLevel LogLevel => _logger.MinLogLevel;

        static LogCategory()
        {
            var type = typeof(T);

            if (type.BaseType != typeof(LogCategory<T>))
            {
                throw new InvalidProgramException($"Categorical log type {typeof(LogCategory<T>)} should have recursive onto self");
            }

            _logger = LoggerFactory.CreateLogger(CategoryName);
        }

        public static void Verbose(string message, params object[] args)
        {
            if (IsEnabled(LogLevel.Verbose))
            {
                _logger.Verbose(message, args);
            }
        }

        public static void Verbose<T1>(string message, T1 arg1)
        {
            if (IsEnabled(LogLevel.Verbose))
            {
                _logger.Verbose(message, arg1);
            }
        }

        public static void Verbose<T1, T2>(string message, T1 arg1, T2 arg2)
        {
            if (IsEnabled(LogLevel.Verbose))
            {
                _logger.Verbose(message, arg1, arg2);
            }
        }

        public static void Verbose<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3)
        {
            if (IsEnabled(LogLevel.Verbose))
            {
                _logger.Verbose(message, arg1, arg2, arg3);
            }
        }

        public static void Debug(string message, params object[] args)
        {
            if (IsEnabled(LogLevel.Debug))
            {
                _logger.Debug(message, args);
            }
        }

        public static void Debug<T1>(string message, T1 arg1)
        {
            if (IsEnabled(LogLevel.Debug))
            {
                _logger.Debug(message, arg1);
            }
        }

        public static void Debug<T1, T2>(string message, T1 arg1, T2 arg2)
        {
            if (IsEnabled(LogLevel.Debug))
            {
                _logger.Debug(message, arg1, arg2);
            }
        }

        public static void Debug<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3)
        {
            if (IsEnabled(LogLevel.Debug))
            {
                _logger.Debug(message, arg1, arg2, arg3);
            }
        }

        public static void Info(string message, params object[] args)
        {
            if (IsEnabled(LogLevel.Info))
            {
                _logger.Info(message, args);
            }
        }

        public static void Info<T1>(string message, T1 arg1)
        {
            if (IsEnabled(LogLevel.Info))
            {
                _logger.Info(message, arg1);
            }
        }

        public static void Info<T1, T2>(string message, T1 arg1, T2 arg2)
        {
            if (IsEnabled(LogLevel.Info))
            {
                _logger.Info(message, arg1, arg2);
            }
        }

        public static void Info<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3)
        {
            if (IsEnabled(LogLevel.Info))
            {
                _logger.Info(message, arg1, arg2, arg3);
            }
        }

        public static void Warn(string message, params object[] args)
        {
            if (IsEnabled(LogLevel.Warn))
            {
                _logger.Warn(message, args);
            }
        }

        public static void Warn<T1>(string message, T1 arg1)
        {
            if (IsEnabled(LogLevel.Warn))
            {
                _logger.Warn(message, arg1);
            }
        }

        public static void Warn<T1, T2>(string message, T1 arg1, T2 arg2)
        {
            if (IsEnabled(LogLevel.Warn))
            {
                _logger.Warn(message, arg1, arg2);
            }
        }

        public static void Warn<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3)
        {
            if (IsEnabled(LogLevel.Warn))
            {
                _logger.Warn(message, arg1, arg2, arg3);
            }
        }

        public static void Error(string message, params object[] args)
        {
            if (IsEnabled(LogLevel.Error))
            {
                _logger.Error(message, args);
            }
        }

        public static void Error<T1>(string message, T1 arg1)
        {
            if (IsEnabled(LogLevel.Error))
            {
                _logger.Error(message, arg1);
            }
        }

        public static void Error<T1, T2>(string message, T1 arg1, T2 arg2)
        {
            if (IsEnabled(LogLevel.Error))
            {
                _logger.Error(message, arg1, arg2);
            }
        }

        public static void Error<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3)
        {
            if (IsEnabled(LogLevel.Error))
            {
                _logger.Error(message, arg1, arg2, arg3);
            }
        }

        public static void Error(string message, Exception exception)
        {
            if (IsEnabled(LogLevel.Error))
            {
                _logger.Error(message, exception);
            }
        }

        public static void Fatal(Exception exception)
        {
            if (IsEnabled(LogLevel.Fatal))
            {
                _logger.Fatal(exception);
            }
        }

        private static bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _logger.MinLogLevel;
        }
    }
}