using UnityEngine;
using UnityEngine.UI;

namespace ProjectPrecipicePT
{
    public class HealthUI : MonoBehaviour
    {
        [SerializeField] private Image _healthFillImage;
        [SerializeField] private GameObject _deathPanel;
        
        private void Start()
        {
            HideDeathPanel();

            if (HealthManager.Instance != null)
            {
                HealthManager.Instance.OnHealthChanged += UpdateHealthBar;
                HealthManager.Instance.OnDeath += ShowDeathPanel;
                HealthManager.Instance.OnRespawn += HideDeathPanel;
                UpdateHealthBar();
            }
        }
        
        private void OnDestroy()
        {
            if (HealthManager.Instance != null)
            {
                HealthManager.Instance.OnHealthChanged -= UpdateHealthBar;
                HealthManager.Instance.OnDeath -= ShowDeathPanel;
                HealthManager.Instance.OnRespawn -= HideDeathPanel;
            }
        }
        
        private void UpdateHealthBar()
        {
            if (_healthFillImage != null && HealthManager.Instance.MaxHealth > 0)
            {
                float fillAmount = (float)HealthManager.Instance.CurrentHealth / HealthManager.Instance.MaxHealth;
                _healthFillImage.fillAmount = fillAmount;
            }
        }
        
        public void OnRespawnButtonPressed()
        {
            HealthManager.Instance.Respawn();
        }
        
        private void ShowDeathPanel()
        {
            _deathPanel.SetActive(true);
        }
        
        private void HideDeathPanel()
        {
            _deathPanel.SetActive(false);
        }
    }
}
