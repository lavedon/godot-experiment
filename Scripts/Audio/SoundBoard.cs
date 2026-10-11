using Godot;

/// <summary>Every sound effect in the game.</summary>
public enum Sfx
{
    Jump, DoubleJump, Bolt, Land, Explosion, Clink, Start, GameOver, HighScore,
    Stomp, Shoot, BossAlarm, BossHit, Victory, Slam, LaserCharge, LaserBeam, Laugh, Roar,
    OneUp, Rebuild,
    PowerUp, Tick, Smash, Grow, Shrink, Rocket,
    NewWorld,
    Unlock,
    PassFlag,
    RevUp, Zoom, WallBonk, VacuumHum, BeepBoop, Spit, // VROOMBA
    RotorBuzz, BombWhistle, LowBattery, PowerDown, TakeOff, Bzzt, // DRONEZILLA
    RobotStep, BoxWhoosh, BoxCrunch, Boing, Kick, Chirp, Ding, ErrorBuzz, DanceBeat, // OPTI-MAX
    FlipUp, FlipDown, FlipThud, SpaceFall, CeilingSlide, // UPSIDE-DOWN STATION
    PauseBlip, MenuTick, Warp, // the pause screen and WORLD WARP
    LavaBubble, LavaPlop, Sizzle, RockSink, Wheee, GeyserWhoosh, CaveClear, BatFlap, StalactiteCrack, Eruption, // VOLCANO ISLAND
}

/// <summary>
/// Plays the music and sound effects. All of them are made by the Synth when the game starts.
/// Volumes are in "decibels" (dB): 0 = full volume, -6 = about half as loud, -12 = about a quarter.
/// </summary>
public partial class SoundBoard : Node
{
    const float MusicVolumeDb = -9f;

    AudioStreamPlayer music = null!;
    readonly Dictionary<Sfx, AudioStreamPlayer> effects = new();
    readonly Dictionary<Sfx, int> timesPlayed = new();
    Tween? musicFade;

    float musicSpeed = 1f;   // how fast the music should play (1 = normal, faster in boss fights)
    bool musicHeld;          // true while the music is dipped (magic rebuild) or powering down (game over)
    float heldPitch = 1f;    // how fast it plays while it's held: slow while it's dipped, normal speed after a game over
    const float DipPitch = 0.6f; // how low the music wobbles down while Bolt-E is in pieces

    public bool MusicOn { get; private set; } = true;

    /// <summary>True while the music is playing (the self-test reads this).</summary>
    public bool MusicPlaying => music.Playing;
    /// <summary>How fast and high the music plays right now: 1 = normal (the self-test reads this).</summary>
    public float MusicPitch => music.PitchScale;
    /// <summary>How many times a sound effect has played since the game started (the self-test reads this).</summary>
    public int TimesPlayed(Sfx name) => timesPlayed.GetValueOrDefault(name);
    /// <summary>The pitch a sound effect last played at: 2 = one octave higher (the self-test reads this).</summary>
    public float PitchOf(Sfx name) => effects[name].PitchScale;

    public override void _Ready()
    {
        SetUpMusic();

        AddEffect(Sfx.Jump, SoundEffects.Jump(), -10f);
        AddEffect(Sfx.DoubleJump, SoundEffects.DoubleJump(), -10f);
        AddEffect(Sfx.Bolt, SoundEffects.Bolt(), -11f, voices: 4);
        AddEffect(Sfx.Land, SoundEffects.Land(), -12f);
        AddEffect(Sfx.Explosion, SoundEffects.Explosion(), -2f, voices: 4);
        AddEffect(Sfx.Clink, SoundEffects.Clink(), -16f, voices: 6);
        AddEffect(Sfx.Start, SoundEffects.StartJingle(), -9f);
        AddEffect(Sfx.GameOver, SoundEffects.GameOverJingle(), -8f);
        AddEffect(Sfx.HighScore, SoundEffects.HighScoreJingle(), -7f);
        AddEffect(Sfx.Stomp, SoundEffects.Stomp(), -6f, voices: 3);
        AddEffect(Sfx.Shoot, SoundEffects.Shoot(), -12f);
        AddEffect(Sfx.BossAlarm, SoundEffects.BossAlarm(), -13f, voices: 1);
        AddEffect(Sfx.BossHit, SoundEffects.BossHit(), -5f);
        AddEffect(Sfx.Victory, SoundEffects.VictoryJingle(), -6f);
        AddEffect(Sfx.Slam, SoundEffects.Slam(), -3f);
        AddEffect(Sfx.LaserCharge, SoundEffects.LaserCharge(), -12f, voices: 1);
        AddEffect(Sfx.LaserBeam, SoundEffects.LaserBeam(), -9f, voices: 1);
        AddEffect(Sfx.Laugh, SoundEffects.Laugh(), -7f, voices: 1);
        AddEffect(Sfx.Roar, SoundEffects.Roar(), -6f, voices: 1);
        AddEffect(Sfx.OneUp, SoundEffects.OneUp(), -8f);
        AddEffect(Sfx.Rebuild, SoundEffects.Rebuild(), -6f);
        AddEffect(Sfx.PowerUp, SoundEffects.PowerUp(), -7f);
        AddEffect(Sfx.Tick, SoundEffects.Tick(), -14f, voices: 3);
        AddEffect(Sfx.Smash, SoundEffects.Smash(), -5f, voices: 3);
        AddEffect(Sfx.Grow, SoundEffects.Grow(), -8f);
        AddEffect(Sfx.Shrink, SoundEffects.Shrink(), -8f);
        AddEffect(Sfx.Rocket, SoundEffects.Rocket(), -7f);
        AddEffect(Sfx.NewWorld, SoundEffects.NewWorld(), -7f);
        AddEffect(Sfx.Unlock, SoundEffects.Unlock(), -6f);
        AddEffect(Sfx.PassFlag, SoundEffects.PassFlag(), -8f);
        AddEffect(Sfx.RevUp, SoundEffects.RevUp(), -9f, voices: 1);
        AddEffect(Sfx.Zoom, SoundEffects.Zoom(), -8f);
        AddEffect(Sfx.WallBonk, SoundEffects.WallBonk(), -4f);
        AddEffect(Sfx.VacuumHum, SoundEffects.VacuumHum(), -12f, voices: 1, loop: true); // (hums until the vacuum switches off)
        AddEffect(Sfx.BeepBoop, SoundEffects.BeepBoop(), -8f, voices: 1);
        AddEffect(Sfx.Spit, SoundEffects.Spit(), -9f, voices: 3);
        AddEffect(Sfx.RotorBuzz, SoundEffects.RotorBuzz(), -19f, voices: 1, loop: true); // (buzzes while DRONEZILLA flies)
        AddEffect(Sfx.BombWhistle, SoundEffects.BombWhistle(), -15f, voices: 5);
        AddEffect(Sfx.LowBattery, SoundEffects.LowBattery(), -9f, voices: 1);
        AddEffect(Sfx.PowerDown, SoundEffects.PowerDown(), -9f, voices: 1);
        AddEffect(Sfx.TakeOff, SoundEffects.TakeOff(), -11f, voices: 1);
        AddEffect(Sfx.Bzzt, SoundEffects.Bzzt(), -8f, voices: 1);
        AddEffect(Sfx.RobotStep, SoundEffects.RobotStep(), -11f, voices: 3);
        AddEffect(Sfx.BoxWhoosh, SoundEffects.BoxWhoosh(), -13f, voices: 3);
        AddEffect(Sfx.BoxCrunch, SoundEffects.BoxCrunch(), -9f, voices: 4);
        AddEffect(Sfx.Boing, SoundEffects.Boing(), -10f, voices: 3);
        AddEffect(Sfx.Kick, SoundEffects.Kick(), -7f);
        AddEffect(Sfx.Chirp, SoundEffects.Chirp(), -15f, voices: 2);
        AddEffect(Sfx.Ding, SoundEffects.Ding(), -8f, voices: 1);
        AddEffect(Sfx.ErrorBuzz, SoundEffects.ErrorBuzz(), -10f, voices: 1);
        AddEffect(Sfx.DanceBeat, SoundEffects.DanceBeat(), -8f, voices: 1);
        AddEffect(Sfx.FlipUp, SoundEffects.FlipWhoosh(up: true), -9f);
        AddEffect(Sfx.FlipDown, SoundEffects.FlipWhoosh(up: false), -9f);
        AddEffect(Sfx.FlipThud, SoundEffects.FlipThud(), -9f, voices: 3);
        AddEffect(Sfx.SpaceFall, SoundEffects.SpaceFall(), -8f, voices: 1);
        AddEffect(Sfx.CeilingSlide, SoundEffects.CeilingSlide(), -11f, voices: 1);
        AddEffect(Sfx.LavaBubble, SoundEffects.LavaBubble(), -12f, voices: 2);
        AddEffect(Sfx.LavaPlop, SoundEffects.LavaPlop(), -10f, voices: 2);
        AddEffect(Sfx.Sizzle, SoundEffects.Sizzle(), -8f, voices: 1);
        AddEffect(Sfx.RockSink, SoundEffects.RockSink(), -12f, voices: 2);
        AddEffect(Sfx.Wheee, SoundEffects.Wheee(), -8f, voices: 1);
        AddEffect(Sfx.GeyserWhoosh, SoundEffects.GeyserWhoosh(), -6f, voices: 1);
        AddEffect(Sfx.CaveClear, SoundEffects.CaveClear(), -7f, voices: 1);
        AddEffect(Sfx.BatFlap, SoundEffects.BatFlap(), -11f, voices: 2);
        AddEffect(Sfx.StalactiteCrack, SoundEffects.StalactiteCrack(), -9f, voices: 3);
        AddEffect(Sfx.Eruption, SoundEffects.Eruption(), -9f, voices: 1);
        // The pause screen's sounds still play while the game is paused (see Main.Pause.cs). MenuTick is the same
        // little tick as the slot machine's, but it's its own player, so the frozen game's sounds stay frozen.
        AddEffect(Sfx.PauseBlip, SoundEffects.PauseBlip(), -9f, playsWhilePaused: true);
        AddEffect(Sfx.MenuTick, SoundEffects.Tick(), -12f, voices: 3, playsWhilePaused: true);
        AddEffect(Sfx.Warp, SoundEffects.Warp(), -7f, voices: 1, playsWhilePaused: true);
    }

    void AddEffect(Sfx name, float[] sound, float volumeDb, int voices = 2, bool loop = false, bool playsWhilePaused = false)
    {
        // "voices" = how many copies can play at the same time (so quick bolts don't cut each other off).
        // loop = it plays again and again until Stop (like VROOMBA's vacuum hum).
        var player = new AudioStreamPlayer { Stream = Synth.ToAudioStream(sound, loop), VolumeDb = volumeDb, MaxPolyphony = voices };
        if (playsWhilePaused) player.ProcessMode = ProcessModeEnum.Always; // (every other sound freezes with the game)
        AddChild(player);
        effects[name] = player;
    }

    /// <summary>Plays a sound effect. pitch 2 = one octave higher, 0.5 = one octave lower.</summary>
    public void Play(Sfx name, float pitch = 1f)
    {
        var player = effects[name];
        player.PitchScale = pitch;
        player.Play();
        timesPlayed[name] = TimesPlayed(name) + 1;
    }

    /// <summary>Stops a sound effect that's still playing (like VROOMBA's vacuum when it switches off).</summary>
    public void Stop(Sfx name) => effects[name].Stop();

    /// <summary>True while this sound effect is playing (the self-test reads this).</summary>
    public bool IsPlaying(Sfx name) => effects[name].Playing;

    /// <summary>Plays the music at this speed and normal volume (if the music is switched on).</summary>
    void PlayMusicAt(float pitch)
    {
        if (!MusicOn) return;
        musicFade?.Kill();
        musicHeld = false;
        music.PitchScale = pitch;
        if (trackFade is null || !trackFade.IsRunning()) music.VolumeDb = MusicVolumeDb;
        if (!music.Playing) music.Play();
    }

    /// <summary>
    /// Starts the music (or makes sure it's playing at normal volume) at this speed: 1 = normal.
    /// (A new game or the title screen: each world has its own music speed, so Main passes Sunny Hills' speed.)
    /// A fresh start defaults to the original runner theme; a level may explicitly choose its own score.
    /// </summary>
    public void StartMusic(float speed = 1f, LevelMusicTheme theme = LevelMusicTheme.Runner)
    {
        musicSpeed = speed;
        levelMusicTheme = theme;
        musicHeld = false; // (a fresh start: nothing is held any more, even if the music is switched off)
        SelectTheme(boss: false);
        PlayMusicAt(speed);
    }

    /// <summary>The music slows down and fades out, like a robot running out of power.</summary>
    public void PowerDownMusic()
    {
        FinishTrackFade();
        musicHeld = true;
        heldPitch = 1f; // (switched back on after the game, it plays at normal speed)
        if (!music.Playing) return;
        musicFade?.Kill();
        musicFade = CreateTween().SetIgnoreTimeScale(); // keep going at normal speed during the slow-motion
        musicFade.TweenProperty(music, AudioStreamPlayer.PropertyName.PitchScale.ToString(), 0.3f, 0.8f).SetEase(Tween.EaseType.In);
        musicFade.Parallel().TweenProperty(music, AudioStreamPlayer.PropertyName.VolumeDb.ToString(), -40f, 0.8f).SetEase(Tween.EaseType.In);
        musicFade.TweenCallback(Callable.From(music.Stop));
    }

    /// <summary>
    /// The current theme follows the world's speed, powers and boss phase (1 = normal speed).
    /// While the music is dipped or powering down, it just remembers the speed for later.
    /// </summary>
    public void SetMusicSpeed(float speed)
    {
        musicSpeed = speed;
        if (!music.Playing || musicHeld) return;
        musicFade?.Kill();
        musicFade = CreateTween();
        musicFade.TweenProperty(music, AudioStreamPlayer.PropertyName.PitchScale.ToString(), speed, 0.6f);
    }

    /// <summary>Bolt-E blew up but has a spare battery: the music only wobbles down (it doesn't stop).</summary>
    public void DipMusic()
    {
        musicHeld = true;
        heldPitch = DipPitch; // (switched off and on while he's in pieces, it comes back still wobbled down)
        if (!music.Playing) return;
        musicFade?.Kill();
        musicFade = CreateTween().SetIgnoreTimeScale(); // keep going at normal speed during the slow-motion
        musicFade.TweenProperty(music, AudioStreamPlayer.PropertyName.PitchScale.ToString(), DipPitch, 0.5f);
    }

    /// <summary>Bolt-E is back! The music goes back up to the speed it should be.</summary>
    public void RestoreMusic()
    {
        musicHeld = false; // (even if the music is switched off, so it never stays held by mistake)
        if (!MusicOn) return;
        if (!music.Playing)
        {
            PlayMusicAt(musicSpeed);
            return;
        }
        musicFade?.Kill();
        musicFade = CreateTween();
        musicFade.TweenProperty(music, AudioStreamPlayer.PropertyName.PitchScale.ToString(), musicSpeed, 0.6f);
    }

    /// <summary>
    /// The pause screen: the music waits right where it is (paused = true), and goes on from there (paused = false).
    /// (Godot also pauses every sound of a paused game by itself. This makes sure the music does, too.)
    /// </summary>
    public void HoldMusicForPause(bool paused)
    {
        music.StreamPaused = paused;
        outgoingMusic.StreamPaused = paused;
    }

    /// <summary>True while the music is waiting for the pause screen to close (the self-test reads this).</summary>
    public bool MusicPausedNow => music.StreamPaused;

    /// <summary>Stops the music and every sound effect right away (the self-test does this before it quits).</summary>
    public void StopEverything()
    {
        FinishTrackFade();
        musicFade?.Kill();
        music.Stop();
        foreach (var player in effects.Values) player.Stop();
    }

    /// <summary>
    /// Music on or off (M, or BACK on a controller). Switched back on, it plays at the speed it should be. But while it's
    /// held (wobbled down while Bolt-E is in pieces, or after a game over) it comes back at the held speed and stays
    /// held, so it still goes back up when Bolt-E is back together.
    /// </summary>
    public void SetMusicOn(bool on)
    {
        MusicOn = on;
        if (on)
        {
            bool held = musicHeld;
            PlayMusicAt(held ? heldPitch : musicSpeed);
            musicHeld = held; // (PlayMusicAt lets go of the hold: keep holding)
        }
        else
        {
            FinishTrackFade();
            musicFade?.Kill();
            music.Stop();
        }
    }
}
