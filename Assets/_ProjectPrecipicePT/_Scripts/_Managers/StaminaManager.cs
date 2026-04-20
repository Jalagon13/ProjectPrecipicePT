using System;
using UnityEngine;

namespace ProjectPrecipicePT
{
    public class StaminaManager : MonoBehaviour
    {
        public static StaminaManager Instance { get; private set; }

        public event Action<int, int, int, int> OnStaminaChanged;

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
        private float _currentStamina;
        private int _currentCarryWeight;
        private float _regenResumeTime;
        private bool _wasSprintingLastFrame;

        public int CurrentMaxStamina => _currentMaxStamina;
        public int CurrentStaminaLimit => _currentStaminaLimit;
        public int CurrentStamina => Mathf.FloorToInt(_currentStamina);
        public int CurrentCarryWeight => _currentCarryWeight;
        public bool HasStaminaForClimbing => _currentStamina > 0.001f;

        private void Awake()
        {
            Instance = this;
            _currentMaxStamina = _startingMaxStaminaAmount;
            _currentStaminaLimit = _currentMaxStamina;
            _currentStamina = _currentMaxStamina;
            PublishStaminaChanged("Initialized");
        }

        private void Start()
        {
            InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
            InventoryManager.Instance.OnCursorStackChanged += HandleCursorStackChanged;
            RefreshCarryWeight("Initial inventory sync");
        }

        private void Update()
        {
            TickRegeneration();
        }

        private void OnDestroy()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
                InventoryManager.Instance.OnCursorStackChanged -= HandleCursorStackChanged;
            }
        }

        private void HandleInventoryChanged()
        {
            RefreshCarryWeight("Inventory changed");
        }

        private void HandleCursorStackChanged(InventorySlotItem _)
        {
            RefreshCarryWeight("Cursor item changed");
        }

        public bool CanStartJump()
        {
            return _currentStamina >= _jumpStaminaCost;
        }

        public bool TryConsumeJumpStamina(string source)
        {
            if (CanSpend(_jumpStaminaCost))
            {
                ConsumeStamina(_jumpStaminaCost, $"{source} stamina cost");
                BeginRecoveryCooldown($"{source} used stamina");
                return true;
            }

            Debug.Log($"StaminaManager: blocked {source} because stamina is too low. Current {Mathf.FloorToInt(_currentStamina)}/{_currentStaminaLimit}, required {_jumpStaminaCost}.");
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
                ConsumeStamina(staminaCost, "Sprint drain");
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
                ConsumeStamina(staminaCost, "Climb drain");
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

        private bool CanSpend(float amount)
        {
            return amount <= 0f || _currentStamina >= amount;
        }

        private void TickRegeneration()
        {
            if (Time.time < _regenResumeTime || _currentStamina >= _currentStaminaLimit)
            {
                return;
            }

            float regenAmount = _staminaRegenPerSecond * Time.deltaTime;
            if (regenAmount <= 0f)
            {
                return;
            }

            SetCurrentStamina(_currentStamina + regenAmount, "Regenerating stamina");
        }

        private void RefreshCarryWeight(string reason)
        {
            int carryWeight = 0;

            if (InventoryManager.Instance != null)
            {
                foreach (InventorySlotItem slot in InventoryManager.Instance.Slots)
                {
                    if (slot == null || slot.IsEmpty || slot.Item == null)
                    {
                        continue;
                    }

                    carryWeight += slot.Item.ItemWeight;
                }

                InventorySlotItem cursorItem = InventoryManager.Instance.CursorStack;
                if (cursorItem != null && !cursorItem.IsEmpty && cursorItem.Item != null)
                {
                    carryWeight += cursorItem.Item.ItemWeight;
                }
            }

            int previousLimit = _currentStaminaLimit;
            _currentCarryWeight = carryWeight;
            _currentMaxStamina = _startingMaxStaminaAmount;
            _currentStaminaLimit = Mathf.Clamp(_currentMaxStamina - _currentCarryWeight, 0, _currentMaxStamina);

            if (_currentStamina > _currentStaminaLimit)
            {
                _currentStamina = _currentStaminaLimit;
            }

            if (previousLimit != _currentStaminaLimit)
            {
                BeginRecoveryCooldown("Carry weight changed");
            }

            PublishStaminaChanged($"{reason}. Carry weight {_currentCarryWeight}");
        }

        private void ConsumeStamina(float amount, string reason)
        {
            if (amount <= 0f)
            {
                return;
            }

            SetCurrentStamina(_currentStamina - amount, reason);
        }

        private void SetCurrentStamina(float value, string reason)
        {
            float clampedValue = Mathf.Clamp(value, 0f, _currentStaminaLimit);
            if (Mathf.Approximately(clampedValue, _currentStamina))
            {
                return;
            }

            _currentStamina = clampedValue;
            PublishStaminaChanged(reason);
        }

        private void PublishStaminaChanged(string reason)
        {
            Debug.Log($"StaminaManager: {reason}. Current {Mathf.FloorToInt(_currentStamina)}/{_currentStaminaLimit}, base max {_currentMaxStamina}, carry weight {_currentCarryWeight}.");
            OnStaminaChanged?.Invoke(Mathf.FloorToInt(_currentStamina), _currentStaminaLimit, _currentMaxStamina, _currentCarryWeight);
        }
    }
}
