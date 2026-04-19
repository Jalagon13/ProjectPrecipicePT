using System;
using UnityEngine;

namespace ProjectPrecipicePT
{
    public class StaminaManager : MonoBehaviour
    {
        public static StaminaManager Instance { get; private set; }
        
        [SerializeField] private readonly int _startingMaxStaminaAmount = 100;
        
        private int _currentMaxStamina;
        private int _currentStaminaLimit;
        private int _currentStamina;

        private void Awake()
        {
            Instance = this;
            _currentMaxStamina = _startingMaxStaminaAmount;
            _currentStamina = _currentMaxStamina;
        }
        
        private void Start()
        {
            InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
        }
        
        private void OnDestroy()
        {
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }

        private void HandleInventoryChanged()
        {
            
        }
    }
}
