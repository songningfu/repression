using UnityEngine;
using UnityEditor;
using System.IO;

namespace SeeAPsychologist.Editor
{
    /// <summary>
    /// 项目清理工具 - 删除不需要的文档和一次性工具
    /// </summary>
    public static class ProjectCleaner
    {
        [MenuItem("Tools/Clean Project/显示清理计划")]
        public static void ShowCleanupPlan()
        {
            string planPath = "Assets/Scripts/CLEANUP_PLAN.md";
            if (File.Exists(planPath))
            {
                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(planPath, 1);
            }
            else
            {
                Debug.LogWarning("[ProjectCleaner] 找不到清理计划文件");
            }
        }

        [MenuItem("Tools/Clean Project/删除重复文档")]
        public static void CleanupDocuments()
        {
            string[] docsToDelete = new string[]
            {
                "Assets/Scripts/WorldState/README_FixedUI.md",
                "Assets/Scripts/WorldState/README_FixedUI.md.meta",
                "Assets/Scripts/WorldState/README_Setup_Interactions.md",
                "Assets/Scripts/WorldState/README_Setup_Interactions.md.meta",
                "Assets/Scripts/WorldState/README_StressInteraction.md",
                "Assets/Scripts/WorldState/README_StressInteraction.md.meta",
                "Assets/Scripts/WorldState/SETUP_GUIDE.md",
                "Assets/Scripts/WorldState/SETUP_GUIDE.md.meta"
            };

            int deletedCount = 0;
            foreach (string path in docsToDelete)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    deletedCount++;
                    Debug.Log($"[ProjectCleaner] 已删除: {path}");
                }
            }

            if (deletedCount > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[ProjectCleaner] ✅ 已删除 {deletedCount} 个重复文档文件");
            }
            else
            {
                Debug.Log("[ProjectCleaner] ℹ️ 没有找到需要删除的文档");
            }
        }

        [MenuItem("Tools/Clean Project/删除一次性工具")]
        public static void CleanupEditorTools()
        {
            string[] toolsToDelete = new string[]
            {
                "Assets/Editor/FixInteractionUIPosition.cs",
                "Assets/Editor/FixInteractionUIPosition.cs.meta",
                "Assets/Editor/StressInteractionCreator.cs",
                "Assets/Editor/StressInteractionCreator.cs.meta"
            };

            int deletedCount = 0;
            foreach (string path in toolsToDelete)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    deletedCount++;
                    Debug.Log($"[ProjectCleaner] 已删除: {path}");
                }
            }

            if (deletedCount > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[ProjectCleaner] ✅ 已删除 {deletedCount} 个工具文件");
            }
            else
            {
                Debug.Log("[ProjectCleaner] ℹ️ 没有找到需要删除的工具");
            }
        }

        [MenuItem("Tools/Clean Project/删除可选功能（慎重）")]
        public static void CleanupOptionalFeatures()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "确认删除",
                "这将删除以下可选功能：\n" +
                "- WorldAlertFollower（UI跟随）\n" +
                "- FixedInteractionUI（固定UI）\n\n" +
                "确定要删除吗？",
                "确定",
                "取消"
            );

            if (!confirmed) return;

            string[] optionalFiles = new string[]
            {
                "Assets/Scripts/WorldState/WorldAlertFollower.cs",
                "Assets/Scripts/WorldState/WorldAlertFollower.cs.meta",
                "Assets/Scripts/WorldState/FixedInteractionUI.cs",
                "Assets/Scripts/WorldState/FixedInteractionUI.cs.meta"
            };

            int deletedCount = 0;
            foreach (string path in optionalFiles)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    deletedCount++;
                    Debug.Log($"[ProjectCleaner] 已删除: {path}");
                }
            }

            if (deletedCount > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[ProjectCleaner] ✅ 已删除 {deletedCount} 个可选功能文件");
            }
            else
            {
                Debug.Log("[ProjectCleaner] ℹ️ 没有找到需要删除的可选功能");
            }
        }

        [MenuItem("Tools/Clean Project/执行完整清理")]
        public static void ExecuteFullCleanup()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "确认完整清理",
                "这将删除：\n" +
                "1. 所有重复的文档文件\n" +
                "2. 所有一次性工具\n" +
                "3. 可选功能（WorldAlertFollower、FixedInteractionUI）\n\n" +
                "确定要继续吗？",
                "确定",
                "取消"
            );

            if (!confirmed) return;

            Debug.Log("[ProjectCleaner] 开始完整清理...");
            
            CleanupDocuments();
            CleanupEditorTools();
            CleanupOptionalFeatures();
            
            Debug.Log("[ProjectCleaner] ✅ 完整清理完成！");
        }

        [MenuItem("Tools/Clean Project/显示项目统计")]
        public static void ShowProjectStats()
        {
            int csFiles = Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories).Length;
            int mdFiles = Directory.GetFiles("Assets/Scripts", "*.md", SearchOption.AllDirectories).Length;
            int editorFiles = Directory.Exists("Assets/Editor") ? 
                Directory.GetFiles("Assets/Editor", "*.cs", SearchOption.AllDirectories).Length : 0;

            Debug.Log("========== 项目统计 ==========");
            Debug.Log($"C# 脚本文件: {csFiles}");
            Debug.Log($"文档文件: {mdFiles}");
            Debug.Log($"编辑器工具: {editorFiles}");
            Debug.Log("==============================");
        }
    }
}

