// ----------------------------------------------------------------------------
// Script: PromptSource
// 作用：挂在可提示物体上，在玩家进入触发范围时发起一次提示请求。
// 使用方法/调用示例：给物体添加 Collider2D(Is Trigger) + PromptSource，配置提示词、概率、时长与优先级。
// ----------------------------------------------------------------------------
using UnityEngine;

namespace SeeAPsychologist.PromptSystem
{
    /// <summary>
    /// 场景提示源。
    /// 负责保存提示配置，并在玩家进入触发区时向玩家提示接收器发起一次请求。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PromptSource : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("提示源唯一标识；为空时会回退到当前物体名。")]
        [SerializeField] private string sourceId = string.Empty;

        [Header("Prompt Content")]
        [Tooltip("该物体可用的提示词列表。会随机抽取一条非空文本。")]
        [SerializeField] private string[] promptTexts = { "这里似乎有什么。" };

        [Header("Trigger Rules")]
        [Tooltip("触发概率，范围 0~1。")]
        [Range(0f, 1f)]
        [SerializeField] private float triggerChance = 0.35f;

        [Tooltip("文本显示时长（秒）。")]
        [SerializeField] private float showDuration = 1.6f;

        [Tooltip("同一提示源成功显示后，再次触发前的冷却时间（秒）。")]
        [SerializeField] private float cooldownSeconds = 0.75f;

        [Tooltip("多个提示重叠时的优先级；数值越高越容易覆盖当前提示。")]
        [SerializeField] private int priority = 0;

        [Tooltip("同一局运行内是否允许重复成功显示。")]
        [SerializeField] private bool allowRepeat = true;

        [Tooltip("一次进入触发区只判定一次；离开后重新进入才会再次判定。")]
        [SerializeField] private bool triggerOncePerEnter = true;

        [Tooltip("玩家 Tag（为空则仅依赖 PlayerPromptReceiver 组件识别玩家）。")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("是否输出阻塞原因日志，便于调试概率/冷却问题。")]
        [SerializeField] private bool logBlockedReason = false;

        private Collider2D _triggerCollider;
        private int _playerOverlapCount;
        private bool _hasAttemptedThisEnter;
        private bool _hasTriggeredSuccessfully;
        private float _nextAvailableTime;

        public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? gameObject.name : sourceId;
        public int Priority => priority;
        public float TriggerChance => triggerChance;
        public float ShowDuration => showDuration;

        private void Awake()
        {
            _triggerCollider = GetComponent<Collider2D>();
            ValidateCollider();
        }

        private void OnValidate()
        {
            triggerChance = Mathf.Clamp01(triggerChance);
            showDuration = Mathf.Max(0f, showDuration);
            cooldownSeconds = Mathf.Max(0f, cooldownSeconds);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!TryGetPlayerReceiver(other, out var receiver))
            {
                return;
            }

            _playerOverlapCount++;
            if (_playerOverlapCount > 1)
            {
                return;
            }

            if (triggerOncePerEnter && _hasAttemptedThisEnter)
            {
                return;
            }

            _hasAttemptedThisEnter = true;
            receiver.TryShowPrompt(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!TryGetPlayerReceiver(other, out _))
            {
                return;
            }

            _playerOverlapCount = Mathf.Max(0, _playerOverlapCount - 1);
            if (_playerOverlapCount <= 0)
            {
                _hasAttemptedThisEnter = false;
            }
        }

        /// <summary>
        /// 由玩家提示接收器调用，尝试生成本次提示请求。
        /// </summary>
        /// <param name="request">输出的请求快照。</param>
        /// <returns>若当前允许发起提示，则返回 true。</returns>
        public bool TryBuildRequest(out PromptRequest request)
        {
            request = default;

            if (!CanIssuePrompt(out var reason))
            {
                if (logBlockedReason && !string.IsNullOrWhiteSpace(reason))
                {
                    Debug.Log($"[PromptSystem] Source '{SourceId}' blocked: {reason}", this);
                }

                return false;
            }

            var selectedText = PickRandomPromptText();
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                Debug.LogWarning($"[PromptSystem] Source '{SourceId}' has no valid prompt text.", this);
                return false;
            }

            request = new PromptRequest(
                source: this,
                text: selectedText,
                triggerChance: triggerChance,
                showDuration: showDuration,
                priority: priority);

            return true;
        }

        /// <summary>
        /// 在本提示成功显示后调用，用于记录冷却与重复触发状态。
        /// </summary>
        public void MarkPromptAccepted()
        {
            _hasTriggeredSuccessfully = true;

            if (cooldownSeconds > 0f)
            {
                _nextAvailableTime = Time.unscaledTime + cooldownSeconds;
            }
        }

        private bool CanIssuePrompt(out string reason)
        {
            if (!isActiveAndEnabled)
            {
                reason = "source disabled";
                return false;
            }

            if (!allowRepeat && _hasTriggeredSuccessfully)
            {
                reason = "repeat disabled";
                return false;
            }

            if (cooldownSeconds > 0f && Time.unscaledTime < _nextAvailableTime)
            {
                reason = "cooldown active";
                return false;
            }

            if (!HasAnyValidPromptText())
            {
                reason = "empty prompt texts";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private bool HasAnyValidPromptText()
        {
            if (promptTexts == null || promptTexts.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < promptTexts.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(promptTexts[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private string PickRandomPromptText()
        {
            if (promptTexts == null || promptTexts.Length == 0)
            {
                return string.Empty;
            }

            var validCount = 0;
            for (var i = 0; i < promptTexts.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(promptTexts[i]))
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                return string.Empty;
            }

            var pickIndex = Random.Range(0, validCount);
            var currentValidIndex = 0;
            for (var i = 0; i < promptTexts.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(promptTexts[i]))
                {
                    continue;
                }

                if (currentValidIndex == pickIndex)
                {
                    return promptTexts[i].Trim();
                }

                currentValidIndex++;
            }

            return string.Empty;
        }

        private bool TryGetPlayerReceiver(Collider2D other, out PlayerPromptReceiver receiver)
        {
            receiver = other.GetComponentInParent<PlayerPromptReceiver>();
            if (receiver == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(playerTag))
            {
                return true;
            }

            if (receiver.CompareTag(playerTag))
            {
                return true;
            }

            if (other.CompareTag(playerTag))
            {
                return true;
            }

            var rigidbodyObject = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : null;
            return rigidbodyObject != null && rigidbodyObject.CompareTag(playerTag);
        }

        private void ValidateCollider()
        {
            if (_triggerCollider == null)
            {
                return;
            }

            if (!_triggerCollider.isTrigger)
            {
                Debug.LogWarning($"[PromptSystem] PromptSource on '{SourceId}' requires Collider2D.IsTrigger = true.", this);
            }
        }
    }
}
