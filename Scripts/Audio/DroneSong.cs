/// <summary>
/// "Rotor Rush" — an original flying-drone battle theme: 32 bars in E minor at 152 BPM.
/// Four eight-bar phrases: rotor chase, climbing answer, suspended hover, and a diving final pass.
/// The lead leaves gaps for radar-like answers; sixteenth-note arpeggios give the propellers their pulse.
/// Each melody word is an eighth note: '-' holds the previous note and '.' rests.
/// </summary>
public static class DroneSong
{
    public const string Title = "Rotor Rush";
    public const float BeatsPerMinute = 152;
    public const int Bars = 32;

    static readonly string[] Chords =
    {
        "Em", "Em", "Cmaj7", "D", "Em", "Cmaj7", "Am", "B7",
        "G", "D", "Em", "B7", "Cmaj7", "Am", "B7", "B7",
        "Cmaj7", "Em", "Am", "B7", "Cmaj7", "D", "Am", "B7",
        "Em", "G", "Cmaj7", "D", "Am", "Cmaj7", "B7", "B7",
    };

    static readonly string[] Melody =
    {
        // Rotor chase: a short E-B-G signal, and a falling answer.
        "E5 . B5 - G5 F#5 E5 .",   "B5 . E6 D6 B5 . G5 A5",
        "G5 . B5 - E6 D6 B5 G5",  "F#5 A5 D6 - A5 F#5 E5 .",
        "E5 . B5 - E6 D6 B5 .",   "G5 B5 E6 - D6 B5 G5 E5",
        "A5 . E6 - C6 B5 A5 G5",  "F#5 D#5 B4 . F#5 A5 D#6 -",

        // Climbing answer: wider leaps and longer arcs above the rotor pattern.
        "B5 - D6 G6 F#6 D6 B5 .", "A5 - D6 F#6 E6 D6 A5 .",
        "G5 B5 E6 - F#6 E6 D6 B5", "F#5 A5 D#6 - F#6 D#6 B5 .",
        "E6 - B5 G5 B5 D6 E6 G6", "E6 C6 A5 - B5 C6 E6 .",
        "D#6 - F#6 A6 F#6 D#6 B5 A5", "F#5 . B5 D#6 F#6 - D#6 .",

        // Hover: half-time drums, a gentler voice, and space between calls.
        "E6 - - - B5 - G5 .",     "F#5 - G5 - B5 - - .",
        "C6 - - - B5 A5 E5 .",    "D#5 - F#5 - B5 - - .",
        "G5 - B5 - E6 - D6 B5",   "A5 - F#5 - E5 F#5 A5 D6",
        "C6 - E6 - B5 A5 G5 E5",  "F#5 . A5 B5 D#6 - F#6 D#6",

        // Final dive: the opening signal an octave up, then a descent into the loop.
        "E6 . B6 - G6 F#6 E6 .",  "D6 B5 G5 B5 D6 G6 F#6 D6",
        "E6 . G6 - E6 D6 B5 G5",  "F#6 E6 D6 - A5 F#5 E5 D5",
        "E6 C6 A5 . C6 E6 G6 E6", "G6 E6 D6 B5 G5 E5 G5 B5",
        "F#6 D#6 B5 A5 F#5 D#5 B4 .", "B5 . A5 F#5 D#5 - B4 D#5",
    };

    static readonly Dictionary<string, (string Root, string Fifth, string[] Notes)> Harmony = new()
    {
        ["Em"]    = ("E2", "B2", new[] { "E4", "G4", "B4", "F#5" }),
        ["Cmaj7"] = ("C2", "G2", new[] { "E4", "G4", "B4", "C5" }),
        ["D"]     = ("D2", "A2", new[] { "D4", "F#4", "A4", "E5" }),
        ["G"]     = ("G2", "D3", new[] { "D4", "G4", "B4", "D5" }),
        ["Am"]    = ("A2", "E3", new[] { "E4", "A4", "C5", "B4" }),
        ["B7"]    = ("B2", "F#3", new[] { "D#4", "F#4", "A4", "B4" }),
    };

    // Going up, circling back, then up again feels like blades rotating rather than a marching bass line.
    static readonly int[] RotorNotes = { 0, 2, 1, 3, 2, 0, 3, 1 };

    public static float[] Render()
    {
        if (Chords.Length != Bars || Melody.Length != Bars ||
            Melody.Any(bar => bar.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length != 8))
            throw new InvalidOperationException("Every drone-song bar needs one chord and eight melody steps.");

        int tickSamples = (int)(Synth.SampleRate * 60f / BeatsPerMinute / 4f);
        float tick = (float)tickSamples / Synth.SampleRate;
        var mix = new float[Bars * 16 * tickSamples];
        var lead = new float[mix.Length];
        var voice = new Envelope(0.009f, 0.10f, 0.32f, 0.055f);
        var hoverVoice = new Envelope(0.025f, 0.24f, 0.42f, 0.14f);
        string[] notes = string.Join(' ', Melody).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < notes.Length; i++)
        {
            if (notes[i] is "-" or ".") continue;
            int held = 1;
            while (i + held < notes.Length && notes[i + held] == "-") held++;
            bool hover = i / 8 is >= 16 and < 24;
            float at = i * 2 * tick, length = held * 2 * tick - 0.035f;
            float frequency = Synth.NoteToFrequency(notes[i]);
            Synth.AddTone(lead, at, length, hover ? Wave.Triangle : Wave.Square12,
                          hover ? 0.22f : 0.145f, hover ? hoverVoice : voice, frequency, vibrato: 0.003f, wrap: true);
            Synth.AddTone(lead, at, length, Wave.Sine, 0.10f, voice, frequency / 2, wrap: true);
        }

        // A short repeat answers each call across the sky. Both echoes wrap with the composition.
        int echo = tickSamples * 3;
        for (int i = 0; i < mix.Length; i++)
            mix[i] = lead[i] + 0.20f * lead[(i - echo + mix.Length) % mix.Length]
                            + 0.08f * lead[(i - echo * 2 + mix.Length) % mix.Length];

        var rotor = new Envelope(0.004f, 0.038f, 0.04f, 0.025f);
        var bass = new Envelope(0.006f, 0.095f, 0.30f, 0.035f);
        var pad = new Envelope(0.04f, 0.22f, 0.12f, 0.13f);
        var ping = new Envelope(0.005f, 0.08f, 0.02f, 0.08f);
        for (int bar = 0; bar < Bars; bar++)
        {
            bool hover = bar is >= 16 and < 24;
            var chord = Harmony[Chords[bar]];
            for (int s = 0; s < 16; s++)
            {
                float at = (bar * 16 + s) * tick;
                if (!hover || s % 2 == 0)
                {
                    float frequency = Synth.NoteToFrequency(chord.Notes[RotorNotes[s % RotorNotes.Length]]);
                    float accent = s % 4 == 0 ? 1f : 0.65f;
                    Synth.AddTone(mix, at, tick * 0.52f, Wave.Triangle, 0.10f * accent, rotor, frequency, wrap: true);
                    if (!hover)
                        Synth.AddTone(mix, at, tick * 0.36f, Wave.Square25, 0.020f * accent, rotor, frequency * 2, wrap: true);
                }

                // A syncopated motor underneath, relaxing to two bass notes per bar while hovering.
                if (hover ? s is 0 or 8 : s is 0 or 3 or 6 or 8 or 11 or 14)
                {
                    float frequency = Synth.NoteToFrequency(s is 6 or 14 ? chord.Fifth : chord.Root);
                    float length = tick * (hover ? 5.5f : s % 8 == 0 ? 1.8f : 0.8f);
                    Synth.AddTone(mix, at, length, Wave.Triangle, 0.29f, bass, frequency, wrap: true);
                    Synth.AddTone(mix, at, length, Wave.Sine, 0.07f, bass, frequency, wrap: true);
                }
                if (s == 0 || s == 10)
                    foreach (string note in chord.Notes.Take(3))
                        Synth.AddTone(mix, at, tick * (hover ? 5 : 2), Wave.Triangle, hover ? 0.045f : 0.028f,
                                      pad, Synth.NoteToFrequency(note) / 2, wrap: true);

                // The soft radar answer sits in a rest, so it doesn't obscure the main tune or attack warnings.
                if (s == 14 && bar % 2 == 0)
                    Synth.AddTone(mix, at, tick * 0.6f, Wave.Sine, 0.055f, ping,
                                  Synth.NoteToFrequency(chord.Notes[2]) * 2, wrap: true);

                if (hover ? s == 0 : s is 0 or 7 or 10)
                    Synth.AddTone(mix, at, 0.12f, Wave.Sine, 0.58f,
                                  new Envelope(0.003f, 0.065f, 0f, 0.025f), 155, 48, wrap: true);
                if (hover ? s == 8 : s is 4 or 12) Snare(mix, at, 1);
                if (s % 2 == 0 || (!hover && s is 7 or 15))
                    Synth.AddTone(mix, at, 0.025f, Wave.HighNoise, hover ? 0.026f : s % 4 == 2 ? 0.065f : 0.035f,
                                  new Envelope(0.002f, 0.012f, 0f, 0.009f), 1, wrap: true);
                if (bar % 8 == 7 && s is 13 or 15) Snare(mix, at, s == 13 ? 0.45f : 0.65f);
            }

            // A quiet descending glide marks the end of each flight path, especially the final dive into bar one.
            if (bar % 8 == 7)
            {
                float at = (bar * 16 + 12) * tick;
                Synth.AddTone(mix, at, tick * 3.5f, Wave.Sine, 0.065f,
                              new Envelope(0.015f, 0.10f, 0.18f, 0.06f),
                              Synth.NoteToFrequency("B5"), Synth.NoteToFrequency("B3"), wrap: true);
                Synth.AddTone(mix, at, tick * 3, Wave.HighNoise, 0.07f,
                              new Envelope(0.12f, 0.10f, 0.02f, 0.09f), 1, wrap: true);
            }
        }

        // Soften the pulse voices and remove DC. Warm both filters from the tail to keep the loop continuous.
        float smooth = 0;
        for (int i = mix.Length - 256; i < mix.Length; i++) smooth += 0.40f * (mix[i] - smooth);
        for (int i = 0; i < mix.Length; i++)
        {
            smooth += 0.40f * (mix[i] - smooth);
            mix[i] = MathF.Tanh(smooth * 1.12f);
        }
        float dc = 0;
        for (int i = mix.Length - 8192; i < mix.Length; i++) dc += 0.003f * (mix[i] - dc);
        for (int i = 0; i < mix.Length; i++)
        {
            dc += 0.003f * (mix[i] - dc);
            mix[i] -= dc;
        }
        Synth.Normalize(mix, 0.85f); // match the other music, leaving room for the boss's warning sounds
        return mix;
    }

    static void Snare(float[] mix, float at, float strength)
    {
        Synth.AddTone(mix, at, 0.095f, Wave.Noise, 0.17f * strength,
                      new Envelope(0.003f, 0.045f, 0f, 0.025f), 1, wrap: true);
        Synth.AddTone(mix, at, 0.055f, Wave.Triangle, 0.105f * strength,
                      new Envelope(0.003f, 0.03f, 0f, 0.018f), 220, 145, wrap: true);
    }
}
