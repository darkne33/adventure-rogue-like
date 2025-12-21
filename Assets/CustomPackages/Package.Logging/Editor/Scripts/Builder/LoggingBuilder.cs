using System.Collections.Generic;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Configs;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Data;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Package.Logging.Editor.CustomPackages.Package.Logging.Editor.Scripts.Builder
{
    public class LoggingBuilder : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private bool _isProduction;
        private readonly List<LoggingData> _cached = new();

        public int callbackOrder { get; } = int.MaxValue;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (LoggingConfig.Instance == null)
            {
                return;
            }

            var count = LoggingConfig.Instance.Data.Count;

            if (_cached.Count != count)
            {
                return;
            }

            for (var index = 0; index < count; index++)
            {
                LoggingConfig.Instance.Data[index] = _cached[index];
            }

            _cached.Clear();

            LoggingConfig.Instance.Save();
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (LoggingConfig.Instance == null)
            {
                return;
            }

            _isProduction = IsProduction();

            foreach (var data in LoggingConfig.Instance.Data)
            {
                _cached.Add(new LoggingData
                {
                    CategoryName = data.CategoryName,
                    LogLevel = data.LogLevel,
                    IsLockDuringBuild = data.IsLockDuringBuild
                });

                if (_isProduction && !data.IsLockDuringBuild)
                {
                    data.LogLevel = LogLevel.Error;
                }
            }

            LoggingConfig.Instance.Save();
        }

        private static bool IsProduction()
        {
            return LoggingConfig.Instance.Credentials == LoggingCredentials.Production;
        }
    }
}