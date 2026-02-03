using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// 运行时安装相机灰度效果：每次场景加载后，确保 MainCamera 上存在 DissociationGrayscaleEffect。
    /// </summary>
    public sealed class DissociationGrayscaleInstaller : MonoBehaviour
    {
        [SerializeField] private WorldStateConfig config;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (config == null)
            {
                config = Resources.Load<WorldStateConfig>("WorldStateConfig");
            }

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Start()
        {
            EnsureOnCamera(Camera.main);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureOnCamera(Camera.main);
        }

        private void EnsureOnCamera(Camera cam)
        {
            if (cam == null) return;

            var effect = cam.GetComponent<DissociationGrayscaleEffect>();
            if (effect == null)
            {
                effect = cam.gameObject.AddComponent<DissociationGrayscaleEffect>();
            }

            effect.SetConfig(config);
        }
    }
}

