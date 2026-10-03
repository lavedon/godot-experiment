/// <summary>
/// Robot Dash's theme song! Change the notes to write your own music.
///
/// How the notes work: every word is one "step" (8 steps in each bar).
///   "C5"  = play a C in octave 5 (bigger number = higher). Add # for sharp, b for flat: "F#5", "Bb4".
///   "-"   = keep holding the note before it.
///   "."   = silence.
/// Drums: "x" = hit, "." = no hit.
/// </summary>
public static class Song
{
    public const float BeatsPerMinute = 144;

    // One chord for each bar. The bass and the bouncy chords follow these.
    static readonly string[] Chords =
    {
        "C", "Am", "F", "G",    "C", "Am", "F", "G",      // part A
        "F", "G", "Em", "Am",   "F", "G", "C", "G",       // part B
    };

    // The main tune, one line per bar (it lines up with the chords above).
    static readonly string[] Melody =
    {
        "G5 - E5 G5 C6 - G5 -",     // C
        "A5 - E5 A5 C6 - A5 -",     // Am
        "F5 - A5 C6 F6 - E6 D6",    // F
        "D6 - B5 - G5 - . .",       // G
        "G5 - E5 G5 C6 - G5 -",     // C
        "A5 - E5 A5 C6 - E6 -",     // Am
        "F6 - E6 D6 C6 - A5 -",     // F
        "B5 - D6 - G6 - . .",       // G

        "A5 C6 F6 - E6 - C6 -",     // F
        "B5 D6 G6 - F6 - D6 -",     // G
        "E6 - D6 - B5 - G5 -",      // Em
        "A5 - C6 - E6 - - -",       // Am
        "F6 - E6 - C6 - A5 -",      // F
        "G5 - B5 - D6 - G6 -",      // G
        "E6 - D6 - C6 - G5 -",      // C
        "B5 - D6 - F6 - D6 B5",     // G
    };

    // Drum pattern for one bar (it repeats every bar)
    const string Kick  = "x . . x x . . .";
    const string Snare = ". . x . . . x .";
    const string HiHat = "x x x x x x x x";

    // The notes in each chord
    static readonly Dictionary<string, string[]> ChordNotes = new()
    {
        ["C"] = new[] { "C4", "E4", "G4" },
        ["Am"] = new[] { "A3", "C4", "E4" },
        ["F"] = new[] { "A3", "C4", "F4" },
        ["G"] = new[] { "G3", "B3", "D4" },
        ["Em"] = new[] { "G3", "B3", "E4" },
    };

    /// <summary>Plays the whole song into a sound that loops forever.</summary>
    public static float[] Render()
    {
        int samplesPerStep = (int)(Synth.SampleRate * 60f / BeatsPerMinute / 2f); // a step is half a beat
        float step = (float)samplesPerStep / Synth.SampleRate;                   // in seconds
        int totalSteps = Chords.Length * 8;
        var music = new float[totalSteps * samplesPerStep];
        var lead = new float[music.Length];

        // ---- Melody (with a little echo, like singing in a big room) ----
        var melodySteps = string.Join(' ', Melody).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var leadVoice = new Envelope(Attack: 0.005f, Decay: 0.25f, Sustain: 0.55f, Release: 0.05f);
        for (int i = 0; i < melodySteps.Length; i++)
        {
            string token = melodySteps[i];
            if (token == "-" || token == ".") continue;
            int length = 1;
            while (i + length < melodySteps.Length && melodySteps[i + length] == "-") length++; // count the "-" holds
            Synth.AddTone(lead, i * step, length * step - 0.02f, Wave.Square25, 0.22f, leadVoice,
                          Synth.NoteToFrequency(token), vibrato: 0.007f, wrap: true);
        }
        int echoDelay = samplesPerStep * 3;
        for (int i = 0; i < music.Length; i++)
            music[i] += lead[i] + 0.3f * lead[(i - echoDelay + music.Length) % music.Length];

        // ---- Bass and chords ----
        var bassVoice = new Envelope(0.003f, 0.15f, 0.5f, 0.02f);
        var stab = new Envelope(0.002f, 0.07f, 0f, 0.02f);
        for (int bar = 0; bar < Chords.Length; bar++)
        {
            string chord = Chords[bar];
            string root = chord.TrimEnd('m'); // "Am" -> "A"
            for (int s = 0; s < 8; s++)
            {
                float time = (bar * 8 + s) * step;
                // Bass bounces between a low note and the same note one octave up
                string bassNote = root + (s % 2 == 0 ? "2" : "3");
                Synth.AddTone(music, time, step * 0.85f, Wave.Triangle, 0.32f, bassVoice, Synth.NoteToFrequency(bassNote), wrap: true);

                // Short chord "stabs" on the off-beats
                if (s % 2 == 1)
                    foreach (string note in ChordNotes[chord])
                        Synth.AddTone(music, time, 0.08f, Wave.Square12, 0.06f, stab, Synth.NoteToFrequency(note), wrap: true);
            }
        }

        // ---- Drums ----
        var kick = Kick.Split(' ');
        var snare = Snare.Split(' ');
        var hihat = HiHat.Split(' ');
        for (int bar = 0; bar < Chords.Length; bar++)
        {
            for (int s = 0; s < 8; s++)
            {
                float time = (bar * 8 + s) * step;
                if (kick[s] == "x")
                    Synth.AddTone(music, time, 0.12f, Wave.Sine, 0.6f, new Envelope(0.001f, 0.1f, 0f, 0.02f), 150, 45, wrap: true);
                if (snare[s] == "x")
                {
                    Synth.AddTone(music, time, 0.12f, Wave.Noise, 0.22f, new Envelope(0.001f, 0.07f, 0f, 0.03f), 1, wrap: true);
                    Synth.AddTone(music, time, 0.06f, Wave.Triangle, 0.2f, new Envelope(0.001f, 0.04f, 0f, 0.02f), 190, 150, wrap: true);
                }
                if (hihat[s] == "x")
                    Synth.AddTone(music, time, 0.03f, Wave.HighNoise, s % 2 == 0 ? 0.12f : 0.07f, new Envelope(0.001f, 0.015f, 0f, 0.01f), 1, wrap: true);
            }
        }

        // ---- Finishing touches: soften harsh edges, keep it from getting too loud ----
        float smooth = 0;
        for (int i = 0; i < music.Length; i++)
        {
            smooth += 0.6f * (music[i] - smooth);  // a gentle "tone knob" that takes off a little fizz
            music[i] = MathF.Tanh(smooth * 1.2f);   // squashes the loudest bits so nothing crackles
        }
        Synth.Normalize(music, 0.85f);
        return music;
    }
}
