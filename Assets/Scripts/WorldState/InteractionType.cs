namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// 世界切换触发源（供交互系统/剧情系统调用）。
    /// </summary>
    public enum InteractionType
    {
        /// <summary>床交互：按解离度阈值决定目标世界。</summary>
        Bed = 0,

        /// <summary>药物交互：按定义强制进入现实世界。</summary>
        MedicineToReality = 10,

        /// <summary>药物交互：按定义强制进入意识世界。</summary>
        MedicineToConsciousness = 11,

        /// <summary>剧情事件：记忆播放结束强制进入意识世界。</summary>
        StoryEventToConsciousness = 20,

        /// <summary>咨询师治疗交互（意识世界减少Stress，现实世界增加Stress）。</summary>
        Therapy = 30,

        /// <summary>睡觉交互（意识世界减少Stress，现实世界增加Stress）。</summary>
        Sleep = 31,

        /// <summary>吃饭交互（意识世界减少Stress，现实世界增加Stress）。</summary>
        Eat = 32,

        /// <summary>娱乐交互（意识世界减少Stress，现实世界增加Stress）。</summary>
        Entertainment = 33
    }
}
