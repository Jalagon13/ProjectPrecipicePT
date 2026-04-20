using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectPrecipicePT
{
    public class PickupPanelUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _amountText;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private Image _iconImage;
        [Header("Animation Settings")]
        [SerializeField] private float _disapearDelay = 3f;
        [SerializeField] private float _lerpDuration = 0.25f;
        [SerializeField] private float _fadeOutFraction = 0.25f;

        private RectTransform _rectTransform;
        private Tween _moveTween;
        private Tween _fadeTween;
        private float _currentTargetY;
        private CanvasGroup _canvasGroup;

        private void InitializeVariables()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _currentTargetY = _rectTransform.anchoredPosition.y;
            InventoryManager.Instance.OnItemPickup += InventoryManager_OnItemPickup;
        }
        
        private void OnDestroy()
        {
            _moveTween?.Kill();
            _fadeTween?.Kill();
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemPickup -= InventoryManager_OnItemPickup;
            }
        }

        public void Setup(ItemSO item, int amount)
        {
            InitializeVariables();

            _iconImage.sprite = item.InventoryIcon;
            _nameText.text = item.ItemName;
            _amountText.text = $"+{amount}";

            float fadeDuration = _disapearDelay * _fadeOutFraction;
            float fadeStartTime = _disapearDelay - fadeDuration;

            _canvasGroup.alpha = 1f;

            _fadeTween = _canvasGroup
                .DOFade(0f, fadeDuration)
                .SetDelay(fadeStartTime)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);

            Destroy(gameObject, _disapearDelay);
        }

        private void InventoryManager_OnItemPickup(ItemSO item, int amount)
        {
            float height = _rectTransform.rect.height;
            float newTargetY = _currentTargetY + height;

            float remainingDuration = _lerpDuration;

            if (_moveTween != null && _moveTween.IsActive() && _moveTween.IsPlaying())
            {
                remainingDuration = Mathf.Max(0f, _lerpDuration - _moveTween.Elapsed());
                _moveTween.Kill();
            }

            _currentTargetY = newTargetY;

            _moveTween = _rectTransform
                .DOAnchorPosY(_currentTargetY, remainingDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);       
        }
    }
}
