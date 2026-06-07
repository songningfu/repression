using UnityEngine;

namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// 运行时自动创建闭眼转场管理器，避免每个场景手动放置 TransitionManager。
    /// </summary>
    public static class EyeBlinkTransitionBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (EyeBlinkTransition.Instance != null) return;

            var go = new GameObject("EyeBlinkTransition");
            go.AddComponent<EyeBlinkTransition>();
        }
    }
}
