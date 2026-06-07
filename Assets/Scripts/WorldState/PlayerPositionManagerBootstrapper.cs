using UnityEngine;

namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// 运行时自动创建玩家位置管理器，供床交互与世界切换恢复落点。
    /// </summary>
    public static class PlayerPositionManagerBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PlayerPositionManager.Instance != null) return;

            var go = new GameObject("PlayerPositionManager");
            go.AddComponent<PlayerPositionManager>();
        }
    }
}
