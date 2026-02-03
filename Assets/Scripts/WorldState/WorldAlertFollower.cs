using UnityEngine;

namespace WorldState
{
    /// <summary>
    /// 让 World Alert UI 跟随玩家头顶
    /// 支持 World Space Canvas 和 Screen Space Canvas
    /// </summary>
    public class WorldAlertFollower : MonoBehaviour
    {
        [Header("跟随设置")]
        [SerializeField] private Transform target; // 跟随目标（玩家）
        [SerializeField] private Vector3 worldOffset = new Vector3(0, 2f, 0); // 世界空间偏移量（头顶上方）
        [SerializeField] private bool autoFindPlayer = true; // 自动查找玩家
        
        [Header("平滑设置")]
        [SerializeField] private bool smoothFollow = false; // 是否平滑跟随（关闭=紧密跟随）
        [SerializeField] private float smoothSpeed = 20f; // 平滑速度（数值越大越紧密，建议15-30）
        
        [Header("Canvas 设置")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RenderMode canvasRenderMode = RenderMode.WorldSpace;
        [SerializeField] private bool faceCamera = true; // 是否始终面向摄像机
        
        private Camera mainCamera;
        
        private void Awake()
        {
            // 获取或添加 Canvas 组件
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }
            
            // 设置 Canvas 为 World Space 模式
            if (canvas != null)
            {
                canvas.renderMode = canvasRenderMode;
                
                if (canvasRenderMode == RenderMode.WorldSpace)
                {
                    // 调整 Canvas 的缩放，使其在世界空间中大小合适
                    transform.localScale = Vector3.one * 0.01f;
                }
            }
        }
        
        private void Start()
        {
            mainCamera = Camera.main;
            
            // 如果启用自动查找且没有设置目标，尝试查找玩家
            if (autoFindPlayer && target == null)
            {
                FindPlayer();
            }
        }
        
        private void LateUpdate()
        {
            if (target == null)
            {
                // 如果目标丢失，尝试重新查找
                if (autoFindPlayer)
                {
                    FindPlayer();
                }
                return;
            }
            
            // 计算目标位置（玩家头顶）
            Vector3 targetPosition = target.position + worldOffset;
            
            // 根据设置选择直接跟随或平滑跟随
            if (smoothFollow)
            {
                // 平滑跟随 - 使用更高的速度值可以让跟随更紧密
                transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
            }
            else
            {
                // 直接跟随 - 完全紧密，无延迟
                transform.position = targetPosition;
            }
            
            // 让 Canvas 始终面向摄像机
            if (faceCamera && mainCamera != null && canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
            }
        }
        
        /// <summary>
        /// 查找玩家对象
        /// </summary>
        private void FindPlayer()
        {
            // 尝试通过标签查找
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log($"[WorldAlertFollower] 找到玩家: {player.name}");
                return;
            }
            
            // 尝试通过名称查找
            player = GameObject.Find("Player Controller");
            if (player != null)
            {
                target = player.transform;
                Debug.Log($"[WorldAlertFollower] 通过名称找到玩家: {player.name}");
                return;
            }
            
            Debug.LogWarning("[WorldAlertFollower] 未找到玩家对象！请确保玩家有 'Player' 标签或名为 'Player Controller'");
        }
        
        /// <summary>
        /// 手动设置跟随目标
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
        
        /// <summary>
        /// 设置偏移量
        /// </summary>
        public void SetOffset(Vector3 newOffset)
        {
            worldOffset = newOffset;
        }
        
        /// <summary>
        /// 设置是否平滑跟随
        /// </summary>
        public void SetSmoothFollow(bool smooth)
        {
            smoothFollow = smooth;
        }
    }
}
