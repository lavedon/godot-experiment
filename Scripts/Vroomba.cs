using Godot;

/// <summary>VROOMBA's three attacks. Every one of them ends with a ZOOM into the wall (BONK!), so he always gets dizzy.</summary>
public enum VroombaAttack
{
    Zoom,         // he revs up, then dashes across the arena and BONKS into the wall
    Suck,         // his vacuum pulls Bolt-E and every loose bolt toward him (then a zoom)
    DustBunnies,  // he spits out fluffy dust bunnies that hop after Bolt-E (then a zoom)
}

/// <summary>
/// VROOMBA, the giant robot vacuum! The second boss: he takes turns with BIG RUSTY (see Main.Vroomba.cs).
/// He drives on the ground, so Bolt-E can JUMP OVER him. He has 6 health and gets angrier as he gets hurt:
///   Phase 1:            ZOOM! He revs up ("!"), then dashes across the arena and BONKS into the wall.
///                       SUCK! His vacuum pulls Bolt-E (ride away!) and gobbles up every loose bolt: YUM!
///   Phase 2 (ANGRY):    he also spits out fluffy DUST BUNNIES that hop after Bolt-E (stomp them!).
///   Phase 3 (FURIOUS):  TURBO zooms: faster, and every other time he zooms there and back first.
/// Every attack ends with a zoom into the wall: BONK! He flips upside down, dizzy... JUMP ON HIM!
/// The bolts he gobbled up fly back out when you stomp him (half each time, and all the rest when he's beaten).
/// What every boss has (health, phases, stomping, flying away...) is in BossBase.cs.
/// Position is the middle of his bottom, on the ground. He's drawn facing right (his bumper and eyes are in front);
/// Scale turns him around.
/// </summary>
public partial class Vroomba : BossBase
{
    public const string VroombaName = "VROOMBA";
    public override string DisplayName => VroombaName;

    // ---- VROOMBA's numbers (try changing these!) ----
    public const float ZoomSpeed = 800f;            // how fast he zooms across the arena (pixels per second)
    public const float TurboSpeed = 1050f;          // ...and when he's FURIOUS (TURBO!)
    public const int TurboZooms = 3;                // every other TURBO zoom goes there, back, and there again before the BONK
    public const float RevSeconds = 0.9f;           // how long he revs up before a zoom (get ready to jump!)
    public const float SuckSeconds = 3f;            // how long his vacuum stays on
    public const float SuckPull = 0.55f;            // how hard the vacuum pulls Bolt-E: 0.55 = 55% of his riding speed (ride away!)
    public const float DizzySeconds = 2.5f;         // how long he's dizzy after a BONK (stomp him now!)
    public const float FuriousDizzySeconds = 1.9f;  // ...and when he's FURIOUS
    public const float ShortestDizzy = 1.8f;        // he's never dizzy for less than this, so there's always time to stomp him
    public const int BunniesWhenAngry = 2;          // dust bunnies he spits out when he's ANGRY...
    public const int BunniesWhenFurious = 3;        // ...and when he's FURIOUS

    // ---- His size, and his spots in the arena ----
    public const float HalfWidth = 115f;            // he's 230 pixels wide...
    public const float Height = 75f;                // ...and 75 pixels tall. (Bolt-E jumps 212 pixels high: hop over him!)
    const float HitHalfWidth = 100f, HitHeight = 68f; // his bump box is a bit smaller than his drawing, so a jump that only just clears him counts
    public const float LeftSpot = 175f, RightSpot = 1105f; // where he parks before an attack (on the side far from Bolt-E)
    const float ArenaRight = 1280f;                 // the screen edges are the walls he bonks into (the left one is at 0)

    // ---- How long things take (seconds) ----
    const float EnterSeconds = 1.6f;      // rolling in from the right
    const float BeepBoopSeconds = 1.2f;   // his happy BEEP BOOP! spin
    const float FlipSeconds = 0.35f;      // flipping upside down after a BONK (and back over afterwards)
    const float RollSeconds = 1.0f;       // rolling to his spot after he's dizzy or hurt
    const float BonkSeconds = 0.4f;       // bouncing back off the wall
    const float TurnSeconds = 0.3f;       // spinning around just before the wall (TURBO zooms)
    const float SpitEvery = 0.45f;        // seconds between dust bunnies
    const float AngrySeconds = 1.5f;      // shaking with rage when he gets ANGRY or FURIOUS
    const float ZoomPickUp = 4000f;       // how quickly he gets up to zooming speed
    const float BounceBack = 50f;         // how far he bounces back off the wall
    const float TurnAroundGap = 40f;      // how far from the wall he spins around on a TURBO zoom
    const float BigHop = 230f;            // he hops right over Bolt-E when Bolt-E is in the way...
    const float LittleHop = 30f;          // ...otherwise it's just a little bounce
    const float DizzySpin = 3f;           // how fast he spins on his back when he's dizzy
    const float WheelRadius = 11f;

    /// <summary>His attacks for each phase. He goes through the list, then starts over. (Every one ends with a ZOOM.)</summary>
    static readonly VroombaAttack[] PhaseOnePlan = { VroombaAttack.Zoom, VroombaAttack.Suck };
    static readonly VroombaAttack[] AngryPlan = { VroombaAttack.DustBunnies, VroombaAttack.Zoom, VroombaAttack.Suck };
    static readonly VroombaAttack[] FuriousPlan = { VroombaAttack.DustBunnies, VroombaAttack.Suck, VroombaAttack.Zoom };

    enum State
    {
        Entering, BeepBoop, Revving, Zooming, Turning, WallBonk, Dizzy, Hurt, Recovering, GettingAngry,
        Sucking, Spitting, FlyingAway, Exploding, Defeated,
    }

    // ---- Things he does that the rest of the game needs to know about ----
    // (GotAngry, Laughed = "BEEP BOOP!", Popped, BlewUp and PieceBounced are in BossBase.cs: every boss has them)
    public event Action? RevvedUp;                    // vroom vroom... he's about to zoom!
    public event Action? Zoomed;                      // ZOOM! off he goes
    public event Action<Vector2>? BonkedWall;         // BONK! into the wall, here
    public event Action? VacuumOn;                    // SUCK! his vacuum switches on...
    public event Action? VacuumOff;                   // ...and off
    public event Action<Vector2, float>? SpatBunny;   // a dust bunny flies out here, going left (-1) or right (1)
    public event Action<Vector2, int>? BoltsOut;      // this many of the bolts he swallowed fly out of him, from here

    State state = State.Entering;
    float stateTime, time;
    float facing = -1;          // 1 = facing (and driving) right, -1 = left
    int planStep;               // which attack in his list comes next
    int angerShown = 1;         // the phase he last showed us
    float holdAttacks;          // after Bolt-E crashes, he waits this many seconds before attacking again (fair play!)
    bool beepBoopIsTaunt;       // true when he BEEP BOOPs because Bolt-E crashed (then he waits for him afterwards)
    float zoomSpeed;            // how fast he's going right now
    int zoomsLeft;              // zooms still to go (the last one always ends with a BONK)
    int turboZoomsDone;         // how many TURBO zooms he did (every other one goes there and back first)
    bool turnedAround;          // (on a TURBO zoom) he already spun around
    int bunniesLeft;            // dust bunnies still to spit out
    float spitTimer, spitSquash, popTimer;
    float flip;                 // 0 = the right way up, 1 = upside down (after a BONK)
    float brushAngle, wheelAngle;
    float rollToX, hopHeight;   // where he rolls to after he's dizzy or hurt, and how high he hops on the way
    bool rolledThere;           // true once he's at his spot
    float cloudDrift;           // how far the big dust cloud has slid along with the world after he blew up
    Vector2 moveFrom;           // where a move started
    RobotExplosion? explosion;

    /// <summary>How many bolts are in his tummy right now (they come back out when he's stomped!).</summary>
    public int Swallowed { get; private set; }
    /// <summary>How many swallowed bolts came back out so far (the self-test reads this: none are ever lost).</summary>
    public int BoltsGivenBack { get; private set; }
    /// <summary>1 = facing right, -1 = facing left (he always faces the way he drives).</summary>
    public float Facing => facing;
    /// <summary>True while his vacuum is on: Bolt-E and the loose bolts get pulled toward him (see Main.Vroomba.cs).</summary>
    public bool Sucking => state == State.Sucking;
    /// <summary>Where his mouth is (at the bottom of his front bumper), on the screen.</summary>
    public Vector2 Mouth => Position + new Vector2(facing * MouthSpot.X, MouthSpot.Y);
    /// <summary>How fast he's zooming right now (the self-test reads this).</summary>
    public float ZoomingSpeed => zoomSpeed;
    /// <summary>Zooms still to go before the BONK (the self-test reads this).</summary>
    public int ZoomsLeft => zoomsLeft;
    /// <summary>True while he lies upside down, wheels in the air (the self-test reads this).</summary>
    public bool UpsideDown => flip >= 1;
    /// <summary>True if his vacuum's wind was drawn in the last frame (the self-test reads this).</summary>
    public bool WindDrawn { get; private set; }
    /// <summary>True if the "!" over him was drawn in the last frame (the self-test reads this).</summary>
    public bool WarningDrawn { get; private set; }
    /// <summary>True when this piece (like "lid" or "brush") flew off when he blew up (the self-test reads this).</summary>
    public bool HasPiece(string name) => explosion?.HasPart(name) == true;

    readonly bool[] drawnIn = new bool[Enum.GetValues<State>().Length];
    /// <summary>Every move he was drawn in so far (the self-test reads this).</summary>
    public IEnumerable<string> MovesDrawn => Enum.GetValues<State>().Where(move => drawnIn[(int)move]).Select(move => move.ToString());
    /// <summary>All of his moves (the self-test checks that every one of them draws).</summary>
    public static IEnumerable<string> EveryMove => Enum.GetNames<State>();

    public override bool CanBeStomped => state == State.Dizzy;
    /// <summary>
    /// Bumping into him is a crash while he's up to something: revving, zooming, sucking, spitting or angry.
    /// (Rolling in, his BEEP BOOP spin, dizzy, hurt or rolling to his spot: he's harmless.)
    /// </summary>
    public override bool IsDangerous => state is State.Revving or State.Zooming or State.Turning
                                                 or State.Sucking or State.Spitting or State.GettingAngry;
    public override bool IsBeaten => state is State.Exploding or State.Defeated;
    public override bool HasBlownUp => state == State.Defeated;
    public override bool Finished => (state == State.Defeated && stateTime > 3.5f)
                                     || (state == State.FlyingAway && Position.X > ArenaRight + HalfWidth + 100);
    public override string CurrentMove => state.ToString();

    public override Rect2 Hitbox => new(Position + new Vector2(-HitHalfWidth, -HitHeight), new Vector2(HitHalfWidth * 2, HitHeight));

    float DizzyTime => Mathf.Max(ShortestDizzy, Phase == 3 ? FuriousDizzySeconds : DizzySeconds);

    public override void _Ready()
    {
        Position = new Vector2(ArenaRight + HalfWidth + 60, GroundY); // just off the screen on the right
        moveFrom = Position;
        Scale = new Vector2(facing, 1);
    }

    // ---------- What the game asks him to do ----------

    public override void LoseHealth(int amount)
    {
        if (IsBeaten) return;
        Health = Math.Max(0, Health - amount);
        if (Health <= 0)
        {
            ChangeState(State.Exploding);
            popTimer = 0;
        }
        else
        {
            GiveBoltsBack((Swallowed + 1) / 2); // half of the bolts he swallowed fly back out (the rest when he's beaten)
            ChangeState(State.Hurt);
        }
    }

    /// <summary>Bolt-E ran out of batteries: VROOMBA rolls away, off the screen.</summary>
    public override void FlyAway()
    {
        if (!IsBeaten) ChangeState(State.FlyingAway);
    }

    /// <summary>
    /// Bolt-E crashed but has a spare battery: VROOMBA does a happy little "BEEP BOOP!" spin, rolls to his spot and
    /// waits holdSeconds before attacking again, so Bolt-E has time to put himself back together. (Not while he's
    /// bonking the wall, dizzy or hurt, so he never steals your chance to stomp him! Just like Big Rusty.)
    /// </summary>
    public override void Taunt(float holdSeconds)
    {
        if (state is State.Entering or State.WallBonk or State.Dizzy or State.Hurt or State.GettingAngry
                  or State.Exploding or State.Defeated or State.FlyingAway) return;
        holdAttacks = holdSeconds;
        beepBoopIsTaunt = true;
        ChangeState(State.BeepBoop);
    }

    /// <summary>A bolt flew into his mouth: YUM! (Main does the flying: see Main.Vroomba.cs.)</summary>
    public void Swallow() => Swallowed++;

    /// <summary>Starts one of his attacks right now. (He picks them himself. The self-test uses this to skip ahead.)</summary>
    public void DoAttack(VroombaAttack attack)
    {
        FaceBoltE();
        switch (attack)
        {
            case VroombaAttack.Suck:
                ChangeState(State.Sucking);
                break;
            case VroombaAttack.DustBunnies:
                ChangeState(State.Spitting);
                break;
            default:
                ChangeState(State.Revving);
                break;
        }
    }

    // ---------- His brain ----------

    void ChangeState(State next)
    {
        if (state == State.Sucking && next != State.Sucking) VacuumOff?.Invoke();
        state = next;
        stateTime = 0;
        moveFrom = Position;
        switch (next)
        {
            case State.BeepBoop:
                TellLaughed(); // "BEEP BOOP!"
                break;
            case State.Revving:
                facing = Position.X > ArenaRight / 2 ? -1 : 1; // he'll zoom across, to the wall on the other side
                RevvedUp?.Invoke();
                break;
            case State.Zooming:
                zoomSpeed = 0;
                Zoomed?.Invoke();
                break;
            case State.Turning:
                turnedAround = false;
                break;
            case State.Sucking:
                VacuumOn?.Invoke();
                break;
            case State.Spitting:
                bunniesLeft = Phase == 3 ? BunniesWhenFurious : BunniesWhenAngry;
                spitTimer = 0.35f;
                break;
            case State.Hurt:
            case State.Recovering:
                // Off to the side that's far from Bolt-E. If Bolt-E is in the way, he hops right over him.
                rollToX = TargetX < ArenaRight / 2 ? RightSpot : LeftSpot;
                hopHeight = (TargetX - Position.X) * (TargetX - rollToX) < 0 ? BigHop : LittleHop;
                rolledThere = false;
                break;
            case State.GettingAngry:
                planStep = 0; // a new phase starts at the top of its attack list
                TellGotAngry(Phase);
                break;
            case State.FlyingAway:
                zoomSpeed = 0;
                break;
        }
    }

    /// <summary>The next attack in his list (every one ends with a ZOOM and a BONK).</summary>
    void StartNextAttack()
    {
        var plan = Phase switch { 1 => PhaseOnePlan, 2 => AngryPlan, _ => FuriousPlan };
        var attack = plan[planStep % plan.Length];
        planStep++;
        DoAttack(attack);
    }

    /// <summary>ZOOM! (When he's FURIOUS, every other zoom goes there and back first.)</summary>
    void StartZoom()
    {
        zoomsLeft = Phase == 3 && ++turboZoomsDone % 2 == 1 ? TurboZooms : 1;
        ChangeState(State.Zooming);
    }

    void FaceBoltE() => facing = TargetX < Position.X ? -1 : 1;

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        time += dt;
        stateTime += dt;
        holdAttacks = Mathf.Max(0, holdAttacks - dt);

        // He keeps an eye on Bolt-E while he sucks, spits, gets angry or waits at his spot
        if (state is State.Sucking or State.Spitting or State.GettingAngry || (state == State.Recovering && rolledThere)) FaceBoltE();

        switch (state)
        {
            case State.Entering:
                // Rolling in from the right
                Position = new Vector2(Mathf.Lerp(moveFrom.X, RightSpot, Smooth(stateTime / EnterSeconds)), GroundY);
                facing = -1;
                if (stateTime > EnterSeconds) ChangeState(State.BeepBoop);
                break;

            case State.BeepBoop:
                SettleDown(dt);
                if (stateTime > BeepBoopSeconds)
                {
                    if (beepBoopIsTaunt)
                    {
                        beepBoopIsTaunt = false;
                        ChangeState(State.Recovering); // to his spot, and wait for Bolt-E to be back together
                    }
                    else
                    {
                        StartNextAttack();
                    }
                }
                break;

            case State.Revving:
                // Vroom vroom! Rumbling on the spot, brushes spinning...
                Position = new Vector2(moveFrom.X + Mathf.Sin(time * 90) * 2, GroundY);
                if (stateTime > RevSeconds) StartZoom();
                break;

            case State.Zooming:
            {
                zoomSpeed = Mathf.MoveToward(zoomSpeed, Phase == 3 ? TurboSpeed : ZoomSpeed, ZoomPickUp * dt);
                float x = Position.X + facing * zoomSpeed * dt;
                float stopAt = facing > 0 ? ArenaRight - HalfWidth : HalfWidth; // his front touches the wall
                if (zoomsLeft > 1) stopAt -= facing * TurnAroundGap;            // (a TURBO zoom: he spins around just before it)
                bool there = facing > 0 ? x >= stopAt : x <= stopAt;
                Position = new Vector2(there ? stopAt : x, GroundY);
                if (there)
                {
                    zoomsLeft--;
                    if (zoomsLeft > 0)
                    {
                        ChangeState(State.Turning);
                    }
                    else
                    {
                        BonkedWall?.Invoke(new Vector2(facing > 0 ? ArenaRight : 0, GroundY - 40));
                        ChangeState(State.WallBonk);
                    }
                }
                break;
            }

            case State.Turning:
                // Screech! Spin around and zoom back the other way
                zoomSpeed = 0;
                if (!turnedAround && stateTime > TurnSeconds / 2)
                {
                    facing = -facing;
                    turnedAround = true;
                }
                if (stateTime > TurnSeconds) ChangeState(State.Zooming);
                break;

            case State.WallBonk:
            {
                // BONK! He bounces back a little, flipping upside down
                float t = Mathf.Min(1, stateTime / BonkSeconds);
                Position = new Vector2(moveFrom.X - facing * BounceBack * Smooth(t), GroundY - 40 * Mathf.Sin(Mathf.Pi * t));
                if (stateTime > BonkSeconds) ChangeState(State.Dizzy);
                break;
            }

            case State.Dizzy:
                // Upside down, wheels in the air, spinning slowly... STOMP HIM!
                Position = new Vector2(Position.X, GroundY);
                if (stateTime > DizzyTime) ChangeState(State.Recovering);
                break;

            case State.Hurt:
                // Ouch! He flips back over and rolls to his spot (blinking)
                if (!rolledThere) rolledThere = RollToHisSpot();
                if (rolledThere)
                {
                    if (Phase != angerShown)
                    {
                        angerShown = Phase;
                        ChangeState(State.GettingAngry);
                    }
                    else
                    {
                        StartNextAttack();
                    }
                }
                break;

            case State.Recovering:
                // He flips back over and rolls to his spot. If Bolt-E is still being put back together, he waits.
                if (!rolledThere)
                {
                    rolledThere = RollToHisSpot();
                }
                else if (holdAttacks > 0)
                {
                    Position = new Vector2(rollToX, GroundY - Mathf.Abs(Mathf.Sin(time * 6)) * 3); // a happy little purr
                }
                else
                {
                    Position = new Vector2(rollToX, GroundY);
                    StartNextAttack();
                }
                break;

            case State.GettingAngry:
                Position = new Vector2(moveFrom.X + Mathf.Sin(time * 70) * 5, GroundY); // shaking with rage
                if (stateTime > AngrySeconds) StartNextAttack();
                break;

            case State.Sucking:
                // SUCK! (Main pulls Bolt-E and the bolts toward his mouth: see Main.Vroomba.cs)
                SettleDown(dt);
                if (stateTime > SuckSeconds) ChangeState(State.Revving); // and then a ZOOM, like always
                break;

            case State.Spitting:
                SettleDown(dt);
                spitTimer -= dt;
                if (spitTimer <= 0)
                {
                    if (bunniesLeft > 0)
                    {
                        SpatBunny?.Invoke(Mouth + new Vector2(facing * 10, -10), facing); // PTOO!
                        bunniesLeft--;
                        spitSquash = 1;
                        spitTimer = bunniesLeft > 0 ? SpitEvery : 0.6f;
                    }
                    else
                    {
                        ChangeState(State.Revving); // and then a ZOOM, like always
                    }
                }
                break;

            case State.FlyingAway:
                // Bolt-E is out of batteries: VROOMBA rolls away happily, bouncing
                facing = 1;
                zoomSpeed = Mathf.MoveToward(zoomSpeed, 700, 1500 * dt);
                Position = new Vector2(Position.X + zoomSpeed * dt, GroundY - Mathf.Abs(Mathf.Sin(stateTime * 9)) * 12);
                break;

            case State.Exploding:
                // Shaking and popping before the big boom
                Position = new Vector2(moveFrom.X + Mathf.Sin(time * 80) * 5, Mathf.MoveToward(Position.Y, GroundY, 900 * dt));
                popTimer -= dt;
                if (popTimer <= 0)
                {
                    popTimer = 0.17f;
                    var box = Hitbox;
                    TellPopped(box.Position + new Vector2((float)GD.RandRange(0, box.Size.X), (float)GD.RandRange(0, box.Size.Y)));
                }
                if (stateTime > 1.6f) BlowUp();
                break;

            case State.Defeated:
                // (His pieces slide along with the ground. He's drawn turned around when he faced left, so the slide is too.)
                explosion?.Update(dt, WorldSpeed * facing);
                cloudDrift += WorldSpeed * dt;
                break;
        }

        // He flips upside down when he bonks the wall, and back over afterwards
        float flipTo = state is State.WallBonk or State.Dizzy ? 1 : 0;
        flip = Mathf.MoveToward(flip, flipTo, dt / FlipSeconds);
        spitSquash = Mathf.MoveToward(spitSquash, 0, dt * 5);

        // The side brushes spin (a blur when he's revving, zooming or sucking), and the wheels roll
        float brushSpin = state switch
        {
            State.Revving or State.Zooming or State.Turning or State.Sucking => 32f,
            State.Dizzy => 5f,
            State.Exploding or State.Defeated => 0f,
            _ => 12f,
        };
        brushAngle += brushSpin * dt;
        float rolling = state switch
        {
            State.Zooming or State.FlyingAway => zoomSpeed,
            State.Revving => 500f,                                  // spinning on the spot!
            State.Entering or State.Hurt or State.Recovering => rolledThere ? 0f : 300f,
            State.Dizzy => 150f,                                    // spinning in the air
            _ => 0f,
        };
        wheelAngle += rolling / WheelRadius * dt;

        // Which way he faces. He spins around for BEEP BOOP and while he's dizzy (never paper-thin), and squishes when he spits.
        float turn = 1;
        if (state == State.BeepBoop) turn = Mathf.Cos(stateTime / BeepBoopSeconds * Mathf.Tau);
        else if (state == State.Dizzy) turn = Mathf.Cos(stateTime * DizzySpin);
        if (Mathf.Abs(turn) < 0.15f) turn = turn < 0 ? -0.15f : 0.15f;
        Scale = new Vector2(facing * turn, 1f - 0.12f * spitSquash);

        // Blink while hurt; flash while breaking down; a bit red when FURIOUS
        bool blink = (state == State.Hurt && stateTime < 1f && (int)(stateTime * 12) % 2 == 0)
                     || (state == State.Exploding && (int)(stateTime * 16) % 2 == 0);
        Modulate = blink ? new Color(1, 1, 1, 0.35f) : Phase == 3 && !IsBeaten ? new Color(1f, 0.88f, 0.85f) : Colors.White;
        QueueRedraw();
    }

    /// <summary>
    /// He flips back over (with a little hop), then rolls to his spot, hopping right over Bolt-E if Bolt-E is in the way.
    /// True when he's there.
    /// </summary>
    bool RollToHisSpot()
    {
        if (stateTime < FlipSeconds)
        {
            Position = new Vector2(moveFrom.X, GroundY - 25 * Mathf.Sin(Mathf.Pi * stateTime / FlipSeconds));
            return false;
        }
        float t = Mathf.Min(1, (stateTime - FlipSeconds) / RollSeconds);
        Position = new Vector2(Mathf.Lerp(moveFrom.X, rollToX, Smooth(t)), GroundY - hopHeight * 4 * t * (1 - t));
        if (Mathf.Abs(rollToX - moveFrom.X) > 1) facing = rollToX > moveFrom.X ? 1 : -1; // facing the way he rolls
        return t >= 1;
    }

    /// <summary>Back down to the ground (after a hop).</summary>
    void SettleDown(float dt) => Position = new Vector2(Position.X, Mathf.MoveToward(Position.Y, GroundY, 900 * dt));

    /// <summary>Some of the bolts in his tummy fly back out!</summary>
    void GiveBoltsBack(int count)
    {
        count = Math.Min(count, Swallowed);
        if (count <= 0) return;
        Swallowed -= count;
        BoltsGivenBack += count;
        BoltsOut?.Invoke(Position + new Vector2(0, -Height), count);
    }

    void BlowUp()
    {
        GiveBoltsBack(Swallowed); // every bolt he swallowed comes back out!
        state = State.Defeated;
        stateTime = 0;
        flip = 0;
        Scale = new Vector2(facing, 1);
        var parts = new List<RobotPart>
        {
            new RobotPart("wheel", new Vector2(-62, -10), WheelRadius, () => DrawWheel(-62), FlatEvery: Mathf.Pi / 8),
            new RobotPart("wheel", new Vector2(62, -10), WheelRadius, () => DrawWheel(62), FlatEvery: Mathf.Pi / 8),
            new RobotPart("body", new Vector2(0, -36), 24, DrawSide, FlatEvery: Mathf.Pi),
            new RobotPart("lid", new Vector2(0, -62), 12, DrawLid, FlatEvery: Mathf.Pi),
            new RobotPart("bumper", new Vector2(78, -35), 20, DrawBumper, FlatEvery: Mathf.Pi),
            new RobotPart("brush", BackBrush, 14, () => DrawBrush(BackBrush), FlatEvery: Mathf.Tau / 3),
            new RobotPart("brush", FrontBrush, 14, () => DrawBrush(FrontBrush), FlatEvery: Mathf.Tau / 3),
            new RobotPart("button", ButtonSpot, 7, DrawButton, FlatEvery: Mathf.Pi),
        };
        explosion = new RobotExplosion(parts, FlipPivot, 0, GroundY - Position.Y, VroombaBlue,
                                       onBounce: TellPieceBounced, boomAt: new Vector2(0, -40));
        TellBlewUp();
    }

    // ---------- Drawing (everything is drawn facing right, at his real size; Scale turns him around) ----------

    // ---- His colors (try changing these!) ----
    public static readonly Color VroombaBlue = new(0.35f, 0.8f, 1f);   // his color: the CLEAN button, his eyes, "BEEP BOOP!"
    static readonly Color Lid = new(0.17f, 0.18f, 0.22f);              // the dark charcoal lid
    static readonly Color Side = new(0.27f, 0.28f, 0.33f);             // the side of his round body
    static readonly Color Silver = new(0.8f, 0.82f, 0.88f);            // the shiny rim ring and the wheel hubs
    static readonly Color Bumper = new(0.55f, 0.58f, 0.64f);           // the front bumper
    static readonly Color Lens = new(0.05f, 0.06f, 0.09f);             // the dark glass of his sensor eyes
    static readonly Color Tire = new(0.12f, 0.12f, 0.14f);
    static readonly Color Bristles = new(1f, 0.8f, 0.25f);             // the side brushes' arms
    static readonly Color AngryOrange = new(1f, 0.6f, 0.15f);
    static readonly Color FuriousRed = new(1f, 0.25f, 0.2f);
    static readonly Color Dust = new(0.75f, 0.72f, 0.68f);
    static readonly Color StarYellow = new(1f, 0.88f, 0.25f);

    // ---- Where his parts are ----
    static readonly Vector2 FlipPivot = new(0, -37);       // he flips upside down around his middle
    static readonly Vector2 BackBrush = new(-104, -13), FrontBrush = new(104, -13);
    static readonly Vector2 BackEye = new(66, -36), FrontEye = new(95, -36);
    static readonly Vector2 ButtonSpot = new(0, -75);
    static readonly Vector2 MouthSpot = new(HalfWidth - 2, -16);  // (at the bottom of his front bumper)

    // ---- Shapes made once (not every time he's drawn) ----
    static readonly Vector2[] LidShape = MakeLid();
    static readonly Vector2[] BackEyeLid = MakeEyelid(BackEye), FrontEyeLid = MakeEyelid(FrontEye);
    // Where the wind lines of his vacuum start (in front of him); they swoosh into his mouth
    static readonly Vector2[] WindStarts =
    {
        new(430, -130), new(470, -70), new(400, -20), new(360, -105), new(450, -40), new(380, -60), new(420, -95), new(350, -12),
    };
    // The big dust cloud when he blows up: where each puff goes, and how big it gets
    static readonly (Vector2 Spot, float Size)[] CloudPuffs =
    {
        (new(-120, -10), 34), (new(-70, -60), 40), (new(0, -80), 46), (new(70, -55), 40), (new(125, -10), 34),
        (new(-40, 10), 30), (new(40, 12), 30), (new(-150, -50), 26), (new(150, -45), 26), (new(-20, -120), 30),
        (new(60, -110), 26), (new(-95, -100), 24), (new(100, -95), 24), (new(0, -30), 50),
    };
    // Where the FURIOUS sparks crackle
    static readonly Vector2[] SparkSpots = { new(-80, -70), new(-30, -88), new(40, -86), new(85, -66) };

    /// <summary>The lid: a flat dome on top of him (half an oval).</summary>
    static Vector2[] MakeLid()
    {
        var lid = new Vector2[17];
        for (int i = 0; i < lid.Length; i++)
        {
            float angle = Mathf.Pi + i * Mathf.Pi / (lid.Length - 1); // from the back, over the top, to the front
            lid[i] = new Vector2(Mathf.Cos(angle) * 110, -56 + Mathf.Sin(angle) * 18);
        }
        return lid;
    }

    /// <summary>A sneaky eyelid over the top of an eye: lower at the front, so he looks up to something.</summary>
    static Vector2[] MakeEyelid(Vector2 eye) => new[]
    {
        eye + new Vector2(-12, -13), eye + new Vector2(12, -13), eye + new Vector2(12, -1), eye + new Vector2(-12, -7),
    };

    public override void _Draw()
    {
        drawnIn[(int)state] = true;
        WindDrawn = WarningDrawn = false;

        if (explosion is not null)
        {
            DrawDustCloud();
            explosion.Draw(this);
            return;
        }

        DrawShadow();
        DrawDustBehind();

        // Upside down after a BONK: everything turns around his middle (the shadow and the stars don't)
        float angle = Mathf.Pi * Smooth(flip);
        if (angle > 0.001f) DrawSetTransform(FlipPivot - FlipPivot.Rotated(angle), angle, Vector2.One);
        DrawWheel(-62);
        DrawWheel(62);
        DrawSide();
        DrawLid();
        DrawBumper();
        DrawBrush(BackBrush);
        DrawBrush(FrontBrush);
        DrawButton();
        if (angle > 0.001f) DrawSetTransform(Vector2.Zero, 0, Vector2.One);

        if (state == State.Zooming) DrawSpeedLines();
        if (state == State.Sucking) DrawWind();
        if (state == State.Revving) DrawWarning();
        if (state is State.Dizzy || (state == State.WallBonk && stateTime > BonkSeconds / 2)) DrawDizzyStars();
        DrawMood();
    }

    void DrawShadow()
    {
        // The shadow is on the ground, however high he hops
        float height = GroundY - Position.Y;
        float size = Mathf.Clamp(1f - height / 400f, 0.4f, 1f);
        Shapes.Ellipse(this, new Vector2(0, height + 2), new Vector2(118 * size, 10 * size), new Color(0, 0, 0, 0.2f));
    }

    void DrawWheel(float x)
    {
        var hub = new Vector2(x, -10);
        DrawCircle(hub, WheelRadius, Tire);
        DrawCircle(hub, 5, Silver);
        var spoke = Vector2.Right.Rotated(wheelAngle) * 8;
        DrawLine(hub - spoke, hub + spoke, Silver.Darkened(0.35f), 2.5f, true);
    }

    void DrawSide()
    {
        // The side of his round body, a dark stripe, and little air vents at the back
        Shapes.RoundRect(this, new Rect2(-HalfWidth, -58, HalfWidth * 2, 44), 16, Side);
        DrawLine(new Vector2(-100, -24), new Vector2(36, -24), Side.Darkened(0.35f), 3, true);
        for (int i = 0; i < 3; i++) DrawCircle(new Vector2(-90 + i * 12, -42), 3, Side.Darkened(0.4f));
    }

    void DrawLid()
    {
        // The dark charcoal lid (a flat dome) with a shine, and the shiny silver rim ring around it
        DrawColoredPolygon(LidShape, Lid);
        Shapes.Ellipse(this, new Vector2(-35, -65), new Vector2(32, 3.5f), new Color(1, 1, 1, 0.16f));
        Shapes.RoundRect(this, new Rect2(-HalfWidth - 1, -61, HalfWidth * 2 + 2, 8), 4, Silver);
        DrawLine(new Vector2(-100, -59), new Vector2(60, -59), new Color(1, 1, 1, 0.7f), 1.5f, true);
    }

    void DrawBumper()
    {
        // The front bumper (it sticks out a little), with his two big sensor eyes, and his mouth at the bottom
        Shapes.RoundRect(this, new Rect2(38, -54, HalfWidth + 3 - 38, 38), 15, Bumper);
        DrawLine(new Vector2(46, -51), new Vector2(108, -51), new Color(1, 1, 1, 0.35f), 2, true);
        if (state is State.Sucking or State.Spitting) Shapes.RoundRect(this, new Rect2(MouthSpot.X - 16, MouthSpot.Y - 5, 18, 10), 4, Lens);
        DrawEye(BackEye, BackEyeLid);
        DrawEye(FrontEye, FrontEyeLid);
    }

    /// <summary>
    /// One big sensor eye: cute and round, sneaky when he attacks, happy when he BEEP BOOPs, spinning spirals when he's
    /// dizzy, and X eyes when he's hurt or beaten. They glow blue, then orange when he's ANGRY and red when he's FURIOUS.
    /// </summary>
    void DrawEye(Vector2 eye, Vector2[] lid)
    {
        DrawCircle(eye, 13, Lens);
        DrawArc(eye, 13, 0, Mathf.Tau, 24, Silver.Darkened(0.25f), 2, true);
        var color = Phase switch { 1 => VroombaBlue, 2 => AngryOrange, _ => FuriousRed };
        switch (state)
        {
            case State.Hurt or State.Exploding or State.Defeated:
                Shapes.XEye(this, eye, 5, color);
                break;
            case State.Dizzy or State.WallBonk:
                for (int ring = 0; ring < 3; ring++)
                    DrawArc(eye, 2.2f + ring * 2.6f, time * 9 + ring, time * 9 + ring + Mathf.Pi * 1.4f, 10, color, 1.8f, true);
                break;
            case State.BeepBoop:
                Shapes.HappyEye(this, eye + new Vector2(0, 1), 6, color, 3.5f);
                break;
            default:
                DrawCircle(eye, 10, new Color(color, 0.25f));                     // the glow
                DrawCircle(eye, 7.5f, color);
                DrawCircle(eye + new Vector2(-2.5f, -3), 2.2f, new Color(1, 1, 1, 0.85f)); // a sparkle
                bool attacking = state is State.Revving or State.Zooming or State.Turning or State.Sucking or State.Spitting
                                          or State.GettingAngry;
                if (attacking) DrawColoredPolygon(lid, Lens); // sneaky!
                break;
        }
    }

    void DrawBrush(Vector2 hub)
    {
        // Three arms spinning around. When he revs, zooms or sucks they spin so fast they blur!
        bool blur = state is State.Revving or State.Zooming or State.Turning or State.Sucking;
        if (blur) DrawCircle(hub, 15, new Color(Bristles, 0.3f));
        var arms = blur ? new Color(Bristles, 0.6f) : Bristles;
        for (int i = 0; i < 3; i++)
            DrawLine(hub, hub + Vector2.Right.Rotated(brushAngle + i * Mathf.Tau / 3) * 14, arms, 3, true);
        DrawCircle(hub, 4.5f, Side.Darkened(0.3f));
    }

    void DrawButton()
    {
        // The glowing round CLEAN button on top (blue, red when he's FURIOUS)
        var color = Phase == 3 ? FuriousRed : VroombaBlue;
        float pulse = 0.5f + 0.5f * Mathf.Sin(time * 5);
        Shapes.Ellipse(this, ButtonSpot, new Vector2(26, 9), new Color(color, 0.18f + 0.15f * pulse)); // the glow
        Shapes.Ellipse(this, ButtonSpot + new Vector2(0, 1), new Vector2(18, 6), Lid.Darkened(0.3f));
        Shapes.Ellipse(this, ButtonSpot, new Vector2(14, 5), color);
        Shapes.Ellipse(this, ButtonSpot + new Vector2(-4, -1.5f), new Vector2(5, 1.6f), new Color(1, 1, 1, 0.75f));
    }

    /// <summary>A tiny puff of dust from his back while he drives (a big one while he revs up).</summary>
    void DrawDustBehind()
    {
        bool driving = state is State.Entering or State.Revving or State.Zooming or State.Turning or State.FlyingAway
                       || (state is State.Hurt or State.Recovering && !rolledThere && stateTime > FlipSeconds);
        if (!driving || GroundY - Position.Y > 5) return; // (not while he's up in the air)
        bool revving = state == State.Revving;
        int puffs = revving ? 5 : 3;
        for (int i = 0; i < puffs; i++)
        {
            float age = (time * (revving ? 3.2f : 2.2f) + i / (float)puffs) % 1f; // 0 = brand new, 1 = gone
            var at = new Vector2(-HalfWidth - 6 - age * (revving ? 60 : 40), -6 - age * (revving ? 34 : 18));
            DrawCircle(at, 4 + age * (revving ? 12 : 7), new Color(Dust, 0.55f * (1 - age)));
        }
    }

    void DrawSpeedLines()
    {
        for (int i = 0; i < 3; i++)
        {
            float length = 50 + 30 * ((i + (int)(time * 20)) % 2);
            float y = -62 + i * 22;
            DrawLine(new Vector2(-HalfWidth - 20, y), new Vector2(-HalfWidth - 20 - length, y), new Color(1, 1, 1, 0.6f), 3, true);
        }
    }

    /// <summary>The wind of his vacuum: lines swooshing into his mouth, and two little swirls.</summary>
    void DrawWind()
    {
        for (int i = 0; i < WindStarts.Length; i++)
        {
            float along = (time * 1.5f + i / (float)WindStarts.Length) % 1f; // 0 = far away, 1 = in his mouth
            var from = WindStarts[i].Lerp(MouthSpot, along);
            var to = WindStarts[i].Lerp(MouthSpot, Mathf.Min(1, along + 0.18f));
            DrawLine(from, to, new Color(1, 1, 1, 0.75f * Mathf.Sin(Mathf.Pi * along)), 3, true);
        }
        for (int k = 0; k < 2; k++)
        {
            float start = time * 8 + k * 2;
            DrawArc(MouthSpot + new Vector2(50 + k * 50, -14), 14 + k * 6, start, start + Mathf.Pi * 1.3f, 12,
                    new Color(1, 1, 1, 0.55f), 2.5f, true);
        }
        WindDrawn = true;
    }

    /// <summary>A big red "!" over him while he revs up: get ready to jump!</summary>
    void DrawWarning()
    {
        var font = ThemeDB.FallbackFont;
        var size = font.GetStringSize("!", HorizontalAlignment.Left, -1, 60);
        var at = new Vector2(-size.X / 2, -100 - Mathf.Abs(Mathf.Sin(time * 10)) * 8);
        DrawStringOutline(font, at, "!", HorizontalAlignment.Left, -1, 60, 10, Colors.White);
        DrawString(font, at, "!", HorizontalAlignment.Left, -1, 60, new Color(0.95f, 0.15f, 0.15f));
        WarningDrawn = true;
    }

    void DrawDizzyStars()
    {
        for (int i = 0; i < 4; i++)
        {
            float angle = time * 5f + i * Mathf.Tau / 4f;
            Shapes.Star(this, new Vector2(Mathf.Cos(angle) * 70, -100 + Mathf.Sin(angle) * 12), 8, time * 4, StarYellow);
        }
    }

    void DrawMood()
    {
        if (IsBeaten || state is State.Dizzy or State.WallBonk or State.FlyingAway) return;

        // ANGRY: steam puffs out of the top, like a kettle
        if (Phase >= 2)
        {
            for (int i = 0; i < 2; i++)
            {
                float rise = (time * 50 + i * 24) % 48;
                var puff = new Color(1, 1, 1, 0.55f * (1 - rise / 48));
                DrawCircle(new Vector2(-24 + i * 48 - (i == 0 ? rise * 0.3f : -rise * 0.3f), -84 - rise), 4 + rise * 0.12f, puff);
            }
        }

        // FURIOUS: crackly sparks
        if (Phase == 3)
        {
            for (int i = 0; i < SparkSpots.Length; i++)
            {
                if (((int)(time * 12) + i) % 3 == 0) continue; // (they flicker)
                var line = Vector2.Right.Rotated(time * 17 + i * 2) * 7;
                DrawLine(SparkSpots[i] - line, SparkSpots[i] + line, new Color(1f, 0.95f, 0.4f), 2, true);
            }
        }
    }

    /// <summary>The big dust cloud when he blows up (it puffs out, then fades away).</summary>
    void DrawDustCloud()
    {
        const float CloudSeconds = 2.5f;
        if (stateTime > CloudSeconds) return;
        float grow = Mathf.Min(1, stateTime / 0.6f);
        float fade = 1 - stateTime / CloudSeconds;
        var middle = new Vector2(-cloudDrift * facing, -45); // (it slides along with the world, like his pieces)
        foreach (var (spot, size) in CloudPuffs)
            DrawCircle(middle + spot * (0.4f + 0.8f * grow), size * (0.5f + 0.7f * grow), new Color(Dust, 0.6f * fade));
    }
}
