// ----------------------------------------------------------------------------
// Script: SceneBgmStarter
// 作用：场景启动时自动播放指定的 BGM。一个场景挂一份。
//
// 工作原理：
//   - Start 时调 AudioManager.PlayMusic(bgmCue)
//   - AudioManager 会自动交叉淡入淡出（如果上个场景有 BGM 在播）
//   - 切到下个场景时，下个场景的 SceneBgmStarter 自动接管
//
// 使用方法：
//   1. 每个场景里建空 GameObject → 命名 "BgmStarter"
//   2. 挂本组件
//   3. Bgm Cue 拖入该场景对应的 AudioCue
//      - Reality_Room        → Cue_BGM_Reality
//      - Reality_Street      → Cue_BGM_Street (可选，也可以共用 Reality)
//      - Consciousness_Dream → Cue_BGM_Consciousness
//      - MainMenu            → Cue_BGM_MainMenu
//
// 如果想要"切场景时停 BGM"，把 Bgm Cue 留空 + Stop On Start 勾上
// ----------------------------------------------------------------------------
using UnityEngine;

namespace SeeAPsychologist.Audio
{
    public sealed class SceneBgmStarter : MonoBehaviour
    {
        [Tooltip("本场景要播放的 BGM AudioCue（Category 应该是 Music，Loop 勾选）。")]
        [SerializeField] private AudioCue bgmCue;

        [Tooltip("勾上 = 即使 bgmCue 为空也强制停掉当前 BGM。适合需要静音的场景（如 Cutscene）。")]
        [SerializeField] private bool stopOnStart = false;

        [Tooltip("延迟应用（秒）。等其他 Manager 初始化好。")]
        [SerializeField, Min(0f)] private float delay = 0.1f;

        private void Start()
        {
            if (delay > 0f) Invoke(nameof(Apply), delay);
            else Apply();
        }

        private void Apply()
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("[SceneBgmStarter] AudioManager 不可用。");
                return;
            }

            if (bgmCue != null)
            {
                AudioManager.Instance.PlayMusic(bgmCue);
                Debug.Log($"[SceneBgmStarter] 播放 BGM: {bgmCue.name}");
            }
            else if (stopOnStart)
            {
                AudioManager.Instance.StopMusic(1f);
                Debug.Log("[SceneBgmStarter] 停止 BGM（场景无 BGM）。");
            }
        }
    }
}
