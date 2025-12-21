using System;
using System.Text;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Data;
using UnityEngine;
using Interfaces_ILogger = Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Interfaces.ILogger;

namespace Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core
{
    internal class Logger : Interfaces_ILogger
    {
        private static readonly StringBuilder _logBuilder = new();

        public string CategoryName { get; }
        public bool WithTimestamp { get; }

        public LogLevel MinLogLevel { get; set; } = LogLevel.Verbose;
        private static string Timestamp => DateTime.Now.ToString("HH:mm:ss.fff");

        public Logger(string categoryName, bool withTimestamp)
        {
            CategoryName = categoryName;
            WithTimestamp = withTimestamp;
        }

        public void Log(LogLevel logLevel, string message, Exception exception = null, params object[] args)
        {
            lock (_logBuilder)
            {
                _logBuilder.Clear();

                if (WithTimestamp)
                {
                    _logBuilder
                        .Append("[")
                        .Append(Timestamp)
                        .Append("] ")
                        .Append(GetLogLevelCode(logLevel))
                        .Append(" [")
                        .Append(CategoryName)
                        .Append("] ")
                        .Append(message);
                }
                else
                {
                    _logBuilder
                        .Append(GetLogLevelCode(logLevel))
                        .Append(" [")
                        .Append(CategoryName)
                        .Append("] ")
                        .Append(message);
                }

                if (exception != null)
                {
                    _logBuilder.AppendLine(" " + exception);
                }
                else
                {
                    _logBuilder.AppendLine();
                }

                var output = _logBuilder.ToString();
                var isArgsNull = args == null || args.Length == 0;

                switch (logLevel)
                {
                    case LogLevel.Verbose:
                        if (isArgsNull)
                        {
                            Debug.Log(output);
                        }
                        else
                        {
                            Debug.LogFormat(output, args);
                        }

                        break;
                    case LogLevel.Debug:
                        if (isArgsNull)
                        {
                            Debug.Log(output);
                        }
                        else
                        {
                            Debug.LogFormat(output, args);
                        }

                        break;
                    case LogLevel.Info:
                        if (isArgsNull)
                        {
                            Debug.Log(output);
                        }
                        else
                        {
                            Debug.LogFormat(output, args);
                        }

                        break;
                    case LogLevel.Warn:
                        if (isArgsNull)
                        {
                            Debug.LogWarning(output);
                        }
                        else
                        {
                            Debug.LogWarningFormat(output, args);
                        }

                        break;
                    case LogLevel.Error:
                        if (isArgsNull)
                        {
                            Debug.LogError(output);
                        }
                        else
                        {
                            Debug.LogErrorFormat(output, args);
                        }

                        break;
                    case LogLevel.Fatal:
                        if (isArgsNull)
                        {
                            Debug.LogError(output);
                        }
                        else
                        {
                            Debug.LogErrorFormat(output, args);
                        }

                        break;
                    case LogLevel.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
                }
            }
        }

        private static string GetLogLevelCode(LogLevel logLevel)
        {
#if UNITY_EDITOR
            return logLevel switch
            {
                LogLevel.Verbose => "<color=#5B942C>[Verbose]</color>",
                LogLevel.Debug => "<color=#3993D4>[Debug]</color>",
                LogLevel.Info => "<color=#00FFE1>[Info]</color>",
                LogLevel.Warn => "<color=#A3880E>[Warn]</color>",
                LogLevel.Error => "<color=#F0524F>[Error]</color>",
                LogLevel.Fatal => "<color=#F0524F>[Fatal]</color>",
                _ => "N"
            };
#elif !UNITY_EDITOR
            return logLevel switch
            {
                LogLevel.Verbose => "[Verbose]",
                LogLevel.Debug => "[Debug]",
                LogLevel.Info => "[Info]",
                LogLevel.Warn => "[Warn]",
                LogLevel.Error => "[Error]",
                LogLevel.Fatal => "[Fatal]",
                _ => "N"
            };
#endif
        }
    }
}