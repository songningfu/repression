namespace SeeAPsychologist.MentalStats
{
    /// <summary>
    /// 数值变化事件载荷：携带前后状态、差值与来源。
    /// 注意：事件回调保证在 Unity 主线程触发（避免 UI / 渲染订阅线程问题）。
    /// </summary>
    public readonly struct MentalStatsChangedEvent
    {
        public readonly MentalStatsState Previous;
        public readonly MentalStatsState Current;
        public readonly int DeltaStress;
        public readonly int DeltaDissociation;
        public readonly MentalStatChangeSource Source;
        public readonly string Context;

        public MentalStatsChangedEvent(
            MentalStatsState previous,
            MentalStatsState current,
            int deltaStress,
            int deltaDissociation,
            MentalStatChangeSource source,
            string context)
        {
            Previous = previous;
            Current = current;
            DeltaStress = deltaStress;
            DeltaDissociation = deltaDissociation;
            Source = source;
            Context = context;
        }
    }
}

