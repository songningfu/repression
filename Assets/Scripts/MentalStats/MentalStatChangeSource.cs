namespace SeeAPsychologist.MentalStats
{
    /// <summary>
    /// 数值变更来源（用于日志/分析/联动追踪）。
    /// </summary>
    public enum MentalStatChangeSource
    {
        Unknown = 0,
        DebugUI = 1,
        DialogueOption = 2,
        Drug = 3,
        Minigame = 4,
        PlotEvent = 5,
        AutoTick = 6,
    }
}

