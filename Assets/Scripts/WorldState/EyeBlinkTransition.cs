using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// 闭眼/睁眼转场效果
    /// 用于世界切换时的自然过渡
    /// </summary>
    public class EyeBlinkTransition : MonoBehaviour
    {
        [Header("UI设置")]
        [SerializeField] private Image blackScreen;
        [SerializeField] private Canvas transitionCanvas;

        [Header("动画设置")]
        [SerializeField] private float closeEyeDuration = 1.5f; // 闭眼时间
        [SerializeField] private float openEyeDuration = 2.0f;  // 睁眼时间
        [SerializeField] private float blackScreenDuration = 1.0f; // 黑屏停留时间

        [Header("动画曲线")]
        [SerializeField] private AnimationCurve closeEyeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve openEyeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        private static EyeBlinkTransition _instance;
        public static EyeBlinkTransition Instance => _instance;

        private bool isTransitioning = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            SetupUI();
        }

        private void SetupUI()
        {
            // 如果没有设置，自动创建
            if (transitionCanvas == null)
            {
                GameObject canvasObj = new GameObject("TransitionCanvas");
                canvasObj.transform.SetParent(transform);
                transitionCanvas = canvasObj.AddComponent<Canvas>();
                transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                transitionCanvas.sortingOrder = 9999; // 最高层级

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // 创建黑屏
            if (blackScreen == null)
            {
                GameObject screenObj = new GameObject("BlackScreen");
                screenObj.transform.SetParent(transitionCanvas.transform, false);

                RectTransform rect = screenObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;

                blackScreen = screenObj.AddComponent<Image>();
                blackScreen.color = Color.black;
            }

            // 初始化为透明
            SetAlpha(0);
        }

        /// <summary>
        /// 执行闭眼/睁眼转场效果
        /// </summary>
        public void PlayTransition(System.Action onBlackScreen = null)
        {
            if (isTransitioning) return;
            StartCoroutine(TransitionCoroutine(onBlackScreen));
        }

        private IEnumerator TransitionCoroutine(System.Action onBlackScreen)
        {
            isTransitioning = true;

            // 1. 闭眼（渐黑）
            yield return StartCoroutine(CloseEyes());

            // 2. 黑屏停留
            yield return new WaitForSeconds(blackScreenDuration);

            // 3. 执行世界切换
            onBlackScreen?.Invoke();

            // 4. 睁眼（渐亮）
            yield return StartCoroutine(OpenEyes());

            isTransitioning = false;
        }

        private IEnumerator CloseEyes()
        {
            float elapsed = 0f;
            while (elapsed < closeEyeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / closeEyeDuration;
                float alpha = closeEyeCurve.Evaluate(t);
                SetAlpha(alpha);
                yield return null;
            }
            SetAlpha(1);
        }

        private IEnumerator OpenEyes()
        {
            float elapsed = 0f;
            while (elapsed < openEyeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / openEyeDuration;
                float alpha = openEyeCurve.Evaluate(t);
                SetAlpha(alpha);
                yield return null;
            }
            SetAlpha(0);
        }

        private void SetAlpha(float alpha)
        {
            if (blackScreen != null)
            {
                Color color = blackScreen.color;
                color.a = alpha;
                blackScreen.color = color;
            }
        }

        /// <summary>
        /// 设置转场时间
        /// </summary>
        public void SetTransitionDuration(float close, float open, float blackDuration)
        {
            closeEyeDuration = close;
            openEyeDuration = open;
            blackScreenDuration = blackDuration;
        }
    }
}

