using Godot;

/// <summary>
/// DRONEZILLA, the third boss! (This is more of Main: "partial" means one class spread over several files.)
/// The bosses take turns: BIG RUSTY, VROOMBA, DRONEZILLA (see BossForFight in Main.Vroomba.cs).
/// (DRONEZILLA himself, his moves and his drawing, is in Dronezilla.cs. His bombs are DroneBomb.cs and his beam is
/// ZapBeam.cs.) Here: what happens when DRONEZILLA does something (sounds, shakes, dust...), his bombs, his zap
/// beam and his mini-drones, the buzz of his rotors, and checking if any of them bumped into Bolt-E.
/// </summary>
public partial class Main
{
    // ---- DRONEZILLA (try changing these!) ----
    const float MiniDroneChase = 80f;  // how fast mini-drones drift after Bolt-E (VROOMBA's dust bunnies hop at 110)
    static readonly Color BoomOrange = new(1f, 0.6f, 0.2f);
    static readonly Color LowBatteryRed = new(1f, 0.3f, 0.25f);
    /// <summary>The tip when he flies in (the arrows are the same as in the other bosses' tips).</summary>
    const string DronezillaHint = "Dodge the bombs! STOMP him when he lands!  ← →  / stick to ride";

    readonly List<DroneBomb> bombs = new();     // his bombs (falling, or going BOOM)
    readonly List<ZapBeam> zapBeams = new();    // his zap beam (aiming, locked, or firing)
    bool rotorBuzzOn;                           // true while his rotor buzz is playing

    /// <summary>DRONEZILLA flies in! (Everything he does that the game needs to hear about is hooked up here.)</summary>
    Dronezilla MakeDronezilla()
    {
        var drone = new Dronezilla { GroundY = GroundY, ZIndex = 5 };

        drone.Laughed += () =>
        {
            popups.Add("BZZT BZZT!", drone.Position + new Vector2(0, -170), Dronezilla.DroneYellow, 36);
            sounds.Play(Sfx.Bzzt);
        };
        drone.TookOff += () => sounds.Play(Sfx.TakeOff);
        drone.BombDropped += AddBomb;
        drone.SwoopWarned += () => sounds.Play(Sfx.BossAlarm, 1.3f);
        drone.Swooped += () => sounds.Play(Sfx.Zoom, 0.8f);
        drone.ZapAimed += () => AddZapBeam(drone);
        drone.MiniDropped += at =>
        {
            AddMiniDrone(at);
            sounds.Play(Sfx.Spit, 1.5f);
        };
        drone.BatteryLow += () =>
        {
            popups.Add("LOW BATTERY!", drone.Position + new Vector2(0, -170), LowBatteryRed, 32);
            sounds.Play(Sfx.LowBattery);
        };
        drone.TouchedDown += at =>
        {
            // Bump! Rotors spin down, a puff of dust and a few little sparks
            sounds.Play(Sfx.PowerDown);
            sounds.Play(Sfx.Land, 0.6f);
            shake = Mathf.Max(shake, 0.15f);
            Rumble(0.3f, 0.3f, 0.15f);
            Burst(at, 14, DustColor, 200f);
            Burst(at + new Vector2(0, -40), 8, new Color(1f, 0.9f, 0.4f), 260f);
        };
        drone.CrashedDown += at =>
        {
            sounds.Play(Sfx.Slam);
            shake = 0.4f;
            Rumble(0.5f, 0.6f, 0.3f);
            Burst(at, 22, DustColor, 300f);
        };
        return drone;
    }

    /// <summary>A bomb falls from DRONEZILLA's belly, with a whistle. (A red ring shows where it will land.)</summary>
    void AddBomb(Vector2 at)
    {
        var bomb = new DroneBomb { Position = at, GroundY = GroundY, ZIndex = 4 };
        bomb.Boomed += where =>
        {
            sounds.Play(Sfx.Explosion, 1.6f); // a smaller, higher BOOM
            shake = Mathf.Max(shake, 0.15f);
            Rumble(0.3f, 0.3f, 0.15f);
            Burst(where + new Vector2(0, -10), 14, BoomOrange, 280f);
        };
        AddChild(bomb);
        bombs.Add(bomb);
        sounds.Play(Sfx.BombWhistle);
    }

    /// <summary>His zap beam starts aiming: a red circle on the ground, under him.</summary>
    void AddZapBeam(Dronezilla drone)
    {
        var beam = new ZapBeam
        {
            GroundY = GroundY,
            Position = new Vector2(drone.Position.X, GroundY),
            TopY = drone.CameraWorldSpot.Y,
            ZIndex = 4,
        };
        beam.Locked += () => sounds.Play(Sfx.Tick, 0.7f);
        beam.StartedFiring += () =>
        {
            sounds.Play(Sfx.LaserBeam);
            shake = Mathf.Max(shake, 0.2f);
            Rumble(0.4f, 0.3f, 0.4f);
        };
        AddChild(beam);
        zapBeams.Add(beam);
        sounds.Play(Sfx.LaserCharge);
    }

    /// <summary>A little mini-drone drops out of DRONEZILLA, hovers at head height and drifts after Bolt-E. Stomp it!</summary>
    void AddMiniDrone(Vector2 at)
    {
        var mini = new Enemy
        {
            Kind = EnemyKind.MiniDrone,
            Position = at,
            GroundY = GroundY,
            Chaser = true,
            ChaseSpeed = MiniDroneChase,
        };
        AddChild(mini);
        enemies.Add(mini);
    }

    /// <summary>
    /// Every frame (in every game state): old bombs and beams are tidied away, the beam's circle follows DRONEZILLA,
    /// everything he made puffs away when he's beaten, and his rotors buzz while he flies (and never after the fight).
    /// </summary>
    void UpdateDronezilla(float dt)
    {
        for (int i = bombs.Count - 1; i >= 0; i--)
        {
            if (!bombs[i].Finished) continue;
            bombs[i].QueueFree();
            bombs.RemoveAt(i);
        }
        for (int i = zapBeams.Count - 1; i >= 0; i--)
        {
            if (!zapBeams[i].Finished) continue;
            zapBeams[i].QueueFree();
            zapBeams.RemoveAt(i);
        }

        var drone = boss as Dronezilla;
        if (drone is not null)
        {
            foreach (var beam in zapBeams) beam.Follow(drone.Position.X, drone.CameraWorldSpot.Y);
            // He stopped aiming (a BZZT BZZT loop, or he flew away): a beam that isn't firing yet never fires
            if (!drone.Zapping) PuffAway(zapBeams, beam => !beam.Firing);
            // Beaten: every bomb, beam and mini-drone puffs away
            if (drone.IsBeaten) ClearDroneDangers();
        }

        bool buzz = drone is { BuzzOn: true } && state is GameState.Playing or GameState.Rebuilding;
        if (buzz != rotorBuzzOn)
        {
            rotorBuzzOn = buzz;
            if (buzz) sounds.Play(Sfx.RotorBuzz);
            else sounds.Stop(Sfx.RotorBuzz);
        }
    }

    /// <summary>Poof! Every bomb, zap beam and mini-drone puffs away (after a rebuild, and when DRONEZILLA is beaten).</summary>
    void ClearDroneDangers()
    {
        PuffAway(bombs, _ => true);
        PuffAway(zapBeams, _ => true);
        PuffAway(enemies, enemy => enemy.Kind == EnemyKind.MiniDrone);
    }

    /// <summary>A fresh start (see ClearWorld): no bombs, no beams, and no rotor buzz.</summary>
    void ClearDroneThings()
    {
        foreach (var bomb in bombs) bomb.QueueFree();
        foreach (var beam in zapBeams) beam.QueueFree();
        bombs.Clear();
        zapBeams.Clear();
        rotorBuzzOn = false;
        sounds.Stop(Sfx.RotorBuzz);
    }

    /// <summary>Did a bomb (falling or going BOOM) or a firing zap beam bump into Bolt-E? (True if he really crashed.)</summary>
    bool CheckDroneBumps(Rect2 robotBox)
    {
        foreach (var bomb in bombs)
        {
            if (bomb.Dangerous && robotBox.Intersects(bomb.Hitbox) && Bonk(bomb)) return true;
        }
        foreach (var beam in zapBeams)
        {
            if (beam.Firing && robotBox.Intersects(beam.Hitbox) && Bonk(beam)) return true;
        }
        return false;
    }
}
