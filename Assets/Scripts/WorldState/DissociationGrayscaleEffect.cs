using SeeAPsychologist.MentalStats;
using UnityEngine;

namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// 相机灰度滤镜：解离度越高画面越灰。
    /// - 不使用 Update/Coroutine 做数值变化；仅订阅 MentalStatsManager.OnStatsChanged
    /// - 渲染阶段使用 OnRenderImage（每帧渲染回调，不参与玩法数值逻辑）
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class DissociationGrayscaleEffect : MonoBehaviour
    {
        private const string ShaderName = "Hidden/SeeAPsychologist/DissociationGrayscale";
        private static readonly int AmountId = Shader.PropertyToID("_Amount");

        [Tooltip("可选：手动指定配置。不填则会从 Resources/WorldStateConfig 加载。")]
        [SerializeField] private WorldStateConfig config;

        [Tooltip("可选：手动指定灰度 Shader。不填则使用 Shader.Find。")]
        [SerializeField] private Shader grayscaleShader;

        private Material _material;
        private float _amount;

        public void SetConfig(WorldStateConfig cfg)
        {
            config = cfg;
            // 立即重算一次（若已存在 manager）
            var mgr = MentalStatsManager.Instance;
            if (mgr != null) ApplyDissociation(mgr.CurrentDissociation);
        }

        private void Awake()
        {
            if (config == null)
            {
                config = Resources.Load<WorldStateConfig>("WorldStateConfig");
            }

            if (grayscaleShader == null)
            {
                grayscaleShader = Shader.Find(ShaderName);
            }

            if (grayscaleShader == null)
            {
                Debug.LogError($"[A3][VFX] Grayscale shader not found: '{ShaderName}'. Effect disabled.");
                enabled = false;
                return;
            }

            _material = new Material(grayscaleShader) { hideFlags = HideFlags.HideAndDontSave };
        }

        private void OnEnable()
        {
            var mgr = MentalStatsManager.Instance;
            if (mgr != null)
            {
                mgr.OnStatsChanged += HandleStatsChanged;
                ApplyDissociation(mgr.CurrentDissociation);
            }
        }

        private void OnDisable()
        {
            var mgr = MentalStatsManager.Instance;
            if (mgr != null)
            {
                mgr.OnStatsChanged -= HandleStatsChanged;
            }
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
                _material = null;
            }
        }

        private void HandleStatsChanged(MentalStatsChangedEvent evt)
        {
            ApplyDissociation(evt.Current.Dissociation);
        }

        private void ApplyDissociation(int dissociation)
        {
            // 解离度越高 -> 越灰（由配置的 0/100 两端灰度强度决定）
            var t = Mathf.Clamp01(dissociation / 100f);
            var a0 = config != null ? config.grayscaleAtDissociation0 : 1f;
            var a100 = config != null ? config.grayscaleAtDissociation100 : 0f;
            _amount = Mathf.Clamp01(Mathf.Lerp(a0, a100, t));
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (_material == null || _amount <= 0f)
            {
                Graphics.Blit(src, dest);
                return;
            }

            _material.SetFloat(AmountId, _amount);
            Graphics.Blit(src, dest, _material);
        }
    }
}

