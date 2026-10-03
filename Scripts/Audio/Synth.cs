using Godot;

/// <summary>The shape of a sound wave. Each shape has its own "color" of sound.</summary>
public enum Wave
{
    Square50,  // hollow, like an old game console
    Square25,  // brighter and buzzier
    Square12,  // thin and nasal
    Triangle,  // soft and round (great for bass)
    Sine,      // the purest, smoothest tone
    Noise,     // "shhhh" (drums, explosions)
    HighNoise, // a sharper, hissier "tsss" (hi-hats)
}

/// <summary>
/// How loud a note is over time:
/// Attack = how fast it starts, Decay = how fast it fades down to the Sustain level, Release = fade-out at the end.
/// </summary>
public record Envelope(float Attack, float Decay, float Sustain, float Release)
{
    public float LevelAt(float t, float noteLength)
    {
        float level = t < Attack
            ? t / Attack
            : Sustain + (1 - Sustain) * Mathf.Exp(-(t - Attack) / Decay);
        if (t > noteLength) level *= Mathf.Max(0, 1 - (t - noteLength) / Release);
        return level;
    }
}

/// <summary>
/// A tiny synthesizer! Robot Dash doesn't use any sound files. It makes every sound with math, like old game consoles did.
/// A sound is just a long list of numbers between -1 and 1 (called "samples"), 44,100 of them for every second.
/// </summary>
public static class Synth
{
    public const int SampleRate = 44100;

    // Always the same "random" noise, so the music sounds the same every time.
    static readonly Random noiseMaker = new(12345);

    /// <summary>An empty (silent) sound that lasts this many seconds.</summary>
    public static float[] Silence(float seconds) => new float[(int)(seconds * SampleRate)];

    /// <summary>Turns a note name like "C4", "F#5" or "Bb3" into a key number (middle C = 60).</summary>
    public static int NoteToKey(string note)
    {
        int key = "C D EF G A B".IndexOf(char.ToUpperInvariant(note[0])); // C=0, D=2, E=4, F=5, G=7, A=9, B=11
        if (key < 0) throw new ArgumentException($"Not a note: {note}");
        int i = 1;
        if (note[i] == '#') { key++; i++; }
        else if (note[i] == 'b') { key--; i++; }
        int octave = int.Parse(note[i..]);
        return (octave + 1) * 12 + key;
    }

    /// <summary>The frequency (how many wiggles per second) of a key. The A above middle C is 440.</summary>
    public static float KeyToFrequency(float key) => 440f * Mathf.Pow(2f, (key - 69f) / 12f);

    public static float NoteToFrequency(string note) => KeyToFrequency(NoteToKey(note));

    /// <summary>
    /// Adds a tone into a sound. The pitch can slide from startFrequency to endFrequency (great for "boing!" and "pew!").
    /// With wrap = true, a note that rings past the end continues at the beginning (for music that loops).
    /// </summary>
    public static void AddTone(float[] sound, float startSeconds, float lengthSeconds, Wave wave, float volume,
                               Envelope envelope, float startFrequency, float endFrequency = -1,
                               float vibrato = 0, bool wrap = false)
    {
        if (endFrequency <= 0) endFrequency = startFrequency;
        int first = (int)(startSeconds * SampleRate);
        int count = (int)((lengthSeconds + envelope.Release) * SampleRate);
        double phase = 0;
        float lastNoise = 0;

        for (int i = 0; i < count; i++)
        {
            int index = first + i;
            if (wrap) index %= sound.Length;
            else if (index >= sound.Length) break;

            float t = (float)i / SampleRate;
            float slide = Mathf.Min(1f, t / lengthSeconds);
            float frequency = startFrequency * Mathf.Pow(endFrequency / startFrequency, slide);
            if (vibrato > 0 && t > 0.12f) frequency *= 1 + vibrato * Mathf.Sin(Mathf.Tau * 5.5f * t); // wobbly singing

            float step = frequency / SampleRate;
            phase += step;
            if (phase >= 1) phase -= 1;
            float p = (float)phase;

            float sample;
            switch (wave)
            {
                case Wave.Square50: sample = Square(p, 0.5f, step); break;
                case Wave.Square25: sample = Square(p, 0.25f, step); break;
                case Wave.Square12: sample = Square(p, 0.125f, step); break;
                case Wave.Triangle: sample = 4f * Mathf.Abs(p - 0.5f) - 1f; break;
                case Wave.Sine: sample = Mathf.Sin(Mathf.Tau * p); break;
                case Wave.Noise: sample = NextNoise(); break;
                default: // HighNoise: the difference between two noises is extra hissy
                    float noise = NextNoise();
                    sample = (noise - lastNoise) * 0.5f;
                    lastNoise = noise;
                    break;
            }

            sound[index] += sample * volume * envelope.LevelAt(t, lengthSeconds);
        }
    }

    /// <summary>A square wave: up for part of each wiggle ("duty"), down for the rest. The PolyBlep bits smooth the corners so it doesn't sound scratchy.</summary>
    static float Square(float phase, float duty, float step)
    {
        float value = phase < duty ? 1f : -1f;
        value += PolyBlep(phase, step);
        value -= PolyBlep((phase - duty + 1f) % 1f, step);
        return value;
    }

    static float PolyBlep(float t, float step)
    {
        if (t < step) { t /= step; return t + t - t * t - 1f; }
        if (t > 1f - step) { t = (t - 1f) / step; return t * t + t + t + 1f; }
        return 0f;
    }

    static float NextNoise() => noiseMaker.NextSingle() * 2f - 1f;

    /// <summary>Makes the loudest part of a sound reach "peak" (so everything is a nice, even volume).</summary>
    public static void Normalize(float[] sound, float peak = 0.9f)
    {
        float loudest = 0;
        foreach (float s in sound) loudest = Mathf.Max(loudest, Mathf.Abs(s));
        if (loudest < 0.0001f) return;
        float scale = peak / loudest;
        for (int i = 0; i < sound.Length; i++) sound[i] *= scale;
    }

    /// <summary>Turns our list of numbers into something Godot can play.</summary>
    public static AudioStreamWav ToAudioStream(float[] sound, bool loop = false)
    {
        var bytes = new byte[sound.Length * 2];
        for (int i = 0; i < sound.Length; i++)
        {
            short value = (short)(Mathf.Clamp(sound[i], -1f, 1f) * short.MaxValue);
            bytes[i * 2] = (byte)(value & 0xFF);
            bytes[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        var stream = new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = SampleRate,
            Stereo = false,
            Data = bytes,
        };
        if (loop)
        {
            stream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
            stream.LoopBegin = 0;
            stream.LoopEnd = sound.Length;
        }
        return stream;
    }
}
