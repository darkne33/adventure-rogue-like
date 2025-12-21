using Core.Services;
using CustomPackages.Package.Extensions;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Core
{
    public class RewardViewMonoComponent : MonoBehaviour
    {
        public Image Icon => _image;
        public TMP_Text Text => _countText;

        [SerializeField] private TMP_Text _countText;
        [SerializeField] private Image _image;

        [Inject] private IIconService _iconService;
        
        public void Initialize(RewardBase rewardBase)
        {
            switch (rewardBase)
            { 
                case MoneyReward moneyReward:
                    _countText.text = TextToLettersConverter.FormatValue(moneyReward.MoneyRewardAmount);
                    break;
            }
        }

        private async UniTaskVoid SetIcon(RewardBase rewardBase)
        {
            var sprite = await _iconService.Get(rewardBase, this.GetCancellationTokenOnDestroy());
            _image.sprite = sprite;
        }
    }
}