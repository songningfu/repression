// ----------------------------------------------------------------------------
// Script: DialogueViewModels
// 作用：定义 DialogueRunner 对外广播给 UI/外部系统的只读事件载荷。
// 使用方法/调用示例：DialogueView 订阅这些结构体事件做显示，不直接修改核心对话状态。
// ----------------------------------------------------------------------------
namespace SeeAPsychologist.Dialogue
{
    /// <summary>
    /// 当前节点的展示数据。
    /// </summary>
    public readonly struct DialogueNodeViewModel
    {
        public readonly string NodeId;
        public readonly string SpeakerName;
        public readonly string PortraitKey;
        public readonly string Text;

        public DialogueNodeViewModel(string nodeId, string speakerName, string portraitKey, string text)
        {
            NodeId = nodeId;
            SpeakerName = speakerName;
            PortraitKey = portraitKey;
            Text = text;
        }
    }

    /// <summary>
    /// 单个选项的展示数据。
    /// </summary>
    public readonly struct DialogueChoiceViewModel
    {
        public readonly int ChoiceId;
        public readonly string Text;
        public readonly bool Interactable;
        public readonly int StressModifier;

        public DialogueChoiceViewModel(int choiceId, string text, bool interactable, int stressModifier)
        {
            ChoiceId = choiceId;
            Text = text;
            Interactable = interactable;
            StressModifier = stressModifier;
        }
    }

    /// <summary>
    /// 简版历史记录项。
    /// </summary>
    public readonly struct DialogueHistoryEntry
    {
        public readonly bool IsChoice;
        public readonly string SpeakerName;
        public readonly string Content;

        public DialogueHistoryEntry(bool isChoice, string speakerName, string content)
        {
            IsChoice = isChoice;
            SpeakerName = speakerName;
            Content = content;
        }
    }

    /// <summary>
    /// UI 控件状态。
    /// </summary>
    public readonly struct DialogueControlState
    {
        public readonly bool IsTyping;
        public readonly bool ShowNextButton;
        public readonly bool CanAdvance;
        public readonly bool FastModeEnabled;
        public readonly string NextButtonLabel;

        public DialogueControlState(
            bool isTyping,
            bool showNextButton,
            bool canAdvance,
            bool fastModeEnabled,
            string nextButtonLabel)
        {
            IsTyping = isTyping;
            ShowNextButton = showNextButton;
            CanAdvance = canAdvance;
            FastModeEnabled = fastModeEnabled;
            NextButtonLabel = nextButtonLabel;
        }
    }

    /// <summary>
    /// 对话结束事件载荷。
    /// </summary>
    public readonly struct DialogueSessionEndedEvent
    {
        public readonly string StartNodeId;
        public readonly string EndNodeId;
        public readonly string Reason;

        public DialogueSessionEndedEvent(string startNodeId, string endNodeId, string reason)
        {
            StartNodeId = startNodeId;
            EndNodeId = endNodeId;
            Reason = reason;
        }
    }
}
