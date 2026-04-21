using Sirenix.OdinInspector;
using UnityEngine;

namespace ProjectPrecipicePT
{
    public class HungerManager : MonoBehaviour
    {
        public static HungerManager Instance { get; private set; }
        

        [Header("Hunger Settings")]
        [SerializeField, Tooltip("Maximum amount of hunger intrusion points allowed.")]
        private int _maxHungerLimit = 50;

        [SerializeField, Tooltip("How many seconds between each automatic hunger increase.")]
        private float _hungerTickRateSeconds = 30f;

        [SerializeField, Tooltip("How many hunger points to add each tick.")]
        private int _hungerPointsPerTick = 1;

        public event System.Action<int> OnHungerChanged;

        public int CurrentHunger { get; private set; }

        private float _nextHungerTickTime;

        private void Awake()
        {
            Instance = this;
            CurrentHunger = 0;
        }

        private void Start()
        {
            _nextHungerTickTime = Time.time + _hungerTickRateSeconds;
            UpdateStaminaIntrusion();
        }

        private void Update()
        {
            if (Time.time >= _nextHungerTickTime)
            {
                _nextHungerTickTime = Time.time + _hungerTickRateSeconds;
                AddHungerPoints(_hungerPointsPerTick);
            }
        }

        [Button("Add Hunger")]
        public void AddHungerPoints(int amount)
        {
            if (amount <= 0 || CurrentHunger >= _maxHungerLimit) return;

            CurrentHunger += amount;
            
            if (CurrentHunger > _maxHungerLimit)
            {
                CurrentHunger = _maxHungerLimit;
            }

            UpdateStaminaIntrusion();
        }

        [Button("Remove Hunger")]
        public void RemoveHungerPoints(int amount)
        {
            if (amount <= 0 || CurrentHunger <= 0) return;

            CurrentHunger -= amount;
            
            if (CurrentHunger < 0)
            {
                CurrentHunger = 0;
            }

            UpdateStaminaIntrusion();
        }

        private void UpdateStaminaIntrusion()
        {
            if (StaminaManager.Instance != null)
            {
                CurrentHunger = StaminaManager.Instance.SetIntrusionAmount(StaminaIntrusionType.Hunger, CurrentHunger, "Hunger updated");
            }
            
            OnHungerChanged?.Invoke(CurrentHunger);
        }
    }
}