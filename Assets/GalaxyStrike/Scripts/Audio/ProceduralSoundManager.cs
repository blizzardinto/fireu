using System;
using System.Collections.Generic;
using UnityEngine;
using GalaxyStrike.Core;

namespace GalaxyStrike.Audio
{
    // ============================================================================
    // 【复古 8-Bit 声音合成器 · ProceduralSoundManager.cs】
    // 对应 C++ 原工程 Sound.h / Sound.cpp：
    // 通过数学波形（方波、噪声、琶音）在 Unity 中纯代码程序化合成全部 8-bit 街机音效与 BGM，
    // 无需任何外部音频资产文件，跨平台零依赖即刻发声！
    // ============================================================================

    public class ProceduralSoundManager : MonoBehaviour
    {
        private static ProceduralSoundManager s_instance;
        public static ProceduralSoundManager Instance => s_instance;

        private AudioSource m_sfxSource;
        private AudioSource m_bgmSource;

        private bool m_soundEnabled = true;
        private readonly Dictionary<SfxType, AudioClip> m_clipCache = new Dictionary<SfxType, AudioClip>();
        private AudioClip m_bgmClip;

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_instance = this;

            m_sfxSource = gameObject.AddComponent<AudioSource>();
            m_sfxSource.playOnAwake = false;

            m_bgmSource = gameObject.AddComponent<AudioSource>();
            m_bgmSource.playOnAwake = false;
            m_bgmSource.loop = true;
            m_bgmSource.volume = 0.35f;

            PreloadAudioClips();
        }

        public bool SoundEnabled
        {
            get => m_soundEnabled;
            set
            {
                m_soundEnabled = value;
                if (!m_soundEnabled)
                {
                    m_bgmSource.Pause();
                }
                else
                {
                    if (!m_bgmSource.isPlaying) m_bgmSource.UnPause();
                }
            }
        }

        public void PlaySfx(SfxType type, bool force = false)
        {
            if (!m_soundEnabled && !force) return;

            if (m_clipCache.TryGetValue(type, out var clip))
            {
                m_sfxSource.PlayOneShot(clip);
            }
        }

        public void StartMusic()
        {
            if (!m_soundEnabled) return;
            if (m_bgmClip == null) m_bgmClip = GenerateBgmClip();
            m_bgmSource.clip = m_bgmClip;
            m_bgmSource.Play();
        }

        public void StopMusic()
        {
            m_bgmSource.Stop();
        }

        private void PreloadAudioClips()
        {
            m_clipCache[SfxType.Shoot]       = GenerateLaserChirp();
            m_clipCache[SfxType.EnemyHit]    = GenerateHitSound();
            m_clipCache[SfxType.Explosion]   = GenerateExplosionSound();
            m_clipCache[SfxType.PowerUp]     = GeneratePowerUpSound();
            m_clipCache[SfxType.ShipUpgrade] = GenerateShipUpgradeSound();
            m_clipCache[SfxType.Bomb]        = GenerateBombSound();
            m_clipCache[SfxType.BossAlert]   = GenerateBossAlertSound();
            m_clipCache[SfxType.PlayerHurt]  = GeneratePlayerHurtSound();
            m_clipCache[SfxType.Start]       = GenerateStartFanfare();
            m_clipCache[SfxType.SpeedUp]     = GenerateSpeedUpSound();
            m_clipCache[SfxType.GameOver]    = GenerateGameOverSound();
        }

        // ------------------------------------------------------------------------
        // 【数学波形程序化合成算法 (Square Wave, White Noise, FM Chirp)】
        // ------------------------------------------------------------------------

        private AudioClip GenerateLaserChirp()
        {
            int sampleRate = 44100;
            float duration = 0.09f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(950f, 220f, progress); // 快速下坠调频
                float phase = t * freq * 2f * Mathf.PI;
                float square = Mathf.Sin(phase) > 0f ? 1f : -1f; // 纯正 8-bit 方波
                float envelope = 1.0f - progress;
                samples[i] = square * envelope * 0.45f;
            }

            var clip = AudioClip.Create("SFX_Shoot", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateHitSound()
        {
            int sampleRate = 44100;
            float duration = 0.05f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1.0f - (t / duration);
                float noise = UnityEngine.Random.Range(-1f, 1f);
                float tone = Mathf.Sin(2f * Mathf.PI * 400f * t) > 0 ? 0.5f : -0.5f;
                samples[i] = (noise * 0.6f + tone * 0.4f) * env * 0.4f;
            }

            var clip = AudioClip.Create("SFX_Hit", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateExplosionSound()
        {
            int sampleRate = 44100;
            float duration = 0.28f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float env = Mathf.Pow(1.0f - progress, 1.8f);
                float noise = UnityEngine.Random.Range(-1f, 1f);
                float sub = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(120f, 40f, progress) * t);
                samples[i] = (noise * 0.7f + sub * 0.3f) * env * 0.55f;
            }

            var clip = AudioClip.Create("SFX_Explosion", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GeneratePowerUpSound()
        {
            // 上行大调三和弦：C5, E5, G5, C6
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };
            float noteDur = 0.06f;
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * noteDur * notes.Length);
            float[] samples = new float[totalSamples];

            for (int n = 0; n < notes.Length; n++)
            {
                int start = Mathf.FloorToInt(n * noteDur * sampleRate);
                int count = Mathf.FloorToInt(noteDur * sampleRate);
                float freq = notes[n];
                for (int i = 0; i < count && (start + i) < totalSamples; i++)
                {
                    float t = (float)i / sampleRate;
                    float progress = (float)i / count;
                    float env = 1.0f - progress * 0.5f;
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t) > 0 ? 0.8f : -0.8f;
                    samples[start + i] = wave * env * 0.35f;
                }
            }

            var clip = AudioClip.Create("SFX_PowerUp", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateShipUpgradeSound()
        {
            // 激昂大琶音
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f, 1318.51f };
            float noteDur = 0.07f;
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * noteDur * notes.Length);
            float[] samples = new float[totalSamples];

            for (int n = 0; n < notes.Length; n++)
            {
                int start = Mathf.FloorToInt(n * noteDur * sampleRate);
                int count = Mathf.FloorToInt(noteDur * sampleRate);
                float freq = notes[n];
                for (int i = 0; i < count && (start + i) < totalSamples; i++)
                {
                    float t = (float)i / sampleRate;
                    float env = 1.0f - ((float)i / count) * 0.3f;
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t) > 0 ? 0.7f : -0.7f;
                    samples[start + i] = wave * env * 0.45f;
                }
            }

            var clip = AudioClip.Create("SFX_ShipUpgrade", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateBombSound()
        {
            int sampleRate = 44100;
            float duration = 0.7f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float env = Mathf.Pow(1.0f - progress, 1.2f);
                float noise = UnityEngine.Random.Range(-1f, 1f);
                float sub = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(80f, 25f, progress) * t);
                samples[i] = (noise * 0.6f + sub * 0.6f) * env * 0.7f;
            }

            var clip = AudioClip.Create("SFX_Bomb", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateBossAlertSound()
        {
            int sampleRate = 44100;
            float duration = 0.45f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                // 双音急促警笛 880Hz / 587Hz
                float freq = ((int)(t * 8f) % 2 == 0) ? 880f : 587f;
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) > 0 ? 0.7f : -0.7f;
                samples[i] = wave * 0.35f;
            }

            var clip = AudioClip.Create("SFX_BossAlert", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GeneratePlayerHurtSound()
        {
            int sampleRate = 44100;
            float duration = 0.16f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1.0f - (t / duration);
                float wave = Mathf.Sin(2f * Mathf.PI * 140f * t) > 0 ? 1f : -1f;
                samples[i] = wave * env * 0.45f;
            }

            var clip = AudioClip.Create("SFX_PlayerHurt", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateStartFanfare()
        {
            float[] notes = { 440f, 554.37f, 659.25f, 880f };
            float noteDur = 0.08f;
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * noteDur * notes.Length);
            float[] samples = new float[totalSamples];

            for (int n = 0; n < notes.Length; n++)
            {
                int start = Mathf.FloorToInt(n * noteDur * sampleRate);
                int count = Mathf.FloorToInt(noteDur * sampleRate);
                float freq = notes[n];
                for (int i = 0; i < count && (start + i) < totalSamples; i++)
                {
                    float t = (float)i / sampleRate;
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t) > 0 ? 0.6f : -0.6f;
                    samples[start + i] = wave * 0.3f;
                }
            }

            var clip = AudioClip.Create("SFX_Start", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateSpeedUpSound()
        {
            float[] notes = { 700f, 950f };
            float noteDur = 0.06f;
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * noteDur * notes.Length);
            float[] samples = new float[totalSamples];

            for (int n = 0; n < notes.Length; n++)
            {
                int start = Mathf.FloorToInt(n * noteDur * sampleRate);
                int count = Mathf.FloorToInt(noteDur * sampleRate);
                float freq = notes[n];
                for (int i = 0; i < count && (start + i) < totalSamples; i++)
                {
                    float t = (float)i / sampleRate;
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t) > 0 ? 0.6f : -0.6f;
                    samples[start + i] = wave * 0.35f;
                }
            }

            var clip = AudioClip.Create("SFX_SpeedUp", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateGameOverSound()
        {
            float[] notes = { 440f, 415.3f, 392f, 349.23f };
            float noteDur = 0.18f;
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * noteDur * notes.Length);
            float[] samples = new float[totalSamples];

            for (int n = 0; n < notes.Length; n++)
            {
                int start = Mathf.FloorToInt(n * noteDur * sampleRate);
                int count = Mathf.FloorToInt(noteDur * sampleRate);
                float freq = notes[n];
                for (int i = 0; i < count && (start + i) < totalSamples; i++)
                {
                    float t = (float)i / sampleRate;
                    float env = 1.0f - ((float)i / count) * 0.4f;
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t) > 0 ? 0.6f : -0.6f;
                    samples[start + i] = wave * env * 0.4f;
                }
            }

            var clip = AudioClip.Create("SFX_GameOver", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateBgmClip()
        {
            // 8-bit 太空 Chiptune 循环旋律 (16步基准 Bassline & Lead)
            int sampleRate = 22050; // 轻量级采样率
            float bpm = 140f;
            float stepDur = 60f / bpm / 2f; // 16分音符
            float[] melody = {
                220f, 0f, 220f, 261.63f, 220f, 0f, 293.66f, 329.63f,
                220f, 0f, 220f, 261.63f, 349.23f, 329.63f, 293.66f, 261.63f
            };

            int totalSamples = Mathf.FloorToInt(sampleRate * stepDur * melody.Length);
            float[] samples = new float[totalSamples];

            for (int s = 0; s < melody.Length; s++)
            {
                int start = Mathf.FloorToInt(s * stepDur * sampleRate);
                int count = Mathf.FloorToInt(stepDur * sampleRate);
                float freq = melody[s];

                if (freq <= 0.1f) continue;

                for (int i = 0; i < count && (start + i) < totalSamples; i++)
                {
                    float t = (float)i / sampleRate;
                    float env = Mathf.Exp(-t * 6f);
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t) > 0 ? 0.35f : -0.35f;
                    float bass = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.25f;
                    samples[start + i] = (wave + bass) * env * 0.3f;
                }
            }

            var clip = AudioClip.Create("BGM_Chiptune", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
