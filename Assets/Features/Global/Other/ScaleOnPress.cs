using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.UI.Animations
{
    public class ScaleOnPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _scalePress = .9f;
        [SerializeField] private float _normalScale = 1f;

        private Tween _scaleTween;
        private bool _buttonPressedDown;
        private bool _buttonWaitingUp;

        public void OnPointerDown(PointerEventData eventData)
        {
            ButtonPressDown();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ButtonPressUp();
        }

        private void OnValidate()
        {
            _target ??= transform;
        }
        private void OnDestroy() =>
            _scaleTween?.Kill();

        private void ButtonPressDown()
        {
            if (_buttonPressedDown)
                return;
            _buttonPressedDown = true;
            _scaleTween?.Kill();
            _scaleTween = _target.DOScale(_scalePress, .05f)
                .OnComplete(() =>
                {
                    _buttonPressedDown = false;
                    if (_buttonWaitingUp)
                        ButtonPressUp();
                });
        }

        private void ButtonPressUp()
        {
            _buttonWaitingUp = true;
            if (_buttonPressedDown)
                return;
            _buttonWaitingUp = false;
            _scaleTween?.Kill();
            _scaleTween = _target.DOScale(_normalScale, .1f);
        }
    }
}