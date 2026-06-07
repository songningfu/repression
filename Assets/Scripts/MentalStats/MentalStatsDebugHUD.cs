// ----------------------------------------------------------------------------
// Script: MentalStatsDebugHUD
// 作用：屏幕左上角实时显示 Stress / Dissociation / 当前世界。开发调试用。
//
// 优点：
//   - 不依赖任何 Canvas/UI 元素
//   - 用 IMGUI 直接画，挂上就用
//   - 自动单例 + DontDestroyOnLoad，跨场景一直可见
//
// 使用方法：
//   方法 1（推荐）：什么都不做，已经自动 Bootstrap，启动就显示
//   方法 2：场景里建空对象挂本组件 + 配字段
//
// 发布前关掉：把 enabled 设 false，或者勾掉 Show On Start，或删掉组件
// ----------------------------------------------------------------------------
using SeeAPsychologist.WorldState;
using UnityEngine;

namespace SeeAPsychologist.MentalStats
{
    public sealed class MentalStatsDebugHUD : MonoBehaviour
    {
        public static MentalStatsDebugHUD Instance { get; private set; }

        [Header("显示")]
        [Tooltip("勾上 = 启动时自动显示。可在游戏内按 Toggle Key 切换。")]
        [SerializeField] private bool showOnStart = true;

        [Tooltip("切换显示/隐藏的快捷键。")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;

        [Header("外观")]
        [SerializeField] private int fontSize = 16;
        [SerializeField] private Color textColor = new Color(1f, 1f, 1f, 0.95f);
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);

        [Tooltip("左上角偏移。")]
        [SerializeField] private Vector2 positionOffset = new Vector2(12f, 12f);

        private bool _visible = true;
        private GUIStyle _labelStyle;
        private Texture2D _bgTexture;

        // -- Bootstrap (自动创建，不用场景里挂) -------------------------------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("MentalStatsDebugHUD");
            go.AddComponent<MentalStatsDebugHUD>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _visible = showOnStart;
            CreateBgTexture();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_bgTexture != null) Destroy(_bgTexture);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible) return;

            EnsureStyle();

            var mgr = MentalStatsManager.Instance;
            var worldMgr = WorldStateManager.Instance;

            string text;
            if (mgr == null)
            {
                text = "MentalStatsManager: <NULL>";
            }
            else
            {
                int s = mgr.CurrentStress;
                int d = mgr.CurrentDissociation;
                string world = worldMgr != null ? worldMgr.CurrentWorld.ToString() : "?";
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

                text = $"<b>HUD</b>  (F1 切换)\n" +
                       $"World:     {world}\n" +
                       $"Scene:     {sceneName}\n" +
                       $"Stress:        {Bar(s)} {s,3}\n" +
                       $"Dissociation:  {Bar(d)} {d,3}";
            }

            var content = new GUIContent(text);
            var size = _labelStyle.CalcSize(content);
            size.x += 24f; size.y += 12f;

            var rect = new Rect(positionOffset.x, positionOffset.y, size.x, size.y);
            GUI.DrawTexture(rect, _bgTexture, ScaleMode.StretchToFill);
            GUI.Label(rect, content, _labelStyle);
        }

        private static string Bar(int value)
        {
            int filled = Mathf.Clamp(value / 10, 0, 10);
            return "[" + new string('|', filled) + new string('.', 10 - filled) + "]";
        }

        private void EnsureStyle()
        {
            if (_labelStyle != null) return;
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                richText = true,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(10, 10, 6, 6)
            };
            _labelStyle.normal.textColor = textColor;
        }

        private void CreateBgTexture()
        {
            _bgTexture = new Texture2D(1, 1);
            _bgTexture.SetPixel(0, 0, backgroundColor);
            _bgTexture.Apply();
        }
    }
}
