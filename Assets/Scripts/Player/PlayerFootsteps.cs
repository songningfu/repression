// ----------------------------------------------------------------------------
// Script: PlayerFootsteps
// 作用：玩家走路触发脚步声，与 Walk 动画自动对齐。
//
// 触发逻辑（默认 AnimationTime 模式）：
//   - 读 Animator 当前 Walk 状态的 normalizedTime（0~1 循环）
//   - 每次进入 leftFootHitTime / rightFootHitTime 区间时触发一次脚步声
//   - 完美对齐动画的脚落地瞬间，不需要手动配 Animation Event
//   - 移动速度变化时自动跟随动画节奏
//
// 备用 Distance 模式：按位移距离触发（不精准但稳定）。
//
// 支持运行时换地面（FootstepSurface 接管 currentCue）。
// ----------------------------------------------------------------------------
using SeeAPsychologist.Audio;
using UnityEngine;

namespace SeeAPsychologist.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerFootsteps : MonoBehaviour
    {
        public enum TriggerMode { AnimationTime, Distance }

        [Header("脚步声配置")]
        [Tooltip("默认脚步声 AudioCue。可被 FootstepSurface 临时覆盖。")]
        [SerializeField] private AudioCue defaultCue;

        [Header("触发模式")]
        [Tooltip("AnimationTime = 跟随 Walk 动画的脚落地时间点（推荐，自动对齐）；Distance = 按位移距离。")]
        [SerializeField] private TriggerMode mode = TriggerMode.AnimationTime;

        [Header("AnimationTime 参数（用于 AnimationTime 模式）")]
        [Tooltip("Walk 动画状态名（必须与 Animator Controller 中的状态名一致）。")]
        [SerializeField] private string walkStateName = "Walk";

        [Tooltip("左脚落地在 Walk 循环的归一化时间（0~1）。看 walk 序列帧第几张左脚最低，比例填这里。")]
        [Range(0f, 1f)] [SerializeField] private float leftFootHitTime = 0.05f;

        [Tooltip("右脚落地在 Walk 循环的归一化时间（0~1）。一般是左脚 + 0.5。")]
        [Range(0f, 1f)] [SerializeField] private float rightFootHitTime = 0.55f;

        [Tooltip("命中容差。每个落地时间点 ±此值范围内算触发，防错过。")]
        [Range(0.01f, 0.15f)] [SerializeField] private float hitTolerance = 0.05f;

        [Header("Distance 参数（用于 Distance 模式）")]
        [Tooltip("每走多远触发一次脚步声。一般 = moveSpeed × 0.5。")]
        [SerializeField, Min(0.01f)] private float strideDistance = 1.2f;

        [Header("通用")]
        [Tooltip("水平速度小于此值时不播脚步声。")]
        [SerializeField, Min(0f)] private float minSpeedToPlay = 0.1f;

        [Tooltip("不填会自动 GetComponent。")]
        [SerializeField] private Animator animator;

        private Rigidbody2D _rb;
        private AudioCue _currentCue;
        private SoundEmitter _lastEmitter;

        // AnimationTime 模式状态
        private bool _leftFired;
        private bool _rightFired;
        private float _prevNormalizedTime = -1f;

        // Distance 模式状态
        private Vector2 _lastFootstepPos;
        private bool _wasMovingLastFrame;

        public AudioCue DefaultCue => defaultCue;
        public AudioCue CurrentCue => _currentCue;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (animator == null) animator = GetComponent<Animator>();
            _currentCue = defaultCue;
            _lastFootstepPos = _rb.position;
        }

        private void Update()
        {
            bool isMoving = Mathf.Abs(_rb.velocity.x) >= minSpeedToPlay;

            // 停下：打断当前脚步声 + 重置状态
            if (!isMoving)
            {
                if (_lastEmitter != null && _lastEmitter.IsPlaying) _lastEmitter.Stop(0f);
                _lastEmitter = null;
                _leftFired = _rightFired = false;
                _prevNormalizedTime = -1f;
                _wasMovingLastFrame = false;
                _lastFootstepPos = _rb.position;
                return;
            }

            if (_currentCue == null) return;

            if (mode == TriggerMode.AnimationTime) UpdateAnimationTimeMode();
            else UpdateDistanceMode();
        }

        // -- AnimationTime 模式 ------------------------------------------------

        private void UpdateAnimationTimeMode()
        {
            if (animator == null)
            {
                // 没有 Animator 退化到 Distance 模式
                UpdateDistanceMode();
                return;
            }

            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(walkStateName))
            {
                // 不在 Walk 状态（在过渡、Idle、WalkStop）— 跳过，等回到 Walk
                _prevNormalizedTime = -1f;
                return;
            }

            float nt = state.normalizedTime % 1f;
            if (nt < 0f) nt += 1f;

            // 进入新循环（nt 从 ~1 跳回 ~0）：重置 fire 标记
            if (_prevNormalizedTime > 0.7f && nt < 0.3f)
            {
                _leftFired = false;
                _rightFired = false;
            }
            _prevNormalizedTime = nt;

            // 左脚命中
            if (!_leftFired && InRange(nt, leftFootHitTime, hitTolerance))
            {
                TriggerFootstep();
                _leftFired = true;
            }
            // 右脚命中
            if (!_rightFired && InRange(nt, rightFootHitTime, hitTolerance))
            {
                TriggerFootstep();
                _rightFired = true;
            }
        }

        private static bool InRange(float v, float target, float tolerance)
            => v >= target - tolerance && v <= target + tolerance;

        // -- Distance 模式 -----------------------------------------------------

        private void UpdateDistanceMode()
        {
            // 从静止 → 移动瞬间立即触发一次（无延迟）
            if (!_wasMovingLastFrame)
            {
                TriggerFootstep();
                _lastFootstepPos = _rb.position;
                _wasMovingLastFrame = true;
                return;
            }

            float traveled = Mathf.Abs(_rb.position.x - _lastFootstepPos.x);
            if (traveled >= strideDistance)
            {
                TriggerFootstep();
                _lastFootstepPos = _rb.position;
            }
        }

        // -- 共用 --------------------------------------------------------------

        private void TriggerFootstep()
        {
            if (AudioManager.Instance == null || _currentCue == null) return;

            // 新脚步打断上一个还没播完的，避免堆叠
            if (_lastEmitter != null && _lastEmitter.IsPlaying) _lastEmitter.Stop(0f);
            _lastEmitter = AudioManager.Instance.PlaySfx(_currentCue);
        }

        // -- 公共 API --------------------------------------------------------

        public void SetCue(AudioCue cue) => _currentCue = cue != null ? cue : defaultCue;
        public void ResetToDefaultCue() => _currentCue = defaultCue;

        /// <summary>
        /// 设置场景级默认 cue（切场景时调）。
        /// 同时把当前 cue 也切过去（前提是没有 FootstepSurface 在覆盖）。
        /// </summary>
        public void SetSceneDefaultCue(AudioCue cue)
        {
            if (cue == null) return;
            defaultCue = cue;
            _currentCue = cue;
        }
    }
}
