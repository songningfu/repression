using UnityEngine;

namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// A3 视觉反馈引导器：自动创建安装器（无需改场景）。
    /// </summary>
    public static class WorldVfxBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Object.FindObjectOfType<DissociationGrayscaleInstaller>() != null) return;

            var go = new GameObject("WorldVfxInstaller");
            go.AddComponent<DissociationGrayscaleInstaller>();
        }
    }
}

