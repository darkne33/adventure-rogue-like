using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using Features.Bosses.Scripts;
using UI;
using UnityEngine;
using Zenject;

namespace Features.Bosses.UI
{
    public sealed class BossIntroService : IDisposable
    {
        private readonly GameplayAssetService _assets;
        private readonly IUIFactory _uiFactory;
        private readonly DiContainer _container;
        private readonly ICharacterProvider _characters;
        private readonly CancellationTokenSource _lifetime = new();
        private BossIntroScreen _screen;
        private bool _isPlaying;
        private bool _disposed;

        public BossIntroService(GameplayAssetService assets, IUIFactory uiFactory,
            DiContainer container, ICharacterProvider characters)
        {
            _assets = assets;
            _uiFactory = uiFactory;
            _container = container;
            _characters = characters;
        }

        public async UniTask PlayAsync(BossConfig boss, CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BossIntroService));
            if (_isPlaying)
                throw new InvalidOperationException("A boss introduction is already playing.");
            if (boss == null)
                throw new ArgumentNullException(nameof(boss));

            CharacterFacade character = _characters.CharacterFacade;
            if (character == null || character.Definition == null)
                throw new InvalidOperationException("The current character is not available for the boss introduction.");

            CharacterDefinition definition = character.Definition;
            if (definition.IntroPortrait == null || boss.IntroPortrait == null)
                throw new InvalidOperationException(
                    $"Boss introduction portraits are missing for {definition.DisplayName} or {boss.DisplayName}.");

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _lifetime.Token, character.GetCancellationTokenOnDestroy());
            CancellationToken token = linkedCancellation.Token;
            token.ThrowIfCancellationRequested();

            bool wasCinematicPaused = character.IsCinematicPaused;
            _isPlaying = true;
            character.SetCinematicPaused(true);
            try
            {
                EnsureScreen();
                _screen.transform.SetAsLastSibling();
                await _screen.PlayAsync(definition.IntroPortrait, definition.IntroDisplayName,
                    boss.IntroPortrait, boss.IntroDisplayName, boss.IntroPortraitRect, token);
                token.ThrowIfCancellationRequested();
            }
            finally
            {
                try
                {
                    if (_screen != null)
                        _screen.Hide();
                }
                finally
                {
                    try
                    {
                        if (character != null)
                            character.SetCinematicPaused(wasCinematicPaused);
                    }
                    finally
                    {
                        _isPlaying = false;
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _lifetime.Cancel();
            if (_screen != null)
            {
                _screen.Hide();
                UnityEngine.Object.Destroy(_screen.gameObject);
                _screen = null;
            }
            _lifetime.Dispose();
        }

        private void EnsureScreen()
        {
            if (_screen != null)
                return;
            if (_assets.BossIntro == null || _uiFactory.UIRoot == null)
                throw new InvalidOperationException("Boss introduction UI must be loaded before entering gameplay.");

            _screen = _container.InstantiatePrefabForComponent<BossIntroScreen>(
                _assets.BossIntro.gameObject, _uiFactory.UIRoot);
            _screen.Hide();
        }
    }
}
