using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectPrecipicePT
{
    public class InventoryBackgroundUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private float _dropForce = 5f;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (InventoryManager.Instance.CursorStack.IsEmpty)
            {
                return;
            }

            ItemSO item = InventoryManager.Instance.CursorStack.Item;

            if (item != null && item.WorldItemPrefab != null)
            {
                Transform cameraTransform = Player.Instance.CameraTransform;

                WorldItem worldItem = Instantiate(item.WorldItemPrefab, cameraTransform.position, Quaternion.identity);

                if (worldItem.TryGetComponent(out Rigidbody rb))
                {
                    rb.AddForce(cameraTransform.forward * _dropForce, ForceMode.Impulse);
                }
            }

            InventoryManager.Instance.ClearCursorStack();
        }
    }
}
