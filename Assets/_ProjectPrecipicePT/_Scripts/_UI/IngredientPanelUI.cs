using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectPrecipicePT
{
    public class IngredientPanelUI : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;
    
        public void Setup(ItemRequirement itemRequirement)
        {
            _iconImage.sprite = itemRequirement.Item.InventoryIcon;
            _nameText.text = itemRequirement.Item.ItemName;
        }
    }
}
