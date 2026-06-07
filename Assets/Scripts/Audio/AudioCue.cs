// ----------------------------------------------------------------------------
// Script: AudioCue
// 作用：可配置的"音频线索"数据资源。一个 AudioCue = 一组可随机的 AudioClip + 播放参数。
//
// 设计目标：
//   - 数据驱动：策划/美术不需要改代码就能调音量、音调、添加新片段
//   - 复用：同一段脚步声 cue 可被多个 player 共用
//   - 自然随机：避免重复感（音调音量轻微浮动、多片段随机选）
//
// 使用方法：
//   1. Project 右键 → Create → SeeAPsychologist → Audio → AudioCue
//   2. 在 Inspector 配置 clips / 音量 / 音调 / 是否循环 / 类别
//   3. 代码里：AudioManager.Instance.Play(myCue) 或者从组件引用直接调
// ----------------------------------------------------------------------------
using UnityEngine;

namespace SeeAPsychologist.Audio
{
    [CreateAssetMenu(menuName = "SeeAPsychologist/Audio/AudioCue", fileName = "AudioCue_")]
    public sealed class AudioCue : ScriptableObject
    {
        [Header("音频片段（随机选）")]
        [Tooltip("一个或多个音频片段。播放时随机选一个，避免重复感。")]
        public AudioClip[] clips;

        [Header("分类")]
        [Tooltip("决定走哪个音量通道。例如 SFX 会被 SFX 音量影响。")]
        public AudioCategory category = AudioCategory.SFX;

        [Header("音量（基础 × 随机区间）")]
        [Tooltip("基础音量（在 AudioManager 的总音量/类别音量基础上再乘）。")]
        [Range(0f, 1f)] public float baseVolume = 1f;

        [Tooltip("音量随机区间（每次播放在 baseVolume × [min,max] 之间）。")]
        public Vector2 volumeRandomRange = new Vector2(0.95f, 1.05f);

        [Header("音调")]
        [Tooltip("音调随机区间（1.0 = 原音调）。轻微随机让音效更自然。")]
        public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

        [Header("播放参数")]
        [Tooltip("是否循环。一般 Music / Ambient 勾，SFX 不勾。")]
        public bool loop = false;

        [Tooltip("空间化：0 = 2D 全屏（UI/Music），1 = 完全 3D（位置音源）。SFX 一般 0~0.3。")]
        [Range(0f, 1f)] public float spatialBlend = 0f;

        [Tooltip("两次播放的最小间隔（秒）。防止短时间重复触发。0 = 不限制。")]
        [Min(0f)] public float minIntervalSeconds = 0f;

        [Header("淡入淡出（仅 Music / Ambient 用）")]
        [Tooltip("淡入时长（秒）。0 = 不淡入。")]
        [Min(0f)] public float fadeInSeconds = 0f;

        [Tooltip("淡出时长（秒）。0 = 不淡出。")]
        [Min(0f)] public float fadeOutSeconds = 0f;

        // -- 运行时辅助 ---------------------------------------------------------

        /// <summary>随机选一个片段。</summary>
        public AudioClip PickRandomClip()
        {
            if (clips == null || clips.Length == 0) return null;
            // 单个直接返；多个跳过 null 后随机
            if (clips.Length == 1) return clips[0];
            int safety = 0;
            while (safety++ < 8)
            {
                var c = clips[Random.Range(0, clips.Length)];
                if (c != null) return c;
            }
            return null;
        }

        /// <summary>本次随机化后的最终音量。</summary>
        public float PickVolume()
        {
            float r = Random.Range(volumeRandomRange.x, volumeRandomRange.y);
            return Mathf.Clamp01(baseVolume * r);
        }

        /// <summary>本次随机化后的音调。</summary>
        public float PickPitch()
        {
            return Random.Range(pitchRange.x, pitchRange.y);
        }
    }
}
