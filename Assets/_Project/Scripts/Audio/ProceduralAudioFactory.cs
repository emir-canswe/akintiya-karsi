using UnityEngine;

namespace Upstream.Audio
{
    /// <summary>
    /// Generates procedural AudioClips in memory so the game has full sound effects and ambient water
    /// out-of-the-box without requiring external audio asset files.
    /// </summary>
    public static class ProceduralAudioFactory
    {
        private const int SampleRate = 44100;

        public static void PopulateAudioManager(AudioManager manager)
        {
            if (manager == null) return;

            // Load high-quality WAV clips from Resources/Audio if available, fallback to procedural synthesis
            AudioClip eatFood = Resources.Load<AudioClip>("Audio/SFX_EatFood") ?? CreatePluck("SFX_EatFood", 540f, 760f, 0.09f, 0.25f);
            AudioClip eatGold = Resources.Load<AudioClip>("Audio/SFX_EatGold") ?? CreateChordChime("SFX_EatGold", new[] { 659.25f, 830.61f, 987.77f }, 0.34f, 0.30f);
            AudioClip turboStart = Resources.Load<AudioClip>("Audio/SFX_TurboStart") ?? CreateSweep("SFX_TurboStart", 280f, 880f, 0.55f, 0.26f, true);
            AudioClip turboLoop = Resources.Load<AudioClip>("Audio/SFX_TurboLoop") ?? CreateWaterRushLoop("SFX_TurboLoop", 0.8f, 0.14f);
            AudioClip hurt = Resources.Load<AudioClip>("Audio/SFX_Hurt") ?? CreateSweep("SFX_Hurt", 150f, 65f, 0.28f, 0.32f, false, true);
            AudioClip die = Resources.Load<AudioClip>("Audio/SFX_Die") ?? CreateSweep("SFX_Die", 260f, 68f, 0.75f, 0.28f, true);
            AudioClip sharkEat = Resources.Load<AudioClip>("Audio/SFX_SharkEat") ?? CreateSharkDevourSound("SFX_SharkEat");
            AudioClip stageUp = Resources.Load<AudioClip>("Audio/SFX_StageUp") ?? CreateChordChime("SFX_StageUp", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.48f, 0.32f);
            AudioClip click = Resources.Load<AudioClip>("Audio/SFX_Click") ?? CreatePluck("SFX_Click", 620f, 700f, 0.05f, 0.18f);
            AudioClip ambient = Resources.Load<AudioClip>("Audio/AMB_River") ?? CreateAmbientStreamLoop("AMB_River", 3.0f, 0.08f);

            manager.SetClips(eatFood, eatGold, turboStart, turboLoop, hurt, die, sharkEat, stageUp, click, ambient);
            manager.SetWaveSplash(Resources.Load<AudioClip>("Audio/SFX_WaveSplash") ?? CreateWaveSplash("SFX_WaveSplash", 0.7f, 0.34f));
        }

        private static AudioClip CreatePluck(string name, float startFreq, float endFreq, float duration, float volume)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            float[] data = new float[samples];
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += freq / SampleRate;
                float env = Mathf.Pow(1f - t, 1.8f);
                data[i] = Mathf.Sin(phase * Mathf.PI * 2f) * env * volume;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateChordChime(string name, float[] freqs, float duration, float volume)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float timeSec = (float)i / SampleRate;
                float env = Mathf.Pow(1f - t, 1.5f);
                float sum = 0f;
                for (int k = 0; k < freqs.Length; k++)
                {
                    float delay = k * 0.045f;
                    if (timeSec >= delay)
                    {
                        float localT = Mathf.Clamp01((timeSec - delay) / Mathf.Max(0.05f, duration - delay));
                        float localEnv = Mathf.Pow(1f - localT, 1.6f);
                        sum += Mathf.Sin((timeSec - delay) * freqs[k] * Mathf.PI * 2f) * localEnv;
                    }
                }
                data[i] = Mathf.Clamp(sum / freqs.Length * volume * env * 1.6f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateSweep(string name, float startFreq, float endFreq, float duration, float volume, bool saw = false, bool square = false)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            float[] data = new float[samples];
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += freq / SampleRate;
                float frac = phase - Mathf.Floor(phase);

                float wave;
                if (square) wave = frac < 0.5f ? 1f : -1f;
                else if (saw) wave = frac * 2f - 1f;
                else wave = Mathf.Sin(phase * Mathf.PI * 2f);

                float env = Mathf.Pow(1f - t, 1.35f);
                data[i] = wave * env * volume;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateSharkDevourSound(string name)
        {
            float duration = 0.52f;
            int samples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[samples];
            float phase = 0f;
            System.Random rng = new System.Random(1337);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float subFreq = Mathf.Lerp(145f, 42f, Mathf.Pow(t, 0.6f));
                phase += subFreq / SampleRate;

                float subBass = Mathf.Sin(phase * Mathf.PI * 2f) * Mathf.Pow(1f - t, 0.9f);
                float crunch = (t > 0.04f && t < 0.24f)
                    ? ((float)rng.NextDouble() * 2f - 1f) * Mathf.Sin((t - 0.04f) / 0.20f * Mathf.PI) * 0.55f
                    : 0f;
                float triumph = Mathf.Sin(i / (float)SampleRate * 587.33f * Mathf.PI * 2f) * Mathf.Pow(1f - t, 1.6f) * 0.35f;

                data[i] = Mathf.Clamp((subBass * 0.65f + crunch + triumph) * 0.55f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Breaking wave: a low swell rising into a burst of hissing foam that washes away.</summary>
        private static AudioClip CreateWaveSplash(string name, float duration, float volume)
        {
            int samples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(7);
            float low = 0f, high = 0f, prevWhite = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float white = (float)rng.NextDouble() * 2f - 1f;
                low = low * 0.97f + white * 0.03f;           // deep rumble of the swell
                high = 0.6f * (white - prevWhite) + 0.4f * high; // bright hiss of the foam
                prevWhite = white;
                float swell = Mathf.Sin(Mathf.Clamp01(t / 0.35f) * Mathf.PI * 0.5f);
                float crash = t < 0.18f ? 0f : Mathf.Pow(1f - (t - 0.18f) / 0.82f, 1.6f);
                float tail = Mathf.Pow(1f - t, 2.2f);
                data[i] = Mathf.Clamp((low * 6f * swell * tail + high * 0.55f * crash) * volume, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateWaterRushLoop(string name, float duration, float volume)
        {
            int samples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(42);
            float prev = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float white = (float)rng.NextDouble() * 2f - 1f;
                prev = prev * 0.88f + white * 0.12f;
                float mod = 0.75f + 0.25f * Mathf.Sin(t * Mathf.PI * 4f);
                data[i] = prev * mod * volume;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateAmbientStreamLoop(string name, float duration, float volume)
        {
            int samples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(99);
            float lp1 = 0f;
            float lp2 = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float white = (float)rng.NextDouble() * 2f - 1f;
                lp1 = lp1 * 0.96f + white * 0.04f;
                lp2 = lp2 * 0.94f + lp1 * 0.06f;
                float lfo = 0.8f + 0.2f * Mathf.Sin(t * Mathf.PI * 2f);
                data[i] = lp2 * lfo * volume * 3.2f;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
