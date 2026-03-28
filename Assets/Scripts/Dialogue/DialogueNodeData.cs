// ----------------------------------------------------------------------------
// Script: DialogueNodeData
// 作用：保存单个对话节点的运行时数据，供 DialogueGraphData 与 DialogueRunner 使用。
// 使用方法/调用示例：由 CSV 导入流程生成，再通过 DialogueGraphData.TryGetNode() 读取。
// ----------------------------------------------------------------------------
using System;

namespace SeeAPsychologist.Dialogue
{
    /// <summary>
    /// 单条对白/旁白节点数据。
    /// </summary>
    [Serializable]
    public sealed class DialogueNodeData
    {
        public string nodeId;
        public string speakerName;
        public string portraitKey;
        public string text;
        public string nextId;
    }
}
