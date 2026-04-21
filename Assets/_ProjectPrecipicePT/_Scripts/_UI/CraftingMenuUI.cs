using UnityEngine;
using System.Collections.Generic;
using System;

namespace ProjectPrecipicePT
{
    public class CraftingMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject _recipeListPanelUI;
        [SerializeField] private RecipePanelUI _recipePanelUIPrefab;
        
        [SerializeField] private CraftItemPanelUI _craftItemPanelUI;
        public CraftItemPanelUI CraftItemPanelUI => _craftItemPanelUI;
        
        [SerializeField] private List<RecipeSO> _defaultRecipes;
        
        private RecipeSO _selectedRecipe;

        public RecipeSO SelectedRecipe 
        { 
            get { return _selectedRecipe; } 
            set 
            { 
                _selectedRecipe = value;
                _craftItemPanelUI.UpdatePanel(_selectedRecipe);
            }  
        }

        private void Start()
        {
            HideCraftingMenu();

            InventoryManager.Instance.OnInventoryOpenChanged += SetCraftingMenuVisible;
        }

        private void OnDestroy()
        {
            InventoryManager.Instance.OnInventoryOpenChanged -= SetCraftingMenuVisible;
        }

        private void SetCraftingMenuVisible(bool isVisible)
        {
            if(isVisible)
            {
                ShowCraftingMenu();
            }
            else
            {
                HideCraftingMenu();
            }
        }
        
        private void ShowCraftingMenu()
        {
            gameObject.SetActive(true);
            ClearRecipeListPanelUI();
            PopulateRecipeListPanelUI();
        }
        
        private void HideCraftingMenu()
        {
            gameObject.SetActive(false);
        }

        private void ClearRecipeListPanelUI()
        {
            foreach (Transform child in _recipeListPanelUI.transform)
            {
                Destroy(child.gameObject);
            }
        }

        private void PopulateRecipeListPanelUI()
        {
            for (int i = 0; i < _defaultRecipes.Count; i++)
            {
                RecipeSO recipe = _defaultRecipes[i];
                RecipePanelUI recipePanelUI = Instantiate(_recipePanelUIPrefab.gameObject, _recipeListPanelUI.transform).GetComponent<RecipePanelUI>();
                recipePanelUI.Setup(recipe, this);
            }
        }
    }
}
