// ----------------------------------------------------------------------------
// Script: AudioCategory
// 作用：音频分类枚举。每个分类对应独立的音量通道与混音组。
// ----------------------------------------------------------------------------
namespace SeeAPsychologist.Audio
{
    /// <summary>
    /// 音频分类。每类音量独立可调，分别走不同的 AudioMixer Group（如果配置了）。
    /// </summary>
    public enum AudioCategory
    {
        /// <summary>背景音乐（BGM）。长循环、淡入淡出。同时只有 1 个。</summary>
        Music = 0,

        /// <summary>环境氛围音（雨声、风声、走廊回响）。长循环，可同时存在多个。</summary>
        Ambient = 1,

        /// <summary>音效（脚步、开门、物品互动）。短促、量大、池化播放。</summary>
        SFX = 2,

        /// <summary>UI 音（按钮点击、菜单切换、对话出字音）。短促、确定性。</summary>
        UI = 3,

        /// <summary>语音/对白（角色配音）。同时通常只有 1 个。</summary>
        Voice = 4
    }
}
