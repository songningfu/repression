using UnityEngine;

namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// 运行时引导创建 WorldStateManager（无需改场景结构）。
    /// </summary>
    public static class WorldStateBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (WorldStateManager.Instance != null) return;

            var go = new GameObject("WorldStateManager");
            go.AddComponent<WorldStateManager>();
        }
    }
}

