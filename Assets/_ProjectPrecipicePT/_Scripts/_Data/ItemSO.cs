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

        [Header("Stacking")]
        [SerializeField, Tooltip("Whether multiple copies of this item can stack together in one slot.")]
        private bool _isStackable = true;
        [SerializeField, Min(1), Tooltip("Maximum number of this item allowed in one stack when the item is stackable.")]
        private int _maxStackSize = 64;

        public string ItemName => _itemName;
        public string ItemDescription => _itemDescription;
        public Sprite InventoryIcon => _inventoryIcon;
        public GameObject WorldPrefab => _worldPrefab;
        public bool IsStackable => _isStackable;
        public int MaxStackSize => _isStackable ? Mathf.Max(1, _maxStackSize) : 1;
    }
}
