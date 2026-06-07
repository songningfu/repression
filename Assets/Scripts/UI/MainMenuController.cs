// ----------------------------------------------------------------------------
// Script: MainMenuController
// 作用：主菜单按钮逻辑。
//
// 提供的公共方法（直接绑到 UI Button.OnClick）：
//   - OnNewGame()    新游戏 → 加载游戏第一个场景
//   - OnContinue()   继续 → 加载保存的进度（暂未实现，预留接口）
//   - OnSettings()   设置 → 打开/关闭设置面板
//   - OnQuit()       退出游戏
//
// 顺带处理：
//   - 进入主菜单时自动播放主菜单 BGM（如果配了 AudioCue）
//   - 离开主菜单时停止 BGM
//
// 使用方法：
//   1. 在 MainMenu 场景里挂本组件到 Canvas 或某个空对象上
//   2. 配 New Game Scene Name = "Reality_Room"
//   3. 每个 Button 的 OnClick 拖入本组件 → 选对应方法
//   4. Settings Panel 拖入设置面板（可选）
//   5. Bgm Cue 拖入主菜单 BGM AudioCue（可选）
// ----------------------------------------------------------------------------
using SeeAPsychologist.Audio;
using SeeAPsychologist.WorldState;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeeAPsychologist.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("场景跳转")]
        [Tooltip("点击“新游戏”后加载的场景名（游戏第一关）。")]
        [SerializeField] private string newGameSceneName = "Reality_Room";

        [Tooltip("是否用 SceneFadeTransition 黑屏过渡（如果项目里已经有）。")]
        [SerializeField] private bool useFadeTransition = true;

        [Header("UI 引用")]
        [Tooltip("设置面板（可选）。点设置按钮会切换它的显隐。")]
        [SerializeField] private GameObject settingsPanel;

        [Tooltip("\"继续\"按钮的 GameObject。没有存档时会自动隐藏。")]
        [SerializeField] private GameObject continueButton;

        [Header("音频")]
        [Tooltip("主菜单 BGM 的 AudioCue（可选）。")]
        [SerializeField] private AudioCue bgmCue;

        [Tooltip("按钮点击音效（可选）。")]
        [SerializeField] private AudioCue clickCue;

        private void Start()
        {
            // 隐藏设置面板
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // 没有存档时隐藏"继续"按钮
            if (continueButton != null)
            {
                continueButton.SetActive(HasSaveData());
            }

            // 播主菜单 BGM
            if (bgmCue != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(bgmCue);
            }
        }

        // -- 按钮回调 ----------------------------------------------------

        public void OnNewGame()
        {
            PlayClickSfx();
            LoadGameScene(newGameSceneName);
        }

        public void OnContinue()
        {
            PlayClickSfx();
            // TODO: 读取存档后跳转保存的场景
            // 现在暂时跟"新游戏"一样
            LoadGameScene(newGameSceneName);
        }

        public void OnSettings()
        {
            PlayClickSfx();
            if (settingsPanel == null) return;
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }

        public void OnQuit()
        {
            PlayClickSfx();
            // 停 BGM
            if (AudioManager.Instance != null) AudioManager.Instance.StopMusic(0.3f);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // -- 内部 --------------------------------------------------------

        private void LoadGameScene(string sceneName)
        {
            // 停 BGM（淡出）
            if (AudioManager.Instance != null) AudioManager.Instance.StopMusic(0.5f);

            if (useFadeTransition && SceneFadeTransition.Instance != null)
            {
                SceneFadeTransition.Instance.FadeToScene(sceneName);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        private void PlayClickSfx()
        {
            if (clickCue == null || AudioManager.Instance == null) return;
            AudioManager.Instance.PlaySfx(clickCue);
        }

        private static bool HasSaveData()
        {
            // TODO: 实际项目里接入你的存档系统
            return PlayerPrefs.HasKey("SaveData");
        }
    }
}
