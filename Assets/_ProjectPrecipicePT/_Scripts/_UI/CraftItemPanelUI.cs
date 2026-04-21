using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectPrecipicePT
{
    public enum CraftItemPanelState
    {
        Idle,
        Crafting
    }

    public class CraftItemPanelUI : MonoBehaviour
    {
        [SerializeField] private GameObject _contentPanel;
        [SerializeField] private Image _outputItemImage;
        [SerializeField] private TextMeshProUGUI _outputItemNameText;
        [SerializeField] private GameObject _ingredientListSectionUI;
        [SerializeField] private IngredientPanelUI _ingredientPanelUIPrefab;
        
        [Header("Craft Button")]
        [SerializeField] private Image _craftButtonCantCraftOverlay;
        [SerializeField] private Image _craftButtonProgressBar;
        [SerializeField] private TextMeshProUGUI _craftButtonText;
        [SerializeField] private string _idleCraftButtonText = "Craft Item";
        [SerializeField] private string _craftingCraftButtonText = "Crafting...";
        [SerializeField] private float _craftingDuration = 2f;
        
        private List<IngredientPanelUI> _ingredientList;
        
        private RecipeSO _currentRecipe;
        private Timer _craftingTimer;

        public CraftItemPanelState State { get; private set; } = CraftItemPanelState.Idle;

        private void Awake()
        {
            Hide();
        }
        
        private void Start()
        {
            InventoryManager.Instance.OnInventoryChanged += UpdateCraftButtonOverlay;
        }

        private void Update()
        {
            if (State == CraftItemPanelState.Crafting && _craftingTimer != null)
            {
                _craftingTimer.Tick(Time.deltaTime);
                _craftButtonProgressBar.fillAmount = _craftingTimer.GetPercentComplete();
            }
        }
        
        private void OnDestroy()
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateCraftButtonOverlay;
            if (_craftingTimer != null)
            {
                _craftingTimer.OnTimerEnd -= HandleCraftingComplete;
            }
        }

        public void UpdatePanel(RecipeSO recipe)
        {
            _currentRecipe = recipe;
            _craftButtonCantCraftOverlay.enabled = false;

            if (State == CraftItemPanelState.Idle)
            {
                _craftButtonProgressBar.fillAmount = 0f;
                _craftButtonProgressBar.enabled = false;
                _craftButtonText.text = _idleCraftButtonText;
            }
            else
            {
                _craftButtonProgressBar.enabled = true;
                _craftButtonText.text = _craftingCraftButtonText;
            }

            PopulateCraftItemPanelUI(recipe);
            UpdateCraftButtonOverlay();
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

            if (_ingredientList == null)
            {
                _ingredientList = new();
            }
            else
            {
                _ingredientList.Clear();
            }
        }

        private void PopulateIngredientListSectionUI(RecipeSO recipe)
        {
            for (int i = 0; i < recipe.Requirements.Count; i++)
            {
                ItemRequirement itemRequirement = recipe.Requirements[i];
                IngredientPanelUI ingredientPanelUI = Instantiate(_ingredientPanelUIPrefab.gameObject, _ingredientListSectionUI.transform).GetComponent<IngredientPanelUI>();
                ingredientPanelUI.Setup(itemRequirement);
                
                _ingredientList.Add(ingredientPanelUI);
            }
        }
        
        public void OnCraftButtonPressed()
        {
            if (State == CraftItemPanelState.Crafting) return;

            foreach (IngredientPanelUI ingredientPanelUI in _ingredientList)
            {
                if(!ingredientPanelUI.HasIngredient)
                {
                    return;
                }
            }
            
            State = CraftItemPanelState.Crafting;
            _craftButtonProgressBar.enabled = true;
            _craftButtonProgressBar.fillAmount = 0f;
            _craftButtonText.text = _craftingCraftButtonText;
            _craftingTimer = new Timer(_craftingDuration);
            _craftingTimer.OnTimerEnd += HandleCraftingComplete;
            UpdateCraftButtonOverlay();
        }

        private void HandleCraftingComplete(object sender, EventArgs e)
        {
            _craftingTimer.OnTimerEnd -= HandleCraftingComplete;

            InventoryManager.Instance.AddItem(_currentRecipe.OutputItem, _currentRecipe.OutputAmount);
            
            foreach (var item in _currentRecipe.Requirements)
            {
                InventoryManager.Instance.RemoveItem(item.Item, item.Amount);
            }

            State = CraftItemPanelState.Idle;
            _craftButtonProgressBar.fillAmount = 0f;
            _craftButtonProgressBar.enabled = false;
            _craftButtonText.text = _idleCraftButtonText;
            UpdateCraftButtonOverlay();
        }


        private void UpdateCraftButtonOverlay()
        {
            if (State == CraftItemPanelState.Crafting)
            {
                _craftButtonCantCraftOverlay.enabled = true;
                return;
            }

            if(_ingredientList == null || _ingredientList.Count == 0)
            {
                _craftButtonCantCraftOverlay.enabled = true;
                return;
            }
        
            foreach (IngredientPanelUI ingredientPanelUI in _ingredientList)
            {
                if (!ingredientPanelUI.HasIngredient)
                {
                    _craftButtonCantCraftOverlay.enabled = true;
                    return;
                }
            }

            _craftButtonCantCraftOverlay.enabled = false;
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
