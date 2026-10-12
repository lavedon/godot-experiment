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

    // ---------- VROOMBA, the giant robot vacuum ----------

    /// <summary>"Vrrrr-RRRRM!" — VROOMBA revving up before a zoom: a buzzy engine going up, with puttering and dust.</summary>
    public static float[] RevUp()
    {
        var sound = Synth.Silence(1.05f);
        Synth.AddTone(sound, 0, 0.9f, Wave.Saw, 0.3f, Held, 70, 260);          // the engine, getting faster
        Synth.AddTone(sound, 0, 0.9f, Wave.Square25, 0.12f, Held, 140, 520);   // ...and higher
        Synth.AddTone(sound, 0, 0.9f, Wave.Noise, 0.1f, new Envelope(0.1f, 0.5f, 0.6f, 0.05f), 1); // dust kicking up
        for (int i = 0; i < 9; i++)
            Synth.AddTone(sound, i * 0.1f, 0.03f, Wave.Noise, 0.15f, Pluck, 1); // putt-putt-putt
        return Done(sound);
    }

    /// <summary>"VROOOOOM!" — VROOMBA zooming across: a whoosh of air and a buzzy engine going past.</summary>
    public static float[] Zoom()
    {
        var sound = Synth.Silence(0.8f);
        Synth.AddTone(sound, 0, 0.6f, Wave.Noise, 0.35f, new Envelope(0.08f, 0.25f, 0.3f, 0.1f), 1);
        Synth.AddTone(sound, 0, 0.55f, Wave.Saw, 0.25f, Held, 420, 180);
        Synth.AddTone(sound, 0, 0.55f, Wave.Square25, 0.1f, Held, 840, 360);
        return Done(sound);
    }

    /// <summary>"THUD! Boi-oi-oing!" — VROOMBA bonking into the wall: a deep thud, a knock, and a wobbly boing.</summary>
    public static float[] WallBonk()
    {
        var sound = Synth.Silence(0.8f);
        Synth.AddTone(sound, 0, 0.18f, Wave.Sine, 0.9f, new Envelope(0.001f, 0.12f, 0f, 0.03f), 160, 45);   // THUD
        Synth.AddTone(sound, 0, 0.04f, Wave.Noise, 0.35f, new Envelope(0.001f, 0.02f, 0f, 0.01f), 1);       // the knock
        Synth.AddTone(sound, 0.06f, 0.5f, Wave.Triangle, 0.5f, new Envelope(0.002f, 0.3f, 0.2f, 0.08f), 170, 420, vibrato: 0.12f); // boing!
        return Done(sound, 0.9f);
    }

    /// <summary>
    /// "Hmmmmmm... wsh wsh wsh" — VROOMBA's vacuum: a low buzzy hum, the hiss of the air, and 4 gusts of air rushing in
    /// every second. It's exactly 1 second long and it LOOPS (the hums fit in it exactly 105 and 210 times, so you can't
    /// hear where it starts again), so it hums for as long as the vacuum is on.
    /// </summary>
    public static float[] VacuumHum()
    {
        var sound = Synth.Silence(1f);
        var steady = new Envelope(0f, 1f, 1f, 0.001f); // no fading in or out, so the loop is smooth
        Synth.AddTone(sound, 0, 1f, Wave.Saw, 0.22f, steady, 105);
        Synth.AddTone(sound, 0, 1f, Wave.Saw, 0.12f, steady, 210);
        Synth.AddTone(sound, 0, 1f, Wave.HighNoise, 0.3f, steady, 1);
        var gust = new Envelope(0.04f, 0.08f, 0.3f, 0.06f);
        for (int i = 0; i < 4; i++)
            Synth.AddTone(sound, 0.05f + i * 0.25f, 0.12f, Wave.Noise, 0.14f, gust, 1); // wsh!
        return Done(sound, 0.7f);
    }

    /// <summary>"BEEP BOOP! bip bip" — VROOMBA being happy (and a bit cheeky) when Bolt-E crashes.</summary>
    public static float[] BeepBoop()
    {
        var sound = Synth.Silence(0.75f);
        var beep = new Envelope(0.004f, 0.1f, 0.6f, 0.03f);
        Synth.AddTone(sound, 0, 0.13f, Wave.Square25, 0.3f, beep, Synth.NoteToFrequency("E6"));                                  // BEEP
        Synth.AddTone(sound, 0.18f, 0.2f, Wave.Square50, 0.3f, beep, Synth.NoteToFrequency("A5"), Synth.NoteToFrequency("E5"));   // BOOP
        Synth.AddTone(sound, 0.42f, 0.06f, Wave.Square12, 0.2f, Pluck, Synth.NoteToFrequency("C7"));                               // bip
        Synth.AddTone(sound, 0.5f, 0.06f, Wave.Square12, 0.2f, Pluck, Synth.NoteToFrequency("G7"));                                // bip!
        return Done(sound);
    }

    /// <summary>"Ptoo!" — VROOMBA spitting out a dust bunny: a little pop, a slide up, and a puff of fluff.</summary>
    public static float[] Spit()
    {
        var sound = Synth.Silence(0.35f);
        Synth.AddTone(sound, 0, 0.03f, Wave.Noise, 0.4f, new Envelope(0.001f, 0.015f, 0f, 0.01f), 1);    // "p"
        Synth.AddTone(sound, 0.02f, 0.14f, Wave.Square50, 0.3f, Held, 380, 900);                          // "too!"
        Synth.AddTone(sound, 0.02f, 0.18f, Wave.Noise, 0.15f, new Envelope(0.005f, 0.08f, 0.1f, 0.04f), 1); // fluff
        return Done(sound);
    }

    // ---------- DRONEZILLA, the giant attack drone ----------

    /// <summary>
    /// "Bzzzzzzz" — DRONEZILLA's rotors: a low buzzy drone and the "wup-wup-wup" of the blades chopping the air,
    /// 30 chops every second. It's exactly 1 second long and it LOOPS (the buzzes fit in it exactly 120 and 181 times,
    /// so you can't hear where it starts again), so it buzzes for as long as he flies.
    /// </summary>
    public static float[] RotorBuzz()
    {
        var sound = Synth.Silence(1f);
        var steady = new Envelope(0f, 1f, 1f, 0.001f); // no fading in or out, so the loop is smooth
        Synth.AddTone(sound, 0, 1f, Wave.Saw, 0.2f, steady, 120);
        Synth.AddTone(sound, 0, 1f, Wave.Square25, 0.08f, steady, 181);
        var chop = new Envelope(0.002f, 0.012f, 0f, 0.005f);
        for (int i = 0; i < 30; i++)
            Synth.AddTone(sound, i / 30f, 0.015f, Wave.Noise, 0.3f, chop, 1); // wup!
        return Done(sound, 0.7f);
    }

    /// <summary>"Wheeeeeeoooo" — a bomb falling: a whistle sliding down.</summary>
    public static float[] BombWhistle()
    {
        var sound = Synth.Silence(0.85f);
        Synth.AddTone(sound, 0, 0.75f, Wave.Sine, 0.4f, new Envelope(0.05f, 0.5f, 0.7f, 0.06f), 1900, 700);
        Synth.AddTone(sound, 0, 0.75f, Wave.Triangle, 0.12f, new Envelope(0.05f, 0.5f, 0.7f, 0.06f), 3800, 1400);
        return Done(sound, 0.7f);
    }

    /// <summary>"Beep beep booop" — LOW BATTERY! Three beeps going down, the last one long and sad.</summary>
    public static float[] LowBattery()
    {
        var sound = Synth.Silence(0.8f);
        var beep = new Envelope(0.003f, 0.08f, 0.7f, 0.03f);
        Synth.AddTone(sound, 0, 0.1f, Wave.Square50, 0.3f, beep, Synth.NoteToFrequency("B5"));
        Synth.AddTone(sound, 0.16f, 0.1f, Wave.Square50, 0.3f, beep, Synth.NoteToFrequency("G5"));
        Synth.AddTone(sound, 0.32f, 0.35f, Wave.Square50, 0.3f, beep, Synth.NoteToFrequency("E5"), Synth.NoteToFrequency("C5"));
        return Done(sound);
    }

    /// <summary>"Wheeeoowwww... clunk" — DRONEZILLA's rotors powering down when he lands.</summary>
    public static float[] PowerDown()
    {
        var sound = Synth.Silence(1f);
        Synth.AddTone(sound, 0, 0.8f, Wave.Saw, 0.25f, Held, 420, 55);
        Synth.AddTone(sound, 0, 0.8f, Wave.Square25, 0.1f, Held, 840, 110);
        Synth.AddTone(sound, 0.82f, 0.04f, Wave.Noise, 0.3f, Pluck, 1); // clunk
        return Done(sound);
    }

    /// <summary>"Vrrrr-WHIIIR!" — DRONEZILLA's rotors whirring up as he takes off (and the wind they blow).</summary>
    public static float[] TakeOff()
    {
        var sound = Synth.Silence(0.9f);
        Synth.AddTone(sound, 0, 0.75f, Wave.Saw, 0.25f, Held, 60, 240);
        Synth.AddTone(sound, 0, 0.75f, Wave.Square25, 0.1f, Held, 120, 480);
        Synth.AddTone(sound, 0, 0.75f, Wave.Noise, 0.12f, new Envelope(0.2f, 0.4f, 0.6f, 0.08f), 1);
        return Done(sound);
    }

    /// <summary>"BZZT BZZT! dee-doo!" — DRONEZILLA being cheeky when Bolt-E crashes: two buzzes and a little tune.</summary>
    public static float[] Bzzt()
    {
        var sound = Synth.Silence(0.95f);
        var buzz = new Envelope(0.004f, 0.1f, 0.8f, 0.03f);
        for (int i = 0; i < 2; i++)
        {
            float start = i * 0.26f;
            Synth.AddTone(sound, start, 0.17f, Wave.Saw, 0.3f, buzz, 150, 135);        // BZZ...
            Synth.AddTone(sound, start, 0.17f, Wave.Square25, 0.15f, buzz, 301, 270);
            Synth.AddTone(sound, start + 0.15f, 0.03f, Wave.Noise, 0.3f, Pluck, 1);   // ...T!
        }
        Synth.AddTone(sound, 0.58f, 0.09f, Wave.Square12, 0.2f, Pluck, Synth.NoteToFrequency("G6")); // dee
        Synth.AddTone(sound, 0.68f, 0.14f, Wave.Square12, 0.2f, Pluck, Synth.NoteToFrequency("C7")); // doo!
        return Done(sound);
    }

    // ---------- OPTI-MAX, the humanoid robot ----------

    /// <summary>"CLANK-thud" — one of OPTI-MAX's heavy metal footsteps: a low thud and a little metal ping.</summary>
    public static float[] RobotStep()
    {
        var sound = Synth.Silence(0.3f);
        Synth.AddTone(sound, 0, 0.12f, Wave.Sine, 0.8f, new Envelope(0.001f, 0.09f, 0f, 0.03f), 130, 50);          // thud
        Synth.AddTone(sound, 0, 0.03f, Wave.Noise, 0.3f, new Envelope(0.001f, 0.02f, 0f, 0.01f), 1);              // the knock
        Synth.AddTone(sound, 0.01f, 0.12f, Wave.Triangle, 0.25f, new Envelope(0.001f, 0.1f, 0f, 0.03f), 1250);     // clank!
        Synth.AddTone(sound, 0.01f, 0.08f, Wave.Square12, 0.08f, new Envelope(0.001f, 0.06f, 0f, 0.02f), 1870);
        return Done(sound, 0.75f);
    }

    /// <summary>"Fwoooosh" — a cardboard box flying through the air.</summary>
    public static float[] BoxWhoosh()
    {
        var sound = Synth.Silence(0.55f);
        Synth.AddTone(sound, 0, 0.42f, Wave.Noise, 0.3f, new Envelope(0.12f, 0.2f, 0.35f, 0.1f), 1);
        Synth.AddTone(sound, 0, 0.4f, Wave.Sine, 0.2f, new Envelope(0.1f, 0.2f, 0.3f, 0.08f), 320, 160);
        return Done(sound, 0.6f);
    }

    /// <summary>"Crrrunch!" — a cardboard box landing (or breaking): crinkly cardboard and a soft thud.</summary>
    public static float[] BoxCrunch()
    {
        var sound = Synth.Silence(0.4f);
        Synth.AddTone(sound, 0, 0.12f, Wave.Sine, 0.6f, new Envelope(0.001f, 0.1f, 0f, 0.03f), 150, 60);
        float[] crinkles = { 0f, 0.035f, 0.08f, 0.13f, 0.19f };
        foreach (float at in crinkles)
            Synth.AddTone(sound, at, 0.03f, Wave.HighNoise, 0.5f, new Envelope(0.001f, 0.025f, 0f, 0.01f), 1);
        Synth.AddTone(sound, 0, 0.2f, Wave.Noise, 0.15f, new Envelope(0.002f, 0.15f, 0f, 0.03f), 1);
        return Done(sound);
    }

    /// <summary>"Boi-oing!" — the soccer ball bouncing.</summary>
    public static float[] Boing()
    {
        var sound = Synth.Silence(0.4f);
        Synth.AddTone(sound, 0, 0.3f, Wave.Triangle, 0.5f, new Envelope(0.002f, 0.22f, 0.1f, 0.05f), 160, 420, vibrato: 0.1f);
        Synth.AddTone(sound, 0, 0.05f, Wave.Sine, 0.4f, new Envelope(0.001f, 0.04f, 0f, 0.01f), 120, 60);
        return Done(sound, 0.7f);
    }

    /// <summary>"THWACK!" — OPTI-MAX kicking the ball.</summary>
    public static float[] Kick()
    {
        var sound = Synth.Silence(0.25f);
        Synth.AddTone(sound, 0, 0.1f, Wave.Sine, 0.8f, new Envelope(0.001f, 0.08f, 0f, 0.02f), 220, 70);
        Synth.AddTone(sound, 0, 0.04f, Wave.Noise, 0.45f, new Envelope(0.001f, 0.03f, 0f, 0.01f), 1);
        return Done(sound);
    }

    /// <summary>"Bip-bweep" — a little computer chirp while OPTI-MAX updates his software.</summary>
    public static float[] Chirp()
    {
        var sound = Synth.Silence(0.18f);
        Synth.AddTone(sound, 0, 0.045f, Wave.Square12, 0.25f, Pluck, 1700, 2300);
        Synth.AddTone(sound, 0.06f, 0.05f, Wave.Square25, 0.2f, Pluck, 1300);
        return Done(sound, 0.5f);
    }

    /// <summary>"DING!" — OPTI-MAX's update is finished.</summary>
    public static float[] Ding()
    {
        var sound = Synth.Silence(1f);
        var ring = new Envelope(0.002f, 0.7f, 0f, 0.2f);
        Synth.AddTone(sound, 0, 0.75f, Wave.Sine, 0.5f, ring, Synth.NoteToFrequency("A6"));
        Synth.AddTone(sound, 0, 0.5f, Wave.Sine, 0.2f, ring, Synth.NoteToFrequency("E7"));
        Synth.AddTone(sound, 0, 0.75f, Wave.Triangle, 0.25f, ring, Synth.NoteToFrequency("A5"));
        return Done(sound, 0.7f);
    }

    /// <summary>"BZZRT-BZZRT!" — the error buzz when OPTI-MAX is stomped in the middle of his update.</summary>
    public static float[] ErrorBuzz()
    {
        var sound = Synth.Silence(0.6f);
        var buzz = new Envelope(0.004f, 0.05f, 0.8f, 0.03f);
        for (int i = 0; i < 2; i++)
        {
            Synth.AddTone(sound, i * 0.22f, 0.16f, Wave.Saw, 0.3f, buzz, 110, 95);
            Synth.AddTone(sound, i * 0.22f, 0.16f, Wave.Square50, 0.18f, buzz, 222, 190);
        }
        return Done(sound);
    }

    /// <summary>"Boots-n-beeps!" — OPTI-MAX's little dance beat: drums and a bleepy robot tune.</summary>
    public static float[] DanceBeat()
    {
        var sound = Synth.Silence(1.4f);
        const float beat = 0.15f;
        for (int i = 0; i < 8; i++)
        {
            float at = i * beat;
            if (i % 2 == 0) Synth.AddTone(sound, at, 0.08f, Wave.Sine, 0.6f, new Envelope(0.001f, 0.07f, 0f, 0.02f), 140, 50); // boots
            else Synth.AddTone(sound, at, 0.03f, Wave.HighNoise, 0.25f, Pluck, 1);                                             // ts
        }
        string[] tune = { "C6", "G5", "C6", "E6", "D6", "G5", "D6", "C7" }; // beep bop beep beep...
        for (int i = 0; i < tune.Length; i++)
            Synth.AddTone(sound, i * beat, beat * 0.7f, Wave.Square25, 0.18f, Pluck with { Decay = 0.12f }, Synth.NoteToFrequency(tune[i]));
        return Done(sound);
    }

    // ---------- UPSIDE-DOWN STATION ----------

    /// <summary>
    /// "FWOOOSH!" — flipping gravity: rushing air that sweeps UP (flipping up to the ceiling) or DOWN (back to the floor),
    /// with a sliding tone and a little sparkle on top.
    /// </summary>
    public static float[] FlipWhoosh(bool up)
    {
        const float length = 0.34f;
        var sound = Synth.Silence(length + 0.1f);
        var random = new Random(up ? 21 : 22);
        float low = 0;
        for (int i = 0; i < (int)(length * Synth.SampleRate); i++)
        {
            float t = (float)i / Synth.SampleRate / length; // 0 to 1
            float sweep = up ? t : 1 - t;
            // Noise through a "filter" that opens (up) or closes (down): "shhh" getting brighter or darker
            float cutoff = 300f + 4500f * sweep * sweep;
            float amount = 1f - MathF.Exp(-MathF.Tau * cutoff / Synth.SampleRate);
            low += amount * (random.NextSingle() * 2f - 1f - low);
            float loudness = MathF.Sin(MathF.PI * t); // swells in and out
            sound[i] = low * loudness * 0.9f;
        }
        Synth.AddTone(sound, 0, length * 0.8f, Wave.Sine, 0.35f, new Envelope(0.02f, 0.2f, 0.4f, 0.05f), up ? 260 : 900, up ? 900 : 260);
        Synth.AddTone(sound, length * 0.55f, 0.08f, Wave.Square12, 0.08f, Pluck, Synth.NoteToFrequency(up ? "E7" : "G6"));
        return Done(sound, 0.75f);
    }

    /// <summary>"Tunk!" — landing on the floor or the ceiling: a little metal thud.</summary>
    public static float[] FlipThud()
    {
        var sound = Synth.Silence(0.2f);
        Synth.AddTone(sound, 0, 0.08f, Wave.Sine, 0.7f, new Envelope(0.001f, 0.06f, 0f, 0.02f), 180, 70);
        Synth.AddTone(sound, 0, 0.06f, Wave.Sine, 0.2f, new Envelope(0.001f, 0.04f, 0f, 0.02f), 1300);
        Synth.AddTone(sound, 0, 0.02f, Wave.Noise, 0.25f, new Envelope(0.001f, 0.01f, 0f, 0.01f), 1);
        return Done(sound, 0.65f);
    }

    /// <summary>"Wheeeeooooo..." — falling out of the station through a gap: a slide whistle going all the way down.</summary>
    public static float[] SpaceFall()
    {
        var sound = Synth.Silence(0.7f);
        Synth.AddTone(sound, 0, 0.55f, Wave.Sine, 0.5f, new Envelope(0.01f, 0.4f, 0.6f, 0.1f), 1500, 180);
        Synth.AddTone(sound, 0, 0.55f, Wave.Triangle, 0.2f, new Envelope(0.01f, 0.4f, 0.6f, 0.1f), 1520, 185);
        return Done(sound, 0.7f);
    }

    /// <summary>"Vvvvrrrr-KSSHH" — the ceiling sliding open (or shut): a whirring motor and a puff of air.</summary>
    public static float[] CeilingSlide()
    {
        var sound = Synth.Silence(1.0f);
        Synth.AddTone(sound, 0, 0.75f, Wave.Saw, 0.18f, new Envelope(0.05f, 0.5f, 0.7f, 0.1f), 70, 110);
        Synth.AddTone(sound, 0, 0.75f, Wave.Square25, 0.06f, new Envelope(0.05f, 0.5f, 0.7f, 0.1f), 140, 220);
        Synth.AddTone(sound, 0.72f, 0.18f, Wave.Noise, 0.35f, new Envelope(0.005f, 0.12f, 0f, 0.05f), 1);
        return Done(sound, 0.6f);
    }

    // ---------- The pause screen ----------

    /// <summary>"Bip-boop!" — the game pauses: two soft notes going down (and the menu plays them going up to resume).</summary>
    public static float[] PauseBlip()
    {
        var sound = Synth.Silence(0.3f);
        Synth.AddTone(sound, 0, 0.07f, Wave.Square25, 0.3f, Pluck, Synth.NoteToFrequency("E6"));
        Synth.AddTone(sound, 0.08f, 0.14f, Wave.Square25, 0.3f, new Envelope(0.002f, 0.12f, 0f, 0.03f), Synth.NoteToFrequency("A5"));
        return Done(sound, 0.7f);
    }

    /// <summary>
    /// "WHOOOSH-sparkle!" — WORLD WARP! A whoosh of wind that swoops up high, a whistle sliding up with it, and a
    /// little rainbow of sparkly notes at the top.
    /// </summary>
    public static float[] Warp()
    {
        const float length = 0.55f; // try changing this!
        var sound = Synth.Silence(length + 0.35f);
        var random = new Random(31);
        float low = 0;
        for (int i = 0; i < (int)(length * Synth.SampleRate); i++)
        {
            float t = (float)i / Synth.SampleRate / length; // 0 to 1
            float cutoff = 250f + 6000f * t * t;            // the "shhh" gets brighter and brighter
            float amount = 1f - MathF.Exp(-MathF.Tau * cutoff / Synth.SampleRate);
            low += amount * (random.NextSingle() * 2f - 1f - low);
            sound[i] = low * MathF.Sin(MathF.PI * t) * 0.9f; // swells in and out
        }
        Synth.AddTone(sound, 0, length, Wave.Sine, 0.35f, new Envelope(0.02f, 0.3f, 0.5f, 0.08f), 200, 1600); // the whistle
        string[] sparkles = { "C7", "E7", "G7", "C8" };
        for (int i = 0; i < sparkles.Length; i++)
            Synth.AddTone(sound, length * 0.6f + i * 0.06f, 0.06f, Wave.Square12, 0.1f, Pluck, Synth.NoteToFrequency(sparkles[i]));
        return Done(sound, 0.75f);
    }

    // ---------- VOLCANO ISLAND and the LAVA CAVES ----------

    /// <summary>"Blub-blub-blub" — a lava bubble is about to pop up: three thick, low bubbles getting higher.</summary>
    public static float[] LavaBubble()
    {
        var sound = Synth.Silence(0.7f);
        for (int i = 0; i < 3; i++)
            Synth.AddTone(sound, i * 0.2f, 0.12f, Wave.Sine, 0.6f, new Envelope(0.005f, 0.09f, 0f, 0.02f), 110 + i * 30, 260 + i * 60);
        return Done(sound, 0.7f);
    }

    /// <summary>"PLOP!" — a blob of lava jumps out of the river: a quick pop and a splash.</summary>
    public static float[] LavaPlop()
    {
        var sound = Synth.Silence(0.35f);
        Synth.AddTone(sound, 0, 0.1f, Wave.Sine, 0.7f, new Envelope(0.002f, 0.08f, 0f, 0.02f), 180, 700);
        Synth.AddTone(sound, 0.03f, 0.2f, Wave.Noise, 0.2f, new Envelope(0.005f, 0.15f, 0f, 0.05f), 1);
        return Done(sound, 0.7f);
    }

    /// <summary>"TSSSSSS!" — something hot in the lava: a hiss that fades, with a little whistle on top.</summary>
    public static float[] Sizzle()
    {
        var sound = Synth.Silence(0.8f);
        Synth.AddTone(sound, 0, 0.7f, Wave.HighNoise, 0.5f, new Envelope(0.01f, 0.5f, 0.2f, 0.15f), 1);
        Synth.AddTone(sound, 0, 0.35f, Wave.Sine, 0.15f, new Envelope(0.01f, 0.3f, 0f, 0.05f), 2400, 1600);
        return Done(sound, 0.7f);
    }

    /// <summary>"Rrrrumble-gloop" — a rock starts sinking into the lava: a low, wobbly rumble.</summary>
    public static float[] RockSink()
    {
        var sound = Synth.Silence(0.7f);
        Synth.AddTone(sound, 0, 0.6f, Wave.Triangle, 0.6f, new Envelope(0.05f, 0.4f, 0.3f, 0.1f), 70, 45, vibrato: 0.04f);
        Synth.AddTone(sound, 0.3f, 0.12f, Wave.Sine, 0.3f, new Envelope(0.005f, 0.1f, 0f, 0.02f), 150, 90);
        return Done(sound, 0.6f);
    }

    /// <summary>"WHEEEEEEE!" — dropping into a hole: a slide whistle that swoops up, then all the way down.</summary>
    public static float[] Wheee()
    {
        var sound = Synth.Silence(0.95f);
        Synth.AddTone(sound, 0, 0.25f, Wave.Sine, 0.5f, new Envelope(0.01f, 0.2f, 0.8f, 0.02f), 700, 1500);
        Synth.AddTone(sound, 0.25f, 0.6f, Wave.Sine, 0.5f, new Envelope(0.005f, 0.4f, 0.7f, 0.1f), 1500, 220);
        Synth.AddTone(sound, 0, 0.85f, Wave.Triangle, 0.12f, new Envelope(0.01f, 0.5f, 0.5f, 0.1f), 1400, 300);
        return Done(sound, 0.75f);
    }

    /// <summary>"KA-WHOOOOSH!" — the geyser blasts Bolt-E up: a deep boom, then rushing steam that swoops up high.</summary>
    public static float[] GeyserWhoosh()
    {
        const float length = 0.9f;
        var sound = Synth.Silence(length + 0.2f);
        var random = new Random(41);
        float low = 0;
        for (int i = 0; i < (int)(length * Synth.SampleRate); i++)
        {
            float t = (float)i / Synth.SampleRate / length; // 0 to 1
            float cutoff = 400f + 5000f * t;                 // the "shhh" of the steam gets brighter
            float amount = 1f - MathF.Exp(-MathF.Tau * cutoff / Synth.SampleRate);
            low += amount * (random.NextSingle() * 2f - 1f - low);
            sound[i] = low * MathF.Sin(MathF.PI * MathF.Min(1, t * 3)) * (1 - t * 0.6f);
        }
        Synth.AddTone(sound, 0, 0.3f, Wave.Sine, 0.8f, new Envelope(0.002f, 0.25f, 0f, 0.05f), 120, 40); // BOOM
        Synth.AddTone(sound, 0.1f, 0.7f, Wave.Sine, 0.3f, new Envelope(0.02f, 0.5f, 0.4f, 0.1f), 300, 1800); // up, up, UP!
        return Done(sound, 0.8f);
    }

    /// <summary>"Ta-da-da-DAAA!" — CAVE CLEAR! A happy little fanfare going up.</summary>
    public static float[] CaveClear() => Tune(new[] { ("G5", 0.1f), ("C6", 0.1f), ("E6", 0.1f), ("G6", 0.18f), ("E6", 0.1f), ("G6", 0.45f) }, Wave.Square25);

    /// <summary>"Flap-flap" — a fire bat flies in: two soft, quick whooshes.</summary>
    public static float[] BatFlap()
    {
        var sound = Synth.Silence(0.35f);
        for (int i = 0; i < 2; i++)
            Synth.AddTone(sound, i * 0.13f, 0.08f, Wave.Noise, 0.5f, new Envelope(0.005f, 0.06f, 0f, 0.02f), 1);
        Synth.AddTone(sound, 0, 0.25f, Wave.Square12, 0.08f, new Envelope(0.005f, 0.2f, 0f, 0.02f), 1800, 2400); // a tiny squeak
        return Done(sound, 0.55f);
    }

    /// <summary>"CRACK!" — a stalactite breaks off the ceiling: a sharp snap and a crumbly rumble.</summary>
    public static float[] StalactiteCrack()
    {
        var sound = Synth.Silence(0.5f);
        Synth.AddTone(sound, 0, 0.03f, Wave.HighNoise, 0.8f, new Envelope(0.001f, 0.025f, 0f, 0.01f), 1);
        Synth.AddTone(sound, 0.02f, 0.35f, Wave.Noise, 0.35f, new Envelope(0.01f, 0.3f, 0f, 0.05f), 1);
        Synth.AddTone(sound, 0, 0.2f, Wave.Triangle, 0.4f, new Envelope(0.002f, 0.18f, 0f, 0.02f), 160, 60);
        return Done(sound, 0.75f);
    }

    /// <summary>"BRRROOOOM..." — the volcano erupts far away: a long, low rumble that rolls in and out.</summary>
    public static float[] Eruption()
    {
        const float length = 1.6f;
        var sound = Synth.Silence(length + 0.1f);
        var random = new Random(43);
        float low1 = 0, low2 = 0;
        for (int i = 0; i < (int)(length * Synth.SampleRate); i++)
        {
            float t = (float)i / Synth.SampleRate / length;
            float amount = 1f - MathF.Exp(-MathF.Tau * 140f / Synth.SampleRate); // only the deep, rumbly part of the noise
            low1 += amount * (random.NextSingle() * 2f - 1f - low1);
            low2 += amount * (low1 - low2);
            sound[i] = low2 * MathF.Sin(MathF.PI * t) * 6f;
        }
        Synth.AddTone(sound, 0, length * 0.8f, Wave.Triangle, 0.25f, new Envelope(0.2f, 0.8f, 0.3f, 0.2f), 50, 38, vibrato: 0.05f);
        return Done(sound, 0.7f);
    }

    // ---------- The WORLD MAP ----------
    // (These make their own "shhh" noise with their own dice, so the noise of every older sound and song stays the same.)

    /// <summary>
    /// "Fwoop!" — the screen closing (or opening) like an eye: soft rushing air that gets brighter, a whistle sliding up,
    /// and a tiny "plink" at the top. (The map plays it lower when the eye closes, and higher when it opens.)
    /// </summary>
    public static float[] MapIris()
    {
        const float length = 0.35f; // try changing this!
        var sound = Synth.Silence(length + 0.25f);
        var random = new Random(61);
        float low = 0;
        for (int i = 0; i < (int)(length * Synth.SampleRate); i++)
        {
            float t = (float)i / Synth.SampleRate / length; // 0 to 1
            float cutoff = 400f + 3000f * t;                 // the "shhh" opens up from dark to bright
            float amount = 1f - MathF.Exp(-MathF.Tau * cutoff / Synth.SampleRate);
            low += amount * (random.NextSingle() * 2f - 1f - low);
            sound[i] = low * MathF.Sin(MathF.PI * t) * 0.8f; // swells in and out
        }
        Synth.AddTone(sound, 0, length * 0.9f, Wave.Sine, 0.3f, new Envelope(0.02f, 0.2f, 0.5f, 0.06f), 300, 900);
        Synth.AddTone(sound, length * 0.85f, 0.05f, Wave.Square12, 0.08f, Pluck, Synth.NoteToFrequency("G6")); // plink!
        return Done(sound, 0.7f);
    }

    /// <summary>"Pip!" — one dot of a new road popping up on the map (the map plays them higher and higher).</summary>
    public static float[] MapPip()
    {
        var sound = Synth.Silence(0.15f);
        Synth.AddTone(sound, 0, 0.045f, Wave.Square25, 0.3f, Pluck, Synth.NoteToFrequency("E6"));
        Synth.AddTone(sound, 0.02f, 0.05f, Wave.Sine, 0.2f, Pluck, Synth.NoteToFrequency("E7")); // a little glint
        return Done(sound, 0.6f);
    }

    /// <summary>"Ta-da-da-DAA, da-DAAAA!" — a world is CLEARED and its gold flag goes up. (Our own tune, a bass, and a cymbal.)</summary>
    public static float[] MapFlag()
    {
        var sound = Tune(new[] { ("C5", 0.1f), ("E5", 0.1f), ("G5", 0.1f), ("C6", 0.2f), ("G5", 0.1f), ("C6", 0.1f), ("E6", 0.6f) },
                         Wave.Square25, normalize: false);
        Synth.AddTone(sound, 0.0f, 0.3f, Wave.Triangle, 0.3f, Held, Synth.NoteToFrequency("C3"));
        Synth.AddTone(sound, 0.3f, 0.2f, Wave.Triangle, 0.3f, Held, Synth.NoteToFrequency("G3"));
        Synth.AddTone(sound, 0.5f, 0.2f, Wave.Triangle, 0.3f, Held, Synth.NoteToFrequency("E3"));
        Synth.AddTone(sound, 0.7f, 0.6f, Wave.Triangle, 0.3f, Held, Synth.NoteToFrequency("C3"));
        AddNoise(sound, new Random(66), 0.7f, 0.5f, 0.1f, new Envelope(0.01f, 0.3f, 0.2f, 0.2f), 9000, 6000, hiss: true); // the cymbal
        return Done(sound);
    }

    /// <summary>"Pff!" — a fluffy cloud puffs away from a "?" world: a soft burst of air and a little thump.</summary>
    public static float[] MapPoof()
    {
        var sound = Synth.Silence(0.4f);
        AddNoise(sound, new Random(62), 0, 0.25f, 0.6f, new Envelope(0.005f, 0.12f, 0f, 0.05f), 1800, 500);
        Synth.AddTone(sound, 0, 0.12f, Wave.Sine, 0.5f, new Envelope(0.002f, 0.09f, 0f, 0.03f), 200, 90);
        return Done(sound, 0.7f);
    }

    /// <summary>"SPLOOSH-blub-blub-blub!" — the space capsule splashing down in the sea: a big splash, then three bubbles.</summary>
    public static float[] MapSplash()
    {
        var sound = Synth.Silence(0.6f);
        AddNoise(sound, new Random(63), 0, 0.25f, 0.8f, new Envelope(0.003f, 0.15f, 0.1f, 0.05f), 5000, 400);
        for (int i = 0; i < 3; i++)
            Synth.AddTone(sound, 0.22f + i * 0.08f, 0.06f, Wave.Sine, 0.3f, new Envelope(0.003f, 0.05f, 0f, 0.02f), 600, 900);
        return Done(sound, 0.75f);
    }

    /// <summary>"Zoooom! TA!" — GO on the map: a quick slide up, and a happy chord.</summary>
    public static float[] MapGo()
    {
        var sound = Synth.Silence(0.55f);
        Synth.AddTone(sound, 0, 0.18f, Wave.Square25, 0.3f, new Envelope(0.003f, 0.15f, 0.6f, 0.02f), 330, 1320);
        foreach (string note in new[] { "C5", "E5", "G5" })
            Synth.AddTone(sound, 0.18f, 0.2f, Wave.Square25, 0.2f, Pluck with { Decay = 0.15f }, Synth.NoteToFrequency(note));
        return Done(sound);
    }

    /// <summary>
    /// "Da-da-da-DAAAAAA!" — ALL 7 WORLDS CLEAR! A run up to a big shiny chord, a drum roll getting louder and louder,
    /// and a cymbal crash.
    /// </summary>
    public static float[] MapAllClear()
    {
        var sound = Synth.Silence(2.3f);
        var random = new Random(64);
        string[] run = { "C5", "E5", "G5", "C6" };
        for (int i = 0; i < run.Length; i++)
            Synth.AddTone(sound, i * 0.1f, 0.09f, Wave.Square25, 0.25f, Held, Synth.NoteToFrequency(run[i]));
        foreach (string note in new[] { "E6", "G6", "C7" })
        {
            Synth.AddTone(sound, 0.4f, 1.2f, Wave.Square25, 0.12f, Held, Synth.NoteToFrequency(note), vibrato: 0.008f);
            Synth.AddTone(sound, 0.4f, 1.2f, Wave.Triangle, 0.1f, Held, Synth.NoteToFrequency(note));
        }
        Synth.AddTone(sound, 0.4f, 1.2f, Wave.Triangle, 0.3f, Held, Synth.NoteToFrequency("C3"));
        for (int i = 0; i < 20; i++) // the drum roll: 20 little hits, louder and louder
            AddNoise(sound, random, i * 0.03f, 0.02f, 0.1f + 0.4f * i / 20f, new Envelope(0.001f, 0.015f, 0f, 0.01f), 2500, 2500);
        AddNoise(sound, random, 0.4f, 0.8f, 0.25f, new Envelope(0.005f, 0.5f, 0.1f, 0.3f), 9000, 6000, hiss: true); // the cymbal
        return Done(sound);
    }

    /// <summary>"Bwom-BWOM!" — the next boss pops up on the map: funny-scary, not scary.</summary>
    public static float[] MapBossSting()
    {
        var sound = Synth.Silence(0.8f);
        float d3 = Synth.NoteToFrequency("D3");
        Synth.AddTone(sound, 0, 0.16f, Wave.Square50, 0.3f, Held, d3, d3 / MathF.Pow(2, 1 / 12f)); // bwom (bending down)
        Synth.AddTone(sound, 0.2f, 0.4f, Wave.Square50, 0.35f, Held, Synth.NoteToFrequency("G2"), vibrato: 0.02f); // BWOM!
        Synth.AddTone(sound, 0.2f, 0.4f, Wave.Triangle, 0.25f, Held, Synth.NoteToFrequency("G2") / 2);
        AddNoise(sound, new Random(65), 0.2f, 0.06f, 0.3f, new Envelope(0.001f, 0.04f, 0f, 0.02f), 1500, 600);
        return Done(sound);
    }

    /// <summary>
    /// Adds "shhh" noise with its own dice: through a filter that goes from "lowFrom" to "lowTo" (100 = a deep rumble,
    /// 6000 = a bright hiss), or extra hissy (hiss = true, like a cymbal).
    /// </summary>
    static void AddNoise(float[] sound, Random random, float start, float length, float volume, Envelope envelope,
                         float lowFrom, float lowTo, bool hiss = false)
    {
        int first = (int)(start * Synth.SampleRate);
        int count = (int)((length + envelope.Release) * Synth.SampleRate);
        float low = 0, last = 0;
        for (int i = 0; i < count && first + i < sound.Length; i++)
        {
            float t = (float)i / Synth.SampleRate;
            float cutoff = lowFrom + (lowTo - lowFrom) * MathF.Min(1, t / length);
            float amount = 1f - MathF.Exp(-MathF.Tau * cutoff / Synth.SampleRate);
            low += amount * (random.NextSingle() * 2f - 1f - low);
            float sample = hiss ? (low - last) * 2f : low; // (the difference between two noises is extra hissy)
            last = low;
            sound[first + i] += sample * volume * envelope.LevelAt(t, length);
        }
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
