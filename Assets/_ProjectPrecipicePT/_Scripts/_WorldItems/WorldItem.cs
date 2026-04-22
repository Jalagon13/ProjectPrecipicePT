using System;
using UnityEngine;

namespace ProjectPrecipicePT
{
    public class WorldItem : MonoBehaviour
    {
        [SerializeField] protected ItemSO _itemSO;
        
        public bool CanInteractWith = true;

        public void OnInteract()
        {
            if(!CanInteractWith) return;
        
            InventoryManager.Instance.AddItem(_itemSO);
            Debug.Log("Picked up: " + _itemSO.ItemName);
            Destroy(gameObject);
        }

        public void SetAsHeldItem()
        {
            CanInteractWith = false;
            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<Collider>().isTrigger = true;
        }
    }
}
