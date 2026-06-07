// ----------------------------------------------------------------------------
// Script: InteractableObject
// 作用：通用"按 F 交互"组件。挂在任何可交互物体上（冰箱、花瓶、收音机、海报...）。
//
// 设计：
//   - 玩家进入 Trigger 范围 → 显示"按 F"提示
//   - 玩家按 F → 触发 UnityEvent onInteract（在 Inspector 里串联任意动作）
//   - 支持一次性（oneShot）、冷却（interactCooldown）
//   - 不依赖任何特定系统（对话、音效、数值都可以串）
//
// 使用方法：
//   1. 物体上加 Collider2D（Is Trigger ✅）+ 本组件
//   2. 在子物体里放一个"按F"文本（默认隐藏），拖到 promptRoot
//   3. Inspector 的 On Interact 列表里加任意回调：
//      - AudioCueTrigger.Play()        ← 播音效
//      - DialogueTrigger.TryStartDialogue() ← 触发对话
//      - MentalStatsManager.AddStress(10)   ← 改应激度
//      - GameObject.SetActive            ← 隐藏物体（捡起物品）
//      - 任意自定义脚本的方法
// ----------------------------------------------------------------------------
using UnityEngine;
using UnityEngine.Events;

namespace SeeAPsychologist.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class InteractableObject : MonoBehaviour
    {
        [Header("提示")]
        [Tooltip("玩家进入范围时显示的提示对象（子物体）。不填则在子物体中按名字 \"按F\" / \"按F交互\" / \"put F\" 自动查找。")]
        [SerializeField] private GameObject promptRoot;

        [Header("交互按键")]
        [SerializeField] private KeyCode interactKey = KeyCode.F;

        [Tooltip("副交互键（可选）。设置后玩家在范围内按这个键会触发 On Secondary Interact 事件。例如：F 看 / G 睡。设为 None 关闭。")]
        [SerializeField] private KeyCode secondaryInteractKey = KeyCode.None;

        [Tooltip("玩家 Tag。")]
        [SerializeField] private string playerTag = "Player";

        [Header("行为控制")]
        [Tooltip("勾选 = 只能触发一次（之后不再显示提示也不响应按键）。")]
        [SerializeField] private bool oneShot = false;

        [Tooltip("两次交互的最小间隔（秒）。防止按住 F 狂触发。")]
        [SerializeField, Min(0f)] private float interactCooldown = 0.5f;

        [Tooltip("交互后是否立即隐藏提示（直到玩家离开再进入）。")]
        [SerializeField] private bool hidePromptAfterInteract = true;

        [Header("事件")]
        [Tooltip("玩家按主交互键（默认 F）时触发。")]
        [SerializeField] private UnityEvent onInteract;

        [Tooltip("玩家按副交互键（如 G）时触发。仅当 Secondary Interact Key 不是 None 时生效。")]
        [SerializeField] private UnityEvent onSecondaryInteract;

        [Tooltip("玩家进入范围时触发（可选）。")]
        [SerializeField] private UnityEvent onEnterRange;

        [Tooltip("玩家离开范围时触发（可选）。")]
        [SerializeField] private UnityEvent onExitRange;

        private int _inRangeCount;
        private bool _consumed;
        private float _lastInteractTime = -999f;
        private bool _promptTemporarilyHidden;
        private PromptFader _promptFader; // 若存在则用平滑淡入淡出，否则用 SetActive 硬切

        public bool CanInteract => (!oneShot || !_consumed) &&
                                   Time.time - _lastInteractTime >= interactCooldown;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning($"[Interaction] {name}: Collider2D 应设为 Is Trigger。", this);
            }

            if (promptRoot == null) promptRoot = FindPromptByName();
            if (promptRoot != null) _promptFader = promptRoot.GetComponent<PromptFader>();
            SetPromptVisible(false);
        }

        private void Update()
        {
            if (_inRangeCount <= 0) return;
            if (!CanInteract) return;

            // 主交互键
            if (Input.GetKeyDown(interactKey))
            {
                FireInteract(onInteract, "OnInteract");
                return;
            }

            // 副交互键（可选）
            if (secondaryInteractKey != KeyCode.None && Input.GetKeyDown(secondaryInteractKey))
            {
                FireInteract(onSecondaryInteract, "OnSecondaryInteract");
            }
        }

        private void FireInteract(UnityEvent evt, string label)
        {
            _lastInteractTime = Time.time;
            _consumed = true;

            try { evt?.Invoke(); }
            catch (System.Exception e) { Debug.LogError($"[Interaction] {name}: {label} 回调抛异常: {e}", this); }

            if (hidePromptAfterInteract)
            {
                _promptTemporarilyHidden = true;
                SetPromptVisible(false);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;
            _inRangeCount++;
            _promptTemporarilyHidden = false;
            RefreshPrompt();
            onEnterRange?.Invoke();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;
            _inRangeCount = Mathf.Max(0, _inRangeCount - 1);
            if (_inRangeCount <= 0)
            {
                _promptTemporarilyHidden = false;
                SetPromptVisible(false);
                onExitRange?.Invoke();
            }
        }

        // -- 内部 -----------------------------------------------------------

        private void RefreshPrompt()
        {
            bool show = _inRangeCount > 0 && CanInteract && !_promptTemporarilyHidden;
            SetPromptVisible(show);
        }

        private bool IsPlayer(Collider2D other)
            => string.IsNullOrWhiteSpace(playerTag) || other.CompareTag(playerTag);

        private void SetPromptVisible(bool v)
        {
            if (promptRoot == null) return;

            // 优先用 PromptFader 平滑动画；没有则硬切 SetActive
            if (_promptFader != null)
            {
                // PromptFader 自己控制可见性（始终保持 active），调 Show/Hide
                if (!promptRoot.activeSelf) promptRoot.SetActive(true);
                if (v) _promptFader.Show();
                else _promptFader.Hide();
            }
            else
            {
                if (promptRoot.activeSelf != v) promptRoot.SetActive(v);
            }
        }

        private GameObject FindPromptByName()
        {
            var names = new[] { "按F", "按F交互", "按F查看", "按F对话", "put F", "Prompt" };
            foreach (var n in names)
            {
                var t = FindChildByName(transform, n);
                if (t != null) return t.gameObject;
            }
            return null;
        }

        private static Transform FindChildByName(Transform root, string name)
        {
            if (root == null) return null;
            for (var i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i);
                if (c.name == name) return c;
                var deep = FindChildByName(c, name);
                if (deep != null) return deep;
            }
            return null;
        }

        // -- 公共 API（可由代码主动调用） ------------------------------------

        /// <summary>外部代码强制触发一次交互（如剧情自动触发）。</summary>
        public void ForceInteract()
        {
            onInteract?.Invoke();
            _consumed = true;
        }

        /// <summary>重置 oneShot 标记（让物体可以再交互）。</summary>
        public void ResetConsumed()
        {
            _consumed = false;
            _promptTemporarilyHidden = false;
            RefreshPrompt();
        }
    }
}
