using UnityEngine;
using SeeAPsychologist.MentalStats;

namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// 床交互桥接（挂到 bed 上即可）。
    /// - 由外部交互系统在"按F"时调用 Interact()
    /// - 只在真正切换世界时播放闭眼/睁眼效果
    /// </summary>
    public sealed class BedWorldSwitchInteractable : MonoBehaviour
    {
        [Tooltip("默认床交互类型（按解离度阈值决定进入现实/意识）。")]
        [SerializeField] private InteractionType interactionType = InteractionType.Bed;

        [Tooltip("可选上下文（用于日志追踪）。")]
        [SerializeField] private string context = "bed";

        [Header("调试选项")]
        [Tooltip("忽略解离度限制，总是允许切换（仅用于测试）")]
        [SerializeField] private bool ignoreDisociationCheck = false;

        /// <summary>
        /// 交互入口：由输入/交互系统调用。
        /// </summary>
        public void Interact()
        {
            var mgr = WorldStateManager.Instance;
            if (mgr == null)
            {
                Debug.LogWarning("[A3][WorldState] WorldStateManager missing; bed interact ignored.");
                return;
            }

            // 先判断是否会切换世界（不实际切换）
            bool willSwitch = CheckIfWillSwitch(mgr, out WorldType targetWorld);

            if (willSwitch)
            {
                // 将床的位置设置为目标世界的出生点
                if (PlayerPositionManager.Instance != null)
                {
                    // 使用床的位置作为目标世界的出生点
                    Vector3 bedPosition = transform.position;
                    PlayerPositionManager.Instance.SetSavedPosition(targetWorld, bedPosition);
                    Debug.Log($"[BedWorldSwitch] 设置 {targetWorld} 世界出生点为床的位置: {bedPosition}");
                }

                // 提前通知音乐管理器准备切换音乐（在转场开始时）
                if (WorldMusicManager.Instance != null)
                {
                    WorldMusicManager.Instance.PrepareMusicForWorld(targetWorld);
                }

                // 会切换世界 - 使用闭眼/睁眼转场效果
                if (EyeBlinkTransition.Instance != null)
                {
                    EyeBlinkTransition.Instance.PlayTransition(() =>
                    {
                        // 在黑屏时执行切换
                        var switched = mgr.TrySwitchWorld(interactionType, out var actualTargetWorld, context);
                        Debug.Log($"[A3][WorldState] Bed interact => switched={switched}, target={actualTargetWorld}");
                    });
                }
                else
                {
                    // 没有转场效果，直接切换
                    var switched = mgr.TrySwitchWorld(interactionType, out var actualTargetWorld, context);
                    Debug.Log($"[A3][WorldState] Bed interact => switched={switched}, target={actualTargetWorld}");
                }
            }
            else
            {
                // 不会切换世界 - 不播放转场效果
                Debug.Log($"[A3][WorldState] Bed interact => switched=false (解离度不够或已在目标世界)");
            }
        }

        /// <summary>
        /// 检查是否会切换世界（不实际执行切换）
        /// </summary>
        private bool CheckIfWillSwitch(WorldStateManager mgr, out WorldType targetWorld)
        {
            targetWorld = mgr.CurrentWorld;
            
            var stats = MentalStatsManager.Instance;
            var dissociation = stats != null ? stats.CurrentDissociation : 0;

            Debug.Log($"[BedWorldSwitch] 当前世界: {mgr.CurrentWorld}, 解离度: {dissociation}");

            // 根据交互类型判断目标世界
            switch (interactionType)
            {
                case InteractionType.Bed:
                    // 获取配置
                    var config = Resources.Load<WorldStateConfig>("WorldStateConfig");
                    if (config == null)
                    {
                        Debug.LogError("[BedWorldSwitch] WorldStateConfig 未找到！");
                        return false;
                    }

                    // 如果启用了忽略解离度检查，直接切换到另一个世界
                    if (ignoreDisociationCheck)
                    {
                        targetWorld = mgr.CurrentWorld == WorldType.Reality 
                            ? WorldType.Consciousness 
                            : WorldType.Reality;
                        Debug.Log($"[BedWorldSwitch] 忽略解离度检查，目标世界: {targetWorld}");
                        return targetWorld != mgr.CurrentWorld;
                    }

                    // 判断目标世界
                    if (dissociation <= config.bedEnterRealityMaxDissociation)
                    {
                        targetWorld = WorldType.Reality;
                        Debug.Log($"[BedWorldSwitch] 解离度 {dissociation} <= {config.bedEnterRealityMaxDissociation}, 目标: Reality");
                    }
                    else if (dissociation >= config.bedEnterConsciousnessMinDissociation)
                    {
                        targetWorld = WorldType.Consciousness;
                        Debug.Log($"[BedWorldSwitch] 解离度 {dissociation} >= {config.bedEnterConsciousnessMinDissociation}, 目标: Consciousness");
                    }
                    else
                    {
                        // 解离度在中间区间，不切换
                        Debug.LogWarning($"[BedWorldSwitch] 解离度 {dissociation} 在中间区间 ({config.bedEnterRealityMaxDissociation}-{config.bedEnterConsciousnessMinDissociation})，无法切换");
                        return false;
                    }
                    break;

                case InteractionType.MedicineToReality:
                    targetWorld = WorldType.Reality;
                    break;

                case InteractionType.MedicineToConsciousness:
                case InteractionType.StoryEventToConsciousness:
                    targetWorld = WorldType.Consciousness;
                    break;

                default:
                    return false;
            }

            // 如果目标世界和当前世界相同，不切换
            bool willSwitch = targetWorld != mgr.CurrentWorld;
            Debug.Log($"[BedWorldSwitch] 目标世界: {targetWorld}, 当前世界: {mgr.CurrentWorld}, 是否切换: {willSwitch}");
            return willSwitch;
        }
    }
}
