namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// 世界切换事件载荷。只包含状态快照与触发上下文，不包含任何可变引用。
    /// </summary>
    public readonly struct WorldSwitchedEvent
    {
        public readonly WorldType PreviousWorld;
        public readonly WorldType CurrentWorld;
        public readonly InteractionType InteractionType;
        public readonly int DissociationAtSwitch;
        public readonly string Context;

        public WorldSwitchedEvent(
            WorldType previousWorld,
            WorldType currentWorld,
            InteractionType interactionType,
            int dissociationAtSwitch,
            string context)
        {
            PreviousWorld = previousWorld;
            CurrentWorld = currentWorld;
            InteractionType = interactionType;
            DissociationAtSwitch = dissociationAtSwitch;
            Context = context;
        }
    }
}

