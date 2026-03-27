using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectPrecipicePT
{
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _selectionImage;
        [SerializeField] private TextMeshProUGUI _countText;
        
        private InventoryUI _inventoryUI;
        
        public int SlotIndex { get; private set; }
        public bool IsHotbarSlot { get; private set; }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            _inventoryUI.HandleSlotClick(SlotIndex, eventData.button);
        }
        
        public void Initialize(InventoryUI inventoryUI, int slotIndex, bool isHotbarSlot)
        {
            SlotIndex = slotIndex;
            IsHotbarSlot = isHotbarSlot;
            _inventoryUI = inventoryUI;
            name = isHotbarSlot ? $"Hotbar Slot {slotIndex + 1}" : $"Inventory Slot {slotIndex + 1}";
        }
        
        public void Refresh(InventoryStack stack, bool isSelected)
        {
            _selectionImage.enabled = isSelected;

            bool showItem = stack != null && !stack.IsEmpty && stack.Item != null;
            _iconImage.enabled = showItem && stack.Item.InventoryIcon != null;
            _iconImage.sprite = showItem ? stack.Item.InventoryIcon : null;
            _countText.text = showItem && stack.Amount > 1 ? stack.Amount.ToString() : string.Empty;
        }
    }
}
