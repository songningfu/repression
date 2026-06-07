// ----------------------------------------------------------------------------
// Script: SceneFootstepDefault
// 作用：场景级"默认脚步声"。挂在每个场景里的一个空物体上，自动告诉 PlayerFootsteps
//       "本场景的默认地面是什么音效"。
//
// 工作原理：
//   - Start 时找到 Tag=Player 的持久玩家
//   - 调 PlayerFootsteps.SetSceneDefaultCue(cue)
//   - 切到下个场景时，下个场景的 SceneFootstepDefault 接管
//
// 与 FootstepSurface 的区别：
//   - SceneFootstepDefault = 整个场景的默认音（一定生效）
//   - FootstepSurface = 场景内的小区域（地毯、瓷砖等），临时覆盖
//
// 使用方法：
//   1. 每个场景里建空 GameObject → 命名 "FootstepDefault"
//   2. 挂本组件 + 拖入该场景对应的 AudioCue
//      - Reality.unity        → Cue_SFX_Footstep_Wood（木地板）
//      - Reality_Street.unity → Cue_SFX_Footstep_Street（街道）
//      - Consciousness.unity  → Cue_SFX_Footstep_Dream（梦境，可选）
// ----------------------------------------------------------------------------
using SeeAPsychologist.Player;
using UnityEngine;

namespace SeeAPsychologist.Audio
{
    public sealed class SceneFootstepDefault : MonoBehaviour
    {
        [Tooltip("本场景的默认脚步声 AudioCue。")]
        [SerializeField] private AudioCue cueForThisScene;

        [Tooltip("玩家 Tag。")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("延迟应用（秒）。SceneDoor 把玩家落到 SpawnPoint 是异步的，稍微等一下避免抢先。")]
        [SerializeField, Min(0f)] private float applyDelay = 0.05f;

        private void Start()
        {
            if (applyDelay > 0f) Invoke(nameof(Apply), applyDelay);
            else Apply();
        }

        private void Apply()
        {
            if (cueForThisScene == null)
            {
                Debug.LogWarning($"[SceneFootstepDefault] {name}: cueForThisScene 未设置。", this);
                return;
            }

            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null)
            {
                Debug.LogWarning($"[SceneFootstepDefault] {name}: 找不到 Tag={playerTag} 的玩家。", this);
                return;
            }

            var pf = player.GetComponent<PlayerFootsteps>();
            if (pf == null)
            {
                Debug.LogWarning($"[SceneFootstepDefault] {name}: 玩家上没有 PlayerFootsteps 组件。", this);
                return;
            }

            pf.SetSceneDefaultCue(cueForThisScene);
            Debug.Log($"[SceneFootstepDefault] 场景 '{gameObject.scene.name}' 默认脚步声切换为 '{cueForThisScene.name}'");
        }
    }
}
