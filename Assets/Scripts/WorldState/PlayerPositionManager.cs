using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// 玩家位置管理器
    /// 为每个世界分别保存和恢复玩家位置，确保玩家在原地转换而不是从天上掉下来
    /// </summary>
    public class PlayerPositionManager : MonoBehaviour
    {
        private static PlayerPositionManager _instance;
        public static PlayerPositionManager Instance => _instance;

        [Header("设置")]
        [Tooltip("玩家对象的标签")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("是否在切换世界时保存位置")]
        [SerializeField] private bool savePositionOnWorldSwitch = true;

        [Header("默认出生点")]
        [Tooltip("现实世界的默认出生点")]
        [SerializeField] private Vector3 realityDefaultSpawn = new Vector3(0, 0, 0);

        [Tooltip("意识世界的默认出生点")]
        [SerializeField] private Vector3 consciousnessDefaultSpawn = new Vector3(0, 0, 0);

        // 为每个世界分别保存位置
        private Dictionary<WorldType, Vector3> savedPositions = new Dictionary<WorldType, Vector3>();
        private bool isRestoringPosition = false;
        private WorldType targetWorld;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // 订阅场景加载事件
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // 取消订阅世界切换事件
            if (WorldStateManager.Instance != null)
            {
                WorldStateManager.Instance.OnWorldSwitched -= OnWorldSwitched;
            }
        }

        private void Start()
        {
            // 订阅世界切换事件
            if (WorldStateManager.Instance != null)
            {
                WorldStateManager.Instance.OnWorldSwitched += OnWorldSwitched;
            }
        }

        /// <summary>
        /// 世界切换前保存玩家位置（保存到当前世界）
        /// </summary>
        public void SavePlayerPosition()
        {
            if (!savePositionOnWorldSwitch) return;

            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                var currentWorld = WorldStateManager.Instance != null 
                    ? WorldStateManager.Instance.CurrentWorld 
                    : WorldType.Reality;

                savedPositions[currentWorld] = player.transform.position;
                Debug.Log($"[PlayerPositionManager] 保存 {currentWorld} 世界玩家位置: {player.transform.position}");
            }
            else
            {
                Debug.LogWarning($"[PlayerPositionManager] 找不到标签为 '{playerTag}' 的玩家对象");
            }
        }

        /// <summary>
        /// 保存玩家位置到指定世界
        /// </summary>
        public void SavePlayerPositionForWorld(WorldType world)
        {
            if (!savePositionOnWorldSwitch) return;

            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                savedPositions[world] = player.transform.position;
                Debug.Log($"[PlayerPositionManager] 保存 {world} 世界玩家位置: {player.transform.position}");
            }
        }

        /// <summary>
        /// 世界切换事件处理
        /// </summary>
        private void OnWorldSwitched(WorldSwitchedEvent evt)
        {
            // 记录目标世界，在场景加载后恢复对应世界的位置
            targetWorld = evt.CurrentWorld;
            isRestoringPosition = true;
        }

        /// <summary>
        /// 场景加载完成后恢复玩家位置
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (isRestoringPosition)
            {
                // 延迟一帧恢复位置，确保玩家对象已经初始化
                StartCoroutine(RestorePlayerPositionDelayed());
            }
        }

        /// <summary>
        /// 延迟恢复玩家位置
        /// </summary>
        private System.Collections.IEnumerator RestorePlayerPositionDelayed()
        {
            // 等待更多帧，确保所有对象都初始化完成
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.1f);

            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                Vector3 positionToRestore;

                // 检查是否有该世界的保存位置
                if (savedPositions.ContainsKey(targetWorld))
                {
                    positionToRestore = savedPositions[targetWorld];
                    Debug.Log($"[PlayerPositionManager] 恢复 {targetWorld} 世界玩家位置: {positionToRestore}");
                }
                else
                {
                    // 使用默认出生点
                    positionToRestore = GetDefaultSpawnPoint(targetWorld);
                    Debug.Log($"[PlayerPositionManager] 使用 {targetWorld} 世界默认出生点: {positionToRestore}");
                }

                // 获取 Rigidbody2D
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

                // 第一次设置：立即传送
                player.transform.position = positionToRestore;
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                    // 暂时禁用重力，防止掉落
                    float originalGravity = rb.gravityScale;
                    rb.gravityScale = 0;
                }

                // 等待物理更新
                yield return new WaitForFixedUpdate();

                // 第二次设置：确保位置
                player.transform.position = positionToRestore;
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }

                // 再等待一帧
                yield return null;

                // 第三次设置：最终确认
                player.transform.position = positionToRestore;
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                    // 恢复重力
                    rb.gravityScale = 1;
                }

                Debug.Log($"[PlayerPositionManager] 最终确认玩家位置: {positionToRestore}");
            }
            else
            {
                Debug.LogWarning($"[PlayerPositionManager] 场景加载后找不到标签为 '{playerTag}' 的玩家对象");
            }

            isRestoringPosition = false;
        }

        /// <summary>
        /// 获取指定世界的默认出生点
        /// </summary>
        private Vector3 GetDefaultSpawnPoint(WorldType world)
        {
            return world == WorldType.Reality ? realityDefaultSpawn : consciousnessDefaultSpawn;
        }

        /// <summary>
        /// 手动设置指定世界的保存位置
        /// </summary>
        public void SetSavedPosition(WorldType world, Vector3 position)
        {
            savedPositions[world] = position;
            Debug.Log($"[PlayerPositionManager] 手动设置 {world} 世界位置: {position}");
        }

        /// <summary>
        /// 获取指定世界的保存位置
        /// </summary>
        public Vector3 GetSavedPosition(WorldType world)
        {
            if (savedPositions.ContainsKey(world))
            {
                return savedPositions[world];
            }
            return GetDefaultSpawnPoint(world);
        }

        /// <summary>
        /// 清除指定世界的保存位置
        /// </summary>
        public void ClearSavedPosition(WorldType world)
        {
            if (savedPositions.ContainsKey(world))
            {
                savedPositions.Remove(world);
                Debug.Log($"[PlayerPositionManager] 清除 {world} 世界的保存位置");
            }
        }

        /// <summary>
        /// 清除所有保存的位置
        /// </summary>
        public void ClearAllSavedPositions()
        {
            savedPositions.Clear();
            Debug.Log($"[PlayerPositionManager] 清除所有世界的保存位置");
        }

        /// <summary>
        /// 设置默认出生点
        /// </summary>
        public void SetDefaultSpawnPoint(WorldType world, Vector3 position)
        {
            if (world == WorldType.Reality)
            {
                realityDefaultSpawn = position;
            }
            else
            {
                consciousnessDefaultSpawn = position;
            }
            Debug.Log($"[PlayerPositionManager] 设置 {world} 世界默认出生点: {position}");
        }
    }
}

