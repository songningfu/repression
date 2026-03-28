// ----------------------------------------------------------------------------
// Script: PromptRequest
// 作用：定义提示系统运行时使用的请求快照，解耦提示源与玩家提示显示逻辑。
// 使用方法/调用示例：由 PromptSource 生成请求，再交给 PlayerPromptReceiver.TryShowPrompt() 消费。
// ----------------------------------------------------------------------------
namespace SeeAPsychologist.PromptSystem
{
    /// <summary>
    /// 运行时提示请求快照。
    /// 该结构只保存一次提示显示所需的最小信息，不包含可变状态。
    /// </summary>
    public readonly struct PromptRequest
    {
        public PromptSource Source { get; }
        public string Text { get; }
        public float TriggerChance { get; }
        public float ShowDuration { get; }
        public int Priority { get; }

        public PromptRequest(
            PromptSource source,
            string text,
            float triggerChance,
            float showDuration,
            int priority)
        {
            Source = source;
            Text = text;
            TriggerChance = triggerChance;
            ShowDuration = showDuration;
            Priority = priority;
        }
    }
}
