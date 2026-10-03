using Godot;

/// <summary>
/// Our hero: Bolt-E, a little robot with a screen face and purple headphones, riding a hoverboard.
/// (This look came from the picture in hero-robot.png!)
/// The robot's "feet" (the bottom of the hoverboard) are at its Position.
/// </summary>
public partial class Robot : Node2D
{
    // ---- Physics numbers (try changing these!) ----
    public const float Gravity = 2600f;        // how fast the robot falls
    public const float JumpSpeed = 1050f;      // how strong a jump is
    public const int MaxJumps = 2;             // 2 = double jump!

    // ---- Colors (try changing these too!) ----
    public Color BodyColor = new(0.23f, 0.24f, 0.3f);        // dark grey head and legs
    public Color BezelColor = new(0.66f, 0.68f, 0.75f);      // silver ring around the face screen
    public Color ScreenColor = new(0.04f, 0.05f, 0.07f);     // black face screen
    public Color EyeColor = new(0.35f, 0.95f, 0.95f);        // glowing cyan eyes
    public Color HeadphoneColor = new(0.4f, 0.35f, 0.7f);    // purple headphones and feet
    public Color AccentColor = new(0.3f, 0.92f, 0.85f);      // teal stripes and lights
    public Color BoardColor = new(0.2f, 0.21f, 0.26f);       // the hoverboard

    public float GroundY;
    public bool OnGround = true;
    public bool Crashed { get; private set; }

    /// <summary>The box we use to check if the robot touched something (a bit smaller than the drawing, to be fair).</summary>
    public Rect2 Hitbox => new(Position + new Vector2(-28, -128), new Vector2(56, 120));

    float velocityY;
    int jumpsLeft = MaxJumps;
    float time;
    float squash = 1f;           // 1 = normal, <1 = squished, >1 = stretched
    float blinkTimer = 2f;
    float happyTimer;
    float tilt;
    float spin;
    float legTuck;               // 0 = standing, 1 = knees pulled up in a jump

    // Music notes floating out of the headphones
    class Note
    {
        public Vector2 Position; // in the same space as the robot's Position
        public float Life;
        public bool Purple;
    }
    readonly List<Note> notes = new();
    float noteTimer = 1f;

    RobotExplosion? explosion; // not null after we crash and blow apart

    /// <summary>Where the robot tilts and spins around (about the middle of its body).</summary>
    static readonly Vector2 Pivot = new(0, -60);

    public float RunSpeed { get; set; }

    // ---- Riding left and right (only during boss fights) ----
    public bool CanMove;            // true during a boss fight
    public float MoveInput;         // -1 = left, 0 = stay, 1 = right (from the controls)
    public float HomeX = 240;       // where Bolt-E normally rides
    const float RideSpeed = 430f;   // how fast it rides left and right
    const float RideAcceleration = 2800f;
    float velocityX;
    float facing = 1;               // 1 = facing right, -1 = facing left

    /// <summary>How fast Bolt-E is going: the world scrolling by, or riding around in a boss fight.</summary>
    float Speed => Mathf.Max(RunSpeed, Mathf.Abs(velocityX));

    /// <summary>True while coming down from a jump (that's when you can stomp things).</summary>
    public bool IsFalling => !OnGround && !Crashed && velocityY > 0;

    /// <summary>Where the robot's feet were one moment ago (so we can tell if it came down from above).</summary>
    public float LastFeetY { get; private set; }

    /// <summary>True right after the second jump in the air (the flip!).</summary>
    public bool DidDoubleJump => jumpsLeft == 0;

    /// <summary>Happens when a piece hits the ground after blowing up (true = a big part, false = a little gear or screw).</summary>
    public event Action<bool>? PieceBounced;

    public void Reset()
    {
        Crashed = false;
        velocityY = 0;
        velocityX = 0;
        facing = 1;
        CanMove = false;
        Scale = Vector2.One;
        Position = new Vector2(HomeX, GroundY);
        OnGround = true;
        jumpsLeft = MaxJumps;
        tilt = 0;
        spin = 0;
        squash = 1f;
        explosion = null;
    }

    /// <summary>Returns true if the robot actually jumped.</summary>
    public bool Jump()
    {
        if (Crashed || jumpsLeft <= 0) return false;
        velocityY = -JumpSpeed * (jumpsLeft == MaxJumps ? 1f : 0.85f);
        jumpsLeft--;
        OnGround = false;
        squash = 1.3f;
        if (jumpsLeft == 0) spin = Mathf.Tau; // do a flip on the double jump!
        return true;
    }

    /// <summary>Boing! Bounce up off an enemy's head. Holding jump bounces higher.</summary>
    public void Bounce(bool high)
    {
        velocityY = high ? -1150f : -800f;
        OnGround = false;
        jumpsLeft = 1; // you get one more jump in the air after a bounce
        squash = 1.3f;
        BeHappy();
    }

    /// <summary>Letting go of jump early makes a small hop.</summary>
    public void ReleaseJump()
    {
        if (velocityY < -400f) velocityY = -400f;
    }

    public void BeHappy() => happyTimer = 0.6f;

    /// <summary>BONK! Bolt-E blows apart into pieces (don't worry, it's put back together for the next game).</summary>
    public void Crash()
    {
        Crashed = true;
        var parts = new[]
        {
            // Back to front, the same order the robot is normally drawn in.
            new RobotPart("hoverboard", new Vector2(0, -16), 9, () => DrawHoverboard(glow: false), FlatEvery: Mathf.Pi),
            new RobotPart("back leg", new Vector2(-14, -46), 20, () => DrawLeg(-14, BodyColor.Darkened(0.3f))),
            new RobotPart("front leg", new Vector2(12, -46), 20, () => DrawLeg(12, BodyColor)),
            new RobotPart("headband", new Vector2(2, -119), 24, DrawHeadband, FlatEvery: Mathf.Pi), // curved, so it only lies on its top or bottom
            new RobotPart("back ear cup", new Vector2(-39, -97), 14, DrawBackEarCup),
            new RobotPart("head", new Vector2(0, -96), 34, DrawHead),
            new RobotPart("front ear cup", new Vector2(41, -97), 15, DrawFrontEarCup),
        };
        explosion = new RobotExplosion(parts, Pivot, tilt - spin, GroundY - Position.Y, AccentColor,
                                       onBounce: bigPart => PieceBounced?.Invoke(bigPart));
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        time += dt;

        if (explosion is not null)
        {
            explosion.Update(dt, RunSpeed);
            UpdateNotes(dt);
            QueueRedraw();
            return;
        }

        LastFeetY = Position.Y;

        // Gravity pulls the robot down.
        if (!OnGround)
        {
            velocityY += Gravity * dt;
            Position += new Vector2(0, velocityY * dt);
            if (Position.Y >= GroundY)
            {
                Position = new Vector2(Position.X, GroundY);
                squash = 0.7f; // squish on landing
                OnGround = true;
                velocityY = 0;
                jumpsLeft = MaxJumps;
            }
        }

        RideLeftAndRight(dt);
        float lean = velocityX * facing / 5000f; // lean forward when riding
        tilt = Mathf.Lerp(tilt, (OnGround ? 0.05f : -velocityY / 6000f) + lean, dt * 10f);

        spin = Mathf.MoveToward(spin, 0, dt * 14f);
        squash = Mathf.Lerp(squash, 1f, dt * 12f);
        legTuck = Mathf.Lerp(legTuck, OnGround ? 0f : 1f, dt * 12f);
        happyTimer -= dt;

        blinkTimer -= dt;
        if (blinkTimer < -0.12f) blinkTimer = (float)GD.RandRange(1.5, 4.0);

        UpdateNotes(dt);
        QueueRedraw();
    }

    void RideLeftAndRight(float dt)
    {
        if (CanMove)
        {
            velocityX = Mathf.MoveToward(velocityX, MoveInput * RideSpeed, RideAcceleration * dt);
            if (MoveInput > 0.2f) facing = 1;
            else if (MoveInput < -0.2f) facing = -1;
        }
        else
        {
            // Not in a boss fight: glide back to the normal spot, facing right
            float toHome = HomeX - Position.X;
            velocityX = Mathf.Abs(toHome) < 2 ? 0 : Mathf.Sign(toHome) * Mathf.Min(320f, Mathf.Abs(toHome) * 4f);
            facing = 1;
        }
        Position = new Vector2(Mathf.Clamp(Position.X + velocityX * dt, 70, 1210), Position.Y);
        Scale = new Vector2(facing, 1); // flip the drawing to face the other way
    }

    void UpdateNotes(float dt)
    {
        // Bolt-E is listening to music! Every so often a note pops out of the headphones.
        if (!Crashed && Speed > 0)
        {
            noteTimer -= dt;
            if (noteTimer <= 0)
            {
                noteTimer = (float)GD.RandRange(0.8, 1.6);
                notes.Add(new Note { Position = Position + new Vector2(46, -112), Life = 1.5f, Purple = GD.Randf() < 0.5f });
            }
        }

        for (int i = notes.Count - 1; i >= 0; i--)
        {
            var note = notes[i];
            note.Life -= dt;
            if (note.Life <= 0) { notes.RemoveAt(i); continue; }
            // Float up, wiggle, and drift backwards as we zoom past.
            note.Position += new Vector2(-RunSpeed * 0.35f - 20f + Mathf.Sin(note.Life * 8f) * 40f, -50f) * dt;
        }
    }

    public override void _Draw()
    {
        if (explosion is not null)
        {
            explosion.Draw(this);
            DrawNotes();
            return;
        }

        // A little shadow on the ground (it stays on the ground while we jump).
        float heightAboveGround = GroundY - Position.Y;
        float shadowSize = Mathf.Clamp(1f - heightAboveGround / 400f, 0.3f, 1f);
        DrawSetTransform(new Vector2(0, heightAboveGround), 0, new Vector2(shadowSize, 0.2f * shadowSize));
        DrawCircle(Vector2.Zero, 45, new Color(0, 0, 0, 0.18f));

        // Everything else gets squished, tilted, and spun around the robot's middle.
        bool riding = OnGround && !Crashed && Speed > 0;
        float hover = riding ? Mathf.Sin(time * 5f) * 2f - 2f : 0f; // gently floating on the hoverboard
        var scale = new Vector2(2f - squash, squash);
        float angle = tilt - spin;
        var scaledPivot = Pivot * scale; // squash around the feet, spin around the middle
        var offset = scaledPivot - scaledPivot.Rotated(angle) + new Vector2(0, hover);
        DrawSetTransform(offset, angle, scale);

        // Draw from back to front: things drawn later cover things drawn earlier.
        DrawHoverboard();
        DrawLeg(-14, BodyColor.Darkened(0.3f)); // back leg (darker, it's in the shadow)
        DrawLeg(12, BodyColor);                 // front leg
        DrawHeadband();
        DrawBackEarCup();
        DrawHead();
        DrawFrontEarCup();

        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        DrawNotes();
    }

    void DrawHoverboard(bool glow = true)
    {
        // Glowing light underneath (it pulses!)
        if (glow)
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(time * 8f);
            Shapes.RoundRect(this, new Rect2(-42, -12, 84, 11), 5.5f, new Color(AccentColor, 0.35f * pulse));
            Shapes.RoundRect(this, new Rect2(-32, -10, 64, 7), 3.5f, new Color(AccentColor, 0.9f * pulse));
        }

        // The board: a darker bottom, the deck on top, and a shiny silver edge
        Shapes.RoundRect(this, new Rect2(-44, -18, 88, 10), 5, BoardColor.Darkened(0.35f));
        Shapes.RoundRect(this, new Rect2(-50, -25, 100, 10), 5, BoardColor);
        DrawLine(new Vector2(-45, -24), new Vector2(45, -24), BezelColor, 2f, true);
        DrawCircle(new Vector2(-40, -20), 1.6f, AccentColor); // tiny power light
    }

    void DrawLeg(float x, Color color)
    {
        // A bendy leg: foot -> ankle -> knee -> hip (the hip is hidden under the head).
        // When jumping, the knee pokes forward and up (a "tuck").
        var ankle = new Vector2(x, -36);
        var knee = new Vector2(x + 9 + legTuck * 9, -52 + legTuck * 4);
        var hip = new Vector2(x - 2, -70);

        Shapes.RoundRect(this, new Rect2(x - 16, -29, 32, 6), 3, HeadphoneColor); // purple sole
        Shapes.RoundRect(this, new Rect2(x - 15, -40, 30, 14), 6, color);          // foot block

        DrawLine(ankle, knee, color, 12f, true);
        DrawLine(knee, hip, color, 12f, true);
        DrawCircle(knee, 6f, color);
        DrawCircle(ankle, 6f, color);

        // A little purple light on the shin
        var light = ankle.Lerp(knee, 0.4f);
        DrawLine(light + new Vector2(0, -2.5f), light + new Vector2(0, 2.5f), HeadphoneColor, 2.5f, true);
    }

    void DrawHeadband()
    {
        // The headband goes over the top of the head. Most of it hides behind the head.
        var center = new Vector2(2, -98);
        DrawArc(center, 42, Mathf.Pi, Mathf.Tau, 28, HeadphoneColor, 7f, true);
        DrawArc(center, 42, Mathf.Pi * 1.22f, Mathf.Pi * 1.3f, 4, AccentColor, 7f, true); // teal stripes
        DrawArc(center, 42, Mathf.Pi * 1.7f, Mathf.Pi * 1.78f, 4, AccentColor, 7f, true);
    }

    void DrawBackEarCup()
    {
        // The far ear cup, peeking out on the left
        Shapes.RoundRect(this, new Rect2(-46, -113, 14, 32), 7, HeadphoneColor.Darkened(0.3f));
    }

    void DrawFrontEarCup()
    {
        // The near ear cup, with a glowing teal ring that pulses to the beat
        var cup = new Vector2(41, -97);
        float beat = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(time * 4f));
        Shapes.RoundRect(this, new Rect2(cup - new Vector2(9, 18), new Vector2(18, 36)), 9, HeadphoneColor);
        DrawCircle(cup, 11f, new Color(AccentColor, 0.2f * beat));
        DrawCircle(cup, 8.5f, new Color(AccentColor, beat), false, 2.5f, true);
        DrawCircle(cup, 4.5f, HeadphoneColor.Lightened(0.25f));
    }

    void DrawHead()
    {
        // Rounded square head -> silver ring -> black screen
        Shapes.RoundRect(this, new Rect2(-36, -130, 72, 68), 20, BodyColor);
        Shapes.RoundRect(this, new Rect2(-31, -125, 62, 56), 16, BezelColor);
        Shapes.RoundRect(this, new Rect2(-27, -121, 54, 48), 13, ScreenColor);

        // Tiny camera at the top, and a speaker slot at the bottom
        DrawCircle(new Vector2(0, -123), 2.2f, ScreenColor);
        DrawCircle(new Vector2(-0.7f, -123.7f), 0.8f, new Color(0.5f, 0.5f, 0.6f));
        Shapes.RoundRect(this, new Rect2(-6, -67, 12, 3), 1.5f, ScreenColor);

        DrawFace();
    }

    void DrawFace()
    {
        // The eyes sit a little to the right, because Bolt-E is looking where it's going.
        var leftEye = new Vector2(-10, -97);
        var rightEye = new Vector2(12, -97);

        if (Crashed)
        {
            Shapes.XEye(this, leftEye, 6, EyeColor);
            Shapes.XEye(this, rightEye, 6, EyeColor);
        }
        else if (happyTimer > 0 || !OnGround)
        {
            Shapes.HappyEye(this, leftEye, 8, EyeColor, 5f);
            Shapes.HappyEye(this, rightEye, 8, EyeColor, 5f);
        }
        else if (blinkTimer < 0)
        {
            SquareEye(leftEye, 15, 3);
            SquareEye(rightEye, 15, 3);
        }
        else
        {
            SquareEye(leftEye, 13, 15);
            SquareEye(rightEye, 13, 15);
        }
    }

    /// <summary>A glowing, rounded-square eye.</summary>
    void SquareEye(Vector2 center, float width, float height)
    {
        var size = new Vector2(width, height);
        Shapes.RoundRect(this, new Rect2(center - size / 2 - new Vector2(4, 4), size + new Vector2(8, 8)), 7, new Color(EyeColor, 0.12f));
        Shapes.RoundRect(this, new Rect2(center - size / 2 - new Vector2(2, 2), size + new Vector2(4, 4)), 5, new Color(EyeColor, 0.22f));
        Shapes.RoundRect(this, new Rect2(center - size / 2, size), 4, EyeColor);
    }

    void DrawNotes()
    {
        foreach (var note in notes)
        {
            var at = note.Position - Position; // notes live in the world, so undo the robot's own position
            at.X *= facing;                    // ...and undo the flip when facing left
            var color = new Color(note.Purple ? HeadphoneColor.Lightened(0.35f) : AccentColor, Mathf.Clamp(note.Life, 0, 1));
            DrawCircle(at, 5f, color);
            DrawLine(at + new Vector2(4.5f, 0), at + new Vector2(4.5f, -18), color, 2.5f, true);
            DrawLine(at + new Vector2(4.5f, -18), at + new Vector2(11, -12), color, 2.5f, true);
        }
    }
}
