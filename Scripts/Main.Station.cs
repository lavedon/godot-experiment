using Godot;

/// <summary>
/// UPSIDE-DOWN STATION, the 6th world! (This is more of Main: "partial" means one class spread over several files.)
///
/// A neon space-station corridor with a FLOOR and a CEILING. Here the jump button doesn't jump: it FLIPS GRAVITY.
/// Bolt-E falls up to the ceiling and rides along it upside-down, and the next press flips him back down. He can flip
/// once more in the air, so a flip that was too early can be fixed.
/// Spike strips, laser gates and gaps come on the floor or on the ceiling, and space junk floats in the middle.
/// StationPlanner (in StationThings.cs) puts them in FAIR patterns, and this file puts them on the track.
/// When a boss is coming, gravity turns back to normal ("GRAVITY ON!") and the ceiling slides open, so every boss
/// fights exactly like everywhere else.
/// The flipping itself is in RobotFlip.cs, the dangers in StationThings.cs, and the drawing in StationScenery.cs.
/// </summary>
public partial class Main
{
    // ---- UPSIDE-DOWN STATION (try changing these!) ----
    const float CeilingSurfaceY = 170f;      // the ceiling's surface: the corridor is GroundY - 170 = 430 pixels tall
    const float CeilingSlideSeconds = 0.9f;  // how long the ceiling takes to slide open or shut
    const float SpaceFallSeconds = 0.45f;    // how long Bolt-E falls through a gap before the KABOOM
    const float GapEdgeForgiveness = 18f;    // riding this close to the edge of a gap is still safe (pixels)
    const float CeilingBackSeconds = NewWorldDelay + 1.5f; // a boss fight that ends without a new world: the ceiling comes back
    const string FlipTip = "Press JUMP to FLIP gravity!";

    static readonly Color NeonCyan = new(0.35f, 0.95f, 1f);
    static readonly Color NeonMagenta = new(1f, 0.35f, 0.85f);

    /// <summary>The middle of the corridor, halfway between the floor and the ceiling.</summary>
    const float CorridorMiddle = (CeilingSurfaceY + GroundY) / 2;

    readonly List<StationGap> stationGaps = new();     // the gaps in the floor and the ceiling
    readonly StationPlanner planner = new();           // puts the dangers in fair patterns (see StationThings.cs)
    readonly List<StationDanger> planned = new();      // the dangers of the pattern being made (reused every time)
    Random stationRandom = new(1);                     // the planner's own dice (so the other worlds' dice never change)
    float ceilingOpen = 1;     // 0 = the ceiling is shut (flipping is on), 1 = it slid open, up off the screen
    float stationSeconds;      // seconds of flipping around in the station (the first ones are easy)
    float noBossSeconds;       // seconds since the last boss (or boss warning) was gone
    float spaceFallTimer;      // seconds left of falling through a gap
    StationGap? fallingThrough; // the gap Bolt-E is falling through (null = none)

    /// <summary>True in UPSIDE-DOWN STATION (also while its colors are still coming in).</summary>
    bool InStation => toWorld.Scenery == Scenery.Station;

    /// <summary>Where the ceiling's surface is right now: it slides up off the screen when it opens.</summary>
    float CeilingY => CeilingSurfaceY - ceilingOpen * (CeilingSurfaceY + 90);

    /// <summary>Turns a "track" position (how far along the whole ride, see StationPlanner) into a spot on the screen.</summary>
    float ScreenX(float track) => track - distance + RobotX;

    /// <summary>A spot over Bolt-E's head, "how far" pixels away (under it when he's upside-down on the ceiling).</summary>
    Vector2 AboveBoltE(float howFar) => robot.Position + new Vector2(0, robot.UpsideDown ? howFar : -howFar);

    // ---------- Arriving and leaving ----------

    /// <summary>A new world. Into the station: the ceiling slides shut and the tip shows. Out of it: its dangers puff away.</summary>
    void StationWorldChanged()
    {
        if (InStation) ArriveInStation(shutNow: false);
        else if (fromWorld.Scenery == Scenery.Station) PuffAwayStationThings();
    }

    /// <summary>
    /// Welcome to UPSIDE-DOWN STATION! The first patterns are easy, and the tip says how to flip.
    /// (shutNow = the ceiling is shut right away: a game that starts here.)
    /// </summary>
    void ArriveInStation(bool shutNow)
    {
        stationSeconds = 0;
        noBossSeconds = CeilingBackSeconds;
        planner.Reset(distance);
        stationRandom = new Random(unchecked(runNumber * 7919 + (int)distance)); // (the same every time, for the self-test)
        if (shutNow) ceilingOpen = 0;
        spawnCountdown = Mathf.Max(spawnCountdown, 700);
        hud.ShowHint(FlipTip);
    }

    /// <summary>A fresh start (a new game or the title screen): no station things, the ceiling open, normal gravity.</summary>
    void ClearStation()
    {
        foreach (var gap in stationGaps) gap.QueueFree();
        stationGaps.Clear();
        ceilingOpen = 1;
        stationSeconds = 0;
        noBossSeconds = CeilingBackSeconds;
        spaceFallTimer = 0;
        fallingThrough = null;
        robot.SetFlipMode(false);
        robot.CeilingY = Robot.NoCeiling;
        hud.SetGravityArrow(false, 0);
    }

    /// <summary>Puff away every station danger: the gaps, and the spikes, laser gates and space junk.</summary>
    void PuffAwayStationThings()
    {
        PuffAway(stationGaps, _ => true);
        PuffAway(obstacles, o => o.Kind is ObstacleKind.SpikeStrip or ObstacleKind.LaserGate or ObstacleKind.SpaceJunk);
    }

    // ---------- Every frame ----------

    /// <summary>
    /// Every frame, on every screen: the ceiling slides open for a boss (and shut again), flipping goes on and off,
    /// the gravity arrow in the corner turns, and a fall through a gap ends in a KABOOM.
    /// </summary>
    void UpdateStation(float dt)
    {
        bool riding = state is GameState.Playing or GameState.Rebuilding;
        noBossSeconds = boss is null && !bossWarningShown ? noBossSeconds + dt : 0;

        // The ceiling is shut in the station, except when a boss is coming (or was just here, or the WORLD MAP is coming
        // after him: see Main.WorldMap.cs)
        bool ceilingWanted = InStation && state != GameState.Title && noBossSeconds >= CeilingBackSeconds && !newWorldComing;
        float before = ceilingOpen;
        ceilingOpen = Mathf.MoveToward(ceilingOpen, ceilingWanted ? 0 : 1, dt / CeilingSlideSeconds);
        bool startedMoving = (before <= 0 && ceilingOpen > 0) || (before >= 1 && ceilingOpen < 1);
        if (InStation && startedMoving) sounds.Play(Sfx.CeilingSlide, ceilingWanted ? 0.85f : 1f);

        // Flipping is on while the ceiling is shut
        bool flipOn = ceilingWanted && ceilingOpen <= 0 && riding;
        if (robot.FlipMode != flipOn)
        {
            bool wasUpsideDown = robot.UpsideDown;
            robot.SetFlipMode(flipOn);
            if (!flipOn && InStation && (boss is not null || bossWarningShown)) GravityOn(wasUpsideDown);
            if (flipOn) planner.Reset(distance); // (flipping starts again with Bolt-E on the floor)
        }
        robot.CeilingY = InStation && ceilingOpen < 0.999f ? CeilingY : Robot.NoCeiling;
        if (state == GameState.Playing && robot.FlipMode) stationSeconds += dt;

        hud.SetGravityArrow(robot.FlipMode, robot.RollAngle);
        UpdateSpaceFall(dt);
    }

    /// <summary>
    /// A boss is coming: GRAVITY ON! Gravity points down again (Bolt-E drops to the floor if he was on the ceiling),
    /// the ceiling slides open, and the station's dangers puff away, so the fight is just like everywhere else.
    /// </summary>
    void GravityOn(bool wasUpsideDown)
    {
        popups.Add("GRAVITY ON!", new Vector2(ScreenWidth / 2, CorridorMiddle + 40), NeonCyan, 46);
        if (wasUpsideDown) sounds.Play(Sfx.FlipDown);
        PuffAwayStationThings();
        Rumble(0.3f, 0.3f, 0.2f);
    }

    // ---------- Flipping ----------

    /// <summary>
    /// The jump button in the station: FLIP! A whoosh that sweeps up (or down), sparkles from the floor or ceiling he
    /// leaves (or a ring of sparkles for a flip in the air), and a tiny buzz.
    /// </summary>
    void FlipPressed()
    {
        bool fromSurface = robot.OnGround;
        float leftSurface = robot.SurfaceY; // (the floor or ceiling he's leaving)
        if (!robot.SwapGravity()) return;

        bool up = robot.UpsideDown;
        sounds.Play(up ? Sfx.FlipUp : Sfx.FlipDown, robot.LastFlipInAir ? 1.2f : 1f);
        Rumble(0.25f, 0f, 0.06f);
        if (fromSurface)
            Sparkles(new Vector2(robot.Position.X, leftSurface), up ? -1 : 1, up ? NeonCyan : NeonMagenta, 16);
        else
            Burst(robot.BodyMiddle, 12, Colors.White, 220f); // a flip in the air: a puff of sparkles all around
    }

    /// <summary>Thud! Bolt-E lands on the floor or the ceiling: a little spark, a tiny thud and a little rumble.</summary>
    void StationLanded()
    {
        float into = robot.UpsideDown ? 1 : -1; // (the sparks fly into the corridor, away from the surface)
        Sparkles(new Vector2(robot.Position.X, robot.SurfaceY), into, robot.UpsideDown ? NeonMagenta : NeonCyan, 8);
        sounds.Play(Sfx.FlipThud, robot.UpsideDown ? 1.15f : 1f);
        Rumble(0.3f, 0.25f, 0.08f);
        shake = Mathf.Max(shake, 0.05f);
    }

    /// <summary>Sparkles spraying away from the floor (into = -1: up) or the ceiling (into = 1: down), white and neon.</summary>
    void Sparkles(Vector2 at, float into, Color color, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float life = Rand(0.25f, 0.6f);
            particles.Add(new Particle
            {
                Position = at + new Vector2(Rand(-30, 30), 0),
                Velocity = new Vector2(0, into).Rotated(Rand(-1.1f, 1.1f)) * Rand(120, 420),
                Life = life,
                MaxLife = life,
                Size = Rand(2.5f, 5.5f),
                Color = i % 3 == 0 ? Colors.White : color,
            });
        }
    }

    // ---------- Gaps: falling out into space ----------

    /// <summary>
    /// Is Bolt-E riding onto a gap, with gravity pulling him into it? Then he falls out into space (and the KABOOM comes
    /// a moment later). MEGA BOLT-E is too big to fall in, and nothing happens while he blinks or rides the rocket.
    /// Returns true if he started falling.
    /// </summary>
    bool CheckStationGaps()
    {
        if (stationGaps.Count == 0 || fallingThrough is not null || state != GameState.Playing) return false;
        if (!robot.OnGround || robot.Crashed || robot.Size > 1.05f || RocketSafe || robot.BlinkTime > 0) return false;
        foreach (var gap in stationGaps)
        {
            if (gap.Ceiling != robot.UpsideDown) continue; // (gravity has to pull him toward the gap)
            if (Mathf.Abs(robot.Position.X - gap.Position.X) < gap.Width / 2 - GapEdgeForgiveness)
            {
                StartSpaceFall(gap);
                return true;
            }
        }
        return false;
    }

    /// <summary>WHOOOA! He falls out through the gap, tumbling (see Robot.FallIntoSpace).</summary>
    void StartSpaceFall(StationGap gap)
    {
        fallingThrough = gap;
        spaceFallTimer = SpaceFallSeconds;
        robot.FallIntoSpace();
        sounds.Play(Sfx.SpaceFall);
        popups.Add("WHOOOA!", AboveBoltE(150), Colors.White, 34);
        Rumble(0.2f, 0.1f, 0.3f);
    }

    /// <summary>The fall through a gap: after a moment he's back at its edge, and... KABOOM!</summary>
    void UpdateSpaceFall(float dt)
    {
        if (fallingThrough is not { } gap) return;
        if (state != GameState.Playing || robot.Crashed)
        {
            fallingThrough = null; // (something else already crashed him)
            return;
        }
        spaceFallTimer -= dt;
        if (spaceFallTimer > 0) return;
        fallingThrough = null;
        robot.StopFallingAt(gap.Ceiling ? CeilingY : GroundY);
        Bonk(gap);
    }

    /// <summary>The gaps slide along with the floor and the ceiling.</summary>
    void MoveStationGaps(float move)
    {
        for (int i = stationGaps.Count - 1; i >= 0; i--)
        {
            stationGaps[i].Position -= new Vector2(move, 0);
            if (stationGaps[i].Right < -150)
            {
                stationGaps[i].QueueFree();
                stationGaps.RemoveAt(i);
            }
        }
    }

    // ---------- Putting dangers and bolts on the track ----------

    /// <summary>Time for the next pattern? The planner makes it fair (see StationPlanner), and here it goes on the track.</summary>
    void SpawnStationThings(float dt)
    {
        spawnCountdown -= speed * dt;
        if (spawnCountdown > 0) return;

        // Plan for the speed the world will have (after a rebuild it's still speeding back up)
        float planSpeed = Mathf.Min(MaxSpeed, Mathf.Max(StartSpeed, Mathf.Max(speed, speedToGetBackTo)) + 60);
        float start = distance + ScreenWidth + 100 - RobotX; // (just past the right side of the screen)
        float end = planner.Plan(start, planSpeed, stationSeconds, stationRandom, planned);
        foreach (var danger in planned) AddStationDanger(danger);
        AddStationBolts(planSpeed);
        spawnCountdown = end - start + planSpeed * (0.2f + 0.3f * stationRandom.NextSingle());
    }

    /// <summary>Puts one planned danger on the track: on the floor, hanging from the ceiling, or floating in the middle.</summary>
    void AddStationDanger(StationDanger danger)
    {
        float x = ScreenX(danger.Middle);
        bool ceiling = danger.Side == StationSide.Ceiling;
        float surface = ceiling ? CeilingSurfaceY : GroundY;
        switch (danger.Kind)
        {
            case StationDangerKind.Gap:
                var gap = new StationGap { Ceiling = ceiling, Width = danger.Right - danger.Left, Position = new Vector2(x, surface) };
                AddChild(gap);
                stationGaps.Add(gap);
                break;

            case StationDangerKind.Junk:
                var junk = new Obstacle { Kind = ObstacleKind.SpaceJunk, Position = new Vector2(x, CorridorMiddle), FloatHeight = GroundY - CorridorMiddle };
                AddChild(junk);
                obstacles.Add(junk);
                break;

            default:
                var kind = danger.Kind == StationDangerKind.Spikes ? ObstacleKind.SpikeStrip : ObstacleKind.LaserGate;
                var thing = new Obstacle { Kind = kind, Hanging = ceiling, Position = new Vector2(x, surface), Scale = new Vector2(1, ceiling ? -1 : 1) };
                AddChild(thing);
                obstacles.Add(thing);
                break;
        }
    }

    /// <summary>
    /// Bolts for the pattern that was just planned: a line of bolts along the safe side, and (where he has to flip) an arc
    /// of bolts right where a flip goes, or a rainbow ? box floating in the middle of the corridor.
    /// </summary>
    void AddStationBolts(float planSpeed)
    {
        if (planned.Count == 0) return;
        if (planner.Switched)
        {
            float middle = (planner.SwitchFrom + planner.SwitchTo) / 2;
            if (TimeForStationBox())
            {
                AddBox(ScreenX(middle)).Position = new Vector2(ScreenX(middle), CorridorMiddle); // (a flip goes right through it!)
                nextBoxAt = Meters + Rand(PowerUps.BoxEveryMin, PowerUps.BoxEveryMax);
            }
            else if (stationRandom.NextSingle() < 0.75f)
            {
                AddFlipArc(middle, planSpeed, toCeiling: planner.LastSide == StationSide.Floor);
            }
        }

        // A line of bolts along the safe side (the side without the dangers)
        if (planner.LastPattern != "junk" && stationRandom.NextSingle() < 0.6f)
        {
            float y = planner.LastSide == StationSide.Floor ? CeilingSurfaceY + 40 : GroundY - 40;
            float from = planned[0].Left, to = planned[^1].Right;
            int count = Math.Min(7, 1 + (int)((to - from) / 55));
            for (int i = 0; i < count; i++) AddBolt(ScreenX(from + i * 55), y);
        }
    }

    /// <summary>
    /// 5 bolts along the path of a flip (the middle of Bolt-E's body on his way across), with the flip starting a little
    /// before "middle". Flip there, and you grab them all!
    /// </summary>
    void AddFlipArc(float middle, float planSpeed, bool toCeiling)
    {
        float direction = toCeiling ? -1 : 1;
        float startY = toCeiling ? GroundY - 65 : CeilingSurfaceY + 65;
        float across = GroundY - CeilingSurfaceY - 130; // how far his body travels (the corridor, minus his own height)
        float startX = middle - StationPlanner.FlipSeconds * planSpeed / 2;
        for (int i = 0; i < 5; i++)
        {
            float t = 0.05f + i * 0.07f;
            float travelled = Mathf.Min(across, Robot.FlipKick * t + Robot.FlipGravity * t * t / 2);
            AddBolt(ScreenX(startX + planSpeed * t), startY + direction * travelled);
        }
    }

    /// <summary>Is it time for a rainbow ? box? (The same rules as on the road: see TrySpawnBox.)</summary>
    bool TimeForStationBox() =>
        Meters >= nextBoxAt && power is null && !roulette.Spinning && boxes.Count == 0 && nextBossAt - Meters > PowerUps.NoBoxesNearBoss;

    /// <summary>Where the rocket board flies: high over the road, or right through the middle of the station's corridor.</summary>
    float RocketFeetY => InStation ? CorridorMiddle + 65 : GroundY - PowerUps.RocketHeight;
}
