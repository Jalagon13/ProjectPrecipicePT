using System;
using UnityEngine;

namespace ProjectPrecipicePT
{
    public enum StaminaIntrusionType
    {
        Weight,
        Hunger,
        Fatigue,
        Radiation
    }

    [Serializable]
    public class StaminaIntrusion
    {
        public StaminaIntrusionType Type;
        public int Amount;
    }

    public class StaminaManager : MonoBehaviour
    {
        public static StaminaManager Instance { get; private set; }
        

        public event Action<int, int, int> OnStaminaChanged;
        public event Action OnIntrusionsChanged;

        [Header("Stamina Pool")]
        [SerializeField, Min(1)] private int _startingMaxStaminaAmount = 100;
        [SerializeField, Min(0)] private int _staminaRegenPerSecond = 20;
        [SerializeField, Min(0f)] private float _staminaRegenDelay = 0.65f;

        [Header("Activity Costs")]
        [SerializeField, Min(0)] private int _sprintStaminaDrainRate = 12;
        [SerializeField, Min(0)] private int _climbStaminaDrainRate = 18;
        [SerializeField, Min(0)] private int _jumpStaminaCost = 15;
        
        private int _currentMaxStamina;
        private int _currentStaminaLimit;
        private int _currentStamina;
        private float _staminaFraction;
        private System.Collections.Generic.List<StaminaIntrusion> _intrusions = new();
        private float _regenResumeTime;
        private bool _wasSprintingLastFrame;

        public int CurrentMaxStamina => _currentMaxStamina;
        public int CurrentStaminaLimit => _currentStaminaLimit;
        public int CurrentStamina => _currentStamina;
        public System.Collections.Generic.IReadOnlyList<StaminaIntrusion> Intrusions => _intrusions;
        public bool HasStaminaForClimbing => _currentStamina > 0;

        private void Awake()
        {
            Instance = this;
            _currentMaxStamina = _startingMaxStaminaAmount;
            _currentStaminaLimit = _currentMaxStamina;
            _currentStamina = _currentMaxStamina;
            _staminaFraction = 0f;
            PublishStaminaChanged("Initialized");
        }

        private void Update()
        {
            TickRegeneration();
        }

        public bool CanStartJump()
        {
            return _currentStamina >= _jumpStaminaCost;
        }

        public bool TryConsumeJumpStamina(string source)
        {
            if (CanSpend(_jumpStaminaCost))
            {
                ConsumeStaminaInt(_jumpStaminaCost, $"{source} stamina cost");
                BeginRecoveryCooldown($"{source} used stamina");
                return true;
            }

            Debug.Log($"StaminaManager: blocked {source} because stamina is too low. Current {_currentStamina}/{_currentStaminaLimit}, required {_jumpStaminaCost}.");
            return false;
        }

        public bool TrySustainSprint(bool isTryingToSprint, float deltaTime)
        {
            if (!isTryingToSprint)
            {
                if (_wasSprintingLastFrame)
                {
                    _wasSprintingLastFrame = false;
                    BeginRecoveryCooldown("Sprint ended");
                }

                return false;
            }

            if (!HasStaminaForClimbing)
            {
                if (_wasSprintingLastFrame)
                {
                    _wasSprintingLastFrame = false;
                    BeginRecoveryCooldown("Sprint exhausted stamina");
                    Debug.Log("StaminaManager: sprint stopped because stamina reached zero.");
                }

                return false;
            }

            float staminaCost = _sprintStaminaDrainRate * Mathf.Max(0f, deltaTime);
            if (staminaCost > 0f)
            {
                ConsumeStaminaFraction(staminaCost, "Sprint drain");
                _regenResumeTime = Mathf.Max(_regenResumeTime, Time.time + _staminaRegenDelay);
            }

            bool canKeepSprinting = HasStaminaForClimbing;
            if (!canKeepSprinting)
            {
                _wasSprintingLastFrame = false;
                BeginRecoveryCooldown("Sprint exhausted stamina");
                Debug.Log("StaminaManager: sprint stopped because stamina reached zero.");
                return false;
            }

            _wasSprintingLastFrame = true;
            return true;
        }

        public void ForceEndSprint(string reason)
        {
            if (!_wasSprintingLastFrame)
            {
                return;
            }

            _wasSprintingLastFrame = false;
            BeginRecoveryCooldown(reason);
        }

        public bool TrySustainClimbing(float deltaTime)
        {
            if (!HasStaminaForClimbing)
            {
                BeginRecoveryCooldown("Climb exhausted stamina");
                Debug.Log("StaminaManager: climbing stopped because stamina reached zero.");
                return false;
            }

            float staminaCost = _climbStaminaDrainRate * Mathf.Max(0f, deltaTime);
            if (staminaCost > 0f)
            {
                ConsumeStaminaFraction(staminaCost, "Climb drain");
                _regenResumeTime = Mathf.Max(_regenResumeTime, Time.time + _staminaRegenDelay);
            }

            if (HasStaminaForClimbing)
            {
                return true;
            }

            BeginRecoveryCooldown("Climb exhausted stamina");
            Debug.Log("StaminaManager: climbing stopped because stamina reached zero.");
            return false;
        }

        public void BeginRecoveryCooldown(string reason)
        {
            _regenResumeTime = Time.time + _staminaRegenDelay;
            Debug.Log($"StaminaManager: recovery cooldown started for {reason}. Regen resumes in {_staminaRegenDelay:0.##} seconds.");
        }

        private bool CanSpend(int amount)
        {
            return amount <= 0 || _currentStamina >= amount;
        }

        private void TickRegeneration()
        {
            if (Time.time < _regenResumeTime || _currentStamina >= _currentStaminaLimit)
            {
                return;
            }

            float regenAmount = _staminaRegenPerSecond * Time.deltaTime;
            AddStaminaFraction(regenAmount, "Regenerating stamina");
        }

        // RefreshCarryWeight removed and delegated to InventoryManager

        public int SetIntrusionAmount(StaminaIntrusionType type, int amount, string reason = "")
        {
            amount = Mathf.Max(0, amount);

            // Prevent intrusions from exceeding the max stamina pool
            int otherIntrusionsTotal = 0;
            foreach (var intrusion in _intrusions)
            {
                if (intrusion.Type != type)
                {
                    otherIntrusionsTotal += intrusion.Amount;
                }
            }
            
            int maxAllowedAmount = Mathf.Max(0, _startingMaxStaminaAmount - otherIntrusionsTotal);
            amount = Mathf.Min(amount, maxAllowedAmount);

            var existing = _intrusions.Find(x => x.Type == type);

            if (amount == 0)
            {
                if (existing != null)
                {
                    _intrusions.Remove(existing);
                }
            }
            else
            {
                if (existing != null)
                {
                    existing.Amount = amount;
                }
                else
                {
                    _intrusions.Add(new StaminaIntrusion { Type = type, Amount = amount });
                }
            }

            RecalculateStaminaLimit(reason);
            OnIntrusionsChanged?.Invoke();
            
            return amount;
        }

        private void RecalculateStaminaLimit(string reason)
        {
            int previousLimit = _currentStaminaLimit;
            int totalIntrusions = 0;

            foreach(var intrusion in _intrusions)
            {
                totalIntrusions += intrusion.Amount;
            }

            _currentMaxStamina = _startingMaxStaminaAmount;
            _currentStaminaLimit = Mathf.Clamp(_currentMaxStamina - totalIntrusions, 0, _currentMaxStamina);

            if (_currentStamina > _currentStaminaLimit)
            {
                _currentStamina = _currentStaminaLimit;
                _staminaFraction = 0f;
            }

            PublishStaminaChanged($"{reason}. Total Intrusions: {totalIntrusions}");
        }

        private void ConsumeStaminaInt(int amount, string reason)
        {
            if (amount <= 0) return;
            SetCurrentStamina(_currentStamina - amount, reason);
        }

        private void ConsumeStaminaFraction(float amount, string reason)
        {
            if (amount <= 0f) return;

            _staminaFraction -= amount;
            if (_staminaFraction <= -1f)
            {
                int intDrain = Mathf.FloorToInt(-_staminaFraction);
                _staminaFraction += intDrain;
                SetCurrentStamina(_currentStamina - intDrain, reason);
            }
        }

        private void AddStaminaFraction(float amount, string reason)
        {
            if (amount <= 0f) return;

            _staminaFraction += amount;
            if (_staminaFraction >= 1f)
            {
                int intAdd = Mathf.FloorToInt(_staminaFraction);
                _staminaFraction -= intAdd;
                SetCurrentStamina(_currentStamina + intAdd, reason);
            }
        }

        private void SetCurrentStamina(int value, string reason)
        {
            int clampedValue = Mathf.Clamp(value, 0, _currentStaminaLimit);
            if (clampedValue == _currentStamina)
            {
                return;
            }

            _currentStamina = clampedValue;
            
            if (_currentStamina <= 0 || _currentStamina >= _currentStaminaLimit)
            {
                _staminaFraction = 0f;
            }

            PublishStaminaChanged(reason);
        }

        private void PublishStaminaChanged(string reason)
        {
            OnStaminaChanged?.Invoke(_currentStamina, _currentStaminaLimit, _currentMaxStamina);
        }
    }
}
