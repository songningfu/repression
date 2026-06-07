// ----------------------------------------------------------------------------
// Script: SceneDoor
// 作用：场景门（同世界场景切换），配合 InteractableObject + UnityEvent 使用。
//
// 与旧 IntraWorldDoor 的区别：
//   - 不自己处理 F 键 / 提示显隐 / Trigger 范围（这些由 InteractableObject 接管）
//   - 只暴露一个公开方法 Enter()，作为 UnityEvent 的回调目标
//
// 使用方法：
//   1. 给门挂 InteractableObject + SceneDoor
//   2. SceneDoor 配置 targetSceneName + targetSpawnId
//   3. InteractableObject.OnInteract → SceneDoor.Enter()
//   4. 目标场景里放一个 SceneSpawnPoint（spawnId 与本组件 targetSpawnId 对应）
//
// 跨场景流程：
//   按 F → InteractableObject 调 Enter() → 记录 pendingSpawnId → LoadScene
//   → 新场景加载后 → 全局钩子找到 SceneSpawnPoint → 把玩家放到正确位置
// ----------------------------------------------------------------------------
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeeAPsychologist.WorldState
{
    public sealed class SceneDoor : MonoBehaviour
    {
        [Header("目标")]
        [Tooltip("要加载的目标场景名（必须已加入 Build Settings）。")]
        [SerializeField] private string targetSceneName;

        [Tooltip("目标场景里 SceneSpawnPoint 的 spawnId。")]
        [SerializeField] private string targetSpawnId;

        [Header("安全")]
        [Tooltip("仅允许同世界场景切换（防止误把跨世界做成普通门）。跨世界请用床/药物。")]
        [SerializeField] private bool restrictToSameWorld = true;

        // 跨场景传递：spawnId
        private static string _pendingSpawnId;

        /// <summary>
        /// UnityEvent 友好的入口。挂到 InteractableObject.OnInteract 上即可。
        /// </summary>
        public void Enter()
        {
            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                Debug.LogError($"[SceneDoor] {name}: targetSceneName 为空。", this);
                return;
            }

            // 同世界限制
            if (restrictToSameWorld &&
                WorldStateManager.Instance != null &&
                WorldStateManager.Instance.Config != null)
            {
                var cfg = WorldStateManager.Instance.Config;
                var currentWorld = WorldStateManager.Instance.CurrentWorld;
                bool sameWorld =
                    (currentWorld == WorldType.Reality && cfg.IsRealityScene(targetSceneName)) ||
                    (currentWorld == WorldType.Consciousness && cfg.IsConsciousnessScene(targetSceneName));

                if (!sameWorld)
                {
                    Debug.LogWarning(
                        $"[SceneDoor] '{targetSceneName}' 不属于当前世界 {currentWorld}。" +
                        "请把场景名加入 WorldStateConfig 的对应列表，或跨世界请用床交互。", this);
                    return;
                }
            }

            _pendingSpawnId = targetSpawnId;
            Debug.Log($"[SceneDoor] 切换到 '{targetSceneName}' (spawnId='{targetSpawnId}')");

            // 优先用 SceneFadeTransition 做黑屏过渡；不存在时直接加载
            if (SceneFadeTransition.Instance != null)
            {
                SceneFadeTransition.Instance.FadeToScene(targetSceneName);
            }
            else
            {
                SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
            }
        }

        // -- 跨场景 SpawnPoint 钩子（全局只注册一次） ---------------------------

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

            // 起协程等一帧让 SceneSpawnPoint 注册完
            var go = new GameObject("[SceneDoor] SpawnApplier");
            go.AddComponent<SpawnApplier>().Run(spawnIdCopy);
        }

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
                    Debug.LogWarning($"[SceneDoor] 找不到 SpawnId='{spawnId}' 的 SceneSpawnPoint。");
                    Destroy(gameObject);
                    yield break;
                }

                var player = GameObject.FindGameObjectWithTag("Player");
                if (player == null)
                {
                    Debug.LogWarning("[SceneDoor] 场景内没有 Tag=Player 的玩家。");
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

                // 场景级覆盖：让玩家在该场景的 scale / 朝向适配背景比例
                if (sp.OverridePlayerScale)
                {
                    player.transform.localScale = sp.PlayerScale;
                }
                if (sp.ResetPlayerFacing)
                {
                    var sr = player.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.flipX = false;
                }

                // 同步给 PlayerPositionManager（按世界存的位置一并刷新）
                if (WorldStateManager.Instance != null && PlayerPositionManager.Instance != null)
                {
                    PlayerPositionManager.Instance.SetSavedPosition(
                        WorldStateManager.Instance.CurrentWorld, sp.Position);
                }

                Debug.Log($"[SceneDoor] 玩家落点：SpawnId='{spawnId}' @ {sp.Position} scale={player.transform.localScale}");
                Destroy(gameObject);
            }
        }
    }
}
