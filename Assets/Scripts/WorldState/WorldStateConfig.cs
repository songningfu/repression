using UnityEngine;

namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// A3 世界切换配置（可配置阈值与场景名，禁止硬编码魔法数）。
    /// 放置方式（推荐）：
    /// - Assets/Resources/WorldStateConfig.asset（资源名必须为 WorldStateConfig）
    /// </summary>
    [CreateAssetMenu(menuName = "SeeAPsychologist/WorldState/WorldStateConfig", fileName = "WorldStateConfig")]
    public sealed class WorldStateConfig : ScriptableObject
    {
        [Header("Scene Names")]
        [Tooltip("现实世界场景名（必须与 Build Settings / Scene Asset 名一致）。")]
        public string realitySceneName = "Reality";

        [Tooltip("意识世界场景名（必须与 Build Settings / Scene Asset 名一致）。")]
        public string consciousnessSceneName = "Consciousness";

        [Tooltip("当无法从当前 SceneName 推断世界时，使用该值作为初始世界。")]
        public WorldType fallbackInitialWorld = WorldType.Reality;

        [Header("Bed Thresholds (by Dissociation 0-100)")]
        [Tooltip("解离度 <= 该值：床交互会进入现实世界。")]
        [Range(0, 100)] public int bedEnterRealityMaxDissociation = 10;

        [Tooltip("解离度 >= 该值：床交互会进入意识世界。")]
        [Range(0, 100)] public int bedEnterConsciousnessMinDissociation = 90;

        [Header("Safety")]
        [Tooltip("若配置不合法（例如 min > max），是否在运行时自动纠正到可用范围。")]
        public bool autoFixInvalidThresholds = true;

        [Header("VFX - Grayscale (Camera Filter)")]
        [Tooltip("解离度=0 时的灰度强度（0=无效果，1=完全灰度）。需求：解离度越高画面越灰。")]
        [Range(0f, 1f)] public float grayscaleAtDissociation0 = 0f;

        [Tooltip("解离度=100 时的灰度强度（0=无效果，1=完全灰度）。")]
        [Range(0f, 1f)] public float grayscaleAtDissociation100 = 1f;

        public void ValidateAndFixIfNeeded()
        {
            bedEnterRealityMaxDissociation = Mathf.Clamp(bedEnterRealityMaxDissociation, 0, 100);
            bedEnterConsciousnessMinDissociation = Mathf.Clamp(bedEnterConsciousnessMinDissociation, 0, 100);

            if (!autoFixInvalidThresholds) return;

            // 允许“中间区间无切换”的设计：RealityMax < ConsciousnessMin
            // 若反了（RealityMax > ConsciousnessMin），则把两者拉回到相邻范围，避免出现“同时满足两边”。
            if (bedEnterRealityMaxDissociation > bedEnterConsciousnessMinDissociation)
            {
                bedEnterRealityMaxDissociation = bedEnterConsciousnessMinDissociation;
            }
        }
    }
}

