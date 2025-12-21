using System;
using System.Diagnostics.CodeAnalysis;

namespace Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Data
{
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class LoggingData
    {
        public string CategoryName;
        public LogLevel LogLevel;
        public bool IsLockDuringBuild;
    }
}