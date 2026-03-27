using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ProjectPrecipicePT
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set;  }
        
        public event Action<int, InventoryStack> OnSelectedHotbarSlotChanged;
        public event Action OnInventoryChanged;

        private const int _minimumHotbarSlotCount = 1;
        private const int _minimumTotalSlotCount = 8;

        [Header("Inventory Layout")]
        [SerializeField, Min(_minimumTotalSlotCount), Tooltip("Total number of inventory slots available to the player, including the hotbar.")]
        private int _slotCount = 24;
        
        [SerializeField, Min(_minimumHotbarSlotCount), Tooltip("Number of slots reserved for the hotbar at the bottom of the screen.")]
        private int _hotbarSlotCount = 8;
        
        private readonly List<InventoryStack> _slots = new();
        
        public InventoryStack CursorStack { get; private set; } = new();
        public InventoryStack SelectedHotbarStack { get; private set; } = new();
        
        public int SelectedHotbarSlotIndex { get; private set; } = -1;
        public int HotbarSlotCount => Mathf.Min(_hotbarSlotCount, _slots.Count);
        public int SlotCount => _slots.Count;
        

        private void Awake()
        {
            Instance = this;
            
            InitializeSlots();
        }
        
        private void Start()
        {
            SubscribeToInput();
            SelectHotbarSlot(0);
        }

        private void OnDestroy()
        {
            UnSubscribeFromInput();
        }

        #region Input

        private void SubscribeToInput()
        {
            GameInput.Instance.OnSelectSlot += GameInput_OnSelectSlot;
            GameInput.Instance.OnScrollWheel += GameInput_OnScrollWheel;
        }

        private void UnSubscribeFromInput()
        {
            GameInput.Instance.OnSelectSlot -= GameInput_OnSelectSlot;
            GameInput.Instance.OnScrollWheel -= GameInput_OnScrollWheel;
        }

        private void GameInput_OnSelectSlot(object sender, InputAction.CallbackContext context)
        {
            if (!context.started || Player.Instance.State != Player.PlayerStateType.Locomotion)
            {
                return;
            }

            var control = context.control; // The control (key/button) that triggered this

            // If the key is Digit1–Digit8
            if (control is KeyControl key)
            {
                int slotIndex = key.keyCode - Key.Digit1; // Convert Key.Digit1 to 0, Digit2 to 1, etc.
                if (slotIndex >= 0 && slotIndex < HotbarSlotCount)
                {
                    SelectHotbarSlot(slotIndex);
                }
            }
        }

        private void GameInput_OnScrollWheel(object sender, InputAction.CallbackContext context)
        {
            if (!context.performed || Player.Instance.State != Player.PlayerStateType.Locomotion)
            {
                return;
            }

            Vector2 scrollDelta = context.ReadValue<Vector2>();
            int itemCount = _hotbarSlotCount;
            if (itemCount == 0) return;

            int selectedSlotIndex = SelectedHotbarSlotIndex;


            if (scrollDelta.y > 0f) // Scroll up
            {
                int upcomingIndex = selectedSlotIndex - 1;
                if (upcomingIndex < 0)
                {
                    selectedSlotIndex = itemCount - 1; // Wrap to last item
                }
                else
                {
                    selectedSlotIndex--;
                }
                SelectHotbarSlot(selectedSlotIndex);
            }
            else if (scrollDelta.y < 0f) // Scroll down
            {
                int upcomingIndex = selectedSlotIndex + 1;
                if (upcomingIndex >= itemCount)
                {
                    selectedSlotIndex = 0; // Wrap to first item
                }
                else
                {
                    selectedSlotIndex++;
                }
                SelectHotbarSlot(selectedSlotIndex);
            }
        }

        #endregion

        #region Inventory Functions
        
        public int AddItem(ItemSO item, int amount = 1)
        {
            if(item == null || amount <= 0)
            {
                return 0;
            }
            
            int remainingAmount = amount;
            FillExistingStacks(item, ref remainingAmount);
            FillEmptySlots(item, ref remainingAmount);
            RefreshAfterInventoryChange();
            return remainingAmount;
        }
        
        public List<InventoryStack> AddItems(IEnumerable<InventoryStack> stacksToAdd)
        {
            List<InventoryStack> leftOvers = new();

            if (stacksToAdd == null)
            {
                return leftOvers;
            }

            foreach (InventoryStack stack in stacksToAdd)
            {
                if(stack == null || stack.IsEmpty)
                {
                    continue;
                }

                int remainingAmount = AddItem(stack.Item, stack.Amount);
                if(remainingAmount > 0)
                {
                    leftOvers.Add(new InventoryStack(stack.Item, remainingAmount));
                }
            }
            
            return leftOvers;
        }
        
        public int Removeitem(ItemSO item, int amount = 1)
        {
            if(item == null || amount <= 0)
            {
                return amount;
            }

            int remainingAmount = amount;
            for (int index = _slots.Count - 1; index >= 0 && remainingAmount > 0; index--)
            {
                InventoryStack slot = _slots[index];
                if(slot.IsEmpty || slot.Item != item)
                {
                    continue;
                }
                
                remainingAmount -= slot.RemoveAmount(remainingAmount);
            }
            
            RefreshAfterInventoryChange();
            return remainingAmount;
        }
        
        public List<InventoryStack> RemoveItems(IEnumerable<InventoryStack> stacksToRemove)
        {
            List<InventoryStack> leftOvers = new();
            
            if (stacksToRemove == null)
            {
                return leftOvers;
            }

            foreach (InventoryStack stack in stacksToRemove)
            {
                if(stack == null || stack.IsEmpty)
                {
                    continue;
                }

                int remainingAmount = Removeitem(stack.Item, stack.Amount);
                if(remainingAmount > 0)
                {
                    leftOvers.Add(new InventoryStack(stack.Item, remainingAmount));
                }
            }
            
            return leftOvers;
        }

        private void FillExistingStacks(ItemSO item, ref int remainingAmount)
        {
            if(!item.IsStackable)
            {
                return;
            }
            
            foreach (InventoryStack slot in _slots)
            {
                if(remainingAmount <= 0)
                {
                    return;
                }
                
                if(!slot.CanStackWith(item) || slot.IsFull)
                {
                    continue;
                }
                
                int addedAmount = slot.AddAmount(remainingAmount);
                remainingAmount -= addedAmount;
            }
        }

        private void FillEmptySlots(ItemSO item, ref int remainingAmount)
        {
            foreach (InventoryStack slot in _slots)
            {
                if(remainingAmount <= 0)
                {
                    return;
                }
                
                if(!slot.IsEmpty)
                {
                    continue;
                }
                
                int amountForSlot = Mathf.Min(item.MaxStackSize, remainingAmount);
                slot.Set(item, amountForSlot);
                remainingAmount -= amountForSlot;
            }
        }

        private void RefreshAfterInventoryChange()
        {
            UpdateSelectedHotbarStack();
            OnInventoryChanged?.Invoke();
        }

        #endregion

        private void SelectHotbarSlot(int hotbarSlotIndex)
        {
            if(HotbarSlotCount == 0)
            {
                SelectedHotbarSlotIndex = 0;
                UpdateSelectedHotbarStack();
                return;
            }
            
            int newIndex = Mathf.Clamp(hotbarSlotIndex, 0, HotbarSlotCount - 1);
            
            // Prevent redundant updates if the slot is already selected
            if (newIndex == SelectedHotbarSlotIndex) return;

            SelectedHotbarSlotIndex = newIndex;
            UpdateSelectedHotbarStack();
            OnSelectedHotbarSlotChanged?.Invoke(SelectedHotbarSlotIndex, SelectedHotbarStack.Clone());
        }

        private void UpdateSelectedHotbarStack()
        {
            if (!IsValidSlotIndex(SelectedHotbarSlotIndex))
            {
                SelectedHotbarStack = new InventoryStack();
                Debug.Log($"Invalid hotbar slot index: {SelectedHotbarSlotIndex}");
                return;
            }
            Debug.Log($"Selected hotbar slot index: {SelectedHotbarSlotIndex}");
            SelectedHotbarStack = _slots[SelectedHotbarSlotIndex].Clone();
        }

        private bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < _slots.Count;
        }

        private void InitializeSlots()
        {
            _slots.Clear();
            
            for (int i = 0; i < _slotCount; i++)
            {
                _slots.Add(new InventoryStack());
            }
        }
    }
}
