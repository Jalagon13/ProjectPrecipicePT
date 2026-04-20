using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectPrecipicePT
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private InventorySlotUI _slotPrefab;
        [SerializeField] private RectTransform _hotbarPanel;
        [SerializeField] private RectTransform _inventoryPanel;
        [SerializeField] private RectTransform _inventoryPivot;
        [SerializeField] private Image _inventoryBackground;
        [SerializeField] private int _inventoryDownOffset = 300;


        [Header("Cursor Inventory UI")]
        [SerializeField] private RectTransform _dragItemRoot;
        [SerializeField] private Image _dragItemIcon;
        [SerializeField] private TextMeshProUGUI _dragItemCountText;

        private readonly List<InventorySlotUI> _slotUis = new();
        private Vector2 _inventoryPivotPosition;

        private void Awake()
        {
            _inventoryPivotPosition = _inventoryPivot.anchoredPosition;
        }

        private void Start()
        {
            BuildSlots();
            RefreshAll();
            
            InventoryManager.Instance.OnInventoryChanged += RefreshAll;
            InventoryManager.Instance.OnSelectedHotbarSlotChanged += HandleSelectedHotbarChanged;
            InventoryManager.Instance.OnInventoryOpenChanged += SetInventoryVisible;
            InventoryManager.Instance.OnCursorStackChanged += RefreshDragItem;
        }
        
        private void OnDestroy()
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshAll;
            InventoryManager.Instance.OnSelectedHotbarSlotChanged -= HandleSelectedHotbarChanged;
            InventoryManager.Instance.OnInventoryOpenChanged -= SetInventoryVisible;
            InventoryManager.Instance.OnCursorStackChanged -= RefreshDragItem;
        }
        
        private void Update()
        {
            UpdateDragItemPosition();
        }

        private void RefreshDragItem(InventorySlotItem cursorStack)
        {
            bool shouldShow = InventoryManager.Instance.IsInventoryOpen &&
                cursorStack != null && !cursorStack.IsEmpty;


            _dragItemRoot.gameObject.SetActive(shouldShow);

            if (!shouldShow) return;

            _dragItemIcon.sprite = cursorStack.Item.InventoryIcon;
            _dragItemIcon.enabled = cursorStack.Item.InventoryIcon != null;
            _dragItemCountText.text = string.Empty;
        }

        private void HandleSelectedHotbarChanged(int arg1, InventorySlotItem stack)
        {
            RefreshAll();
        }

        private void BuildSlots()
        {
            CreateSlotRange(0, InventoryManager.Instance.HotbarSlotCount, _hotbarPanel, true);
            CreateSlotRange(InventoryManager.Instance.HotbarSlotCount, InventoryManager.Instance.Slots.Count, _inventoryPanel, false);
        }

        private void CreateSlotRange(int startIndex, int endIndex, RectTransform parent, bool isHotbarSlot)
        {
            for (int slotIndex = startIndex; slotIndex < endIndex; slotIndex++)
            {
                InventorySlotUI slotUi = CreateSlotInstance(parent);
                slotUi.Initialize(this, slotIndex, isHotbarSlot);
                _slotUis.Add(slotUi);
            }
        }

        private InventorySlotUI CreateSlotInstance(RectTransform parent)
        {
            InventorySlotUI slot = Instantiate(_slotPrefab, parent);
            slot.transform.localScale = Vector3.one;
            return slot;
        }

        private void RefreshAll()
        {
            foreach (InventorySlotUI slotUi in _slotUis)
            {
                InventorySlotItem stack = InventoryManager.Instance.GetSlot(slotUi.SlotIndex);
                bool isSelectedHotbarSlot = slotUi.IsHotbarSlot && slotUi.SlotIndex == InventoryManager.Instance.SelectedHotbarSlotIndex;
                slotUi.Refresh(stack, isSelectedHotbarSlot);
            }

            RefreshDragItem(InventoryManager.Instance.CursorStack);
        }

        public void SetInventoryVisible(bool isVisible)
        {
            _inventoryPanel.gameObject.SetActive(isVisible);
            _inventoryBackground.gameObject.SetActive(isVisible);
            _inventoryPivot.anchoredPosition = isVisible ? Vector2.down * _inventoryDownOffset : _inventoryPivotPosition;

            if (!isVisible)
            {
                _dragItemRoot.gameObject.SetActive(false);
            }
            else
            {
                RefreshDragItem(InventoryManager.Instance.CursorStack);
            }
        }

        private void UpdateDragItemPosition()
        {
            if (_dragItemRoot == null || !_dragItemRoot.gameObject.activeSelf || Mouse.current == null)
            {
                return;
            }

            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            _dragItemRoot.position = mouseScreenPosition;
        }

        public void HandleSlotClick(int slotIndex, PointerEventData.InputButton button)
        {
            bool isShiftHeld = Keyboard.current != null &&
               ((Keyboard.current.leftShiftKey?.isPressed ?? false) ||
                (Keyboard.current.rightShiftKey?.isPressed ?? false));

            if (button == PointerEventData.InputButton.Left)
            {
                InventoryManager.Instance.HandleSlotLeftClick(slotIndex, isShiftHeld);
            }
        }
    }
}
