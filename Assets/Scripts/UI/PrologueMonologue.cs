// ----------------------------------------------------------------------------
// Script: PrologueMonologue
// 作用：开场黑屏独白序列。一段段文字淡入淡出 + BGM。
//
// 工作流程：
//   1. 启动时播 BGM（如果配了）
//   2. 依次播放 lines 列表里的每段文字
//   3. 每段：淡入 → 停留 → 淡出
//   4. 全部播完 → 用 SceneFadeTransition 跳到下一个场景
//   5. 玩家可按空格 / 任意键跳过
//
// 使用方法：
//   1. 新建 Prologue.unity 场景
//   2. 加 Canvas → Image（全屏黑底）+ TextMeshPro（屏幕中央）
//   3. 挂本组件，绑定 Text Component + Lines + Bgm Cue + Next Scene Name
// ----------------------------------------------------------------------------
using System;
using System.Collections;
using SeeAPsychologist.Audio;
using SeeAPsychologist.WorldState;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeeAPsychologist.UI
{
    public sealed class PrologueMonologue : MonoBehaviour
    {
        [Serializable]
        public class Line
        {
            [TextArea(2, 6)]
            [Tooltip("要显示的独白文本。可多行（用换行符）。")]
            public string text;

            [Tooltip("淡入时长（秒）。")]
            [Min(0f)] public float fadeIn = 1.2f;

            [Tooltip("完全显示的停留时长（秒）。")]
            [Min(0f)] public float hold = 4.0f;

            [Tooltip("淡出时长（秒）。")]
            [Min(0f)] public float fadeOut = 1.2f;

            [Tooltip("本段字体大小（覆盖默认）。0 = 用默认。")]
            public int fontSizeOverride = 0;
        }

        [Header("文本")]
        [Tooltip("用来显示独白的 TextMeshPro 组件。")]
        [SerializeField] private TMP_Text textComponent;

        [Tooltip("默认字体大小（每段可单独覆盖）。")]
        [SerializeField] private int defaultFontSize = 42;

        [Tooltip("独白列表，按顺序播放。")]
        [SerializeField] private Line[] lines;

        [Header("BGM")]
        [Tooltip("开场 BGM 的 AudioCue（可选）。")]
        [SerializeField] private AudioCue bgmCue;

        [Header("跳转")]
        [Tooltip("独白全部播完后加载的场景名（必须加入 Build Settings）。")]
        [SerializeField] private string nextSceneName = "Reality_Room";

        [Tooltip("用 SceneFadeTransition 黑屏过渡跳转（推荐）。")]
        [SerializeField] private bool useFadeTransition = true;

        [Header("跳过")]
        [Tooltip("是否允许玩家按键跳过。")]
        [SerializeField] private bool allowSkip = true;

        [Tooltip("跳过键。")]
        [SerializeField] private KeyCode skipKey = KeyCode.Space;

        [Tooltip("点击鼠标也跳过。")]
        [SerializeField] private bool skipOnMouseClick = true;

        [Header("起始")]
        [Tooltip("场景开始时先停顿多少秒再开始（让 BGM 先入场）。")]
        [Min(0f)] [SerializeField] private float initialDelay = 1.5f;

        [Tooltip("两段独白之间的间隔（秒）。")]
        [Min(0f)] [SerializeField] private float gapBetweenLines = 0.6f;

        private bool _skipped;

        private void Start()
        {
            if (textComponent == null)
            {
                Debug.LogError("[PrologueMonologue] textComponent 未配置。", this);
                LoadNext();
                return;
            }

            // 初始全透明
            SetAlpha(0f);
            textComponent.fontSize = defaultFontSize;

            // 播 BGM
            if (bgmCue != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(bgmCue);
            }

            StartCoroutine(PlaySequence());
        }

        private void Update()
        {
            if (_skipped || !allowSkip) return;
            bool keyPressed = Input.GetKeyDown(skipKey);
            bool clicked = skipOnMouseClick && Input.GetMouseButtonDown(0);
            if (keyPressed || clicked)
            {
                _skipped = true;
                StopAllCoroutines();
                LoadNext();
            }
        }

        private IEnumerator PlaySequence()
        {
            if (initialDelay > 0f) yield return new WaitForSecondsRealtime(initialDelay);

            if (lines == null || lines.Length == 0)
            {
                LoadNext();
                yield break;
            }

            foreach (var line in lines)
            {
                if (line == null || string.IsNullOrWhiteSpace(line.text)) continue;

                textComponent.text = line.text;
                textComponent.fontSize = line.fontSizeOverride > 0 ? line.fontSizeOverride : defaultFontSize;

                // 淡入
                yield return Fade(0f, 1f, line.fadeIn);
                // 停留
                if (line.hold > 0f) yield return new WaitForSecondsRealtime(line.hold);
                // 淡出
                yield return Fade(1f, 0f, line.fadeOut);
                // 段间间隔
                if (gapBetweenLines > 0f) yield return new WaitForSecondsRealtime(gapBetweenLines);
            }

            LoadNext();
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (duration <= 0.0001f)
            {
                SetAlpha(to);
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            SetAlpha(to);
        }

        private void SetAlpha(float a)
        {
            var c = textComponent.color; c.a = a; textComponent.color = c;
        }

        private void LoadNext()
        {
            // 停 BGM（淡出）
            if (AudioManager.Instance != null) AudioManager.Instance.StopMusic(1f);

            if (useFadeTransition && SceneFadeTransition.Instance != null)
            {
                SceneFadeTransition.Instance.FadeToScene(nextSceneName);
            }
            else
            {
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}
