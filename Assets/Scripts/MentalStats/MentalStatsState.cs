namespace SeeAPsychologist.MentalStats
{
    /// <summary>
    /// 心理稳态数值快照（只读、可用于事件派发与回放/调试）。
    /// </summary>
    public readonly struct MentalStatsState
    {
        public readonly int Stress;
        public readonly int Dissociation;

        public MentalStatsState(int stress, int dissociation)
        {
            Stress = stress;
            Dissociation = dissociation;
        }
    }
}

