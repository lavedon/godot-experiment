using Godot;

/// <summary>Every sound effect in the game.</summary>
public enum Sfx
{
    Jump, DoubleJump, Bolt, Land, Explosion, Clink, Start, GameOver, HighScore,
    Stomp, Shoot, BossAlarm, BossHit, Victory, Slam, LaserCharge, LaserBeam, Laugh, Roar,
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
    Tween? musicFade;

    public bool MusicOn { get; private set; } = true;

    public override void _Ready()
    {
        music = new AudioStreamPlayer { Stream = Synth.ToAudioStream(Song.Render(), loop: true), VolumeDb = MusicVolumeDb };
        AddChild(music);

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
    }

    void AddEffect(Sfx name, float[] sound, float volumeDb, int voices = 2)
    {
        // "voices" = how many copies can play at the same time (so quick bolts don't cut each other off)
        var player = new AudioStreamPlayer { Stream = Synth.ToAudioStream(sound), VolumeDb = volumeDb, MaxPolyphony = voices };
        AddChild(player);
        effects[name] = player;
    }

    /// <summary>Plays a sound effect. pitch 2 = one octave higher, 0.5 = one octave lower.</summary>
    public void Play(Sfx name, float pitch = 1f)
    {
        var player = effects[name];
        player.PitchScale = pitch;
        player.Play();
    }

    /// <summary>Starts the music (or makes sure it's playing at normal speed and volume).</summary>
    public void StartMusic()
    {
        if (!MusicOn) return;
        musicFade?.Kill();
        music.PitchScale = 1f;
        music.VolumeDb = MusicVolumeDb;
        if (!music.Playing) music.Play();
    }

    /// <summary>The music slows down and fades out, like a robot running out of power.</summary>
    public void PowerDownMusic()
    {
        if (!music.Playing) return;
        musicFade?.Kill();
        musicFade = CreateTween().SetIgnoreTimeScale(); // keep going at normal speed during the slow-motion
        musicFade.TweenProperty(music, AudioStreamPlayer.PropertyName.PitchScale.ToString(), 0.3f, 0.8f).SetEase(Tween.EaseType.In);
        musicFade.Parallel().TweenProperty(music, AudioStreamPlayer.PropertyName.VolumeDb.ToString(), -40f, 0.8f).SetEase(Tween.EaseType.In);
        musicFade.TweenCallback(Callable.From(music.Stop));
    }

    /// <summary>During a boss fight the music plays faster and higher, to make it exciting! (1 = normal speed)</summary>
    public void SetMusicSpeed(float speed)
    {
        if (!music.Playing) return;
        musicFade?.Kill();
        musicFade = CreateTween();
        musicFade.TweenProperty(music, AudioStreamPlayer.PropertyName.PitchScale.ToString(), speed, 0.6f);
    }

    public void SetMusicOn(bool on)
    {
        MusicOn = on;
        if (on)
        {
            StartMusic();
        }
        else
        {
            musicFade?.Kill();
            music.Stop();
        }
    }
}
