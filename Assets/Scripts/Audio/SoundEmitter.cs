// ----------------------------------------------------------------------------
// Script: SoundEmitter
// 作用：封装 AudioSource 的"声源对象"。被 AudioManager 池化复用。
//
// 职责：
//   - 包装一个 AudioSource
//   - 应用 AudioCue 的所有参数（音量、音调、空间化、循环）
//   - 跟随目标 Transform（3D 音源用）
//   - 播完自动通知 AudioManager 回收
//   - 支持淡入淡出
//
// 用户不需要直接挂这个脚本到任何对象上，由 AudioManager 自动 Spawn。
// ----------------------------------------------------------------------------
using System;
using System.Collections;
using UnityEngine;

namespace SeeAPsychologist.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class SoundEmitter : MonoBehaviour
    {
        private AudioSource _source;
        private Transform _followTarget;
        private Coroutine _autoReleaseRoutine;
        private Coroutine _fadeRoutine;
        private float _finalVolume;

        public bool IsPlaying => _source != null && _source.isPlaying;
        public AudioCategory Category { get; private set; }
        public AudioCue CurrentCue { get; private set; }

        /// <summary>播完时通知（用于回池）。</summary>
        public event Action<SoundEmitter> OnFinished;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
        }

        /// <summary>
        /// 播放一个 AudioCue。
        /// </summary>
        /// <param name="cue">音频配置</param>
        /// <param name="finalVolume">最终音量（AudioCue 随机化 × 类别音量 × 总音量）</param>
        /// <param name="position">空间位置（spatialBlend > 0 时生效）</param>
        /// <param name="followTarget">可选：跟随该 Transform 移动</param>
        public void Play(AudioCue cue, float finalVolume, Vector3 position, Transform followTarget = null)
        {
            if (cue == null) return;
            var clip = cue.PickRandomClip();
            if (clip == null) return;

            CurrentCue = cue;
            Category = cue.category;
            _followTarget = followTarget;
            _finalVolume = Mathf.Clamp01(finalVolume);

            transform.position = position;
            _source.clip = clip;
            _source.pitch = cue.PickPitch();
            _source.loop = cue.loop;
            _source.spatialBlend = cue.spatialBlend;
            _source.volume = cue.fadeInSeconds > 0f ? 0f : _finalVolume;

            _source.Play();

            // 淡入
            if (cue.fadeInSeconds > 0f)
            {
                StartFade(_finalVolume, cue.fadeInSeconds);
            }

            // 非循环：监视播放结束自动回收
            StopAutoReleaseRoutine();
            if (!cue.loop)
            {
                _autoReleaseRoutine = StartCoroutine(AutoReleaseAfterPlay(clip.length / Mathf.Max(0.01f, _source.pitch)));
            }
        }

        /// <summary>停止（可选淡出）。</summary>
        public void Stop(float fadeOutSeconds = 0f)
        {
            StopAutoReleaseRoutine();

            if (fadeOutSeconds > 0f && _source.isPlaying)
            {
                StartFade(0f, fadeOutSeconds, releaseAfter: true);
            }
            else
            {
                _source.Stop();
                Release();
            }
        }

        /// <summary>外部修改最终音量（如总音量变化时被 AudioManager 调用）。</summary>
        public void SetFinalVolume(float volume)
        {
            _finalVolume = Mathf.Clamp01(volume);
            if (_fadeRoutine == null) _source.volume = _finalVolume;
        }

        private void LateUpdate()
        {
            if (_followTarget != null) transform.position = _followTarget.position;
        }

        // -- 内部 --------------------------------------------------------------

        private IEnumerator AutoReleaseAfterPlay(float duration)
        {
            yield return new WaitForSeconds(duration + 0.05f);
            Release();
        }

        private void StartFade(float toVolume, float duration, bool releaseAfter = false)
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeRoutine(toVolume, duration, releaseAfter));
        }

        private IEnumerator FadeRoutine(float toVolume, float duration, bool releaseAfter)
        {
            float fromVolume = _source.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _source.volume = Mathf.Lerp(fromVolume, toVolume, t / duration);
                yield return null;
            }
            _source.volume = toVolume;
            _fadeRoutine = null;

            if (releaseAfter)
            {
                _source.Stop();
                Release();
            }
        }

        private void StopAutoReleaseRoutine()
        {
            if (_autoReleaseRoutine != null)
            {
                StopCoroutine(_autoReleaseRoutine);
                _autoReleaseRoutine = null;
            }
        }

        private void Release()
        {
            StopAutoReleaseRoutine();
            CurrentCue = null;
            _followTarget = null;
            OnFinished?.Invoke(this);
        }
    }
}
