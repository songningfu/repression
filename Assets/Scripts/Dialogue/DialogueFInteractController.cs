// ----------------------------------------------------------------------------
// Script: DialogueFInteractController
// 作用：让对话物体在玩家靠近时显示“按F对话”，并在按下交互键后通过 DialogueTrigger 启动对话。
// 使用方法/调用示例：给对话物体添加 Collider2D(Is Trigger) + DialogueTrigger + DialogueFInteractController，
// 再在子物体上准备一个默认隐藏的“按F对话”提示对象。
// ----------------------------------------------------------------------------
using UnityEngine;

namespace SeeAPsychologist.Dialogue
{
    /// <summary>
    /// 对话 F 键交互控制器。
    /// 只负责距离检测、提示显隐与按键触发，不直接管理对话状态机。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class DialogueFInteractController : MonoBehaviour
    {
        [Header("Prompt (Optional)")]
        [Tooltip("提示文字根对象（直接 SetActive）。不填会自动在子物体中按名字查找：\"按F对话\"、\"按F交互\" 或 \"put F\"。")]
        [SerializeField] private GameObject promptRoot;

        [Header("Dialogue")]
        [Tooltip("要触发的 DialogueTrigger；不填会自动从同物体获取。")]
        [SerializeField] private DialogueTrigger dialogueTrigger;

        [Tooltip("对话运行器；仅用于在对话进行中隐藏提示。不填时可自动查找场景中的 DialogueRunner。")]
        [SerializeField] private DialogueRunner runner;

        [Tooltip("当 runner 未绑定时，是否自动在场景里查找 DialogueRunner。")]
        [SerializeField] private bool autoFindRunner = true;

        [Header("Interaction")]
        [Tooltip("交互按键（默认 F）。")]
        [SerializeField] private KeyCode interactKey = KeyCode.F;

        [Tooltip("玩家 Tag（为空则不按 Tag 过滤）。")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("成功触发对话后是否立即隐藏提示。")]
        [SerializeField] private bool hidePromptAfterInteract = true;

        private Collider2D _triggerCollider;
        private int _inRangeCount;
        private bool _subscribed;

        private void Awake()
        {
            _triggerCollider = GetComponent<Collider2D>();
            dialogueTrigger ??= GetComponent<DialogueTrigger>();
            ResolveRunner();

            if (dialogueTrigger == null)
            {
                Debug.LogWarning("[Dialogue] DialogueFInteractController requires DialogueTrigger on the same GameObject or in the serialized field.", this);
            }

            if (promptRoot == null)
            {
                promptRoot = FindPromptRootByName();
            }

            ValidateCollider();
            SetPromptVisible(false);
        }

        private void OnEnable()
        {
            SubscribeRunner();
            RefreshPromptVisibility();
        }

        private void OnDisable()
        {
            UnsubscribeRunner();
            if (_inRangeCount <= 0)
            {
                SetPromptVisible(false);
            }
        }

        private void Update()
        {
            if (_inRangeCount <= 0)
            {
                return;
            }

            if (dialogueTrigger == null)
            {
                return;
            }

            ResolveRunner();
            if (runner != null && runner.IsActive)
            {
                SetPromptVisible(false);
                return;
            }

            if (!dialogueTrigger.CanTrigger)
            {
                SetPromptVisible(false);
                return;
            }

            if (!Input.GetKeyDown(interactKey))
            {
                return;
            }

            var started = dialogueTrigger.TryStartDialogue();
            if (!started)
            {
                RefreshPromptVisibility();
                return;
            }

            if (hidePromptAfterInteract)
            {
                SetPromptVisible(false);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            _inRangeCount++;
            RefreshPromptVisibility();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            _inRangeCount = Mathf.Max(0, _inRangeCount - 1);
            RefreshPromptVisibility();
        }

        private void HandleDialogueVisibilityChanged(bool _)
        {
            RefreshPromptVisibility();
        }

        private void ResolveRunner()
        {
            if (runner != null || !autoFindRunner)
            {
                return;
            }

            runner = FindObjectOfType<DialogueRunner>(true);
        }

        private void SubscribeRunner()
        {
            if (_subscribed)
            {
                return;
            }

            ResolveRunner();
            if (runner == null)
            {
                return;
            }

            runner.OnDialogueVisibilityChanged += HandleDialogueVisibilityChanged;
            _subscribed = true;
        }

        private void UnsubscribeRunner()
        {
            if (!_subscribed || runner == null)
            {
                return;
            }

            runner.OnDialogueVisibilityChanged -= HandleDialogueVisibilityChanged;
            _subscribed = false;
        }

        private void RefreshPromptVisibility()
        {
            var shouldShow = _inRangeCount > 0 &&
                             dialogueTrigger != null &&
                             dialogueTrigger.CanTrigger &&
                             (runner == null || !runner.IsActive);

            SetPromptVisible(shouldShow);
        }

        private bool IsPlayer(Collider2D other)
        {
            if (string.IsNullOrWhiteSpace(playerTag))
            {
                return true;
            }

            return other.CompareTag(playerTag);
        }

        private void SetPromptVisible(bool visible)
        {
            if (promptRoot != null && promptRoot.activeSelf != visible)
            {
                promptRoot.SetActive(visible);
            }
        }

        private GameObject FindPromptRootByName()
        {
            var byDialogueName = FindChildByName(transform, "按F对话");
            if (byDialogueName != null)
            {
                return byDialogueName.gameObject;
            }

            var byInteractName = FindChildByName(transform, "按F交互");
            if (byInteractName != null)
            {
                return byInteractName.gameObject;
            }

            var byEnName = FindChildByName(transform, "put F");
            if (byEnName != null)
            {
                return byEnName.gameObject;
            }

            return null;
        }

        private static Transform FindChildByName(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }

                var nested = FindChildByName(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private void ValidateCollider()
        {
            if (_triggerCollider != null && !_triggerCollider.isTrigger)
            {
                Debug.LogWarning("[Dialogue] DialogueFInteractController requires Collider2D.IsTrigger = true.", this);
            }
        }
    }
}
