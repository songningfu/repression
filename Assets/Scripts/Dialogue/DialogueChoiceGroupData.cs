// ----------------------------------------------------------------------------
// Script: DialogueChoiceGroupData
// 作用：把同一 NodeId 下的多个选项序列化存入 DialogueGraphData。
// 使用方法/调用示例：由 CSV 导入器按 NodeId 聚合生成，运行时通过 DialogueGraphData.GetChoices() 读取。
// ----------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace SeeAPsychologist.Dialogue
{
    /// <summary>
    /// 某个对话节点对应的选项集合。
    /// </summary>
    [Serializable]
    public sealed class DialogueChoiceGroupData
    {
        public string nodeId;
        public List<DialogueChoiceData> choices = new();
    }
}
