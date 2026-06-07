// ----------------------------------------------------------------------------
// Script: AudioCueTrigger
// 作用：给场景物体一个"播 AudioCue"的入口，方便 UnityEvent / Animation Event 调用。
//
// 使用方法：
//   1. 挂在任何物体上（冰箱、门、开关等）
//   2. 拖入要播的 AudioCue
//   3. 在 InteractableObject.OnInteract 或 UnityEvent 列表里调用本组件的 Play()
//
// 示例（冰箱）：
//   冰箱 GameObject
//   ├ InteractableObject
//   │  └ OnInteract → AudioCueTrigger.Play()
//   │  └ OnInteract → DialogueTrigger.TryStartDialogue()
//   └ AudioCueTrigger
//      └ Cue: Cue_SFX_FridgeOpen
// ----------------------------------------------------------------------------
using UnityEngine;

namespace SeeAPsychologist.Audio
{
    public sealed class AudioCueTrigger : MonoBehaviour
    {
        [Tooltip("要播放的 AudioCue。")]
        [SerializeField] private AudioCue cue;

        [Tooltip("3D 空间音：勾上 = 在本物体位置播；不勾 = 2D 全屏。")]
        [SerializeField] private bool playAtPosition = false;

        public AudioCue Cue { get => cue; set => cue = value; }

        /// <summary>无参版本，UnityEvent 默认能直接选。</summary>
        public void Play()
        {
            if (cue == null || AudioManager.Instance == null) return;
            if (playAtPosition) AudioManager.Instance.PlaySfx(cue, transform.position);
            else AudioManager.Instance.PlaySfx(cue);
        }

        /// <summary>允许外部传 cue（覆盖默认）。</summary>
        public void PlayCue(AudioCue overrideCue)
        {
            var c = overrideCue != null ? overrideCue : cue;
            if (c == null || AudioManager.Instance == null) return;
            if (playAtPosition) AudioManager.Instance.PlaySfx(c, transform.position);
            else AudioManager.Instance.PlaySfx(c);
        }
    }
}
