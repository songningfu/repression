using System.Collections.Generic;
using UnityEngine;

namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// A3 世界切换配置（可配置阈值与场景列表）。
    /// 放置方式（推荐）：Assets/Resources/WorldStateConfig.asset（资源名必须为 WorldStateConfig）
    ///
    /// 兼容说明：
    /// - 老字段 realitySceneName / consciousnessSceneName 仍然保留，作为「床切换的默认目标场景」。
    /// - 新字段 realityScenes / consciousnessScenes 列出该世界所包含的所有场景（含老字段在内）。
    /// - GuessWorldFromScene 同时查列表与老字段，向后兼容。
    /// </summary>
    [CreateAssetMenu(menuName = "SeeAPsychologist/WorldState/WorldStateConfig", fileName = "WorldStateConfig")]
    public sealed class WorldStateConfig : ScriptableObject
    {
        [Header("Default Scenes (床切换时进入这两个)")]
        [Tooltip("床切换到现实世界时加载的默认场景（一般是卧室）。")]
        public string realitySceneName = "Reality";

        [Tooltip("床切换到意识世界时加载的默认场景。")]
        public string consciousnessSceneName = "Consciousness";

        [Header("All Scenes In Each World (用于场景门、World 判定)")]
        [Tooltip("现实世界所包含的所有场景名（含 realitySceneName 也建议加入）。")]
        public List<string> realityScenes = new();

        [Tooltip("意识世界所包含的所有场景名（含 consciousnessSceneName 也建议加入）。")]
        public List<string> consciousnessScenes = new();

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
        [Tooltip("解离度=0 时的灰度强度（0=无效果，1=完全灰度）。")]
        [Range(0f, 1f)] public float grayscaleAtDissociation0 = 0f;

        [Tooltip("解离度=100 时的灰度强度（0=无效果，1=完全灰度）。")]
        [Range(0f, 1f)] public float grayscaleAtDissociation100 = 1f;

        public bool IsRealityScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return false;
            if (sceneName == realitySceneName) return true;
            return realityScenes != null && realityScenes.Contains(sceneName);
        }

        public bool IsConsciousnessScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return false;
            if (sceneName == consciousnessSceneName) return true;
            return consciousnessScenes != null && consciousnessScenes.Contains(sceneName);
        }

        public void ValidateAndFixIfNeeded()
        {
            bedEnterRealityMaxDissociation = Mathf.Clamp(bedEnterRealityMaxDissociation, 0, 100);
            bedEnterConsciousnessMinDissociation = Mathf.Clamp(bedEnterConsciousnessMinDissociation, 0, 100);

            if (!autoFixInvalidThresholds) return;

            if (bedEnterRealityMaxDissociation > bedEnterConsciousnessMinDissociation)
            {
                bedEnterRealityMaxDissociation = bedEnterConsciousnessMinDissociation;
            }
        }
    }
}
