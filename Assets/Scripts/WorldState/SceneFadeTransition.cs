// ----------------------------------------------------------------------------
// Script: SceneFadeTransition
// 作用：场景切换时的全屏黑色淡入淡出过渡。
//
// 工作流程（FadeToScene 调用后）：
//   1. 屏幕从透明 → 黑色（fadeOutDuration）
//   2. 加载新场景（异步）
//   3. 短暂停留（holdDuration）让场景资源准备好
//   4. 屏幕从黑色 → 透明（fadeInDuration）
//
// 设计：
//   - 全局单例 + DontDestroyOnLoad，自动 Bootstrap，不需要场景里放
//   - 运行时动态创建黑色全屏 Canvas + Image（不需要 prefab）
//   - SortOrder 9999 → 在所有 UI 之上
// ----------------------------------------------------------------------------
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SeeAPsychologist.WorldState
{
    public sealed class SceneFadeTransition : MonoBehaviour
    {
        public static SceneFadeTransition Instance { get; private set; }

        [Header("时长")]
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.5f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.5f;
        [Tooltip("黑屏停留时长（让新场景准备时间）。")]
        [SerializeField, Min(0f)] private float holdDuration = 0.1f;

        [Header("颜色")]
        [SerializeField] private Color fadeColor = Color.black;

        private CanvasGroup _canvasGroup;
        private Image _fadeImage;
        private bool _isTransitioning;

        public bool IsTransitioning => _isTransitioning;

        // -- 自动 Bootstrap ---------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("SceneFadeTransition");
            go.AddComponent<SceneFadeTransition>();
        }

        // -- 生命周期 ---------------------------------------------------------

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateFadeUI();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // -- 公共 API ---------------------------------------------------------

        /// <summary>
        /// 淡出黑屏 → 加载新场景 → 淡入。
        /// </summary>
        /// <param name="sceneName">目标场景名</param>
        /// <param name="onSceneLoaded">加载完后（淡入前）的回调，可用于额外初始化</param>
        public void FadeToScene(string sceneName, Action onSceneLoaded = null)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[SceneFadeTransition] 正在过渡中，忽略新请求。");
                return;
            }
            StartCoroutine(FadeRoutine(sceneName, onSceneLoaded));
        }

        /// <summary>外部强制淡入（如游戏启动时主动揭幕）。</summary>
        public void ForceFadeIn() => StartCoroutine(FadeAlpha(1f, 0f, fadeInDuration));

        /// <summary>外部强制淡出（如玩家死亡定格）。</summary>
        public void ForceFadeOut() => StartCoroutine(FadeAlpha(0f, 1f, fadeOutDuration));

        // -- 内部 -------------------------------------------------------------

        private IEnumerator FadeRoutine(string sceneName, Action onSceneLoaded)
        {
            _isTransitioning = true;

            // 1. 淡入黑屏
            yield return FadeAlpha(0f, 1f, fadeOutDuration);

            // 2. 加载场景（异步，避免卡帧）
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone) yield return null;

            // 3. 让新场景的 Awake/Start 跑完 + 自定义回调（例如把玩家放 SpawnPoint）
            yield return null; // 等一帧
            try { onSceneLoaded?.Invoke(); }
            catch (Exception e) { Debug.LogError($"[SceneFadeTransition] onSceneLoaded 回调异常: {e}"); }

            if (holdDuration > 0f) yield return new WaitForSeconds(holdDuration);

            // 4. 淡出黑屏
            yield return FadeAlpha(1f, 0f, fadeInDuration);

            _isTransitioning = false;
        }

        private IEnumerator FadeAlpha(float from, float to, float duration)
        {
            if (_canvasGroup == null) yield break;

            if (duration <= 0.0001f)
            {
                _canvasGroup.alpha = to;
                _canvasGroup.blocksRaycasts = to > 0.5f;
                yield break;
            }

            _canvasGroup.blocksRaycasts = true;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }
            _canvasGroup.alpha = to;
            _canvasGroup.blocksRaycasts = to > 0.5f;
        }

        private void CreateFadeUI()
        {
            // Canvas
            var canvasGo = new GameObject("FadeCanvas");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            _canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            // 全屏 Image
            var imgGo = new GameObject("FadeImage");
            imgGo.transform.SetParent(canvasGo.transform, false);
            _fadeImage = imgGo.AddComponent<Image>();
            _fadeImage.color = fadeColor;
            _fadeImage.raycastTarget = true;

            var rt = _fadeImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
