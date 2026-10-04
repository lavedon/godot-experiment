using Godot;

/// <summary>
/// Rainbow ? boxes and their powers! (This is more of Main: "partial" means one class spread over several files.)
///
/// Sometimes a rainbow ? box floats over the road. Jump into it and the slot machine over Bolt-E's head
/// (PowerRoulette.cs) spins and lands on a power:
///   MEGA BOLT-E:  he grows giant and SMASHES crates, cones and drones, and flattens bad robots.
///   BOLT MAGNET:  every bolt nearby zooms in, and extra bolts sprinkle in.
///   ROCKET BOARD: he zooms up over everything, then floats down on a little parachute.
/// The numbers for the powers (how long they last, how big, how high...) are in PowerUps.cs.
/// </summary>
public partial class Main
{
    static readonly Color PowerGold = new(1f, 0.85f, 0.2f);

    readonly List<PowerBox> boxes = new();
    PowerRoulette roulette = null!;  // the slot machine over Bolt-E's head
    PowerUp? power;                  // the power that's on right now (null = none)
    PowerUp? lastPower;              // the power the last box gave (the next box never gives the same one)
    float powerLeft;                 // seconds left of the power
    float boost = 1f;                // the world rushes by this much faster (1.5 on the rocket board)
    float magnetBoltTimer;           // seconds until the magnet sprinkles in another bolt
    float rocketBoltTimer;           // seconds until the next bolt in the line of bolts in the sky
    float warnTickTimer;             // seconds until the next "tick" when the power is almost over
    float nextBoxAt;                 // meters: a box can come once we've gone this far
    bool landingFromRocket;          // floating down on the parachute: the landing spot gets cleared

    /// <summary>
    /// True while nothing can reach Bolt-E: up on the rocket board, floating down on the parachute,
    /// and in the moment he lands, until the landing spot has been cleared.
    /// </summary>
    bool RocketSafe => robot.Rocketing || robot.Parachuting || landingFromRocket;

    /// <summary>Makes the slot machine (called once, when the game starts).</summary>
    void SetUpPowerUps()
    {
        roulette = new PowerRoulette();
        roulette.Ticked += flick => sounds.Play(Sfx.Tick, 1f + 0.06f * flick); // each tick a little higher
        roulette.Picked += StartPowerUp;
        AddChild(roulette);
    }

    // ---------- The boxes ----------

    /// <summary>
    /// Called when a new obstacle comes. If it's time for a box (and there's lots of room after this obstacle),
    /// a box floats in the middle of the gap, high enough that you have to jump for it.
    /// With the normal numbers the first box can come from 100 m, and Big Rusty comes at 400 m,
    /// so boxes can show up from 100 m to 250 m (no boxes in the last 150 m before him).
    /// </summary>
    void TrySpawnBox(float x, float gap, float minGap, ObstacleKind kind)
    {
        bool timeForABox = Meters >= nextBoxAt && power is null && !roulette.Spinning && boxes.Count == 0;
        bool farFromBigRusty = nextBossAt - Meters > PowerUps.NoBoxesNearBoss; // no boxes near a boss fight
        bool lotsOfRoom = kind != ObstacleKind.Drone && gap > minGap + 150;  // (not after a drone: you ride under those!)
        if (!timeForABox || !farFromBigRusty || !lotsOfRoom) return;

        AddBox(x + gap / 2);
        spawnCountdown += 350; // a little more room after the box
        nextBoxAt = Meters + Rand(PowerUps.BoxEveryMin, PowerUps.BoxEveryMax);
    }

    /// <summary>Puts a ? box over the road at x (200 pixels up: any jump reaches it, but riding along never does).</summary>
    PowerBox AddBox(float x)
    {
        var box = new PowerBox { Position = new Vector2(x, GroundY - 200) };
        AddChild(box);
        boxes.Add(box);
        return box;
    }

    /// <summary>Bolt-E jumped into a box! A rainbow of sparkles, and the slot machine starts spinning.</summary>
    void OpenBox(PowerBox box)
    {
        var at = box.Position;
        box.QueueFree();
        boxes.Remove(box);
        for (int i = 0; i < 3; i++)
            Burst(at, 12, Color.FromHsv(i / 3f, 0.75f, 1f), 260f); // red, green and blue sparkles
        sounds.Play(Sfx.Bolt, 0.7f);
        roulette.Start(PickBoxPower());
        spawnCountdown += 400; // a little room while the slot machine spins
    }

    /// <summary>Picks a power for a box: any of the three, but never the same one as last time.</summary>
    PowerUp PickBoxPower()
    {
        var choices = new List<PowerUp> { PowerUp.Mega, PowerUp.Magnet, PowerUp.Rocket };
        if (lastPower is PowerUp last) choices.Remove(last);
        return choices[GD.RandRange(0, choices.Count - 1)];
    }

    // ---------- Starting and ending a power ----------

    /// <summary>The slot machine landed on a power: start it! (Not if Bolt-E crashed, or Big Rusty is coming.)</summary>
    void StartPowerUp(PowerUp newPower)
    {
        if (state != GameState.Playing || bossWarningShown) return;
        if (power is not null) EndPowerUp(quiet: true); // (only one power at a time)

        power = newPower;
        lastPower = newPower;
        powerLeft = PowerUps.Seconds(newPower);
        warnTickTimer = 0;
        magnetBoltTimer = 0;
        rocketBoltTimer = 0;
        hud.ShowBanner(PowerUps.Name(newPower), PowerGold);
        hud.ShowPowerBar(newPower, 1);
        sounds.Play(Sfx.PowerUp);
        Rumble(0.4f, 0.4f, 0.2f);

        switch (newPower)
        {
            case PowerUp.Mega:
                robot.Grow(PowerUps.MegaSize);
                sounds.Play(Sfx.Grow);
                sounds.SetMusicSpeed(1.12f * CurrentWorld.MusicSpeed); // the music speeds up! (each world has its own speed)
                break;
            case PowerUp.Magnet:
                robot.ShowMagnet = true;
                robot.ShowFace(RobotFace.Hearts, PowerUps.MagnetSeconds);
                break;
            case PowerUp.Rocket:
                robot.StartRocket(GroundY - PowerUps.RocketHeight);
                sounds.Play(Sfx.Rocket);
                break;
        }
    }

    /// <summary>
    /// The power is over. Normally (time's up, or the boss warning): MEGA Bolt-E shrinks with a "pfffft",
    /// the magnet goes away, or the rocket's parachute pops open, and Bolt-E blinks (safe) for a moment.
    /// quiet = true (a crash, game over, a new game): everything just stops, with no sounds, words or blinking.
    /// </summary>
    void EndPowerUp(bool quiet = false)
    {
        roulette.Stop(); // (a slot machine that's still spinning stops too, so it can never start a power later)
        if (power is not PowerUp ending) return;

        switch (ending)
        {
            case PowerUp.Mega:
                if (quiet) robot.SetSizeNow(1);
                else
                {
                    robot.Shrink();
                    sounds.Play(Sfx.Shrink);
                }
                break;
            case PowerUp.Magnet:
                robot.ShowMagnet = false;
                robot.ShowFace(RobotFace.Normal, 0);
                break;
            case PowerUp.Rocket:
                if (quiet)
                {
                    robot.StopRocketNow();
                    landingFromRocket = false;
                }
                else
                {
                    robot.StartParachute(); // pop!
                    landingFromRocket = true;
                    sounds.Play(Sfx.Jump, 1.4f);
                }
                break;
        }
        if (!quiet) robot.BlinkTime = Mathf.Max(robot.BlinkTime, 1); // a moment to get used to normal again

        sounds.SetMusicSpeed(CurrentWorld.MusicSpeed); // back to this world's normal music speed
        robot.PowerWarning = false;
        power = null;
        hud.HidePowerBar();
    }

    /// <summary>A crash, game over, or a fresh start: every power stops right away, quietly.</summary>
    void StopPowersNow()
    {
        EndPowerUp(quiet: true);
        landingFromRocket = false;
        boost = 1;
    }

    // ---------- Every frame ----------

    /// <summary>Every frame while playing: the slot machine follows Bolt-E, and the power counts down.</summary>
    void UpdatePowerUps(float dt)
    {
        roulette.Position = robot.Position + new Vector2(0, -175 * robot.Size); // over his head
        if (power is not PowerUp p) return;

        powerLeft -= dt;
        if (powerLeft <= 0 || bossWarningShown)
        {
            EndPowerUp(); // time's up (or Big Rusty is coming)!
            return;
        }
        hud.ShowPowerBar(p, powerLeft / PowerUps.Seconds(p));

        // Almost over: Bolt-E blinks, and the clock goes tick... tick... tick
        robot.PowerWarning = powerLeft < PowerUps.WarningSeconds;
        if (robot.PowerWarning)
        {
            warnTickTimer -= dt;
            if (warnTickTimer <= 0)
            {
                sounds.Play(Sfx.Tick, 1.4f);
                warnTickTimer = 0.5f; // every half a second
            }
        }

        if (p == PowerUp.Magnet)
        {
            // Extra bolts sprinkle in from the right
            magnetBoltTimer -= dt;
            if (magnetBoltTimer <= 0)
            {
                magnetBoltTimer += 0.45f;
                AddBolt(ScreenWidth + 40, Rand(GroundY - 260, GroundY - 40));
            }
        }
        else if (p == PowerUp.Rocket)
        {
            // A line of bolts waits up in the sky, right where the rocket board flies
            rocketBoltTimer -= dt;
            if (rocketBoltTimer <= 0)
            {
                rocketBoltTimer += 0.25f;
                AddBolt(ScreenWidth + 40, GroundY - PowerUps.RocketHeight - 60);
            }
        }
    }

    /// <summary>BOLT MAGNET: a bolt close enough zooms over to Bolt-E (called for every bolt, every frame).</summary>
    void PullTowardMagnet(BoltPickup bolt, float dt)
    {
        var target = robot.Position + new Vector2(0, -60 * robot.Size);
        if (bolt.Position.DistanceTo(target) < PowerUps.MagnetReach)
            bolt.Position = bolt.Position.MoveToward(target, (700 + speed) * dt);
    }

    /// <summary>
    /// Bolt-E just landed. After the parachute, anything dangerous close by puffs away (and there's a little breather).
    /// MEGA Bolt-E landing shakes the whole screen: KA-THOOM!
    /// </summary>
    void LandedWithPowers()
    {
        if (landingFromRocket)
        {
            landingFromRocket = false;
            bool Near(Node2D thing) => Mathf.Abs(thing.Position.X - robot.Position.X) < 300;
            PuffAway(obstacles, Near);
            PuffAway(enemies, Near);
            spawnCountdown = Mathf.Max(spawnCountdown, 500);
        }

        if (robot.Size > 1.05f)
        {
            shake = 0.2f;
            Rumble(0.4f, 0.6f, 0.15f);
            sounds.Play(Sfx.Slam, 1.7f);
            Burst(robot.Position, 20, new Color(0.9f, 0.85f, 0.75f), 260f);
        }
    }

    // ---------- MEGA BOLT-E smashing things ----------

    /// <summary>SMASH! A crate, cone or drone bursts into bouncing pieces, and 2 bolts pop out.</summary>
    void SmashObstacle(Obstacle obstacle)
    {
        obstacle.Smash();
        bonusPoints += 25;
        popups.Add("SMASH! +25", new Vector2(obstacle.Position.X, GroundY - 130 * robot.Size - 60), PowerGold, 34); // (above MEGA Bolt-E's head)
        sounds.Play(Sfx.Smash);
        shake = 0.15f;
        Rumble(0.5f, 0.5f, 0.15f);
        AddBolt(obstacle.Position.X + 40, GroundY - 120);
        AddBolt(obstacle.Position.X + 90, GroundY - 80);
    }

    /// <summary>SPLAT! MEGA Bolt-E flattens a bad robot, even from the side.</summary>
    void SplatEnemy(Enemy enemy)
    {
        enemy.Stomp();
        stomps++;
        bonusPoints += 25;
        popups.Add("SPLAT! +25", new Vector2(enemy.Position.X, enemy.Top - 10), PowerGold, 34);
        sounds.Play(Sfx.Stomp);
        Burst(new Vector2(enemy.Position.X, enemy.Top + 10), 12, Colors.White, 200f);
    }
}
