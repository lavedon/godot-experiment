using Godot;

/// <summary>
/// VROOMBA, the second boss! (This is more of Main: "partial" means one class spread over several files.)
///
/// The bosses TAKE TURNS: game 1 starts with BIG RUSTY, game 2 with VROOMBA, game 3 with Big Rusty again... and after
/// that every boss fight is the other boss. So the kid meets VROOMBA in every other game, even if he only ever reaches
/// the first fight. (VROOMBA himself, his moves and his drawing, is in Vroomba.cs.)
/// Here: picking the boss, what happens when VROOMBA does something (sounds, dust, dust bunnies...), his vacuum pulling
/// Bolt-E and gobbling up the loose bolts, and the bolts flying back out when he's stomped.
/// </summary>
public partial class Main
{
    // ---- VROOMBA (try changing these!) ----
    const float SuckBoltSpeed = 650f;        // how fast loose bolts fly into his mouth (pixels per second)
    const float DustBunnyChase = 110f;       // how fast dust bunnies hop after Bolt-E (Big Rusty's Pup-Bots go 150)
    static readonly Color DustColor = new(0.78f, 0.75f, 0.7f);

    int gameNumber;        // games started since the game was opened: game 1, game 2...
    int bossFightNumber;   // boss fights in this game so far: fight 1, fight 2...
    bool testTakeTurns;    // (the self-test) true = SpawnBoss() follows the taking-turns rule during the self-test too

    /// <summary>
    /// The bosses take turns! Which boss comes in this game's fight number "fightNumber" (1 = the first fight)?
    /// Game 1: BIG RUSTY, VROOMBA, BIG RUSTY...  Game 2: VROOMBA, BIG RUSTY, VROOMBA...  Game 3 is like game 1, and so on.
    /// </summary>
    static BossKind BossForFight(int gameNumber, int fightNumber) =>
        (gameNumber + fightNumber) % 2 == 0 ? BossKind.BigRusty : BossKind.Vroomba;

    /// <summary>
    /// The boss for the next fight, by the taking-turns rule. (During the self-test it's always Big Rusty, so every old
    /// test still meets the boss it expects. The VROOMBA tests ask for him: SpawnBoss(BossKind.Vroomba).)
    /// </summary>
    BossKind NextBossKind() => selfTesting && !testTakeTurns ? BossKind.BigRusty : BossForFight(gameNumber, bossFightNumber);

    /// <summary>VROOMBA rolls in! (Everything he does that the game needs to hear about is hooked up here.)</summary>
    Vroomba MakeVroomba()
    {
        var vroomba = new Vroomba { GroundY = GroundY, ZIndex = 5 };

        vroomba.Laughed += () =>
        {
            popups.Add("BEEP BOOP!", vroomba.Position + new Vector2(0, -140), Vroomba.VroombaBlue, 36);
            sounds.Play(Sfx.BeepBoop);
        };
        vroomba.RevvedUp += () => sounds.Play(Sfx.RevUp);
        vroomba.Zoomed += () => sounds.Play(Sfx.Zoom);
        vroomba.BonkedWall += at =>
        {
            sounds.Play(Sfx.WallBonk);
            shake = 0.4f;
            Rumble(0.6f, 0.8f, 0.3f);
            Burst(at, 18, DustColor, 260f);
        };
        vroomba.VacuumOn += () => sounds.Play(Sfx.VacuumHum);
        vroomba.VacuumOff += () => sounds.Stop(Sfx.VacuumHum);
        vroomba.SpatBunny += (at, direction) =>
        {
            AddDustBunny(at, direction);
            sounds.Play(Sfx.Spit);
        };
        vroomba.BoltsOut += FountainBolts;
        return vroomba;
    }

    /// <summary>
    /// Every frame of a VROOMBA fight: while his vacuum is on, Bolt-E is pulled toward him (riding away is faster than
    /// the pull), and every loose bolt in the arena flies into his mouth. YUM!
    /// </summary>
    void UpdateVroomba(float dt)
    {
        if (boss is not Vroomba vroomba || !vroomba.Sucking) return;

        float toHim = vroomba.Position.X - robot.Position.X;
        if (Mathf.Abs(toHim) > 10)
        {
            float pull = Vroomba.SuckPull * Robot.RideSpeed * dt;
            robot.Position = new Vector2(Mathf.Clamp(robot.Position.X + Mathf.Sign(toHim) * pull, Robot.LeftEdge, Robot.RightEdge),
                                         robot.Position.Y);
        }

        var mouth = vroomba.Mouth;
        float step = SuckBoltSpeed * dt;
        for (int i = bolts.Count - 1; i >= 0; i--)
        {
            var bolt = bolts[i];
            if (bolt.Position.X < 0 || bolt.Position.X > ScreenWidth) continue; // (only the bolts in the arena)
            bolt.Velocity = Vector2.Zero; // (a bolt that was still flying out gets sucked back in)
            var toMouth = mouth - bolt.Position;
            if (toMouth.Length() > step + 12)
            {
                bolt.Position += toMouth.Normalized() * step;
                continue;
            }

            // Gulp!
            bolt.QueueFree();
            bolts.RemoveAt(i);
            vroomba.Swallow();
            sounds.Play(Sfx.Bolt, 0.7f);
            if (!popups.JustAdded("YUM!", 0.4f)) popups.Add("YUM!", mouth + new Vector2(0, -70), Vroomba.VroombaBlue, 30);
        }
    }

    /// <summary>Bolts he swallowed fly back out of him like a fountain, and land in the arena: grab them!</summary>
    void FountainBolts(Vector2 from, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var bolt = new BoltPickup
            {
                Position = from,
                Velocity = new Vector2(Rand(-260f, 260f), Rand(-820f, -560f)),
                LandY = GroundY - 40,
            };
            AddChild(bolt);
            bolts.Add(bolt);
        }
        sounds.Play(Sfx.Bolt, 1.5f);
    }

    /// <summary>A fluffy dust bunny flies out of VROOMBA's mouth, lands, and hops after Bolt-E. Stomp it!</summary>
    void AddDustBunny(Vector2 at, float direction)
    {
        var bunny = new Enemy
        {
            Kind = EnemyKind.DustBunny,
            Position = at,
            GroundY = GroundY,
            Chaser = true,
            ChaseSpeed = DustBunnyChase,
            Thrown = new Vector2(direction * 240, -380),
        };
        AddChild(bunny);
        enemies.Add(bunny);
    }
}
