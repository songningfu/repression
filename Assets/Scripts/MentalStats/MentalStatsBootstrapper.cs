using UnityEngine;

namespace SeeAPsychologist.MentalStats
{
    /// <summary>
    /// 方案A：自动引导创建 MentalStatsManager，确保每个场景都存在（跨场景常驻）。
    ///
    /// 使用方式：
    /// - 将 MentalStatsConfig 资产放入 Resources，并命名为 "MentalStatsConfig"
    ///   例如：Assets/Resources/MentalStatsConfig.asset
    /// - 进入 Play 或运行游戏时，本脚本会在加载任何场景之前自动创建 Manager（若场景里已放则不会重复创建）
    /// </summary>
    public static class MentalStatsBootstrapper
    {
        private const string DefaultConfigResourcePath = "MentalStatsConfig";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (MentalStatsManager.Instance != null) return;

            var go = new GameObject("MentalStatsManager");
            // 先挂 Tag，确保 MentalStatsManager.Awake 能识别该实例来自引导创建。
            go.AddComponent<MentalStatsBootstrapTag>();
            var manager = go.AddComponent<MentalStatsManager>();

            var config = Resources.Load<MentalStatsConfig>(DefaultConfigResourcePath);
            if (config != null)
            {
                manager.SetConfig(config, resetToInitialValues: true, source: MentalStatChangeSource.Unknown);
            }
            else
            {
                Debug.LogWarning(
                    $"[MentalStats] No MentalStatsConfig found at Resources/{DefaultConfigResourcePath}. " +
                    "MentalStatsManager will use default values (Stress=0, Dissociation=0, tickSeconds=5, tickDelta=1).");
            }
        }
    }
}

