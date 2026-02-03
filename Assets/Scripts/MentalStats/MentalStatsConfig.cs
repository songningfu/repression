using UnityEngine;

namespace SeeAPsychologist.MentalStats
{
    /// <summary>
    /// A1 心理稳态数值配置。
    /// - Stress / Dissociation 范围固定为 0-100（由 Manager 强制 Clamp）。
    /// - 自动变化规则：Stress=100 时 Dissociation 每 tickSeconds +tickDelta；Stress=0 时每 tickSeconds -tickDelta。
    /// </summary>
    [CreateAssetMenu(menuName = "SeeAPsychologist/MentalStats/MentalStatsConfig", fileName = "MentalStatsConfig")]
    public sealed class MentalStatsConfig : ScriptableObject
    {
        [Header("Initial Values (Clamped to 0-100)")]
        [Range(0, 100)] public int initialStress = 0;
        [Range(0, 100)] public int initialDissociation = 0;

        [Header("Auto Dissociation Tick (Stress == 0 or 100)")]
        [Min(0.1f)] public float tickSeconds = 5f;
        [Min(1)] public int tickDelta = 1;
    }
}

