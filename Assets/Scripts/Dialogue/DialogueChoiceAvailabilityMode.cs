// ----------------------------------------------------------------------------
// Script: DialogueChoiceAvailabilityMode
// 作用：定义超出应激阈值时，对话选项在 UI 中的呈现方式。
// 使用方法/调用示例：在 CSV 中填写 UnavailableMode 字段，或在生成后的数据资源中直接配置。
// ----------------------------------------------------------------------------
namespace SeeAPsychologist.Dialogue
{
    /// <summary>
    /// 选项不可用时的显示策略。
    /// Hidden：完全隐藏；Disabled：显示但不可点击。
    /// </summary>
    public enum DialogueChoiceAvailabilityMode
    {
        Hidden = 0,
        Disabled = 1,
    }
}
