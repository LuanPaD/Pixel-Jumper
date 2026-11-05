using UnityEngine;

/// <summary>Efeitos sonoros retrô gerados por código (ondas quadradas), sem arquivos de áudio.</summary>
public static class Sfx
{
    private const int Rate = 44100;
    private static AudioSource source;
    private static AudioClip jump, coin, hit, checkpoint, win;

    public static float Volume = 0.3f;

    public static void Jump() { Play(ref jump, () => Tone(280f, 640f, 0.16f)); }
    public static void Coin() { Play(ref coin, () => Cat(Tone(988f, 988f, 0.07f), Tone(1319f, 1319f, 0.18f))); }
    public static void Hit() { Play(ref hit, () => Cat(Tone(220f, 60f, 0.25f), Tone(0f, 0f, 0.02f, true))); }
    public static void Checkpoint() { Play(ref checkpoint, () => Cat(Tone(523f, 523f, 0.08f), Tone(659f, 659f, 0.08f), Tone(784f, 784f, 0.14f))); }
    public static void Win() { Play(ref win, () => Cat(Tone(523f, 523f, 0.12f), Tone(659f, 659f, 0.12f), Tone(784f, 784f, 0.12f), Tone(1047f, 1047f, 0.35f))); }

    private static void Play(ref AudioClip clip, System.Func<float[]> build)
    {
        if (source == null)
        {
            var go = new GameObject("Sfx");
            Object.DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        if (clip == null)
        {
            float[] samples = build();
            clip = AudioClip.Create("sfx", samples.Length, 1, Rate, false);
            clip.SetData(samples, 0);
        }

        source.PlayOneShot(clip, Volume);
    }

    private static float[] Tone(float f0, float f1, float duration, bool noise = false)
    {
        int n = Mathf.Max(1, (int)(Rate * duration));
        var samples = new float[n];
        var rng = new System.Random(3);
        float phase = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            phase += Mathf.Lerp(f0, f1, t) / Rate;
            float envelope = 1f - t;
            float wave = noise ? (float)(rng.NextDouble() * 2.0 - 1.0) : (phase % 1f < 0.5f ? 1f : -1f);
            samples[i] = wave * envelope * 0.5f;
        }

        return samples;
    }

    private static float[] Cat(params float[][] parts)
    {
        int total = 0;
        foreach (var p in parts) total += p.Length;

        var all = new float[total];
        int offset = 0;
        foreach (var p in parts)
        {
            System.Array.Copy(p, 0, all, offset, p.Length);
            offset += p.Length;
        }
        return all;
    }
}
