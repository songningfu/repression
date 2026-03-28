// ----------------------------------------------------------------------------
// Script: DialogueChoiceButtonView
// 作用：封装单个对话选项按钮的显示与点击转发，避免 DialogueView 直接操作过多 UI 细节。
// 使用方法/调用示例：把它挂到选项按钮 Prefab 上，并在 DialogueView.choiceButtonPrefab 中绑定。
// ----------------------------------------------------------------------------
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeeAPsychologist.Dialogue
{
    /// <summary>
    /// 对话选项按钮视图。
    /// </summary>
    public sealed class DialogueChoiceButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        private int _choiceId;
        private Action<int> _onClicked;

        private void Reset()
        {
            button = GetComponent<Button>();
            label = GetComponentInChildren<TMP_Text>(true);
        }

        /// <summary>
        /// 绑定按钮显示与点击回调。
        /// </summary>
        public void Bind(DialogueChoiceViewModel model, Action<int> onClicked)
        {
            _choiceId = model.ChoiceId;
            _onClicked = onClicked;

            if (label != null)
            {
                label.text = model.Text;
                label.raycastTarget = false;
            }

            if (button != null)
            {
                button.interactable = model.Interactable;
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        private void HandleClick()
        {
            _onClicked?.Invoke(_choiceId);
        }
    }
}
