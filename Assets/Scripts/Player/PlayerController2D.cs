// ----------------------------------------------------------------------------
// Script: PlayerController2D
// 作用：本游戏（2D 叙事横版）的完整玩家控制器。
//       负责：水平输入读取 + 物理移动 + Animator 状态切换 + Sprite 朝向翻转。
//
// 设计思路（为什么这么写）：
//   - 本游戏是室内叙事横版，不需要跳跃 / 自由落体 / 接地检测
//   - 所以禁用重力 + 锁定 Y 轴，玩家永远在地板高度水平行走
//   - 用 Dynamic Rigidbody2D 配合 velocity 移动，保留碰撞检测（撞墙/门会停）
//   - 不依赖任何 ScriptableObject 配置，所有参数 Inspector 直接调
//
// 使用方法：
//   1. 把本组件挂在 player 对象上（同物体必须有 Rigidbody2D / Animator / SpriteRenderer / Collider2D）
//   2. Animator Controller 里必须有 Bool 参数 "IsMoving"
//   3. Animator 状态机：Idle / Walk / (可选) WalkStop，用 IsMoving 切换
//   4. 立绘 Pivot 建议 Bottom（脚底锚点）
// ----------------------------------------------------------------------------
using UnityEngine;

namespace SeeAPsychologist.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlayerController2D : MonoBehaviour
    {
        [Header("移动")]
        [Tooltip("水平移动速度（单位/秒）。手感慢=2~3，正常=4~5，跑=7~10。")]
        [SerializeField] private float moveSpeed = 4f;

        [Tooltip("水平输入死区。绝对值小于此值时视为无输入。")]
        [SerializeField, Range(0f, 0.9f)] private float inputDeadZone = 0.1f;

        [Header("Animator")]
        [Tooltip("控制 Idle/Walk 切换的 Bool 参数名（必须与 Animator Controller 中一致）。")]
        [SerializeField] private string isMovingParam = "IsMoving";

        [Header("朝向")]
        [Tooltip("立绘素材本身默认是否朝右（看 idle.PNG 原始朝向）。")]
        [SerializeField] private bool defaultFacingRight = true;

        [Header("物理自动配置（Awake 时强制设置，避免人为遗漏）")]
        [Tooltip("勾选后 Awake 会自动配置 Rigidbody2D：无重力 / 锁旋转 / 锁 Y 轴。")]
        [SerializeField] private bool autoConfigureRigidbody = true;

        private Rigidbody2D _rb;
        private Animator _animator;
        private SpriteRenderer _sprite;
        private int _isMovingHash;
        private int _lastFacingSign = 1;
        private float _currentHorizontal;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _sprite = GetComponent<SpriteRenderer>();
            _isMovingHash = Animator.StringToHash(isMovingParam);

            if (autoConfigureRigidbody)
            {
                // 关键：本游戏不需要重力和自由落体
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _rb.gravityScale = 0f;
                _rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
                _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                _rb.drag = 0f;
                _rb.angularDrag = 0f;
            }
        }

        private void Update()
        {
            // 读输入 + 设置动画状态（Update 频率更高，动画切换更跟手）
            _currentHorizontal = Input.GetAxisRaw("Horizontal");
            bool isMoving = Mathf.Abs(_currentHorizontal) > inputDeadZone;
            _animator.SetBool(_isMovingHash, isMoving);
            UpdateFacing(_currentHorizontal);
        }

        private void FixedUpdate()
        {
            // 移动 (FixedUpdate 与物理同步，避免抖动)
            float vx = Mathf.Abs(_currentHorizontal) > inputDeadZone
                ? Mathf.Sign(_currentHorizontal) * moveSpeed
                : 0f;

            _rb.velocity = new Vector2(vx, 0f);
        }

        private void UpdateFacing(float horizontal)
        {
            if (Mathf.Abs(horizontal) < 0.01f) return;
            int sign = horizontal > 0f ? 1 : -1;
            if (sign == _lastFacingSign) return;

            _lastFacingSign = sign;
            bool faceRight = sign == 1;
            _sprite.flipX = defaultFacingRight ? !faceRight : faceRight;
        }

        // -- 公共 API（给对话/剧情系统用） -------------------------------------

        /// <summary>
        /// 临时禁用/启用玩家控制（对话期间用）。
        /// </summary>
        public void SetControlEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled)
            {
                _rb.velocity = Vector2.zero;
                _animator.SetBool(_isMovingHash, false);
            }
        }
    }
}
