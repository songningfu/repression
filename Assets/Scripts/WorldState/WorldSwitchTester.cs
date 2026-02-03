using UnityEngine;
using SeeAPsychologist.MentalStats;

namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// 世界切换测试工具
    /// 用于在游戏运行时调整解离度，测试世界切换功能
    /// </summary>
    public class WorldSwitchTester : MonoBehaviour
    {
        [Header("快捷键设置")]
        [Tooltip("增加解离度的按键")]
        [SerializeField] private KeyCode increaseDissociationKey = KeyCode.PageUp;

        [Tooltip("减少解离度的按键")]
        [SerializeField] private KeyCode decreaseDissociationKey = KeyCode.PageDown;

        [Tooltip("设置解离度为 100 的按键")]
        [SerializeField] private KeyCode setMaxDissociationKey = KeyCode.Alpha9;

        [Tooltip("设置解离度为 0 的按键")]
        [SerializeField] private KeyCode setMinDissociationKey = KeyCode.Alpha0;

        [Header("显示信息")]
        [Tooltip("是否在屏幕上显示当前状态")]
        [SerializeField] private bool showOnScreenInfo = true;

        private void Update()
        {
            if (MentalStatsManager.Instance == null) return;

            // 增加解离度（通过增加压力到 100，然后等待自动增加）
            if (Input.GetKeyDown(increaseDissociationKey))
            {
                // 直接修改压力到 100，触发解离度自动增加
                int currentStress = MentalStatsManager.Instance.CurrentStress;
                int delta = 100 - currentStress;
                if (delta > 0)
                {
                    MentalStatsManager.Instance.ModifyStress(delta, MentalStatChangeSource.DebugUI, "WorldSwitchTester");
                }
                Debug.Log($"[WorldSwitchTester] 设置压力为 100，解离度将自动增加");
            }

            // 减少解离度（通过减少压力到 0，然后等待自动减少）
            if (Input.GetKeyDown(decreaseDissociationKey))
            {
                // 直接修改压力到 0，触发解离度自动减少
                int currentStress = MentalStatsManager.Instance.CurrentStress;
                int delta = -currentStress;
                if (delta < 0)
                {
                    MentalStatsManager.Instance.ModifyStress(delta, MentalStatChangeSource.DebugUI, "WorldSwitchTester");
                }
                Debug.Log($"[WorldSwitchTester] 设置压力为 0，解离度将自动减少");
            }

            // 快速设置解离度为 100
            if (Input.GetKeyDown(setMaxDissociationKey))
            {
                // 先设置压力为 100
                int currentStress = MentalStatsManager.Instance.CurrentStress;
                MentalStatsManager.Instance.ModifyStress(100 - currentStress, MentalStatChangeSource.DebugUI, "WorldSwitchTester:Max");
                Debug.Log($"[WorldSwitchTester] 设置压力为 100（解离度将快速增加到 100）");
            }

            // 快速设置解离度为 0
            if (Input.GetKeyDown(setMinDissociationKey))
            {
                // 先设置压力为 0
                int currentStress = MentalStatsManager.Instance.CurrentStress;
                MentalStatsManager.Instance.ModifyStress(-currentStress, MentalStatChangeSource.DebugUI, "WorldSwitchTester:Min");
                Debug.Log($"[WorldSwitchTester] 设置压力为 0（解离度将快速减少到 0）");
            }
        }

        private void OnGUI()
        {
            if (!showOnScreenInfo) return;
            if (MentalStatsManager.Instance == null) return;
            if (WorldStateManager.Instance == null) return;

            int stress = MentalStatsManager.Instance.CurrentStress;
            int dissociation = MentalStatsManager.Instance.CurrentDissociation;
            WorldType currentWorld = WorldStateManager.Instance.CurrentWorld;

            // 显示当前状态
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 16;
            style.alignment = TextAnchor.UpperLeft;
            style.normal.textColor = Color.white;

            string info = $"当前世界: {currentWorld}\n" +
                         $"压力值: {stress}\n" +
                         $"解离度: {dissociation}\n\n" +
                         $"快捷键:\n" +
                         $"[{increaseDissociationKey}] 增加解离度\n" +
                         $"[{decreaseDissociationKey}] 减少解离度\n" +
                         $"[{setMaxDissociationKey}] 解离度 → 100\n" +
                         $"[{setMinDissociationKey}] 解离度 → 0\n\n" +
                         $"切换条件:\n" +
                         $"进入现实世界: 解离度 ≤ 10\n" +
                         $"进入意识世界: 解离度 ≥ 90";

            GUI.Box(new Rect(10, 10, 300, 250), info, style);

            // 显示是否可以切换
            string canSwitch = "";
            if (currentWorld == WorldType.Reality)
            {
                if (dissociation >= 90)
                {
                    canSwitch = "✓ 可以切换到意识世界";
                    GUI.contentColor = Color.green;
                }
                else
                {
                    canSwitch = $"✗ 需要解离度 ≥ 90 (当前 {dissociation})";
                    GUI.contentColor = Color.red;
                }
            }
            else
            {
                if (dissociation <= 10)
                {
                    canSwitch = "✓ 可以切换到现实世界";
                    GUI.contentColor = Color.green;
                }
                else
                {
                    canSwitch = $"✗ 需要解离度 ≤ 10 (当前 {dissociation})";
                    GUI.contentColor = Color.red;
                }
            }

            GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
            statusStyle.fontSize = 18;
            statusStyle.fontStyle = FontStyle.Bold;
            statusStyle.normal.textColor = GUI.contentColor;

            GUI.Label(new Rect(10, 270, 300, 30), canSwitch, statusStyle);
            GUI.contentColor = Color.white;
        }
    }
}

