using Godot;

/// <summary>A boss chooses its score independently of its name or implementation.</summary>
public enum BossMusicTheme { TitanCircuit, RotorRush, IronShadow }

/// <summary>The running score for a level, remembered while a boss's own theme plays.</summary>
public enum LevelMusicTheme { Runner, NeonSlipstream }

public partial class SoundBoard
{
    AudioStreamWav runnerTheme = null!;
    AudioStreamWav stationTheme = null!;
    AudioStreamWav bossTheme = null!;
    AudioStreamWav droneTheme = null!;
    AudioStreamWav humanoidTheme = null!;
    AudioStreamPlayer outgoingMusic = null!;
    Tween? trackFade;
    LevelMusicTheme levelMusicTheme = LevelMusicTheme.Runner;

    /// <summary>The selected theme, even while muted or rebuilding (also read by the self-test).</summary>
    public bool BossMusicActive => music.Stream == bossTheme || music.Stream == droneTheme || music.Stream == humanoidTheme;
    public bool DroneMusicActive => music.Stream == droneTheme;
    public bool HumanoidMusicActive => music.Stream == humanoidTheme;
    public bool StationMusicActive => music.Stream == stationTheme;

    void SetUpMusic()
    {
        runnerTheme = Synth.ToAudioStream(Song.Render(), loop: true);
        bossTheme = Synth.ToAudioStream(BossSong.Render(), loop: true);
        droneTheme = Synth.ToAudioStream(DroneSong.Render(), loop: true);
        humanoidTheme = Synth.ToAudioStream(HumanoidSong.Render(), loop: true);
        stationTheme = Synth.ToAudioStream(StationSong.Render(), loop: true);
        music = new AudioStreamPlayer { Stream = runnerTheme, VolumeDb = MusicVolumeDb };
        outgoingMusic = new AudioStreamPlayer();
        AddChild(music);
        AddChild(outgoingMusic);
    }

    /// <summary>
    /// Select a level's running score and speed. During a boss fight, remember the level without interrupting
    /// the battle music or its phase speed; SetBossMusic(false, speed) returns to this selection after victory.
    /// </summary>
    public void SetLevelMusic(LevelMusicTheme theme, float speed)
    {
        levelMusicTheme = theme;
        if (BossMusicActive) return;
        musicSpeed = speed;
        SelectTheme(boss: false);
        SetMusicSpeed(speed);
    }

    /// <summary>Choose a battle score at spawn; victory returns to the selected level score.</summary>
    public void SetBossMusic(bool enabled, float speed, BossMusicTheme theme = BossMusicTheme.TitanCircuit)
    {
        musicSpeed = speed;
        SelectTheme(enabled, theme);
        SetMusicSpeed(speed);
    }

    void SelectTheme(bool boss, BossMusicTheme theme = BossMusicTheme.TitanCircuit)
    {
        AudioStreamWav level = levelMusicTheme == LevelMusicTheme.NeonSlipstream ? stationTheme : runnerTheme;
        AudioStreamWav next = !boss ? level : theme switch
        {
            BossMusicTheme.RotorRush => droneTheme,
            BossMusicTheme.IronShadow => humanoidTheme,
            _ => bossTheme,
        };
        if (music.Stream == next) return;

        musicFade?.Kill();
        FinishTrackFade();
        bool wasPlaying = music.Playing;
        (music, outgoingMusic) = (outgoingMusic, music);
        music.Stream = next;
        music.PitchScale = musicHeld ? heldPitch : musicSpeed;
        if (!MusicOn || !wasPlaying)
        {
            outgoingMusic.Stop();
            music.VolumeDb = MusicVolumeDb;
            return;
        }

        // A brief crossfade avoids a hard cut in the middle of a note. Rendered streams are cached:
        // starting a fight never needs to synthesize a song, and a phase change never restarts it.
        music.VolumeDb = -60f;
        music.Play();
        trackFade = CreateTween().SetIgnoreTimeScale();
        trackFade.TweenProperty(music, AudioStreamPlayer.PropertyName.VolumeDb.ToString(), MusicVolumeDb, 0.3);
        trackFade.Parallel().TweenProperty(outgoingMusic, AudioStreamPlayer.PropertyName.VolumeDb.ToString(), -60f, 0.3);
        var fadingOut = outgoingMusic;
        trackFade.TweenCallback(Callable.From(fadingOut.Stop));
    }

    void FinishTrackFade()
    {
        trackFade?.Kill();
        trackFade = null;
        outgoingMusic.Stop();
        music.VolumeDb = MusicVolumeDb;
    }

    // ---------- The WORLD MAP's own tune: "Island Hop" (see MapSong.cs and Main.WorldMap.cs) ----------

    AudioStreamPlayer mapMusic = null!; // it keeps playing while the game is frozen behind the map

    /// <summary>Makes the map's tune. (Called at the very END of _Ready, so every older sound stays exactly the same.)</summary>
    void SetUpMapMusic()
    {
        mapMusic = new AudioStreamPlayer
        {
            Stream = Synth.ToAudioStream(MapSong.Render(), loop: true),
            VolumeDb = MusicVolumeDb - 2,
            ProcessMode = ProcessModeEnum.Always, // (the game is paused, but this one plays on, like the pause screen's blip)
        };
        AddChild(mapMusic);
    }

    /// <summary>True while the map's tune is playing (the self-test reads this).</summary>
    public bool MapMusicPlaying => mapMusic.Playing;

    /// <summary>The map opens: the level's music waits right where it is, and the map's tune plays from the start.</summary>
    public void StartMapMusic()
    {
        HoldMusicForPause(true);
        if (MusicOn) mapMusic.Play();
    }

    /// <summary>
    /// The map closes: its tune STOPS (it's never just paused), and the level's music goes on. (Main unpauses the game
    /// FIRST: the music's fades belong to the frozen game.)
    /// </summary>
    public void StopMapMusic()
    {
        mapMusic.Stop();
        HoldMusicForPause(false);
    }
}
