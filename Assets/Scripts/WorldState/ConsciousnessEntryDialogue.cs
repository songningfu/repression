// ----------------------------------------------------------------------------
// Script: ConsciousnessEntryDialogue
// 作用：从现实世界床交互进入意识世界后，在转场结束自动播放入场对话（含立绘）。
// 使用方法：挂到 Consciousness 场景任意常驻物体上，绑定 DialogueRunner 与入口 NodeId。
// ----------------------------------------------------------------------------
using System.Collections;
using SeeAPsychologist.Dialogue;
using UnityEngine;

namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// 意识世界入场导演：监听世界切换，在闭眼转场结束后启动对话。
    /// </summary>
    public sealed class ConsciousnessEntryDialogue : MonoBehaviour
    {
        [Header("对话")]
        [SerializeField] private DialogueRunner runner;
        [SerializeField] private string entryNodeId = "15000";

        [Header("落点")]
        [Tooltip("进入意识世界后把玩家放到该 SpawnPoint（与 SceneSpawnPoint.spawnId 对应）。")]
        [SerializeField] private string spawnId = "FromRoom";

        [Tooltip("是否套用 SpawnPoint 上的玩家缩放。床切换进意识世界时建议关闭，避免角色突然变小。")]
        [SerializeField] private bool applySpawnScale;

        [Header("时序")]
        [SerializeField] private float delayAfterTransition = 0.35f;

        private bool _entryPlayed;

        private void Awake()
        {
            if (runner == null)
            {
                runner = FindObjectOfType<DialogueRunner>(true);
            }

            if (WorldStateManager.Instance != null)
            {
                WorldStateManager.Instance.OnWorldSwitched += HandleWorldSwitched;
            }
        }

        private void OnDestroy()
        {
            if (WorldStateManager.Instance != null)
            {
                WorldStateManager.Instance.OnWorldSwitched -= HandleWorldSwitched;
            }
        }

        private void HandleWorldSwitched(WorldSwitchedEvent evt)
        {
            if (_entryPlayed) return;
            if (evt.PreviousWorld != WorldType.Reality || evt.CurrentWorld != WorldType.Consciousness) return;

            StartCoroutine(PlayEntryRoutine());
        }

        private IEnumerator PlayEntryRoutine()
        {
            _entryPlayed = true;

            if (EyeBlinkTransition.Instance != null)
            {
                while (EyeBlinkTransition.Instance.IsTransitioning)
                {
                    yield return null;
                }
            }

            yield return new WaitForSeconds(delayAfterTransition);

            ApplySpawnPoint();
            TryStartDialogue();
        }

        private void ApplySpawnPoint()
        {
            var spawn = SceneSpawnPoint.Find(spawnId);
            if (spawn == null)
            {
                Debug.LogWarning($"[ConsciousnessEntry] 找不到 SpawnId='{spawnId}'，跳过落点校正。");
                return;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[ConsciousnessEntry] 找不到 Player，跳过落点校正。");
                return;
            }

            var rb = player.GetComponent<Rigidbody2D>();
            player.transform.position = spawn.Position;

            if (applySpawnScale && spawn.OverridePlayerScale)
            {
                player.transform.localScale = spawn.PlayerScale;
            }

            if (spawn.ResetPlayerFacing)
            {
                var sr = player.GetComponent<SpriteRenderer>();
                if (sr != null) sr.flipX = false;
            }

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            if (PlayerPositionManager.Instance != null)
            {
                PlayerPositionManager.Instance.SetSavedPosition(WorldType.Consciousness, spawn.Position);
            }
        }

        private void TryStartDialogue()
        {
            if (runner == null)
            {
                runner = FindObjectOfType<DialogueRunner>(true);
            }

            if (runner == null)
            {
                Debug.LogError("[ConsciousnessEntry] DialogueRunner 缺失，无法播放入场对话。");
                return;
            }

            if (!runner.StartDialogue(entryNodeId))
            {
                Debug.LogError($"[ConsciousnessEntry] 无法启动节点 {entryNodeId}，请检查 Act1_Consciousness 图资源。");
            }
        }
    }
}
