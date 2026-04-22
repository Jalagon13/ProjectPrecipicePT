using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectPrecipicePT
{
    public class IngredientPanelUI : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _amountNeededText;
        [SerializeField] private Image _notAvailableOverlay;
        
        private ItemRequirement _itemReq;
        
        public bool HasIngredient { get; private set; }
        
        private void Start()
        {
            InventoryManager.Instance.OnInventoryChanged += UpdateIngredientStatus;
        }
        
        private void OnDestroy()
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateIngredientStatus;
        }

        public void Setup(ItemRequirement itemRequirement)
        {
            _iconImage.sprite = itemRequirement.Item.InventoryIcon;
            _nameText.text = itemRequirement.Item.ItemName;
            _itemReq = itemRequirement;

            UpdateIngredientStatus();
        }

        private void UpdateIngredientStatus()
        {
            if(_itemReq.Item == null) return;
            
            int currentAmount = InventoryManager.Instance.GetItemAmount(_itemReq.Item);
            _amountNeededText.text = $"{currentAmount}/{_itemReq.Amount}";
            HasIngredient = InventoryManager.Instance.HasItemAmount(_itemReq.Item, _itemReq.Amount);
            _notAvailableOverlay.enabled = !HasIngredient;
        }
    }
}
