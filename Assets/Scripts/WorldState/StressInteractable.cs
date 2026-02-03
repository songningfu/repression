using UnityEngine;
using SeeAPsychologist.MentalStats;

namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// Stress 交互组件（挂到交互对象上）。
    /// 完全即时响应，无任何延迟
    /// </summary>
    public sealed class StressInteractable : MonoBehaviour
    {
        [Header("交互设置")]
        [Tooltip("交互类型（咨询师、睡觉、吃饭、娱乐等）。")]
        [SerializeField] private InteractionType interactionType = InteractionType.Therapy;

        [Tooltip("意识世界中减少的 Stress 值（正数）。")]
        [SerializeField] private int stressDecreaseInConsciousness = 10;

        [Tooltip("现实世界中增加的 Stress 值（正数）。")]
        [SerializeField] private int stressIncreaseInReality = 10;

        [Tooltip("可选上下文（用于日志追踪）。")]
        [SerializeField] private string context = "";

        private void Awake()
        {
            if (string.IsNullOrEmpty(context))
            {
                context = interactionType.ToString();
            }
        }

        /// <summary>
        /// 交互入口：由输入/交互系统调用。
        /// 完全即时，无冷却，无延迟
        /// </summary>
        public void Interact()
        {
            var worldMgr = WorldStateManager.Instance;
            if (worldMgr == null)
            {
                Debug.LogWarning("[WorldState] WorldStateManager missing; interact ignored.");
                return;
            }

            var mentalMgr = MentalStatsManager.Instance;
            if (mentalMgr == null)
            {
                Debug.LogWarning("[WorldState] MentalStatsManager missing; interact ignored.");
                return;
            }

            // 获取当前世界类型
            var currentWorld = worldMgr.CurrentWorld;
            
            // 根据世界类型调整 Stress - 立即生效
            if (currentWorld == WorldType.Consciousness)
            {
                // 意识世界：减少 Stress
                mentalMgr.ModifyStress(-stressDecreaseInConsciousness, MentalStatChangeSource.Interaction, context);
            }
            else if (currentWorld == WorldType.Reality)
            {
                // 现实世界：增加 Stress
                mentalMgr.ModifyStress(stressIncreaseInReality, MentalStatChangeSource.Interaction, context);
            }
        }

        /// <summary>
        /// 设置 Stress 变化值
        /// </summary>
        public void SetStressValues(int decreaseInConsciousness, int increaseInReality)
        {
            stressDecreaseInConsciousness = decreaseInConsciousness;
            stressIncreaseInReality = increaseInReality;
        }
    }
}
