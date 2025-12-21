using System;
using System.Collections.Generic;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Data;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Utilities;
using UnityEditor;
using UnityEngine;

namespace Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Configs
{
    [CreateAssetMenu(fileName = "LoggingConfig", menuName = "Tools/Packages/LoggingConfig")]
    public class LoggingConfig : SingletonScriptableObject<LoggingConfig>
    {
        [field: SerializeField] public List<LoggingData> Data { get; set; }
        [field: SerializeField] public LoggingCredentials Credentials { get; set; }

        public void Save()
        {
#if UNITY_EDITOR
            Data.Sort((a, b) => string.Compare(a.CategoryName, b.CategoryName, StringComparison.Ordinal));
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
#endif
        }
    }
}