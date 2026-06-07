// ----------------------------------------------------------------------------
// Script: FootstepSurface
// 作用：标记一块"地面区域"，玩家进入时切换脚步声 AudioCue。
//
// 使用方法：
//   1. 场景里建一个空 GameObject（命名建议 "Surface_WoodFloor" / "Surface_Tile" 等）
//   2. 加 BoxCollider2D / PolygonCollider2D，**勾选 Is Trigger**，覆盖该地面范围
//   3. 挂本组件，拖入对应的 AudioCue（如木地板脚步 cue）
//   4. 玩家走进 = 切换；走出 = 自动恢复 PlayerFootsteps 默认 cue
//
// 多区域叠加：用 stackPriority 决定优先级（数字大的覆盖小的，例如脚下叠着两块时取最高的）。
// ----------------------------------------------------------------------------
using SeeAPsychologist.Audio;
using SeeAPsychologist.Player;
using UnityEngine;

namespace SeeAPsychologist.Audio
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class FootstepSurface : MonoBehaviour
    {
        [Tooltip("玩家踩到这块地面时使用的脚步声 AudioCue。")]
        [SerializeField] private AudioCue surfaceCue;

        [Tooltip("叠加区域时的优先级。数字大的覆盖数字小的。")]
        [SerializeField] private int stackPriority = 0;

        [Tooltip("玩家 Tag。")]
        [SerializeField] private string playerTag = "Player";

        public AudioCue Cue => surfaceCue;
        public int StackPriority => stackPriority;

        private void Reset()
        {
            // 自动把 Collider 设为 Trigger（防止挡住玩家）
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(playerTag)) return;
            var pf = other.GetComponent<PlayerFootsteps>();
            if (pf != null) FootstepSurfaceTracker.Get(pf).Push(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag(playerTag)) return;
            var pf = other.GetComponent<PlayerFootsteps>();
            if (pf != null) FootstepSurfaceTracker.Get(pf).Pop(this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var col = GetComponent<Collider2D>();
            if (col == null) return;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.25f);
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
#endif
    }
}
