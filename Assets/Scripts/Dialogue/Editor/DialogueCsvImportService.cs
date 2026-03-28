// ----------------------------------------------------------------------------
// Script: DialogueCsvImportService
// 作用：把节点/选项 CSV 解析并生成 DialogueGraphData 资源，同时做基础校验。
// 使用方法/调用示例：仅供编辑器按钮或菜单调用，执行 Import(definition) 完成资源生成。
// ----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SeeAPsychologist.Dialogue.Editor
{
    /// <summary>
    /// 对话 CSV 导入服务。
    /// </summary>
    internal static class DialogueCsvImportService
    {
        internal static DialogueGraphData Import(DialogueCsvImportDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (definition.NodesCsv == null || definition.ChoicesCsv == null)
            {
                throw new InvalidOperationException("NodesCsv / ChoicesCsv 不能为空。");
            }

            var nodeRows = DialogueCsvParser.ParseTable(definition.NodesCsv, "DialogueNodes");
            var choiceRows = DialogueCsvParser.ParseTable(definition.ChoicesCsv, "DialogueChoices");

            var nodes = ParseNodes(nodeRows);
            var choiceGroups = ParseChoiceGroups(choiceRows, nodes);
            ValidateLinks(nodes, choiceGroups);

            var graph = definition.OutputGraph;
            if (graph == null)
            {
                graph = CreateOutputGraph(definition);
                definition.SetOutputGraph(graph);
                EditorUtility.SetDirty(definition);
            }

            Undo.RecordObject(graph, "Import Dialogue Graph");
            graph.SetImportedData(
                importedGraphId: definition.GraphId,
                importedNodes: nodes,
                importedChoiceGroups: choiceGroups);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return graph;
        }

        private static List<DialogueNodeData> ParseNodes(List<Dictionary<string, string>> rows)
        {
            var result = new List<DialogueNodeData>(rows.Count);
            var usedNodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var nodeId = Required(row, "NodeId", $"DialogueNodes row {i + 2}");
                if (!usedNodeIds.Add(nodeId))
                {
                    throw new InvalidOperationException($"DialogueNodes 中存在重复 NodeId：{nodeId}");
                }

                result.Add(new DialogueNodeData
                {
                    nodeId = nodeId,
                    speakerName = Optional(row, "SpeakerName"),
                    portraitKey = Optional(row, "PortraitKey"),
                    text = Required(row, "Text", $"DialogueNodes row {i + 2}"),
                    nextId = Optional(row, "NextId"),
                });
            }

            return result;
        }

        private static List<DialogueChoiceGroupData> ParseChoiceGroups(
            List<Dictionary<string, string>> rows,
            List<DialogueNodeData> nodes)
        {
            var nodeIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < nodes.Count; i++)
            {
                nodeIdSet.Add(nodes[i].nodeId);
            }

            var usedChoiceIds = new HashSet<int>();
            var groups = new Dictionary<string, List<DialogueChoiceData>>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowName = $"DialogueChoices row {i + 2}";
                var choiceId = ParseInt(Required(row, "ChoiceId", rowName), "ChoiceId", rowName);
                if (!usedChoiceIds.Add(choiceId))
                {
                    throw new InvalidOperationException($"DialogueChoices 中存在重复 ChoiceId：{choiceId}");
                }

                var nodeId = Required(row, "NodeId", rowName);
                if (!nodeIdSet.Contains(nodeId))
                {
                    throw new InvalidOperationException($"{rowName} 引用了不存在的 NodeId：{nodeId}");
                }

                if (!groups.TryGetValue(nodeId, out var list))
                {
                    list = new List<DialogueChoiceData>();
                    groups.Add(nodeId, list);
                }

                var choice = new DialogueChoiceData
                {
                    choiceId = choiceId,
                    choiceOrder = ParseInt(Required(row, "ChoiceOrder", rowName), "ChoiceOrder", rowName),
                    nodeId = nodeId,
                    optionText = Required(row, "OptionText", rowName),
                    nextId = Optional(row, "NextId"),
                    stressModifier = ParseInt(Optional(row, "StressModifier", "0"), "StressModifier", rowName),
                    thresholdLow = ParseInt(Optional(row, "ThresholdLow", "0"), "ThresholdLow", rowName),
                    thresholdHigh = ParseInt(Optional(row, "ThresholdHigh", "100"), "ThresholdHigh", rowName),
                    unavailableMode = ParseUnavailableMode(Optional(row, "UnavailableMode", "Hidden"), rowName),
                };

                if (choice.thresholdLow > choice.thresholdHigh)
                {
                    throw new InvalidOperationException($"{rowName} 的 ThresholdLow 不能大于 ThresholdHigh。");
                }

                list.Add(choice);
            }

            var result = new List<DialogueChoiceGroupData>(groups.Count);
            foreach (var pair in groups)
            {
                pair.Value.Sort((a, b) => a.choiceOrder.CompareTo(b.choiceOrder));
                result.Add(new DialogueChoiceGroupData
                {
                    nodeId = pair.Key,
                    choices = pair.Value,
                });
            }

            return result;
        }

        private static void ValidateLinks(List<DialogueNodeData> nodes, List<DialogueChoiceGroupData> choiceGroups)
        {
            var nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < nodes.Count; i++)
            {
                nodeIds.Add(nodes[i].nodeId);
            }

            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (!string.IsNullOrWhiteSpace(node.nextId) && !nodeIds.Contains(node.nextId))
                {
                    throw new InvalidOperationException($"节点 '{node.nodeId}' 的 NextId '{node.nextId}' 不存在。");
                }
            }

            for (var i = 0; i < choiceGroups.Count; i++)
            {
                var group = choiceGroups[i];
                for (var j = 0; j < group.choices.Count; j++)
                {
                    var choice = group.choices[j];
                    if (!string.IsNullOrWhiteSpace(choice.nextId) && !nodeIds.Contains(choice.nextId))
                    {
                        throw new InvalidOperationException($"选项 '{choice.choiceId}' 的 NextId '{choice.nextId}' 不存在。");
                    }
                }
            }
        }

        private static DialogueGraphData CreateOutputGraph(DialogueCsvImportDefinition definition)
        {
            var definitionPath = AssetDatabase.GetAssetPath(definition);
            var definitionFolder = Path.GetDirectoryName(definitionPath)?.Replace("\\", "/");
            if (string.IsNullOrWhiteSpace(definitionFolder))
            {
                throw new InvalidOperationException("无法确定 DialogueCsvImportDefinition 的资源目录。");
            }

            var assetName = string.IsNullOrWhiteSpace(definition.GraphId)
                ? $"{definition.name}_Graph.asset"
                : $"{definition.GraphId}.asset";

            var outputPath = AssetDatabase.GenerateUniqueAssetPath($"{definitionFolder}/{assetName}");
            var graph = ScriptableObject.CreateInstance<DialogueGraphData>();
            AssetDatabase.CreateAsset(graph, outputPath);
            return graph;
        }

        private static string Required(Dictionary<string, string> row, string key, string rowName)
        {
            var value = Optional(row, key);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"{rowName} 缺少必填列 {key}。");
            }

            return value.Trim();
        }

        private static string Optional(Dictionary<string, string> row, string key, string fallback = "")
        {
            return row.TryGetValue(key, out var value) ? value?.Trim() ?? fallback : fallback;
        }

        private static int ParseInt(string rawValue, string fieldName, string rowName)
        {
            if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                throw new InvalidOperationException($"{rowName} 的 {fieldName} 不是合法整数：{rawValue}");
            }

            return result;
        }

        private static DialogueChoiceAvailabilityMode ParseUnavailableMode(string rawValue, string rowName)
        {
            if (Enum.TryParse(rawValue, ignoreCase: true, out DialogueChoiceAvailabilityMode mode))
            {
                return mode;
            }

            throw new InvalidOperationException($"{rowName} 的 UnavailableMode 非法：{rawValue}。允许值：Hidden / Disabled");
        }
    }
}
