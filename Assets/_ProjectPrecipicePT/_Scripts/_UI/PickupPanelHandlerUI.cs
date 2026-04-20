using UnityEngine;

namespace ProjectPrecipicePT
{
    public class PickupPanelHandlerUI : MonoBehaviour
    {
        [SerializeField] private PickupPanelUI _pickupPanelUIPrefab;
        
        private void Start()
        {
            InventoryManager.Instance.OnItemPickup += InventoryManager_OnItemPickup;
        }
        
        private void OnDestroy()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemPickup -= InventoryManager_OnItemPickup;
            }
        }
        
        private void InventoryManager_OnItemPickup(ItemSO item, int amount)
        {
            PickupPanelUI pickupPanelUI = Instantiate(_pickupPanelUIPrefab.gameObject, transform).GetComponent<PickupPanelUI>();
            pickupPanelUI.Setup(item, amount);
        }
    }
}
