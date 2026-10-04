using Godot;

/// <summary>The moves Dad (or Mom) can pick with the number keys while driving Big Rusty (see Main.Driver.cs).</summary>
public enum BossMove { Fireballs, Helpers, Pound, Laser, Laugh }

/// <summary>
/// What Big Rusty says to Dad's order: Ok (here it comes!), Busy (he's in the middle of something: wait for the
/// thinking dots), Tired (only a ground pound works now) or NotYet (he isn't angry enough for that move yet).
/// </summary>
public enum DriveAnswer { Ok, Busy, Tired, NotYet }

/// <summary>
/// BIG RUSTY, the boss! (from enemy-boss-who-can-shoot.png)
/// When he shows up, the world stops and Bolt-E can ride left and right: it's an arena fight!
/// He has 6 health and gets angrier as he gets hurt:
///   Phase 1:            shoots bouncing fireballs, then does a GROUND POUND.
///   Phase 2 (ANGRY):    more fireballs, drops Pup-Bot helpers, then a ground pound.
///   Phase 3 (FURIOUS):  lots of fireballs, a giant laser (stay on the ground!), then TWO ground pounds.
/// After a ground pound he's dizzy for a moment: that's when you jump on his head!
/// Dad (or Mom) can DRIVE him with the number keys: then he wears a blue cap, and thinking dots over his head
/// show when he's waiting for Dad's next order. Fair-play rules keep it winnable (see Request).
/// Position is the bottom-middle of his hover disc.
/// </summary>
public partial class Boss : Node2D
{
    public const string DisplayName = "BIG RUSTY";
    public const int MaxHealth = 6;
    public const float Size = 1.5f; // how big he is (1 = the size of the drawing below)

    // (WaitingForDriver = Dad is driving him, and he's waiting for Dad's next order: the thinking dots show)
    enum State
    {
        Entering, Laughing, Shooting, DroppingHelpers, Laser, PoundRise, PoundAim, PoundSlam,
        Dizzy, Recovering, Hurt, GettingAngry, FlyingAway, Exploding, Defeated, WaitingForDriver,
    }

    // ---- Dad drives Big Rusty (try changing these!) ----
    public const int AttacksBeforeHeMustPound = 2;  // after this many attacks he's TIRED: only a ground pound works (and after a pound he's dizzy!)
    public const float DriverWaitBeforeAI = 3f;     // if Dad picks nothing for this many seconds, Rusty's own brain picks the next move
    public const int LaughsInARow = 2;              // Dad can make him laugh this many times in a row, then he has to do a real move first

    /// <summary>True once Dad (or Mom) took over with the number keys (see Main.Driver.cs).</summary>
    public bool DriverControlled { get; private set; }
    BossMove? request;         // Dad's order, waiting to happen (only taken while he waits with his thinking dots)
    int attacksSincePound;     // fireballs, helpers and lasers since his last ground pound
    int laughsSinceRealMove;   // Dad's laughs since Rusty's last real move (fireballs, helpers, laser or ground pound)
    bool aiTurn;               // true for a moment while his own brain picks a move for Dad
    float laughCooldown;       // seconds before Dad can make him laugh again
    bool capOnHead = true;     // Dad's cap flies off on its own when he blows up

    /// <summary>He's TIRED after 2 attacks in a row: only a ground pound (or a laugh) works until he has pounded.</summary>
    public bool IsTired => DriverControlled && attacksSincePound >= AttacksBeforeHeMustPound;
    /// <summary>Dad can take over (or give an order) any time, except while he flies in, flies away or blows up.</summary>
    public bool CanBeDriven => state is not (State.Entering or State.Exploding or State.Defeated or State.FlyingAway);
    /// <summary>Fireballs, helpers and lasers since his last ground pound (the self-test reads this).</summary>
    public int AttacksSincePound => attacksSincePound;
    /// <summary>True if Dad's blue cap was drawn in the last frame (the self-test reads this).</summary>
    public bool CapDrawn { get; private set; }
    /// <summary>True if his World Tour outfit was drawn in the last frame (the self-test reads this).</summary>
    public bool OutfitDrawn { get; private set; }
    /// <summary>How many thinking dots were drawn in the last frame: 3 while he waits for Dad's order (the self-test reads this).</summary>
    public int DotsDrawn { get; private set; }
    /// <summary>True when Dad's cap flew off as its own piece when he blew up (the self-test reads this).</summary>
    public bool CapFlewOff => explosion?.HasPart("cap") == true;

    public float GroundY;
    public float WorldSpeed;   // so his pieces slide along with the ground after he blows up
    public float TargetX;      // where Bolt-E is, so he can aim at it
    public int Health { get; private set; } = MaxHealth;

    /// <summary>What he dresses up in for the world he's in: a party hat, cool sunglasses, a bobble hat or a space helmet.</summary>
    public BossOutfit Outfit;
    bool outfitOnHead = true;  // a hat flies off on its own when he blows up

    /// <summary>True while his outfit is still on his head (the self-test reads this).</summary>
    public bool OutfitOnHead => outfitOnHead;
    /// <summary>True when his hat flew off as its own piece when he blew up (the self-test reads this).</summary>
    public bool HatFlewOff => explosion?.HasPart("hat") == true;
    /// <summary>
    /// The sunglasses go up on his forehead when he winds up a ground pound, gets hurt, is dizzy or breaks down,
    /// so you can always see his flashing eyes, his spinning dizzy eyes and his X eyes.
    /// </summary>
    public bool SunglassesUp => state is State.Dizzy or State.PoundRise or State.Hurt or State.Exploding or State.Defeated;

    /// <summary>1 = normal, 2 = ANGRY, 3 = FURIOUS</summary>
    public int Phase => Health > 4 ? 1 : Health > 2 ? 2 : 3;

    // Things he does that the rest of the game needs to know about
    public event Action<Vector2, float>? Fired;          // a fireball comes out here, going left (-1) or right (1)
    public event Action? Warned;                          // he's about to do a ground pound!
    public event Action<float>? Slammed;                  // he landed a ground pound at this x
    public event Action<Vector2, float>? LaserFired;      // a laser starts here, going left (-1) or right (1)
    public event Action<Vector2>? HelperDropped;          // a Pup-Bot helper drops out here
    public event Action<int>? GotAngry;                   // he moved to phase 2 or 3
    public event Action? Laughed;                         // HA HA HA!
    public event Action<Vector2>? Popped;                 // a little explosion while he's breaking down
    public event Action? BlewUp;                          // the big KABOOM at the very end
    public event Action<bool>? PieceBounced;              // one of his pieces hit the ground (true = a big piece)

    /// <summary>He can only be stomped while he's dizzy after a ground pound.</summary>
    public bool CanBeStomped => state == State.Dizzy;
    /// <summary>Bumping into him hurts at these times. (While dizzy he's harmless: go stomp him!)</summary>
    public bool IsDangerous => state is State.Laughing or State.Shooting or State.DroppingHelpers or State.Laser
                                     or State.PoundSlam or State.GettingAngry or State.WaitingForDriver;
    /// <summary>True once his health reaches 0 (he's breaking down or blown up).</summary>
    public bool IsBeaten => state is State.Exploding or State.Defeated;
    public bool HasBlownUp => state == State.Defeated;
    public bool Finished => (state == State.Defeated && stateTime > 3.5f) || (state == State.FlyingAway && Position.Y < -400);
    public string CurrentMove => state.ToString();

    public Rect2 Hitbox
    {
        get
        {
            var size = new Vector2(92, 146) * Size;
            size.Y *= Squash;
            return new Rect2(Position + new Vector2(-size.X / 2, -size.Y), size);
        }
    }
    public float Top => Hitbox.Position.Y;

    // ---- Colors ----
    static readonly Color Orange = new(0.97f, 0.58f, 0.12f);
    static readonly Color DarkOrange = new(0.85f, 0.45f, 0.08f);
    static readonly Color Brown = new(0.45f, 0.28f, 0.17f);
    static readonly Color Screen = new(0.05f, 0.04f, 0.03f);
    static readonly Color EyeYellow = new(1f, 0.9f, 0.15f);

    State state = State.Entering;
    float stateTime, time;
    float facing = -1;     // -1 = facing left (the way he's drawn), 1 = facing right
    int planStep;          // which attack in his list he's doing
    int shotsLeft, helpersLeft, poundsLeft;
    float shotTimer, helperTimer, popTimer;
    bool laserFired;
    int angerShown = 1;    // the phase he last showed us
    float recoil;          // the blaster kicks back when it shoots
    float holdAttacks;     // after Bolt-E crashes, he waits this many seconds before attacking again (fair play!)
    Vector2 moveFrom;      // where a smooth move started
    RobotExplosion? explosion;

    Vector2 Home => new(1000, GroundY - 230);   // where he floats while attacking
    float Squash => state == State.Dizzy ? 0.78f : 1f;
    float TimeBetweenShots => Phase switch { 1 => 1.1f, 2 => 0.95f, _ => 0.8f };
    float DizzyTime => Phase == 3 ? 2.3f : 2.7f;

    /// <summary>His list of attacks for each phase. He goes through it, then starts over.</summary>
    State[] AttackPlan => Phase switch
    {
        1 => new[] { State.Shooting, State.PoundRise },
        2 => new[] { State.Shooting, State.DroppingHelpers, State.PoundRise },
        _ => new[] { State.Shooting, State.Laser, State.PoundRise },
    };

    /// <summary>Where the end of his blaster is, on the screen.</summary>
    Vector2 Muzzle => Position + new Vector2(-104 + recoil * 10, -71) * Scale;

    public override void _Ready()
    {
        Position = new Vector2(1650, GroundY - 230);
        moveFrom = Position;
        Scale = new Vector2(Size, Size);
    }

    /// <summary>Ouch! Called when Bolt-E lands on his head.</summary>
    public void Stomp()
    {
        if (!CanBeStomped) return;
        LoseHealth(1);
    }

    /// <summary>
    /// Ouch! He loses some health and gets hurt. With no health left he shakes, pops and blows up.
    /// (A stomp takes 1. The self-test calls this too, so it doesn't have to wait for him to get dizzy 6 times.)
    /// </summary>
    public void LoseHealth(int amount)
    {
        Health = Math.Max(0, Health - amount);
        if (Health <= 0)
        {
            ChangeState(State.Exploding);
            popTimer = 0;
        }
        else
        {
            ChangeState(State.Hurt);
        }
    }

    /// <summary>When Bolt-E crashes, Big Rusty flies away.</summary>
    public void FlyAway()
    {
        if (!IsBeaten) ChangeState(State.FlyingAway);
    }

    /// <summary>
    /// Bolt-E crashed but has a spare battery: Big Rusty laughs "HA HA HA!", floats back to his spot,
    /// and waits holdSeconds before attacking again, so Bolt-E has time to put himself back together.
    /// (Not while he's dizzy, so he never steals your chance to stomp him!)
    /// </summary>
    public void Taunt(float holdSeconds)
    {
        if (state is State.Entering or State.Hurt or State.GettingAngry or State.Dizzy
                  or State.Exploding or State.Defeated or State.FlyingAway) return;
        holdAttacks = holdSeconds;
        Laughed?.Invoke();
        ChangeState(State.Recovering);
    }

    // ---------- Dad drives Big Rusty ----------

    /// <summary>
    /// Dad (or Mom) takes over! From now on Big Rusty waits for orders (with thinking dots over his head).
    /// The attacks he did by himself before don't count, so Dad's first order is never "TIRED!".
    /// </summary>
    public void TakeControl()
    {
        DriverControlled = true;
        attacksSincePound = 0;
    }

    /// <summary>
    /// Dad asks for a move. It only happens while Big Rusty waits with his thinking dots (otherwise: Busy, "Wait...").
    /// The fair-play rules:
    ///   - Pup-Bot helpers only once he's ANGRY, and the giant laser only once he's FURIOUS (NotYet).
    ///   - After 2 attacks he's TIRED: only a ground pound works (and after a ground pound he's always dizzy,
    ///     so Bolt-E gets his chance to stomp him). Laughing is always allowed, but only once every 2 seconds,
    ///     and only 2 laughs in a row: then he has to do a real move first ("Wait..."). Dad picks one, or after
    ///     3 seconds his own brain does. So laughing over and over can never stop the fight.
    /// </summary>
    public DriveAnswer Request(BossMove move)
    {
        if (state != State.WaitingForDriver) return DriveAnswer.Busy;
        if (move == BossMove.Laugh && (laughCooldown > 0 || laughsSinceRealMove >= LaughsInARow)) return DriveAnswer.Busy;
        if (move == BossMove.Helpers && Phase < 2) return DriveAnswer.NotYet;
        if (move == BossMove.Laser && Phase < 3) return DriveAnswer.NotYet;
        if (IsTired && move is not (BossMove.Pound or BossMove.Laugh)) return DriveAnswer.Tired;
        request = move; // (it happens in _Process, in a moment)
        return DriveAnswer.Ok;
    }

    /// <summary>Does Dad's order.</summary>
    void DoOrder(BossMove order)
    {
        request = null;
        switch (order)
        {
            case BossMove.Fireballs:
                ChangeState(State.Shooting);
                break;
            case BossMove.Helpers:
                ChangeState(State.DroppingHelpers);
                break;
            case BossMove.Laser:
                ChangeState(State.Laser);
                break;
            case BossMove.Pound:
                StartPound();
                break;
            case BossMove.Laugh:
                laughCooldown = 2;     // (seconds before he can laugh again)
                laughsSinceRealMove++; // (after 2 laughs in a row, a real move has to come first)
                ChangeState(State.Laughing);
                break;
        }
    }

    /// <summary>GROUND POUND! (Two in a row when he's FURIOUS.)</summary>
    void StartPound()
    {
        poundsLeft = Phase == 3 ? 2 : 1;
        ChangeState(State.PoundRise);
    }

    void ChangeState(State next)
    {
        state = next;
        stateTime = 0;
        moveFrom = Position;
        request = null; // (an order that hasn't happened yet is forgotten if something else happens first, like Bolt-E crashing)
        // A real move (Dad's pick, or his own brain's): Dad can make him laugh again
        if (next is State.Shooting or State.DroppingHelpers or State.Laser or State.PoundRise) laughsSinceRealMove = 0;
        switch (next)
        {
            case State.Shooting:
                shotsLeft = Phase + 2; // 3, 4, then 5 fireballs
                shotTimer = 0.7f;
                attacksSincePound++;
                break;
            case State.DroppingHelpers:
                helpersLeft = 2;
                helperTimer = 0.5f;
                attacksSincePound++;
                break;
            case State.Laser:
                laserFired = false;
                attacksSincePound++;
                break;
            case State.PoundRise:
                Warned?.Invoke();
                break;
            case State.Laughing:
                Laughed?.Invoke();
                break;
            case State.GettingAngry:
                GotAngry?.Invoke(Phase);
                break;
            case State.Dizzy:
                attacksSincePound = 0; // a ground pound landed: he's not tired any more
                break;
        }
    }

    void StartAttackPlan()
    {
        planStep = 0;
        StartAttack();
    }

    void NextAttack()
    {
        planStep = (planStep + 1) % AttackPlan.Length;
        StartAttack();
    }

    void StartAttack()
    {
        // Dad is driving: wait for his order (with thinking dots) instead of picking the next attack by himself
        if (DriverControlled && !aiTurn)
        {
            ChangeState(State.WaitingForDriver);
            return;
        }
        var attack = AttackPlan[planStep];
        if (attack == State.PoundRise) StartPound();
        else ChangeState(attack);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        time += dt;
        stateTime += dt;
        recoil = Mathf.MoveToward(recoil, 0, dt * 5);
        holdAttacks = Mathf.Max(0, holdAttacks - dt);
        laughCooldown = Mathf.Max(0, laughCooldown - dt);

        // Turn to face Bolt-E (but not in the middle of a laser blast)
        bool canTurn = state is State.Laughing or State.Shooting or State.DroppingHelpers or State.GettingAngry or State.WaitingForDriver
                       || (state == State.Laser && !laserFired);
        if (canTurn) facing = TargetX < Position.X ? -1 : 1;

        switch (state)
        {
            case State.WaitingForDriver:
                // Thinking dots... what will Dad pick? (He hovers at his spot. After a laser he's down low,
                // so first he floats back up smoothly: see Hover.)
                Hover();
                if (request is BossMove order)
                {
                    DoOrder(order);
                }
                else if (stateTime > DriverWaitBeforeAI)
                {
                    // Dad didn't pick anything, so Rusty's own brain picks the next move (the fight never stops).
                    // When he's TIRED, even his own brain has to pick the ground pound.
                    aiTurn = true;
                    if (IsTired) StartPound();
                    else StartAttack();
                    aiTurn = false;
                }
                break;

            case State.Entering:
                Position = moveFrom.Lerp(Home, Smooth(stateTime / 1.5f));
                if (stateTime > 1.5f) ChangeState(State.Laughing);
                break;

            case State.Laughing:
                Hover();
                if (stateTime > 1.6f) StartAttackPlan();
                break;

            case State.Shooting:
                Hover();
                shotTimer -= dt;
                if (shotTimer <= 0)
                {
                    if (shotsLeft > 0)
                    {
                        Fired?.Invoke(Muzzle, facing);
                        recoil = 1;
                        shotsLeft--;
                        shotTimer = TimeBetweenShots;
                    }
                    else
                    {
                        NextAttack();
                    }
                }
                break;

            case State.DroppingHelpers:
                Hover();
                helperTimer -= dt;
                if (helperTimer <= 0)
                {
                    if (helpersLeft > 0)
                    {
                        HelperDropped?.Invoke(Position + new Vector2(0, -20));
                        helpersLeft--;
                        helperTimer = helpersLeft > 0 ? 0.9f : 2.5f; // after the last one, give Bolt-E time to deal with them
                    }
                    else
                    {
                        NextAttack();
                    }
                }
                break;

            case State.Laser:
                // Float down low, then blast a laser straight across at head height
                Position = moveFrom.Lerp(new Vector2(Home.X, GroundY - 50), Smooth(stateTime / 0.7f));
                if (!laserFired && stateTime > 0.8f)
                {
                    laserFired = true;
                    LaserFired?.Invoke(Muzzle, facing);
                }
                if (stateTime > 0.8f + global::Laser.TotalTime + 0.3f) NextAttack();
                break;

            case State.PoundRise:
                // A short wind-up, then zoom up off the top of the screen
                Position = moveFrom.Lerp(new Vector2(moveFrom.X, -420), Smooth((stateTime - 0.25f) / 0.55f));
                if (stateTime > 0.8f) ChangeState(State.PoundAim);
                break;

            case State.PoundAim:
                // His shadow follows Bolt-E... then stops just before he slams down. Get out of the way!
                if (stateTime < 1.0f)
                {
                    float aimX = Mathf.Clamp(TargetX, 270, 1030); // never too close to the edges, so there is room to escape
                    Position = new Vector2(Mathf.Lerp(Position.X, aimX, dt * 4f), -420);
                }
                if (stateTime > 1.4f) ChangeState(State.PoundSlam);
                break;

            case State.PoundSlam:
                Position += new Vector2(0, 2300 * dt);
                if (Position.Y >= GroundY)
                {
                    Position = new Vector2(Position.X, GroundY);
                    Slammed?.Invoke(Position.X);
                    poundsLeft--;
                    ChangeState(poundsLeft > 0 ? State.PoundRise : State.Dizzy); // FURIOUS: pound again!
                }
                break;

            case State.Dizzy:
                if (stateTime > DizzyTime) ChangeState(State.Recovering);
                break;

            case State.Recovering:
                Position = moveFrom.Lerp(Home, Smooth(stateTime / 1.1f));
                if (stateTime > 1.1f)
                {
                    if (holdAttacks > 0) Hover(); // waiting for Bolt-E to be back together
                    else StartAttackPlan();
                }
                break;

            case State.Hurt:
                // Fly back up to his spot, blinking
                Position = moveFrom.Lerp(Home, Smooth(stateTime / 1.1f));
                if (stateTime > 1.1f)
                {
                    if (Phase != angerShown)
                    {
                        angerShown = Phase;
                        ChangeState(State.GettingAngry);
                    }
                    else
                    {
                        StartAttackPlan();
                    }
                }
                break;

            case State.GettingAngry:
                Position = Home + new Vector2(Mathf.Sin(time * 70) * 6, 0); // shaking with rage
                if (stateTime > 1.5f) StartAttackPlan();
                break;

            case State.FlyingAway:
                Position += new Vector2(250, -450) * dt;
                break;

            case State.Exploding:
                // Shaking and popping before the big boom
                Position = moveFrom + new Vector2(Mathf.Sin(time * 80) * 5, 0);
                popTimer -= dt;
                if (popTimer <= 0)
                {
                    popTimer = 0.17f;
                    var box = Hitbox;
                    Popped?.Invoke(box.Position + new Vector2((float)GD.RandRange(0, box.Size.X), (float)GD.RandRange(0, box.Size.Y)));
                }
                if (stateTime > 1.6f) BlowUp();
                break;

            case State.Defeated:
                explosion?.Update(dt, WorldSpeed / Size);
                break;
        }

        // Size, which way he faces, and squished when dizzy
        float squish = state == State.Dizzy ? 1.1f : 1f;
        Scale = new Vector2(Size * squish * (facing < 0 ? 1 : -1), Size * Squash);

        // Blink while hurt; flash while breaking down; a bit red when FURIOUS
        bool blink = (state == State.Hurt && (int)(stateTime * 12) % 2 == 0) || (state == State.Exploding && (int)(stateTime * 16) % 2 == 0);
        Modulate = blink ? new Color(1, 1, 1, 0.35f) : Phase == 3 && !IsBeaten ? new Color(1f, 0.88f, 0.85f) : Colors.White;
        QueueRedraw();
    }

    const float FloatBackSeconds = 0.6f; // how long he takes to float back up to his spot (for example after a laser)

    /// <summary>
    /// He floats at his spot, bobbing up and down. If a move starts while he's somewhere else (like down low after a
    /// laser, when Dad quickly picks fireballs, helpers or a laugh), he first floats there smoothly instead of jumping
    /// there in one go. (When he's already at his spot, you can't see the difference.)
    /// </summary>
    void Hover()
    {
        var spot = Home + new Vector2(0, Mathf.Sin(time * 2.5f) * 12);
        Position = stateTime < FloatBackSeconds ? moveFrom.Lerp(spot, Smooth(stateTime / FloatBackSeconds)) : spot;
    }

    /// <summary>Makes movement start and stop gently instead of jerking.</summary>
    static float Smooth(float t)
    {
        t = Mathf.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }

    void BlowUp()
    {
        state = State.Defeated;
        stateTime = 0;
        Scale = new Vector2(Size * (facing < 0 ? 1 : -1), Size);
        var parts = new List<RobotPart>
        {
            new RobotPart("hover disc", new Vector2(0, -12), 10, () => DrawHoverDisc(glow: false), FlatEvery: Mathf.Pi),
            new RobotPart("claw arm", new Vector2(40, -48), 18, DrawClawArm),
            new RobotPart("body", new Vector2(0, -58), 40, DrawBody),
            new RobotPart("head", new Vector2(0, -122), 36, DrawHead),
            new RobotPart("blaster", new Vector2(-68, -71), 12, DrawBlasterArm, FlatEvery: Mathf.Pi),
        };
        // A party hat or a bobble hat flies off his head as its own piece! (Sunglasses and the helmet stay on.)
        // While Dad drives him he wears Dad's blue cap instead of his outfit, so the cap flies off.
        if (DriverControlled)
        {
            capOnHead = false;
            parts.Insert(4, new RobotPart("cap", new Vector2(-8, -150), 12, DrawDriverCap, FlatEvery: Mathf.Pi)); // (drawn just after the head)
        }
        else if (Outfit is BossOutfit.PartyHat or BossOutfit.BobbleHat)
        {
            outfitOnHead = false;
            parts.Insert(4, new RobotPart("hat", new Vector2(0, -170), 20, DrawOutfit)); // (drawn just after the head)
        }
        explosion = new RobotExplosion(parts, new Vector2(0, -75), 0, (GroundY - Position.Y) / Size, Orange,
                                       onBounce: bigPiece => PieceBounced?.Invoke(bigPiece));
        BlewUp?.Invoke();
    }

    // ---------- Drawing (everything is drawn at size 1, facing left; Scale makes it big and turns it around) ----------

    public override void _Draw()
    {
        // (The self-test checks what was drawn: these get set again while drawing)
        CapDrawn = OutfitDrawn = false;
        DotsDrawn = 0;

        if (explosion is not null)
        {
            explosion.Draw(this);
            return;
        }

        DrawShadow();

        // Back to front
        DrawHoverDisc();
        DrawClawArm();
        DrawBody();
        DrawHead();
        DrawBlasterArm();
        DrawMood();
        if (state == State.WaitingForDriver) DrawThinkingDots(); // waiting for Dad's order

        if (state == State.PoundRise)
        {
            var font = ThemeDB.FallbackFont;
            var at = new Vector2(-30, -168 - Mathf.Abs(Mathf.Sin(time * 10)) * 8);
            DrawStringOutline(font, at, "!", HorizontalAlignment.Center, 60, 64, 10, Colors.White);
            DrawString(font, at, "!", HorizontalAlignment.Center, 60, 64, new Color(0.95f, 0.15f, 0.15f));
        }
    }

    void DrawShadow()
    {
        // The shadow is drawn on the ground, however high he is
        float height = (GroundY - Position.Y) / Scale.Y;
        if (state is State.PoundAim or State.PoundSlam)
        {
            // A big target shadow so you know where he'll land. It turns red right before he drops!
            bool locked = state == State.PoundSlam || stateTime > 1.0f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(time * 20);
            Shapes.Ellipse(this, new Vector2(0, height), new Vector2(56, 11), new Color(0, 0, 0, 0.35f));
            if (locked)
                Shapes.Ellipse(this, new Vector2(0, height), new Vector2(60, 13), new Color(1f, 0.15f, 0.1f, 0.25f + 0.25f * pulse));
            return;
        }
        float shadow = Mathf.Clamp(1f - height / 500f, 0.4f, 1f);
        Shapes.Ellipse(this, new Vector2(0, height), new Vector2(50 * shadow, 9 * shadow), new Color(0, 0, 0, 0.18f));
    }

    void DrawHoverDisc(bool glow = true)
    {
        if (glow && state != State.Dizzy)
        {
            float pulse = 0.7f + 0.3f * Mathf.Sin(time * 9f);
            Shapes.Ellipse(this, new Vector2(0, -2), new Vector2(44, 7), new Color(1f, 0.6f, 0.15f, 0.45f * pulse));
        }
        Shapes.Ellipse(this, new Vector2(0, -9), new Vector2(52, 10), Brown.Darkened(0.25f));
        Shapes.Ellipse(this, new Vector2(0, -14), new Vector2(48, 7), Brown);
    }

    void DrawClawArm()
    {
        // His back arm: a grabby claw
        DrawCircle(new Vector2(36, -72), 9, DarkOrange);
        Shapes.RoundRect(this, new Rect2(30, -70, 20, 36), 7, Orange);
        Shapes.RoundRect(this, new Rect2(29, -56, 22, 6), 2, Brown);
        DrawArc(new Vector2(41, -24), 11, Mathf.Pi * 0.3f, Mathf.Pi * 1.7f, 16, Brown, 7, true);
    }

    void DrawBody()
    {
        // Egg-shaped body
        Shapes.Ellipse(this, new Vector2(0, -58), new Vector2(40, 44), Orange);
        Shapes.Ellipse(this, new Vector2(-14, -74), new Vector2(12, 9), new Color(1, 1, 1, 0.22f)); // shine

        // Battle scratches
        DrawLine(new Vector2(-16, -30), new Vector2(10, -42), Brown, 3, true);
        DrawLine(new Vector2(-12, -25), new Vector2(14, -37), Brown, 2.5f, true);

        // Brown collar with a drip, and a red button
        Shapes.RoundRect(this, new Rect2(-26, -102, 52, 13), 6, Brown);
        DrawColoredPolygon(new[] { new Vector2(-20, -92), new Vector2(12, -92), new Vector2(-4, -72) }, Brown);
        DrawCircle(new Vector2(-21, -84), 5, new Color(0.85f, 0.12f, 0.12f));
        DrawCircle(new Vector2(-22.5f, -85.5f), 1.6f, new Color(1, 1, 1, 0.7f));
    }

    void DrawHead()
    {
        // Ear discs, round head, brown stripe on top
        DrawCircle(new Vector2(-35, -120), 9, DarkOrange);
        DrawCircle(new Vector2(35, -120), 9, DarkOrange);
        DrawCircle(new Vector2(0, -122), 36, Orange);
        DrawArc(new Vector2(0, -122), 32, Mathf.Pi * 1.3f, Mathf.Pi * 1.7f, 12, Brown, 7, true);

        // Black face screen, looking at Bolt-E
        Shapes.RoundRect(this, new Rect2(-30, -142, 52, 40), 18, Screen);
        var leftEye = new Vector2(-16, -122);
        var rightEye = new Vector2(4, -122);
        if (state is State.Hurt or State.Exploding or State.Defeated)
        {
            Shapes.XEye(this, leftEye, 5, EyeYellow);
            Shapes.XEye(this, rightEye, 5, EyeYellow);
        }
        else if (state == State.Dizzy)
        {
            // Spinning spiral eyes
            foreach (var eye in new[] { leftEye, rightEye })
                for (int ring = 0; ring < 3; ring++)
                    DrawArc(eye, 2.5f + ring * 2.2f, time * 9 + ring, time * 9 + ring + Mathf.Pi * 1.4f, 10, EyeYellow, 1.8f, true);
        }
        else
        {
            // Eyes get redder as he gets angrier (and flash when he's about to pound)
            var eyes = Phase switch { 1 => EyeYellow, 2 => new Color(1f, 0.55f, 0.1f), _ => new Color(1f, 0.2f, 0.15f) };
            if (state == State.PoundRise && (int)(time * 10) % 2 == 0) eyes = Colors.White;
            Shapes.AngryEye(this, leftEye, new Vector2(13, 11), eyes, Screen, innerOnRight: true);
            Shapes.AngryEye(this, rightEye, new Vector2(13, 11), eyes, Screen, innerOnRight: false);
        }

        // Dressed up for the world he's in... but while Dad drives him, he wears Dad's blue cap instead
        if (DriverControlled)
        {
            if (capOnHead) DrawDriverCap();
        }
        else if (outfitOnHead)
        {
            DrawOutfit();
        }
    }

    // ---- Dad's cap and the thinking dots (try changing the color!) ----
    static readonly Color CapBlue = new(0.2f, 0.45f, 0.95f);
    static readonly Color DotInk = new(0.13f, 0.18f, 0.32f); // the dark ring around each thinking dot (the menus' ink color)
    static readonly Vector2[] ThinkingDots = { new(-16, -182), new(0, -188), new(16, -182) };

    /// <summary>
    /// Dad's little blue cap with a white star, so everyone can see who's driving Big Rusty. The brim sticks out in front.
    /// (No words on it: when he turns around his whole drawing flips, and the words would be backwards!)
    /// </summary>
    void DrawDriverCap()
    {
        Shapes.RoundRect(this, new Rect2(-30, -162, 60, 22), 10, CapBlue);
        Shapes.RoundRect(this, new Rect2(-46, -146, 30, 8), 4, CapBlue.Darkened(0.2f)); // the brim
        Shapes.Star(this, new Vector2(0, -151), 6, 0, Colors.White);
        CapDrawn = true;
    }

    /// <summary>
    /// Three white thinking dots over his head while he waits for Dad's order. They glow one after another.
    /// Each dot has a dark ring and never fades away completely, so you can see them on every sky: in front of the
    /// bright sun, and in Snowy Peaks' falling snow.
    /// </summary>
    void DrawThinkingDots()
    {
        for (int i = 0; i < ThinkingDots.Length; i++)
        {
            DrawCircle(ThinkingDots[i], 7, DotInk);
            DrawCircle(ThinkingDots[i], 5, new Color(1, 1, 1, 0.6f + 0.4f * Mathf.Sin(6 * time + i)));
        }
        DotsDrawn = ThinkingDots.Length;
    }

    // ---- His outfits for the World Tour (try changing the colors!) ----
    static readonly Color PartyPink = new(1f, 0.4f, 0.7f);
    static readonly Color PartyYellow = new(1f, 0.9f, 0.3f);
    static readonly Color ShadesBlack = new(0.08f, 0.08f, 0.12f);
    static readonly Color BobbleRed = new(0.9f, 0.2f, 0.25f);
    static readonly Color HelmetGlass = new(0.7f, 0.9f, 1f, 0.25f);

    // The party hat's cone, and the bobble hat's woolly half-circle (made once, not every time he's drawn)
    static readonly Vector2[] PartyHatCone = { new(-14, -155), new(14, -155), new(0, -195) };
    static readonly Vector2[] BobbleHatShape = MakeBobbleHat();

    static Vector2[] MakeBobbleHat()
    {
        var hat = new Vector2[13];
        for (int i = 0; i < hat.Length; i++)
        {
            float angle = Mathf.Pi + i * Mathf.Pi / (hat.Length - 1); // from the left side, over the top, to the right side
            hat[i] = new Vector2(0, -140) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 34;
        }
        return hat;
    }

    /// <summary>
    /// Big Rusty dressed up for the world: a party hat in Candy Land, cool sunglasses in Night City,
    /// a bobble hat in Snowy Peaks and a fishbowl space helmet on the Moon.
    /// It's drawn on his head, so it turns around with him.
    /// </summary>
    void DrawOutfit()
    {
        if (Outfit != BossOutfit.None) OutfitDrawn = true; // (for the self-test)
        switch (Outfit)
        {
            case BossOutfit.PartyHat:
                // A pink cone with 2 yellow stripes and a white pom-pom on top
                DrawColoredPolygon(PartyHatCone, PartyPink);
                DrawLine(new Vector2(-9.5f, -168), new Vector2(9.5f, -168), PartyYellow, 3.5f, true);
                DrawLine(new Vector2(-5, -181), new Vector2(5, -181), PartyYellow, 3.5f, true);
                DrawCircle(new Vector2(0, -195), 6, Colors.White);
                break;

            case BossOutfit.Sunglasses:
            {
                // Two dark lenses, a bridge between them, and a white shine. Pushed up on his forehead at some moments.
                float y = SunglassesUp ? -131 - 24 : -131;
                Shapes.RoundRect(this, new Rect2(-32, y, 24, 16), 5, ShadesBlack);
                Shapes.RoundRect(this, new Rect2(-10, y, 24, 16), 5, ShadesBlack);
                DrawLine(new Vector2(-14, y + 3), new Vector2(-4, y + 3), ShadesBlack, 3, true);     // the bridge
                DrawLine(new Vector2(14, y + 4), new Vector2(33, y + 8), ShadesBlack, 3, true);     // the arm, back to his ear
                DrawLine(new Vector2(-27, y + 11), new Vector2(-20, y + 4), new Color(1, 1, 1, 0.75f), 2.5f, true); // shine!
                DrawLine(new Vector2(-5, y + 11), new Vector2(2, y + 4), new Color(1, 1, 1, 0.75f), 2.5f, true);
                break;
            }

            case BossOutfit.BobbleHat:
            {
                // A red woolly half-circle, a white band, and a big white pom-pom
                DrawColoredPolygon(BobbleHatShape, BobbleRed);
                Shapes.RoundRect(this, new Rect2(-36, -146, 72, 10), 5, Colors.White);
                DrawCircle(new Vector2(0, -178), 9, Colors.White);
                break;
            }

            case BossOutfit.SpaceHelmet:
                // A see-through fishbowl over his whole head, with a white rim and a shine
                DrawCircle(new Vector2(0, -122), 50, HelmetGlass);
                DrawArc(new Vector2(0, -122), 50, 0, Mathf.Tau, 48, Colors.White, 3, true);
                DrawArc(new Vector2(0, -122), 41, Mathf.Pi * 1.1f, Mathf.Pi * 1.4f, 10, new Color(1, 1, 1, 0.8f), 5, true);
                break;
        }
    }

    void DrawBlasterArm()
    {
        // His front arm: a blaster pointing at Bolt-E. It glows before each shot!
        float kick = recoil * 10;
        DrawCircle(new Vector2(-36, -72), 10, DarkOrange);
        Shapes.RoundRect(this, new Rect2(-84 + kick, -80, 50, 18), 6, Orange);
        Shapes.RoundRect(this, new Rect2(-70 + kick, -81, 6, 20), 2, Brown);
        Shapes.RoundRect(this, new Rect2(-56 + kick, -81, 6, 20), 2, Brown);
        Shapes.RoundRect(this, new Rect2(-98 + kick, -83, 16, 24), 5, DarkOrange);
        DrawCircle(new Vector2(-98 + kick, -71), 6.5f, Screen);

        float charge = 0;
        if (state == State.Shooting && shotsLeft > 0) charge = Mathf.Clamp(1f - shotTimer / 0.5f, 0, 1);
        if (state == State.Laser && !laserFired) charge = Mathf.Clamp(stateTime / 0.8f, 0, 1);
        if (charge > 0)
        {
            var glow = state == State.Laser ? new Color(1f, 0.2f, 0.2f) : new Color(1f, 0.6f, 0.1f);
            DrawCircle(new Vector2(-100 + kick, -71), 6 + 10 * charge, new Color(glow, 0.4f * charge));
            DrawCircle(new Vector2(-100 + kick, -71), 5 * charge, new Color(1f, 0.95f, 0.5f, charge));
        }
    }

    void DrawMood()
    {
        // Dizzy: stars spinning around his head
        if (state == State.Dizzy)
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = time * 5f + i * Mathf.Tau / 4f;
                Shapes.Star(this, new Vector2(Mathf.Cos(angle) * 44, -168 + Mathf.Sin(angle) * 10), 7, time * 4, new Color(1f, 0.88f, 0.25f));
            }
            return;
        }
        if (IsBeaten || state == State.FlyingAway) return;

        // ANGRY: steam puffs out of his ears
        if (Phase >= 2)
        {
            for (int i = 0; i < 3; i++)
            {
                float rise = (time * 50 + i * 16) % 48;
                var puff = new Color(1, 1, 1, 0.55f * (1 - rise / 48));
                DrawCircle(new Vector2(-40 - rise * 0.3f, -126 - rise), 4 + rise * 0.12f, puff);
                DrawCircle(new Vector2(40 + rise * 0.3f, -126 - rise), 4 + rise * 0.12f, puff);
            }
        }

        // FURIOUS: crackly sparks flying off him
        if (Phase == 3)
        {
            var random = new Random((int)(time * 12));
            for (int i = 0; i < 4; i++)
            {
                var at = new Vector2(random.Next(-40, 40), random.Next(-150, -20));
                var line = Vector2.Right.Rotated(random.NextSingle() * Mathf.Tau) * 7;
                DrawLine(at - line, at + line, new Color(1f, 0.95f, 0.4f), 2, true);
            }
        }
    }
}
