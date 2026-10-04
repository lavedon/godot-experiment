/// <summary>
/// All the game's sound effects, made with the Synth. Try changing the numbers and listen to what happens!
/// Frequencies: bigger number = higher sound. 440 is the note A.
/// </summary>
public static class SoundEffects
{
    static readonly Envelope Pluck = new(Attack: 0.002f, Decay: 0.08f, Sustain: 0f, Release: 0.02f);
    static readonly Envelope Held = new(Attack: 0.005f, Decay: 0.2f, Sustain: 0.7f, Release: 0.06f);

    /// <summary>"Boing!" — a quick slide upward.</summary>
    public static float[] Jump()
    {
        var sound = Synth.Silence(0.25f);
        Synth.AddTone(sound, 0, 0.16f, Wave.Square50, 0.35f, new Envelope(0.002f, 0.12f, 0.2f, 0.05f), 280, 720);
        return Done(sound);
    }

    /// <summary>A higher "boing" with sparkles for the double jump.</summary>
    public static float[] DoubleJump()
    {
        var sound = Synth.Silence(0.35f);
        Synth.AddTone(sound, 0, 0.14f, Wave.Square25, 0.3f, new Envelope(0.002f, 0.1f, 0.2f, 0.04f), 520, 1250);
        Synth.AddTone(sound, 0.07f, 0.1f, Wave.Square12, 0.15f, Pluck, Synth.NoteToFrequency("G6"));
        Synth.AddTone(sound, 0.13f, 0.12f, Wave.Square12, 0.12f, Pluck, Synth.NoteToFrequency("C7"));
        return Done(sound);
    }

    /// <summary>"Bling!" — two quick notes, like a coin.</summary>
    public static float[] Bolt()
    {
        var sound = Synth.Silence(0.32f);
        Synth.AddTone(sound, 0, 0.06f, Wave.Square25, 0.3f, Held, Synth.NoteToFrequency("B5"));
        Synth.AddTone(sound, 0.06f, 0.2f, Wave.Square25, 0.3f, new Envelope(0.002f, 0.12f, 0f, 0.03f), Synth.NoteToFrequency("E6"));
        return Done(sound);
    }

    /// <summary>A soft "thup" when landing.</summary>
    public static float[] Land()
    {
        var sound = Synth.Silence(0.12f);
        Synth.AddTone(sound, 0, 0.07f, Wave.Sine, 0.6f, new Envelope(0.002f, 0.05f, 0f, 0.02f), 140, 55);
        Synth.AddTone(sound, 0, 0.03f, Wave.Noise, 0.15f, new Envelope(0.001f, 0.015f, 0f, 0.01f), 1);
        return Done(sound, 0.7f);
    }

    /// <summary>KABOOM! Rumbly noise that gets darker and quieter, a deep boom, and a "power down" zap.</summary>
    public static float[] Explosion()
    {
        var sound = Synth.Silence(1.4f);
        var random = new Random(7);
        float low1 = 0, low2 = 0;
        for (int i = 0; i < sound.Length; i++)
        {
            float t = (float)i / Synth.SampleRate;
            // Noise through a "filter" that slowly closes, so the boom goes from "KSSH" to "rrrumble"
            float cutoff = 3000f * MathF.Exp(-t * 2.5f) + 120f;
            float amount = 1f - MathF.Exp(-MathF.Tau * cutoff / Synth.SampleRate);
            float noise = random.NextSingle() * 2f - 1f;
            low1 += amount * (noise - low1);
            low2 += amount * (low1 - low2);
            float loudness = MathF.Exp(-t * 3.2f);
            // Random crackles, like bits of metal popping
            float crackle = random.NextSingle() < 0.002f * MathF.Exp(-t * 2f) ? (random.NextSingle() * 2f - 1f) * 0.5f : 0f;
            sound[i] = low2 * loudness * 3f + crackle;
        }
        Synth.AddTone(sound, 0, 0.5f, Wave.Sine, 0.8f, new Envelope(0.002f, 0.35f, 0f, 0.1f), 110, 32);          // deep boom
        Synth.AddTone(sound, 0, 0.45f, Wave.Square25, 0.12f, new Envelope(0.002f, 0.4f, 0.3f, 0.05f), 900, 90); // robot zap
        return Done(sound, 0.95f);
    }

    /// <summary>"Tink!" — a little piece of metal bouncing (it sounds like a bell because the tones don't match nicely).</summary>
    public static float[] Clink()
    {
        var sound = Synth.Silence(0.2f);
        var ring = new Envelope(0.001f, 0.06f, 0f, 0.02f);
        Synth.AddTone(sound, 0, 0.15f, Wave.Sine, 0.4f, ring, 1900);
        Synth.AddTone(sound, 0, 0.15f, Wave.Sine, 0.25f, ring, 2870);
        Synth.AddTone(sound, 0, 0.1f, Wave.Sine, 0.15f, ring, 4410);
        Synth.AddTone(sound, 0, 0.01f, Wave.Noise, 0.3f, new Envelope(0.0005f, 0.006f, 0f, 0.002f), 1);
        return Done(sound);
    }

    /// <summary>"Bwomp!" — squishing an enemy: a low thump and a little squeak.</summary>
    public static float[] Stomp()
    {
        var sound = Synth.Silence(0.25f);
        Synth.AddTone(sound, 0, 0.1f, Wave.Sine, 0.7f, new Envelope(0.001f, 0.08f, 0f, 0.02f), 320, 70);
        Synth.AddTone(sound, 0.03f, 0.08f, Wave.Square25, 0.2f, Pluck, 700, 1100);
        Synth.AddTone(sound, 0, 0.04f, Wave.Noise, 0.25f, new Envelope(0.001f, 0.02f, 0f, 0.01f), 1);
        return Done(sound);
    }

    /// <summary>"Pew!" — the boss's blaster: a fast slide from high to low.</summary>
    public static float[] Shoot()
    {
        var sound = Synth.Silence(0.25f);
        Synth.AddTone(sound, 0, 0.18f, Wave.Square25, 0.35f, new Envelope(0.001f, 0.12f, 0.2f, 0.04f), 1400, 220);
        Synth.AddTone(sound, 0, 0.05f, Wave.Noise, 0.2f, new Envelope(0.001f, 0.03f, 0f, 0.01f), 1);
        return Done(sound);
    }

    /// <summary>"Wee-woo, wee-woo!" — a siren when the boss is coming or about to swoop.</summary>
    public static float[] BossAlarm()
    {
        var sound = Synth.Silence(1.0f);
        for (int i = 0; i < 6; i++)
            Synth.AddTone(sound, i * 0.15f, 0.14f, Wave.Square50, 0.25f, Held, i % 2 == 0 ? 880 : 660);
        return Done(sound);
    }

    /// <summary>"CLANG!" — stomping on the boss's metal head.</summary>
    public static float[] BossHit()
    {
        var sound = Synth.Silence(0.6f);
        var ring = new Envelope(0.001f, 0.18f, 0f, 0.05f);
        Synth.AddTone(sound, 0, 0.45f, Wave.Sine, 0.4f, ring, 520);
        Synth.AddTone(sound, 0, 0.4f, Wave.Sine, 0.3f, ring, 1240);
        Synth.AddTone(sound, 0, 0.3f, Wave.Sine, 0.2f, ring, 1850);
        Synth.AddTone(sound, 0, 0.12f, Wave.Sine, 0.6f, new Envelope(0.001f, 0.1f, 0f, 0.02f), 200, 60);
        Synth.AddTone(sound, 0, 0.03f, Wave.Noise, 0.3f, new Envelope(0.001f, 0.015f, 0f, 0.01f), 1);
        return Done(sound);
    }

    /// <summary>"KA-THOOM!" — Big Rusty's ground pound.</summary>
    public static float[] Slam()
    {
        var sound = Synth.Silence(0.9f);
        Synth.AddTone(sound, 0, 0.55f, Wave.Sine, 0.9f, new Envelope(0.002f, 0.35f, 0f, 0.1f), 140, 32);
        Synth.AddTone(sound, 0, 0.4f, Wave.Triangle, 0.4f, new Envelope(0.002f, 0.25f, 0f, 0.1f), 75, 40);
        Synth.AddTone(sound, 0, 0.25f, Wave.Noise, 0.35f, new Envelope(0.001f, 0.1f, 0f, 0.05f), 1);
        return Done(sound, 0.95f);
    }

    /// <summary>"Wheeeee-EEE" — the laser charging up.</summary>
    public static float[] LaserCharge()
    {
        var sound = Synth.Silence(1.0f);
        Synth.AddTone(sound, 0, 0.9f, Wave.Square12, 0.25f, Held, 200, 1600, vibrato: 0.02f);
        return Done(sound, 0.7f);
    }

    /// <summary>"BZZZZZZT!" — the laser beam.</summary>
    public static float[] LaserBeam()
    {
        var sound = Synth.Silence(0.8f);
        var beam = new Envelope(0.005f, 0.4f, 0.7f, 0.08f);
        Synth.AddTone(sound, 0, 0.7f, Wave.Square50, 0.3f, beam, 110, vibrato: 0.03f);
        Synth.AddTone(sound, 0, 0.7f, Wave.Square25, 0.2f, beam, 221.5f, vibrato: 0.03f);
        Synth.AddTone(sound, 0, 0.7f, Wave.Noise, 0.12f, beam, 1);
        return Done(sound, 0.85f);
    }

    /// <summary>"HA! HA! HA!" — Big Rusty laughing at you.</summary>
    public static float[] Laugh()
    {
        var sound = Synth.Silence(1.1f);
        float[] pitches = { 330, 300, 270 };
        for (int i = 0; i < pitches.Length; i++)
        {
            float start = i * 0.26f;
            Synth.AddTone(sound, start, 0.03f, Wave.Noise, 0.25f, new Envelope(0.001f, 0.02f, 0f, 0.01f), 1); // the "h"
            Synth.AddTone(sound, start + 0.02f, 0.17f, Wave.Square50, 0.3f, Held, pitches[i], pitches[i] * 0.85f, vibrato: 0.04f);
            Synth.AddTone(sound, start + 0.02f, 0.17f, Wave.Triangle, 0.2f, Held, pitches[i] / 2, pitches[i] * 0.42f);
        }
        return Done(sound);
    }

    /// <summary>"GRRRAAAH!" — Big Rusty getting angry.</summary>
    public static float[] Roar()
    {
        var sound = Synth.Silence(1.2f);
        Synth.AddTone(sound, 0, 0.9f, Wave.Square50, 0.3f, Held, 300, 90, vibrato: 0.05f);
        Synth.AddTone(sound, 0, 0.9f, Wave.Square25, 0.2f, Held, 152, 46, vibrato: 0.05f);
        Synth.AddTone(sound, 0, 0.6f, Wave.Noise, 0.15f, Held, 1);
        return Done(sound);
    }

    /// <summary>The victory song for beating the boss!</summary>
    public static float[] VictoryJingle()
    {
        var sound = Tune(new[] { ("G5", 0.12f), ("C6", 0.12f), ("E6", 0.12f), ("G6", 0.3f), ("E6", 0.12f), ("G6", 0.9f) },
                         Wave.Square25, normalize: false);
        Synth.AddTone(sound, 0.0f, 0.36f, Wave.Triangle, 0.3f, Held, Synth.NoteToFrequency("C3"));
        Synth.AddTone(sound, 0.36f, 0.42f, Wave.Triangle, 0.3f, Held, Synth.NoteToFrequency("G3"));
        Synth.AddTone(sound, 0.78f, 0.9f, Wave.Triangle, 0.3f, Held, Synth.NoteToFrequency("C4"));
        return Done(sound);
    }

    /// <summary>"Ready, go!" — a quick happy run of notes.</summary>
    public static float[] StartJingle() => Tune(new[] { ("C5", 0.06f), ("E5", 0.06f), ("G5", 0.06f), ("C6", 0.3f) }, Wave.Square25);

    /// <summary>"Wah, wah, wah, waaaah" — the sad trombone for game over.</summary>
    public static float[] GameOverJingle()
    {
        var notes = new[] { ("G4", 0.3f), ("F#4", 0.3f), ("F4", 0.3f), ("E4", 1.0f) };
        var sound = Synth.Silence(2.3f);
        float time = 0.1f;
        foreach (var (note, length) in notes)
        {
            float wobble = length > 0.5f ? 0.025f : 0f; // the last note wobbles sadly
            Synth.AddTone(sound, time, length - 0.04f, Wave.Square50, 0.25f, Held, Synth.NoteToFrequency(note), vibrato: wobble);
            Synth.AddTone(sound, time, length - 0.04f, Wave.Triangle, 0.2f, Held, Synth.NoteToFrequency(note) / 2, vibrato: wobble);
            time += length;
        }
        return Done(sound);
    }

    /// <summary>"Ta-da-da-DAAA!" — a fanfare for a new high score.</summary>
    public static float[] HighScoreJingle()
    {
        var sound = Tune(new[] { ("C5", 0.1f), ("E5", 0.1f), ("G5", 0.1f), ("C6", 0.25f), ("A5", 0.1f), ("C6", 0.7f) }, Wave.Square25, normalize: false);
        Synth.AddTone(sound, 0.0f, 0.3f, Wave.Triangle, 0.3f, Held, Synth.NoteToFrequency("C3"));
        Synth.AddTone(sound, 0.3f, 0.35f, Wave.Triangle, 0.3f, Held, Synth.NoteToFrequency("G3"));
        Synth.AddTone(sound, 0.65f, 0.8f, Wave.Triangle, 0.3f, Held, Synth.NoteToFrequency("C4"));
        return Done(sound);
    }

    /// <summary>"1-UP!" — a sparkly run up the notes when a new spare battery is charged. The long last note wobbles.</summary>
    public static float[] OneUp() =>
        Tune(new[] { ("C6", 0.06f), ("E6", 0.06f), ("G6", 0.06f), ("C7", 0.06f), ("E7", 0.06f), ("G7", 0.55f) }, Wave.Square25);

    /// <summary>
    /// "Vwoooop... zip zip zip... ding-DING!" — Bolt-E putting himself back together.
    /// The "vwoop" is the explosion's robot zap played backwards (low to high), the zips are pieces snapping into place,
    /// and the little bell is the face screen switching back on.
    /// </summary>
    public static float[] Rebuild()
    {
        var sound = Synth.Silence(1.2f);
        Synth.AddTone(sound, 0, 0.7f, Wave.Square25, 0.2f, Held, 90, 900); // power coming back up

        var zip = new Envelope(0.001f, 0.02f, 0f, 0.01f);
        foreach (float at in new[] { 0.05f, 0.2f, 0.32f, 0.42f, 0.5f, 0.56f })
            Synth.AddTone(sound, at, 0.03f, Wave.Noise, 0.25f, zip, 1); // a piece zips into place

        var bell = new Envelope(0.001f, 0.18f, 0f, 0.05f);
        Synth.AddTone(sound, 0.75f, 0.12f, Wave.Sine, 0.4f, bell, Synth.NoteToFrequency("E6"));
        Synth.AddTone(sound, 0.82f, 0.3f, Wave.Sine, 0.4f, bell, Synth.NoteToFrequency("B6"));
        return Done(sound);
    }

    // ---------- Powers from the rainbow ? boxes ----------

    /// <summary>"Ta-da-da-da-da-DAAA!" — a sparkly run up the notes when a power starts, with a low note underneath.</summary>
    public static float[] PowerUp()
    {
        var sound = Tune(new[] { ("C6", 0.07f), ("E6", 0.07f), ("G6", 0.07f), ("C7", 0.07f), ("E7", 0.07f), ("G7", 0.35f) },
                         Wave.Square25, normalize: false);
        Synth.AddTone(sound, 0, 0.5f, Wave.Triangle, 0.3f, Held, Synth.NoteToFrequency("C4"));
        return Done(sound);
    }

    /// <summary>"Tick!" — a tiny click for each flick of the slot machine (and the "almost over" clock).</summary>
    public static float[] Tick()
    {
        var sound = Synth.Silence(0.06f);
        Synth.AddTone(sound, 0, 0.025f, Wave.Square12, 0.3f, Pluck, 1500);
        return Done(sound);
    }

    /// <summary>"SMASH!" — a crunchy crack, a low thud, and a little metal "ting".</summary>
    public static float[] Smash()
    {
        var sound = Synth.Silence(0.3f);
        Synth.AddTone(sound, 0, 0.12f, Wave.Noise, 0.5f, new Envelope(0.001f, 0.06f, 0f, 0.03f), 1);   // crack!
        Synth.AddTone(sound, 0, 0.15f, Wave.Triangle, 0.5f, new Envelope(0.002f, 0.1f, 0f, 0.03f), 220, 90); // thud
        Synth.AddTone(sound, 0, 0.1f, Wave.Sine, 0.2f, new Envelope(0.001f, 0.06f, 0f, 0.02f), 1900);     // ting
        return Done(sound);
    }

    /// <summary>"Bwip-bwip-BWIIIP!" — growing into MEGA BOLT-E (three slides up, each one bigger, like Mario).</summary>
    public static float[] Grow()
    {
        var sound = Synth.Silence(0.55f);
        Synth.AddTone(sound, 0, 0.1f, Wave.Square50, 0.3f, Held, 300, 500);
        Synth.AddTone(sound, 0.12f, 0.1f, Wave.Square50, 0.3f, Held, 400, 650);
        Synth.AddTone(sound, 0.25f, 0.2f, Wave.Square50, 0.3f, Held, 500, 1000);
        return Done(sound);
    }

    /// <summary>"Pfffft..." — shrinking back to normal: a slide down, like air coming out of a balloon.</summary>
    public static float[] Shrink()
    {
        var sound = Synth.Silence(0.6f);
        Synth.AddTone(sound, 0, 0.45f, Wave.Square50, 0.3f, Held, 700, 150);
        Synth.AddTone(sound, 0, 0.4f, Wave.Noise, 0.15f, new Envelope(0.02f, 0.3f, 0.3f, 0.1f), 1); // the "fffft" air
        return Done(sound);
    }

    /// <summary>"FWOOOOSH!" — the rocket board blasting off: a roar of noise and a whistle going up.</summary>
    public static float[] Rocket()
    {
        var sound = Synth.Silence(1.1f);
        Synth.AddTone(sound, 0, 0.8f, Wave.Noise, 0.3f, new Envelope(0.05f, 0.5f, 0.4f, 0.2f), 1);
        Synth.AddTone(sound, 0, 0.6f, Wave.Square12, 0.2f, Held, 300, 1200);
        return Done(sound);
    }

    // ---------- The World Tour ----------

    /// <summary>
    /// "Ta-da-da-da-da-DAAA... twinkle twinkle!" — welcome to a new world! A happy run up to a long high note,
    /// a bouncy bass underneath, and 6 little sparkles at the end (always the same "random" ones).
    /// </summary>
    public static float[] NewWorld()
    {
        var sound = Tune(new[] { ("C6", 0.1f), ("G5", 0.1f), ("C6", 0.1f), ("E6", 0.1f), ("G6", 0.15f), ("C7", 0.55f) },
                         Wave.Square25, normalize: false);
        Synth.AddTone(sound, 0.0f, 0.3f, Wave.Triangle, 0.3f, Held, Synth.NoteToFrequency("C3"));
        Synth.AddTone(sound, 0.3f, 0.15f, Wave.Triangle, 0.3f, Held, Synth.NoteToFrequency("G3"));
        Synth.AddTone(sound, 0.45f, 0.5f, Wave.Triangle, 0.3f, Held, Synth.NoteToFrequency("C4"));

        // Sparkles: high notes from C7 up to C8 (only the happy ones: C, D, E, G, A and the top C)
        int[] happySteps = { 0, 2, 4, 7, 9, 12 };
        var random = new Random(3);
        for (int i = 0; i < 6; i++)
        {
            float at = 0.5f + random.NextSingle() * 0.4f; // somewhere between 0.5 and 0.9 seconds
            int key = Synth.NoteToKey("C7") + happySteps[random.Next(happySteps.Length)];
            Synth.AddTone(sound, at, 0.04f, Wave.Square12, 0.12f, Pluck, Synth.KeyToFrequency(key));
        }
        return Done(sound);
    }

    // ---------- Bolt-E's looks ----------

    /// <summary>
    /// "Ding-ding-ding-ding-DIIING... bling bling!" — a NEW LOOK! A quick run up to a high note,
    /// and two little bells ringing on top.
    /// </summary>
    public static float[] Unlock()
    {
        var sound = Tune(new[] { ("C6", 0.06f), ("E6", 0.06f), ("G6", 0.06f), ("C7", 0.06f), ("E7", 0.3f) },
                         Wave.Square12, normalize: false);
        var bell = new Envelope(0.001f, 0.25f, 0f, 0.05f);
        Synth.AddTone(sound, 0.24f, 0.4f, Wave.Sine, 0.2f, bell, 1568); // (a G)
        Synth.AddTone(sound, 0.24f, 0.4f, Wave.Sine, 0.2f, bell, 2637); // (an E, higher up)
        return Done(sound);
    }

    // ---------- The family race ----------

    /// <summary>
    /// "Ta-da-DA!" — zooming past somebody's flag (or your own record). A happy cheer, never a "nah-nah" taunt:
    /// a quick run up to a high note, a little crowd going "yaaay" (soft noise), and a low note underneath.
    /// </summary>
    public static float[] PassFlag()
    {
        var sound = Tune(new[] { ("C6", 0.08f), ("E6", 0.08f), ("G6", 0.08f), ("C7", 0.3f) }, Wave.Square25, normalize: false);
        Synth.AddTone(sound, 0, 0.5f, Wave.Noise, 0.08f, new Envelope(0.05f, 0.3f, 0.3f, 0.1f), 1);   // the crowd
        Synth.AddTone(sound, 0, 0.4f, Wave.Triangle, 0.3f, Held, Synth.NoteToFrequency("C4"));          // the low note
        return Done(sound);
    }

    /// <summary>Plays a list of (note, seconds) one after another.</summary>
    static float[] Tune((string Note, float Length)[] notes, Wave wave, bool normalize = true)
    {
        float total = 0.1f;
        foreach (var (_, length) in notes) total += length;
        var sound = Synth.Silence(total + 0.4f);
        float time = 0;
        foreach (var (note, length) in notes)
        {
            Synth.AddTone(sound, time, length - 0.01f, wave, 0.3f, Held, Synth.NoteToFrequency(note), vibrato: length > 0.5f ? 0.01f : 0f);
            time += length;
        }
        return normalize ? Done(sound) : sound;
    }

    static float[] Done(float[] sound, float peak = 0.8f)
    {
        Synth.Normalize(sound, peak);
        return sound;
    }
}
