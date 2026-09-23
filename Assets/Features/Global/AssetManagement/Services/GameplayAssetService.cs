using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.Quests.Scripts;
using Features.Relics.Scripts;
using Features.RunResults.Scripts;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Configs;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Factories;
using UnityEngine;

namespace Core
{
    // Loaded by BootstrapState before any gameplay scene installs its dependencies.
    public sealed class GameplayAssetService : IDisposable
    {
        private readonly IAddressableLoadService _assets;
        private readonly List<string> _addresses = new();
        private readonly CancellationTokenSource _lifetime = new();
        private bool _initialized;
        private bool _disposed;

        public ProgressionConfiguration Progression { get; private set; }
        public QuestCompletionNotificationView QuestNotification { get; private set; }
        public RelicPoolConfiguration RelicPool { get; private set; }
        public RelicChestConfiguration RelicChest { get; private set; }
        public Shader ProximityFadeShader { get; private set; }
        public RunResultsPanel RunResults { get; private set; }

        public GameplayAssetService(IAddressableLoadService assets) => _assets = assets;

        public async UniTask Initialize(CancellationToken token)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameplayAssetService));
            if (_initialized)
                return;

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(token, _lifetime.Token);
            token = linkedCancellation.Token;
            try
            {
                LoggingConfig logging = await Load<LoggingConfig>(
                    "Assets/CustomPackages/Package.Logging/Content/Configs/LoggingConfig.asset", token);
                LoggingConfig.SetInstance(logging);
                LoggerFactory.UpdateMinLogLevels();

                Progression = await Load<ProgressionConfiguration>(ProgressionConfiguration.Address, token);
                QuestNotification = await LoadPrefab<QuestCompletionNotificationView>(
                    QuestCompletionNotificationController.Address, token);
                RelicPool = await Load<RelicPoolConfiguration>(
                    "Assets/Features/Relics/Configs/RelicPoolConfiguration.asset", token);
                RelicChest = await Load<RelicChestConfiguration>(
                    "Assets/Features/Relics/Configs/RelicChestConfiguration.asset", token);
                ProximityFadeShader = await Load<Shader>(
                    "Assets/Features/Character/Shaders/ProximityFadeLit.shader", token);
                RunResults = await LoadPrefab<RunResultsPanel>(
                    "Assets/Features/RunResults/Prefabs/RunResultsPanel.prefab", token);
                _initialized = true;
            }
            catch
            {
                ReleaseAssets();
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _lifetime.Cancel();
            ReleaseAssets();
            _lifetime.Dispose();
        }

        private void ReleaseAssets()
        {
            LoggingConfig.SetInstance(null);
            for (int i = _addresses.Count - 1; i >= 0; i--)
                _assets.Release(_addresses[i]);
            _addresses.Clear();
            Progression = null;
            QuestNotification = null;
            RelicPool = null;
            RelicChest = null;
            ProximityFadeShader = null;
            RunResults = null;
            _initialized = false;
        }

        private async UniTask<T> Load<T>(string address, CancellationToken token) where T : UnityEngine.Object
        {
            _addresses.Add(address);
            T asset = await _assets.Load<T>(address, token);
            token.ThrowIfCancellationRequested();
            if (asset == null)
                throw new InvalidOperationException($"Required Addressable asset is missing: {address}");
            return asset;
        }

        private async UniTask<T> LoadPrefab<T>(string address, CancellationToken token) where T : Component
        {
            GameObject prefab = await Load<GameObject>(address, token);
            if (!prefab.TryGetComponent(out T component))
                throw new InvalidOperationException($"Addressable prefab '{address}' requires {typeof(T).Name}.");
            return component;
        }
    }
}
