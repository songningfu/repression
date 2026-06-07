// ----------------------------------------------------------------------------
// Script: AudioManagerBootstrapper
// 作用：游戏启动时自动创建 AudioManager，无需在场景里手动放置。
// ----------------------------------------------------------------------------
using UnityEngine;

namespace SeeAPsychologist.Audio
{
    public static class AudioManagerBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (AudioManager.Instance != null) return;

            // 注意：不加 AudioListener —— 场景里的 Main Camera 已经有了，
            // 加在这里会导致 Unity 报 "Multiple AudioListeners" 警告。
            var go = new GameObject("AudioManager");
            go.AddComponent<AudioManager>();
        }
    }
}
