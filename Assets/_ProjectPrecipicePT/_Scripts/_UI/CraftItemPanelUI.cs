using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectPrecipicePT
{
    public class CraftItemPanelUI : MonoBehaviour
    {
        [SerializeField] private GameObject _contentPanel;
        [SerializeField] private Image _outputItemImage;
        [SerializeField] private TextMeshProUGUI _outputItemNameText;
        [SerializeField] private GameObject _ingredientListSectionUI;
        [SerializeField] private IngredientPanelUI _ingredientPanelUIPrefab;
        
        private void Awake()
        {
            Hide();
        }
    
        public void UpdatePanel(RecipeSO recipe)
        {
            PopulateCraftItemPanelUI(recipe);
            Show();
        }

        private void PopulateCraftItemPanelUI(RecipeSO recipe)
        {
            _outputItemImage.sprite = recipe.OutputItem.InventoryIcon;
            _outputItemNameText.text = recipe.OutputItem.ItemName;
            
            ClearIngredientListSectionUI();
            PopulateIngredientListSectionUI(recipe);
        }

        private void ClearIngredientListSectionUI()
        {
            foreach (Transform child in _ingredientListSectionUI.transform)
            {
                Destroy(child.gameObject);
            }
        }

        private void PopulateIngredientListSectionUI(RecipeSO recipe)
        {
            for (int i = 0; i < recipe.Requirements.Count; i++)
            {
                ItemRequirement itemRequirement = recipe.Requirements[i];
                IngredientPanelUI ingredientPanelUI = Instantiate(_ingredientPanelUIPrefab.gameObject, _ingredientListSectionUI.transform).GetComponent<IngredientPanelUI>();
                ingredientPanelUI.Setup(itemRequirement);
            }
        }
        
        public void OnCraftButtonPressed()
        {
            
        }

        public void Show()
        {
            _contentPanel.SetActive(true);
        }
        
        private void Hide()
        {
            _contentPanel.SetActive(false);
        }
    }
}
