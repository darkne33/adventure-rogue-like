using UnityEngine;

public class CharacterHealthView : MonoBehaviour, IHealthView
{
    [SerializeField] private UnityEngine.UI.Image[] _hearts;
    [SerializeField] private Sprite _fullHeart;
    [SerializeField] private Sprite _halfHeart;
    [SerializeField] private Sprite _emptyHeart;
    [SerializeField] private Sprite _emptyHalfHeart;

    public void UpdateHealth(float currentHealth, float maximumHealth)
    {
        int maximumHalves = Mathf.Clamp(Mathf.RoundToInt(maximumHealth * 2f), 0, _hearts.Length * 2);
        int currentHalves = Mathf.Clamp(Mathf.RoundToInt(currentHealth * 2f), 0, maximumHalves);

        for (int i = 0; i < _hearts.Length; i++)
        {
            UnityEngine.UI.Image heart = _hearts[i];
            int capacity = Mathf.Clamp(maximumHalves - i * 2, 0, 2);
            heart.gameObject.SetActive(capacity > 0);
            if (capacity == 0)
                continue;

            int filled = Mathf.Clamp(currentHalves - i * 2, 0, capacity);
            if (capacity == 1)
            {
                // A half container has no right half, even when it is filled.
                heart.sprite = filled > 0 ? _fullHeart : _emptyHalfHeart;
                heart.fillAmount = 0.5f;
            }
            else
            {
                heart.sprite = filled == 2 ? _fullHeart : filled == 1 ? _halfHeart : _emptyHeart;
                heart.fillAmount = 1f;
            }
        }
    }
}
