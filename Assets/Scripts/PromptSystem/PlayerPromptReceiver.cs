// ----------------------------------------------------------------------------
// Script: PlayerPromptReceiver
// 作用：挂在玩家身上，统一处理来自不同提示源的请求、概率判定与优先级竞争。
// 使用方法/调用示例：给玩家添加 PlayerPromptReceiver，并在 Inspector 绑定一个 PlayerPromptView。
// ----------------------------------------------------------------------------
using UnityEngine;

namespace SeeAPsychologist.PromptSystem
{
    /// <summary>
    /// 玩家提示接收器。
    /// 负责进行最终触发概率判定，并把被接受的提示交给显示层播放。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerPromptReceiver : MonoBehaviour
    {
        [Header("View")]
        [Tooltip("负责在玩家头顶显示提示的视图组件。建议直接在 Inspector 拖拽绑定。")]
        [SerializeField] private PlayerPromptView promptView;

        [Header("Priority Rules")]
        [Tooltip("仅允许更高优先级的新提示覆盖当前提示。")]
        [SerializeField] private bool requireStrictHigherPriorityToOverride = true;

        [Tooltip("是否输出接收/拒绝日志，便于调试重叠触发。")]
        [SerializeField] private bool logDecision = false;

        private PromptSource _currentSource;
        private int _currentPriority;

        private void Awake()
        {
            if (promptView == null)
            {
                promptView = GetComponentInChildren<PlayerPromptView>(true);
            }

            _currentPriority = int.MinValue;
        }

        private void OnEnable()
        {
            if (promptView != null)
            {
                promptView.PromptHidden += HandlePromptHidden;
            }
        }

        private void OnDisable()
        {
            if (promptView != null)
            {
                promptView.PromptHidden -= HandlePromptHidden;
            }
        }

        /// <summary>
        /// 尝试显示来自某个提示源的提示。
        /// </summary>
        /// <param name="source">提示源。</param>
        /// <returns>若本次请求最终被接受并开始显示，则返回 true。</returns>
        public bool TryShowPrompt(PromptSource source)
        {
            if (source == null)
            {
                return false;
            }

            if (promptView == null)
            {
                Debug.LogWarning("[PromptSystem] PlayerPromptReceiver missing PlayerPromptView reference.", this);
                return false;
            }

            if (!source.TryBuildRequest(out var request))
            {
                return false;
            }

            if (!CanAcceptRequest(request))
            {
                if (logDecision)
                {
                    Debug.Log($"[PromptSystem] Receiver rejected '{source.SourceId}' because current prompt has higher or equal priority.", this);
                }

                return false;
            }

            if (!PassChanceCheck(request.TriggerChance))
            {
                if (logDecision)
                {
                    Debug.Log($"[PromptSystem] Receiver missed chance check for '{source.SourceId}'.", this);
                }

                return false;
            }

            _currentSource = source;
            _currentPriority = request.Priority;

            source.MarkPromptAccepted();
            promptView.Show(request.Text, request.ShowDuration);

            if (logDecision)
            {
                Debug.Log($"[PromptSystem] Receiver accepted '{source.SourceId}' -> \"{request.Text}\"", this);
            }

            return true;
        }

        /// <summary>
        /// 主动清理当前提示。
        /// </summary>
        public void HideCurrentPrompt()
        {
            if (promptView == null)
            {
                return;
            }

            promptView.Hide();
        }

        private bool CanAcceptRequest(PromptRequest request)
        {
            if (promptView == null || !promptView.IsPlaying)
            {
                return true;
            }

            if (_currentSource == null)
            {
                return true;
            }

            if (!requireStrictHigherPriorityToOverride)
            {
                return request.Priority >= _currentPriority;
            }

            return request.Priority > _currentPriority;
        }

        private static bool PassChanceCheck(float triggerChance)
        {
            if (triggerChance <= 0f)
            {
                return false;
            }

            if (triggerChance >= 1f)
            {
                return true;
            }

            return Random.value <= triggerChance;
        }

        private void HandlePromptHidden()
        {
            _currentSource = null;
            _currentPriority = int.MinValue;
        }
    }
}
