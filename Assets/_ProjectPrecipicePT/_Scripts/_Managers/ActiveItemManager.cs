using UnityEngine;

namespace ProjectPrecipicePT
{
    public class ActiveItemManager : MonoBehaviour
    {
        public static ActiveItemManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }
    }
}