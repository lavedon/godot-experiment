using Godot;

/// <summary>
/// Spare batteries and the magic rebuild! (This is more of Main: "partial" means one class spread over several files.)
///
/// Bolt-E starts each game with a spare battery, and every 100 bolts charges another one (up to 3).
/// When he crashes with a spare, there's still a big KABOOM... then a battery zooms over, every piece flies back
/// into place (the head goes last), his face screen boots up, and he flips and keeps riding.
/// For a moment he blinks: he can't crash, but he can still stomp and grab bolts.
/// With no spare left, a crash is the usual game over.
/// </summary>
public partial class Main
{
    // ---- Spare batteries (try changing these! StartingBatteries = 0 is the classic game) ----
    const int StartingBatteries = 1;          // spare batteries at the start of every game
    public const int MaxBatteries = 3;        // the most spares Bolt-E can carry
    const int BoltsPerBattery = 100;          // bolts it takes to charge a new spare
    const int PointsWhenBatteriesFull = 100;  // bonus points for 100 bolts when he already has the most spares
    const float RebuildDelay = 1.0f;          // seconds to watch the KABOOM before the pieces fly back
    const float RebuildSeconds = 0.9f;        // how long the pieces take to fly back together
    const float SafeBlinkSeconds = 2.5f;      // how long he blinks (and can't crash) after the rebuild
    const float SpeedAfterRebuild = 0.85f;    // the world speeds back up to 85% of the speed before the crash
    const float ClearAround = 500f;           // dangerous things closer than this (in pixels) puff away after the rebuild

    int spareBatteries;        // green batteries left
    int boltsTowardBattery;    // bolts collected toward the next spare (0 to 99)
    int batteriesUsed;         // spares used up this game (saved with the score)
    float rebuildTimer;        // seconds since the crash
    float speedAtCrash;        // how fast the world was going when Bolt-E crashed
    bool rebuildStarted;       // true once the pieces have started flying back
    bool batteryFlown;         // true once the battery has zoomed over from the corner
    bool crashedInBossFight;   // crashed in a Big Rusty fight (then the speed after the fight stays the same)

    /// <summary>Every bolt charges the yellow battery a little. 100 bolts = a new spare battery (1-UP!).</summary>
    void ChargeSpareBattery()
    {
        boltsTowardBattery++;
        if (boltsTowardBattery >= BoltsPerBattery)
        {
            boltsTowardBattery = 0;
            var aboveBoltE = robot.Position + new Vector2(0, -170);
            if (spareBatteries < MaxBatteries)
            {
                spareBatteries++;
                popups.Add("1-UP!", aboveBoltE, new Color(0.45f, 1f, 0.5f), 44);
                sounds.Play(Sfx.OneUp);
                Rumble(0.4f, 0.4f, 0.25f);
                hud.PopBattery();
            }
            else
            {
                // Already carrying the most spares: bonus points instead!
                bonusPoints += PointsWhenBatteriesFull;
                popups.Add($"FULL! +{PointsWhenBatteriesFull}", aboveBoltE, new Color(1f, 0.85f, 0.25f), 36);
                sounds.Play(Sfx.OneUp, 1.25f);
            }
        }
        hud.SetBatteries(spareBatteries, boltsTowardBattery / (float)BoltsPerBattery);
    }

    /// <summary>Crashed with a spare battery: KABOOM (like a game over), then the magic rebuild starts.</summary>
    void StartRebuild()
    {
        state = GameState.Rebuilding;
        rebuildTimer = 0;
        rebuildStarted = batteryFlown = false;
        spareBatteries--;
        batteriesUsed++;
        speedAtCrash = speed;
        crashedInBossFight = BossFightActive;
        StopPowersNow(); // any power ends right away, quietly (see Main.PowerUps.cs)

        // The same big KABOOM as a game over... but the music only wobbles down
        robot.CanMove = false;
        robot.MoveInput = 0;
        robot.Crash();
        shake = 0.5f;
        Rumble(0.6f, 1f, 0.6f);
        sounds.Play(Sfx.Explosion);
        sounds.DipMusic();

        boss?.Taunt(RebuildDelay + RebuildSeconds + 1.2f); // HA HA HA! (and he waits until Bolt-E is back)
        hud.SetBatteries(spareBatteries, boltsTowardBattery / (float)BoltsPerBattery);

        // Slow motion for a moment, so you can see the boom! (The timer ignores the slow motion: 0.35 real seconds.)
        Engine.TimeScale = 0.3;
        GetTree().CreateTimer(0.35, ignoreTimeScale: true).Timeout += () =>
        {
            if (state == GameState.Rebuilding) Engine.TimeScale = 1;
        };
    }

    /// <summary>Every frame while Bolt-E is in pieces.</summary>
    void UpdateRebuild(float dt)
    {
        // The world skids to a stop. (It still slides along, so the meters stay right.)
        speed = Mathf.MoveToward(speed, 0, 900f * dt);
        distance += speed * dt;
        rebuildTimer += dt;
        hud.SetScore(Score, boltsCollected, bestScore);

        // A spare battery zooms from the corner to Bolt-E's chest, arriving just as the rebuild starts...
        if (!batteryFlown && rebuildTimer >= RebuildDelay - Hud.BatteryFlightSeconds)
        {
            batteryFlown = true;
            hud.FlyBatteryTo(new Vector2(robot.Position.X, GroundY - 75)); // he's always rebuilt standing on the ground
        }

        // ...and every piece flies back into place! (When they're all back, robot.Rebuilt calls FinishRebuild.)
        if (rebuildTimer >= RebuildDelay && !rebuildStarted)
        {
            rebuildStarted = true;
            robot.Rebuild(RebuildSeconds);
            sounds.Play(Sfx.Rebuild);
        }
    }

    /// <summary>Bolt-E is back together! He rides again, blinking (and safe) for a moment.</summary>
    void FinishRebuild()
    {
        state = GameState.Playing;
        Engine.TimeScale = 1;
        robot.BlinkTime = SafeBlinkSeconds;

        // Poof! Dangerous things close by disappear, and so does everything Big Rusty fired.
        bool Near(Node2D thing) => Mathf.Abs(thing.Position.X - robot.Position.X) < ClearAround;
        PuffAway(obstacles, Near);
        PuffAway(enemies, Near);
        PuffAway(blasts, _ => true);
        PuffAway(shockwaves, _ => true);
        PuffAway(lasers, _ => true);
        spawnCountdown = Mathf.Max(spawnCountdown, 600); // a little breather before new obstacles

        // Speed back up to 85% of the old speed. (In a boss fight, the speed for after the fight stays the same.)
        if (!crashedInBossFight)
            speedToGetBackTo = Mathf.Max(StartSpeed, SpeedAfterRebuild * Mathf.Max(speedAtCrash, speedToGetBackTo));

        sounds.RestoreMusic();
        hud.ShowBanner("BOLT-E IS BACK!", new Color(0.45f, 1f, 0.5f));
        Rumble(0.3f, 0.2f, 0.2f);
    }

    /// <summary>
    /// Poof! Removes the things in a list that "which" picks, each in a puff of white smoke.
    /// (The &lt;T&gt; means it works with any list of things: obstacles, enemies, fireballs, lasers...)
    /// </summary>
    void PuffAway<T>(List<T> things, Func<T, bool> which) where T : Node2D
    {
        for (int i = things.Count - 1; i >= 0; i--)
        {
            if (!which(things[i])) continue;
            Burst(things[i].Position + new Vector2(0, -30), 10, Colors.White, 200f);
            things[i].QueueFree();
            things.RemoveAt(i);
        }
    }
}
