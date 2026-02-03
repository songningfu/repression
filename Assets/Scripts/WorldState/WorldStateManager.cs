using System;
using SeeAPsychologist.MentalStats;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// A3 世界状态与切换控制器（单一入口：TrySwitchWorld）。
    ///
    /// 设计约束：
    /// - 不写入 MentalStats，只读取 MentalStatsManager.CurrentDissociation
    /// - 通过事件对外广播切换结果：OnWorldSwitched
    /// - 所有阈值/场景名由 WorldStateConfig 驱动（Resources/WorldStateConfig）
    /// </summary>
    public sealed class WorldStateManager : MonoBehaviour
    {
        public static WorldStateManager Instance { get; private set; }

        [SerializeField] private WorldStateConfig config;

        public WorldType CurrentWorld { get; private set; }

        /// <summary>
        /// 世界切换完成事件（在目标场景 loaded 后触发，保证 CurrentWorld 已更新）。
        /// </summary>
        public event Action<WorldSwitchedEvent> OnWorldSwitched;

        private PendingSwitch _pending;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (config == null)
            {
                config = Resources.Load<WorldStateConfig>("WorldStateConfig");
                if (config == null)
                {
                    Debug.LogError("[A3][WorldState] WorldStateConfig missing at Resources/WorldStateConfig. " +
                                   "World switching will use fallback defaults and may not match design intent.");
                    config = ScriptableObject.CreateInstance<WorldStateConfig>();
                }
            }

            config.ValidateAndFixIfNeeded();
            CurrentWorld = GuessWorldFromScene(SceneManager.GetActiveScene().name, config);
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        /// <summary>
        /// 对外入口：尝试切换世界。
        /// </summary>
        /// <param name="interactionType">触发源类型。</param>
        /// <param name="targetWorld">输出：目标世界（即使不切换，也会输出“本次判定目标/当前世界”）。</param>
        /// <param name="context">可选上下文（例如药物ID/剧情节点ID）。</param>
        /// <returns>若成功发起切换（目标世界 != 当前世界 且开始加载目标场景），返回 true；否则 false。</returns>
        public bool TrySwitchWorld(InteractionType interactionType, out WorldType targetWorld, string context = null)
        {
            targetWorld = CurrentWorld;

            var stats = MentalStatsManager.Instance;
            var dissociation = stats != null ? stats.CurrentDissociation : 0;

            if (!TryResolveTargetWorld(interactionType, dissociation, out targetWorld))
            {
                return false;
            }

            if (targetWorld == CurrentWorld)
            {
                return false;
            }

            var sceneToLoad = GetSceneNameForWorld(targetWorld, config);
            if (string.IsNullOrWhiteSpace(sceneToLoad))
            {
                Debug.LogError($"[A3][WorldState] Scene name for {targetWorld} is empty. Switch aborted.");
                return false;
            }

            _pending = new PendingSwitch(
                previous: CurrentWorld,
                target: targetWorld,
                interactionType: interactionType,
                dissociationAtSwitch: dissociation,
                context: context);

            Debug.Log($"[A3][WorldState] Switching world: {CurrentWorld} -> {targetWorld} " +
                      $"(interaction={interactionType}, dissociation={dissociation}, scene='{sceneToLoad}')");

            SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
            return true;
        }

        /// <summary>
        /// 便捷重载：忽略 targetWorld 输出。
        /// </summary>
        public bool TrySwitchWorld(InteractionType interactionType, string context = null)
        {
            return TrySwitchWorld(interactionType, out _, context);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var newWorld = GuessWorldFromScene(scene.name, config);
            if (newWorld != CurrentWorld)
            {
                var prev = CurrentWorld;
                CurrentWorld = newWorld;

                // 若是由 TrySwitchWorld 发起，则用 pending 的上下文；否则作为“外部切场景”的同步。
                if (_pending.IsValid && _pending.Target == newWorld)
                {
                    RaiseWorldSwitched(new WorldSwitchedEvent(
                        _pending.Previous,
                        newWorld,
                        _pending.InteractionType,
                        _pending.DissociationAtSwitch,
                        _pending.Context));
                    _pending = default;
                }
                else
                {
                    RaiseWorldSwitched(new WorldSwitchedEvent(
                        prev,
                        newWorld,
                        InteractionType.Bed,
                        dissociationAtSwitch: MentalStatsManager.Instance != null ? MentalStatsManager.Instance.CurrentDissociation : 0,
                        context: "SceneLoadedSync"));
                }
            }
        }

        private void RaiseWorldSwitched(WorldSwitchedEvent evt)
        {
            try
            {
                OnWorldSwitched?.Invoke(evt);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[A3][WorldState] OnWorldSwitched subscriber threw exception: {ex}");
            }
        }

        private bool TryResolveTargetWorld(InteractionType interactionType, int dissociation, out WorldType targetWorld)
        {
            // 注意：这里不允许写入 MentalStats，只基于传入的 dissociation 判定。
            switch (interactionType)
            {
                case InteractionType.Bed:
                    // 0-10 -> Reality，90-100 -> Consciousness，中间区间不切换
                    if (dissociation <= config.bedEnterRealityMaxDissociation)
                    {
                        targetWorld = WorldType.Reality;
                        return true;
                    }

                    if (dissociation >= config.bedEnterConsciousnessMinDissociation)
                    {
                        targetWorld = WorldType.Consciousness;
                        return true;
                    }

                    targetWorld = CurrentWorld;
                    return false;

                case InteractionType.MedicineToReality:
                    targetWorld = WorldType.Reality;
                    return true;

                case InteractionType.MedicineToConsciousness:
                case InteractionType.StoryEventToConsciousness:
                    targetWorld = WorldType.Consciousness;
                    return true;

                default:
                    targetWorld = CurrentWorld;
                    return false;
            }
        }

        private static WorldType GuessWorldFromScene(string sceneName, WorldStateConfig cfg)
        {
            if (!string.IsNullOrWhiteSpace(cfg.realitySceneName) && sceneName == cfg.realitySceneName)
                return WorldType.Reality;
            if (!string.IsNullOrWhiteSpace(cfg.consciousnessSceneName) && sceneName == cfg.consciousnessSceneName)
                return WorldType.Consciousness;
            return cfg.fallbackInitialWorld;
        }

        private static string GetSceneNameForWorld(WorldType world, WorldStateConfig cfg)
        {
            return world == WorldType.Reality ? cfg.realitySceneName : cfg.consciousnessSceneName;
        }

        private readonly struct PendingSwitch
        {
            public readonly bool IsValid;
            public readonly WorldType Previous;
            public readonly WorldType Target;
            public readonly InteractionType InteractionType;
            public readonly int DissociationAtSwitch;
            public readonly string Context;

            public PendingSwitch(
                WorldType previous,
                WorldType target,
                InteractionType interactionType,
                int dissociationAtSwitch,
                string context)
            {
                IsValid = true;
                Previous = previous;
                Target = target;
                InteractionType = interactionType;
                DissociationAtSwitch = dissociationAtSwitch;
                Context = context;
            }
        }
    }
}

