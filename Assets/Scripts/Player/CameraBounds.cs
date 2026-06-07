// ----------------------------------------------------------------------------
// Script: CameraBounds
// 作用：定义当前场景的相机边界（地图边界），让 CameraFollow2D 自动读取。
//
// 使用方法：
//   1. 场景里建空 GameObject → 命名 "CameraBounds"
//   2. 挂本组件，调四个边界值（Min/Max X/Y）
//   3. CameraFollow2D 启动时会自动找到本组件并用它的边界覆盖 Inspector 配置
//
// 优点：
//   - 每个场景独立配置边界
//   - Main Camera 可以做成 Prefab 全场景复用（不需要每个场景改 CameraFollow2D 字段）
//   - Scene 视图有青色 Gizmo 框，直观调边界
// ----------------------------------------------------------------------------
using UnityEngine;

namespace SeeAPsychologist.Player
{
    [DisallowMultipleComponent]
    public sealed class CameraBounds : MonoBehaviour
    {
        [Header("地图边界（世界坐标）")]
        [Tooltip("地图左边 X")]
        public float minX = -10f;

        [Tooltip("地图右边 X")]
        public float maxX = 10f;

        [Tooltip("地图底部 Y")]
        public float minY = -5f;

        [Tooltip("地图顶部 Y")]
        public float maxY = 5f;

        public Rect GetRect()
        {
            return Rect.MinMaxRect(
                Mathf.Min(minX, maxX),
                Mathf.Min(minY, maxY),
                Mathf.Max(minX, maxX),
                Mathf.Max(minY, maxY)
            );
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
            var bl = new Vector3(minX, minY, 0);
            var br = new Vector3(maxX, minY, 0);
            var tr = new Vector3(maxX, maxY, 0);
            var tl = new Vector3(minX, maxY, 0);
            Gizmos.DrawLine(bl, br);
            Gizmos.DrawLine(br, tr);
            Gizmos.DrawLine(tr, tl);
            Gizmos.DrawLine(tl, bl);
        }
#endif
    }
}
