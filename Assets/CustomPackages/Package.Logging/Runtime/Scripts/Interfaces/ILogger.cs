using System;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Data;

namespace Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Interfaces
{
    public interface ILogger
    {
        string CategoryName { get; }
        LogLevel MinLogLevel { get; set; }
        void Log(LogLevel logLevel, string message, Exception exception, params object[] args);
    }
}