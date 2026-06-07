// ----------------------------------------------------------------------------
// Script: PromptFader
// 作用：让"按F"提示文字/图片有淡入淡出 + 轻微上浮的动画效果。
//
// 工作原理：
//   - 挂在提示物体上（TextMeshPro 3D Text、SpriteRenderer、或 UI Image 均可）
//   - 自动找到子级的所有 TMP_Text / SpriteRenderer / Image / CanvasGroup
//   - Show() / Hide() 平滑改变 alpha + position
//   - 不依赖 SetActive，本物体始终 active，用 alpha 控制可见性
//
// 使用方法：
//   1. 挂在"按F"提示物体上（替代 SetActive 的硬切换）
//   2. InteractableObject 会自动检测此组件并调用 Show/Hide
//   3. Inspector 可调时长、上浮距离、是否开始就显示
// ----------------------------------------------------------------------------
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeeAPsychologist.Interaction
{
    [DisallowMultipleComponent]
    public sealed class PromptFader : MonoBehaviour
    {
        [Header("时长")]
        [Tooltip("淡入用时（秒）。")]
        [SerializeField, Min(0f)] private float fadeInDuration = 0.18f;

        [Tooltip("淡出用时（秒）。")]
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.22f;

        [Header("位移（出现时上浮）")]
        [Tooltip("出现时从基准位置往下偏移多少，再上浮到位。0 = 无位移动画。")]
        [SerializeField] private float floatOffset = 0.1f;

        [Header("起始状态")]
        [Tooltip("勾选 = 启动时默认显示；不勾 = 启动时默认隐藏。")]
        [SerializeField] private bool startVisible = false;

        [Tooltip("勾选 = 用 Time.unscaledDeltaTime（不受暂停影响）。")]
        [SerializeField] private bool useUnscaledTime = true;

        // -- 渲染对象引用 ---------------------------------------------------------
        private readonly List<TMP_Text> _tmpTexts = new();
        private readonly List<SpriteRenderer> _spriteRenderers = new();
        private readonly List<Image> _images = new();
        private readonly List<CanvasGroup> _canvasGroups = new();

        // -- 状态 ----------------------------------------------------------------
        private Vector3 _baseLocalPosition;
        private float _currentAlpha;
        private float _targetAlpha;
        private bool _wantVisible;

        public bool IsVisible => _wantVisible;

        private void Awake()
        {
            CacheTargets();
            _baseLocalPosition = transform.localPosition;

            if (startVisible)
            {
                _wantVisible = true;
                _currentAlpha = _targetAlpha = 1f;
            }
            else
            {
                _wantVisible = false;
                _currentAlpha = _targetAlpha = 0f;
            }

            ApplyAlpha(_currentAlpha);
            ApplyOffset(_currentAlpha);
        }

        private void CacheTargets()
        {
            _tmpTexts.Clear(); _spriteRenderers.Clear(); _images.Clear(); _canvasGroups.Clear();
            GetComponentsInChildren(true, _tmpTexts);
            GetComponentsInChildren(true, _spriteRenderers);
            GetComponentsInChildren(true, _images);
            GetComponentsInChildren(true, _canvasGroups);
        }

        private void Update()
        {
            if (Mathf.Approximately(_currentAlpha, _targetAlpha)) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float duration = _wantVisible ? fadeInDuration : fadeOutDuration;
            float speed = duration > 0.0001f ? 1f / duration : 1000f;

            _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, speed * dt);
            ApplyAlpha(_currentAlpha);
            ApplyOffset(_currentAlpha);
        }

        // -- 公共 API ---------------------------------------------------------

        public void Show()
        {
            _wantVisible = true;
            _targetAlpha = 1f;
        }

        public void Hide()
        {
            _wantVisible = false;
            _targetAlpha = 0f;
        }

        /// <summary>无视淡入淡出，立刻设到目标状态（场景初始化用）。</summary>
        public void SetImmediate(bool visible)
        {
            _wantVisible = visible;
            _currentAlpha = _targetAlpha = visible ? 1f : 0f;
            ApplyAlpha(_currentAlpha);
            ApplyOffset(_currentAlpha);
        }

        // -- 应用 -------------------------------------------------------------

        private void ApplyAlpha(float a)
        {
            for (int i = 0; i < _tmpTexts.Count; i++)
            {
                if (_tmpTexts[i] == null) continue;
                var c = _tmpTexts[i].color; c.a = a; _tmpTexts[i].color = c;
            }
            for (int i = 0; i < _spriteRenderers.Count; i++)
            {
                if (_spriteRenderers[i] == null) continue;
                var c = _spriteRenderers[i].color; c.a = a; _spriteRenderers[i].color = c;
            }
            for (int i = 0; i < _images.Count; i++)
            {
                if (_images[i] == null) continue;
                var c = _images[i].color; c.a = a; _images[i].color = c;
            }
            for (int i = 0; i < _canvasGroups.Count; i++)
            {
                if (_canvasGroups[i] == null) continue;
                _canvasGroups[i].alpha = a;
            }
        }

        private void ApplyOffset(float a)
        {
            if (Mathf.Approximately(floatOffset, 0f)) return;
            // a=0 时位置在基准下方 floatOffset；a=1 时回到基准
            float y = -floatOffset * (1f - a);
            transform.localPosition = _baseLocalPosition + new Vector3(0f, y, 0f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 编辑时调参方便预览
            if (!Application.isPlaying) CacheTargets();
        }
#endif
    }
}
