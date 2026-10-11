using Godot;

/// <summary>OPTI-MAX's attacks. He does one or two of them, then he stops for a SOFTWARE UPDATE (stomp him!).</summary>
public enum OptiAttack
{
    Boxes,  // BOX TOSS! He picks up boxes from his stack and throws them in a high arc at Bolt-E (a shadow shows where)
    Stomp,  // SUPER STOMP! He crouches, jumps up high and lands with a STOMP: two teal shockwaves run along the ground (jump!)
    Kick,   // SOCCER KICK! He drops a ball and kicks it: it bounces toward Bolt-E (jump over it, or ride under a high bounce)
    Run,    // ROBOT RUN! He sprints across the arena (but he always stops before he gets to Bolt-E)
}

/// <summary>
/// OPTI-MAX, the humanoid robot! The fourth boss: he takes turns with BIG RUSTY, VROOMBA and DRONEZILLA (see
/// Main.Vroomba.cs). He's a tall, slim, white-and-black factory helper robot with a glossy black head and a glowing
/// teal light strip around his face. He walks and moves like a person.
/// He has 6 health and gets angrier as he gets hurt:
///   Phase 1:            BOX TOSS (2 boxes) and SUPER STOMP, taking turns
///   Phase 2 (ANGRY):    he also does the SOCCER KICK and the ROBOT RUN, and throws 3 boxes
///   Phase 3 (FURIOUS):  faster, 4 boxes, 2 balls, and TWO super stomps in a row
/// After every attack he freezes: "UPDATING..." He kneels down on one knee while a progress bar fills up... JUMP ON HIM!
/// What every boss has (health, phases, stomping, flying away...) is in BossBase.cs. What the game does when he throws
/// a box (and so on) is in Main.OptiMax.cs. His boxes are ThrownBox.cs, his ball is KickBall.cs, his shockwaves are
/// StompWave.cs and his box stacks are BoxStack.cs.
///
/// How he's drawn: a little SKELETON of angles (see Pose). Each leg has a thigh angle and a knee bend, each arm has an
/// arm angle and an elbow bend, and his body can lean forward. Every move just picks those angles, and the drawing
/// puts white plates and black joints on the bones. Position is the spot on the ground between his feet. He's drawn
/// facing right; Scale turns him around.
/// </summary>
public partial class OptiMax : BossBase
{
    public const string OptiMaxName = "OPTI-MAX";
    public override string DisplayName => OptiMaxName;

    // ---- OPTI-MAX's numbers (try changing these!) ----
    public const int BoxesWhenCalm = 2;            // boxes in one BOX TOSS...
    public const int BoxesWhenAngry = 3;           // ...when he's ANGRY...
    public const int BoxesWhenFurious = 4;         // ...and when he's FURIOUS
    public const float BoxFlightSeconds = 1.05f;   // how long a box flies through the air (time to ride away!)...
    public const float FuriousBoxFlightSeconds = 0.85f; // ...and when he's FURIOUS
    public const float StompJumpHeight = 170f;     // how high he jumps for a SUPER STOMP
    public const int BallsWhenAngry = 1;           // soccer balls he kicks when he's ANGRY...
    public const int BallsWhenFurious = 2;         // ...and when he's FURIOUS
    public const float WalkSpeed = 320f;           // how fast he walks (Bolt-E rides 430)
    public const float RunSpeed = 620f;            // how fast he sprints in a ROBOT RUN
    public const float RunStopShort = 220f;        // a run always stops at least this far from Bolt-E (he never squashes him on a wall)
    public const float UpdateSeconds = 2.6f;       // how long his SOFTWARE UPDATE takes (STOMP HIM!)...
    public const float FuriousUpdateSeconds = 2.0f;// ...and when he's FURIOUS
    public const float ShortestUpdate = 1.8f;      // an update never takes less than this, so there's always time to stomp him
    public const float KneelAway = 260f;           // he kneels at least this far from Bolt-E (he never kneels on him!)
    public const float FuriousSpeedUp = 0.8f;      // when he's FURIOUS his moves take only 80% of the time

    // ---- His spots in the arena ----
    public const float StackLeftX = 95f, StackRightX = 1185f;  // his two little stacks of boxes, at the ends of the arena
    public const float StackReach = 105f;                       // he stands this far from a stack to pick up its top box
    public const float KneelLeft = 150f, KneelRight = 1130f;    // where he can kneel
    const float ArenaMiddle = 640f, ArenaRight = 1280f;
    const float EnterSpotX = StackRightX - StackReach;          // where he stops after walking in (next to his stack)
    const float ShortestRun = 140f;                             // he doesn't bother running less than this

    // ---- How long things take (seconds) ----
    const float EnterSpeed = 210f;       // walking in, slowly and heavily: clank, clank...
    const float HelloSeconds = 1.1f;     // his little wave hello
    const float PickSeconds = 0.42f;     // bending down to pick up a box
    const float LiftSeconds = 0.35f;     // lifting it over his head
    const float ThrowSeconds = 0.32f;    // and THROW!
    const float CrouchSeconds = 0.5f;    // crouching down for a super stomp
    const float AirSeconds = 0.8f;       // up in the air
    const float LandSeconds = 0.45f;     // landing (STOMP!) and getting back up
    const float HoldBallSeconds = 0.55f; // holding the ball, then dropping it
    const float KickSeconds = 0.42f;     // the kick
    const float BrakeSeconds = 0.3f;     // stopping after a run
    const float FreezeSeconds = 0.5f;    // frozen: "UPDATING..."
    const float KneelSeconds = 0.45f;    // kneeling down
    const float StandUpSeconds = 0.55f;  // standing back up after an update
    const float HurtSeconds = 1.4f;      // ERROR! sparks, then wobbling back up
    const float AngrySeconds = 1.3f;     // shaking his fists when he gets ANGRY or FURIOUS
    const float DanceSeconds = 1.6f;     // BEEP BOP DANCE!
    const float ByeSeconds = 0.9f;       // waving goodbye before he jogs off
    const float FlyAwaySpeed = 380f;     // (jogging off, really)
    const float ChirpEvery = 0.32f;      // computer chirps while he updates

    /// <summary>
    /// His attack plan for each phase: a list of SEQUENCES. He does every attack in one sequence, then he stops for a
    /// SOFTWARE UPDATE (stomp him!). Then the next sequence. After the last one, he starts over.
    /// </summary>
    static readonly OptiAttack[][] PhaseOnePlan = { new[] { OptiAttack.Boxes }, new[] { OptiAttack.Stomp } };
    static readonly OptiAttack[][] AngryPlan =
    {
        new[] { OptiAttack.Kick, OptiAttack.Boxes }, new[] { OptiAttack.Run, OptiAttack.Stomp },
    };
    static readonly OptiAttack[][] FuriousPlan =
    {
        new[] { OptiAttack.Kick, OptiAttack.Boxes }, new[] { OptiAttack.Run, OptiAttack.Stomp },
    };

    enum State
    {
        Entering, Hello, Idle, Walking, PickingUp, Lifting, Throwing, Crouching, Jumping, Landing, HoldingBall, Kicking,
        Running, Braking, Positioning, Freezing, Kneeling, Updating, StandingUp, Hurt, GettingAngry, Dancing, FlyingAway,
        Exploding, Defeated,
    }

    // ---- Things he does that the rest of the game needs to know about ----
    // (GotAngry, Laughed = "BEEP BOP DANCE!", Popped, BlewUp and PieceBounced are in BossBase.cs: every boss has them)
    public event Action<Vector2>? Footstep;                 // clank! a foot came down, here
    public event Action<float>? BoxPickedUp;                // he took the top box off the stack at this x
    public event Action<Vector2, float, float>? BoxThrown;  // a box flies from here, to land at this x, after this many seconds
    public event Action? Jumped;                             // up he goes for a SUPER STOMP
    public event Action<float>? Stomped;                    // STOMP! he landed at this x (two shockwaves!)
    public event Action<Vector2, float>? BallKicked;        // a ball is kicked from here, this way (-1 = left, 1 = right)
    public event Action<Vector2>? UpdateStarted;            // "UPDATING..." (his head is here)
    public event Action? Chirped;                           // a little computer chirp while he updates
    public event Action<Vector2>? UpdateDone;               // DING! the update finished (his head is here)
    public event Action<Vector2>? Errored;                  // ERROR! he was stomped while updating (sparks from his head, here)
    public event Action<Vector2>? FellOver;                 // beaten: he fell over backward, CLANK! (here)

    State state = State.Entering;
    float stateTime, time;
    float facing = -1;          // 1 = facing right, -1 = facing left
    int planStep;               // which sequence in his plan comes next
    OptiAttack[] sequence = PhaseOnePlan[0];
    int sequenceStep;           // which attack in the sequence he's doing
    int angerShown = 1;         // the phase he last showed us
    float holdAttacks;          // after Bolt-E crashes, he waits this many seconds before attacking again (fair play!)
    float idleWait;             // how long he stands still before his next attack
    int boxesLeft, stompsLeft, ballsLeft;
    bool holdingBox, holdingBall, ballDropped;
    float stackX;               // the stack he's getting boxes from
    float walkGoal;             // where he's walking to
    float runGoal, runSpeedNow; // where his ROBOT RUN stops, and how fast he's going
    float progress;             // his update: 0 = just started, 1 = DING!
    float chirpTimer, popTimer, fallTime = -1;
    float stride;               // how far his walk cycle has gone (each step is half a turn)
    int lastStep;
    float jitter;               // shaking (angry, ERROR!, beaten)
    RobotExplosion? explosion;

    /// <summary>1 = facing right, -1 = facing left.</summary>
    public float Facing => facing;
    /// <summary>His update: 0 = just started, 1 = done (the progress bar over his head).</summary>
    public float UpdateProgress => progress;
    /// <summary>True from the moment he freezes ("UPDATING...") until he stands back up.</summary>
    public bool IsUpdating => state is State.Freezing or State.Kneeling or State.Updating;
    /// <summary>How high the top of his head is above the ground, in pixels (the self-test reads this).</summary>
    public float HeadTopHeight => GroundY - (Position.Y + headTopY);
    /// <summary>Where his ROBOT RUN will stop.</summary>
    public float RunGoal => runGoal;
    /// <summary>True while he does an attack (his eyes narrow).</summary>
    public bool Attacking => state is State.Walking or State.PickingUp or State.Lifting or State.Throwing or State.Crouching
                                   or State.Jumping or State.Landing or State.HoldingBall or State.Kicking or State.Running
                                   or State.Braking;
    /// <summary>The words on his progress bar in the last frame: "UPDATING...", "ERROR!" or "" (the self-test reads this).</summary>
    public string BarLabel { get; private set; } = "";
    /// <summary>True when this piece (like "head" or "hand") flew off when he blew up (the self-test reads this).</summary>
    public bool HasPiece(string name) => explosion?.HasPart(name) == true;

    readonly bool[] drawnIn = new bool[Enum.GetValues<State>().Length];
    /// <summary>Every move he was drawn in so far (the self-test reads this).</summary>
    public IEnumerable<string> MovesDrawn => Enum.GetValues<State>().Where(move => drawnIn[(int)move]).Select(move => move.ToString());
    /// <summary>All of his moves (the self-test checks that every one of them draws).</summary>
    public static IEnumerable<string> EveryMove => Enum.GetNames<State>();

    /// <summary>He can only be stomped while he's UPDATING (kneeling, with the progress bar over his head).</summary>
    public override bool CanBeStomped => state == State.Updating;
    /// <summary>
    /// Bumping into him is a crash while he walks, runs, throws, stomps, kicks, gets angry or dances. While he walks in,
    /// waves, stands still for a moment, UPDATES, is hurt, or leaves, he never hurts Bolt-E.
    /// </summary>
    public override bool IsDangerous => Attacking || state is State.Positioning or State.GettingAngry or State.Dancing;
    public override bool IsBeaten => state is State.Exploding or State.Defeated;
    public override bool HasBlownUp => state == State.Defeated;
    public override bool Finished => (state == State.Defeated && stateTime > 3.5f)
                                     || (state == State.FlyingAway && (Position.X < -140 || Position.X > ArenaRight + 140));
    public override string CurrentMove => state.ToString();

    public override Rect2 Hitbox
    {
        get
        {
            float left = facing > 0 ? hitLeft : -hitRight;
            float right = facing > 0 ? hitRight : -hitLeft;
            return new Rect2(Position + new Vector2(left, hitTop), new Vector2(right - left, hitBottom - hitTop));
        }
    }

    float UpdateTime => Mathf.Max(ShortestUpdate, Phase == 3 ? FuriousUpdateSeconds : UpdateSeconds);
    float Quick => Phase == 3 ? FuriousSpeedUp : 1f; // FURIOUS: everything goes faster

    public override void _Ready()
    {
        Position = new Vector2(ArenaRight + 120, GroundY); // just off the screen, on the right
        Scale = new Vector2(facing, 1);
        pose = StandPose;
        Build(pose, ref skeleton);
        Build(StandPose, ref rest);
        UpdateHitbox();
    }

    // ---------- What the game asks him to do ----------

    public override void LoseHealth(int amount)
    {
        if (IsBeaten) return;
        Health = Math.Max(0, Health - amount);
        ChangeState(Health <= 0 ? State.Exploding : State.Hurt);
    }

    /// <summary>Bolt-E ran out of batteries: OPTI-MAX waves goodbye and jogs off the screen.</summary>
    public override void FlyAway()
    {
        if (!IsBeaten && state != State.FlyingAway) ChangeState(State.FlyingAway);
    }

    /// <summary>
    /// Bolt-E crashed but has a spare battery: OPTI-MAX does a funny robot dance ("BEEP BOP DANCE!") and waits
    /// holdSeconds before attacking again, so Bolt-E has time to put himself back together. Boxes and balls already
    /// flying still finish, but no new ones come. (Never while he updates, is hurt or blows up: he never steals your
    /// stomp chance!)
    /// </summary>
    public override void Taunt(float holdSeconds)
    {
        if (IsBeaten || state == State.FlyingAway) return;
        holdAttacks = Mathf.Max(holdAttacks, holdSeconds); // he always waits for Bolt-E...
        if (state is State.Entering or State.Hello or State.Positioning or State.Freezing or State.Kneeling or State.Updating
                  or State.StandingUp or State.Hurt or State.GettingAngry or State.Dancing) return; // ...but only dances when it can't spoil an update
        holdingBox = holdingBall = false;
        ChangeState(State.Dancing);
    }

    /// <summary>Starts one of his attacks right now, then he updates. (He picks them himself. The self-test uses this to skip ahead.)</summary>
    public void DoAttack(OptiAttack attack) => DoSequence(attack);

    /// <summary>Starts a whole sequence of attacks right now. When they're done he stops for a SOFTWARE UPDATE.</summary>
    public void DoSequence(params OptiAttack[] attacks)
    {
        sequence = attacks;
        sequenceStep = 0;
        StartAttack(sequence[0]);
    }

    // ---------- His brain ----------

    void StartAttack(OptiAttack attack)
    {
        holdingBox = holdingBall = false;
        switch (attack)
        {
            case OptiAttack.Boxes:
                boxesLeft = Phase switch { 1 => BoxesWhenCalm, 2 => BoxesWhenAngry, _ => BoxesWhenFurious };
                GoToStack();
                break;
            case OptiAttack.Stomp:
                stompsLeft = Phase == 3 ? 2 : 1;
                ChangeState(State.Crouching);
                break;
            case OptiAttack.Kick:
                ballsLeft = Phase == 3 ? BallsWhenFurious : BallsWhenAngry;
                ChangeState(State.HoldingBall);
                break;
            default:
                if (PlanRun()) ChangeState(State.Running);
                else NextInSequence(); // (no room to run: on to the next thing)
                break;
        }
    }

    /// <summary>The next attack in his sequence. After the last one: "UPDATING..." (and he kneels down)</summary>
    void NextInSequence()
    {
        sequenceStep++;
        if (sequenceStep < sequence.Length) StartAttack(sequence[sequenceStep]);
        else StartUpdate();
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

    /// <summary>After an update (or an ERROR!): get angry (if he got angrier), or stand still a moment before the next attack.</summary>
    void BackToWork()
    {
        if (Phase != angerShown)
        {
            angerShown = Phase;
            ChangeState(State.GettingAngry);
        }
        else
        {
            Rest(0.35f);
        }
    }

    void Rest(float seconds)
    {
        idleWait = seconds;
        ChangeState(State.Idle);
    }

    /// <summary>
    /// Time for a SOFTWARE UPDATE: first he walks a bit if Bolt-E is too close (he never kneels on him!), then he
    /// freezes and kneels.
    /// </summary>
    void StartUpdate()
    {
        float x = Mathf.Clamp(Position.X, KneelLeft, KneelRight);
        if (Mathf.Abs(x - TargetX) < KneelAway)
        {
            float side = x >= TargetX ? 1 : -1; // (away from Bolt-E)
            x = Mathf.Clamp(TargetX + side * KneelAway, KneelLeft, KneelRight);
        }
        if (Mathf.Abs(x - Position.X) > 4)
        {
            walkGoal = x;
            ChangeState(State.Positioning);
        }
        else
        {
            ChangeState(State.Freezing);
        }
    }

    /// <summary>Walks to the stack of boxes on his side (never past Bolt-E: then he uses the other stack).</summary>
    void GoToStack()
    {
        stackX = Position.X >= ArenaMiddle ? StackRightX : StackLeftX;
        float spot = StandSpot(stackX);
        bool boltEInTheWay = TargetX > Mathf.Min(Position.X, spot) - 40 && TargetX < Mathf.Max(Position.X, spot) + 40
                             && Mathf.Abs(spot - Position.X) > 40;
        if (boltEInTheWay)
        {
            stackX = stackX == StackRightX ? StackLeftX : StackRightX;
            spot = StandSpot(stackX);
        }
        walkGoal = spot;
        if (Mathf.Abs(spot - Position.X) > 4) ChangeState(State.Walking);
        else ChangeState(State.PickingUp);
    }

    /// <summary>Where he stands to pick up a box: on the arena side of the stack.</summary>
    static float StandSpot(float stack) => stack > ArenaMiddle ? stack - StackReach : stack + StackReach;

    /// <summary>
    /// Plans a ROBOT RUN to the other side of the arena. If Bolt-E is ahead of him, he stops RunStopShort before Bolt-E
    /// (so he never squashes Bolt-E against a wall). Returns false if there's no room for a real run.
    /// </summary>
    bool PlanRun()
    {
        float goal = StandSpot(Position.X >= ArenaMiddle ? StackLeftX : StackRightX);
        float direction = Mathf.Sign(goal - Position.X);
        if ((TargetX - Position.X) * direction > 0) // Bolt-E is ahead of him
            goal = direction > 0 ? Mathf.Min(goal, TargetX - RunStopShort) : Mathf.Max(goal, TargetX + RunStopShort);
        runGoal = goal;
        runSpeedNow = 0;
        if ((goal - Position.X) * direction < ShortestRun) return false;
        facing = direction;
        return true;
    }

    void ChangeState(State next)
    {
        state = next;
        stateTime = 0;
        switch (next)
        {
            case State.Walking or State.Positioning:
                if (Mathf.Abs(walkGoal - Position.X) > 1) facing = Mathf.Sign(walkGoal - Position.X);
                break;
            case State.PickingUp:
                facing = stackX > Position.X ? 1 : -1; // (he turns to the stack)
                holdingBox = false;
                break;
            case State.Lifting:
            case State.HoldingBall:
                FaceBoltE();
                holdingBall = next == State.HoldingBall;
                ballDropped = false;
                break;
            case State.Jumping:
                Jumped?.Invoke();
                break;
            case State.Freezing:
                holdingBox = holdingBall = false;
                progress = 0;
                UpdateStarted?.Invoke(HeadSpot);
                chirpTimer = 0;
                break;
            case State.Updating:
                chirpTimer = 0;
                break;
            case State.Hurt:
                holdingBox = holdingBall = false;
                Errored?.Invoke(HeadSpot);
                break;
            case State.GettingAngry:
                planStep = 0; // a new phase starts at the top of its plan
                holdingBox = holdingBall = false;
                TellGotAngry(Phase);
                break;
            case State.Dancing:
                TellLaughed(); // "BEEP BOP DANCE!"
                break;
            case State.FlyingAway:
                holdingBox = holdingBall = false;
                break;
            case State.Exploding:
                holdingBox = holdingBall = false;
                popTimer = 0;
                fallTime = -1;
                break;
        }
    }

    void FaceBoltE()
    {
        if (Mathf.Abs(TargetX - Position.X) > 8) facing = TargetX < Position.X ? -1 : 1;
    }

    /// <summary>The middle of his head, on the screen.</summary>
    Vector2 HeadSpot => Position + new Vector2(skeleton.HeadCenter.X * facing, skeleton.HeadCenter.Y);

    /// <summary>Walks toward x. Returns true when he's there.</summary>
    bool WalkTo(float x, float speed, float dt)
    {
        float next = Mathf.MoveToward(Position.X, x, speed * dt);
        Position = new Vector2(next, Position.Y);
        return Mathf.Abs(next - x) < 0.5f;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        time += dt;
        stateTime += dt;
        holdAttacks = Mathf.Max(0, holdAttacks - dt);
        float startX = Position.X;
        jitter = 0;

        switch (state)
        {
            case State.Entering:
                // Walking in from the right, heavy and clanky
                facing = -1;
                if (WalkTo(EnterSpotX, EnterSpeed, dt)) ChangeState(State.Hello);
                break;

            case State.Hello:
                // A little wave hello! (Then the fight begins.)
                FaceBoltE();
                if (stateTime >= HelloSeconds) Rest(0.2f);
                break;

            case State.Idle:
                // Standing still for a moment (or waiting until Bolt-E is back together)
                FaceBoltE();
                if (stateTime >= idleWait && holdAttacks <= 0) StartNextSequence();
                break;

            case State.Walking:
                // Off to his stack of boxes
                if (WalkTo(walkGoal, WalkSpeed, dt)) ChangeState(State.PickingUp);
                break;

            case State.PickingUp:
                // He bends down and picks up the top box
                if (!holdingBox && stateTime >= PickSeconds * 0.55f * Quick)
                {
                    holdingBox = true;
                    BoxPickedUp?.Invoke(stackX);
                }
                if (stateTime >= PickSeconds * Quick) ChangeState(State.Lifting);
                break;

            case State.Lifting:
                // ...lifts it over his head (turned toward Bolt-E)...
                FaceBoltE();
                if (stateTime >= LiftSeconds * Quick) ChangeState(State.Throwing);
                break;

            case State.Throwing:
                // ...and THROWS it in a high arc, to where Bolt-E is!
                if (holdingBox && stateTime >= ThrowSeconds * 0.4f * Quick)
                {
                    holdingBox = false;
                    float landX = Mathf.Clamp(TargetX, 90, ArenaRight - 90);
                    BoxThrown?.Invoke(Position + new Vector2(heldBox.X * facing, heldBox.Y), landX,
                                      Phase == 3 ? FuriousBoxFlightSeconds : BoxFlightSeconds);
                }
                if (stateTime >= ThrowSeconds * Quick)
                {
                    boxesLeft--;
                    if (boxesLeft > 0) ChangeState(State.PickingUp); // another one!
                    else NextInSequence();
                }
                break;

            case State.Crouching:
                // SUPER STOMP: down he goes (knees bent)...
                if (stateTime >= CrouchSeconds * Quick) ChangeState(State.Jumping);
                break;

            case State.Jumping:
                // ...up he jumps, high in the air...
                if (stateTime >= AirSeconds)
                {
                    Stomped?.Invoke(Position.X); // ...STOMP! (two shockwaves)
                    ChangeState(State.Landing);
                }
                break;

            case State.Landing:
                if (stateTime >= LandSeconds * Quick)
                {
                    stompsLeft--;
                    if (stompsLeft > 0) ChangeState(State.Crouching); // FURIOUS: again!
                    else NextInSequence();
                }
                break;

            case State.HoldingBall:
                // SOCCER KICK: he holds the ball, then drops it in front of his foot...
                if (stateTime >= HoldBallSeconds * 0.45f * Quick) ballDropped = true;
                if (stateTime >= HoldBallSeconds * Quick) ChangeState(State.Kicking);
                break;

            case State.Kicking:
                // ...and KICKS it toward Bolt-E!
                if (holdingBall && stateTime >= KickSeconds * 0.45f * Quick)
                {
                    holdingBall = false;
                    BallKicked?.Invoke(Position + new Vector2(BallSpotX * facing, -KickBall.Radius), facing);
                }
                if (stateTime >= KickSeconds * Quick)
                {
                    ballsLeft--;
                    if (ballsLeft > 0) ChangeState(State.HoldingBall);
                    else NextInSequence();
                }
                break;

            case State.Running:
                // ROBOT RUN! Faster and faster, arms pumping
                runSpeedNow = Mathf.MoveToward(runSpeedNow, RunSpeed, RunSpeed * 4 * dt);
                if (WalkTo(runGoal, runSpeedNow, dt)) ChangeState(State.Braking);
                break;

            case State.Braking:
                if (stateTime >= BrakeSeconds) NextInSequence();
                break;

            case State.Positioning:
                // A few steps away from Bolt-E, so he doesn't kneel on him
                if (WalkTo(walkGoal, WalkSpeed, dt)) ChangeState(State.Freezing);
                break;

            case State.Freezing:
                // He freezes right where he is: "UPDATING..."
                if (stateTime >= FreezeSeconds) ChangeState(State.Kneeling);
                break;

            case State.Kneeling:
                // Down on one knee, head bowed...
                if (stateTime >= KneelSeconds) ChangeState(State.Updating);
                break;

            case State.Updating:
                // ...while the progress bar fills up. STOMP HIM NOW!
                progress = Mathf.Min(1, stateTime / UpdateTime);
                chirpTimer -= dt;
                if (chirpTimer <= 0)
                {
                    chirpTimer = ChirpEvery;
                    Chirped?.Invoke();
                }
                if (progress >= 1)
                {
                    UpdateDone?.Invoke(HeadSpot); // DING!
                    ChangeState(State.StandingUp);
                }
                break;

            case State.StandingUp:
                if (stateTime >= StandUpSeconds) BackToWork();
                break;

            case State.Hurt:
                // ERROR! Sparks fly from his head, he wobbles, and stands back up
                if (stateTime < 0.6f) jitter = 2.5f;
                if (stateTime >= HurtSeconds) BackToWork();
                break;

            case State.GettingAngry:
                FaceBoltE();
                jitter = 1.5f;
                if (stateTime >= AngrySeconds) Rest(0);
                break;

            case State.Dancing:
                // BEEP BOP DANCE!
                if (stateTime >= DanceSeconds) Rest(0.3f);
                break;

            case State.FlyingAway:
                // Bolt-E is out of batteries: OPTI-MAX waves goodbye and jogs off the nearest side of the screen
                if (stateTime < ByeSeconds)
                {
                    FaceBoltE();
                }
                else
                {
                    facing = Position.X >= ArenaMiddle ? 1 : -1;
                    Position += new Vector2(facing * FlyAwaySpeed * dt, 0);
                }
                break;

            case State.Exploding:
                // Sparks and stumbling... then he falls over backward, CLANK! ...then the big boom
                popTimer -= dt;
                if (popTimer <= 0)
                {
                    popTimer = 0.17f;
                    var box = Hitbox;
                    TellPopped(box.Position + new Vector2((float)GD.RandRange(0, box.Size.X), (float)GD.RandRange(0, box.Size.Y)));
                }
                if (stateTime < StumbleSeconds)
                {
                    jitter = 2f;
                    Position += new Vector2(-facing * 45 * dt, 0); // stumbling backward
                }
                else if (fallTime < 0 && pose.Tilt >= FallenTilt - 0.01f)
                {
                    fallTime = stateTime;
                    FellOver?.Invoke(Position + new Vector2(-facing * 150, 0));
                }
                if (fallTime >= 0 && stateTime >= fallTime + 0.4f) BlowUp();
                break;

            case State.Defeated:
                // (His pieces slide along with the ground. He's drawn turned around when he faced left, so the slide is too.)
                explosion?.Update(dt, WorldSpeed * facing);
                break;
        }

        if (state != State.Defeated)
        {
            MovePose(dt, Mathf.Abs(Position.X - startX));
            UpdateHitbox();
        }
        Scale = new Vector2(facing, 1);

        // Blink while hurt; flash while breaking down
        bool blink = (state == State.Hurt && stateTime < 1f && (int)(stateTime * 12) % 2 == 0)
                     || (state == State.Exploding && (int)(stateTime * 16) % 2 == 0);
        Modulate = blink ? new Color(1, 1, 1, 0.45f) : Colors.White;
        QueueRedraw();
    }

    const float StumbleSeconds = 0.9f;  // stumbling around before he falls over
    const float FallSeconds = 0.55f;    // falling over backward
    const float FallenTilt = 1.45f;     // how far he tips over (1.57 would be flat on the ground)

    void BlowUp()
    {
        state = State.Defeated;
        stateTime = 0;
        Scale = new Vector2(facing, 1);
        var parts = new List<RobotPart>
        {
            new("foot", rest.FarAnkle + new Vector2(8, 8), 7, () => DrawFoot(rest.FarAnkle, rest.FarFootAngle, true), FlatEvery: Mathf.Pi),
            new("leg", rest.FarKnee, 12, () => DrawLeg(rest.FarHip, rest.FarKnee, rest.FarAnkle, true), FlatEvery: Mathf.Pi),
            new("arm", rest.FarElbow, 10, () => DrawArm(rest.FarShoulder, rest.FarElbow, rest.FarHand, true), FlatEvery: Mathf.Pi),
            new("hand", rest.FarHand, 7, () => DrawHand(rest.FarElbow, rest.FarHand, true), FlatEvery: Mathf.Pi),
            new("chest", (rest.Hip + rest.Shoulder) / 2, 26, () => DrawBody(rest), FlatEvery: Mathf.Pi),
            new("leg", rest.NearKnee, 12, () => DrawLeg(rest.NearHip, rest.NearKnee, rest.NearAnkle, false), FlatEvery: Mathf.Pi),
            new("foot", rest.NearAnkle + new Vector2(8, 8), 7, () => DrawFoot(rest.NearAnkle, rest.NearFootAngle, false), FlatEvery: Mathf.Pi),
            new("head", rest.HeadCenter, 18, () => DrawHead(rest), FlatEvery: Mathf.Pi),
            new("arm", rest.NearElbow, 10, () => DrawArm(rest.NearShoulder, rest.NearElbow, rest.NearHand, false), FlatEvery: Mathf.Pi),
            new("hand", rest.NearHand, 7, () => DrawHand(rest.NearElbow, rest.NearHand, false), FlatEvery: Mathf.Pi),
        };
        explosion = new RobotExplosion(parts, Vector2.Zero, -pose.Tilt, 0, Teal,
                                       onBounce: TellPieceBounced, boomAt: new Vector2(-90, -60));
        TellBlewUp();
    }

    // ---------- His skeleton: the angles of his body, arms and legs ----------

    // ---- How long his bones are (in pixels) ----
    const float ThighLength = 80f, ShinLength = 76f;
    const float AnkleHeight = 14f;   // his ankle is this high when his foot is flat on the ground
    const float KneeSize = 9f;       // (how far a knee on the ground sticks out)
    const float TorsoLength = 78f;   // from his hips up to his shoulders
    const float HeadReach = 26f;     // from his shoulders to the middle of his head
    const float HeadWidth = 15f, HeadHeight = 19f; // his egg-shaped head (half of each)
    const float UpperArmLength = 60f, ForearmLength = 54f;
    const float BodyHalfWidth = 26f; // half the width of his bump box
    const float WalkStep = 113f;     // how far he moves in one step when walking...
    const float RunStep = 180f;      // ...and running
    const float BallSpotX = 36f;     // where the ball sits in front of his foot

    /// <summary>
    /// A pose: all the angles of his skeleton (in radians: 1.57 is a quarter turn). Angles of legs and arms are measured
    /// from straight down, and bigger = more forward. Knee and elbow bends: 0 = straight.
    /// </summary>
    struct Pose
    {
        public float Lean, Head;                              // his body leans forward; his head tips forward
        public float NearThigh, NearKnee, FarThigh, FarKnee;  // his legs (near = the one in front of us)
        public float NearArm, NearElbow, FarArm, FarElbow;    // his arms
        public float Lift;                                    // how high in the air he is (for the super stomp jump)
        public float Tilt;                                    // tipping over backward (when he's beaten)

        public Pose(float lean, float head, float nearThigh, float nearKnee, float farThigh, float farKnee,
                    float nearArm, float nearElbow, float farArm, float farElbow)
        {
            Lean = lean; Head = head;
            NearThigh = nearThigh; NearKnee = nearKnee; FarThigh = farThigh; FarKnee = farKnee;
            NearArm = nearArm; NearElbow = nearElbow; FarArm = farArm; FarElbow = farElbow;
            Lift = 0; Tilt = 0;
        }

        /// <summary>A pose part of the way from a to b (t = 0 is a, t = 1 is b).</summary>
        public static Pose Blend(in Pose a, in Pose b, float t) => new()
        {
            Lean = Mathf.Lerp(a.Lean, b.Lean, t), Head = Mathf.Lerp(a.Head, b.Head, t),
            NearThigh = Mathf.Lerp(a.NearThigh, b.NearThigh, t), NearKnee = Mathf.Lerp(a.NearKnee, b.NearKnee, t),
            FarThigh = Mathf.Lerp(a.FarThigh, b.FarThigh, t), FarKnee = Mathf.Lerp(a.FarKnee, b.FarKnee, t),
            NearArm = Mathf.Lerp(a.NearArm, b.NearArm, t), NearElbow = Mathf.Lerp(a.NearElbow, b.NearElbow, t),
            FarArm = Mathf.Lerp(a.FarArm, b.FarArm, t), FarElbow = Mathf.Lerp(a.FarElbow, b.FarElbow, t),
            Lift = Mathf.Lerp(a.Lift, b.Lift, t), Tilt = Mathf.Lerp(a.Tilt, b.Tilt, t),
        };
    }

    // ---- His poses (try changing these!)       lean   head   near leg     far leg       near arm     far arm ----
    static readonly Pose StandPose = new(0.03f, 0f, 0.07f, 0.06f, -0.05f, 0.06f, 0.1f, 0.25f, -0.06f, 0.3f);
    static readonly Pose KneelPose = new(0.8f, 1.1f, 1.8f, 1.3f, 0.8f, 2.45f, 0.25f, 1.3f, 0.05f, 0.4f); // (back knee down, sitting on his heel)
    static readonly Pose PickPose = new(0.6f, 0.3f, 0.9f, 1.7f, 0.75f, 1.6f, 0.5f, 0.3f, 0.45f, 0.35f);
    static readonly Pose LiftPose = new(-0.08f, -0.15f, 0.1f, 0.1f, -0.12f, 0.1f, 2.95f, 0.35f, 2.85f, 0.4f);
    static readonly Pose ThrowPose = new(0.35f, 0.2f, 0.45f, 0.35f, -0.3f, 0.1f, 1.35f, 0.1f, 1.25f, 0.15f);
    static readonly Pose CrouchPose = new(0.35f, 0.1f, 1.0f, 1.9f, 0.9f, 1.9f, -0.7f, 0.4f, -0.8f, 0.4f);
    static readonly Pose AirPose = new(0.05f, -0.1f, 0.55f, 1.1f, 0.35f, 1.2f, 2.6f, 0.3f, 2.5f, 0.35f);
    static readonly Pose LandPose = new(0.45f, 0.25f, 1.0f, 1.9f, 0.9f, 1.9f, 0.7f, 0.2f, 0.6f, 0.25f);
    static readonly Pose HoldBallPose = new(0.05f, 0.3f, 0.07f, 0.06f, -0.05f, 0.06f, 0.75f, 1.35f, 0.65f, 1.4f);
    static readonly Pose KickBackPose = new(0.12f, 0.3f, -0.55f, 1.3f, 0.12f, 0.1f, 0.5f, 0.6f, -0.4f, 0.4f);
    static readonly Pose KickPose = new(-0.12f, 0.2f, 1.25f, 0.1f, -0.05f, 0.08f, -0.5f, 0.5f, 0.7f, 0.5f);
    static readonly Pose BrakePose = new(-0.15f, -0.05f, 0.5f, 0.15f, -0.2f, 0.5f, 0.6f, 1.2f, -0.3f, 1.2f);

    Pose pose;              // the pose he's in right now (it glides toward the pose his move wants)
    Skeleton skeleton;      // where all his joints are right now
    Skeleton rest;          // where his joints are when he stands still (for his pieces when he blows up)
    float hitLeft, hitRight, hitTop, hitBottom; // his bump box, around his skeleton (facing right)
    float headTopY;         // the top of his head (facing right, 0 = the ground)
    Vector2 heldBox, heldBall; // where the box (or ball) in his hands is

    /// <summary>The pose his move wants right now.</summary>
    Pose WantedPose()
    {
        float t;
        switch (state)
        {
            case State.Entering or State.Walking or State.Positioning:
                return WalkPose(stride, running: false);
            case State.Running:
                return WalkPose(stride, running: true);
            case State.FlyingAway:
                return stateTime < ByeSeconds ? WavePose(goodbye: true) : WalkPose(stride, running: true);
            case State.Hello:
                return WavePose(goodbye: false);
            case State.PickingUp:
                return PickPose;
            case State.Lifting:
                return LiftPose;
            case State.Throwing:
                return ThrowPose;
            case State.Crouching:
                return CrouchPose;
            case State.Jumping:
            {
                t = Mathf.Min(1, stateTime / AirSeconds);
                var air = AirPose;
                air.Lift = 4 * StompJumpHeight * t * (1 - t); // up... and down
                if (t > 0.7f) air = Pose.Blend(air, LandPose with { Lift = air.Lift }, (t - 0.7f) / 0.3f); // legs ready to land
                return air;
            }
            case State.Landing:
                t = stateTime / (LandSeconds * Quick);
                return t < 0.45f ? LandPose : Pose.Blend(LandPose, StandPose, (t - 0.45f) / 0.55f);
            case State.HoldingBall:
                return HoldBallPose;
            case State.Kicking:
                return stateTime < KickSeconds * 0.4f * Quick ? KickBackPose : KickPose;
            case State.Braking:
                return BrakePose;
            case State.Kneeling or State.Updating:
                return KneelPose;
            case State.Hurt:
                return stateTime < 0.6f ? KneelPose : StandPose;
            case State.GettingAngry:
            {
                float shake = Mathf.Sin(time * 40) * 0.15f;
                return new Pose(0.18f, 0.15f, 0.25f, 0.35f, -0.15f, 0.3f, 0.55f + shake, 1.7f, 0.5f - shake, 1.7f);
            }
            case State.Dancing:
                return DancePose();
            case State.Exploding:
            {
                float wobble = Mathf.Sin(time * 14);
                var stumble = new Pose(-0.15f + wobble * 0.1f, -0.3f, 0.3f + wobble * 0.2f, 0.4f, -0.25f, 0.3f,
                                       1.6f + wobble * 0.6f, 0.4f, 2.0f - wobble * 0.6f, 0.4f);
                if (stateTime > StumbleSeconds)
                {
                    float fall = Mathf.Min(1, (stateTime - StumbleSeconds) / FallSeconds);
                    stumble.Tilt = FallenTilt * fall * fall; // faster and faster... CLANK!
                }
                return stumble;
            }
            default:
                // Standing still, breathing gently
                var stand = StandPose;
                stand.Lean += Mathf.Sin(time * 2.2f) * 0.015f;
                stand.Head += Mathf.Sin(time * 1.3f) * 0.04f;
                return stand;
        }
    }

    /// <summary>One step of walking (or running) at this point in his walk cycle. His arms swing the other way to his legs.</summary>
    static Pose WalkPose(float cycle, bool running)
    {
        float swing = running ? 0.75f : 0.42f;  // how far his legs swing
        float bend = running ? 1.35f : 0.7f;    // how much his knee bends as the leg swings forward
        float arms = running ? 0.95f : 0.38f;   // how far his arms swing
        float elbows = running ? 1.5f : 0.3f;
        float near = Mathf.Sin(cycle), far = Mathf.Sin(cycle + Mathf.Pi);
        return new Pose(running ? 0.28f : 0.06f, running ? -0.1f : 0.03f,
                        swing * near, 0.1f + bend * Mathf.Max(0, Mathf.Cos(cycle)),
                        swing * far, 0.1f + bend * Mathf.Max(0, Mathf.Cos(cycle + Mathf.Pi)),
                        -arms * near, elbows, -arms * far, elbows);
    }

    /// <summary>Waving hello (or goodbye) with his near hand.</summary>
    Pose WavePose(bool goodbye)
    {
        var wave = StandPose;
        wave.NearArm = 2.55f;
        wave.NearElbow = 0.55f + 0.5f * Mathf.Sin(time * (goodbye ? 9 : 11));
        wave.Head = -0.1f + Mathf.Sin(time * 3) * 0.08f;
        return wave;
    }

    /// <summary>BEEP BOP DANCE! Arms pumping, knees bouncing, head bobbing.</summary>
    Pose DancePose()
    {
        float beat = Mathf.Sin(time * 12);
        float bounce = beat * beat;
        return new Pose(Mathf.Sin(time * 6) * 0.12f, beat * 0.3f,
                        0.3f, 0.3f + 0.45f * bounce, -0.25f, 0.3f + 0.45f * bounce,
                        2.1f + 0.8f * beat, 1.5f, 2.1f - 0.8f * beat, 1.5f);
    }

    /// <summary>His pose glides toward the pose his move wants (frozen while "UPDATING...": he doesn't move at all!).</summary>
    void MovePose(float dt, float movedX)
    {
        // His walk cycle goes along with how far he moved, so his feet don't slide on the ground
        if (state is State.Entering or State.Walking or State.Positioning or State.Running or State.FlyingAway)
        {
            stride += movedX * Mathf.Pi / (state is State.Running or State.FlyingAway ? RunStep : WalkStep);
            int step = (int)(stride / Mathf.Pi);
            if (step != lastStep)
            {
                lastStep = step;
                Footstep?.Invoke(Position); // clank!
            }
        }

        var wanted = WantedPose();
        float follow = state switch
        {
            State.Freezing => 0f,  // frozen in the middle of what he was doing!
            State.Jumping or State.Exploding => 60f,
            State.Entering or State.Walking or State.Positioning or State.Running or State.FlyingAway => 30f,
            State.Kneeling => 9f,
            State.StandingUp or State.Hurt => 7f,
            State.Dancing or State.Kicking => 22f,
            _ => 14f,
        };
        if (follow > 0) pose = Pose.Blend(pose, wanted, 1 - Mathf.Exp(-follow * dt));
        if (state is State.Jumping or State.Exploding) // (these must be exact: the jump arc, and falling over)
        {
            pose.Lift = wanted.Lift;
            pose.Tilt = wanted.Tilt;
        }
        Build(pose, ref skeleton);
    }

    /// <summary>Where all his joints are (facing right; 0,0 = the ground between his feet).</summary>
    struct Skeleton
    {
        public Vector2 Hip, NearHip, FarHip, NearKnee, FarKnee, NearAnkle, FarAnkle;
        public Vector2 Shoulder, NearShoulder, FarShoulder, NearElbow, FarElbow, NearHand, FarHand, HeadCenter;
        public float BodyAngle, HeadAngle, NearFootAngle, FarFootAngle;
    }

    /// <summary>How far down a leg reaches from the hip (its lowest point: the foot, or a knee on the ground).</summary>
    static float LegReach(float thigh, float knee)
    {
        float kneeDown = ThighLength * Mathf.Cos(thigh);
        float ankleDown = kneeDown + ShinLength * Mathf.Cos(thigh - knee);
        return Mathf.Max(kneeDown + KneeSize, ankleDown + AnkleHeight);
    }

    /// <summary>A point along a bone that starts at "from", points at "angle" (0 = straight down) and is "length" long.</summary>
    static Vector2 Bone(Vector2 from, float angle, float length) => from + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * length;

    /// <summary>
    /// Works out where every joint is for a pose. His hips are just high enough that his longest leg touches the
    /// ground (so bending his knees makes him crouch). Then the whole skeleton is tipped over (Tilt) and lifted (Lift).
    /// </summary>
    static void Build(in Pose p, ref Skeleton s)
    {
        float hipHeight = Mathf.Max(LegReach(p.NearThigh, p.NearKnee), LegReach(p.FarThigh, p.FarKnee));
        var hip = new Vector2(0, -hipHeight);
        var nearHip = hip + new Vector2(4, 0);
        var farHip = hip + new Vector2(-4, -2);
        var nearKnee = Bone(nearHip, p.NearThigh, ThighLength);
        var farKnee = Bone(farHip, p.FarThigh, ThighLength);
        var nearAnkle = Bone(nearKnee, p.NearThigh - p.NearKnee, ShinLength);
        var farAnkle = Bone(farKnee, p.FarThigh - p.FarKnee, ShinLength);

        var up = new Vector2(Mathf.Sin(p.Lean), -Mathf.Cos(p.Lean));       // the way his body points
        var front = new Vector2(Mathf.Cos(p.Lean), Mathf.Sin(p.Lean));     // the way his chest faces
        var shoulder = hip + up * TorsoLength;
        var nearShoulder = shoulder + front * 3 - up * 4;
        var farShoulder = shoulder - front * 7 - up * 2;
        float headAngle = p.Lean + p.Head;
        var headCenter = shoulder + new Vector2(Mathf.Sin(headAngle), -Mathf.Cos(headAngle)) * HeadReach;
        var nearElbow = Bone(nearShoulder, p.NearArm, UpperArmLength);
        var farElbow = Bone(farShoulder, p.FarArm, UpperArmLength);
        var nearHand = Bone(nearElbow, p.NearArm + p.NearElbow, ForearmLength);
        var farHand = Bone(farElbow, p.FarArm + p.FarElbow, ForearmLength);

        // Tip the whole skeleton over around his feet, and lift it up
        float turn = -p.Tilt;
        var lift = new Vector2(0, -p.Lift);
        s.Hip = hip.Rotated(turn) + lift;
        s.NearHip = nearHip.Rotated(turn) + lift;
        s.FarHip = farHip.Rotated(turn) + lift;
        s.NearKnee = nearKnee.Rotated(turn) + lift;
        s.FarKnee = farKnee.Rotated(turn) + lift;
        s.NearAnkle = nearAnkle.Rotated(turn) + lift;
        s.FarAnkle = farAnkle.Rotated(turn) + lift;
        s.Shoulder = shoulder.Rotated(turn) + lift;
        s.NearShoulder = nearShoulder.Rotated(turn) + lift;
        s.FarShoulder = farShoulder.Rotated(turn) + lift;
        s.NearElbow = nearElbow.Rotated(turn) + lift;
        s.FarElbow = farElbow.Rotated(turn) + lift;
        s.NearHand = nearHand.Rotated(turn) + lift;
        s.FarHand = farHand.Rotated(turn) + lift;
        s.HeadCenter = headCenter.Rotated(turn) + lift;
        s.BodyAngle = p.Lean + turn;
        s.HeadAngle = headAngle + turn;
        // A foot is flat on the ground, but its toes point down when the shin leans back (like kneeling, or pushing off)
        s.NearFootAngle = -Mathf.Min(0, p.NearThigh - p.NearKnee) * 0.9f + turn;
        s.FarFootAngle = -Mathf.Min(0, p.FarThigh - p.FarKnee) * 0.9f + turn;
    }

    /// <summary>His bump box goes around his skeleton: body and head (a bit smaller than the drawing, to be fair).</summary>
    void UpdateHitbox()
    {
        var s = skeleton;
        // The top of his tipped egg-shaped head
        float headUp = Mathf.Sqrt(HeadWidth * HeadWidth * Mathf.Pow(Mathf.Sin(s.HeadAngle), 2)
                                  + HeadHeight * HeadHeight * Mathf.Pow(Mathf.Cos(s.HeadAngle), 2));
        headTopY = s.HeadCenter.Y - headUp;
        hitTop = Mathf.Min(headTopY + 4, s.Shoulder.Y - 12);
        hitLeft = Mathf.Min(s.Hip.X, Mathf.Min(s.Shoulder.X, s.HeadCenter.X)) - BodyHalfWidth;
        hitRight = Mathf.Max(s.Hip.X, Mathf.Max(s.Shoulder.X, s.HeadCenter.X)) + BodyHalfWidth;
        hitBottom = Mathf.Max(Mathf.Max(s.NearAnkle.Y, s.FarAnkle.Y) + AnkleHeight, Mathf.Max(s.NearKnee.Y, s.FarKnee.Y) + KneeSize);

        // Where the box (or ball) in his hands is
        var hands = (s.NearHand + s.FarHand) / 2;
        bool armsUp = s.NearHand.Y < s.NearShoulder.Y;
        heldBox = hands + new Vector2(armsUp ? 0 : 10, armsUp ? -ThrownBox.Height / 2 + 2 : 2);
        heldBall = hands + new Vector2(6, 4);
    }

    // ---------- Drawing (everything is drawn facing right, at his real size; Scale turns him around) ----------

    // ---- His colors (try changing these!) ----
    public static readonly Color Teal = new(0.25f, 0.95f, 0.88f);     // his face light strip and eyes...
    static readonly Color AngryOrange = new(1f, 0.6f, 0.15f);          // ...orange when he's ANGRY...
    static readonly Color FuriousRed = new(1f, 0.22f, 0.2f);           // ...and red when he's FURIOUS
    static readonly Color Plate = new(0.93f, 0.93f, 0.95f);            // his white plates
    static readonly Color PlateShade = new(0.78f, 0.79f, 0.83f);       // the shady side of his plates
    static readonly Color JointBlack = new(0.1f, 0.1f, 0.12f);         // his black joints, hands and feet
    static readonly Color HeadBlack = new(0.05f, 0.05f, 0.07f);        // his glossy black head
    static readonly Color VisorBlack = new(0.02f, 0.02f, 0.04f);       // his face
    static readonly Color Mechanics = new(0.4f, 0.42f, 0.47f);         // the metal bits in his joints
    static readonly Color SparkYellow = new(1f, 0.92f, 0.35f);
    static readonly Color ErrorRed = new(1f, 0.2f, 0.18f);
    static readonly Color StompYellow = new(1f, 0.85f, 0.2f);

    Color EyeColor => Phase switch { 1 => Teal, 2 => AngryOrange, _ => FuriousRed };

    // ---- Shapes made once (not every time he's drawn). His body shapes: 0,0 = his hips, up is -y, his chest faces +x ----
    static readonly Vector2[] PelvisShape = { new(-17, -12), new(-8, -18), new(12, -18), new(19, -8), new(17, 8), new(6, 13), new(-9, 13), new(-18, 4) };
    static readonly Vector2[] WaistShape = { new(-15, -37), new(17, -37), new(15, -12), new(-14, -12) };
    static readonly Vector2[] ChestShape = { new(-19, -74), new(-6, -82), new(14, -81), new(25, -70), new(28, -54), new(24, -38), new(14, -31), new(-12, -31), new(-20, -42), new(-22, -60) };
    static readonly Vector2[] ChestSide = { new(-22, -60), new(-19, -74), new(-11, -78), new(-13, -40), new(-20, -42) };
    static readonly Vector2[] YokeShape = { new(-14, -79), new(12, -82), new(6, -91), new(-8, -90) };
    static readonly Vector2[] FootShape = { new(-12, 14), new(-12, 5), new(-6, -3), new(8, 1), new(21, 6), new(27, 10), new(27, 14) };
    // (and somewhere to put each shape once it's turned and moved into place: the same lists every time)
    readonly Vector2[] pelvisPlaced = new Vector2[8], waistPlaced = new Vector2[4], chestPlaced = new Vector2[10];
    readonly Vector2[] chestSidePlaced = new Vector2[5], yokePlaced = new Vector2[4], footPlaced = new Vector2[7];
    readonly Vector2[] stripPlaced = new Vector2[14];
    readonly Vector2[] arrow = new Vector2[3];

    /// <summary>The words on his progress bar, made once ("0%" to "100%"), so no new words are made every frame.</summary>
    static readonly string[] Percents = Enumerable.Range(0, 101).Select(percent => $"{percent}%").ToArray();
    const string UpdatingWords = "UPDATING...", ErrorWords = "ERROR!";

    /// <summary>Turns a shape and moves it into place (into "into"), for drawing.</summary>
    static Vector2[] Place(Vector2[] shape, Vector2[] into, Vector2 at, float angle)
    {
        for (int i = 0; i < shape.Length; i++) into[i] = at + shape[i].Rotated(angle);
        return into;
    }

    /// <summary>A bone with round ends (a "capsule"): a thick line with a circle at each end.</summary>
    void Capsule(Vector2 from, Vector2 to, float width, Color color)
    {
        DrawLine(from, to, color, width, true);
        DrawCircle(from, width / 2, color);
        DrawCircle(to, width / 2, color);
    }

    /// <summary>The far side of him is a bit darker (it's in the shade).</summary>
    static Color Shaded(Color color, bool far) => far ? color.Darkened(0.22f) : color;

    public override void _Draw()
    {
        drawnIn[(int)state] = true;
        BarLabel = "";

        if (explosion is not null)
        {
            explosion.Draw(this);
            return;
        }

        var s = skeleton;
        if (jitter > 0) // shaking (angry, ERROR!, breaking down): every part is drawn a little to the side
        {
            var shake = new Vector2(Mathf.Sin(time * 63) * jitter, Mathf.Sin(time * 47) * jitter * 0.5f);
            Shift(ref s, shake);
        }

        // His shadow on the ground (smaller when he jumps)
        float near = Mathf.Clamp(1 - pose.Lift / 300f, 0.3f, 1f);
        Shapes.Ellipse(this, new Vector2(s.Hip.X * 0.5f, 1), new Vector2(42 * near + (state is State.Kneeling or State.Updating ? 40 : 0), 7 * near),
                       new Color(0, 0, 0, 0.28f * near));

        // Back to front: far arm, far leg, his body, near leg, head, what he's holding, near arm
        DrawArm(s.FarShoulder, s.FarElbow, s.FarHand, true);
        DrawHand(s.FarElbow, s.FarHand, true);
        DrawLeg(s.FarHip, s.FarKnee, s.FarAnkle, true);
        DrawFoot(s.FarAnkle, s.FarFootAngle, true);
        DrawBody(s);
        DrawLeg(s.NearHip, s.NearKnee, s.NearAnkle, false);
        DrawFoot(s.NearAnkle, s.NearFootAngle, false);
        DrawHead(s);
        if (holdingBox) ThrownBox.DrawBox(this, heldBox);
        if (holdingBall) DrawHeldBall();
        DrawArm(s.NearShoulder, s.NearElbow, s.NearHand, false);
        DrawHand(s.NearElbow, s.NearHand, false);

        if (state == State.Hurt && stateTime < 0.9f) DrawSparks(s.HeadCenter);
        if (state is State.Freezing or State.Kneeling or State.Updating or State.StandingUp or State.Hurt) DrawProgressBar(s);
        if (state == State.Updating) DrawStompArrow(s);
        if (Phase == 3 && !IsBeaten && state is not (State.Updating or State.Kneeling or State.FlyingAway)) DrawFuriousSparks(s);
    }

    /// <summary>Moves every joint a little (for shaking).</summary>
    static void Shift(ref Skeleton s, Vector2 by)
    {
        s.Hip += by; s.NearHip += by; s.FarHip += by; s.NearKnee += by; s.FarKnee += by; s.NearAnkle += by; s.FarAnkle += by;
        s.Shoulder += by; s.NearShoulder += by; s.FarShoulder += by; s.NearElbow += by; s.FarElbow += by;
        s.NearHand += by; s.FarHand += by; s.HeadCenter += by;
    }

    /// <summary>A leg: black hip joint, white thigh plate, black knee, white shin plate, black ankle.</summary>
    void DrawLeg(Vector2 hip, Vector2 knee, Vector2 ankle, bool far)
    {
        var white = Shaded(Plate, far);
        var black = Shaded(JointBlack, far);
        Capsule(hip, knee, 19, black);                                          // the black hip joint and thigh bone
        Capsule(hip.Lerp(knee, 0.3f), hip.Lerp(knee, 0.9f), 23, white);         // the white thigh plate
        DrawLine(hip.Lerp(knee, 0.35f), hip.Lerp(knee, 0.85f), Shaded(PlateShade, far), 4, true); // (a crease)
        Capsule(knee.Lerp(ankle, 0.55f), ankle, 13, black);                     // the black ankle machinery
        Capsule(knee.Lerp(ankle, 0.12f), knee.Lerp(ankle, 0.62f), 19, white);   // the white shin plate
        DrawCircle(knee, 10, black);                                            // the knee
        DrawCircle(knee, 4, Mechanics);
        DrawCircle(ankle, 7, black);
    }

    /// <summary>A black foot (its toes point the way he faces).</summary>
    void DrawFoot(Vector2 ankle, float angle, bool far)
    {
        DrawColoredPolygon(Place(FootShape, footPlaced, ankle, angle), Shaded(JointBlack, far));
    }

    /// <summary>An arm: black shoulder joint, white shoulder cap and upper arm, black elbow, white forearm.</summary>
    void DrawArm(Vector2 shoulder, Vector2 elbow, Vector2 hand, bool far)
    {
        var white = Shaded(Plate, far);
        var black = Shaded(JointBlack, far);
        DrawCircle(shoulder, 11, black);                                        // the black shoulder joint
        Capsule(shoulder, shoulder.Lerp(elbow, 0.85f), 17, white);              // the white upper arm
        DrawCircle(shoulder.Lerp(elbow, 0.1f), 13.5f, white);                   // the round shoulder cap
        DrawCircle(shoulder.Lerp(elbow, 0.1f) + new Vector2(-3, -4), 5, new Color(1, 1, 1, far ? 0.15f : 0.5f)); // (a shine)
        Capsule(elbow.Lerp(hand, 0.12f), elbow.Lerp(hand, 0.85f), 15, white);   // the white forearm
        DrawLine(elbow.Lerp(hand, 0.3f), elbow.Lerp(hand, 0.8f), Shaded(Mechanics, far), 3, true); // (the cables inside)
        DrawCircle(elbow, 9, black);                                            // the black elbow
    }

    /// <summary>A black robot hand with fingers, at the end of the forearm.</summary>
    void DrawHand(Vector2 elbow, Vector2 hand, bool far)
    {
        var black = Shaded(JointBlack, far);
        var along = (hand - elbow).Normalized();
        var across = new Vector2(-along.Y, along.X);
        DrawCircle(hand, 5, black);                                              // the wrist
        var palm = hand + along * 7;
        Shapes.Ellipse(this, palm, new Vector2(8, 6), black, along.Angle());     // the palm
        for (int finger = -1; finger <= 1; finger++)                             // three fingers...
        {
            var start = palm + along * 5 + across * (finger * 3.2f);
            DrawLine(start, start + along * 8 + across * (finger * 1.2f), black, 2.6f, true);
        }
        DrawLine(palm + across * 4, palm + across * 8 + along * 4, black, 2.6f, true); // ...and a thumb
    }

    /// <summary>His body: black hips with machinery, a black waist, the white chest plate, and a black neck.</summary>
    void DrawBody(in Skeleton s)
    {
        float angle = s.BodyAngle;
        DrawColoredPolygon(Place(PelvisShape, pelvisPlaced, s.Hip, angle), JointBlack);
        DrawCircle(s.NearHip, 7, Mechanics);                                     // hip machinery
        DrawCircle(s.NearHip, 3, JointBlack);
        DrawCircle(s.Hip + new Vector2(-10, 4).Rotated(angle), 4, Mechanics);
        DrawColoredPolygon(Place(WaistShape, waistPlaced, s.Hip, angle), JointBlack);
        DrawColoredPolygon(Place(ChestShape, chestPlaced, s.Hip, angle), Plate);
        DrawColoredPolygon(Place(ChestSide, chestSidePlaced, s.Hip, angle), PlateShade);
        DrawLine(s.Hip + new Vector2(-14, -33).Rotated(angle), s.Hip + new Vector2(16, -33).Rotated(angle), PlateShade, 2, true);
        DrawColoredPolygon(Place(YokeShape, yokePlaced, s.Hip, angle), JointBlack);
        // The neck
        var headDown = new Vector2(-Mathf.Sin(s.HeadAngle), Mathf.Cos(s.HeadAngle));
        Capsule(s.Shoulder + new Vector2(0, -6).Rotated(angle), s.HeadCenter + headDown * 12, 11, JointBlack);
    }

    /// <summary>
    /// His glossy black egg-shaped head, with a thin glowing light strip around his face and two soft glowing eyes.
    /// The eyes are calm, narrow when he attacks, spinning loading circles while he UPDATES, happy when he dances or
    /// waves, glitchy when he's hurt, and X eyes when he's beaten. Teal, then orange (ANGRY), then red (FURIOUS).
    /// </summary>
    void DrawHead(in Skeleton s)
    {
        float angle = s.HeadAngle;
        var middle = s.HeadCenter;
        Shapes.Ellipse(this, middle, new Vector2(HeadWidth, HeadHeight), HeadBlack, angle);
        Shapes.Ellipse(this, middle + new Vector2(-2, -4).Rotated(angle), new Vector2(HeadWidth - 1, HeadHeight - 3), HeadBlack, angle);
        var face = middle + new Vector2(5, 2).Rotated(angle);
        Shapes.Ellipse(this, face, new Vector2(10, 15), VisorBlack, angle);

        // The light strip around the edge of his face (with a soft glow)
        var color = EyeColor;
        for (int i = 0; i < stripPlaced.Length; i++)
        {
            float around = Mathf.Pi * (0.45f + 1.15f * i / (stripPlaced.Length - 1)); // from his chin, round the back of his face, to the top
            stripPlaced[i] = face + new Vector2(Mathf.Cos(around) * 11, Mathf.Sin(around) * 16).Rotated(angle);
        }
        DrawPolyline(stripPlaced, new Color(color, 0.3f), 6, true);
        DrawPolyline(stripPlaced, color, 2.2f, true);

        // The shine on his glossy head
        Shapes.Ellipse(this, middle + new Vector2(-6, -11).Rotated(angle), new Vector2(5, 2.4f), new Color(1, 1, 1, 0.35f), angle - 0.6f);
        DrawCircle(middle + new Vector2(-10, -4).Rotated(angle), 1.6f, new Color(1, 1, 1, 0.45f));

        DrawEye(middle + new Vector2(11, -2).Rotated(angle), 1f, angle, color);
        DrawEye(middle + new Vector2(3, -3).Rotated(angle), 0.75f, angle, color);
    }

    /// <summary>One soft glowing eye on his black face ("size" is smaller for the far eye).</summary>
    void DrawEye(Vector2 eye, float size, float angle, Color color)
    {
        var flat = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); // (along his face)
        switch (state)
        {
            case State.Exploding or State.Defeated:
            {
                var a = new Vector2(3, 3).Rotated(angle) * size;
                var b = new Vector2(3, -3).Rotated(angle) * size;
                DrawLine(eye - a, eye + a, color, 1.8f, true);
                DrawLine(eye - b, eye + b, color, 1.8f, true);
                break;
            }
            case State.Freezing or State.Kneeling or State.Updating:
                // Loading circles, spinning round and round
                DrawCircle(eye, 5 * size, new Color(color, 0.15f));
                DrawArc(eye, 3.4f * size, time * 9, time * 9 + 4.2f, 10, color, 1.5f, true);
                break;
            case State.Hurt:
                // Glitchy red flickers: ERROR!
                if ((int)(time * 14) % 2 == 0) DrawRect(new Rect2(eye - new Vector2(2.5f, 2.5f) * size, new Vector2(5, 5) * size), ErrorRed);
                break;
            case State.Dancing or State.Hello:
                DrawArc(eye + new Vector2(0, 1.5f).Rotated(angle), 3.2f * size, angle + Mathf.Pi * 1.15f, angle + Mathf.Pi * 1.85f, 8, color, 2f, true);
                break;
            case State.FlyingAway when stateTime < ByeSeconds:
                DrawArc(eye + new Vector2(0, 1.5f).Rotated(angle), 3.2f * size, angle + Mathf.Pi * 1.15f, angle + Mathf.Pi * 1.85f, 8, color, 2f, true);
                break;
            default:
                DrawCircle(eye, 6 * size, new Color(color, 0.22f)); // the soft glow
                if (Attacking || state == State.GettingAngry)
                {
                    DrawLine(eye - flat * 3.4f * size, eye + flat * 3.4f * size, color, 2.2f * size, true); // narrow: watch out!
                }
                else
                {
                    DrawCircle(eye, 2.8f * size, color);
                }
                break;
        }
    }

    /// <summary>The ball in his hands, then dropping in front of his foot.</summary>
    void DrawHeldBall()
    {
        if (!ballDropped)
        {
            KickBall.DrawBall(this, heldBall, 0);
            return;
        }
        float fallen = Mathf.Min(1, (stateTime - HoldBallSeconds * 0.45f * Quick) / 0.2f);
        if (state == State.Kicking) fallen = 1;
        var floor = new Vector2(BallSpotX, -KickBall.Radius);
        KickBall.DrawBall(this, heldBall.Lerp(floor, fallen * fallen), 0);
    }

    /// <summary>
    /// The progress bar over his head: "UPDATING..." filling up 0% to 100% (or a red "ERROR!" when he's stomped).
    /// (Words would come out backwards when he faces left, so they're turned back around.)
    /// </summary>
    void DrawProgressBar(in Skeleton s)
    {
        bool error = state == State.Hurt;
        if (error && stateTime > 0.9f) return;
        if (state == State.StandingUp && stateTime > 0.35f) return;
        float filled = error || state == State.StandingUp ? 1 : progress;
        var spot = new Vector2((s.HeadCenter.X + s.Hip.X) / 2, Mathf.Min(headTopY, s.Shoulder.Y) - 78);
        DrawSetTransform(spot, 0, new Vector2(facing, 1)); // (facing left, this turns the words back the right way)

        var color = error ? ErrorRed : Teal;
        Shapes.RoundRect(this, new Rect2(-62, -10, 124, 20), 6, new Color(0.05f, 0.06f, 0.08f, 0.9f));
        if (filled > 0.01f) Shapes.RoundRect(this, new Rect2(-59, -7, 118 * filled, 14), 4, color);
        var font = ThemeDB.FallbackFont;
        string label = error ? ErrorWords : UpdatingWords;
        bool flash = !error || (int)(stateTime * 8) % 2 == 0;
        if (flash)
        {
            float width = font.GetStringSize(label, HorizontalAlignment.Left, -1, 20).X;
            var at = new Vector2(-width / 2, -18);
            DrawStringOutline(font, at, label, HorizontalAlignment.Left, -1, 20, 6, new Color(0.05f, 0.06f, 0.08f));
            DrawString(font, at, label, HorizontalAlignment.Left, -1, 20, color);
        }
        if (!error)
        {
            string percent = Percents[Mathf.Clamp((int)(filled * 100), 0, 100)];
            float width = font.GetStringSize(percent, HorizontalAlignment.Left, -1, 13).X;
            var at = new Vector2(-width / 2, 5);
            DrawStringOutline(font, at, percent, HorizontalAlignment.Left, -1, 13, 4, new Color(0.05f, 0.06f, 0.08f));
            DrawString(font, at, percent, HorizontalAlignment.Left, -1, 13, Colors.White);
        }
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        BarLabel = label;
    }

    /// <summary>A bouncing yellow arrow over his back while he updates: STOMP HERE!</summary>
    void DrawStompArrow(in Skeleton s)
    {
        float x = (s.Hip.X + s.Shoulder.X) / 2;
        float y = Mathf.Min(headTopY, s.Shoulder.Y) - 22 - Mathf.Abs(Mathf.Sin(time * 6)) * 10;
        arrow[0] = new Vector2(x - 13, y - 16);
        arrow[1] = new Vector2(x + 13, y - 16);
        arrow[2] = new Vector2(x, y);
        DrawColoredPolygon(arrow, StompYellow);
        DrawRect(new Rect2(x - 5, y - 29, 10, 14), StompYellow);
    }

    /// <summary>ERROR! Sparks crackling out of his head.</summary>
    void DrawSparks(Vector2 head)
    {
        for (int i = 0; i < 6; i++)
        {
            if (((int)(time * 20) + i) % 3 == 0) continue; // (they flicker)
            var direction = Vector2.Up.Rotated(-1.3f + i * 0.52f + Mathf.Sin(time * 31 + i) * 0.2f);
            var start = head + direction * 20;
            var middle = start + direction * 9 + direction.Orthogonal() * 4;
            var end = start + direction * 18;
            DrawLine(start, middle, SparkYellow, 2.2f, true);
            DrawLine(middle, end, SparkYellow, 2.2f, true);
        }
    }

    /// <summary>FURIOUS: little red-hot sparks crackling around him.</summary>
    void DrawFuriousSparks(in Skeleton s)
    {
        for (int i = 0; i < 4; i++)
        {
            if (((int)(time * 12) + i) % 3 == 0) continue; // (they flicker)
            var spot = s.Shoulder + new Vector2(-34 + i * 22, -10 + (i % 2) * 60);
            var line = Vector2.Right.Rotated(time * 17 + i * 2) * 6;
            DrawLine(spot - line, spot + line, new Color(1f, 0.55f, 0.3f), 2, true);
        }
    }
}
