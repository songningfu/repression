using TMPro;
using UnityEngine.EventSystems;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SeeAPsychologist.MentalStats
{
    /// <summary>
    /// 调试 UI 绑定器（用于你现有的四个按钮与两个 TMP 显示）。
    ///
    /// 用法（不需要改场景结构）：
    /// - 把这个脚本挂到 Canvas 或任意可激活对象上
    /// - 勾选 autoFindReferences=true 时，会按名字自动查找：
    ///   - Buttons: "+1", "+10", "-1", "-10"
    ///   - TMP_Text: "应激度", "解离度"
    /// - 也可在 Inspector 手动拖引用（更稳）
    /// </summary>
    public sealed class MentalStatsDebugUIBinder : MonoBehaviour
    {
        [Header("Manager")]
        [SerializeField] private MentalStatsManager manager;
        [SerializeField] private MentalStatsConfig configForAutoCreate;
        [SerializeField] private bool autoCreateManagerIfMissing = true;

        [Header("Auto Find (by GameObject name)")]
        [SerializeField] private bool autoFindReferences = true;

        [Header("Buttons")]
        [SerializeField] private Button plus1;
        [SerializeField] private Button plus10;
        [SerializeField] private Button minus1;
        [SerializeField] private Button minus10;

        [Header("TMP Displays (value text)")]
        [SerializeField] private TMP_Text stressText;
        [SerializeField] private TMP_Text dissociationText;

        private void Awake()
        {
            if (autoFindReferences)
            {
                plus1 ??= FindComponentByName<Button>("+1");
                plus10 ??= FindComponentByName<Button>("+10");
                minus1 ??= FindComponentByName<Button>("-1");
                minus10 ??= FindComponentByName<Button>("-10");

                stressText ??= FindComponentByName<TMP_Text>("应激度");
                dissociationText ??= FindComponentByName<TMP_Text>("解离度");
            }

            if (manager == null)
            {
                manager = MentalStatsManager.Instance;
            }

            if (manager == null && autoCreateManagerIfMissing)
            {
                var go = new GameObject("MentalStatsManager");
                manager = go.AddComponent<MentalStatsManager>();

                // 允许用户在 Inspector 提供一个配置资产；不提供则用 Manager 内部默认值。
                if (configForAutoCreate != null)
                {
                    // 运行时注入：避免 UnityEditor.SerializedObject（运行时不可用）
                    manager.SetConfig(configForAutoCreate, resetToInitialValues: true, source: MentalStatChangeSource.DebugUI);
                }
            }

            WireButtons();
        }

        private void OnEnable()
        {
            if (manager != null)
            {
                manager.OnStatsChanged += HandleStatsChanged;
                RefreshTexts(manager.Snapshot);
            }

            // Unity UI 默认把 Space/Enter 作为 Submit，会触发“当前选中按钮”的 onClick。
            // 这里清空选中，避免出现“空格重复触发某个按钮（如 +10）”的误行为。
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private void OnDisable()
        {
            if (manager != null)
            {
                manager.OnStatsChanged -= HandleStatsChanged;
            }
        }

        private void WireButtons()
        {
            BindButton(plus1, +1);
            BindButton(plus10, +10);
            BindButton(minus1, -1);
            BindButton(minus10, -10);
        }

        private void BindButton(Button button, int delta)
        {
            if (button == null) return;
            button.onClick.AddListener(() =>
            {
                if (manager == null)
                {
                    Debug.LogWarning("[MentalStats] MentalStatsManager is missing; click ignored.");
                    return;
                }

                manager.ModifyStress(delta, MentalStatChangeSource.DebugUI, context: $"Button({delta:+#;-#;0})");

                // 点击后取消选中，避免 Space(Submit) 再次触发同一个按钮。
                EventSystem.current?.SetSelectedGameObject(null);
            });
        }

        private void HandleStatsChanged(MentalStatsChangedEvent evt)
        {
            RefreshTexts(evt.Current);
        }

        private void RefreshTexts(MentalStatsState state)
        {
            if (stressText != null) stressText.text = state.Stress.ToString();
            if (dissociationText != null) dissociationText.text = state.Dissociation.ToString();
        }

        private T FindComponentByName<T>(string objectName) where T : Component
        {
            // 包含 inactive，避免层级折叠/禁用导致找不到
            var transforms = GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name == objectName)
                {
                    return transforms[i].GetComponent<T>();
                }
            }

            return null;
        }
    }
}

