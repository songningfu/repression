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
        StoryEventToConsciousness = 20
    }
}

