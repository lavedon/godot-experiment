using Godot;

public partial class Main
{
    async Task HumanoidMusicFlow()
    {
        AudioStreamPlayer[] PlayingMusic() => sounds.GetChildren().OfType<AudioStreamPlayer>().Where(player =>
            player.Playing && player.Stream is AudioStreamWav { LoopMode: AudioStreamWav.LoopModeEnum.Forward }).ToArray();

        sounds.SetMusicOn(true);
        sounds.SetBossMusic(true, 1.1f, BossMusicTheme.IronShadow);
        await Wait(0.5);
        Check(sounds.BossMusicActive && sounds.HumanoidMusicActive && !sounds.DroneMusicActive && PlayingMusic().Length == 1,
              "Iron Shadow crossfades from the runner score and is recognized as boss music");
        var player = PlayingMusic().Single();
        var humanoidStream = player.Stream;
        player.Seek(14);
        sounds.SetBossMusic(true, 1.22f, BossMusicTheme.IronShadow);
        await Wait(0.7);
        Check(PlayingMusic().Single() == player && player.Stream == humanoidStream && player.GetPlaybackPosition() >= 14,
              "changing the battle phase keeps the cached humanoid score in its current phrase");
        Check(Mathf.Abs(sounds.MusicPitch - 1.22f) < 0.01f, "the theme follows the supplied world and phase speed");

        sounds.DipMusic();
        await Wait(0.7);
        sounds.SetMusicOn(false);
        Check(PlayingMusic().Length == 0, "mute stops both music players during a humanoid rebuild");
        sounds.SetMusicOn(true);
        Check(sounds.HumanoidMusicActive && Mathf.Abs(sounds.MusicPitch - 0.6f) < 0.01f,
              "unmuting preserves Iron Shadow and the rebuild dip");
        sounds.SetMusicSpeed(1.28f);
        sounds.RestoreMusic();
        await Wait(0.8);
        Check(sounds.HumanoidMusicActive && Mathf.Abs(sounds.MusicPitch - 1.28f) < 0.01f,
              "restoring uses the latest speed without losing the humanoid theme");

        sounds.SetMusicOn(false);
        sounds.SetBossMusic(false, 1);
        sounds.SetBossMusic(true, 1.1f, BossMusicTheme.IronShadow);
        Check(sounds.HumanoidMusicActive && PlayingMusic().Length == 0, "a muted spawn selects Iron Shadow silently");
        sounds.SetMusicOn(true);
        Check(PlayingMusic().Length == 1 && PlayingMusic()[0].Stream == humanoidStream,
              "unmuting reuses the cached humanoid composition");

        sounds.SetBossMusic(true, 1.1f, BossMusicTheme.RotorRush);
        await Wait(0.5);
        Check(sounds.DroneMusicActive && !sounds.HumanoidMusicActive && PlayingMusic().Length == 1,
              "DRONEZILLA can still select Rotor Rush after Iron Shadow");
        sounds.SetBossMusic(true, 1.1f);
        await Wait(0.5);
        Check(sounds.BossMusicActive && !sounds.HumanoidMusicActive && !sounds.DroneMusicActive && PlayingMusic().Length == 1,
              "existing two-argument calls still select Titan Circuit");
        sounds.SetBossMusic(true, 1.1f, BossMusicTheme.IronShadow);
        sounds.SetBossMusic(false, 1);
        sounds.SetBossMusic(true, 1.1f, BossMusicTheme.RotorRush);
        sounds.SetBossMusic(true, 1.1f, BossMusicTheme.IronShadow);
        await Wait(0.5);
        Check(sounds.HumanoidMusicActive && PlayingMusic().Length == 1 && PlayingMusic()[0].Stream == humanoidStream,
              "rapid changes across all four scores leave only the last selection playing");

        sounds.DipMusic();
        await Wait(0.7);
        sounds.SetBossMusic(false, 1.05f);
        await Wait(0.5);
        Check(!sounds.BossMusicActive && Mathf.Abs(sounds.MusicPitch - 0.6f) < 0.01f && PlayingMusic().Length == 1,
              "victory can leave Iron Shadow while preserving an active rebuild dip");
        sounds.RestoreMusic();
        await Wait(0.8);
        Check(Mathf.Abs(sounds.MusicPitch - 1.05f) < 0.01f, "the runner score recovers to the post-victory speed");

        sounds.SetBossMusic(true, 1.1f, BossMusicTheme.IronShadow);
        sounds.SetMusicOn(false);
        await Wait(0.5);
        Check(PlayingMusic().Length == 0, "muting during the humanoid crossfade cannot revive the outgoing score");
        sounds.SetMusicOn(true);
        sounds.SetBossMusic(false, 1);
        sounds.SetBossMusic(true, 1.1f, BossMusicTheme.IronShadow);
        sounds.PowerDownMusic();
        await Wait(1);
        Check(PlayingMusic().Length == 0, "game over stops Iron Shadow even during its crossfade");
        sounds.StartMusic();
        await Wait(0.5);
        Check(!sounds.BossMusicActive && PlayingMusic().Length == 1, "a fresh run returns to the runner score");
        sounds.SetBossMusic(true, 1.1f, BossMusicTheme.IronShadow);
        sounds.StopEverything();
        await Wait(0.5);
        Check(PlayingMusic().Length == 0, "shutdown stops both sides of the humanoid crossfade");
    }

    Task HumanoidMusicAudio()
    {
        float[] samples = HumanoidSong.Render();
        double seconds = (double)samples.Length / Synth.SampleRate;
        Check(Math.Abs(seconds - HumanoidSong.Bars * 4 * 60 / HumanoidSong.BeatsPerMinute) < 0.02,
              $"Iron Shadow is a complete 32-bar loop ({seconds:0.00} seconds at {HumanoidSong.BeatsPerMinute} BPM)");
        Check(samples.All(float.IsFinite), "all humanoid-song samples are finite");
        float peak = samples.Max(MathF.Abs);
        double rms = Math.Sqrt(samples.Average(s => (double)s * s));
        Check(peak <= 0.851f && rms is > 0.06 and < 0.35,
              $"the mechanical mix leaves room for attack cues: peak {peak:0.000}, RMS {rms:0.000}");
        Check(Math.Abs(samples.Average(s => (double)s)) < 0.01, "the humanoid mix has no significant DC offset");
        float seam = MathF.Abs(samples[0] - samples[^1]);
        Check(seam < 0.035f, $"the loop wraps without an abrupt edge ({seam:0.0000})");
        int barSamples = samples.Length / HumanoidSong.Bars;
        double quietestBar = Enumerable.Range(0, HumanoidSong.Bars).Min(bar =>
            Math.Sqrt(samples.Skip(bar * barSamples).Take(barSamples).Average(s => (double)s * s)));
        Check(quietestBar > 0.025, $"even the stalking passage stays audible (quietest bar RMS {quietestBar:0.000})");
        using var stream = Synth.ToAudioStream(samples, loop: true);
        Check(stream.LoopMode == AudioStreamWav.LoopModeEnum.Forward && stream.LoopBegin == 0 && stream.LoopEnd == samples.Length,
              "Godot loops the full humanoid composition without a silent tail");
        string preview = Path.Combine(Path.GetDirectoryName(selfTestDbPath)!, "iron-shadow.wav");
        Check(stream.SaveToWav(preview) == Error.Ok, $"listenable preview saved to {preview}");
        return Task.CompletedTask;
    }
}
