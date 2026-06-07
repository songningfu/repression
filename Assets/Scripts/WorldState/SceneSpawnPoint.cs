// ----------------------------------------------------------------------------
// Script: SceneSpawnPoint
// 作用：标记一个场景内的出生点。门切场景时，目标场景里相同 spawnId 的出生点
//       会被 PlayerPositionManager 用来定位玩家。
// 使用方法：在场景中创建空 GameObject，挂该脚本，填写一个唯一 ID（如 "FromCorridor"）。
// ----------------------------------------------------------------------------
using UnityEngine;
using System.Collections.Generic;

namespace SeeAPsychologist.WorldState
{
    [DisallowMultipleComponent]
    public sealed class SceneSpawnPoint : MonoBehaviour
    {
        [Tooltip("在当前场景内唯一的出生点 ID（与门那侧 SceneDoor.targetSpawnId 对应）。")]
        [SerializeField] private string spawnId;

        [Header("玩家覆盖（针对该场景的玩家适配）")]
        [Tooltip("勾上 = 切场景到此 SpawnPoint 后，强制把玩家 transform.localScale 设为下方的值。" +
                 "用于不同场景背景比例不同时，让玩家视觉大小适配。")]
        [SerializeField] private bool overridePlayerScale = false;

        [Tooltip("玩家在本场景应该的 localScale。一般 X/Y/Z 一致。")]
        [SerializeField] private Vector3 playerScale = new Vector3(0.35f, 0.35f, 0.35f);

        [Tooltip("勾上 = 强制把玩家朝向重置为面向右（取消 SpriteRenderer.flipX）。")]
        [SerializeField] private bool resetPlayerFacing = false;

        public string SpawnId => spawnId;
        public Vector3 Position => transform.position;
        public bool OverridePlayerScale => overridePlayerScale;
        public Vector3 PlayerScale => playerScale;
        public bool ResetPlayerFacing => resetPlayerFacing;

        private static readonly List<SceneSpawnPoint> _all = new();
        public static IReadOnlyList<SceneSpawnPoint> All => _all;

        private void OnEnable() { if (!_all.Contains(this)) _all.Add(this); }
        private void OnDisable() { _all.Remove(this); }

        public static SceneSpawnPoint Find(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i] != null && _all[i].spawnId == id) return _all[i];
            }
            return null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.6f);
        }
#endif
    }
}
