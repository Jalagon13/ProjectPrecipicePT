using System;
using UnityEngine;

namespace ProjectPrecipicePT
{
    public class HoldItemPivot : MonoBehaviour
    {
        private WorldItem _currentlyHeldWorldItem;
    
        private void Start()
        {
            InventoryManager.Instance.OnSelectedHotbarSlotChanged += UpdateHoldItem;
        }
        
        private void OnDestroy()
        {
            InventoryManager.Instance.OnSelectedHotbarSlotChanged -= UpdateHoldItem;
        }

        private void UpdateHoldItem(int arg1, InventorySlotItem item)
        {
            if(!item.IsEmpty)
            {
                if (item.Item.WorldItemPrefab == null) return;
                
                ClearCurrentlyHeldWorldItem();

                _currentlyHeldWorldItem = Instantiate(item.Item.WorldItemPrefab, transform).GetComponent<WorldItem>();
                _currentlyHeldWorldItem.SetAsHeldItem();
            }
            else
            {
                ClearCurrentlyHeldWorldItem();
            }
        }
        
        private void ClearCurrentlyHeldWorldItem()
        {
            if (_currentlyHeldWorldItem != null)
            {
                Destroy(_currentlyHeldWorldItem.gameObject);
            }
        }
    }
}
