using UnityEngine;

namespace ProjectPrecipicePT
{
    public class WorldItem : MonoBehaviour
    {
        [SerializeField] private ItemSO _itemSO;

        public void OnInteract()
        {
            InventoryManager.Instance.AddItem(_itemSO);
            Debug.Log("Picked up: " + _itemSO.ItemName);
            Destroy(gameObject);
        }
    }
}
