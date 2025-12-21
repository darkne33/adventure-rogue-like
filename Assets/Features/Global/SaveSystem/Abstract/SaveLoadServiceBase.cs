using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using Data = CustomPackages.Package.Extensions.Data;

namespace Infrastructure.SaveSystem.Implementation
{
    public abstract class SaveLoadServiceBase<T> : ISaveLoadService where T : class, new()
    {
        protected abstract string SaveKey { get; }
        protected abstract string CollectionName { get; }
        protected T LoadedData { get; private set; }

        private readonly List<ISaveReader<T>> _saveReaders;
        private readonly List<ISaveWriter<T>> _saveWriters;

        protected SaveLoadServiceBase()
        {
            _saveWriters = new List<ISaveWriter<T>>();
            _saveReaders = new List<ISaveReader<T>>();

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
        }

        public async UniTask InitializeAndLoad()
        {
            LoadedData = await LoadDataFromNakama() ?? CreateEmptySavedData();
            Load();
        }

        public UniTask Clear()
        {
            Log.SaveSystem.Info($"Delete save for {CollectionName}_{SaveKey}");
            LoadedData = null;
            return UniTask.CompletedTask;
        }

        public void Register(object subject)
        {
            if (subject is ISaveWriter<T> writer) _saveWriters.Add(writer);
            if (subject is ISaveReader<T> reader) _saveReaders.Add(reader);
        }

        public void Unregister(object subject)
        {
            if (subject is ISaveWriter<T> writer)
            {
                _saveWriters.Remove(writer);
            }
            else
            {
                Log.Gameplay.Warn($"Try to unregister {subject} that is not ISaveWriter<T>");
            }

            if (subject is ISaveReader<T> reader)
            {
                _saveReaders.Remove(reader);
            }
            else
            {
                Log.Gameplay.Warn($"Try to unregister {subject} that is not ISaveReader<T>");
            }
        }

        public void Load()
        {
            foreach (var saveReader in _saveReaders)
            {
                saveReader.ReadSave(LoadedData);
            }
        }

        public void Load(object subject)
        {
            if (subject is ISaveReader<T> reader)
            {
                reader.ReadSave(LoadedData);
            }
        }

        public UniTask Save() =>
            SaveToServer(PrepareDataForSave());

        public void RewriteExistSaveData() =>
            LoadedData = PrepareDataForSave();

        public async UniTask DeleteSaveAndReload()
        {
            await Clear();
            Load();
        }

        private T CreateEmptySavedData() =>
            new();

        private UniTask<T> LoadDataFromNakama()
        {
            var data = Data.LoadFromFile<T>(CollectionName, SaveKey);
            return UniTask.FromResult(data);
        }

        private T PrepareDataForSave()
        {
            T saveData = CreateEmptySavedData();

            foreach (var saveWriter in _saveWriters)
            {
                saveWriter.WriteSave(saveData);
            }

            return saveData;
        }

        private UniTask SaveToServer(T saveData)
        {
            Log.SaveSystem.Error($"Not implemented {CollectionName}_{SaveKey}");
            return UniTask.CompletedTask;
        }
    }
}