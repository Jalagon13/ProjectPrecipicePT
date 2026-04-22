using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectPrecipicePT
{
    public class WorldItemConsumable : WorldItem, IActionable
    {
        private void Start()
        {
            GameInput.Instance.OnPrimaryAction += OnPrimaryAction;
            GameInput.Instance.OnSecondaryAction += OnSecondaryAction;
        }
        
        private void OnDestroy()
        {
            GameInput.Instance.OnPrimaryAction -= OnPrimaryAction;
            GameInput.Instance.OnSecondaryAction -= OnSecondaryAction;
        }

        private void OnPrimaryAction(object sender, InputAction.CallbackContext e)
        {
            if(InventoryManager.Instance.CraftingMenuUI.CraftingMenuUIOpen) return;
        
            if(e.started)
            {
                OnPrimaryActionStarted();
            }
            else if(e.performed)
            {
                OnPrimaryActionPerformed();
            }
            else if(e.canceled)
            {
                OnPrimaryActionCanceled();
            }
        }

        private void OnSecondaryAction(object sender, InputAction.CallbackContext e)
        {
            if (InventoryManager.Instance.CraftingMenuUI.CraftingMenuUIOpen) return;

            if (e.started)
            {
                OnSecondaryActionStarted();
            }
            else if (e.performed)
            {
                OnSecondaryActionPerformed();
            }
            else if (e.canceled)
            {
                OnSecondaryActionCanceled();
            }
        }

        public void OnPrimaryActionStarted()
        {
            if(_itemSO is not ConsumableItemSO)
            {
                Debug.LogError($"You are trying to consumbe an item that is not a consumable item");
                return;
            }
            Debug.Log($"Consuming: {_itemSO.ItemName}");
            HungerManager.Instance.RemoveHungerPoints((_itemSO as ConsumableItemSO).ReplenishValue);
            InventoryManager.Instance.ClearSelectedSlotItem();
        }

        public void OnPrimaryActionPerformed()
        {
            
        }

        public void OnPrimaryActionCanceled()
        {
            
        }

        public void OnSecondaryActionStarted()
        {
            
        }

        public void OnSecondaryActionPerformed()
        {
            
        }

        public void OnSecondaryActionCanceled()
        {
            
        }
    }
}