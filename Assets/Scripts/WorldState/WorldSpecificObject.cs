using UnityEngine;

namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// 控制对象只在特定世界中显示
    /// 例如：cure 只在意识世界显示
    /// </summary>
    public class WorldSpecificObject : MonoBehaviour
    {
        [Header("显示设置")]
        [Tooltip("只在哪个世界显示")]
        [SerializeField] private WorldType showInWorld = WorldType.Consciousness;

        [Tooltip("是否在启动时自动检查")]
        [SerializeField] private bool checkOnStart = true;

        private void Start()
        {
            if (checkOnStart)
            {
                CheckAndToggle();
            }

            // 订阅世界切换事件
            if (WorldStateManager.Instance != null)
            {
                WorldStateManager.Instance.OnWorldSwitched += OnWorldSwitched;
            }
        }

        private void OnDestroy()
        {
            // 取消订阅
            if (WorldStateManager.Instance != null)
            {
                WorldStateManager.Instance.OnWorldSwitched -= OnWorldSwitched;
            }
        }

        private void OnWorldSwitched(WorldSwitchedEvent evt)
        {
            CheckAndToggle();
        }

        private void CheckAndToggle()
        {
            var worldMgr = WorldStateManager.Instance;
            if (worldMgr == null)
            {
                Debug.LogWarning("[WorldSpecificObject] WorldStateManager not found!");
                return;
            }

            // 检查当前世界是否匹配
            bool shouldShow = worldMgr.CurrentWorld == showInWorld;
            
            // 显示或隐藏对象
            gameObject.SetActive(shouldShow);
        }
    }
}
