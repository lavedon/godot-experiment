using Godot;

/// <summary>
/// BIG RUSTY, the boss! (from enemy-boss-who-can-shoot.png)
/// When he shows up, the world stops and Bolt-E can ride left and right: it's an arena fight!
/// He has 6 health and gets angrier as he gets hurt:
///   Phase 1:            shoots bouncing fireballs, then does a GROUND POUND.
///   Phase 2 (ANGRY):    more fireballs, drops Pup-Bot helpers, then a ground pound.
///   Phase 3 (FURIOUS):  lots of fireballs, a giant laser (stay on the ground!), then TWO ground pounds.
/// After a ground pound he's dizzy for a moment: that's when you jump on his head!
/// Position is the bottom-middle of his hover disc.
/// </summary>
public partial class Boss : Node2D
{
    public const string DisplayName = "BIG RUSTY";
    public const int MaxHealth = 6;
    public const float Size = 1.5f; // how big he is (1 = the size of the drawing below)

    enum State
    {
        Entering, Laughing, Shooting, DroppingHelpers, Laser, PoundRise, PoundAim, PoundSlam,
        Dizzy, Recovering, Hurt, GettingAngry, FlyingAway, Exploding, Defeated,
    }

    public float GroundY;
    public float WorldSpeed;   // so his pieces slide along with the ground after he blows up
    public float TargetX;      // where Bolt-E is, so he can aim at it
    public int Health { get; private set; } = MaxHealth;

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
                                     or State.PoundSlam or State.GettingAngry;
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
        Health--;
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

    void ChangeState(State next)
    {
        state = next;
        stateTime = 0;
        moveFrom = Position;
        switch (next)
        {
            case State.Shooting:
                shotsLeft = Phase + 2; // 3, 4, then 5 fireballs
                shotTimer = 0.7f;
                break;
            case State.DroppingHelpers:
                helpersLeft = 2;
                helperTimer = 0.5f;
                break;
            case State.Laser:
                laserFired = false;
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
        var attack = AttackPlan[planStep];
        if (attack == State.PoundRise) poundsLeft = Phase == 3 ? 2 : 1;
        ChangeState(attack);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        time += dt;
        stateTime += dt;
        recoil = Mathf.MoveToward(recoil, 0, dt * 5);

        // Turn to face Bolt-E (but not in the middle of a laser blast)
        bool canTurn = state is State.Laughing or State.Shooting or State.DroppingHelpers or State.GettingAngry
                       || (state == State.Laser && !laserFired);
        if (canTurn) facing = TargetX < Position.X ? -1 : 1;

        switch (state)
        {
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
                if (stateTime > 1.1f) StartAttackPlan();
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

    void Hover() => Position = Home + new Vector2(0, Mathf.Sin(time * 2.5f) * 12);

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
        var parts = new[]
        {
            new RobotPart("hover disc", new Vector2(0, -12), 10, () => DrawHoverDisc(glow: false), FlatEvery: Mathf.Pi),
            new RobotPart("claw arm", new Vector2(40, -48), 18, DrawClawArm),
            new RobotPart("body", new Vector2(0, -58), 40, DrawBody),
            new RobotPart("head", new Vector2(0, -122), 36, DrawHead),
            new RobotPart("blaster", new Vector2(-68, -71), 12, DrawBlasterArm, FlatEvery: Mathf.Pi),
        };
        explosion = new RobotExplosion(parts, new Vector2(0, -75), 0, (GroundY - Position.Y) / Size, Orange,
                                       onBounce: bigPiece => PieceBounced?.Invoke(bigPiece));
        BlewUp?.Invoke();
    }

    // ---------- Drawing (everything is drawn at size 1, facing left; Scale makes it big and turns it around) ----------

    public override void _Draw()
    {
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
