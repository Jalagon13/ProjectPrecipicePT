using UnityEngine;

namespace ProjectPrecipicePT
{
    public class HealthManager : MonoBehaviour
    {
        public static HealthManager Instance { get; private set; }
        
        [SerializeField] private int _startingMaxHealth = 100;
        
        private void Awake()
        {
            Instance = this;
        }
    }
}
