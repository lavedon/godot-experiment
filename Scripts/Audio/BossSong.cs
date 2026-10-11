/// <summary>
/// "Titan Circuit" — an original boss theme: 32 bars of A-minor chiptune at 144 BPM.
/// A charging riff, a rising answer, a half-time bridge, then an octave-up final push.
/// Each melody word is an eighth note; '-' holds the previous note and '.' is a rest.
/// All releases and echoes wrap around the loop, including the final E7 turnaround.
/// </summary>
public static class BossSong
{
    public const float BeatsPerMinute = 144;
    public const int Bars = 32;

    static readonly string[] Chords =
    {
        "Am", "Am", "F", "G", "Am", "F", "Dm", "E7",
        "Am", "C", "G", "Dm", "F", "Dm", "E7", "E7",
        "Dm", "Am", "F", "E7", "Dm", "F", "E7", "E7",
        "Am", "C", "F", "G", "Dm", "F", "E7", "E7",
    };

    static readonly string[] Melody =
    {
        "A4 - A4 E5 A5 - G5 E5",   "C5 E5 A5 - G5 E5 C5 -",
        "F5 - E5 C5 A4 C5 F5 -",   "G5 - D5 G5 B5 A5 G5 E5",
        "A4 - A4 E5 A5 - C6 B5",   "A5 G5 F5 - E5 C5 A4 C5",
        "D5 - F5 A5 G5 F5 E5 D5",  "E5 - G#5 - B5 - G#5 E5",

        "A5 - E5 A5 C6 - B5 A5",   "G5 - E5 G5 C6 B5 G5 E5",
        "D5 G5 B5 - A5 G5 D5 -",   "F5 - A5 - D6 C6 A5 F5",
        "A5 - C6 - A5 G5 F5 E5",   "F5 A5 D6 - C6 A5 F5 D5",
        "E5 G#5 B5 E6 D6 B5 G#5 E5", "B5 - G#5 - E5 . E5 G#5",

        "A5 - - - F5 - E5 D5",     "E5 - - - C5 - B4 A4",
        "C5 - F5 - A5 - - -",      "G#5 - E5 - B4 - - -",
        "D5 - F5 - A5 - C6 -",     "C6 - A5 - G5 F5 E5 C5",
        "B4 E5 G#5 B5 D6 - B5 G#5", "E5 . E5 G#5 B5 D6 E6 G#5",

        "A5 - A5 E6 A6 - G6 E6",   "G6 E6 C6 - E6 G6 E6 C6",
        "F6 - E6 C6 A5 C6 F6 -",   "G6 - D6 G6 B5 D6 G6 E6",
        "D6 - F6 A6 G6 F6 E6 D6",  "C6 A5 F5 A5 C6 - A5 F5",
        "E5 G#5 B5 E6 D6 B5 G#5 E5", "B5 - G#5 E5 D5 B4 G#4 E4",
    };

    // Low root, fifth, and close chord voicings keep the lead clear.
    static readonly Dictionary<string, (string Root, string Fifth, string[] Notes)> Harmony = new()
    {
        ["Am"] = ("A2", "E3", new[] { "A3", "C4", "E4" }),
        ["C"]  = ("C3", "G3", new[] { "G3", "C4", "E4" }),
        ["F"]  = ("F2", "C3", new[] { "A3", "C4", "F4" }),
        ["G"]  = ("G2", "D3", new[] { "G3", "B3", "D4" }),
        ["Dm"] = ("D2", "A2", new[] { "A3", "D4", "F4" }),
        ["E7"] = ("E2", "B2", new[] { "G#3", "B3", "D4" }),
    };

    public static float[] Render()
    {
        // Integer sixteenth-note lengths keep every instrument and the loop on the same sample grid.
        int tickSamples = (int)(Synth.SampleRate * 60f / BeatsPerMinute / 4f);
        float tick = (float)tickSamples / Synth.SampleRate;
        var mix = new float[Bars * 16 * tickSamples];
        var lead = new float[mix.Length];
        var voice = new Envelope(0.006f, 0.18f, 0.5f, 0.07f);
        string[] notes = string.Join(' ', Melody).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (Chords.Length != Bars || Melody.Length != Bars ||
            Melody.Any(bar => bar.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length != 8))
            throw new InvalidOperationException("Every boss-song bar needs one chord and eight melody steps.");

        for (int i = 0; i < notes.Length; i++)
        {
            if (notes[i] is "-" or ".") continue;
            int held = 1;
            while (i + held < notes.Length && notes[i + held] == "-") held++;
            float at = i * 2 * tick, length = held * 2 * tick - 0.025f;
            float frequency = Synth.NoteToFrequency(notes[i]);
            bool bridge = i / 8 is >= 16 and < 24;
            Synth.AddTone(lead, at, length, Wave.Square25, bridge ? 0.14f : 0.18f, voice, frequency,
                          vibrato: 0.004f, wrap: true);
            // A quiet octave below gives the fanfare some body without another competing melody.
            Synth.AddTone(lead, at, length, Wave.Triangle, 0.09f, voice, frequency / 2, wrap: true);
        }

        int echo = tickSamples * 6; // dotted-eighth echo, quieter than the next note
        for (int i = 0; i < mix.Length; i++)
            mix[i] = lead[i] + 0.22f * lead[(i - echo + mix.Length) % mix.Length];

        var bass = new Envelope(0.004f, 0.11f, 0.55f, 0.025f);
        var stab = new Envelope(0.008f, 0.12f, 0.15f, 0.06f);
        var pluck = new Envelope(0.003f, 0.045f, 0.05f, 0.018f);
        for (int bar = 0; bar < Bars; bar++)
        {
            bool bridge = bar is >= 16 and < 24;
            var chord = Harmony[Chords[bar]];
            for (int s = 0; s < 16; s++)
            {
                float at = (bar * 16 + s) * tick;
                // A gallop (long-short-short), relaxing into quarter notes in the bridge.
                if (bridge ? s % 4 == 0 : s % 4 != 1)
                {
                    float frequency = Synth.NoteToFrequency(s % 8 == 6 ? chord.Fifth : chord.Root);
                    float length = tick * (s % 4 == 0 ? 1.6f : 0.65f);
                    Synth.AddTone(mix, at, length, Wave.Triangle, 0.30f, bass, frequency, wrap: true);
                    Synth.AddTone(mix, at, length, Wave.Square50, 0.035f, bass, frequency, wrap: true);
                }
                if (s == 0 || s == (bridge ? 8 : 6) || (!bridge && s == 12))
                    foreach (string note in chord.Notes)
                        Synth.AddTone(mix, at, bridge ? tick * 5 : tick * 1.3f, Wave.Square50,
                                      bridge ? 0.028f : 0.042f, stab, Synth.NoteToFrequency(note), wrap: true);

                // The clockwork arpeggio opens out in the answer and the final push.
                if (!bridge && (bar is >= 8 and < 16 || bar >= 24) && s % 2 == 1)
                {
                    float frequency = Synth.NoteToFrequency(chord.Notes[(s / 2) % 3]) * 4;
                    Synth.AddTone(mix, at, tick * 0.55f, Wave.Square12, 0.032f, pluck, frequency, wrap: true);
                }

                if (s == 0 || s == 10 || (!bridge && (s == 6 || s == 8)))
                    Synth.AddTone(mix, at, 0.14f, Wave.Sine, 0.65f, new Envelope(0.002f, 0.075f, 0f, 0.03f), 140, 42, wrap: true);
                if (bridge ? s == 8 : s == 4 || s == 12)
                    Snare(mix, at, 1);
                if (s % 2 == 0)
                    Synth.AddTone(mix, at, 0.035f, Wave.HighNoise, bridge ? 0.035f : (s % 4 == 0 ? 0.075f : 0.045f),
                                  new Envelope(0.001f, 0.018f, 0f, 0.012f), 1, wrap: true);
                if (bar % 4 == 3 && s >= 13)
                {
                    // Descending toms and a snare pickup announce the next four-bar phrase.
                    Synth.AddTone(mix, at, 0.10f, Wave.Sine, 0.30f, new Envelope(0.002f, 0.07f, 0f, 0.04f),
                                  180 - (s - 13) * 35, 70 - (s - 13) * 10, wrap: true);
                    if (s == 15) Snare(mix, at, 0.65f);
                }
            }
            if (bar % 8 == 0)
                Synth.AddTone(mix, bar * 16 * tick, 0.5f, Wave.HighNoise, 0.13f,
                              new Envelope(0.004f, 0.18f, 0f, 0.14f), 1, wrap: true);
        }

        // Warm the chip voices. Prime the filter with the end of the loop so its state wraps too.
        float smooth = 0;
        for (int i = mix.Length - 256; i < mix.Length; i++) smooth += 0.48f * (mix[i] - smooth);
        for (int i = 0; i < mix.Length; i++)
        {
            smooth += 0.48f * (mix[i] - smooth);
            mix[i] = MathF.Tanh(smooth * 1.15f);
        }
        // Asymmetric pulse waves carry a DC bias. A 21 Hz high-pass removes it and subsonic rumble.
        // Warm this filter from the loop tail as well, so wrapping never resets its state.
        float dc = 0;
        for (int i = mix.Length - 8192; i < mix.Length; i++) dc += 0.003f * (mix[i] - dc);
        for (int i = 0; i < mix.Length; i++)
        {
            dc += 0.003f * (mix[i] - dc);
            mix[i] -= dc;
        }
        Synth.Normalize(mix, 0.85f); // same peak as the runner theme; leave space for attack warnings
        return mix;
    }

    static void Snare(float[] mix, float at, float strength)
    {
        Synth.AddTone(mix, at, 0.12f, Wave.Noise, 0.19f * strength,
                      new Envelope(0.002f, 0.055f, 0f, 0.035f), 1, wrap: true);
        Synth.AddTone(mix, at, 0.065f, Wave.Triangle, 0.15f * strength,
                      new Envelope(0.002f, 0.04f, 0f, 0.02f), 190, 130, wrap: true);
    }
}
