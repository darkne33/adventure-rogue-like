using System;
using Features.Bosses.Scripts;
using global::UI;
using UnityEngine;
using Zenject;

namespace Features.Bosses.UI
{
    public sealed class BossHealthUIController : ITickable, IDisposable
    {
        private readonly IRogueLikeRuntimeDataService _runtimeData;
        private readonly IPanelsProvider _panelsProvider;
        private readonly DiContainer _container;

        private BossFacade _boss;
        private BossRoomData _room;
        private BossHealthCanvas _view;
        private bool _isHiding;

        public BossHealthUIController(IRogueLikeRuntimeDataService runtimeData,
            IPanelsProvider panelsProvider, DiContainer container)
        {
            _runtimeData = runtimeData;
            _panelsProvider = panelsProvider;
            _container = container;
            _runtimeData.RoomChanged += OnRoomChanged;
        }

        public void Show(BossFacade boss, BossRoomData room)
        {
            HideImmediately();
            if (boss == null || boss.HealthSystem == null || boss.IsDead || room == null ||
                room.IsCompleted || !ReferenceEquals(_runtimeData.CurrentRoomData, room))
                return;

            if (boss.Config.HealthCanvasPrefab == null)
            {
                Debug.LogError($"Boss health canvas prefab is missing in {boss.Config.name}.", boss.Config);
                return;
            }

            _boss = boss;
            _room = room;
            Transform root = _panelsProvider.GetRootFor(PanelLocation.OverlayUI);
            _view = _container.InstantiatePrefabForComponent<BossHealthCanvas>(
                boss.Config.HealthCanvasPrefab, root);
            _view.name = "BossHealthCanvas";
            _view.Show(boss.HealthSystem.CurrentHealth, boss.HealthSystem.MaxHealth);
        }

        public void Tick()
        {
            if (_view == null || _isHiding)
                return;

            if (!ReferenceEquals(_runtimeData.CurrentRoomData, _room))
            {
                HideImmediately();
                return;
            }

            if (_boss != null)
                _view.SetHealth(_boss.HealthSystem.CurrentHealth, _boss.HealthSystem.MaxHealth);

            if (_boss == null || _boss.IsDead || !_boss.isActiveAndEnabled || _room.IsCompleted)
            {
                _isHiding = true;
                _view.HideAndDestroy();
                _boss = null;
            }
        }

        public void Dispose()
        {
            _runtimeData.RoomChanged -= OnRoomChanged;
            HideImmediately();
        }

        private void OnRoomChanged(RoomData previous, RoomData current)
        {
            if (!ReferenceEquals(current, _room))
                HideImmediately();
        }

        private void HideImmediately()
        {
            if (_view != null)
                UnityEngine.Object.Destroy(_view.gameObject);
            _view = null;
            _boss = null;
            _room = null;
            _isHiding = false;
        }
    }
}
