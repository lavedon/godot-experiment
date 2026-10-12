/// <summary>
/// "Island Hop": the WORLD MAP's own tune (it plays while the map is up and the game waits behind it, frozen).
/// It's our own bouncy overworld tune: 16 bars in F major at 112 BPM, then it starts again (one loop is 34 seconds).
/// A marimba plays the melody, a tuba plays the bass, a ukulele strums the chords, and bongos, a soft kick drum and a
/// shaker keep the beat. Every melody word is an eighth note: "-" holds the note before, and "." is a little rest.
/// Try changing the melody! (Every bar needs exactly 8 words, or the game says so when it starts.)
/// </summary>
public static class MapSong
{
    public const string Title = "Island Hop";
    public const float BeatsPerMinute = 112;
    public const int Bars = 16;

    static readonly string[] Chords =
    {
        "F", "F", "Bb", "F",   "Gm", "C", "F", "C",     // A: hop, hop, over the hill, and home
        "Dm", "Am", "Bb", "F", "Gm", "C7", "F", "C7",   // B: the sea, the clouds, the rocket... and around again
    };

    static readonly string[] Melody =
    {
        "F5 - A5 C6 - A5 F5 .",     "G5 A5 G5 F5 C5 - . .",     "D6 - Bb5 D6 - Bb5 F5 .", "C6 - A5 F5 A5 - . .",
        "Bb5 - D6 Bb5 G5 - A5 Bb5", "C6 - E5 G5 C6 - Bb5 G5",   "A5 - F5 A5 C6 - F6 -",   "E6 - - D6 C6 - . .",
        "D6 - F6 D6 A5 - D6 .",     "C6 - E6 C6 A5 - C6 .",     "Bb5 - D6 F6 D6 - Bb5 .", "A5 C6 F6 - C6 A5 F5 .",
        "G5 Bb5 D6 - Bb5 G5 D5 .",  "E5 G5 C6 - E6 - D6 C6",    "F6 - C6 A5 F5 - A5 C6",  "G5 - - E5 C5 - . .",
    };

    /// <summary>Each chord: its low note (root), the note five steps up (fifth), and the three notes the ukulele strums.</summary>
    static readonly Dictionary<string, (string Root, string Fifth, string[] Notes)> Harmony = new()
    {
        ["F"] = ("F2", "C3", new[] { "A3", "C4", "F4" }),
        ["Bb"] = ("Bb1", "F2", new[] { "Bb3", "D4", "F4" }),
        ["Gm"] = ("G2", "D3", new[] { "Bb3", "D4", "G4" }),
        ["C"] = ("C2", "G2", new[] { "G3", "C4", "E4" }),
        ["C7"] = ("C2", "G2", new[] { "G3", "Bb3", "E4" }),
        ["Dm"] = ("D2", "A2", new[] { "A3", "D4", "F4" }),
        ["Am"] = ("A2", "E3", new[] { "A3", "C4", "E4" }),
    };

    public static float[] Render()
    {
        if (Chords.Length != Bars || Melody.Length != Bars ||
            Melody.Any(bar => bar.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length != 8))
            throw new InvalidOperationException("Every Island Hop bar needs one chord and eight melody steps.");

        int stepSamples = (int)MathF.Round(Synth.SampleRate * 60f / BeatsPerMinute / 2f); // one eighth note
        float step = (float)stepSamples / Synth.SampleRate;
        var mix = new float[Bars * 8 * stepSamples];
        var lead = new float[mix.Length];

        // The marimba: a soft sine with a little triangle, and a tiny "tock" when the mallet hits
        var marimba = new Envelope(0.003f, 0.18f, 0.15f, 0.06f);
        var tock = new Envelope(0.001f, 0.012f, 0f, 0.01f);
        string[] notes = string.Join(' ', Melody).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < notes.Length; i++)
        {
            if (notes[i] is "-" or ".") continue;
            int held = 1;
            while (i + held < notes.Length && notes[i + held] == "-") held++;
            float at = i * step, length = held * step - 0.02f;
            float frequency = Synth.NoteToFrequency(notes[i]);
            Synth.AddTone(lead, at, length, Wave.Sine, 0.20f, marimba, frequency, wrap: true);
            Synth.AddTone(lead, at, length, Wave.Triangle, 0.06f, marimba, frequency, wrap: true);
            Synth.AddTone(lead, at, 0.03f, Wave.Sine, 0.04f, tock, frequency * 4, wrap: true);
        }

        var tuba = new Envelope(0.006f, 0.12f, 0.35f, 0.05f);
        var uke = new Envelope(0.003f, 0.07f, 0.1f, 0.05f);
        var drum = new Envelope(0.002f, 0.05f, 0f, 0.02f);
        var shakerDice = new Random(112); // (the shaker's own noise, so the other songs' noise stays the same)
        for (int bar = 0; bar < Bars; bar++)
        {
            var chord = Harmony[Chords[bar]];
            float root = Synth.NoteToFrequency(chord.Root), fifth = Synth.NoteToFrequency(chord.Fifth);
            for (int s = 0; s < 8; s++)
            {
                float at = (bar * 8 + s) * step;

                // The tuba: the root on steps 0 and 4, the fifth on 2 and 6, and a walk up into the next part
                float bass = s is 0 or 4 ? root : s is 2 or 6 ? fifth : 0;
                if (s == 7 && bar % 4 == 3)
                    bass = Synth.KeyToFrequency(Synth.NoteToKey(Harmony[Chords[(bar + 1) % Bars]].Root) - 2); // a whole step below
                if (bass > 0)
                {
                    Synth.AddTone(mix, at, 0.18f, Wave.Triangle, 0.30f, tuba, bass, wrap: true);
                    Synth.AddTone(mix, at, 0.18f, Wave.Sine, 0.06f, tuba, bass, wrap: true);
                }

                // The ukulele strums the chord on steps 2 and 6 (one string a tiny bit after the other)
                if (s is 2 or 6)
                    for (int n = 0; n < chord.Notes.Length; n++)
                        Synth.AddTone(mix, at + n * 0.012f, 0.15f, Wave.Triangle, 0.05f, uke, Synth.NoteToFrequency(chord.Notes[n]), wrap: true);

                // The bongos, the soft kick and the shaker
                if (s is 1 or 3 or 5) Synth.AddTone(mix, at, 0.07f, Wave.Sine, 0.22f, drum, 380, 300, wrap: true);
                if (s is 6 or 7) Synth.AddTone(mix, at, 0.09f, Wave.Sine, 0.25f, drum, 250, 200, wrap: true);
                if (s is 0 or 4) Synth.AddTone(mix, at, 0.1f, Wave.Sine, 0.4f, drum, 120, 50, wrap: true);
                Shake(mix, at, s % 2 == 1 ? 0.08f : 0.05f, shakerDice);
            }
        }

        // The marimba, with a light echo (3 steps later) that wraps around into the first bar
        int echo = stepSamples * 3;
        for (int i = 0; i < mix.Length; i++)
            mix[i] += lead[i] + 0.25f * lead[(i - echo + mix.Length) % mix.Length];

        // Smooth it, round off the loudest bits, and take away any slow drift: the filters start from the END of the
        // song, so where the loop starts again there's no click
        float smooth = 0;
        for (int i = mix.Length - 256; i < mix.Length; i++) smooth += 0.35f * (mix[i] - smooth);
        for (int i = 0; i < mix.Length; i++)
        {
            smooth += 0.35f * (mix[i] - smooth);
            mix[i] = MathF.Tanh(smooth * 1.1f);
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

    /// <summary>"Tss": one shake of the shaker (a very short hiss), with its own noise.</summary>
    static void Shake(float[] mix, float at, float volume, Random dice)
    {
        int first = (int)(at * Synth.SampleRate);
        int count = (int)(0.035f * Synth.SampleRate);
        float last = 0;
        for (int i = 0; i < count; i++)
        {
            float noise = dice.NextSingle() * 2f - 1f;
            float t = (float)i / count;
            mix[(first + i) % mix.Length] += (noise - last) * 0.5f * volume * (1 - t) * (1 - t);
            last = noise;
        }
    }
}
