using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Sounds
{
    public class ToggleViewMonoComponent : MonoBehaviour
    {
        [field: SerializeField] public Button Button { get; private set; }
        
        [SerializeField] private Image _toggle;
        [SerializeField] private GameObject _on;
        [SerializeField] private GameObject _off;

        [SerializeField] private Sprite _onSprite;
        [SerializeField] private Sprite _offSprite;

        private readonly float _disabledX = -72;
        private readonly float _enabledX = 80;
        
        private Tween _tween;

        public void Initialize(bool disabled)
        {
            if (disabled)
            {
                _toggle.rectTransform.anchoredPosition = new Vector2(_disabledX, _toggle.rectTransform.anchoredPosition.y);
                _on.gameObject.SetActive(false);
                _off.gameObject.SetActive(true);
                _toggle.sprite = _offSprite;
            }
            else
            {
                _toggle.rectTransform.anchoredPosition = new Vector2(_enabledX, _toggle.rectTransform.anchoredPosition.y);
                _on.gameObject.SetActive(true);
                _off.gameObject.SetActive(false);
                _toggle.sprite = _onSprite;
            }
        }
        
        public UniTask SetToggle(bool disabled, float duration)
        {
            _tween = disabled
                ? _toggle.rectTransform.DOAnchorPosX(_disabledX, duration)
                : _toggle.rectTransform.DOAnchorPosX(_enabledX, duration);
            _tween.SetLink(_toggle.gameObject).SetId("ToggleAudioViewMonoComponent");
            
            if (disabled)
            {
                _on.SetActive(false);
                _off.SetActive(true);
                _toggle.sprite = _offSprite;
            }
            else
            {
                _on.SetActive(true);
                _off.SetActive(false);
                _toggle.sprite = _onSprite;
            }

            return _tween.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
        }
    }
}