// ----------------------------------------------------------------------------
// Script: DialogueCsvImportDefinitionEditor
// 作用：为 DialogueCsvImportDefinition 提供“一键生成/更新图资源”的 Inspector 按钮。
// 使用方法/调用示例：选中导入定义资源后，在 Inspector 点击按钮即可执行导入。
// ----------------------------------------------------------------------------
using UnityEditor;
using UnityEngine;

namespace SeeAPsychologist.Dialogue.Editor
{
    /// <summary>
    /// 对话 CSV 导入定义自定义 Inspector。
    /// </summary>
    [CustomEditor(typeof(DialogueCsvImportDefinition))]
    public sealed class DialogueCsvImportDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            var definition = (DialogueCsvImportDefinition)target;
            using (new EditorGUI.DisabledScope(definition == null))
            {
                if (GUILayout.Button("生成 / 更新 DialogueGraphData"))
                {
                    TryImport(definition);
                }
            }
        }

        private static void TryImport(DialogueCsvImportDefinition definition)
        {
            try
            {
                var graph = DialogueCsvImportService.Import(definition);
                EditorUtility.DisplayDialog(
                    "Dialogue Import",
                    $"导入完成。\n输出资源：{AssetDatabase.GetAssetPath(graph)}",
                    "确定");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Dialogue] Import failed: {ex}");
                EditorUtility.DisplayDialog(
                    "Dialogue Import Failed",
                    ex.Message,
                    "确定");
            }
        }
    }
}
