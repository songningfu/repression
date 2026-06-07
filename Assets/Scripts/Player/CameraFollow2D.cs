// ----------------------------------------------------------------------------
// Script: CameraFollow2D
// 作用：2D 相机跟随 + 地图边界限制（防止相机超出地图露出黑边）。
//
// 工作原理：
//   - LateUpdate 把相机中心同步到 target.position + offset
//   - 根据相机视野（orthographicSize + aspect）计算视野半宽/半高
//   - 把相机中心 Clamp 在 [min + 半宽, max - 半宽] 之间
//     → 相机边缘永远不会超出地图边界，自然就不会出现黑墙
//
// 使用方法：
//   1. 挂在 Main Camera 上
//   2. Target 拖入 player（不填会自动按 Tag=Player 查找）
//   3. 设置 Map Bounds（Min/Max X/Y 是地图四角的世界坐标）
//   4. 在 Scene 视图能看到青色的地图框 + 黄色的"相机中心允许范围"框
// ----------------------------------------------------------------------------
using UnityEngine;

namespace SeeAPsychologist.Player
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [Header("跟随目标")]
        [Tooltip("通常是 player。不填则自动按 Tag=Player 查找。")]
        [SerializeField] private Transform target;

        [Tooltip("相对目标的偏移（例如 Y=1 让相机略偏上）。")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 0.5f);

        [Header("跟随手感")]
        [Tooltip("勾选 = 平滑跟随；不勾 = 紧贴目标。")]
        [SerializeField] private bool smoothFollow = true;

        [Tooltip("平滑速度（数值越大越紧跟，5~15 比较自然）。")]
        [SerializeField, Range(1f, 30f)] private float smoothSpeed = 8f;

        [Header("地图边界（世界坐标）")]
        [Tooltip("开启后，相机不会超出此范围。")]
        [SerializeField] private bool useBounds = true;

        [Tooltip("地图左边 X 世界坐标。")]
        [SerializeField] private float minX = -10f;

        [Tooltip("地图右边 X 世界坐标。")]
        [SerializeField] private float maxX = 10f;

        [Tooltip("地图底部 Y 世界坐标。")]
        [SerializeField] private float minY = -5f;

        [Tooltip("地图顶部 Y 世界坐标。")]
        [SerializeField] private float maxY = 5f;

        private Camera _cam;
        private CameraBounds _sceneBounds;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            RefreshSceneBounds();
            EnsureTarget();
            // 第一帧立即把相机放到位（避免开场有一个滑动）
            if (target != null)
            {
                transform.position = ClampToBounds(GetTargetPosition());
            }
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // 切场景后重新找 player 和 CameraBounds
            target = null;
            RefreshSceneBounds();
            EnsureTarget();
            if (target != null) transform.position = ClampToBounds(GetTargetPosition());
        }

        private void RefreshSceneBounds()
        {
            _sceneBounds = FindObjectOfType<CameraBounds>();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                EnsureTarget();
                if (target == null) return;
            }

            Vector3 desired = GetTargetPosition();

            // 平滑或紧跟
            Vector3 next = smoothFollow
                ? Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime)
                : desired;

            // 边界限制
            transform.position = ClampToBounds(next);
        }

        private Vector3 GetTargetPosition()
        {
            // 保持相机原本的 Z（一般是 -10）
            return new Vector3(
                target.position.x + offset.x,
                target.position.y + offset.y,
                transform.position.z);
        }

        private Vector3 ClampToBounds(Vector3 pos)
        {
            if (!useBounds || _cam == null || !_cam.orthographic) return pos;

            float halfHeight = _cam.orthographicSize;
            float halfWidth = halfHeight * _cam.aspect;

            // 优先用场景里的 CameraBounds（每场景独立），否则用 Inspector 配置
            float boundMinX = _sceneBounds != null ? _sceneBounds.minX : minX;
            float boundMaxX = _sceneBounds != null ? _sceneBounds.maxX : maxX;
            float boundMinY = _sceneBounds != null ? _sceneBounds.minY : minY;
            float boundMaxY = _sceneBounds != null ? _sceneBounds.maxY : maxY;

            float clampedMinX = boundMinX + halfWidth;
            float clampedMaxX = boundMaxX - halfWidth;
            float clampedMinY = boundMinY + halfHeight;
            float clampedMaxY = boundMaxY - halfHeight;

            // 处理"地图比相机视野还小"的边角情况：直接居中
            if (clampedMinX > clampedMaxX)
            {
                pos.x = (boundMinX + boundMaxX) * 0.5f;
            }
            else
            {
                pos.x = Mathf.Clamp(pos.x, clampedMinX, clampedMaxX);
            }

            if (clampedMinY > clampedMaxY)
            {
                pos.y = (boundMinY + boundMaxY) * 0.5f;
            }
            else
            {
                pos.y = Mathf.Clamp(pos.y, clampedMinY, clampedMaxY);
            }

            return pos;
        }

        private void EnsureTarget()
        {
            if (target != null) return;
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) target = go.transform;
        }

#if UNITY_EDITOR
        // 在 Scene 视图可视化地图边界 + 相机中心允许范围
        private void OnDrawGizmos()
        {
            // 青色：地图边界
            Gizmos.color = Color.cyan;
            Vector3 bl = new Vector3(minX, minY, 0);
            Vector3 br = new Vector3(maxX, minY, 0);
            Vector3 tr = new Vector3(maxX, maxY, 0);
            Vector3 tl = new Vector3(minX, maxY, 0);
            Gizmos.DrawLine(bl, br);
            Gizmos.DrawLine(br, tr);
            Gizmos.DrawLine(tr, tl);
            Gizmos.DrawLine(tl, bl);

            // 黄色：相机中心的允许范围（地图边界向内收缩相机半宽/半高）
            var cam = GetComponent<Camera>();
            if (cam != null && cam.orthographic)
            {
                float hh = cam.orthographicSize;
                float hw = hh * cam.aspect;
                float a = minX + hw, b = maxX - hw, c = minY + hh, d = maxY - hh;
                if (a <= b && c <= d)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(new Vector3(a, c, 0), new Vector3(b, c, 0));
                    Gizmos.DrawLine(new Vector3(b, c, 0), new Vector3(b, d, 0));
                    Gizmos.DrawLine(new Vector3(b, d, 0), new Vector3(a, d, 0));
                    Gizmos.DrawLine(new Vector3(a, d, 0), new Vector3(a, c, 0));
                }
            }
        }
#endif
    }
}
