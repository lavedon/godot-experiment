/// <summary>
/// "Iron Shadow" — OPTI-MAX's original battle theme: 32 bars at 132 BPM.
/// D minor with a Phrygian flat second: heavy footsteps, a hunt, an eerie stalking passage,
/// then a tightening pursuit. Each melody word is an eighth note; '-' holds and '.' rests.
/// Metallic FM strikes and a low, clipped pulse give this robot weight without masking attack cues.
/// </summary>
public static class HumanoidSong
{
    public const string Title = "Iron Shadow";
    public const float BeatsPerMinute = 132;
    public const int Bars = 32;

    static readonly string[] Chords =
    {
        "Dm", "Dm", "Eb/D", "Dm", "Bb", "Gm", "Adim", "A7",
        "Dm", "Eb/D", "Dm", "Bb", "Gm", "Adim", "A7", "A7",
        "Dm", "Eb/D", "Dm", "Adim", "Gm", "Eb/D", "Adim", "A7",
        "Dm", "Eb/D", "Dm", "Bb", "Gm", "Adim", "A7", "A7",
    };

    static readonly string[] Melody =
    {
        // Footsteps: the semitone curls back toward D; the tritone reaches out like a metal hand.
        "D4 - . Eb4 D4 . Ab4 A4", "F4 - E4 Eb4 D4 - . A3",
        "Eb4 - . D4 Eb4 G4 F4 .", "D4 . A3 D4 F4 - Eb4 D4",
        "F4 - . D4 Bb3 D4 F4 .",  "G4 - F4 Eb4 D4 - Bb3 .",
        "Eb4 . A3 C4 Eb4 - F4 Eb4", "E4 - C#4 . Bb3 A3 C#4 .",

        // Hunt: the same crooked signal answers itself higher up, with deliberate gaps.
        "D5 . A4 F4 Eb4 D4 . A4", "Bb4 - G4 Eb4 D4 . Eb4 G4",
        "A4 D5 . Eb5 D5 A4 F4 .", "Bb4 A4 F4 - D4 F4 A4 Bb4",
        "G4 Bb4 D5 - C5 Bb4 G4 .", "A4 . Eb5 D5 C5 A4 Eb4 .",
        "C#5 - E5 D5 Bb4 A4 G4 E4", "Bb4 A4 G4 E4 C#4 - A3 .",

        // Machine breath: sparse, hollow notes and distant metal instead of a busy lead.
        "D4 - - . A3 - . .",     "Eb4 - - . D4 - . .",
        "F4 - E4 Eb4 D4 - - .",  "Eb4 - - . A3 - . .",
        "Bb3 - D4 - G4 - . .",   "G4 - F4 Eb4 D4 - . .",
        "A3 . C4 - Eb4 - F4 Eb4", "E4 . G4 Bb4 A4 G4 E4 C#4",

        // Pursuit: shorter answers, then a chromatic turn that pulls the loop back into D.
        "D5 . Eb5 D5 A4 . F4 D4", "Eb5 D5 Bb4 G4 Eb4 . G4 Bb4",
        "A4 D5 F5 Eb5 D5 . A4 F4", "Bb4 . A4 F4 D4 F4 Bb4 A4",
        "G4 Bb4 D5 C5 Bb4 G4 F4 Eb4", "Eb5 D5 C5 A4 Eb4 . A4 C5",
        "C#5 E5 Bb4 A4 G4 E4 C#4 .", "E4 . Bb3 A3 C#4 - Eb4 C#4",
    };

    static readonly Dictionary<string, (string Root, string Fifth, string[] Notes)> Harmony = new()
    {
        ["Dm"]   = ("D2", "A2", new[] { "D3", "F3", "A3" }),
        ["Eb/D"] = ("D2", "Bb2", new[] { "Eb3", "G3", "Bb3" }),
        ["Bb"]   = ("Bb1", "F2", new[] { "D3", "F3", "Bb3" }),
        ["Gm"]   = ("G1", "D2", new[] { "D3", "G3", "Bb3" }),
        ["Adim"] = ("A1", "Eb2", new[] { "C3", "Eb3", "A3" }),
        ["A7"]   = ("A1", "E2", new[] { "G3", "C#4", "E4" }),
    };

    public static float[] Render()
    {
        if (Chords.Length != Bars || Melody.Length != Bars ||
            Melody.Any(bar => bar.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length != 8))
            throw new InvalidOperationException("Every humanoid-song bar needs one chord and eight melody steps.");

        int tickSamples = (int)(Synth.SampleRate * 60f / BeatsPerMinute / 4f);
        float tick = (float)tickSamples / Synth.SampleRate;
        var mix = new float[Bars * 16 * tickSamples];
        var lead = new float[mix.Length];
        var voice = new Envelope(0.012f, 0.12f, 0.30f, 0.06f);
        var hollow = new Envelope(0.045f, 0.28f, 0.20f, 0.18f);
        string[] notes = string.Join(' ', Melody).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < notes.Length; i++)
        {
            if (notes[i] is "-" or ".") continue;
            int held = 1;
            while (i + held < notes.Length && notes[i + held] == "-") held++;
            bool stalk = i / 8 is >= 16 and < 24;
            float at = i * 2 * tick, length = held * 2 * tick - 0.04f;
            float frequency = Synth.NoteToFrequency(notes[i]);
            Synth.AddTone(lead, at, length, stalk ? Wave.Sine : Wave.Square50,
                          stalk ? 0.20f : 0.14f, stalk ? hollow : voice, frequency, wrap: true);
            Synth.AddTone(lead, at, length, Wave.Triangle, 0.075f, voice, frequency / 2, wrap: true);
            if (stalk) Metal(lead, at, frequency * 2, 0.045f, 0.65f);
        }

        // One distant answer, wrapped across the loop like every instrument's release.
        int echo = tickSamples * 6;
        for (int i = 0; i < mix.Length; i++)
            mix[i] = lead[i] + 0.16f * lead[(i - echo + mix.Length) % mix.Length];

        var bass = new Envelope(0.007f, 0.12f, 0.35f, 0.045f);
        var pad = new Envelope(0.12f, 0.32f, 0.20f, 0.24f);
        var stab = new Envelope(0.008f, 0.10f, 0.04f, 0.04f);
        for (int bar = 0; bar < Bars; bar++)
        {
            bool stalk = bar is >= 16 and < 24;
            bool pursuit = bar >= 24;
            var chord = Harmony[Chords[bar]];
            for (int s = 0; s < 16; s++)
            {
                float at = (bar * 16 + s) * tick;
                // Three + three + two eighths: a lurching gait, with a short second step at each heel strike.
                if (stalk ? s is 0 or 8 : s is 0 or 1 or 6 or 7 or 12 or 13)
                {
                    bool heel = s % 2 == 0;
                    float frequency = Synth.NoteToFrequency(s == 12 ? chord.Fifth : chord.Root);
                    float length = tick * (stalk ? 4.5f : heel ? 0.8f : 1.5f);
                    Synth.AddTone(mix, at, length, Wave.Triangle, heel ? 0.34f : 0.22f, bass, frequency, wrap: true);
                    Synth.AddTone(mix, at, length, Wave.Square50, heel ? 0.055f : 0.028f, bass, frequency, wrap: true);
                }

                if (s == 0)
                    foreach (string note in chord.Notes)
                        Synth.AddTone(mix, at, tick * (stalk ? 11 : 5), Wave.Triangle,
                                      stalk ? 0.052f : 0.032f, pad, Synth.NoteToFrequency(note), wrap: true);
                if (!stalk && s is 6 or 14)
                    foreach (string note in chord.Notes)
                        Synth.AddTone(mix, at, tick * 0.7f, Wave.Square25, 0.023f, stab,
                                      Synth.NoteToFrequency(note), wrap: true);

                if (stalk ? s == 0 : s is 0 or 6 or 12)
                    Synth.AddTone(mix, at, 0.17f, Wave.Sine, 0.64f,
                                  new Envelope(0.004f, 0.085f, 0f, 0.035f), 115, 39, wrap: true);
                if (s == 8 || (!stalk && pursuit && s == 4)) Clank(mix, at, 1);
                if (s % (stalk ? 4 : 2) == 0)
                    Synth.AddTone(mix, at, 0.024f, Wave.HighNoise, stalk ? 0.020f : s % 4 == 2 ? 0.040f : 0.024f,
                                  new Envelope(0.002f, 0.012f, 0f, 0.01f), 1, wrap: true);

                // A small metallic counter-rhythm; its pitch belongs to the current harmony.
                if (s == 10 || (!stalk && bar % 2 == 1 && s == 15))
                    Metal(mix, at, Synth.NoteToFrequency(chord.Notes[bar % 3]) * 2, stalk ? 0.050f : 0.036f, 0.32f);
                if (bar % 8 == 7 && s is 12 or 14 or 15)
                {
                    Synth.AddTone(mix, at, 0.13f, Wave.Triangle, 0.21f,
                                  new Envelope(0.005f, 0.07f, 0f, 0.035f), 145 - (s - 12) * 20, 50, wrap: true);
                    if (s == 15) Clank(mix, at, 0.5f);
                }
            }
        }

        // Round the edges of the chip voices. Warm the filters from the tail so neither resets at the seam.
        float smooth = 0;
        for (int i = mix.Length - 256; i < mix.Length; i++) smooth += 0.30f * (mix[i] - smooth);
        for (int i = 0; i < mix.Length; i++)
        {
            smooth += 0.30f * (mix[i] - smooth);
            mix[i] = MathF.Tanh(smooth * 1.2f);
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

    static void Clank(float[] mix, float at, float strength)
    {
        Metal(mix, at, 185, 0.19f * strength, 0.18f);
        Synth.AddTone(mix, at, 0.09f, Wave.Noise, 0.115f * strength,
                      new Envelope(0.003f, 0.038f, 0f, 0.025f), 1, wrap: true);
    }

    // An inharmonic FM bell: a smooth attack, bright metal that quickly softens, and an exact zero at release.
    static void Metal(float[] mix, float at, float frequency, float volume, float seconds)
    {
        int first = (int)(at * Synth.SampleRate), count = (int)(seconds * Synth.SampleRate);
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / Synth.SampleRate;
            float phase = MathF.Tau * frequency * t;
            float brightness = 2.4f * MathF.Exp(-t * 16);
            float envelope = MathF.Min(1, t / 0.004f) * MathF.Exp(-t * 9) * (1f - (float)i / (count - 1));
            mix[(first + i) % mix.Length] += volume * envelope * MathF.Sin(phase + brightness * MathF.Sin(phase * 2.76f));
        }
    }
}
