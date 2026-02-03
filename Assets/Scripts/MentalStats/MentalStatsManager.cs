using System;
using System.Threading;
using UnityEngine;

namespace SeeAPsychologist.MentalStats
{
    /// <summary>
    /// A1 心理稳态数值管理器（单一数据源）。
    ///
    /// 硬性规则：
    /// - Stress 只能通过 ModifyStress 由明确事件驱动修改
    /// - 禁止使用 Update/FixedUpdate/Coroutine 做随时间自动增减（本实现使用 System.Threading.Timer + 主线程派发）
    /// - Stress / Dissociation 范围固定 0-100
    /// - Stress 变化后必须立即更新“解离度自动变化状态”，并广播 OnStatsChanged
    ///
    /// 自动变化规则：
    /// - Stress == 100：Dissociation 每 tickSeconds +tickDelta
    /// - Stress == 0：Dissociation 每 tickSeconds -tickDelta
    /// - 0 < Stress < 100：Dissociation 不自动变化
    /// </summary>
    public sealed class MentalStatsManager : MonoBehaviour
    {
        public const int MinValue = 0;
        public const int MaxValue = 100;

        public static MentalStatsManager Instance { get; private set; }

        [SerializeField] private MentalStatsConfig config;

        private int _stress;
        private int _dissociation;

        private SynchronizationContext _unityContext;

        private readonly object _timerGate = new();
        private Timer _autoTimer;
        private int _autoDirection; // +1 / -1 / 0

        /// <summary>
        /// 数值变化事件。保证在 Unity 主线程触发。
        /// </summary>
        public event Action<MentalStatsChangedEvent> OnStatsChanged;

        public int CurrentStress => _stress;
        public int CurrentDissociation => _dissociation;
        public MentalStatsState Snapshot => new(_stress, _dissociation);

        /// <summary>
        /// 设置配置（用于运行时注入/调试）。正常情况下建议在 Inspector 直接赋值。
        /// </summary>
        /// <param name="newConfig">新的配置对象。</param>
        /// <param name="resetToInitialValues">若为 true，会把当前 Stress/Dissociation 重置到配置初始值并广播事件。</param>
        /// <param name="source">用于事件追踪的来源。</param>
        public void SetConfig(MentalStatsConfig newConfig, bool resetToInitialValues, MentalStatChangeSource source)
        {
            if (newConfig == null)
            {
                Debug.LogWarning("[MentalStats] SetConfig called with null config; ignored.");
                return;
            }

            config = newConfig;

            if (!resetToInitialValues)
            {
                EvaluateAutoDissociation();
                return;
            }

            var prev = new MentalStatsState(_stress, _dissociation);
            _stress = Clamp01To100(config.initialStress);
            _dissociation = Clamp01To100(config.initialDissociation);

            EvaluateAutoDissociation();

            var cur = new MentalStatsState(_stress, _dissociation);
            RaiseStatsChanged(
                prev,
                cur,
                deltaStress: _stress - prev.Stress,
                deltaDissociation: _dissociation - prev.Dissociation,
                source: source,
                context: "SetConfig(resetToInitialValues=true)");
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // 单例冲突处理（关键：优先保留“场景内实例”，避免 Inspector 引用指向被销毁对象）
                // - 若当前实例来自 Bootstrap，而已有实例不是 -> 销毁当前
                // - 若当前实例不是 Bootstrap，而已有实例是 -> 销毁已有并由当前接管
                // - 其他情况（均为 Bootstrap 或均非 Bootstrap）-> 保留已有，销毁当前
                var existingIsBootstrap = Instance.GetComponent<MentalStatsBootstrapTag>() != null;
                var thisIsBootstrap = GetComponent<MentalStatsBootstrapTag>() != null;

                if (thisIsBootstrap && !existingIsBootstrap)
                {
                    Destroy(gameObject);
                    return;
                }

                if (!thisIsBootstrap && existingIsBootstrap)
                {
                    Destroy(Instance.gameObject);
                    Instance = this;
                    DontDestroyOnLoad(gameObject);
                    return;
                }

                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 记录主线程上下文，用于将 Timer 回调派发回主线程。
            _unityContext = SynchronizationContext.Current;
            if (_unityContext == null)
            {
                Debug.LogWarning("[MentalStats] SynchronizationContext.Current is null. " +
                                 "Timer callbacks may not marshal to Unity main thread correctly in this environment.");
            }

            var initialStress = config != null ? config.initialStress : 0;
            var initialDissociation = config != null ? config.initialDissociation : 0;
            _stress = Clamp01To100(initialStress);
            _dissociation = Clamp01To100(initialDissociation);
        }

        private void Start()
        {
            // Bootstrapper 在 BeforeSceneLoad 创建本对象时，Awake 可能拿不到 Unity 的主线程 SynchronizationContext。
            // 这里在 Start 再补一次，通常可确保 UnitySynchronizationContext 已就绪。
            _unityContext ??= SynchronizationContext.Current;
            if (_unityContext == null)
            {
                Debug.LogError("[MentalStats] Unity main thread SynchronizationContext is still null in Start(). " +
                               "Auto dissociation tick will be disabled to avoid firing events from background threads.");
                StopAutoTimer();
            }

            // 初始化时也触发一次，便于 UI/渲染订阅后立即刷新。
            RaiseStatsChanged(
                previous: new MentalStatsState(_stress, _dissociation),
                current: new MentalStatsState(_stress, _dissociation),
                deltaStress: 0,
                deltaDissociation: 0,
                source: MentalStatChangeSource.Unknown,
                context: "Init");

            EvaluateAutoDissociation();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            StopAutoTimer();
        }

        /// <summary>
        /// 修改应激度（唯一入口）。
        /// </summary>
        /// <param name="delta">应激度变化量（可正可负）。</param>
        /// <param name="source">变化来源。</param>
        /// <param name="context">可选上下文（如对话节点ID/药物ID）。</param>
        /// <returns>若 clamp 后应激度确实发生变化，返回 true；否则 false。</returns>
        public bool ModifyStress(int delta, MentalStatChangeSource source, string context = null)
        {
            if (delta == 0) return false;

            var prev = new MentalStatsState(_stress, _dissociation);
            var newStress = Clamp01To100(_stress + delta);
            if (newStress == _stress) return false;

            _stress = newStress;

            // 立即更新自动变化状态（不直接“随时间”改 Stress；这里只是启动/停止自动 tick）。
            EvaluateAutoDissociation();

            var cur = new MentalStatsState(_stress, _dissociation);
            RaiseStatsChanged(prev, cur, deltaStress: _stress - prev.Stress, deltaDissociation: 0, source, context);
            return true;
        }

        private void EvaluateAutoDissociation()
        {
            var direction = _stress switch
            {
                MaxValue => +1,
                MinValue => -1,
                _ => 0
            };

            if (direction == 0)
            {
                StopAutoTimer();
                return;
            }

            StartAutoTimer(direction);
        }

        private void StartAutoTimer(int direction)
        {
            // 没有主线程上下文就不要启动：否则回调可能在后台线程触发 OnStatsChanged，导致 Unity API 报错。
            if (_unityContext == null)
            {
                Debug.LogError("[MentalStats] Cannot start auto dissociation timer: Unity SynchronizationContext is null.");
                return;
            }

            var tickSeconds = config != null ? config.tickSeconds : 5f;
            var periodMs = Mathf.Max(100, Mathf.RoundToInt(tickSeconds * 1000f));

            lock (_timerGate)
            {
                if (_autoTimer != null && _autoDirection == direction)
                {
                    return; // 已在正确方向 ticking
                }

                StopAutoTimer_NoLock();
                _autoDirection = direction;

                _autoTimer = new Timer(_ =>
                {
                    // Timer 回调在非主线程，必须派发回主线程。
                    _unityContext.Post(__ => AutoTickOnMainThread(direction), null);
                }, null, dueTime: periodMs, period: periodMs);
            }
        }

        private void StopAutoTimer()
        {
            lock (_timerGate)
            {
                StopAutoTimer_NoLock();
            }
        }

        private void StopAutoTimer_NoLock()
        {
            _autoDirection = 0;
            _autoTimer?.Dispose();
            _autoTimer = null;
        }

        private void AutoTickOnMainThread(int direction)
        {
            // 主线程二次校验：Stress 已经不在边界则停止。
            if (direction == +1 && _stress != MaxValue)
            {
                StopAutoTimer();
                return;
            }

            if (direction == -1 && _stress != MinValue)
            {
                StopAutoTimer();
                return;
            }

            var delta = config != null ? Mathf.Max(1, config.tickDelta) : 1;
            var prev = new MentalStatsState(_stress, _dissociation);
            var newDissociation = Clamp01To100(_dissociation + direction * delta);

            if (newDissociation == _dissociation)
            {
                // 已到达边界（0 或 100），继续 tick 没意义，停止 timer。
                StopAutoTimer();
                return;
            }

            _dissociation = newDissociation;
            var cur = new MentalStatsState(_stress, _dissociation);
            RaiseStatsChanged(
                prev,
                cur,
                deltaStress: 0,
                deltaDissociation: _dissociation - prev.Dissociation,
                source: MentalStatChangeSource.AutoTick,
                context: direction > 0 ? "Stress==100" : "Stress==0");
        }

        private void RaiseStatsChanged(
            MentalStatsState previous,
            MentalStatsState current,
            int deltaStress,
            int deltaDissociation,
            MentalStatChangeSource source,
            string context)
        {
            try
            {
                OnStatsChanged?.Invoke(new MentalStatsChangedEvent(
                    previous,
                    current,
                    deltaStress,
                    deltaDissociation,
                    source,
                    context));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MentalStats] OnStatsChanged subscriber threw exception: {ex}");
            }
        }

        private static int Clamp01To100(int value)
        {
            if (value < MinValue) return MinValue;
            if (value > MaxValue) return MaxValue;
            return value;
        }
    }
}

