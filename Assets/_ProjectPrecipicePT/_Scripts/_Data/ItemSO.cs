using UnityEngine;

namespace ProjectPrecipicePT
{
    [CreateAssetMenu(fileName = "New Item", menuName = "Project Precipice/Item Data")]
    public class ItemSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Tooltip("Display name shown anywhere this item is referenced in UI.")]
        private string _itemName;
        [SerializeField, TextArea, Tooltip("Longer description for the item.")]
        private string _itemDescription;

        [Header("Visuals")]
        [SerializeField, Tooltip("Sprite used for the inventory icon. This is the fallback approach instead of rendering the prefab automatically.")]
        private Sprite _inventoryIcon;
        [SerializeField, Tooltip("Prefab used when this item exists as a physical object in the world.")]
        private GameObject _worldPrefab;

        public string ItemName => _itemName;
        public string ItemDescription => _itemDescription;
        public Sprite InventoryIcon => _inventoryIcon;
        public GameObject WorldPrefab => _worldPrefab;
    }
}
