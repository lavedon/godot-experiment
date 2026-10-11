/// <summary>
/// "Neon Slipstream" — an original space-station running theme: 48 bars in F# minor at 128 BPM.
/// Airlock, cyan rails, inverted gravity, observation window, magenta lights, and the return tunnel.
/// Glass keys, a gently pumping pad and mirrored arpeggios keep the corridor moving without boss-like urgency.
/// Each melody word is an eighth note: '-' holds the previous note and '.' rests.
/// </summary>
public static class StationSong
{
    public const string Title = "Neon Slipstream";
    public const float BeatsPerMinute = 128;
    public const int Bars = 48;

    static readonly string[] Chords =
    {
        "F#m9", "F#m9", "Dmaj7", "E6", "F#m9", "Amaj7", "Dmaj7", "C#sus",
        "F#m9", "E6", "Dmaj7", "Amaj7", "Bm7", "Dmaj7", "C#sus", "C#7",
        "F#m9", "Dmaj7", "Amaj7", "E6", "Bm7", "Dmaj7", "C#sus", "C#7",
        "Dmaj7", "F#m9", "Bm7", "E6", "Dmaj7", "Amaj7", "C#sus", "C#7",
        "F#m9", "E6", "Dmaj7", "Amaj7", "F#m9", "Bm7", "Dmaj7", "C#sus",
        "Bm7", "Amaj7", "E6", "Dmaj7", "F#m9", "Dmaj7", "C#sus", "C#7",
    };

    static readonly string[] Melody =
    {
        // Airlock: a signal in the dark, answered by its reflection.
        "C#5 - . G#4 A4 - . .",  "F#4 - G#4 A4 C#5 - B4 .",
        "A4 - . E5 F#5 - E5 C#5", "B4 - G#4 . F#4 G#4 B4 .",
        "C#5 - G#4 A4 F#4 - . .", "E5 - C#5 . B4 C#5 E5 G#5",
        "F#5 - E5 C#5 A4 - B4 .", "G#4 - C#5 - F#5 - E5 .",

        // Cyan rails: a longer line above the steady pulse.
        "A4 C#5 F#5 - G#5 F#5 E5 C#5", "B4 - E5 G#5 F#5 E5 B4 .",
        "C#5 E5 F#5 - A5 F#5 E5 C#5", "B4 C#5 E5 - G#5 E5 C#5 .",
        "D5 - F#5 A5 F#5 E5 D5 B4", "C#5 E5 A5 - G#5 F#5 E5 .",
        "G#4 C#5 F#5 - E5 C#5 B4 .", "G#4 - E#5 - G#5 F#5 E#5 C#5",

        // Inverted gravity: falling answers, then a lift back toward the ceiling.
        "F#5 - E5 C#5 A4 G#4 F#4 .", "E5 - C#5 A4 F#4 A4 C#5 .",
        "G#5 E5 C#5 - B4 C#5 E5 .", "F#5 E5 B4 G#4 E4 - G#4 B4",
        "F#5 D5 B4 - A4 B4 D5 F#5", "E5 C#5 A4 - B4 C#5 E5 F#5",
        "G#5 - F#5 E5 C#5 - B4 G#4", "E#5 - C#5 . B4 G#4 C#5 .",

        // Observation window: the drums recede; long notes hang above the planet.
        "F#5 - - - E5 - C#5 .", "G#4 - A4 - C#5 - - .",
        "D5 - - - A4 - B4 .",  "G#4 - B4 - F#5 - - .",
        "E5 - C#5 - A4 - - .", "B4 - C#5 - G#5 - E5 .",
        "F#5 - E5 - C#5 - - .", "G#4 - B4 - E#5 - C#5 .",

        // Magenta lights: the signal returns, with a small countermelody in the sequencer.
        "C#5 - G#4 A4 F#5 - E5 C#5", "B4 E5 G#5 - F#5 E5 B4 .",
        "A4 C#5 E5 - F#5 E5 C#5 A4", "G#4 B4 C#5 E5 G#5 - E5 .",
        "F#5 E5 C#5 A4 G#4 A4 C#5 .", "D5 F#5 A5 - F#5 D5 B4 A4",
        "C#5 - E5 F#5 A5 - G#5 F#5", "E5 C#5 B4 G#4 C#5 - F#5 .",

        // Return tunnel: the lights thin out again, and C# leads naturally back into F# minor.
        "F#5 - D5 B4 A4 B4 D5 .", "E5 - C#5 B4 G#4 B4 C#5 E5",
        "G#5 F#5 E5 B4 G#4 - B4 .", "F#5 E5 C#5 A4 F#4 A4 C#5 .",
        "C#5 - . G#4 A4 - F#4 .", "E5 - C#5 A4 F#4 - E4 .",
        "G#4 - B4 C#5 F#5 - E5 C#5", "B4 - G#4 . E#4 - C#4 .",
    };

    static readonly Dictionary<string, (string Root, string[] Notes)> Harmony = new()
    {
        ["F#m9"]  = ("F#2", new[] { "A3", "C#4", "E4", "G#4" }),
        ["Dmaj7"] = ("D2", new[] { "A3", "C#4", "D4", "F#4" }),
        ["E6"]    = ("E2", new[] { "G#3", "B3", "C#4", "E4" }),
        ["Amaj7"] = ("A2", new[] { "G#3", "B3", "C#4", "E4" }),
        ["Bm7"]   = ("B1", new[] { "A3", "B3", "D4", "F#4" }),
        ["C#sus"] = ("C#2", new[] { "G#3", "B3", "C#4", "F#4" }),
        ["C#7"]   = ("C#2", new[] { "G#3", "B3", "C#4", "E#4" }),
    };

    static readonly int[] Rising = { 0, 1, 3, 2, 1, 2, 3, 1 };
    static readonly int[] Falling = { 3, 2, 0, 1, 2, 1, 0, 2 };
    static readonly int[] BassSteps = { 0, 0, 12, 7, 0, 0, 7, 12 };

    public static float[] Render()
    {
        if (Chords.Length != Bars || Melody.Length != Bars ||
            Melody.Any(bar => bar.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length != 8))
            throw new InvalidOperationException("Every station-song bar needs one chord and eight melody steps.");

        int tickSamples = (int)MathF.Round(Synth.SampleRate * 60f / BeatsPerMinute / 4f);
        float tick = (float)tickSamples / Synth.SampleRate;
        var mix = new float[Bars * 16 * tickSamples];
        var keys = new float[mix.Length];
        var atmosphere = new float[mix.Length];
        var glassVoice = new Envelope(0.012f, 0.25f, 0.16f, 0.20f);
        string[] notes = string.Join(' ', Melody).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < notes.Length; i++)
        {
            if (notes[i] is "-" or ".") continue;
            int held = 1;
            while (i + held < notes.Length && notes[i + held] == "-") held++;
            bool floating = i / 8 is >= 24 and < 32;
            float at = i * 2 * tick, length = held * 2 * tick - 0.025f;
            float frequency = Synth.NoteToFrequency(notes[i]);
            Synth.AddTone(keys, at, length, Wave.Sine, floating ? 0.20f : 0.17f, glassVoice, frequency, wrap: true);
            Synth.AddTone(keys, at, length, Wave.Triangle, 0.065f, glassVoice, frequency, wrap: true);
            // A brief octave glint gives the soft keys a glassy attack, without an alarm-like high tone.
            Synth.AddTone(keys, at, 0.10f, Wave.Sine, 0.037f,
                          new Envelope(0.005f, 0.055f, 0f, 0.08f), frequency * 2, wrap: true);
        }

        var pad = new Envelope(0.18f, 0.55f, 0.42f, 0.38f);
        var pluck = new Envelope(0.006f, 0.060f, 0.025f, 0.045f);
        var bass = new Envelope(0.008f, 0.13f, 0.38f, 0.04f);
        for (int bar = 0; bar < Bars; bar++)
        {
            bool floating = bar is >= 24 and < 32;
            bool opening = bar < 8 || bar >= 44;
            var chord = Harmony[Chords[bar]];
            int[] pattern = bar / 8 % 2 == 0 ? Rising : Falling;
            float root = Synth.NoteToFrequency(chord.Root);
            foreach (string note in chord.Notes)
            {
                float frequency = Synth.NoteToFrequency(note);
                Synth.AddTone(atmosphere, bar * 16 * tick, tick * 14, Wave.Triangle, 0.030f, pad, frequency, wrap: true);
                Synth.AddTone(atmosphere, bar * 16 * tick, tick * 14, Wave.Sine, 0.020f, pad, frequency * 1.002f, wrap: true);
            }
            for (int s = 0; s < 16; s++)
            {
                float at = (bar * 16 + s) * tick;
                if (s % 2 == 1 && (!floating || s % 4 == 1))
                {
                    float frequency = Synth.NoteToFrequency(chord.Notes[pattern[s / 2]]) * 2;
                    Synth.AddTone(keys, at, tick * 0.7f, Wave.Triangle, floating ? 0.035f : 0.055f,
                                  pluck, frequency, wrap: true);
                    if (!floating && !opening)
                        Synth.AddTone(keys, at, tick * 0.4f, Wave.Square25, 0.012f, pluck, frequency, wrap: true);
                }
                if (floating ? s is 0 or 8 : s % 2 == 0)
                {
                    float frequency = root * MathF.Pow(2, (floating ? 0 : BassSteps[s / 2]) / 12f);
                    float length = tick * (floating ? 5.5f : s % 4 == 0 ? 1.35f : 0.8f);
                    Synth.AddTone(mix, at, length, Wave.Triangle, 0.28f, bass, frequency, wrap: true);
                    Synth.AddTone(mix, at, length, Wave.Sine, 0.065f, bass, frequency, wrap: true);
                }

                // A steady four-beat drive; the window section floats over half as many kicks.
                if (floating ? s is 0 or 8 : s % 4 == 0)
                    Synth.AddTone(mix, at, 0.13f, Wave.Sine, 0.51f,
                                  new Envelope(0.004f, 0.075f, 0f, 0.03f), 125, 44, wrap: true);
                if (floating ? s == 12 : s is 4 or 12) Clap(mix, at, floating ? 0.45f : 0.8f);
                if (s % 2 == 0)
                    Synth.AddTone(mix, at, s % 4 == 2 ? 0.055f : 0.02f, Wave.HighNoise,
                                  floating ? 0.018f : s % 4 == 2 ? 0.038f : 0.022f,
                                  new Envelope(0.002f, 0.023f, 0f, 0.018f), 1, wrap: true);
                if (bar % 8 == 7 && s == 15) Clap(mix, at, 0.35f);
            }

            // A very soft air sweep signals a new corridor, never a sharp impact or a long silence.
            if (bar % 8 == 0)
                Synth.AddTone(atmosphere, bar * 16 * tick, tick * 5, Wave.HighNoise, 0.026f,
                              new Envelope(0.12f, 0.22f, 0f, 0.25f), 1, wrap: true);
        }

        // Tunnel reflections wrap into bar one. The pad gently breathes between quarter-note kicks.
        int echo = tickSamples * 3, farEcho = tickSamples * 7, beatSamples = tickSamples * 4;
        for (int i = 0; i < mix.Length; i++)
        {
            float breath = 0.70f - 0.30f * MathF.Cos(MathF.Tau * (i % beatSamples) / beatSamples);
            mix[i] += keys[i] + 0.25f * keys[(i - echo + mix.Length) % mix.Length]
                             + 0.12f * keys[(i - farEcho + mix.Length) % mix.Length]
                             + atmosphere[i] * breath;
        }

        float smooth = 0;
        for (int i = mix.Length - 256; i < mix.Length; i++) smooth += 0.32f * (mix[i] - smooth);
        for (int i = 0; i < mix.Length; i++)
        {
            smooth += 0.32f * (mix[i] - smooth);
            mix[i] = MathF.Tanh(smooth * 1.12f);
        }
        float dc = 0;
        for (int i = mix.Length - 8192; i < mix.Length; i++) dc += 0.003f * (mix[i] - dc);
        for (int i = 0; i < mix.Length; i++)
        {
            dc += 0.003f * (mix[i] - dc);
            mix[i] -= dc;
        }
        Synth.Normalize(mix, 0.85f);
        return mix;
    }

    static void Clap(float[] mix, float at, float strength)
    {
        for (int tap = 0; tap < 3; tap++)
            Synth.AddTone(mix, at + tap * 0.013f, tap == 2 ? 0.07f : 0.012f, Wave.Noise,
                          0.09f * strength, new Envelope(0.002f, 0.028f, 0f, 0.02f), 1, wrap: true);
        Synth.AddTone(mix, at, 0.065f, Wave.Triangle, 0.060f * strength,
                      new Envelope(0.004f, 0.04f, 0f, 0.02f), 185, 155, wrap: true);
    }
}
