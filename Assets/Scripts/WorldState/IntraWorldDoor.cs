// ----------------------------------------------------------------------------
// Script: IntraWorldDoor
// 作用：同一个世界内（如现实世界里：卧室 ↔ 走廊 ↔ 街道）的场景门。
//       - 玩家进入触发范围 → 显示按 F 提示
//       - 玩家按 F → 加载目标场景，并把玩家放到目标场景里指定 SpawnId 的 SceneSpawnPoint 上
//       注意：跨世界（如现实 ↔ 意识）请用 BedWorldSwitchInteractable，不要用本组件。
//
// 使用方法：
//   1. 挂到门的 GameObject 上，给门加 BoxCollider2D（Is Trigger = true）
//   2. 填 targetSceneName（必须已加入 Build Settings）
//   3. 填 targetSpawnId（对端场景里一个 SceneSpawnPoint 的 ID）
//   4. 在 promptRoot 里塞一个子物体（如 "按F"），平时会隐藏，玩家进入时显示
// ----------------------------------------------------------------------------
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeeAPsychologist.WorldState
{
    [DisallowMultipleComponent]
    public sealed class IntraWorldDoor : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("目标场景名（必须与 Build Settings 中的场景名一致）。")]
        [SerializeField] private string targetSceneName;

        [Tooltip("目标场景内 SceneSpawnPoint 的 spawnId。")]
        [SerializeField] private string targetSpawnId;

        [Header("Interaction")]
        [SerializeField] private KeyCode interactKey = KeyCode.F;
        [SerializeField] private string playerTag = "Player";

        [Header("Prompt (Optional)")]
        [Tooltip("按 F 提示物体（子物体），不填会按名字 \"按F\" / \"put F\" 自动查找。")]
        [SerializeField] private GameObject promptRoot;

        [Header("Safety")]
        [Tooltip("仅允许在同一世界内的场景之间使用（防止误用为跨世界切换）。")]
        [SerializeField] private bool restrictToSameWorld = true;

        private int _inRangeCount;
        private bool _consumed;

        // 跨场景传递：目标 SpawnId（场景加载完后由本类自身处理 → 让 PlayerPositionManager 知道去哪）
        private static string _pendingSpawnId;

        private void Awake()
        {
            if (promptRoot == null)
            {
                var t = FindChildByName(transform, "按F") ?? FindChildByName(transform, "put F");
                if (t != null) promptRoot = t.gameObject;
            }
            SetPromptVisible(false);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Update()
        {
            if (_inRangeCount <= 0 || _consumed) return;
            if (!Input.GetKeyDown(interactKey)) return;
            TryEnter();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;
            _inRangeCount++;
            SetPromptVisible(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;
            _inRangeCount = Mathf.Max(0, _inRangeCount - 1);
            if (_inRangeCount <= 0) SetPromptVisible(false);
        }

        private void TryEnter()
        {
            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                Debug.LogError("[IntraWorldDoor] targetSceneName is empty.", this);
                return;
            }

            if (restrictToSameWorld && WorldStateManager.Instance != null && WorldStateManager.Instance.Config != null)
            {
                var cfg = WorldStateManager.Instance.Config;
                var currentWorld = WorldStateManager.Instance.CurrentWorld;
                bool targetInSameWorld =
                    (currentWorld == WorldType.Reality && cfg.IsRealityScene(targetSceneName)) ||
                    (currentWorld == WorldType.Consciousness && cfg.IsConsciousnessScene(targetSceneName));

                if (!targetInSameWorld)
                {
                    Debug.LogWarning($"[IntraWorldDoor] '{targetSceneName}' 不属于当前世界 {currentWorld}，" +
                                     "跨世界请使用床/药物。已忽略本次按键。", this);
                    return;
                }
            }

            _consumed = true;
            SetPromptVisible(false);
            _pendingSpawnId = targetSpawnId;

            Debug.Log($"[IntraWorldDoor] 进入场景 '{targetSceneName}' → SpawnId='{targetSpawnId}'");
            SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 本物体一般是上一个场景的，加载新场景后会被销毁；用 static 字段做跨场景传递。
            // 这里没机会再跑。具体的"把玩家放到 SpawnPoint"逻辑由下方静态钩子完成。
        }

        // -- 跨场景钩子 ---------------------------------------------------------
        // 在新场景被加载后，把玩家放到对应 SpawnPoint。挂到 RuntimeInitialize 上保证全局只订阅一次。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterGlobalHook()
        {
            SceneManager.sceneLoaded -= GlobalOnSceneLoaded;
            SceneManager.sceneLoaded += GlobalOnSceneLoaded;
        }

        private static void GlobalOnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (string.IsNullOrEmpty(_pendingSpawnId)) return;
            var spawnIdCopy = _pendingSpawnId;
            _pendingSpawnId = null;

            var runner = new GameObject("[IntraWorldDoor] SpawnApplier").AddComponent<SpawnApplier>();
            runner.Run(spawnIdCopy);
        }

        // 协程 host：等一帧让 SceneSpawnPoint OnEnable 注册完，再把玩家放过去
        private sealed class SpawnApplier : MonoBehaviour
        {
            public void Run(string spawnId) => StartCoroutine(Apply(spawnId));

            private IEnumerator Apply(string spawnId)
            {
                yield return null;
                yield return new WaitForSeconds(0.05f);

                var sp = SceneSpawnPoint.Find(spawnId);
                if (sp == null)
                {
                    Debug.LogWarning($"[IntraWorldDoor] 找不到 SpawnId='{spawnId}' 的 SceneSpawnPoint，玩家位置未调整。");
                    Destroy(gameObject);
                    yield break;
                }

                var player = GameObject.FindGameObjectWithTag("Player");
                if (player == null)
                {
                    Debug.LogWarning("[IntraWorldDoor] 场景内没有 Tag=Player 的玩家对象。");
                    Destroy(gameObject);
                    yield break;
                }

                var rb = player.GetComponent<Rigidbody2D>();
                player.transform.position = sp.Position;
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
                Debug.Log($"[IntraWorldDoor] 玩家落点：SpawnId='{spawnId}' @ {sp.Position}");

                // 同步给 PlayerPositionManager（按世界存的位置一并刷新，避免再次切世界时被覆盖）
                if (WorldStateManager.Instance != null && PlayerPositionManager.Instance != null)
                {
                    PlayerPositionManager.Instance.SetSavedPosition(WorldStateManager.Instance.CurrentWorld, sp.Position);
                }

                Destroy(gameObject);
            }
        }

        // -- helpers -----------------------------------------------------------
        private bool IsPlayer(Collider2D other)
            => string.IsNullOrWhiteSpace(playerTag) || other.CompareTag(playerTag);

        private void SetPromptVisible(bool v)
        {
            if (promptRoot != null && promptRoot.activeSelf != v) promptRoot.SetActive(v);
        }

        private static Transform FindChildByName(Transform root, string name)
        {
            if (root == null) return null;
            for (var i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i);
                if (c.name == name) return c;
                var deep = FindChildByName(c, name);
                if (deep != null) return deep;
            }
            return null;
        }
    }
}
