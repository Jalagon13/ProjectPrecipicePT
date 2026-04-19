using System;
using UnityEngine;

namespace ProjectPrecipicePT
{
    [Serializable]
    public class InventorySlotItem
    {
        [SerializeField] private ItemSO _item;

        public InventorySlotItem()
        {
            _item = null;
        }

        public InventorySlotItem(ItemSO item)
        {
            Set(item);
        }

        public ItemSO Item => _item;
        public bool IsEmpty => _item == null;

        public void Set(ItemSO item)
        {
            _item = item;
        }

        public void Clear()
        {
            _item = null;
        }

        public InventorySlotItem Clone()
        {
            return IsEmpty ? new InventorySlotItem() : new InventorySlotItem(_item);
        }
    }
}
