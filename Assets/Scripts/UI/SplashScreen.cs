// ----------------------------------------------------------------------------
// Script: SplashScreen
// 作用：游戏启动的开屏 logo 序列。挂在 Splash 场景里。
//
// 工作流程：
//   1. 依次播放 entries 列表里的每个 logo
//   2. 每个 logo：淡入 → 停留 → 淡出
//   3. 全部播完 → 跳转到下一个场景（默认 MainMenu）
//   4. 用户按任意键/点击可以跳过整个序列（可关）
//
// 使用方法：
//   1. 在 Splash 场景里建个 Canvas（Screen Space Overlay）+ 一个全屏 Image（默认 alpha=0）
//   2. 挂本组件 + 拖入那个 Image 到 Target Image 字段
//   3. 在 Entries 配多个 logo（每个一张图 + 时间参数）
//   4. 配 Next Scene Name 为 "MainMenu"
// ----------------------------------------------------------------------------
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SeeAPsychologist.UI
{
    public sealed class SplashScreen : MonoBehaviour
    {
        [Serializable]
        public class LogoEntry
        {
            [Tooltip("要显示的 logo 图片。")]
            public Sprite logo;

            [Tooltip("淡入用时（秒）。")]
            [Min(0f)] public float fadeIn = 1.5f;

            [Tooltip("完全显示的停留时长（秒）。")]
            [Min(0f)] public float hold = 1.5f;

            [Tooltip("淡出用时（秒）。")]
            [Min(0f)] public float fadeOut = 1f;

            [Tooltip("背景色（一般纯黑，特殊场合可不同）。")]
            public Color backgroundColor = Color.black;
        }

        [Header("Logo 序列")]
        [Tooltip("按顺序播放的 logo 列表。")]
        [SerializeField] private LogoEntry[] entries;

        [Header("引用")]
        [Tooltip("用来显示 logo 的全屏 Image（Canvas 下的子物体）。")]
        [SerializeField] private Image logoImage;

        [Tooltip("背景纯色 Image（可选，铺满屏幕）。")]
        [SerializeField] private Image backgroundImage;

        [Header("跳转")]
        [Tooltip("全部 logo 播完后要加载的场景名（必须在 Build Settings）。")]
        [SerializeField] private string nextSceneName = "MainMenu";

        [Header("交互")]
        [Tooltip("勾上 = 玩家按任意键或点击鼠标可以跳过整个 Splash。")]
        [SerializeField] private bool allowSkip = true;

        private bool _skipped;

        private void Start()
        {
            if (logoImage == null)
            {
                Debug.LogError("[SplashScreen] logoImage 未配置。", this);
                LoadNext();
                return;
            }

            // 初始全透明
            var c = logoImage.color; c.a = 0f; logoImage.color = c;

            StartCoroutine(PlaySequence());
        }

        private void Update()
        {
            if (!allowSkip || _skipped) return;
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                _skipped = true;
                StopAllCoroutines();
                LoadNext();
            }
        }

        private IEnumerator PlaySequence()
        {
            if (entries == null || entries.Length == 0)
            {
                Debug.LogWarning("[SplashScreen] entries 为空，直接跳转。");
                LoadNext();
                yield break;
            }

            foreach (var entry in entries)
            {
                if (entry == null || entry.logo == null) continue;

                // 背景色
                if (backgroundImage != null) backgroundImage.color = entry.backgroundColor;

                logoImage.sprite = entry.logo;
                logoImage.preserveAspect = true;

                // 淡入
                yield return FadeImage(logoImage, 0f, 1f, entry.fadeIn);
                // 停留
                if (entry.hold > 0f) yield return new WaitForSeconds(entry.hold);
                // 淡出
                yield return FadeImage(logoImage, 1f, 0f, entry.fadeOut);
            }

            LoadNext();
        }

        private static IEnumerator FadeImage(Image img, float from, float to, float duration)
        {
            if (duration <= 0.0001f)
            {
                var c = img.color; c.a = to; img.color = c;
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var c = img.color; c.a = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)); img.color = c;
                yield return null;
            }
            var fc = img.color; fc.a = to; img.color = fc;
        }

        private void LoadNext()
        {
            if (string.IsNullOrWhiteSpace(nextSceneName))
            {
                Debug.LogError("[SplashScreen] nextSceneName 为空。", this);
                return;
            }
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
