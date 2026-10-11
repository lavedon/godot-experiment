using Godot;

/// <summary>A boss chooses its score independently of its name or implementation.</summary>
public enum BossMusicTheme { TitanCircuit, RotorRush, IronShadow }

public partial class SoundBoard
{
    AudioStreamWav runnerTheme = null!;
    AudioStreamWav bossTheme = null!;
    AudioStreamWav droneTheme = null!;
    AudioStreamWav humanoidTheme = null!;
    AudioStreamPlayer outgoingMusic = null!;
    Tween? trackFade;

    /// <summary>The selected theme, even while muted or rebuilding (also read by the self-test).</summary>
    public bool BossMusicActive => music.Stream == bossTheme || music.Stream == droneTheme || music.Stream == humanoidTheme;
    public bool DroneMusicActive => music.Stream == droneTheme;
    public bool HumanoidMusicActive => music.Stream == humanoidTheme;

    void SetUpMusic()
    {
        runnerTheme = Synth.ToAudioStream(Song.Render(), loop: true);
        bossTheme = Synth.ToAudioStream(BossSong.Render(), loop: true);
        droneTheme = Synth.ToAudioStream(DroneSong.Render(), loop: true);
        humanoidTheme = Synth.ToAudioStream(HumanoidSong.Render(), loop: true);
        music = new AudioStreamPlayer { Stream = runnerTheme, VolumeDb = MusicVolumeDb };
        outgoingMusic = new AudioStreamPlayer();
        AddChild(music);
        AddChild(outgoingMusic);
    }

    /// <summary>Choose a battle score at spawn; speed follows the world and boss phase.</summary>
    public void SetBossMusic(bool enabled, float speed, BossMusicTheme theme = BossMusicTheme.TitanCircuit)
    {
        musicSpeed = speed;
        SelectTheme(enabled, theme);
        SetMusicSpeed(speed);
    }

    void SelectTheme(bool boss, BossMusicTheme theme = BossMusicTheme.TitanCircuit)
    {
        AudioStreamWav next = !boss ? runnerTheme : theme switch
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
}
