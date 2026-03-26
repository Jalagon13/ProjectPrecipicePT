using FMODUnity;
using UnityEngine;

namespace ProjectPrecipicePT
{
    public class FMODEvents : MonoBehaviour
    {
        public static FMODEvents Instance { get; private set; }

        // [field: Header("Player SFX")]
        // [field: SerializeField] public EventReference PlayerHurtSFX { get; private set; }
        // [field: SerializeField] public EventReference ToolSwingSFX { get; private set; }

        private void Awake()
        {
            Instance = this;
        }
    }
}
