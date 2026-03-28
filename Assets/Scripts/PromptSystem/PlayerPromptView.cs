// ----------------------------------------------------------------------------
// Script: PlayerPromptView
// 作用：控制玩家头顶提示文本的显示、隐藏以及淡入淡出表现。
// 使用方法/调用示例：挂在玩家的提示文本节点上，绑定 TextMeshPro；运行时由 PlayerPromptReceiver 调用 Show/Hide。
// ----------------------------------------------------------------------------
using System;
using TMPro;
using UnityEngine;

namespace SeeAPsychologist.PromptSystem
{
    /// <summary>
    /// 玩家提示文本视图。
    /// 只负责表现层，不做任何提示业务判断。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerPromptView : MonoBehaviour
    {
        private enum PromptViewState
        {
            Hidden,
            FadingIn,
            Showing,
            FadingOut
        }

        [Header("Text Reference")]
        [Tooltip("要控制的 TextMeshPro 文本组件。建议直接拖拽玩家头顶提示文字对象。")]
        [SerializeField] private TextMeshPro promptText;

        [Header("Fade Settings")]
        [Tooltip("淡入时长（秒）。")]
        [SerializeField] private float fadeInDuration = 0.15f;

        [Tooltip("淡出时长（秒）。")]
        [SerializeField] private float fadeOutDuration = 0.2f;

        [Tooltip("当请求未显式指定时，使用的默认显示时长（秒）。")]
        [SerializeField] private float defaultShowDuration = 1.6f;

        [Tooltip("是否使用不受 Time.timeScale 影响的时间。")]
        [SerializeField] private bool useUnscaledTime = true;

        private PromptViewState _state = PromptViewState.Hidden;
        private Color _baseColor = Color.white;
        private float _stateTimer;
        private float _currentDisplayDuration;
        private float _fadeStartAlpha;

        public event Action PromptHidden;

        public bool IsPlaying => _state != PromptViewState.Hidden;

        private void Awake()
        {
            if (promptText == null)
            {
                promptText = GetComponent<TextMeshPro>();
            }

            if (promptText == null)
            {
                promptText = GetComponentInChildren<TextMeshPro>(true);
            }

            if (promptText == null)
            {
                Debug.LogWarning("[PromptSystem] PlayerPromptView requires a TextMeshPro reference.", this);
                return;
            }

            _baseColor = promptText.color;
            HideImmediate(invokeEvent: false);
        }

        private void OnValidate()
        {
            fadeInDuration = Mathf.Max(0f, fadeInDuration);
            fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
            defaultShowDuration = Mathf.Max(0f, defaultShowDuration);
        }

        private void Update()
        {
            if (promptText == null || _state == PromptViewState.Hidden)
            {
                return;
            }

            var deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            switch (_state)
            {
                case PromptViewState.FadingIn:
                    UpdateFadeIn(deltaTime);
                    break;

                case PromptViewState.Showing:
                    UpdateShowing(deltaTime);
                    break;

                case PromptViewState.FadingOut:
                    UpdateFadeOut(deltaTime);
                    break;
            }
        }

        /// <summary>
        /// 显示一条提示文本。
        /// </summary>
        /// <param name="text">要显示的文本。</param>
        /// <param name="showDuration">显示时长；小于等于 0 时使用默认时长。</param>
        public void Show(string text, float showDuration)
        {
            if (promptText == null)
            {
                Debug.LogWarning("[PromptSystem] PlayerPromptView.Show ignored because TextMeshPro is missing.", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                Hide();
                return;
            }

            promptText.text = text.Trim();
            promptText.enabled = true;

            _currentDisplayDuration = showDuration > 0f ? showDuration : defaultShowDuration;
            _stateTimer = 0f;
            _fadeStartAlpha = GetCurrentAlpha();

            if (fadeInDuration <= 0f)
            {
                SetAlpha(1f);
                _state = PromptViewState.Showing;
                return;
            }

            if (_fadeStartAlpha <= 0f)
            {
                SetAlpha(0f);
                _fadeStartAlpha = 0f;
            }

            _state = PromptViewState.FadingIn;
        }

        /// <summary>
        /// 请求按当前淡出配置隐藏文本。
        /// </summary>
        public void Hide()
        {
            if (promptText == null || _state == PromptViewState.Hidden)
            {
                return;
            }

            _stateTimer = 0f;
            _fadeStartAlpha = GetCurrentAlpha();

            if (fadeOutDuration <= 0f || _fadeStartAlpha <= 0f)
            {
                HideImmediate(invokeEvent: true);
                return;
            }

            _state = PromptViewState.FadingOut;
        }

        /// <summary>
        /// 立即隐藏文本并清空显示内容。
        /// </summary>
        public void HideImmediate()
        {
            HideImmediate(invokeEvent: true);
        }

        private void UpdateFadeIn(float deltaTime)
        {
            _stateTimer += deltaTime;
            var progress = fadeInDuration <= 0f ? 1f : Mathf.Clamp01(_stateTimer / fadeInDuration);
            SetAlpha(Mathf.Lerp(_fadeStartAlpha, 1f, progress));

            if (progress >= 1f)
            {
                _state = PromptViewState.Showing;
                _stateTimer = 0f;
            }
        }

        private void UpdateShowing(float deltaTime)
        {
            if (_currentDisplayDuration <= 0f)
            {
                return;
            }

            _stateTimer += deltaTime;
            if (_stateTimer >= _currentDisplayDuration)
            {
                Hide();
            }
        }

        private void UpdateFadeOut(float deltaTime)
        {
            _stateTimer += deltaTime;
            var progress = fadeOutDuration <= 0f ? 1f : Mathf.Clamp01(_stateTimer / fadeOutDuration);
            SetAlpha(Mathf.Lerp(_fadeStartAlpha, 0f, progress));

            if (progress >= 1f)
            {
                HideImmediate(invokeEvent: true);
            }
        }

        private void HideImmediate(bool invokeEvent)
        {
            if (promptText != null)
            {
                SetAlpha(0f);
                promptText.text = string.Empty;
                promptText.enabled = false;
            }

            _state = PromptViewState.Hidden;
            _stateTimer = 0f;
            _currentDisplayDuration = 0f;
            _fadeStartAlpha = 0f;

            if (invokeEvent)
            {
                PromptHidden?.Invoke();
            }
        }

        private float GetCurrentAlpha()
        {
            return promptText == null ? 0f : promptText.color.a;
        }

        private void SetAlpha(float alpha)
        {
            if (promptText == null)
            {
                return;
            }

            var color = _baseColor;
            color.a = Mathf.Clamp01(alpha);
            promptText.color = color;
        }
    }
}
