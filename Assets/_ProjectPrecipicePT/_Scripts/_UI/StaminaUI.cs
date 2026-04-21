using UnityEngine;
using UnityEngine.UI;

namespace ProjectPrecipicePT
{
    public class StaminaUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField, Tooltip("The Image representing usable stamina. Should be Filled, Horizontal, Origin Left.")]
        private Image _staminaFillImage;
        
        [SerializeField, Tooltip("The Image representing lost capacity. Should be Filled, Horizontal, Origin Right.")]
        private Image _reservedFillImage;

        [Header("Settings")]
        [SerializeField, Tooltip("How fast the UI bars smoothly animate to their target values.")]
        private float _lerpSpeed = 10f;

        private float _targetCurrentStamina;
        private float _targetBaseMaxStamina = 100f; // Prevent division by zero
        private float _targetCarryWeight;

        private float _visualCurrentStamina;
        private float _visualCarryWeight;

        private void Start()
        {
            StaminaManager.Instance.OnStaminaChanged += HandleStaminaChanged;
                
            // Initialize to current state immediately
            _targetCurrentStamina = StaminaManager.Instance.CurrentStamina;
            _targetBaseMaxStamina = StaminaManager.Instance.CurrentMaxStamina;
            _targetCarryWeight = StaminaManager.Instance.CurrentCarryWeight;

            _visualCurrentStamina = _targetCurrentStamina;
            _visualCarryWeight = _targetCarryWeight;
            
            UpdateUIBars();
        }

        private void OnDestroy()
        {
            StaminaManager.Instance.OnStaminaChanged -= HandleStaminaChanged;
        }

        private void Update()
        {
            if (_targetBaseMaxStamina <= 0f) return;

            // Smoothly interpolate the visual values towards the actual integer targets
            _visualCurrentStamina = Mathf.Lerp(_visualCurrentStamina, _targetCurrentStamina, Time.deltaTime * _lerpSpeed);
            _visualCarryWeight = Mathf.Lerp(_visualCarryWeight, _targetCarryWeight, Time.deltaTime * _lerpSpeed);

            UpdateUIBars();
        }

        private void HandleStaminaChanged(int currentStamina, int maxStamina, int baseMaxStamina, int carryWeight)
        {
            _targetCurrentStamina = currentStamina;
            _targetBaseMaxStamina = baseMaxStamina;
            _targetCarryWeight = carryWeight;
        }

        private void UpdateUIBars()
        {
            if (_targetBaseMaxStamina <= 0f) return;

            if (_staminaFillImage != null)
            {
                _staminaFillImage.fillAmount = Mathf.Clamp01(_visualCurrentStamina / _targetBaseMaxStamina);
            }

            if (_reservedFillImage != null)
            {
                _reservedFillImage.fillAmount = Mathf.Clamp01(_visualCarryWeight / _targetBaseMaxStamina);
            }
        }
    }
}
