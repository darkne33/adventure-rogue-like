using System;
using System.Collections.Generic;
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

        private readonly List<BossFacade> _bosses = new List<BossFacade>(2);
        private float _maxHealth;
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

            _bosses.Add(boss);
            _maxHealth = boss.HealthSystem.MaxHealth;
            _room = room;
            Transform root = _panelsProvider.GetRootFor(PanelLocation.OverlayUI);
            _view = _container.InstantiatePrefabForComponent<BossHealthCanvas>(
                boss.Config.HealthCanvasPrefab, root);
            _view.name = "BossHealthCanvas";
            _view.Show(boss.HealthSystem.CurrentHealth, _maxHealth);
        }

        public void ReplaceBoss(BossFacade original, BossFacade first, BossFacade second)
        {
            if (_view == null || _isHiding || original == null || first == null || second == null)
                return;

            int index = _bosses.IndexOf(original);
            if (index < 0)
                return;

            _bosses[index] = first;
            _bosses.Add(second);
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

            float currentHealth = 0f;
            bool hasLivingBoss = false;
            foreach (BossFacade boss in _bosses)
            {
                if (boss == null || boss.IsDead || !boss.isActiveAndEnabled || boss.HealthSystem == null)
                    continue;

                hasLivingBoss = true;
                currentHealth += boss.HealthSystem.CurrentHealth;
            }

            _view.SetHealth(currentHealth, _maxHealth);

            if (!hasLivingBoss || _room.IsCompleted)
            {
                _isHiding = true;
                _view.HideAndDestroy();
                _bosses.Clear();
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
            _bosses.Clear();
            _maxHealth = 0f;
            _room = null;
            _isHiding = false;
        }
    }
}
