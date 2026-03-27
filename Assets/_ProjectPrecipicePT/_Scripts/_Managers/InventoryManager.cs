using System;
using System.Collections;
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
        public event Action<bool> OnInventoryOpenChanged;
        public event Action<InventoryStack> OnCursorStackChanged;
        
        private const int _minimumHotbarSlotCount = 1;
        private const int _minimumTotalSlotCount = 8;
        
        [Header("Inventory Layout")]
        [SerializeField, Min(_minimumTotalSlotCount), Tooltip("Total number of inventory slots available to the player, including the hotbar.")]
        private int _slotCount = 24;
        
        [SerializeField, Min(_minimumHotbarSlotCount), Tooltip("Number of slots reserved for the hotbar at the bottom of the screen.")]
        private int _hotbarSlotCount = 8;

        [Header("Starting Items")]
        [SerializeField] private float _initialDelay;
        [SerializeField] private float _delayBetweenItemsGiven;
        [SerializeField] private List<InventoryStack> _startingItems = new();

        private readonly List<InventoryStack> _slots = new();
        
        public InventoryStack CursorStack { get; private set; } = new();
        public InventoryStack SelectedHotbarStack { get; private set; } = new();
        
        public int SelectedHotbarSlotIndex { get; private set; } = -1;
        public int HotbarSlotCount => Mathf.Min(_hotbarSlotCount, _slots.Count);
        public int SlotCount => _slots.Count;
        public bool IsInventoryOpen { get; private set; }
        

        private void Awake()
        {
            Instance = this;
            
            InitializeSlots();
        }
        
        private IEnumerator Start()
        {
            SubscribeToInput();
            SelectHotbarSlot(0);
            
            yield return null;
            
            CloseInventory(force: true);
            
            yield return new WaitForSeconds(_initialDelay);
            
            foreach (InventoryStack stack in _startingItems)
            {
                AddItem(stack.Item, stack.Amount);
                yield return new WaitForSeconds(_delayBetweenItemsGiven);
            }
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
            GameInput.Instance.OnToggleInventory += GameInput_OnToggleInventory;
        }

        private void UnSubscribeFromInput()
        {
            GameInput.Instance.OnSelectSlot -= GameInput_OnSelectSlot;
            GameInput.Instance.OnScrollWheel -= GameInput_OnScrollWheel;
            GameInput.Instance.OnToggleInventory -= GameInput_OnToggleInventory;
        }

        private void GameInput_OnToggleInventory(object sender, InputAction.CallbackContext e)
        {
            ToggleInventory();
        }

        public void ToggleInventory()
        {
            if(IsInventoryOpen)
            {
                CloseInventory();
                return;
            }
            
            OpenInventory();
        }

        private void OpenInventory()
        {
            if(IsInventoryOpen || !CanOpenInventory())
            {
                return;
            }
            
            IsInventoryOpen = true;
            OnInventoryOpenChanged?.Invoke(true);
            OnInventoryChanged?.Invoke();
            OnCursorStackChanged?.Invoke(CursorStack.Clone());

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            GameInput.Instance.SetGameplayInputBlocked(true);
        }

        private void CloseInventory(bool force = false)
        {
            if ((!IsInventoryOpen && !force) || !CursorStack.IsEmpty)
            {
                return;
            }
            
            IsInventoryOpen = false;
            OnInventoryOpenChanged?.Invoke(false);
            OnInventoryChanged?.Invoke();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            GameInput.Instance.SetGameplayInputBlocked(false);
        }

        private bool CanOpenInventory()
        {
            return Player.Instance == null || Player.Instance.State == Player.PlayerStateType.Locomotion;
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

        #region Inventory Item Functions
        
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

        #region Slot Click Functions

        public void HandleSlotLeftClick(int slotIndex, bool isShiftHeld)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return;
            }

            if (isShiftHeld && CursorStack.IsEmpty)
            {
                QuickMoveSlot(slotIndex);
                return;
            }
            
            InventoryStack slotStack = _slots[slotIndex];
            if(CursorStack.IsEmpty)
            {
                if(slotStack.IsEmpty)
                {
                    return;
                }
                
                CursorStack = slotStack.Clone();
                slotStack.Clear();
                RefreshAfterInventoryChange();
                NotifyCursorStackChanged();
                return;
            }
            
            if(slotStack.IsEmpty)
            {
                _slots[slotIndex] = CursorStack.Clone();
                CursorStack.Clear();
                RefreshAfterInventoryChange();
                NotifyCursorStackChanged();
                return;
            }

            if (slotStack.CanStackWith(CursorStack.Item))
            {
                int movedAmount = slotStack.AddAmount(CursorStack.Amount);
                CursorStack.RemoveAmount(movedAmount);
                if (CursorStack.IsEmpty)
                {
                    CursorStack.Clear();
                }

                RefreshAfterInventoryChange();
                NotifyCursorStackChanged();
                return;
            }

            InventoryStack swappedStack = slotStack.Clone();
            _slots[slotIndex] = CursorStack.Clone();
            CursorStack = swappedStack;
            RefreshAfterInventoryChange();
            NotifyCursorStackChanged();
        }

        public void HandleSlotRightClick(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return;
            }

            InventoryStack slotStack = _slots[slotIndex];
            if (CursorStack.IsEmpty)
            {
                if (slotStack.IsEmpty)
                {
                    return;
                }

                int amountToTake = Mathf.CeilToInt(slotStack.Amount * 0.5f);
                CursorStack = new InventoryStack(slotStack.Item, amountToTake);
                slotStack.RemoveAmount(amountToTake);
                RefreshAfterInventoryChange();
                NotifyCursorStackChanged();
                return;
            }

            if (slotStack.IsEmpty)
            {
                _slots[slotIndex] = new InventoryStack(CursorStack.Item, 1);
                CursorStack.RemoveAmount(1);
                RefreshAfterInventoryChange();
                NotifyCursorStackChanged();
                return;
            }

            if (!slotStack.CanStackWith(CursorStack.Item) || slotStack.IsFull)
            {
                return;
            }

            slotStack.AddAmount(1);
            CursorStack.RemoveAmount(1);
            RefreshAfterInventoryChange();
            NotifyCursorStackChanged();
        }

        private void NotifyCursorStackChanged()
        {
            if (CursorStack.IsEmpty)
            {
                CursorStack.Clear();
            }

            OnCursorStackChanged?.Invoke(CursorStack.Clone());
        }

        private void QuickMoveSlot(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return;
            }

            InventoryStack sourceStack = _slots[slotIndex];
            if (sourceStack.IsEmpty)
            {
                return;
            }

            bool isHotbarSlot = slotIndex < HotbarSlotCount;
            int targetStart = isHotbarSlot ? HotbarSlotCount : 0;
            int targetEnd = isHotbarSlot ? _slots.Count : HotbarSlotCount;

            MoveSlotIntoRange(slotIndex, targetStart, targetEnd);
            RefreshAfterInventoryChange();
        }

        private void MoveSlotIntoRange(int sourceIndex, int targetStart, int targetEnd)
        {
            InventoryStack sourceStack = _slots[sourceIndex];
            if (sourceStack.IsEmpty)
            {
                return;
            }

            for (int targetIndex = targetStart; targetIndex < targetEnd && !sourceStack.IsEmpty; targetIndex++)
            {
                if (targetIndex == sourceIndex)
                {
                    continue;
                }

                InventoryStack targetStack = _slots[targetIndex];
                if (!targetStack.CanStackWith(sourceStack.Item) || targetStack.IsFull)
                {
                    continue;
                }

                int movedAmount = targetStack.AddAmount(sourceStack.Amount);
                sourceStack.RemoveAmount(movedAmount);
            }

            for (int targetIndex = targetStart; targetIndex < targetEnd && !sourceStack.IsEmpty; targetIndex++)
            {
                if (targetIndex == sourceIndex)
                {
                    continue;
                }

                InventoryStack targetStack = _slots[targetIndex];
                if (!targetStack.IsEmpty)
                {
                    continue;
                }

                targetStack.Set(sourceStack.Item, sourceStack.Amount);
                sourceStack.Clear();
            }
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
            
            SelectedHotbarStack = _slots[SelectedHotbarSlotIndex].Clone();
        }

        public InventoryStack GetSlot(int slotIndex)
        {
            return IsValidSlotIndex(slotIndex) ? _slots[slotIndex] : new InventoryStack();
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
