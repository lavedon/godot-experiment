using Godot;

public partial class Main
{
    async Task DroneMusicFlow()
    {
        AudioStreamPlayer[] PlayingMusic() => sounds.GetChildren().OfType<AudioStreamPlayer>().Where(player =>
            player.Playing && player.Stream is AudioStreamWav { LoopMode: AudioStreamWav.LoopModeEnum.Forward }).ToArray();

        sounds.SetMusicOn(true);
        sounds.SetBossMusic(true, 1.1f, BossMusicTheme.RotorRush);
        await Wait(0.5);
        Check(sounds.BossMusicActive && sounds.DroneMusicActive && PlayingMusic().Length == 1,
              "the drone selects its own battle theme and finishes the runner crossfade");
        var player = PlayingMusic().Single();
        var droneStream = player.Stream;
        Check(Mathf.Abs(sounds.MusicPitch - 1.1f) < 0.01f, "the new song follows the supplied battle speed");
        player.Seek(12);
        sounds.SetBossMusic(true, 1.22f, BossMusicTheme.RotorRush);
        await Wait(0.7);
        Check(PlayingMusic().Single() == player && player.Stream == droneStream && player.GetPlaybackPosition() >= 12,
              "a phase change keeps the cached song playing without restarting its phrase");
        Check(Mathf.Abs(sounds.MusicPitch - 1.22f) < 0.01f, "the furious phase speeds up Rotor Rush");

        sounds.DipMusic();
        await Wait(0.6);
        sounds.SetMusicOn(false);
        Check(PlayingMusic().Length == 0, "muting the drone fight stops both crossfade players");
        sounds.SetMusicOn(true);
        Check(sounds.DroneMusicActive && Mathf.Abs(sounds.MusicPitch - 0.6f) < 0.01f,
              "unmuting keeps the drone theme and the rebuild dip");
        sounds.RestoreMusic();
        await Wait(0.8);
        Check(sounds.DroneMusicActive && Mathf.Abs(sounds.MusicPitch - 1.22f) < 0.01f,
              "rebuilding restores the drone's latest battle tempo");

        sounds.SetMusicOn(false);
        sounds.SetBossMusic(false, 1);
        sounds.SetBossMusic(true, 1.15f, BossMusicTheme.RotorRush);
        Check(sounds.DroneMusicActive && PlayingMusic().Length == 0, "a muted spawn remembers the drone score silently");
        sounds.SetMusicOn(true);
        Check(PlayingMusic().Length == 1 && PlayingMusic()[0].Stream == droneStream, "unmute reuses the cached drone stream");

        sounds.SetBossMusic(true, 1.1f); // existing two-argument calls still mean Titan Circuit
        await Wait(0.5);
        Check(sounds.BossMusicActive && !sounds.DroneMusicActive && PlayingMusic().Length == 1,
              "Big Rusty and VROOMBA keep Titan Circuit with the existing API");
        sounds.SetBossMusic(true, 1.1f, BossMusicTheme.RotorRush);
        sounds.SetBossMusic(false, 1);
        sounds.SetBossMusic(true, 1.1f, BossMusicTheme.RotorRush);
        await Wait(0.5);
        Check(sounds.DroneMusicActive && PlayingMusic().Length == 1 && PlayingMusic()[0].Stream == droneStream,
              "rapid changes between all three scores leave only the last song playing");
        sounds.SetBossMusic(false, 1);
        await Wait(0.5);
        Check(!sounds.BossMusicActive && !sounds.DroneMusicActive && PlayingMusic().Length == 1,
              "victory returns from Rotor Rush to the runner theme");

        sounds.SetBossMusic(true, 1.1f, BossMusicTheme.RotorRush);
        sounds.PowerDownMusic();
        await Wait(1);
        Check(PlayingMusic().Length == 0, "a final crash stops the drone music even during its crossfade");
        sounds.StartMusic();
        await Wait(0.5);
        Check(!sounds.BossMusicActive && PlayingMusic().Length == 1, "restarting selects the runner score again");
        sounds.SetBossMusic(true, 1.1f, BossMusicTheme.RotorRush);
        sounds.StopEverything();
        await Wait(0.5);
        Check(PlayingMusic().Length == 0, "shutdown stops both sides of a drone crossfade");
    }

    Task DroneMusicAudio()
    {
        float[] samples = DroneSong.Render();
        double seconds = (double)samples.Length / Synth.SampleRate;
        Check(Math.Abs(seconds - DroneSong.Bars * 4 * 60 / DroneSong.BeatsPerMinute) < 0.02,
              $"Rotor Rush is a complete 32-bar loop ({seconds:0.00} seconds at {DroneSong.BeatsPerMinute} BPM)");
        Check(samples.All(float.IsFinite), "every drone-song sample is finite");
        float peak = samples.Max(MathF.Abs);
        double rms = Math.Sqrt(samples.Average(s => (double)s * s));
        Check(peak <= 0.851f && rms is > 0.06 and < 0.35, $"the new mix leaves room for warnings: peak {peak:0.000}, RMS {rms:0.000}");
        Check(Math.Abs(samples.Average(s => (double)s)) < 0.01, "the pulse voices leave no significant DC offset");
        float seam = MathF.Abs(samples[0] - samples[^1]);
        Check(seam < 0.035f, $"the loop wraps without a hard edge ({seam:0.0000})");
        int barSamples = samples.Length / DroneSong.Bars;
        double quietestBar = Enumerable.Range(0, DroneSong.Bars).Min(bar =>
            Math.Sqrt(samples.Skip(bar * barSamples).Take(barSamples).Average(s => (double)s * s)));
        Check(quietestBar > 0.025, $"every bar carries music, including the hovering bridge (quietest RMS {quietestBar:0.000})");
        using var stream = Synth.ToAudioStream(samples, loop: true);
        Check(stream.LoopMode == AudioStreamWav.LoopModeEnum.Forward && stream.LoopBegin == 0 && stream.LoopEnd == samples.Length,
              "Godot repeats the entire drone composition with no silent tail");
        string preview = Path.Combine(Path.GetDirectoryName(selfTestDbPath)!, "rotor-rush.wav");
        Check(stream.SaveToWav(preview) == Error.Ok, $"listenable preview saved to {preview}");
        return Task.CompletedTask;
    }
}
