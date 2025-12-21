using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Core.Sounds
{
    public class ToggleAudioViewMonoComponent : MonoBehaviour
    {
        [SerializeField] private Image _toggle;
        [SerializeField] private Button _on;
        [SerializeField] private Button _off;

        [SerializeField] private Sprite _onSprite;
        [SerializeField] private Sprite _offSprite;

        [Inject] private ISoundsService _soundsService;

        private Tween _tween;

        public async UniTask ToggleOn(bool isMute, float duration)
        {
            _tween = isMute
                ? _toggle.rectTransform.DOAnchorPosX(-72, duration)
                : _toggle.rectTransform.DOAnchorPosX(80, duration);
            _tween.SetLink(_toggle.gameObject).SetId("ToggleAudioViewMonoComponent");
            if (isMute)
            {
                _on.gameObject.SetActive(false);
                _off.gameObject.SetActive(true);
                _toggle.sprite = _offSprite;
            }
            else
            {
                _on.gameObject.SetActive(true);
                _off.gameObject.SetActive(false);
                _toggle.sprite = _onSprite;
            }
            
            await _tween.ToUniTask();
        }
    }
}