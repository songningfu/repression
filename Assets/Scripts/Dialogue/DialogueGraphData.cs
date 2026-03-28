// ----------------------------------------------------------------------------
// Script: DialogueGraphData
// 作用：保存整套对话图的运行时数据与查找索引，作为 DialogueRunner 的唯一数据源。
// 使用方法/调用示例：由 CSV 导入工具生成资源，再拖给 DialogueRunner.graph 使用。
// ----------------------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;

namespace SeeAPsychologist.Dialogue
{
    /// <summary>
    /// 对话图数据资源。
    /// </summary>
    [CreateAssetMenu(menuName = "SeeAPsychologist/Dialogue/DialogueGraphData", fileName = "DialogueGraphData")]
    public sealed class DialogueGraphData : ScriptableObject
    {
        [SerializeField] private string graphId = "DialogueGraph";
        [SerializeField] private List<DialogueNodeData> nodes = new();
        [SerializeField] private List<DialogueChoiceGroupData> choiceGroups = new();

        private readonly Dictionary<string, DialogueNodeData> _nodeLookup = new();
        private readonly Dictionary<string, IReadOnlyList<DialogueChoiceData>> _choiceLookup = new();
        private static readonly IReadOnlyList<DialogueChoiceData> EmptyChoices = new List<DialogueChoiceData>();

        public string GraphId => graphId;
        public IReadOnlyList<DialogueNodeData> Nodes => nodes;
        public IReadOnlyList<DialogueChoiceGroupData> ChoiceGroups => choiceGroups;

        private void OnEnable()
        {
            RebuildLookup();
        }

        /// <summary>
        /// 用导入结果覆盖当前资源内容，并重建运行时索引。
        /// </summary>
        public void SetImportedData(
            string importedGraphId,
            List<DialogueNodeData> importedNodes,
            List<DialogueChoiceGroupData> importedChoiceGroups)
        {
            graphId = string.IsNullOrWhiteSpace(importedGraphId) ? name : importedGraphId.Trim();
            nodes = importedNodes ?? new List<DialogueNodeData>();
            choiceGroups = importedChoiceGroups ?? new List<DialogueChoiceGroupData>();
            RebuildLookup();
        }

        /// <summary>
        /// 根据 NodeId 读取节点。
        /// </summary>
        public bool TryGetNode(string nodeId, out DialogueNodeData node)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                node = null;
                return false;
            }

            if (_nodeLookup.Count != nodes.Count)
            {
                RebuildLookup();
            }

            return _nodeLookup.TryGetValue(nodeId.Trim(), out node);
        }

        /// <summary>
        /// 获取某个节点下的全部候选选项；若没有则返回空列表。
        /// </summary>
        public IReadOnlyList<DialogueChoiceData> GetChoices(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                return EmptyChoices;
            }

            if (_choiceLookup.Count != choiceGroups.Count)
            {
                RebuildLookup();
            }

            return _choiceLookup.TryGetValue(nodeId.Trim(), out var choices) ? choices : EmptyChoices;
        }

        /// <summary>
        /// 重建节点与选项查找字典。
        /// </summary>
        public void RebuildLookup()
        {
            _nodeLookup.Clear();
            _choiceLookup.Clear();

            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node == null || string.IsNullOrWhiteSpace(node.nodeId))
                {
                    continue;
                }

                var nodeId = node.nodeId.Trim();
                if (_nodeLookup.ContainsKey(nodeId))
                {
                    Debug.LogWarning($"[Dialogue] Duplicate nodeId '{nodeId}' found in graph '{name}'. Later entry ignored.");
                    continue;
                }

                _nodeLookup.Add(nodeId, node);
            }

            for (var i = 0; i < choiceGroups.Count; i++)
            {
                var group = choiceGroups[i];
                if (group == null || string.IsNullOrWhiteSpace(group.nodeId))
                {
                    continue;
                }

                var nodeId = group.nodeId.Trim();
                if (_choiceLookup.ContainsKey(nodeId))
                {
                    Debug.LogWarning($"[Dialogue] Duplicate choice group for nodeId '{nodeId}' found in graph '{name}'. Later group ignored.");
                    continue;
                }

                _choiceLookup.Add(nodeId, group.choices ?? EmptyChoices);
            }
        }
    }
}
