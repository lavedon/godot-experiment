using Godot;

public partial class Main
{
    // Keep the music checks together so new bosses can share the same playback contract.
    async Task BossMusicFlow()
    {
        int PlayingLoops() => sounds.GetChildren().OfType<AudioStreamPlayer>().Count(player =>
            player.Playing && player.Stream is AudioStreamWav { LoopMode: AudioStreamWav.LoopModeEnum.Forward });

        sounds.SetMusicOn(true);
        Check(!sounds.BossMusicActive, "title screen uses the runner theme");
        var rusty = StartBossFight("TBattleMusic", world: 1);
        Check(sounds.BossMusicActive, "spawning a boss selects the dedicated battle theme");
        await Wait(0.8);
        Check(PlayingLoops() == 1, "only the battle theme plays after the crossfade");
        Check(Mathf.Abs(sounds.MusicPitch - 1.1f * CurrentWorld.MusicSpeed) < 0.01f, "battle tempo follows the world");
        rusty.LoseHealth(2);
        Check(await WaitUntil(() => sounds.BossMusicActive && Mathf.Abs(sounds.MusicPitch - 1.15f * CurrentWorld.MusicSpeed) < 0.01f, 3),
              "the angry phase speeds up the battle theme");

        robot.BlinkTime = 0;
        Check(Bonk(null) && state == GameState.Rebuilding, "a crash in battle uses the spare battery");
        await Wait(0.7);
        sounds.SetMusicOn(false);
        Check(PlayingLoops() == 0, "mute stops both music players");
        sounds.SetMusicOn(true);
        Check(sounds.BossMusicActive && Mathf.Abs(sounds.MusicPitch - 0.6f) < 0.01f,
              "unmuting during a rebuild keeps the boss theme dipped");
        Check(await WaitUntil(() => state == GameState.Playing, 4), "the battle resumes after rebuilding");
        robot.BlinkTime = 1e9f;
        await Wait(0.8);
        Check(sounds.BossMusicActive && Mathf.Abs(sounds.MusicPitch - 1.15f * CurrentWorld.MusicSpeed) < 0.01f,
              "rebuilding restores the current boss-phase tempo");
        rusty.LoseHealth(4);
        Check(await WaitUntil(() => rusty.HasBlownUp, 3), "the boss is defeated");
        await Wait(0.8);
        Check(!sounds.BossMusicActive && PlayingLoops() == 1, "victory crossfades back to just the runner theme");
        Check(Mathf.Abs(sounds.MusicPitch - CurrentWorld.MusicSpeed) < 0.01f, "victory restores the world's tempo");

        // Select both themes with music off: turning it on must use the latest battle state.
        GoToTitle();
        sounds.SetMusicOn(false);
        StartBossFight("TMutedBattle");
        Check(sounds.BossMusicActive && PlayingLoops() == 0, "a muted boss spawn remembers the theme without playing");
        sounds.SetMusicOn(true);
        Check(sounds.BossMusicActive && PlayingLoops() == 1, "unmute starts the battle theme");
        sounds.SetMusicOn(false);
        sounds.SetBossMusic(false, CurrentWorld.MusicSpeed);
        Check(!sounds.BossMusicActive && PlayingLoops() == 0, "a muted victory selects the runner theme silently");
        sounds.SetMusicOn(true);
        Check(!sounds.BossMusicActive && PlayingLoops() == 1, "unmuting after victory plays the runner theme");

        // Interrupt the actual crossfade: no delayed tween may revive an old song later.
        sounds.SetBossMusic(true, 1.1f);
        sounds.SetMusicOn(false);
        await Wait(0.5);
        Check(PlayingLoops() == 0, "muting during a crossfade leaves no music behind");
        sounds.SetMusicOn(true);
        sounds.SetBossMusic(false, 1);
        spareBatteries = 0;
        robot.BlinkTime = 0;
        Check(Bonk(null) && state == GameState.GameOver, "a final crash powers down during the crossfade");
        await Wait(1);
        Check(PlayingLoops() == 0, "powering down during a crossfade stops all music");
        StartGame();
        QuietRoad();
        await Wait(0.8);
        Check(!sounds.BossMusicActive && sounds.MusicPlaying && Mathf.Abs(sounds.MusicPitch - 1) < 0.01f,
              "restarting after a battle returns to the runner theme at normal pitch");

        sounds.SetBossMusic(true, 1.1f);
        sounds.DipMusic();
        await Wait(0.7);
        sounds.SetBossMusic(false, 1.05f);
        await Wait(0.4);
        Check(!sounds.BossMusicActive && Mathf.Abs(sounds.MusicPitch - 0.6f) < 0.01f,
              "a victory during rebuilding changes theme without releasing the dip");
        sounds.RestoreMusic();
        await Wait(0.8);
        Check(Mathf.Abs(sounds.MusicPitch - 1.05f) < 0.01f, "restoring after that victory uses the new tempo");
        sounds.SetBossMusic(true, 1.1f);
        sounds.StopEverything();
        await Wait(0.5);
        Check(PlayingLoops() == 0, "shutdown stops both sides of a crossfade");
    }

    Task BossMusicAudio()
    {
        float[] samples = BossSong.Render();
        double seconds = (double)samples.Length / Synth.SampleRate;
        Check(Math.Abs(seconds - BossSong.Bars * 4 * 60 / BossSong.BeatsPerMinute) < 0.02,
              $"the complete 32-bar composition lasts {seconds:0.00} seconds");
        Check(samples.All(float.IsFinite), "every rendered sample is finite");
        float peak = samples.Max(MathF.Abs);
        double rms = Math.Sqrt(samples.Average(s => (double)s * s));
        Check(peak <= 0.851f && rms is > 0.06 and < 0.35, $"music has headroom: peak {peak:0.000}, RMS {rms:0.000}");
        Check(Math.Abs(samples.Average(s => (double)s)) < 0.01, "the mix has no significant DC offset");
        Check(MathF.Abs(samples[0] - samples[^1]) < 0.035f,
              $"the loop has no abrupt boundary jump ({MathF.Abs(samples[0] - samples[^1]):0.0000})");
        using var stream = Synth.ToAudioStream(samples, loop: true);
        Check(stream.LoopMode == AudioStreamWav.LoopModeEnum.Forward && stream.LoopBegin == 0 && stream.LoopEnd == samples.Length,
              "Godot loops the entire track with no extra silent tail");
        // A listenable review copy beside the isolated test database; never in the player's save folder.
        string preview = Path.Combine(Path.GetDirectoryName(selfTestDbPath)!, "titan-circuit.wav");
        Check(stream.SaveToWav(preview) == Error.Ok, $"WAV preview exported to {preview}");
        return Task.CompletedTask;
    }
}
