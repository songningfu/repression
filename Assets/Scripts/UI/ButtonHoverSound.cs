// ----------------------------------------------------------------------------
// Script: ButtonHoverSound
// 作用：给一组 Button 批量加上鼠标悬停音效（和可选的点击音效）。
//
// 设计：
//   - 挂在按钮容器的父对象上（如 ButtonsGroup / Canvas）
//   - Start 时自动找所有子级的 UI Button
//   - 给每个按钮挂 EventTrigger 监听 PointerEnter（和可选 PointerClick）
//   - 触发时播放配置好的 AudioCue
//
// 使用方法：
//   1. 选中 ButtonsGroup（按钮的父对象）
//   2. Add Component → ButtonHoverSound
//   3. Hover Cue 拖入 Cue_UI_Hover
//   4. （可选）Click Cue 拖入 Cue_UI_Click，并勾选 Play Click Sound
// ----------------------------------------------------------------------------
using SeeAPsychologist.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SeeAPsychologist.UI
{
    [DisallowMultipleComponent]
    public sealed class ButtonHoverSound : MonoBehaviour
    {
        [Header("音效")]
        [Tooltip("鼠标悬停按钮时播放的 AudioCue。")]
        [SerializeField] private AudioCue hoverCue;

        [Tooltip("点击按钮时播放的 AudioCue（可选）。注意：如果按钮已经在 OnClick 里调了 click 音，这里别再勾，否则会响两次。")]
        [SerializeField] private AudioCue clickCue;

        [Tooltip("勾上 = 启用点击音效（默认不启用，避免和 MainMenuController.clickCue 重复）。")]
        [SerializeField] private bool playClickSound = false;

        [Header("查找范围")]
        [Tooltip("勾上 = 包含未激活的按钮（一般不需要）。")]
        [SerializeField] private bool includeInactive = false;

        [Tooltip("勾上 = 只处理直接子对象上的按钮，不递归到孙级。")]
        [SerializeField] private bool directChildrenOnly = false;

        private void Start()
        {
            Button[] buttons;
            if (directChildrenOnly)
            {
                // 只找直接子级
                var list = new System.Collections.Generic.List<Button>();
                for (int i = 0; i < transform.childCount; i++)
                {
                    var btn = transform.GetChild(i).GetComponent<Button>();
                    if (btn != null) list.Add(btn);
                }
                buttons = list.ToArray();
            }
            else
            {
                buttons = GetComponentsInChildren<Button>(includeInactive);
            }

            int attached = 0;
            foreach (var btn in buttons)
            {
                if (btn == null) continue;
                AttachListeners(btn);
                attached++;
            }

            Debug.Log($"[ButtonHoverSound] 为 {attached} 个按钮挂上了悬停音效。");
        }

        private void AttachListeners(Button btn)
        {
            var trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

            // PointerEnter（悬停进入）
            if (hoverCue != null)
            {
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                entry.callback.AddListener(_ => PlayCue(hoverCue));
                trigger.triggers.Add(entry);
            }

            // PointerClick（点击，可选）
            if (playClickSound && clickCue != null)
            {
                var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                click.callback.AddListener(_ => PlayCue(clickCue));
                trigger.triggers.Add(click);
            }
        }

        private static void PlayCue(AudioCue cue)
        {
            if (cue == null || AudioManager.Instance == null) return;
            AudioManager.Instance.PlaySfx(cue);
        }
    }
}
