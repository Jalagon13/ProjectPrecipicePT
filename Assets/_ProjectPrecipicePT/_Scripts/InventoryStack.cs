using System;
using UnityEngine;

namespace ProjectPrecipicePT
{
    [Serializable]
    public class InventoryStack
    {
        [SerializeField] private ItemSO _item;
        [SerializeField, Min(0)] private int _amount;

        public InventoryStack()
        {
            _item = null;
            _amount = 0;
        }

        public InventoryStack(ItemSO item, int amount)
        {
            Set(item, amount);
        }

        public ItemSO Item => _item;
        public int Amount => _amount;
        public bool IsEmpty => _item == null || _amount <= 0;
        public bool IsFull => !IsEmpty && _amount >= _item.MaxStackSize;

        public void Set(ItemSO item, int amount)
        {
            _item = item;
            _amount = item == null ? 0 : Mathf.Max(0, amount);

            if (_item == null || _amount <= 0)
            {
                Clear();
                return;
            }

            if (!_item.IsStackable)
            {
                _amount = 1;
            }
            else
            {
                _amount = Mathf.Min(_amount, _item.MaxStackSize);
            }
        }

        public void Clear()
        {
            _item = null;
            _amount = 0;
        }

        public bool CanStackWith(ItemSO item)
        {
            return !IsEmpty &&
                   item != null &&
                   _item == item &&
                   _item.IsStackable;
        }

        public int AddAmount(int amountToAdd)
        {
            if (IsEmpty || amountToAdd <= 0)
            {
                return 0;
            }

            int room = _item.MaxStackSize - _amount;
            int addedAmount = Mathf.Clamp(amountToAdd, 0, room);
            _amount += addedAmount;
            return addedAmount;
        }

        public int RemoveAmount(int amountToRemove)
        {
            if (IsEmpty || amountToRemove <= 0)
            {
                return 0;
            }

            int removedAmount = Mathf.Clamp(amountToRemove, 0, _amount);
            _amount -= removedAmount;

            if (_amount <= 0)
            {
                Clear();
            }

            return removedAmount;
        }

        public InventoryStack Clone()
        {
            return IsEmpty ? new InventoryStack() : new InventoryStack(_item, _amount);
        }
    }
}