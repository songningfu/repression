using UnityEngine;

namespace SeeAPsychologist.WorldState
{
    /// <summary>
    /// 世界音乐管理器
    /// 根据当前世界自动播放对应的背景音乐
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class WorldMusicManager : MonoBehaviour
    {
        [Header("音乐设置")]
        [Tooltip("现实世界音乐 - There's Something Outside")]
        [SerializeField] private AudioClip realityMusic;

        [Tooltip("意识世界音乐 - 流亡")]
        [SerializeField] private AudioClip consciousnessMusic;

        [Header("播放设置")]
        [Tooltip("音量")]
        [SerializeField] private float volume = 0.5f;

        [Tooltip("是否循环播放")]
        [SerializeField] private bool loop = true;

        [Tooltip("淡出时间（秒）- 应该匹配转场的闭眼时间")]
        [SerializeField] private float fadeOutTime = 1.5f;

        [Tooltip("淡入时间（秒）- 应该匹配转场的睁眼时间")]
        [SerializeField] private float fadeInTime = 2.0f;

        [Tooltip("使用平滑曲线过渡")]
        [SerializeField] private bool useSmoothCurve = true;

        private AudioSource audioSource;
        private float targetVolume;
        private bool isFading = false;
        private Coroutine currentFadeCoroutine;

        private static WorldMusicManager _instance;
        public static WorldMusicManager Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // 获取或添加 AudioSource
            audioSource = GetComponent<AudioSource>();
            audioSource.loop = loop;
            audioSource.volume = 0; // 初始音量为0
            targetVolume = volume;
        }

        private void Start()
        {
            // 订阅世界切换事件
            if (WorldStateManager.Instance != null)
            {
                WorldStateManager.Instance.OnWorldSwitched += OnWorldSwitched;
                
                // 播放当前世界的音乐（初始化时直接播放，不淡入）
                PlayMusicForWorld(WorldStateManager.Instance.CurrentWorld, immediate: true);
            }
        }

        /// <summary>
        /// 提前切换音乐（在转场动画开始时调用）
        /// </summary>
        public void PrepareMusicForWorld(WorldType targetWorld)
        {
            PlayMusicForWorld(targetWorld, immediate: false);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;

            // 取消订阅
            if (WorldStateManager.Instance != null)
            {
                WorldStateManager.Instance.OnWorldSwitched -= OnWorldSwitched;
            }
        }

        private void OnWorldSwitched(WorldSwitchedEvent evt)
        {
            // 不在这里切换音乐，而是提前在转场开始时切换
            // 这个事件只用于确保音乐状态同步
        }

        /// <summary>
        /// 播放指定世界的音乐
        /// </summary>
        private void PlayMusicForWorld(WorldType world, bool immediate = false)
        {
            AudioClip targetClip = world == WorldType.Reality ? realityMusic : consciousnessMusic;

            if (targetClip == null)
            {
                Debug.LogWarning($"[WorldMusicManager] {world} 世界的音乐未设置！");
                return;
            }

            // 如果是相同的音乐，不需要切换
            if (audioSource.clip == targetClip && audioSource.isPlaying)
            {
                return;
            }

            // 停止之前的淡入淡出协程
            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
            }

            if (immediate)
            {
                // 立即切换（用于初始化）
                audioSource.clip = targetClip;
                audioSource.volume = targetVolume;
                audioSource.Play();
            }
            else if (fadeOutTime > 0 || fadeInTime > 0)
            {
                // 切换音乐（带淡入淡出）
                currentFadeCoroutine = StartCoroutine(CrossfadeMusic(targetClip));
            }
            else
            {
                // 直接切换
                audioSource.clip = targetClip;
                audioSource.volume = targetVolume;
                audioSource.Play();
            }

            Debug.Log($"[WorldMusicManager] 播放 {world} 世界音乐: {targetClip.name}");
        }

        /// <summary>
        /// 淡入淡出切换音乐（使用平滑曲线）
        /// </summary>
        private System.Collections.IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            isFading = true;

            // 淡出当前音乐
            float startVolume = audioSource.volume;
            float elapsed = 0;

            while (elapsed < fadeOutTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutTime;
                
                // 使用平滑曲线使过渡更自然
                if (useSmoothCurve)
                {
                    t = Mathf.SmoothStep(0, 1, t);
                }
                
                audioSource.volume = Mathf.Lerp(startVolume, 0, t);
                yield return null;
            }

            audioSource.volume = 0;

            // 切换音乐
            audioSource.Stop();
            audioSource.clip = newClip;
            audioSource.Play();

            // 淡入新音乐
            elapsed = 0;
            while (elapsed < fadeInTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInTime;
                
                // 使用平滑曲线使过渡更自然
                if (useSmoothCurve)
                {
                    t = Mathf.SmoothStep(0, 1, t);
                }
                
                audioSource.volume = Mathf.Lerp(0, targetVolume, t);
                yield return null;
            }

            audioSource.volume = targetVolume;
            isFading = false;
            currentFadeCoroutine = null;
        }

        /// <summary>
        /// 设置音量
        /// </summary>
        public void SetVolume(float newVolume)
        {
            targetVolume = Mathf.Clamp01(newVolume);
            if (!isFading)
            {
                audioSource.volume = targetVolume;
            }
        }

        /// <summary>
        /// 暂停音乐
        /// </summary>
        public void Pause()
        {
            audioSource.Pause();
        }

        /// <summary>
        /// 继续播放
        /// </summary>
        public void Resume()
        {
            audioSource.UnPause();
        }

        /// <summary>
        /// 停止音乐
        /// </summary>
        public void Stop()
        {
            audioSource.Stop();
        }
    }
}

