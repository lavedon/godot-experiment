using Godot;

public partial class Main
{
    async Task StationMusicFlow()
    {
        AudioStreamPlayer[] PlayingMusic() => sounds.GetChildren().OfType<AudioStreamPlayer>().Where(player =>
            player.Playing && player.Stream is AudioStreamWav { LoopMode: AudioStreamWav.LoopModeEnum.Forward }).ToArray();

        sounds.SetMusicOn(true);
        sounds.SetLevelMusic(LevelMusicTheme.NeonSlipstream, 1.1f);
        await Wait(0.7);
        Check(sounds.StationMusicActive && !sounds.BossMusicActive && PlayingMusic().Length == 1,
              "entering the station crossfades to its own running score");
        var player = PlayingMusic().Single();
        var stationStream = player.Stream;
        Check(Mathf.Abs(sounds.MusicPitch - 1.1f) < 0.01f, "the station theme follows the level's speed");
        player.Seek(18);
        sounds.SetLevelMusic(LevelMusicTheme.NeonSlipstream, 1.15f);
        await Wait(0.7);
        Check(PlayingMusic().Single() == player && player.Stream == stationStream && player.GetPlaybackPosition() >= 18,
              "reselecting the level with a new speed keeps the current musical phrase");
        Check(Mathf.Abs(sounds.MusicPitch - 1.15f) < 0.01f, "speed changes still reach the selected level score");

        sounds.DipMusic();
        await Wait(0.7);
        sounds.SetMusicOn(false);
        Check(PlayingMusic().Length == 0, "muting during a station rebuild stops both crossfade players");
        sounds.SetMusicOn(true);
        Check(sounds.StationMusicActive && Mathf.Abs(sounds.MusicPitch - 0.6f) < 0.01f,
              "unmuting preserves the station score and its rebuild dip");
        sounds.SetMusicSpeed(1.23f);
        sounds.RestoreMusic();
        await Wait(0.8);
        Check(sounds.StationMusicActive && Mathf.Abs(sounds.MusicPitch - 1.23f) < 0.01f,
              "rebuilding restores the latest running tempo");

        // Every boss can interrupt this level; its own score and phase tempo must survive level bookkeeping.
        foreach (var theme in Enum.GetValues<BossMusicTheme>())
        {
            sounds.SetBossMusic(true, 1.28f, theme);
            await Wait(0.7);
            var bossPlayer = PlayingMusic().Single();
            var bossStream = bossPlayer.Stream;
            sounds.SetLevelMusic(LevelMusicTheme.Runner, 0.95f);
            sounds.SetLevelMusic(LevelMusicTheme.NeonSlipstream, 1.1f);
            Check(sounds.BossMusicActive && !sounds.StationMusicActive && bossPlayer.Stream == bossStream
                  && Mathf.Abs(sounds.MusicPitch - 1.28f) < 0.01f,
                  $"{theme} keeps its battle score and tempo when the level selection is refreshed");
            sounds.SetBossMusic(false, 1.1f);
            await Wait(0.7);
            Check(sounds.StationMusicActive && !sounds.BossMusicActive && PlayingMusic().Length == 1
                  && PlayingMusic()[0].Stream == stationStream && Mathf.Abs(sounds.MusicPitch - 1.1f) < 0.01f,
                  $"victory after {theme} returns to the cached station score at its level speed");
        }

        sounds.SetMusicOn(false);
        sounds.SetBossMusic(true, 1.3f, BossMusicTheme.IronShadow);
        sounds.SetLevelMusic(LevelMusicTheme.Runner, 0.95f);
        Check(sounds.HumanoidMusicActive && PlayingMusic().Length == 0, "a muted world change remembers the destination without interrupting the boss");
        sounds.SetBossMusic(false, 0.95f);
        sounds.SetMusicOn(true);
        Check(!sounds.StationMusicActive && !sounds.BossMusicActive && Mathf.Abs(sounds.MusicPitch - 0.95f) < 0.01f,
              "victory uses the latest level selection even when it changed while muted");

        sounds.SetMusicOn(false);
        sounds.SetLevelMusic(LevelMusicTheme.NeonSlipstream, 1.1f);
        Check(sounds.StationMusicActive && PlayingMusic().Length == 0, "a muted station entry selects its score silently");
        sounds.SetMusicOn(true);
        Check(PlayingMusic().Length == 1 && PlayingMusic()[0].Stream == stationStream,
              "unmute starts the remembered station stream");
        sounds.DipMusic();
        await Wait(0.7);
        sounds.SetLevelMusic(LevelMusicTheme.Runner, 1.05f);
        await Wait(0.5);
        Check(!sounds.StationMusicActive && !sounds.BossMusicActive && Mathf.Abs(sounds.MusicPitch - 0.6f) < 0.01f,
              "leaving the station during a rebuild changes theme while preserving the dip");
        sounds.RestoreMusic();
        await Wait(0.8);
        Check(Mathf.Abs(sounds.MusicPitch - 1.05f) < 0.01f, "restoring after a level change uses the destination speed");

        sounds.SetLevelMusic(LevelMusicTheme.NeonSlipstream, 1.1f);
        sounds.SetBossMusic(true, 1.2f, BossMusicTheme.RotorRush);
        sounds.SetBossMusic(false, 1.1f);
        sounds.SetLevelMusic(LevelMusicTheme.Runner, 1);
        sounds.SetLevelMusic(LevelMusicTheme.NeonSlipstream, 1.1f);
        await Wait(0.7);
        Check(sounds.StationMusicActive && PlayingMusic().Length == 1 && PlayingMusic()[0].Stream == stationStream,
              "rapid level and boss transitions leave only the last score playing");
        sounds.SetLevelMusic(LevelMusicTheme.Runner, 1);
        sounds.SetLevelMusic(LevelMusicTheme.NeonSlipstream, 1.1f);
        sounds.SetMusicOn(false);
        await Wait(0.5);
        Check(PlayingMusic().Length == 0, "muting during a level crossfade cannot revive either player");
        sounds.SetMusicOn(true);
        sounds.PowerDownMusic();
        await Wait(1);
        Check(PlayingMusic().Length == 0, "game over powers down the station music");
        sounds.StartMusic();
        await Wait(0.5);
        Check(!sounds.StationMusicActive && !sounds.BossMusicActive && PlayingMusic().Length == 1,
              "the default title or restart returns to the original runner score");
        sounds.SetBossMusic(true, 1.1f);
        sounds.SetBossMusic(false, 1);
        await Wait(0.5);
        Check(!sounds.StationMusicActive && !sounds.BossMusicActive, "restarting also forgets the old station for future victories");
        sounds.StartMusic(1.1f, LevelMusicTheme.NeonSlipstream);
        await Wait(0.5);
        Check(sounds.StationMusicActive && PlayingMusic().Length == 1, "a direct level start can explicitly select Neon Slipstream");
        sounds.SetLevelMusic(LevelMusicTheme.Runner, 1);
        sounds.StopEverything();
        await Wait(0.5);
        Check(PlayingMusic().Length == 0, "shutdown stops both sides of a level crossfade");
    }

    Task StationMusicAudio()
    {
        float[] samples = StationSong.Render();
        double seconds = (double)samples.Length / Synth.SampleRate;
        Check(Math.Abs(seconds - StationSong.Bars * 4 * 60 / StationSong.BeatsPerMinute) < 0.02,
              $"Neon Slipstream is a complete 48-bar loop ({seconds:0.00} seconds at {StationSong.BeatsPerMinute} BPM)");
        Check(samples.All(float.IsFinite), "all station-song samples are finite");
        float peak = samples.Max(MathF.Abs);
        double rms = Math.Sqrt(samples.Average(s => (double)s * s));
        Check(peak <= 0.851f && rms is > 0.06 and < 0.35,
              $"the station mix leaves room for game sounds: peak {peak:0.000}, RMS {rms:0.000}");
        Check(Math.Abs(samples.Average(s => (double)s)) < 0.01, "the station mix has no significant DC offset");
        float seam = MathF.Abs(samples[0] - samples[^1]);
        Check(seam < 0.035f, $"the tunnel echoes wrap without a hard edge ({seam:0.0000})");
        int barSamples = samples.Length / StationSong.Bars;
        double quietestBar = Enumerable.Range(0, StationSong.Bars).Min(bar =>
            Math.Sqrt(samples.Skip(bar * barSamples).Take(barSamples).Average(s => (double)s * s)));
        Check(quietestBar > 0.025, $"every bar stays audible, including the floating section (quietest RMS {quietestBar:0.000})");
        using var stream = Synth.ToAudioStream(samples, loop: true);
        Check(stream.LoopMode == AudioStreamWav.LoopModeEnum.Forward && stream.LoopBegin == 0 && stream.LoopEnd == samples.Length,
              "Godot loops the entire station track with no extra silent tail");
        string preview = Path.Combine(Path.GetDirectoryName(selfTestDbPath)!, "neon-slipstream.wav");
        Check(stream.SaveToWav(preview) == Error.Ok, $"listenable preview saved to {preview}");
        return Task.CompletedTask;
    }
}
