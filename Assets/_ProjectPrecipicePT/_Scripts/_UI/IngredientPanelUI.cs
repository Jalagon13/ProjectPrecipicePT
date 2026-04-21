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
        [SerializeField] private Image _NotAvailableOverlay;
        
        private ItemRequirement _itemReq;
        
        private void Start()
        {
            InventoryManager.Instance.OnInventoryChanged += UpdateIngredientStatus;
        }
        
        private void OnDestroy()
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateIngredientStatus;
        }

        private void UpdateIngredientStatus()
        {
            Debug.Log($"Ingredient Panel Updated");
        }

        public void Setup(ItemRequirement itemRequirement)
        {
            _iconImage.sprite = itemRequirement.Item.InventoryIcon;
            _nameText.text = itemRequirement.Item.ItemName;
            _amountNeededText.text = $"x{itemRequirement.Amount}";
            _itemReq = itemRequirement;
        }
        
        private bool HasRequiredIngredient()
        {
            // Loop through the inventory and check if I have enough of this ingredient or not.
            // Create a function in the InventoryManager for this
        
            return true;
        }
    }
}
