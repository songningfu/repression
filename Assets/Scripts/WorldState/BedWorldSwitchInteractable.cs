using UnityEngine;

namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// 床交互桥接（挂到 bed 上即可）。
    /// - 由外部交互系统在“按F”时调用 Interact()
    /// - 不做输入轮询，不使用 Update/Coroutine
    /// </summary>
    public sealed class BedWorldSwitchInteractable : MonoBehaviour
    {
        [Tooltip("默认床交互类型（按解离度阈值决定进入现实/意识）。")]
        [SerializeField] private InteractionType interactionType = InteractionType.Bed;

        [Tooltip("可选上下文（用于日志追踪）。")]
        [SerializeField] private string context = "bed";

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

            var switched = mgr.TrySwitchWorld(interactionType, out var target, context);
            Debug.Log($"[A3][WorldState] Bed interact => switched={switched}, target={target}");
        }
    }
}

