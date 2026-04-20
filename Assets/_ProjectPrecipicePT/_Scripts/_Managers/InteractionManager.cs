using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectPrecipicePT
{
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance { get; private set; }

        [SerializeField, Min(0f)] private float _interactDistance = 3f;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            GameInput.Instance.OnInteract += HandleInteract;
        }

        private void OnDestroy()
        {
            GameInput.Instance.OnInteract -= HandleInteract;
        }

        private void HandleInteract(object sender, InputAction.CallbackContext context)
        {
            if (context.started)
            {
                ShootInteractRay();
            }
        }

        private void ShootInteractRay()
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, _interactDistance))
            {
                if (hit.collider.gameObject.TryGetComponent<WorldItem>(out WorldItem worldItem))
                {
                    worldItem.OnInteract();
                }
            }   
        }
    }
}
