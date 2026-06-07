// ----------------------------------------------------------------------------
// Script: MentalStatsTrigger
// 作用：让 UnityEvent / Animation Event 能方便地修改心理稳态数值。
//
// 与 StressInteractable 的区别：
//   - StressInteractable：固定逻辑（现实+10/意识-10），适合"咨询/休息"这类语义明确的交互
//   - MentalStatsTrigger：暴露任意方法（AddStress / AddDissociation / SetXxx），让 Inspector 自由配置
//
// 使用方法：
//   1. 挂在交互物体上
//   2. InteractableObject.OnInteract 里加回调：本组件 → AddStress(int 数值)
//   3. 不需要任何字段配置，全部参数从 UnityEvent 传入
// ----------------------------------------------------------------------------
using UnityEngine;

namespace SeeAPsychologist.MentalStats
{
    public sealed class MentalStatsTrigger : MonoBehaviour
    {
        [Header("默认值（可选）")]
        [Tooltip("默认的应激度变化量。AddStressDefault() 调用时使用。")]
        [SerializeField] private int defaultStressDelta = 10;

        [Tooltip("调试日志。")]
        [SerializeField] private bool debugLog = false;

        // -- 通用 API（UnityEvent 友好，接受 int 参数） ----------------------

        /// <summary>增加应激度（正负皆可）。这是唯一允许直接修改的数值。</summary>
        public void AddStress(int delta)
        {
            var mgr = MentalStatsManager.Instance;
            if (mgr == null) { Warn(); return; }
            mgr.ModifyStress(delta, MentalStatChangeSource.Interaction, name);
            if (debugLog) Debug.Log($"[MentalStatsTrigger] {name} AddStress({delta}) → 当前 Stress={mgr.CurrentStress}");
        }

        // 注意：Dissociation 不能直接修改，它由 Stress=0/100 自动联动。这是项目设计原则。

        // -- 默认值版本（UnityEvent 无参数也能直接选） ----------------------

        /// <summary>用 defaultStressDelta 增加应激度。</summary>
        public void AddStressDefault()
        {
            AddStress(defaultStressDelta);
        }

        /// <summary>减少应激度 = 反向加。</summary>
        public void SubtractStressDefault()
        {
            AddStress(-defaultStressDelta);
        }

        // -- 按世界自动反向（与 StressInteractable 等价语义） ---------------

        /// <summary>
        /// 智能调整：现实世界 +delta，意识世界 -delta。
        /// 适合"令人焦虑的物品"（冰箱、镜子等）— 在现实里看了焦虑度上升，但在意识里没影响（或反过来）。
        /// </summary>
        public void AdjustByWorld(int delta)
        {
            var worldMgr = SeeAPsychologist.WorldState.WorldStateManager.Instance;
            if (worldMgr == null || MentalStatsManager.Instance == null) { Warn(); return; }

            int signedDelta = worldMgr.CurrentWorld == SeeAPsychologist.WorldState.WorldType.Reality
                ? delta
                : -delta;
            AddStress(signedDelta);
        }

        private static void Warn()
        {
            Debug.LogWarning("[MentalStatsTrigger] MentalStatsManager not available.");
        }
    }
}
