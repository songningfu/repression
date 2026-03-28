// ----------------------------------------------------------------------------
// Script: DialogueCsvImportDefinition
// 作用：定义一组 CSV 导入输入与输出资源，供编辑器按钮生成 DialogueGraphData。
// 使用方法/调用示例：创建该资源，绑定 Nodes/Choices 两张 CSV，再点击自定义 Inspector 的“生成/更新图资源”。
// ----------------------------------------------------------------------------
using UnityEngine;

namespace SeeAPsychologist.Dialogue
{
    /// <summary>
    /// 对话 CSV 导入定义。
    /// </summary>
    [CreateAssetMenu(menuName = "SeeAPsychologist/Dialogue/DialogueCsvImportDefinition", fileName = "DialogueCsvImportDefinition")]
    public sealed class DialogueCsvImportDefinition : ScriptableObject
    {
        [Header("Import Source")]
        [SerializeField] private TextAsset nodesCsv;
        [SerializeField] private TextAsset choicesCsv;

        [Header("Output")]
        [SerializeField] private DialogueGraphData outputGraph;
        [SerializeField] private string graphId = "DialogueGraph";

        [Header("Import Options")]
        [SerializeField] private bool clearExistingOutputBeforeImport = true;

        public TextAsset NodesCsv => nodesCsv;
        public TextAsset ChoicesCsv => choicesCsv;
        public DialogueGraphData OutputGraph => outputGraph;
        public string GraphId => graphId;
        public bool ClearExistingOutputBeforeImport => clearExistingOutputBeforeImport;

        /// <summary>
        /// 由编辑器脚本在首次生成资源时回填输出图引用。
        /// </summary>
        public void SetOutputGraph(DialogueGraphData graph)
        {
            outputGraph = graph;
        }
    }
}
