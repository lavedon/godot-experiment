using Godot;

/// <summary>DRONEZILLA's attacks. He does one or two of them, then his battery runs low and he LANDS (stomp him!).</summary>
public enum DroneAttack
{
    Bombs,       // BOMBS AWAY! He flies over Bolt-E and drops bombs (a red ring shows where each one will land)
    Swoop,       // SWOOP! A "!", then he dives down low across the arena (jump over him!)
    Zap,         // ZAP BEAM! A red circle follows Bolt-E, LOCKS, then a beam zaps down on it (ride out of it!)
    MiniDrones,  // MINI-DRONES! Little buzzing drones that drift after Bolt-E (stomp them!)
}

/// <summary>
/// DRONEZILLA, the giant attack drone! The third boss: he takes turns with BIG RUSTY and VROOMBA (see Main.Vroomba.cs).
/// He FLIES, high above Bolt-E, so most of the time Bolt-E can't reach him (and bumping into him up there never hurts).
/// He has 6 health and gets angrier as he gets hurt:
///   Phase 1:            BOMBS AWAY! (3 bombs, each with a red target ring) and SWOOP! (a dive across the arena: jump!)
///   Phase 2 (ANGRY):    he also drops MINI-DRONES (stomp them!) and fires his ZAP BEAM (ride out of the red circle!)
///   Phase 3 (FURIOUS):  5 bombs, and his swoop goes there AND back.
/// After every attack his battery runs low: "LOW BATTERY!" He lands to charge up... JUMP ON HIM!
/// What every boss has (health, phases, stomping, flying away...) is in BossBase.cs. What the game does when he drops a
/// bomb (and so on) is in Main.Dronezilla.cs. His bombs are DroneBomb.cs, his beam is ZapBeam.cs.
/// Position is the middle of the bottom of his landing skids. He's drawn facing right (his camera and visor are in
/// front); Scale turns him around.
/// </summary>
public partial class Dronezilla : BossBase
{
    public const string DronezillaName = "DRONEZILLA";
    public override string DisplayName => DronezillaName;

    // ---- DRONEZILLA's numbers (try changing these!) ----
    public const float CruiseHeight = 365f;        // how high he flies: the bottom of his skids, in pixels above the ground
    public const int BombsWhenCalm = 3;            // bombs in one BOMBS AWAY...
    public const int BombsWhenFurious = 5;         // ...and when he's FURIOUS
    public const float BombEvery = 0.55f;          // seconds between two bombs...
    public const float FuriousBombEvery = 0.42f;   // ...and when he's FURIOUS
    public const float FlyOverSpeed = 520f;        // how fast he flies over to Bolt-E before his first bomb...
    public const float BombChaseSpeed = 300f;      // ...and after it (Bolt-E rides 430: get away!)
    public const float SwoopWarnSeconds = 0.8f;    // the "!" before a swoop (get ready to jump!)
    public const float SwoopSeconds = 1.6f;        // one swoop across the arena takes this long...
    public const float FuriousSwoopSeconds = 1.3f; // ...and when he's FURIOUS (there AND back!)
    public const float SwoopLow = 35f;             // how low his skids get in a swoop (his bump box starts 12 higher)
    public const float ZapTrackSeconds = 1.2f;     // the red circle follows Bolt-E this long...
    public const float ZapLockSeconds = 0.5f;      // ...then it LOCKS (it stops and glows: ride out of it!)...
    public const float ZapFireSeconds = 0.5f;      // ...then the zap beam fires for this long
    public const float ZapFollowSpeed = 320f;      // how fast he (and the red circle) follows Bolt-E
    public const int MinisWhenAngry = 2;           // mini-drones he drops when he's ANGRY...
    public const int MinisWhenFurious = 3;         // ...and when he's FURIOUS
    public const float LandedSeconds = 2.6f;       // how long he sits on the ground with a flat battery (STOMP HIM!)
    public const float FuriousLandedSeconds = 1.9f;// ...and when he's FURIOUS
    public const float ShortestLanded = 1.8f;      // he never sits for less than this, so there's always time to stomp him
    public const float LandAway = 300f;            // he never lands closer to Bolt-E than this (no landing on his head!)

    // ---- His size, and his spots in the arena ----
    public const float HalfWidth = 64f;            // his body is 128 pixels wide (with his arms about 220, with the rotors 290)
    const float HitHalfWidth = 62f;                // his bump box: from the top of his body...
    const float HitTop = 92f;
    const float HitBottom = 12f;                   // ...down to his skid legs (a bit smaller than his drawing, to be fair)
    public const float LandLeft = 150f, LandRight = 1130f; // where he can land (inside the arena)
    const float AimLeft = 80f, AimRight = 1200f;   // how far to the sides he flies to aim at Bolt-E
    const float SwoopLeft = 60f, SwoopRight = 1220f;
    const float EnterSpotX = 1000f;                // where he stops after flying in
    const float ArenaRight = 1280f;

    // ---- How long things take (seconds) ----
    const float EnterSeconds = 1.8f;       // flying in from the top-right
    const float LowBatterySeconds = 0.8f;  // "LOW BATTERY!" sputtering in the air
    const float DescendSeconds = 1.2f;     // gliding down to his landing spot
    const float TakeOffSeconds = 0.8f;     // back up to cruising height
    const float HurtSeconds = 1.3f;        // wobbling back up after a stomp
    const float AngrySeconds = 1.3f;       // shaking with rage when he gets ANGRY or FURIOUS
    const float TauntSeconds = 1.3f;       // his happy loop-the-loop
    const float MiniEvery = 0.4f;          // seconds between mini-drones
    const float BombsLandTime = 0.8f;      // after the last bomb he waits for them to land
    const float ClimbSpeed = 260f;         // how fast he climbs back up to cruising height
    const float LoopSize = 55f;            // how big his loop-the-loop is
    const float TiltPerSpeed = 0.0005f;    // how much he leans into the way he flies
    const float MaxTilt = 0.4f;

    /// <summary>
    /// His attack plan for each phase: a list of SEQUENCES. He does every attack in one sequence, then his battery runs
    /// low and he lands (stomp him!). Then the next sequence. After the last one, he starts over.
    /// </summary>
    static readonly DroneAttack[][] PhaseOnePlan = { new[] { DroneAttack.Bombs }, new[] { DroneAttack.Swoop } };
    static readonly DroneAttack[][] AngryPlan =
    {
        new[] { DroneAttack.MiniDrones, DroneAttack.Bombs }, new[] { DroneAttack.Zap, DroneAttack.Swoop },
    };
    static readonly DroneAttack[][] FuriousPlan =
    {
        new[] { DroneAttack.MiniDrones, DroneAttack.Bombs }, new[] { DroneAttack.Zap, DroneAttack.Swoop },
    };

    enum State
    {
        Entering, Hovering, Bombing, SwoopWarning, Swooping, Zapping, DroppingMinis, LowBattery, Descending, Landed,
        TakingOff, Hurt, GettingAngry, Taunting, FlyingAway, Exploding, Defeated,
    }

    // ---- Things he does that the rest of the game needs to know about ----
    // (GotAngry, Laughed = "BZZT BZZT!", Popped, BlewUp and PieceBounced are in BossBase.cs: every boss has them)
    public event Action<Vector2>? BombDropped;   // a bomb falls from his belly, here
    public event Action? SwoopWarned;            // "!" he's about to swoop
    public event Action? Swooped;                // WHOOSH! down he dives
    public event Action? ZapAimed;               // his zap beam starts aiming (a red circle follows Bolt-E)
    public event Action<Vector2>? MiniDropped;   // a mini-drone drops out of his belly, here
    public event Action? BatteryLow;             // LOW BATTERY! (he's going to land)
    public event Action<Vector2>? TouchedDown;   // he landed, here (his rotors spin down)
    public event Action? TookOff;                // his rotors whir back up
    public event Action<Vector2>? CrashedDown;   // beaten: he crashed into the ground, here

    State state = State.Entering;
    float stateTime, time;
    float facing = -1;          // 1 = facing right, -1 = facing left
    int planStep;               // which sequence in his plan comes next
    DroneAttack[] sequence = PhaseOnePlan[0];
    int sequenceStep;           // which attack in the sequence he's doing
    int angerShown = 1;         // the phase he last showed us
    float holdAttacks;          // after Bolt-E crashes, he waits this many seconds before attacking again (fair play!)
    float hoverWait;            // how long he hovers before his next attack
    int bombsLeft, minisLeft, swoopsLeft;
    float bombTimer, miniTimer;
    float swoopFrom, swoopTo;   // a swoop goes from here to there
    float landX;                // where he's going to land
    float battery = 1;          // his battery: 1 = full, 0 = flat
    float rotorSpeed = 55f;     // how fast his rotor blades spin
    float rotorAngle;
    float tilt;                 // how much he leans (nose down = leaning forward)
    float flySpeed, fallSpeed, popTimer, crashTime;
    bool crashedDown;           // (beaten) he hit the ground
    Vector2 moveFrom;           // where a move started
    Vector2 camLook;            // where his red camera eye looks (at Bolt-E!)
    RobotExplosion? explosion;

    /// <summary>1 = facing right, -1 = facing left.</summary>
    public float Facing => facing;
    /// <summary>His battery: 1 = full, 0 = flat (the icon on his side shows it).</summary>
    public float Battery => battery;
    /// <summary>How fast his rotor blades spin (0 = stopped). The self-test reads this.</summary>
    public float RotorSpeed => rotorSpeed;
    /// <summary>Where he'll land (once his battery is low).</summary>
    public float LandingSpotX => landX;
    /// <summary>True while he aims and fires his zap beam (the beam follows him: see Main.Dronezilla.cs).</summary>
    public bool Zapping => state == State.Zapping;
    /// <summary>True while his rotors buzz (he's flying). Main plays the buzzing sound then.</summary>
    public bool BuzzOn => state is not (State.Landed or State.Exploding or State.Defeated or State.FlyingAway);
    /// <summary>True while he does an attack (his eyes look mischievous).</summary>
    public bool Attacking => state is State.Bombing or State.SwoopWarning or State.Swooping or State.Zapping or State.DroppingMinis;
    /// <summary>His camera eye (where the zap beam comes out), on the screen.</summary>
    public Vector2 CameraWorldSpot => Position + new Vector2(CameraSpot.X * facing, CameraSpot.Y);
    /// <summary>True if the "!" over him was drawn in the last frame (the self-test reads this).</summary>
    public bool WarningDrawn { get; private set; }
    /// <summary>True if his landing spot was drawn on the ground in the last frame (the self-test reads this).</summary>
    public bool LandingSpotDrawn { get; private set; }
    /// <summary>True when this piece (like "blade" or "camera") flew off when he blew up (the self-test reads this).</summary>
    public bool HasPiece(string name) => explosion?.HasPart(name) == true;

    readonly bool[] drawnIn = new bool[Enum.GetValues<State>().Length];
    /// <summary>Every move he was drawn in so far (the self-test reads this).</summary>
    public IEnumerable<string> MovesDrawn => Enum.GetValues<State>().Where(move => drawnIn[(int)move]).Select(move => move.ToString());
    /// <summary>All of his moves (the self-test checks that every one of them draws).</summary>
    public static IEnumerable<string> EveryMove => Enum.GetNames<State>();

    /// <summary>He can only be stomped when he has LANDED (his battery is flat).</summary>
    public override bool CanBeStomped => state == State.Landed;
    /// <summary>Bumping into him is only a crash during a SWOOP. Up in the air, or landed, he never hurts Bolt-E.</summary>
    public override bool IsDangerous => state == State.Swooping;
    public override bool IsBeaten => state is State.Exploding or State.Defeated;
    public override bool HasBlownUp => state == State.Defeated;
    public override bool Finished => (state == State.Defeated && stateTime > 3.5f) || (state == State.FlyingAway && Position.Y < -260);
    public override string CurrentMove => state.ToString();

    public override Rect2 Hitbox => new(Position + new Vector2(-HitHalfWidth, -HitTop), new Vector2(HitHalfWidth * 2, HitTop - HitBottom));

    float LandedTime => Mathf.Max(ShortestLanded, Phase == 3 ? FuriousLandedSeconds : LandedSeconds);
    float CruiseY => GroundY - CruiseHeight;

    public override void _Ready()
    {
        Position = new Vector2(ArenaRight + 200, -150); // just off the screen, top-right
        moveFrom = Position;
        Scale = new Vector2(facing, 1);
        TookOff?.Invoke(); // (a rising buzz as he flies in)
    }

    // ---------- What the game asks him to do ----------

    public override void LoseHealth(int amount)
    {
        if (IsBeaten) return;
        Health = Math.Max(0, Health - amount);
        ChangeState(Health <= 0 ? State.Exploding : State.Hurt);
    }

    /// <summary>Bolt-E ran out of batteries: DRONEZILLA zooms up and away, off the top of the screen.</summary>
    public override void FlyAway()
    {
        if (!IsBeaten) ChangeState(State.FlyingAway);
    }

    /// <summary>
    /// Bolt-E crashed but has a spare battery: DRONEZILLA does a happy loop-the-loop ("BZZT BZZT!") and waits
    /// holdSeconds before attacking again, so Bolt-E has time to put himself back together. Bombs already falling
    /// still land, but no new ones come. (Never while he's landed, hurt or blowing up: he never steals your stomp chance!)
    /// </summary>
    public override void Taunt(float holdSeconds)
    {
        if (IsBeaten || state == State.FlyingAway) return;
        holdAttacks = Mathf.Max(holdAttacks, holdSeconds); // he always waits for Bolt-E...
        if (state is State.Entering or State.LowBattery or State.Descending or State.Landed or State.Hurt
                  or State.GettingAngry or State.Taunting) return; // ...but only loops when it can't spoil a landing
        ChangeState(State.Taunting);
    }

    /// <summary>Starts one of his attacks right now, then he lands. (He picks them himself. The self-test uses this to skip ahead.)</summary>
    public void DoAttack(DroneAttack attack) => DoSequence(attack);

    /// <summary>Starts a whole sequence of attacks right now. When they're done his battery runs low and he lands.</summary>
    public void DoSequence(params DroneAttack[] attacks)
    {
        sequence = attacks;
        sequenceStep = 0;
        battery = 1;
        StartAttack(sequence[0]);
    }

    // ---------- His brain ----------

    void StartAttack(DroneAttack attack)
    {
        ChangeState(attack switch
        {
            DroneAttack.Bombs => State.Bombing,
            DroneAttack.Swoop => State.SwoopWarning,
            DroneAttack.Zap => State.Zapping,
            _ => State.DroppingMinis,
        });
    }

    /// <summary>The next attack in his sequence. After the last one: LOW BATTERY! (and he lands)</summary>
    void NextInSequence()
    {
        sequenceStep++;
        if (sequenceStep < sequence.Length) StartAttack(sequence[sequenceStep]);
        else ChangeState(State.LowBattery);
    }

    /// <summary>The next sequence in his plan (or first he gets ANGRY, if he got angrier since we last saw him).</summary>
    void StartNextSequence()
    {
        if (Phase != angerShown)
        {
            angerShown = Phase;
            ChangeState(State.GettingAngry);
            return;
        }
        var plan = Phase switch { 1 => PhaseOnePlan, 2 => AngryPlan, _ => FuriousPlan };
        var attacks = plan[planStep % plan.Length];
        planStep++;
        DoSequence(attacks);
    }

    /// <summary>He's back up in the air: get angry (if he got angrier), or hover a moment before the next attack.</summary>
    void BackInTheAir()
    {
        if (Phase != angerShown)
        {
            angerShown = Phase;
            ChangeState(State.GettingAngry);
        }
        else
        {
            Hover(0.15f);
        }
    }

    void Hover(float seconds)
    {
        hoverWait = seconds;
        ChangeState(State.Hovering);
    }

    /// <summary>Where to land: near where he is, but never closer to Bolt-E than LandAway, and inside the arena.</summary>
    float PickLandingSpot()
    {
        float x = Mathf.Clamp(Position.X, LandLeft, LandRight);
        if (Mathf.Abs(x - TargetX) < LandAway)
        {
            float side = x >= TargetX ? 1 : -1;
            x = TargetX + side * LandAway;
            if (x < LandLeft || x > LandRight) x = TargetX - side * LandAway; // no room there: the other side
        }
        return Mathf.Clamp(x, LandLeft, LandRight);
    }

    void ChangeState(State next)
    {
        state = next;
        stateTime = 0;
        moveFrom = Position;
        switch (next)
        {
            case State.Bombing:
                bombsLeft = Phase == 3 ? BombsWhenFurious : BombsWhenCalm;
                bombTimer = -1; // (the first bomb drops once he's over Bolt-E)
                break;
            case State.SwoopWarning:
                swoopsLeft = Phase == 3 ? 2 : 1;
                swoopFrom = TargetX < ArenaRight / 2 ? SwoopRight : SwoopLeft; // from the side far from Bolt-E
                swoopTo = swoopFrom == SwoopRight ? SwoopLeft : SwoopRight;
                SwoopWarned?.Invoke();
                break;
            case State.Swooping:
                Swooped?.Invoke();
                break;
            case State.Zapping:
                ZapAimed?.Invoke();
                break;
            case State.DroppingMinis:
                minisLeft = Phase == 3 ? MinisWhenFurious : MinisWhenAngry;
                miniTimer = 0.3f;
                break;
            case State.LowBattery:
                battery = 0.08f;
                landX = PickLandingSpot();
                BatteryLow?.Invoke();
                break;
            case State.Landed:
                TouchedDown?.Invoke(Position);
                break;
            case State.TakingOff:
            case State.Hurt:
                TookOff?.Invoke(); // his rotors whir back up (sputtering, when he's hurt)
                if (next == State.Hurt) battery = 0.5f;
                break;
            case State.GettingAngry:
                planStep = 0; // a new phase starts at the top of its plan
                TellGotAngry(Phase);
                break;
            case State.Taunting:
                TellLaughed(); // "BZZT BZZT!"
                break;
            case State.FlyingAway:
                flySpeed = 0;
                break;
            case State.Exploding:
                fallSpeed = -260; // a last little hop... then down he goes
                popTimer = 0;
                crashedDown = false;
                break;
        }
    }

    void FaceBoltE()
    {
        if (Mathf.Abs(TargetX - Position.X) > 8) facing = TargetX < Position.X ? -1 : 1;
    }

    /// <summary>Back up to cruising height (if he's lower).</summary>
    void ClimbToCruise(float dt) => Position = new Vector2(Position.X, Mathf.MoveToward(Position.Y, CruiseY, ClimbSpeed * dt));

    /// <summary>Flies sideways toward Bolt-E (but not off the screen).</summary>
    void FollowBoltE(float speed, float dt)
    {
        float aim = Mathf.Clamp(TargetX, AimLeft, AimRight);
        Position = new Vector2(Mathf.MoveToward(Position.X, aim, speed * dt), Position.Y);
        FaceBoltE();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        time += dt;
        stateTime += dt;
        holdAttacks = Mathf.Max(0, holdAttacks - dt);
        float startX = Position.X;

        switch (state)
        {
            case State.Entering:
                // Flying in from the top-right
                Position = moveFrom.Lerp(new Vector2(EnterSpotX, CruiseY), Smooth(stateTime / EnterSeconds));
                facing = -1;
                if (stateTime >= EnterSeconds) Hover(0.6f);
                break;

            case State.Hovering:
                // Bobbing in the air, waiting for his next attack (or for Bolt-E to be back together)
                FaceBoltE();
                ClimbToCruise(dt);
                if (stateTime >= hoverWait && holdAttacks <= 0 && Position.Y <= CruiseY + 1) StartNextSequence();
                break;

            case State.Bombing:
                // BOMBS AWAY! He flies over Bolt-E and drops his bombs, one after another
                ClimbToCruise(dt);
                if (bombsLeft > 0) FollowBoltE(bombTimer < 0 ? FlyOverSpeed : BombChaseSpeed, dt);
                if (bombTimer < 0)
                {
                    // The first bomb drops once he's right over Bolt-E (or after 2 seconds, if he can't catch up)
                    if (Mathf.Abs(Mathf.Clamp(TargetX, AimLeft, AimRight) - Position.X) < 30 || stateTime > 2f) DropBomb();
                }
                else
                {
                    bombTimer -= dt;
                    if (bombTimer <= 0)
                    {
                        if (bombsLeft > 0) DropBomb();
                        else NextInSequence();
                    }
                }
                break;

            case State.SwoopWarning:
                // "!" He flies to the side far from Bolt-E...
                Position = moveFrom.Lerp(new Vector2(swoopFrom, CruiseY), Smooth(stateTime / SwoopWarnSeconds));
                facing = swoopTo > swoopFrom ? 1 : -1;
                if (stateTime >= SwoopWarnSeconds) ChangeState(State.Swooping);
                break;

            case State.Swooping:
            {
                // ...and SWOOPS down low across the arena, in a big U, and back up at the other side. JUMP!
                float t = Mathf.Min(1, stateTime / (Phase == 3 ? FuriousSwoopSeconds : SwoopSeconds));
                float dip = 1 - Mathf.Sin(Mathf.Pi * t); // 1 = up high, 0 = at the bottom of the U
                float height = SwoopLow + (CruiseHeight - SwoopLow) * dip * dip;
                Position = new Vector2(Mathf.Lerp(swoopFrom, swoopTo, t), GroundY - height);
                facing = swoopTo > swoopFrom ? 1 : -1;
                if (t >= 1)
                {
                    swoopsLeft--;
                    if (swoopsLeft > 0)
                    {
                        (swoopFrom, swoopTo) = (swoopTo, swoopFrom); // FURIOUS: and back again!
                        ChangeState(State.Swooping);
                    }
                    else
                    {
                        NextInSequence();
                    }
                }
                break;
            }

            case State.Zapping:
                // ZAP BEAM! He follows Bolt-E (with the red circle under him), stops (LOCKED!), and ZAPS
                ClimbToCruise(dt);
                if (stateTime < ZapTrackSeconds) FollowBoltE(ZapFollowSpeed, dt);
                if (stateTime >= ZapTrackSeconds + ZapLockSeconds + ZapFireSeconds + 0.25f) NextInSequence();
                break;

            case State.DroppingMinis:
                // MINI-DRONES! Out of his belly, one after another
                ClimbToCruise(dt);
                FaceBoltE();
                miniTimer -= dt;
                if (miniTimer <= 0)
                {
                    if (minisLeft > 0)
                    {
                        MiniDropped?.Invoke(Position + new Vector2(0, -8));
                        minisLeft--;
                        miniTimer = minisLeft > 0 ? MiniEvery : 0.5f;
                    }
                    else
                    {
                        NextInSequence();
                    }
                }
                break;

            case State.LowBattery:
                // LOW BATTERY! Sputtering in the air (his landing spot shows on the ground)
                if (stateTime >= LowBatterySeconds) ChangeState(State.Descending);
                break;

            case State.Descending:
            {
                // Gliding slowly down to his landing spot
                float t = Smooth(stateTime / DescendSeconds);
                Position = new Vector2(Mathf.Lerp(moveFrom.X, landX, t), Mathf.Lerp(moveFrom.Y, GroundY, t));
                if (Mathf.Abs(landX - moveFrom.X) > 2) facing = landX > moveFrom.X ? 1 : -1;
                if (stateTime >= DescendSeconds)
                {
                    Position = new Vector2(landX, GroundY);
                    ChangeState(State.Landed);
                }
                break;
            }

            case State.Landed:
                // On the ground, rotors stopped, charging his battery... STOMP HIM!
                Position = new Vector2(landX, GroundY);
                battery = Mathf.Min(1, stateTime / LandedTime);
                if (stateTime >= LandedTime) ChangeState(State.TakingOff);
                break;

            case State.TakingOff:
                // Fully charged: rotors whir up, and up he goes
                Position = new Vector2(moveFrom.X, Mathf.Lerp(moveFrom.Y, CruiseY, Smooth(stateTime / TakeOffSeconds)));
                if (stateTime >= TakeOffSeconds) BackInTheAir();
                break;

            case State.Hurt:
            {
                // Ouch! His rotors sputter back on and he wobbles up to cruising height (blinking)
                float t = Mathf.Min(1, stateTime / HurtSeconds);
                float wobble = 1 - t;
                Position = new Vector2(moveFrom.X + Mathf.Sin(time * 9) * 10 * wobble,
                                       Mathf.Lerp(moveFrom.Y, CruiseY, Smooth(t)) + Mathf.Sin(time * 15) * 8 * wobble);
                if (stateTime >= HurtSeconds) BackInTheAir();
                break;
            }

            case State.GettingAngry:
                ClimbToCruise(dt);
                FaceBoltE();
                if (stateTime >= AngrySeconds)
                {
                    if (holdAttacks > 0) Hover(0);
                    else StartNextSequence();
                }
                break;

            case State.Taunting:
            {
                // BZZT BZZT! A happy loop-the-loop
                float angle = Mathf.Tau * Smooth(stateTime / TauntSeconds);
                Position = moveFrom + new Vector2(Mathf.Sin(angle) * LoopSize * facing, -(1 - Mathf.Cos(angle)) * LoopSize);
                if (stateTime >= TauntSeconds) Hover(0.3f);
                break;
            }

            case State.FlyingAway:
                // Bolt-E is out of batteries: DRONEZILLA zooms up and away
                flySpeed = Mathf.MoveToward(flySpeed, 900, 1400 * dt);
                Position += new Vector2(facing * 0.5f, -1).Normalized() * flySpeed * dt;
                break;

            case State.Exploding:
                // Sputtering, smoking and spinning down out of the sky... CRASH! ...then the big boom
                if (!crashedDown)
                {
                    fallSpeed += 900 * dt;
                    Position = new Vector2(Position.X - facing * 40 * dt, Mathf.Min(GroundY, Position.Y + fallSpeed * dt));
                    if (Position.Y >= GroundY && fallSpeed > 0)
                    {
                        crashedDown = true;
                        crashTime = stateTime;
                        CrashedDown?.Invoke(Position);
                    }
                }
                popTimer -= dt;
                if (popTimer <= 0)
                {
                    popTimer = 0.17f;
                    var box = Hitbox;
                    TellPopped(box.Position + new Vector2((float)GD.RandRange(0, box.Size.X), (float)GD.RandRange(0, box.Size.Y)));
                }
                if (crashedDown && stateTime >= Mathf.Max(1.2f, crashTime + 0.35f)) BlowUp();
                break;

            case State.Defeated:
                // (His pieces slide along with the ground. He's drawn turned around when he faced left, so the slide is too.)
                explosion?.Update(dt, WorldSpeed * facing);
                break;
        }

        // His battery runs down while he attacks
        if (Attacking) battery = Mathf.MoveToward(battery, 0.25f, 0.12f * dt);

        // He leans into the way he flies (nose down when he goes fast), loops the loop, or spins as he falls
        if (state == State.Taunting)
        {
            tilt = -Mathf.Tau * Smooth(stateTime / TauntSeconds);
        }
        else if (state == State.Exploding && !crashedDown)
        {
            tilt += 8 * dt;
        }
        else if (dt > 0)
        {
            float speedForward = (Position.X - startX) / dt * facing;
            float leanTo = state is State.Landed or State.Descending or State.Defeated ? 0
                         : state == State.Exploding ? 0.3f
                         : Mathf.Clamp(speedForward * TiltPerSpeed, -MaxTilt, MaxTilt);
            tilt = Mathf.Lerp(Mathf.Wrap(tilt, -Mathf.Pi, Mathf.Pi), leanTo, 1 - Mathf.Exp(-8 * dt));
        }

        // The rotors: fast when flying, faster in a dive, sputtering when he's hurt or his battery is low, stopped when landed
        float spinTo = state switch
        {
            State.Landed or State.Defeated => 0f,
            State.Exploding => 20f + 15f * Mathf.Sin(time * 9),
            State.Hurt => 35f + 25f * Mathf.Sin(time * 13),
            State.LowBattery => 38f + 12f * Mathf.Sin(time * 10),
            State.Descending => 32f,
            State.Swooping => 80f,
            _ => 55f,
        };
        rotorSpeed = Mathf.MoveToward(rotorSpeed, spinTo, (spinTo < rotorSpeed ? 30f : 90f) * dt);
        rotorAngle = Mathf.PosMod(rotorAngle + rotorSpeed * dt, Mathf.Tau);

        // His red camera eye looks at Bolt-E
        var toBoltE = new Vector2((TargetX - CameraWorldSpot.X) * facing, GroundY - 60 - CameraWorldSpot.Y);
        camLook = toBoltE.LengthSquared() > 1 ? toBoltE.Normalized() * 4 : Vector2.Zero;

        Scale = new Vector2(facing, 1);

        // Blink while hurt; flash while breaking down; a bit red when FURIOUS
        bool blink = (state == State.Hurt && stateTime < 1f && (int)(stateTime * 12) % 2 == 0)
                     || (state == State.Exploding && (int)(stateTime * 16) % 2 == 0);
        Modulate = blink ? new Color(1, 1, 1, 0.35f) : Phase == 3 && !IsBeaten ? new Color(1f, 0.88f, 0.85f) : Colors.White;
        QueueRedraw();
    }

    void DropBomb()
    {
        BombDropped?.Invoke(Position + new Vector2(0, -10)); // BOMBS AWAY!
        bombsLeft--;
        bombTimer = bombsLeft > 0 ? (Phase == 3 ? FuriousBombEvery : BombEvery) : BombsLandTime;
    }

    void BlowUp()
    {
        state = State.Defeated;
        stateTime = 0;
        tilt = 0;
        rotorSpeed = 0;
        Scale = new Vector2(facing, 1);
        var parts = new List<RobotPart>
        {
            new("skids", new Vector2(0, -12), 10, DrawSkids, FlatEvery: Mathf.Pi),
            new("arm", new Vector2(-72, -82), 14, () => DrawArm(-1), FlatEvery: Mathf.Pi),
            new("arm", new Vector2(72, -82), 14, () => DrawArm(1), FlatEvery: Mathf.Pi),
            new("body", Pivot, 33, DrawBody, FlatEvery: Mathf.Pi),
            new("camera", CameraSpot, 15, DrawCamera, FlatEvery: Mathf.Tau),
            new("blade", new Vector2(-RotorX - BladeLength / 2, RotorY), 5, () => DrawBlade(-1, -1), FlatEvery: Mathf.Pi),
            new("blade", new Vector2(-RotorX + BladeLength / 2, RotorY), 5, () => DrawBlade(-1, 1), FlatEvery: Mathf.Pi),
            new("blade", new Vector2(RotorX - BladeLength / 2, RotorY), 5, () => DrawBlade(1, -1), FlatEvery: Mathf.Pi),
            new("blade", new Vector2(RotorX + BladeLength / 2, RotorY), 5, () => DrawBlade(1, 1), FlatEvery: Mathf.Pi),
        };
        explosion = new RobotExplosion(parts, Pivot, 0, GroundY - Position.Y, DroneYellow,
                                       onBounce: TellPieceBounced, boomAt: new Vector2(0, -50));
        TellBlewUp();
    }

    // ---------- Drawing (everything is drawn facing right, at his real size; Scale turns him around) ----------

    // ---- His colors (try changing these!) ----
    public static readonly Color DroneYellow = new(1f, 0.82f, 0.15f);    // his hazard stripes and "BZZT BZZT!"
    static readonly Color Gunmetal = new(0.36f, 0.39f, 0.45f);           // his body
    static readonly Color DarkMetal = new(0.2f, 0.22f, 0.26f);           // his arms, motors and skids
    static readonly Color Silver = new(0.8f, 0.82f, 0.88f);
    static readonly Color HazardBlack = new(0.1f, 0.1f, 0.12f);
    static readonly Color Visor = new(0.06f, 0.08f, 0.14f);              // his face visor and camera lens
    static readonly Color Blades = new(0.88f, 0.9f, 0.95f);
    static readonly Color CameraRed = new(1f, 0.15f, 0.12f);
    static readonly Color EyeGreen = new(0.45f, 1f, 0.55f);               // his LED eyes: green...
    static readonly Color AngryOrange = new(1f, 0.6f, 0.15f);             // ...orange when he's ANGRY...
    static readonly Color FuriousRed = new(1f, 0.25f, 0.2f);              // ...and red when he's FURIOUS
    static readonly Color NavRed = new(1f, 0.2f, 0.2f), NavGreen = new(0.3f, 1f, 0.4f);
    static readonly Color Smoke = new(0.25f, 0.25f, 0.28f);
    static readonly Color StarYellow = new(1f, 0.88f, 0.25f);

    // ---- Where his parts are ----
    static readonly Vector2 Pivot = new(0, -59);          // the middle of his body (he tilts around it)
    static readonly Vector2 CameraSpot = new(40, -17);    // his big round camera eye, under his front
    static readonly Vector2 BackEye = new(28, -72), FrontEye = new(48, -72);
    const float RotorX = 96f, RotorY = -104f, BladeLength = 46f;

    // ---- Shapes made once (not every time he's drawn) ----
    static readonly Vector2[][] Stripes = MakeStripes();
    static readonly Vector2[] BackEyeLid = MakeEyelid(BackEye, innerOnRight: true), FrontEyeLid = MakeEyelid(FrontEye, innerOnRight: false);
    readonly Vector2[] arrow = new Vector2[3]; // the bouncing "stomp here!" arrow (its corners change, the list doesn't)

    /// <summary>The black stripes of his yellow-and-black hazard band: slanted 4-cornered shapes.</summary>
    static Vector2[][] MakeStripes()
    {
        var stripes = new Vector2[8][];
        for (int i = 0; i < stripes.Length; i++)
        {
            float x = -58 + i * 15;
            stripes[i] = new[] { new Vector2(x, -46), new Vector2(x + 7, -46), new Vector2(x + 13, -58), new Vector2(x + 6, -58) };
        }
        return stripes;
    }

    /// <summary>A sneaky eyelid over the top of an LED eye: lower on the inside, so he looks mischievous.</summary>
    static Vector2[] MakeEyelid(Vector2 eye, bool innerOnRight) => new[]
    {
        eye + new Vector2(-8, -8), eye + new Vector2(8, -8),
        eye + new Vector2(8, innerOnRight ? -1 : -5), eye + new Vector2(-8, innerOnRight ? -5 : -1),
    };

    public override void _Draw()
    {
        drawnIn[(int)state] = true;
        WarningDrawn = LandingSpotDrawn = false;

        if (explosion is not null)
        {
            explosion.Draw(this);
            return;
        }

        DrawGroundShadow();
        if (state is State.LowBattery or State.Descending) DrawLandingSpot();

        // Bobbing gently in the air (sputtering when his battery is low, shaking when he's angry), leaning as he flies
        var wiggle = Vector2.Zero;
        if (state is not (State.Landed or State.Exploding or State.Swooping or State.Taunting or State.Descending))
            wiggle.Y = Mathf.Sin(time * 3) * 5;
        if (state == State.LowBattery) wiggle += new Vector2(Mathf.Sin(time * 40) * 2, Mathf.Sin(time * 23) * 3);
        if (state == State.GettingAngry) wiggle.X += Mathf.Sin(time * 70) * 4;
        DrawSetTransform(wiggle + Pivot - Pivot.Rotated(tilt), tilt, Vector2.One);
        DrawSkids();
        DrawArm(-1);
        DrawArm(1);
        DrawRotor(-1);
        DrawRotor(1);
        DrawCamera();
        DrawBody();
        DrawNavLights();
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);

        if (state == State.SwoopWarning) DrawWarning();
        if (state == State.Landed) DrawStompArrow();
        if (state == State.Hurt) DrawDizzyStars();
        if (state == State.Exploding) DrawSmoke();
        DrawMood();
    }

    /// <summary>His dark shadow on the ground: it gets bigger and darker as he comes lower (so you can see where he is).</summary>
    void DrawGroundShadow()
    {
        float height = GroundY - Position.Y;
        float near = Mathf.Clamp(1f - height / 520f, 0.2f, 1f); // 1 = on the ground
        Shapes.Ellipse(this, new Vector2(0, height + 2), new Vector2(20 + 100 * near, 4 + 9 * near), new Color(0, 0, 0, 0.1f + 0.22f * near));
    }

    /// <summary>Where he's going to land: a blinking yellow ring on the ground.</summary>
    void DrawLandingSpot()
    {
        var spot = new Vector2((landX - Position.X) * facing, GroundY - Position.Y);
        float blink = (int)(time * 6) % 2 == 0 ? 0.85f : 0.4f;
        DrawSetTransform(spot, 0, new Vector2(1, 0.22f));
        DrawCircle(Vector2.Zero, 80, new Color(0, 0, 0, 0.15f));
        DrawArc(Vector2.Zero, 80, 0, Mathf.Tau, 36, new Color(DroneYellow, blink), 7, true);
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        LandingSpotDrawn = true;
    }

    /// <summary>His two landing skids (the far one a bit darker and higher, because it's behind).</summary>
    void DrawSkids()
    {
        DrawSkid(new Vector2(-8, -4), DarkMetal.Darkened(0.3f));
        DrawSkid(Vector2.Zero, DarkMetal);
    }

    void DrawSkid(Vector2 shift, Color color)
    {
        DrawLine(shift + new Vector2(-28, -28), shift + new Vector2(-36, -5), color, 5, true);
        DrawLine(shift + new Vector2(28, -28), shift + new Vector2(36, -5), color, 5, true);
        Shapes.RoundRect(this, new Rect2(shift + new Vector2(-56, -7), new Vector2(112, 7)), 3.5f, color);
        DrawCircle(shift + new Vector2(56, -9), 4.5f, color); // the curly front tip
    }

    /// <summary>One arm out to the side (-1 = back, 1 = front), with its motor on the end.</summary>
    void DrawArm(float side)
    {
        float x = side * RotorX;
        DrawLine(new Vector2(side * 44, -70), new Vector2(x, -84), DarkMetal, 11, true);
        Shapes.RoundRect(this, new Rect2(x - 12, -96, 24, 17), 6, DarkMetal);
        DrawLine(new Vector2(x - 8, -93), new Vector2(x + 8, -93), new Color(1, 1, 1, 0.25f), 2, true);
        DrawLine(new Vector2(x, -96), new Vector2(x, RotorY), DarkMetal.Darkened(0.2f), 4, true); // the rotor's mast
    }

    /// <summary>
    /// A spinning rotor, seen from the side: a see-through blur when it's fast, and a blade that turns (it looks longer
    /// and shorter as it goes round). When the rotor stops, it's just the blade.
    /// </summary>
    void DrawRotor(float side)
    {
        var hub = new Vector2(side * RotorX, RotorY);
        float blur = Mathf.Clamp((rotorSpeed - 8f) / 45f, 0, 1);
        if (blur > 0.02f) Shapes.Ellipse(this, hub, new Vector2(BladeLength + 2, 6), new Color(Blades, 0.35f * blur));
        float turn = rotorAngle + (side > 0 ? 0.9f : 0f);
        float reach = Mathf.Cos(turn) * BladeLength;
        if (Mathf.Abs(reach) < 3) reach = 3;
        var lift = new Vector2(reach, Mathf.Sin(turn) * 2.5f);
        DrawLine(hub - lift, hub + lift, new Color(Blades, 1f - 0.55f * blur), 5, true);
        DrawCircle(hub, 5, Silver);
    }

    /// <summary>One rotor blade, lying still (for when he blows up): -1 = the back rotor, 1 = the front one.</summary>
    void DrawBlade(float side, float direction)
    {
        var hub = new Vector2(side * RotorX, RotorY);
        DrawLine(hub, hub + new Vector2(direction * BladeLength, 0), Blades, 5, true);
        DrawCircle(hub, 4, Silver);
    }

    /// <summary>His big round camera eye under his front. Its red middle looks at Bolt-E!</summary>
    void DrawCamera()
    {
        DrawLine(new Vector2(CameraSpot.X, -30), CameraSpot, DarkMetal, 7, true);
        DrawCircle(CameraSpot, 15, DarkMetal);
        DrawCircle(CameraSpot, 15, Silver.Darkened(0.3f), false, 2, true);
        var lens = CameraSpot + new Vector2(3, 0);
        DrawCircle(lens, 10.5f, Visor);
        var middle = lens + (IsBeaten ? Vector2.Zero : camLook);
        bool on = state is not (State.Landed or State.Defeated) || (int)(time * 3) % 2 == 0;
        if (on)
        {
            DrawCircle(middle, 7.5f, new Color(CameraRed, 0.3f)); // the glow
            DrawCircle(middle, 4.5f, CameraRed);
        }
        DrawCircle(lens + new Vector2(-3, -4), 2.2f, new Color(1, 1, 1, 0.8f)); // a sparkle
    }

    /// <summary>His chunky round body: gunmetal, with a yellow-and-black hazard band, his battery, visor and antenna.</summary>
    void DrawBody()
    {
        Shapes.RoundRect(this, new Rect2(-HalfWidth, -92, HalfWidth * 2, 66), 22, Gunmetal);
        Shapes.RoundRect(this, new Rect2(-HalfWidth + 6, -40, HalfWidth * 2 - 12, 13), 6, Gunmetal.Darkened(0.3f)); // his belly
        DrawRect(new Rect2(-14, -31, 28, 4), HazardBlack);                                                      // the bomb hatch
        Shapes.Ellipse(this, new Vector2(-12, -85), new Vector2(38, 4), new Color(1, 1, 1, 0.18f));               // a shine

        // The hazard band: yellow with black stripes
        DrawRect(new Rect2(-60, -58, 120, 12), DroneYellow);
        foreach (var stripe in Stripes) DrawColoredPolygon(stripe, HazardBlack);

        DrawBatteryIcon();

        // The antenna (its light blinks red when his battery is low)
        DrawLine(new Vector2(-34, -91), new Vector2(-44, -116), DarkMetal, 3, true);
        bool lowBattery = state is State.LowBattery or State.Descending or State.Landed;
        bool lightOn = (int)(time * (lowBattery ? 6 : 2)) % 2 == 0;
        DrawCircle(new Vector2(-44, -118), 5, lightOn ? (lowBattery ? NavRed : DroneYellow) : DarkMetal);

        // The face visor, and his two LED eyes
        Shapes.RoundRect(this, new Rect2(16, -85, 46, 26), 9, Visor);
        DrawLine(new Vector2(22, -82), new Vector2(54, -82), new Color(1, 1, 1, 0.25f), 2, true);
        DrawEye(BackEye, BackEyeLid);
        DrawEye(FrontEye, FrontEyeLid);
    }

    /// <summary>The battery on his side: green when it's full, then yellow, then blinking red. It charges up when he lands.</summary>
    void DrawBatteryIcon()
    {
        DrawRect(new Rect2(-54, -84, 24, 12), HazardBlack);
        DrawRect(new Rect2(-30, -81, 3, 6), HazardBlack);
        bool low = battery < 0.3f;
        if (low && (int)(time * 6) % 2 == 1) return; // (blink!)
        var color = battery > 0.6f ? NavGreen : battery >= 0.3f ? DroneYellow : NavRed;
        DrawRect(new Rect2(-52, -82, 20 * Mathf.Clamp(battery, 0.1f, 1f), 8), color);
    }

    /// <summary>
    /// One LED eye: round and glowing, mischievous when he attacks, sleepy when his battery is low, spinning spirals when
    /// he's knocked dizzy, happy when he loops the loop, and X eyes when he's beaten. Green, then orange (ANGRY), then red (FURIOUS).
    /// </summary>
    void DrawEye(Vector2 eye, Vector2[] lid)
    {
        var color = Phase switch { 1 => EyeGreen, 2 => AngryOrange, _ => FuriousRed };
        switch (state)
        {
            case State.Exploding or State.Defeated:
                Shapes.XEye(this, eye, 4, color);
                break;
            case State.Hurt:
                for (int ring = 0; ring < 2; ring++)
                    DrawArc(eye, 2f + ring * 2.8f, time * 9 + ring, time * 9 + ring + Mathf.Pi * 1.4f, 10, color, 1.8f, true);
                break;
            case State.LowBattery or State.Descending or State.Landed:
                // Sleepy, half-shut eyes: his battery is flat
                DrawArc(eye, 4.5f, 0.15f, Mathf.Pi - 0.15f, 8, color.Darkened(0.25f), 3, true);
                DrawLine(eye + new Vector2(-5.5f, 0), eye + new Vector2(5.5f, 0), color.Darkened(0.25f), 2.5f, true);
                break;
            case State.Taunting:
                Shapes.HappyEye(this, eye + new Vector2(0, 1), 5, color, 3);
                break;
            default:
                DrawCircle(eye, 7.5f, new Color(color, 0.25f)); // the glow
                DrawCircle(eye, 5, color);
                DrawCircle(eye + new Vector2(-1.5f, -1.8f), 1.5f, new Color(1, 1, 1, 0.85f)); // a sparkle
                if (Attacking || state == State.GettingAngry) DrawColoredPolygon(lid, Visor); // mischievous!
                break;
        }
    }

    /// <summary>Blinking nav lights at the ends of his arms: red at the back, green at the front.</summary>
    void DrawNavLights()
    {
        if (state is State.Landed) return; // (switched off to save power)
        bool backOn = (int)(time * 2.5f) % 2 == 0;
        var back = new Vector2(-RotorX - 14, -88);
        var front = new Vector2(RotorX + 14, -88);
        DrawCircle(back, backOn ? 8 : 4, new Color(NavRed, backOn ? 0.35f : 0.15f));
        DrawCircle(back, 3.5f, backOn ? NavRed : NavRed.Darkened(0.6f));
        DrawCircle(front, !backOn ? 8 : 4, new Color(NavGreen, !backOn ? 0.35f : 0.15f));
        DrawCircle(front, 3.5f, !backOn ? NavGreen : NavGreen.Darkened(0.6f));
    }

    /// <summary>A big red "!" over him before a swoop: get ready to jump!</summary>
    void DrawWarning()
    {
        if ((int)(stateTime * 8) % 2 == 1) return; // it flashes
        var font = ThemeDB.FallbackFont;
        var size = font.GetStringSize("!", HorizontalAlignment.Left, -1, 60);
        var at = new Vector2(-size.X / 2, -135);
        DrawStringOutline(font, at, "!", HorizontalAlignment.Left, -1, 60, 10, Colors.White);
        DrawString(font, at, "!", HorizontalAlignment.Left, -1, 60, new Color(0.95f, 0.15f, 0.15f));
        WarningDrawn = true;
    }

    /// <summary>A bouncing yellow arrow over him while he's landed: STOMP HERE!</summary>
    void DrawStompArrow()
    {
        float y = -150 - Mathf.Abs(Mathf.Sin(time * 6)) * 14;
        arrow[0] = new Vector2(-16, y);
        arrow[1] = new Vector2(16, y);
        arrow[2] = new Vector2(0, y + 20);
        DrawColoredPolygon(arrow, DroneYellow);
        DrawRect(new Rect2(-6, y - 16, 12, 17), DroneYellow);
    }

    void DrawDizzyStars()
    {
        for (int i = 0; i < 4; i++)
        {
            float angle = time * 5f + i * Mathf.Tau / 4f;
            Shapes.Star(this, new Vector2(Mathf.Cos(angle) * 70, -130 + Mathf.Sin(angle) * 12), 8, time * 4, StarYellow);
        }
    }

    /// <summary>Black smoke puffing out of him while he breaks down.</summary>
    void DrawSmoke()
    {
        for (int i = 0; i < 6; i++)
        {
            float age = (time * 1.4f + i / 6f) % 1f; // 0 = brand new, 1 = gone
            var at = new Vector2(-20 + Mathf.Sin(i * 2.1f) * 25 - age * 40, -80 - age * 90);
            DrawCircle(at, 7 + age * 18, new Color(Smoke, 0.6f * (1 - age)));
        }
    }

    void DrawMood()
    {
        if (IsBeaten || Phase < 3 || state is State.Landed or State.FlyingAway) return;
        // FURIOUS: crackly sparks all over him
        for (int i = 0; i < 4; i++)
        {
            if (((int)(time * 12) + i) % 3 == 0) continue; // (they flicker)
            var spot = new Vector2(-50 + i * 33, -96 + (i % 2) * 70);
            var line = Vector2.Right.Rotated(time * 17 + i * 2) * 7;
            DrawLine(spot - line, spot + line, new Color(1f, 0.95f, 0.4f), 2, true);
        }
    }
}
