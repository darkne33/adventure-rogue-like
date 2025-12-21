using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CsvHelper;
using Newtonsoft.Json;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using UnityEngine;

namespace CustomPackages.Package.Extensions
{
    public static class Data
    {
        public static void Save<T>(T value, string key)
        {
            var json = JsonConvert.SerializeObject(value);
            PlayerPrefs.SetString(key, json);
        }

        public static void SaveToCSV<T>(IEnumerable<T> result, string key)
        {
            var fileName = BasePath + $"/{key}.csv";

            using var writer = new StreamWriter(fileName);
            using var csv = new CsvWriter(writer);
            csv.WriteRecords(result);
        }

        public static void SaveToFile<T>(T value, string key)
        {
            var json = JsonConvert.SerializeObject(value, Formatting.Indented);
            var fileName = $"{key}.json";

            if (!Directory.Exists(FileDataPath)) Directory.CreateDirectory(FileDataPath);
            File.WriteAllText(FileDataPath + fileName, json);
        }

        public static void SaveToFileCustomLocation<T>(T value, string key, string path)
        {
            var json = JsonConvert.SerializeObject(value);
            var fileName = $"{key}.json";

            var customLocation = BasePath + path;
            if (!Directory.Exists(customLocation)) Directory.CreateDirectory(customLocation);
            File.WriteAllText(customLocation + fileName, json);
        }

        public static async Task WriteToFileAsync<T>(string key, T value)
        {
            if (!Directory.Exists(FileDataPath)) Directory.CreateDirectory(FileDataPath);
            var json = JsonConvert.SerializeObject(value);
            var fileName = $"{key}.json";
            await using var outputFile = new StreamWriter(Path.Combine(FileDataPath, fileName));
            await outputFile.WriteAsync(json);
        }

        public static T LoadFromFile<T>(string key)
        {
            var fileName = $"{key}.json";
            string path = FileDataPath + fileName;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<T>(json);
            }

            return default;
        }

        public static T LoadFromFile<T>(string key, string location)
        {
            var fileName = $"{key}.json";
            string path = BasePath + location + fileName;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<T>(json);
            }

            Log.Editor.Warn($"File not found with name : {key} on path : {FileDataPath}");
            return default;
        }

        public static void DeleteFile(string key)
        {
            var fileName = $"{key}.json";
            string path = FileDataPath + fileName;
            if (File.Exists(path))
            {
                File.Delete(path);
                return;
            }

            Log.Editor.Warn($"File not found with name : {key} on path : {FileDataPath}");
        }

        public static T Get<T>(string key)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                Log.Editor.Warn($"No exist data with key {key}");
                return default;
            }

            var data = PlayerPrefs.GetString(key);
            return JsonConvert.DeserializeObject<T>(data);
        }

        public static bool Exist(string key) =>
            PlayerPrefs.HasKey(key);

        public static bool ExistOnDisk(string key) =>
            File.Exists($"{FileDataPath}{key}.json");

        public static bool ExistOnDisk(string key, string location) =>
            File.Exists($"{BasePath}{location}{key}.json");

        public static void ExistDirectory(string pathFolder)
        {
            string path = FileDataPath + pathFolder;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        public static void DeleteRootFolder()
        {
            if (Directory.Exists(FileDataPath)) Directory.Delete(FileDataPath, true);
        }

        public static void DeleteRootFolder(string path)
        {
            if (Directory.Exists(BasePath + path)) Directory.Delete(BasePath + path, true);
        }

#if !UNITY_EDITOR
        private static string FileDataPath = BasePath + "/FileData/";
        private static string BasePath => Application.persistentDataPath;
#else
        private static string FileDataPath = BasePath + "/FileData/";
        private static string BasePath => Application.dataPath;
#endif
    }
}