using UnityEngine;
using System;
using Sirenix.OdinInspector;

namespace ProjectPrecipicePT
{
    public class HealthManager : MonoBehaviour
    {
        public static HealthManager Instance { get; private set; }
        
        [SerializeField] private int _startingMaxHealth = 100;
        
        private int _currentHealth;
        
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _startingMaxHealth;
        
        public event Action OnHealthChanged;
        public event Action OnDeath;
        public event Action OnRespawn;
        
        private void Awake()
        {
            Instance = this;
            _currentHealth = _startingMaxHealth;
        }
        
        [Button("Add Health")]
        public void AddHealth(int amount)
        {
            _currentHealth = Mathf.Clamp(_currentHealth + amount, 0, _startingMaxHealth);
            OnHealthChanged?.Invoke();
        }

        [Button("Remove Health")]
        public void RemoveHealth(int amount)
        {
            if (_currentHealth <= 0) return;

            _currentHealth = Mathf.Clamp(_currentHealth - amount, 0, _startingMaxHealth);
            OnHealthChanged?.Invoke();

            if (_currentHealth <= 0)
            {
                OnDeath?.Invoke();
            }
        }

        public void Respawn()
        {
            _currentHealth = _startingMaxHealth;
            OnHealthChanged?.Invoke();
            OnRespawn?.Invoke();
        }
    }
}
