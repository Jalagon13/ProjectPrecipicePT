using UnityEngine;

namespace ProjectPrecipicePT
{
    [CreateAssetMenu(fileName = "New Consumable", menuName = "Project Precipice/Consumable Item Data")]
    public class ConsumableItemSO : ItemSO
    {
        [Header("Consumable Settings")]
        [field: SerializeField, Min(0)] public int ReplenishValue { get; private set; } = 4;
    }
}
