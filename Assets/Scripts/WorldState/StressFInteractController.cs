using System;
using UnityEngine;

namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// 通用交互控制器（MVP）：
    /// - 平时隐藏提示文字（如 "按F交互"）
    /// - 玩家进入触发范围后显示提示
    /// - 玩家在范围内按 F 时，调用 StressInteractable.Interact()
    ///
    /// 说明：
    /// - 这里的 Update 只做"按键触发事件检测"，不做任何随时间自动增减数值的玩法逻辑（符合项目硬性规则）。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StressFInteractController : MonoBehaviour
    {
        [Header("Prompt (Optional)")]
        [Tooltip("提示文字的根对象（直接 SetActive）。不填会自动在子物体中按名字查找：\"按F交互\" 或 \"put F\"。")]
        [SerializeField] private GameObject promptRoot;

        [Header("Interaction")]
        [Tooltip("按键（默认 F）。")]
        [SerializeField] private KeyCode interactKey = KeyCode.F;

        [Tooltip("玩家 Tag（为空则不按 Tag 过滤）。")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("是否在交互后立即隐藏提示（设为false可以持续交互）。")]
        [SerializeField] private bool hidePromptAfterInteract = false;

        private StressInteractable _interactable;
        private int _inRangeCount;

        private void Awake()
        {
            _interactable = GetComponent<StressInteractable>();
            if (_interactable == null)
            {
                Debug.LogWarning("[WorldState] StressFInteractController requires StressInteractable on the same GameObject.", this);
            }

            if (promptRoot == null)
            {
                promptRoot = FindPromptRootByName();
            }

            SetPromptVisible(false);
        }

        private void OnEnable()
        {
            // 防止在编辑器热重载/重复启用时提示残留
            if (_inRangeCount <= 0) SetPromptVisible(false);
        }

        private void Update()
        {
            if (_inRangeCount <= 0) return;
            if (_interactable == null) return;
            if (!Input.GetKeyDown(interactKey)) return;

            _interactable.Interact();
            if (hidePromptAfterInteract) SetPromptVisible(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;
            _inRangeCount++;
            SetPromptVisible(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;
            _inRangeCount = Mathf.Max(0, _inRangeCount - 1);
            if (_inRangeCount <= 0) SetPromptVisible(false);
        }

        private bool IsPlayer(Collider2D other)
        {
            if (string.IsNullOrWhiteSpace(playerTag)) return true;
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
            // 查找提示对象名 "按F交互"
            var byCnName = FindChildByName(transform, "按F交互");
            if (byCnName != null) return byCnName.gameObject;

            // 兜底：有人可能把对象名直接叫 put F
            var byEnName = FindChildByName(transform, "put F");
            if (byEnName != null) return byEnName.gameObject;

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
    }
}
