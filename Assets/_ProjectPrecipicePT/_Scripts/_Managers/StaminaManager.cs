using UnityEngine;

namespace ProjectPrecipicePT
{
    public class StaminaManager : MonoBehaviour
    {
        public static StaminaManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }
        
        
    }
}
