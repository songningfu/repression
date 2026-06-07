// ----------------------------------------------------------------------------
// Script: AudioManager
// 作用：全局音频管理器（单例，DontDestroyOnLoad）。
//
// 核心能力：
//   - 5 类音量独立控制（Master / Music / Ambient / SFX / UI / Voice），PlayerPrefs 持久化
//   - 音乐 / 环境音：交叉淡入淡出切换，每类同时只有一个长期声源
//   - SFX / UI / Voice：声源池化，按需生成 SoundEmitter，播完回池
//   - 全部通过 AudioCue 配置驱动，调用方一行 API 搞定
//
// 用户不需要在场景里放 AudioManager —— 由 AudioManagerBootstrapper 自动创建。
//
// 调用示例：
//   AudioManager.Instance.PlayMusic(myMusicCue);
//   AudioManager.Instance.PlaySfx(footstepCue);
//   AudioManager.Instance.PlaySfx(doorOpenCue, doorTransform.position);
//   AudioManager.Instance.SetVolume(AudioCategory.Music, 0.6f);
//   AudioManager.Instance.StopMusic(fadeOut: 1.5f);
// ----------------------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;

namespace SeeAPsychologist.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        // -- 音量（0~1）--------------------------------------------------------
        private const string PP_MASTER = "Audio.Volume.Master";
        private const string PP_MUSIC  = "Audio.Volume.Music";
        private const string PP_AMB    = "Audio.Volume.Ambient";
        private const string PP_SFX    = "Audio.Volume.SFX";
        private const string PP_UI     = "Audio.Volume.UI";
        private const string PP_VOICE  = "Audio.Volume.Voice";

        private float _master  = 1f;
        private float _music   = 0.8f;
        private float _ambient = 0.7f;
        private float _sfx     = 1f;
        private float _ui      = 1f;
        private float _voice   = 1f;

        public float MasterVolume  => _master;
        public float MusicVolume   => _music;
        public float AmbientVolume => _ambient;
        public float SfxVolume     => _sfx;
        public float UIVolume      => _ui;
        public float VoiceVolume   => _voice;

        // -- 池 ----------------------------------------------------------------
        private const int InitialPoolSize = 8;
        private const int MaxPoolSize = 32;

        private readonly Queue<SoundEmitter> _sfxPool = new();
        private readonly List<SoundEmitter> _activeEmitters = new();

        private SoundEmitter _musicEmitter;
        private SoundEmitter _ambientEmitter;

        private Transform _poolRoot;
        private readonly Dictionary<AudioCue, float> _lastPlayTime = new();

        // -- 生命周期 ----------------------------------------------------------

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _poolRoot = new GameObject("[Pool]").transform;
            _poolRoot.SetParent(transform);

            LoadVolumes();
            WarmupPool();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // -- 公共 API：播放 -----------------------------------------------------

        /// <summary>播放背景音乐（同时只有 1 个，自动交叉淡入淡出）。</summary>
        public void PlayMusic(AudioCue cue)
        {
            if (cue == null) return;
            CrossfadeLongLived(ref _musicEmitter, cue, AudioCategory.Music);
        }

        /// <summary>停止音乐（可选淡出）。</summary>
        public void StopMusic(float fadeOut = 1f)
        {
            if (_musicEmitter != null && _musicEmitter.IsPlaying) _musicEmitter.Stop(fadeOut);
        }

        /// <summary>播放环境氛围音（同时只有 1 个，自动交叉淡入淡出）。</summary>
        public void PlayAmbient(AudioCue cue)
        {
            if (cue == null) return;
            CrossfadeLongLived(ref _ambientEmitter, cue, AudioCategory.Ambient);
        }

        public void StopAmbient(float fadeOut = 1f)
        {
            if (_ambientEmitter != null && _ambientEmitter.IsPlaying) _ambientEmitter.Stop(fadeOut);
        }

        /// <summary>播放短音效（2D，无位置）。</summary>
        public SoundEmitter PlaySfx(AudioCue cue)
        {
            return InternalPlayShort(cue, Vector3.zero, null);
        }

        /// <summary>播放空间化音效（3D，跟随某个位置）。</summary>
        public SoundEmitter PlaySfx(AudioCue cue, Vector3 worldPosition)
        {
            return InternalPlayShort(cue, worldPosition, null);
        }

        /// <summary>播放音效并跟随某个 Transform。</summary>
        public SoundEmitter PlaySfxFollow(AudioCue cue, Transform follow)
        {
            return InternalPlayShort(cue, follow != null ? follow.position : Vector3.zero, follow);
        }

        // -- 公共 API：音量 -----------------------------------------------------

        public void SetVolume(AudioCategory category, float value)
        {
            value = Mathf.Clamp01(value);
            switch (category)
            {
                case AudioCategory.Music:   _music = value;   PlayerPrefs.SetFloat(PP_MUSIC,  value); break;
                case AudioCategory.Ambient: _ambient = value; PlayerPrefs.SetFloat(PP_AMB,    value); break;
                case AudioCategory.SFX:     _sfx = value;     PlayerPrefs.SetFloat(PP_SFX,    value); break;
                case AudioCategory.UI:      _ui = value;      PlayerPrefs.SetFloat(PP_UI,     value); break;
                case AudioCategory.Voice:   _voice = value;   PlayerPrefs.SetFloat(PP_VOICE,  value); break;
            }
            RefreshActiveVolumes();
        }

        public void SetMasterVolume(float value)
        {
            _master = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PP_MASTER, _master);
            RefreshActiveVolumes();
        }

        public float GetVolume(AudioCategory c) => c switch
        {
            AudioCategory.Music   => _music,
            AudioCategory.Ambient => _ambient,
            AudioCategory.SFX     => _sfx,
            AudioCategory.UI      => _ui,
            AudioCategory.Voice   => _voice,
            _ => 1f
        };

        /// <summary>清空所有活动声源（场景切换/重启时可调）。</summary>
        public void StopAll(float fadeOut = 0f)
        {
            StopMusic(fadeOut);
            StopAmbient(fadeOut);
            for (int i = _activeEmitters.Count - 1; i >= 0; i--)
            {
                var e = _activeEmitters[i];
                if (e != null) e.Stop(fadeOut);
            }
        }

        // -- 内部：池 ----------------------------------------------------------

        private void WarmupPool()
        {
            for (int i = 0; i < InitialPoolSize; i++)
            {
                _sfxPool.Enqueue(CreateEmitter());
            }
        }

        private SoundEmitter CreateEmitter()
        {
            var go = new GameObject("Emitter");
            go.transform.SetParent(_poolRoot);
            go.SetActive(false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            var emitter = go.AddComponent<SoundEmitter>();
            emitter.OnFinished += ReturnToPool;
            return emitter;
        }

        private SoundEmitter SpawnEmitter()
        {
            SoundEmitter e;
            while (_sfxPool.Count > 0)
            {
                e = _sfxPool.Dequeue();
                if (e != null) { e.gameObject.SetActive(true); _activeEmitters.Add(e); return e; }
            }

            // 池空，且未达上限就扩容；否则复用最早一个（粗暴抢占）
            int total = _sfxPool.Count + _activeEmitters.Count;
            if (total < MaxPoolSize)
            {
                e = CreateEmitter();
                e.gameObject.SetActive(true);
                _activeEmitters.Add(e);
                return e;
            }

            // 抢占：停最早一个 active
            var oldest = _activeEmitters[0];
            _activeEmitters.RemoveAt(0);
            oldest.Stop(0f);
            oldest.gameObject.SetActive(true);
            _activeEmitters.Add(oldest);
            return oldest;
        }

        private void ReturnToPool(SoundEmitter e)
        {
            if (e == null) return;
            _activeEmitters.Remove(e);
            e.gameObject.SetActive(false);
            if (_sfxPool.Count < MaxPoolSize) _sfxPool.Enqueue(e);
            else Destroy(e.gameObject);
        }

        // -- 内部：播放逻辑 -----------------------------------------------------

        private SoundEmitter InternalPlayShort(AudioCue cue, Vector3 pos, Transform follow)
        {
            if (cue == null) return null;
            if (!CheckMinInterval(cue)) return null;

            float finalVol = ComputeFinalVolume(cue);
            if (finalVol <= 0f) return null; // 静音直接跳过

            var emitter = SpawnEmitter();
            emitter.Play(cue, finalVol, pos, follow);
            _lastPlayTime[cue] = Time.time;
            return emitter;
        }

        private void CrossfadeLongLived(ref SoundEmitter current, AudioCue cue, AudioCategory expected)
        {
            // 淡出旧的
            if (current != null && current.IsPlaying)
            {
                current.Stop(cue.fadeOutSeconds > 0f ? cue.fadeOutSeconds : 0.8f);
            }

            // 起新的
            float finalVol = ComputeFinalVolume(cue);
            var newEmitter = SpawnEmitter();
            newEmitter.Play(cue, finalVol, transform.position);
            current = newEmitter;
        }

        private bool CheckMinInterval(AudioCue cue)
        {
            if (cue.minIntervalSeconds <= 0f) return true;
            if (!_lastPlayTime.TryGetValue(cue, out var last)) return true;
            return Time.time - last >= cue.minIntervalSeconds;
        }

        private float ComputeFinalVolume(AudioCue cue)
        {
            float categoryVol = GetVolume(cue.category);
            return cue.PickVolume() * categoryVol * _master;
        }

        private void RefreshActiveVolumes()
        {
            for (int i = 0; i < _activeEmitters.Count; i++)
            {
                var e = _activeEmitters[i];
                if (e == null || e.CurrentCue == null) continue;
                e.SetFinalVolume(e.CurrentCue.PickVolume() * GetVolume(e.Category) * _master);
            }
        }

        // -- 持久化 ------------------------------------------------------------

        private void LoadVolumes()
        {
            _master  = PlayerPrefs.GetFloat(PP_MASTER, 1f);
            _music   = PlayerPrefs.GetFloat(PP_MUSIC,  0.8f);
            _ambient = PlayerPrefs.GetFloat(PP_AMB,    0.7f);
            _sfx     = PlayerPrefs.GetFloat(PP_SFX,    1f);
            _ui      = PlayerPrefs.GetFloat(PP_UI,     1f);
            _voice   = PlayerPrefs.GetFloat(PP_VOICE,  1f);
        }
    }
}
