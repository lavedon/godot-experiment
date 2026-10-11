using Godot;

public partial class Main
{
    async Task DronezillaMusicBattle()
    {
        sounds.SetMusicOn(true);
        var drone = StartDronezillaFight("TDroneBattleMusic", world: 1);
        Check(sounds.BossMusicActive && sounds.DroneMusicActive, "spawning DRONEZILLA automatically selects Rotor Rush");
        await Wait(0.8);
        Check(sounds.MusicPlaying && Mathf.Abs(sounds.MusicPitch - 1.1f * CurrentWorld.MusicSpeed) < 0.01f,
              "DRONEZILLA's entrance music follows the current world speed");

        drone.LoseHealth(2);
        Check(await WaitUntil(() => sounds.DroneMusicActive && Mathf.Abs(sounds.MusicPitch - 1.15f * CurrentWorld.MusicSpeed) < 0.01f, 4),
              "the angry phase speeds up Rotor Rush without switching tracks");
        robot.BlinkTime = 0;
        Check(Bonk(null) && state == GameState.Rebuilding, "a crash in the drone fight starts the real rebuild flow");
        await Wait(0.7);
        sounds.SetMusicOn(false);
        sounds.SetMusicOn(true);
        Check(sounds.DroneMusicActive && Mathf.Abs(sounds.MusicPitch - 0.6f) < 0.01f,
              "muting and unmuting during the rebuild preserves the drone score and its dip");
        Check(await WaitUntil(() => state == GameState.Playing, 4), "the hero finishes rebuilding in the drone fight");
        robot.BlinkTime = 1e9f;
        await Wait(0.8);
        Check(sounds.DroneMusicActive && Mathf.Abs(sounds.MusicPitch - 1.15f * CurrentWorld.MusicSpeed) < 0.01f,
              "the rebuilt hero hears the drone score at the current phase speed again");

        drone.LoseHealth(2);
        Check(await WaitUntil(() => sounds.DroneMusicActive && Mathf.Abs(sounds.MusicPitch - 1.22f * CurrentWorld.MusicSpeed) < 0.01f, 4),
              "the furious phase keeps Rotor Rush and raises its tempo");
        drone.DoAttack(DroneAttack.Swoop);
        Check(await FastForwardUntil(() => drone.CanBeStomped, 14), "DRONEZILLA finishes a swoop and lands to recharge");
        Check(sounds.DroneMusicActive && sounds.MusicPlaying, "the score continues through his low-battery stomp window");
        drone.LoseHealth(BossBase.MaxHealth);
        Check(await WaitUntil(() => drone.HasBlownUp, 4), "DRONEZILLA is defeated through his normal victory flow");
        await Wait(0.5);
        Check(!sounds.BossMusicActive && sounds.MusicPlaying, "defeating DRONEZILLA returns to the runner theme");

        GoToTitle();
        sounds.SetMusicOn(false);
        drone = StartDronezillaFight("TMutedDroneMusic");
        Check(sounds.DroneMusicActive && !sounds.MusicPlaying, "a muted DRONEZILLA spawn still remembers Rotor Rush");
        sounds.SetMusicOn(true);
        Check(sounds.DroneMusicActive && sounds.MusicPlaying, "unmuting starts the score chosen by DRONEZILLA's spawn");
        spareBatteries = 0;
        robot.BlinkTime = 0;
        Check(Bonk(null) && state == GameState.GameOver, "a final crash ends the drone encounter");
        await Wait(1);
        Check(!sounds.MusicPlaying, "game over powers down Rotor Rush");
        StartGame();
        QuietRoad();
        await Wait(0.5);
        Check(!sounds.BossMusicActive && sounds.MusicPlaying, "restarting after DRONEZILLA returns to the runner score");

        GoToTitle();
        StartBossFight("TRustyKeepsMusic");
        Check(sounds.BossMusicActive && !sounds.DroneMusicActive, "BIG RUSTY still selects Titan Circuit");
        GoToTitle();
        StartVroombaFight("TVroombaKeepsMusic");
        Check(sounds.BossMusicActive && !sounds.DroneMusicActive, "VROOMBA still selects Titan Circuit");
    }

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
