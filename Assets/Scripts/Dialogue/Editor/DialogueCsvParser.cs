// ----------------------------------------------------------------------------
// Script: DialogueCsvParser
// 作用：提供对话 CSV 的轻量解析能力，支持带引号字段与逗号转义。
// 使用方法/调用示例：仅供编辑器导入工具调用，解析 Nodes/Choices 两张 TextAsset。
// ----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SeeAPsychologist.Dialogue.Editor
{
    /// <summary>
    /// 简易 CSV 解析器。
    /// </summary>
    internal static class DialogueCsvParser
    {
        internal static List<Dictionary<string, string>> ParseTable(TextAsset csvAsset, string label)
        {
            if (csvAsset == null)
            {
                throw new ArgumentNullException(nameof(csvAsset), $"{label} csv is null.");
            }

            var rows = ParseLines(csvAsset.text);
            if (rows.Count == 0)
            {
                throw new InvalidOperationException($"{label} csv is empty.");
            }

            var headers = rows[0];
            if (headers.Count == 0)
            {
                throw new InvalidOperationException($"{label} csv header row is empty.");
            }

            var result = new List<Dictionary<string, string>>();
            for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                if (IsRowEmpty(row))
                {
                    continue;
                }

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
                {
                    var header = headers[columnIndex]?.Trim();
                    if (string.IsNullOrWhiteSpace(header))
                    {
                        continue;
                    }

                    var value = columnIndex < row.Count ? row[columnIndex] : string.Empty;
                    dict[header] = value?.Trim() ?? string.Empty;
                }

                result.Add(dict);
            }

            return result;
        }

        private static List<List<string>> ParseLines(string text)
        {
            var rows = new List<List<string>>();
            var currentRow = new List<string>();
            var currentField = new StringBuilder();
            var insideQuotes = false;

            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];

                if (insideQuotes)
                {
                    if (ch == '"')
                    {
                        var nextIsQuote = i + 1 < text.Length && text[i + 1] == '"';
                        if (nextIsQuote)
                        {
                            currentField.Append('"');
                            i++;
                        }
                        else
                        {
                            insideQuotes = false;
                        }
                    }
                    else
                    {
                        currentField.Append(ch);
                    }

                    continue;
                }

                switch (ch)
                {
                    case '"':
                        insideQuotes = true;
                        break;

                    case ',':
                        currentRow.Add(currentField.ToString());
                        currentField.Clear();
                        break;

                    case '\r':
                        break;

                    case '\n':
                        currentRow.Add(currentField.ToString());
                        currentField.Clear();
                        rows.Add(currentRow);
                        currentRow = new List<string>();
                        break;

                    default:
                        currentField.Append(ch);
                        break;
                }
            }

            if (insideQuotes)
            {
                Debug.LogWarning("[Dialogue] CSV ended while still inside quotes. Parsed result may be incomplete.");
            }

            currentRow.Add(currentField.ToString());
            if (!IsRowEmpty(currentRow))
            {
                rows.Add(currentRow);
            }

            return rows;
        }

        private static bool IsRowEmpty(List<string> row)
        {
            for (var i = 0; i < row.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(row[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
