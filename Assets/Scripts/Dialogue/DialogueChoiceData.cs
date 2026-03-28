// ----------------------------------------------------------------------------
// Script: DialogueChoiceData
// 作用：保存单个对话选项的运行时数据，包括阈值过滤与应激改变量。
// 使用方法/调用示例：由 DialogueGraphData 返回后，交给 DialogueRunner 按当前应激度过滤。
// ----------------------------------------------------------------------------
using System;

namespace SeeAPsychologist.Dialogue
{
    /// <summary>
    /// 单个节点下的玩家选项定义。
    /// </summary>
    [Serializable]
    public sealed class DialogueChoiceData
    {
        public int choiceId;
        public int choiceOrder;
        public string nodeId;
        public string optionText;
        public string nextId;
        public int stressModifier;
        public int thresholdLow;
        public int thresholdHigh = 100;
        public DialogueChoiceAvailabilityMode unavailableMode = DialogueChoiceAvailabilityMode.Hidden;

        /// <summary>
        /// 判断当前应激度是否落在可用阈值区间内（闭区间）。
        /// </summary>
        public bool IsAvailableForStress(int stress)
        {
            return stress >= thresholdLow && stress <= thresholdHigh;
        }
    }
}
