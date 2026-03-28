// ----------------------------------------------------------------------------
// Script: DialogueTrigger
// 作用：提供一个可挂在交互物、按钮或剧情节点上的对话启动入口。
// 使用方法/调用示例：在 Inspector 中绑定 Runner 与 StartNodeId，然后从按钮事件或其他脚本调用 TryStartDialogue()。
// ----------------------------------------------------------------------------
using UnityEngine;
using UnityEngine.Events;

namespace SeeAPsychologist.Dialogue
{
    /// <summary>
    /// 通用对话触发器。
    /// </summary>
    public sealed class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] private DialogueRunner runner;
        [SerializeField] private string startNodeId;
        [SerializeField] private bool triggerOnStart;
        [SerializeField] private bool oneShot = true;
        [SerializeField] private UnityEvent onDialogueStarted;

        private bool _consumed;

        /// <summary>
        /// 当前是否仍允许继续触发这段对话。
        /// </summary>
        public bool CanTrigger => !_consumed || !oneShot;

        private void Start()
        {
            if (triggerOnStart)
            {
                TryStartDialogue();
            }
        }

        /// <summary>
        /// 尝试启动配置中的对话入口节点。
        /// </summary>
        public bool TryStartDialogue()
        {
            if (_consumed && oneShot)
            {
                return false;
            }

            if (runner == null)
            {
                runner = FindObjectOfType<DialogueRunner>(true);
            }

            if (runner == null)
            {
                Debug.LogWarning("[Dialogue] DialogueRunner is missing on DialogueTrigger.");
                return false;
            }

            var started = runner.StartDialogue(startNodeId);
            if (!started)
            {
                return false;
            }

            _consumed = true;
            onDialogueStarted?.Invoke();
            return true;
        }
    }
}
