using Godot;

/// <summary>Special faces Bolt-E can show on his screen for a moment (see Robot.ShowFace).</summary>
public enum RobotFace
{
    Normal,   // his usual eyes
    Booting,  // three loading dots, then the eyes open (after the magic rebuild)
    Hearts,   // pink heart eyes (he LOVES the bolt magnet!)
    Stars,    // spinning gold star eyes (he zoomed past somebody's flag, or his own record!)
}

/// <summary>
/// Our hero: Bolt-E, a little robot with a screen face and purple headphones, riding a hoverboard.
/// (This look came from the picture in hero-robot.png!) He can wear other looks too (see Looks.cs):
/// other colors, hats, boards (a skateboard, a hot dog, a rainbow, a cloud) and trails.
/// The robot's "feet" (the bottom of the hoverboard) are at its Position.
/// </summary>
public partial class Robot : Node2D
{
    // ---- Physics numbers (try changing these!) ----
    public const float Gravity = 2600f;        // how fast the robot falls
    public const float JumpSpeed = 1050f;      // how strong a jump is
    public const int MaxJumps = 2;             // 2 = double jump!

    // ---- Colors (the look he's wearing sets these. Invent your own look in Looks.cs!) ----
    public Color BodyColor = Looks.Classic.Body;              // head and legs
    public Color BezelColor = Looks.Classic.Bezel;            // the ring around the face screen
    public Color ScreenColor = Looks.Classic.Screen;          // the face screen
    public Color EyeColor = Looks.Classic.Eyes;               // glowing eyes
    public Color HeadphoneColor = Looks.Classic.Headphones;   // headphones and feet
    public Color AccentColor = Looks.Classic.Accent;          // stripes and lights
    public Color BoardColor = Looks.Classic.BoardColor;       // the hoverboard

    // ---- The rest of his look (see Looks.cs) ----
    public Hat Hat;                  // what he wears on his head
    public Board BoardStyle;         // what he rides on: a hoverboard, a skateboard, a hot dog...
    public Trail TrailStyle;         // what floats out behind him: sparkles, hearts or a rainbow
    public bool RainbowEyes;         // eyes that slowly change through every color

    /// <summary>
    /// A light-colored body gets a thin dark outline around his head and board, so he's easy to see on snow and candy.
    /// (The classic dark grey body has brightness 0.24, so it doesn't need one.)
    /// </summary>
    public bool Rim => Worlds.Brightness(BodyColor) > 0.55f;

    // ---- The look things drawn the last time he was drawn (the self-test checks these) ----
    public Hat? HatDrawn { get; private set; }
    public Board? BoardDrawn { get; private set; }
    public Trail? TrailDrawn { get; private set; }
    public bool RimDrawn { get; private set; }

    public float GroundY;
    public bool OnGround = true;
    public bool Crashed { get; private set; }

    /// <summary>
    /// How strong gravity is in this world: 1 = normal, 0.55 = FLOATY moon jumps (see Worlds.cs).
    /// Jumps get gentler too (by the square root), so Bolt-E jumps just as high, but hangs in the air longer.
    /// (It changes with SetGravityScale.)
    /// </summary>
    public float GravityScale { get; private set; } = 1;

    /// <summary>
    /// Gravity never gets weaker than this, so a Gravity of 0 (or less) in Worlds.cs can't make Bolt-E float away
    /// forever. (Keep a world's Gravity between 0.2 and 2.)
    /// </summary>
    public const float LeastGravity = 0.1f;

    /// <summary>Jump and bounce speeds are multiplied by this, so a jump is always just as high, whatever the gravity.</summary>
    float JumpScale => Mathf.Sqrt(Mathf.Max(LeastGravity, GravityScale));

    /// <summary>
    /// A new world, with its own gravity. If Bolt-E is in the middle of a jump right then, his speed changes to match
    /// (by the square root again), so the rest of the jump still goes just as high as always.
    /// </summary>
    public void SetGravityScale(float scale)
    {
        scale = Mathf.Max(LeastGravity, scale);
        if (!OnGround) velocityY *= Mathf.Sqrt(scale / GravityScale);
        GravityScale = scale;
    }

    /// <summary>
    /// The box we use to check if the robot touched something (a bit smaller than the drawing, to be fair).
    /// It grows with him when he's MEGA BOLT-E.
    /// </summary>
    public Rect2 Hitbox => new(Position + new Vector2(-28, -128) * Size, new Vector2(56, 120) * Size);

    // ---- Powers from the rainbow ? boxes (see PowerUps.cs and Main.PowerUps.cs) ----

    /// <summary>How big Bolt-E is: 1 = normal, 1.8 = MEGA BOLT-E!</summary>
    public float Size { get; private set; } = 1;
    float[] sizeSteps = Array.Empty<float>();  // sizes still to show while growing or shrinking (like Mario!)
    int nextSizeStep;                          // which of those sizes comes next
    float sizeStepTimer;                       // seconds until the next size
    const float SizeStepSeconds = 0.1f;        // each size shows for this long

    /// <summary>True while he zooms along high up on the rocket board.</summary>
    public bool Rocketing { get; private set; }
    /// <summary>True while he floats down on the little parachute after the rocket board.</summary>
    public bool Parachuting { get; private set; }
    float rocketFeetY;                         // how high the rocket board flies (where his feet are)

    /// <summary>Shows the red magnet floating over his head.</summary>
    public bool ShowMagnet;
    /// <summary>The power is almost over: he blinks as a warning (like when BlinkTime is on).</summary>
    public bool PowerWarning;

    static readonly Color MagnetRed = new(0.9f, 0.15f, 0.15f);
    static readonly Color Silver = new(0.85f, 0.87f, 0.92f);
    static readonly Color HeartPink = new(1f, 0.45f, 0.65f);
    static readonly Color StarGold = new(1f, 0.88f, 0.25f);
    static readonly Color Ink = new(0.13f, 0.18f, 0.32f);      // the dark outline color (the same as the menus' ink)

    // ---- Look animations ----
    float propellerAngle;        // the propeller cap spins (faster when he goes faster)
    float wheelSpin;             // the skateboard wheels roll

    // ---- The trail: sparkles, hearts or rainbow bubbles floating out behind him (try changing these!) ----
    const float TrailEvery = 0.05f;   // seconds between two trail bits
    const float TrailLife = 0.6f;     // seconds a trail bit lasts

    class TrailBit
    {
        public Vector2 Position;     // in the same space as the robot's Position (it stays where it was dropped)
        public float Life;
        public Trail Style;          // (so old bits keep their look when he changes looks)
        public bool Gold;            // sparkles take turns: white, gold, white, gold...
    }
    readonly List<TrailBit> trail = new();
    float trailTimer;
    bool nextSparkleGold;

    /// <summary>How many trail bits are floating behind him right now (the self-test reads this).</summary>
    public int TrailBits => trail.Count;
    /// <summary>Where the oldest trail bit is in the world (the self-test checks it drifts away behind him).</summary>
    public Vector2? OldestTrailBit => trail.Count > 0 ? trail[0].Position : null;

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

    /// <summary>
    /// After the magic rebuild Bolt-E flashes for this many seconds, and while he flashes he can't crash.
    /// (Not the same as blinkTimer, which makes his eyes blink!)
    /// </summary>
    public float BlinkTime;
    float rebuildFlash;          // the bright flash when he's back together (seconds left)

    // A special face on the screen for a moment (see ShowFace)
    RobotFace face = RobotFace.Normal;
    float faceTimer;             // seconds left to show it
    float faceAge;               // seconds it has been showing

    /// <summary>Happens when every piece is back together after the magic rebuild.</summary>
    public event Action? Rebuilt;

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

    /// <summary>The special face showing right now (Normal when there isn't one).</summary>
    public RobotFace Face => faceTimer > 0 ? face : RobotFace.Normal;

    /// <summary>True while he's doing a flip (the self-test reads this).</summary>
    public bool Flipping => spin > 0.01f;

    public void Reset()
    {
        Crashed = false;
        velocityY = 0;
        velocityX = 0;
        facing = 1;
        CanMove = false;
        SetSizeNow(1);
        Scale = Vector2.One;
        Position = new Vector2(HomeX, GroundY);
        OnGround = true;
        jumpsLeft = MaxJumps;
        tilt = 0;
        spin = 0;
        squash = 1f;
        explosion = null;
        BlinkTime = 0;
        rebuildFlash = 0;
        face = RobotFace.Normal;
        faceTimer = 0;
        faceAge = 0;
        Rocketing = false;
        Parachuting = false;
        ShowMagnet = false;
        PowerWarning = false;
        Modulate = Colors.White;
        Visible = true;
        trail.Clear();
    }

    /// <summary>True if one of his flying pieces is the part with this name, like "head" or "hat" (the self-test reads this).</summary>
    public bool HasPiece(string name) => explosion?.HasPart(name) == true;

    /// <summary>Returns true if the robot actually jumped. (No jumping on the rocket board or the parachute!)</summary>
    public bool Jump()
    {
        if (Crashed || Rocketing || Parachuting || jumpsLeft <= 0) return false;
        velocityY = -JumpSpeed * (jumpsLeft == MaxJumps ? 1f : 0.85f) * JumpScale;
        jumpsLeft--;
        OnGround = false;
        squash = 1.3f;
        if (jumpsLeft == 0) spin = Mathf.Tau; // do a flip on the double jump!
        return true;
    }

    /// <summary>Boing! Bounce up off an enemy's head. Holding jump bounces higher.</summary>
    public void Bounce(bool high)
    {
        velocityY = (high ? -1150f : -800f) * JumpScale;
        OnGround = false;
        jumpsLeft = 1; // you get one more jump in the air after a bounce
        squash = 1.3f;
        BeHappy();
    }

    /// <summary>Letting go of jump early makes a small hop.</summary>
    public void ReleaseJump()
    {
        float smallHop = -400f * JumpScale;
        if (velocityY < smallHop) velocityY = smallHop;
    }

    public void BeHappy() => happyTimer = 0.6f;

    /// <summary>Do a flip!</summary>
    public void Flip() => spin = Mathf.Tau;

    /// <summary>Shows a special face on the screen for a few seconds.</summary>
    public void ShowFace(RobotFace newFace, float seconds)
    {
        face = newFace;
        faceTimer = seconds;
        faceAge = 0;
    }

    // ---------- Powers: growing and shrinking, the rocket board, the parachute ----------

    /// <summary>MEGA BOLT-E! He grows in steps, like Mario: bigger, smaller, BIGGER... then giant.</summary>
    public void Grow(float targetSize) => StartSizeSteps(1.3f, 1.1f, 1.5f, 1.3f, targetSize);

    /// <summary>Back to normal size, in steps: smaller, bigger, smaller... normal.</summary>
    public void Shrink() => StartSizeSteps(1.4f, 1.6f, 1.2f, 1.4f, 1.0f);

    /// <summary>Changes his size right away (and stops any growing or shrinking steps).</summary>
    public void SetSizeNow(float size)
    {
        sizeSteps = Array.Empty<float>();
        nextSizeStep = 0;
        Size = size;
    }

    void StartSizeSteps(params float[] sizes)
    {
        sizeSteps = sizes;
        nextSizeStep = 0;
        sizeStepTimer = 0; // the first size shows right away
    }

    void UpdateSize(float dt)
    {
        if (nextSizeStep >= sizeSteps.Length) return;
        sizeStepTimer -= dt;
        if (sizeStepTimer > 0) return;
        Size = sizeSteps[nextSizeStep];
        nextSizeStep++;
        sizeStepTimer += SizeStepSeconds;
    }

    /// <summary>ROCKET BOARD! He zooms up until his feet are at feetY and flies along up there (no jumping).</summary>
    public void StartRocket(float feetY)
    {
        Rocketing = true;
        Parachuting = false;
        rocketFeetY = feetY;
        OnGround = false;
        velocityY = 0;
        jumpsLeft = 0;
    }

    /// <summary>The rocket runs out: pop! A little parachute opens and he floats down slowly.</summary>
    public void StartParachute()
    {
        Rocketing = false;
        Parachuting = !OnGround; // (only if he's up in the air)
        velocityY = 0;
    }

    /// <summary>No more rocket or parachute, right away: if he's up high, he just falls down normally.</summary>
    public void StopRocketNow()
    {
        Rocketing = false;
        Parachuting = false;
    }

    /// <summary>BONK! Bolt-E blows apart into pieces (don't worry, it's put back together for the next game).</summary>
    public void Crash()
    {
        // A crash ends every power right away: normal size, no rocket or parachute, no warning blinks
        SetSizeNow(1);
        Scale = new Vector2(facing, 1);
        StopRocketNow();
        PowerWarning = false;
        Modulate = Colors.White; // never see-through in pieces (the explosion doesn't update this)
        Visible = true;          // and never blinking in pieces
        Crashed = true;
        var parts = new List<RobotPart>
        {
            // Back to front, the same order the robot is normally drawn in.
            // (A fluffy cloud board is bigger than a hoverboard, so it bounces on a bigger circle.)
            new RobotPart("hoverboard", new Vector2(0, -16), BoardStyle == Board.Cloud ? 20 : 9, () => DrawHoverboard(glow: false), FlatEvery: Mathf.Pi),
            new RobotPart("back leg", new Vector2(-14, -46), 20, () => DrawLeg(-14, BodyColor.Darkened(0.3f))),
            new RobotPart("front leg", new Vector2(12, -46), 20, () => DrawLeg(12, BodyColor)),
            new RobotPart("headband", new Vector2(2, -119), 24, DrawHeadband, FlatEvery: Mathf.Pi), // curved, so it only lies on its top or bottom
            new RobotPart("back ear cup", new Vector2(-39, -97), 14, DrawBackEarCup),
            new RobotPart("head", new Vector2(0, -96), 34, DrawHead),
            new RobotPart("front ear cup", new Vector2(41, -97), 15, DrawFrontEarCup),
        };
        // His hat flies off as its own bouncing piece! (It flies back on last, together with his head.)
        if (Hat != Hat.None)
        {
            bool helmet = Hat == Hat.SpaceHelmet; // (the space helmet is a big bubble around his whole head)
            parts.Add(new RobotPart("hat", helmet ? new Vector2(0, -98) : new Vector2(0, -150), helmet ? 52 : 22, DrawHat, FlatEvery: Mathf.Pi));
        }
        explosion = new RobotExplosion(parts, Pivot, tilt - spin, GroundY - Position.Y, AccentColor,
                                       onBounce: bigPart => PieceBounced?.Invoke(bigPart));
    }

    /// <summary>The magic rebuild: every piece flies back into place over "seconds" (the head goes last).</summary>
    public void Rebuild(float seconds)
    {
        if (explosion is null || explosion.PuttingBack) return;
        explosion.StartPuttingBackTogether(seconds);
    }

    /// <summary>Every piece is back! Bolt-E stands on the ground again, does a flip, and his face screen boots up.</summary>
    void BackTogether()
    {
        explosion = null;
        Crashed = false;
        Position = new Vector2(Position.X, GroundY); // on the ground, even if he crashed in the air
        OnGround = true;
        velocityY = 0;
        velocityX = 0;
        jumpsLeft = MaxJumps;
        tilt = 0;
        Flip();
        squash = 1.3f;
        rebuildFlash = 0.25f;
        ShowFace(RobotFace.Booting, 0.7f);
        Rebuilt?.Invoke();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        time += dt;
        UpdateLook(dt);

        if (explosion is not null)
        {
            explosion.Update(dt, RunSpeed);
            if (explosion.IsBackTogether) BackTogether();
            UpdateNotes(dt);
            UpdateTrail(dt);
            QueueRedraw();
            return;
        }

        // Flashing (and safe!) for a moment after the magic rebuild. He also flashes when a power is almost over.
        BlinkTime = Mathf.Max(0, BlinkTime - dt);
        bool blinkOff = BlinkTime > 0 && (int)(BlinkTime * 12) % 2 == 0;
        bool warningOff = PowerWarning && (int)(time * 12) % 2 == 0;
        // The blink: like in old arcade games, he hides for a split second, again and again.
        // (Hiding works better than see-through: see-through would show his parts overlapping.)
        Visible = !(blinkOff || warningOff);
        rebuildFlash = Mathf.Max(0, rebuildFlash - dt);
        if (faceTimer > 0)
        {
            faceTimer -= dt;
            faceAge += dt;
        }

        LastFeetY = Position.Y;
        UpdateSize(dt);

        if (Rocketing)
        {
            // Whoosh! The rocket board zooms up high and flies along up there (no falling, no landing).
            Position = new Vector2(Position.X, Mathf.MoveToward(Position.Y, rocketFeetY, 900f * dt));
            velocityY = 0;
        }
        else if (!OnGround)
        {
            // Gravity pulls the robot down (less on the Moon!). With the parachute open, only a little bit,
            // and never faster than ParachuteFallSpeed.
            velocityY += Gravity * GravityScale * (Parachuting ? 0.12f : 1f) * dt;
            if (Parachuting) velocityY = Mathf.Min(velocityY, PowerUps.ParachuteFallSpeed);
            Position += new Vector2(0, velocityY * dt);
            if (Position.Y >= GroundY)
            {
                Position = new Vector2(Position.X, GroundY);
                squash = 0.7f; // squish on landing
                OnGround = true;
                velocityY = 0;
                jumpsLeft = MaxJumps;
                Parachuting = false; // landed safely!
            }
        }

        RideLeftAndRight(dt);
        float lean = velocityX * facing / 5000f; // lean forward when riding
        float tiltTarget = Rocketing ? -0.12f : (OnGround ? 0.05f : -velocityY / 6000f) + lean; // on the rocket board: nose up!
        tilt = Mathf.Lerp(tilt, tiltTarget, dt * 10f);

        spin = Mathf.MoveToward(spin, 0, dt * 14f);
        squash = Mathf.Lerp(squash, 1f, dt * 12f);
        legTuck = Mathf.Lerp(legTuck, OnGround ? 0f : 1f, dt * 12f);
        happyTimer -= dt;

        blinkTimer -= dt;
        if (blinkTimer < -0.12f) blinkTimer = (float)GD.RandRange(1.5, 4.0);

        UpdateNotes(dt);
        UpdateTrail(dt);
        QueueRedraw();
    }

    /// <summary>The moving parts of his look: the propeller spins, the skateboard wheels roll, and rainbow eyes change color.</summary>
    void UpdateLook(float dt)
    {
        propellerAngle += dt * (6 + Speed / 60);  // faster when he goes faster!
        wheelSpin += dt * Speed / 20;
        if (RainbowEyes) EyeColor = Color.FromHsv(time * 0.25f % 1, 0.55f, 1); // all the way around the rainbow every 4 seconds
    }

    /// <summary>Drops a new trail bit behind him every 0.05 seconds while he rides, and lets the old ones drift away and fade.</summary>
    void UpdateTrail(float dt)
    {
        if (TrailStyle != Trail.None && !Crashed && Speed > 0)
        {
            trailTimer -= dt;
            if (trailTimer <= 0)
            {
                trailTimer = TrailEvery;
                nextSparkleGold = !nextSparkleGold;
                // Out of the back of the board (the back is on the right when he faces left)
                trail.Add(new TrailBit
                {
                    Position = Position + new Vector2(-46 * facing, -14) * Size,
                    Life = TrailLife,
                    Style = TrailStyle,
                    Gold = nextSparkleGold,
                });
            }
        }

        for (int i = trail.Count - 1; i >= 0; i--)
        {
            var bit = trail[i];
            bit.Life -= dt;
            if (bit.Life <= 0) { trail.RemoveAt(i); continue; }
            bit.Position.X -= RunSpeed * dt; // left behind as the world rolls by
        }
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
        Scale = new Vector2(facing, 1) * Size; // flip the drawing to face the other way (and make MEGA Bolt-E giant)
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
                notes.Add(new Note { Position = Position + new Vector2(46, -112) * Size, Life = 1.5f, Purple = GD.Randf() < 0.5f });
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
        HatDrawn = null;
        BoardDrawn = null;
        TrailDrawn = null;
        RimDrawn = false;
        DrawTrail(); // behind everything else

        if (explosion is not null)
        {
            explosion.Draw(this);
            DrawNotes();
            return;
        }

        // A little shadow on the ground (it stays on the ground while we jump).
        // (Everything we draw is made bigger by Size, so we divide by Size to keep the shadow exactly on the ground.)
        float heightAboveGround = GroundY - Position.Y;
        float shadowSize = Mathf.Clamp(1f - heightAboveGround / 400f, 0.3f, 1f);
        DrawSetTransform(new Vector2(0, heightAboveGround / Size), 0, new Vector2(shadowSize, 0.2f * shadowSize));
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
        if (Parachuting) DrawParachute();
        if (Rocketing) DrawRocketFlames();
        DrawHoverboard();
        DrawLeg(-14, BodyColor.Darkened(0.3f)); // back leg (darker, it's in the shadow)
        DrawLeg(12, BodyColor);                 // front leg
        DrawHeadband();
        DrawBackEarCup();
        DrawHead();
        DrawFrontEarCup();
        DrawHat();
        if (ShowMagnet) DrawMagnet();

        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        DrawNotes();

        // The bright flash when the magic rebuild finishes (the explosion started it, here it grows and fades away)
        if (rebuildFlash > 0)
        {
            float f = rebuildFlash / 0.25f; // 1 = just back together, 0 = done
            DrawCircle(new Vector2(0, -75), 40 + 50 * (1 - f), new Color(1, 1, 1, 0.8f * f));
        }
    }

    /// <summary>What he rides on: the classic hoverboard, a skateboard, a hot dog, a rainbow or a cloud (see BoardStyle).</summary>
    void DrawHoverboard(bool glow = true)
    {
        // Glowing light underneath (it pulses!) - every board has it, even the hot dog
        if (glow)
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(time * 8f);
            Shapes.RoundRect(this, new Rect2(-42, -12, 84, 11), 5.5f, new Color(AccentColor, 0.35f * pulse));
            Shapes.RoundRect(this, new Rect2(-32, -10, 64, 7), 3.5f, new Color(AccentColor, 0.9f * pulse));
        }

        switch (BoardStyle)
        {
            case Board.Skateboard: DrawSkateboard(); break;
            case Board.HotDog: DrawHotDogBoard(); break;
            case Board.Rainbow: DrawRainbowBoard(); break;
            case Board.Cloud: DrawCloudBoard(); break;
            default: DrawClassicBoard(); break;
        }
        BoardDrawn = BoardStyle;
    }

    /// <summary>A thin dark outline around the deck of the board (only for light-colored looks, see Rim).</summary>
    void DrawDeckRim(float height = 14)
    {
        if (!Rim) return;
        Shapes.RoundRect(this, new Rect2(-52, -27, 104, height), 7, Ink);
        RimDrawn = true;
    }

    /// <summary>The classic hoverboard: a darker bottom, the deck on top, a shiny silver edge and a tiny power light.</summary>
    void DrawClassicBoard()
    {
        DrawDeckRim();
        Shapes.RoundRect(this, new Rect2(-44, -18, 88, 10), 5, BoardColor.Darkened(0.35f));
        Shapes.RoundRect(this, new Rect2(-50, -25, 100, 10), 5, BoardColor);
        DrawLine(new Vector2(-45, -24), new Vector2(45, -24), BezelColor, 2f, true);
        DrawCircle(new Vector2(-40, -20), 1.6f, AccentColor); // tiny power light
    }

    static readonly Color WheelGrey = new(0.15f, 0.15f, 0.18f);
    static readonly float[] WheelX = { -32, 32 };   // where the two skateboard wheels are

    /// <summary>A skateboard: the deck, two metal trucks, and two wheels with spokes that roll as he rides.</summary>
    void DrawSkateboard()
    {
        DrawDeckRim();
        foreach (float x in WheelX)
            DrawRect(new Rect2(x - 4, -17, 8, 6), BezelColor); // the truck that holds the wheel
        Shapes.RoundRect(this, new Rect2(-50, -25, 100, 10), 5, BoardColor);
        DrawLine(new Vector2(-45, -24), new Vector2(45, -24), BezelColor, 2f, true);
        foreach (float x in WheelX)
        {
            var hub = new Vector2(x, -12);
            DrawCircle(hub, 7, WheelGrey);
            for (int s = 0; s < 3; s++) // 3 spokes, turning as the wheel rolls
                DrawLine(hub, hub + Vector2.Right.Rotated(wheelSpin + s * Mathf.Tau / 3) * 5.5f, AccentColor, 1.5f, true);
            DrawCircle(hub, 2, BezelColor);
        }
    }

    static readonly Color Bun = new(0.9f, 0.7f, 0.4f);
    static readonly Color Sausage = new(0.8f, 0.3f, 0.2f);
    static readonly Color Mustard = new(1f, 0.85f, 0.15f);
    static readonly Vector2[] MustardZigzag = MakeZigzag();

    static Vector2[] MakeZigzag()
    {
        var points = new Vector2[11];
        for (int i = 0; i < points.Length; i++)
            points[i] = new Vector2(-40 + i * 8, i % 2 == 0 ? -24 : -18); // up, down, up, down...
        return points;
    }

    /// <summary>A HOT DOG to ride on: the bun, the sausage on top, and a zigzag of yellow mustard.</summary>
    void DrawHotDogBoard()
    {
        if (Rim)
        {
            Shapes.Ellipse(this, new Vector2(0, -18), new Vector2(54, 12), Ink);
            RimDrawn = true;
        }
        Shapes.Ellipse(this, new Vector2(0, -18), new Vector2(52, 10), Bun);
        Shapes.RoundRect(this, new Rect2(-48, -27, 96, 12), 6, Sausage);
        DrawPolyline(MustardZigzag, Mustard, 2.5f, true);
    }

    /// <summary>A rainbow board: 6 thin stripes whose colors slide along the rainbow.</summary>
    void DrawRainbowBoard()
    {
        DrawDeckRim(height: 22);
        for (int i = 0; i < 6; i++)
        {
            var color = Color.FromHsv(Mathf.PosMod(i / 6f + time * 0.2f, 1f), 0.75f, 1f);
            Shapes.RoundRect(this, new Rect2(-50, -25 + i * 3, 100, 3), 1.5f, color);
        }
    }

    // The 4 bumps of a cloud in the sky (where and how big), the same as Main.DrawCloud
    static readonly (Vector2 At, float Radius)[] CloudBumps =
    {
        (new Vector2(0, 0), 32), (new Vector2(-34, 10), 24), (new Vector2(34, 8), 26), (new Vector2(10, -16), 26),
    };
    const float CloudBoardSize = 0.6f;   // the cloud board is 0.6 times as big as a cloud in the sky

    /// <summary>A fluffy white cloud to ride on (with a dark outline for light-colored looks).</summary>
    void DrawCloudBoard()
    {
        var middle = new Vector2(0, -18);
        if (Rim)
        {
            foreach (var (at, radius) in CloudBumps)
                DrawCircle(middle + at * CloudBoardSize, radius * CloudBoardSize + 2, Ink);
            RimDrawn = true;
        }
        foreach (var (at, radius) in CloudBumps)
            DrawCircle(middle + at * CloudBoardSize, radius * CloudBoardSize, Colors.White);
    }

    /// <summary>ROCKET BOARD flames shooting out of the back of the board. They flicker longer and shorter really fast.</summary>
    void DrawRocketFlames()
    {
        float tip = -95 - 25 * Mathf.Abs(Mathf.Sin(time * 40f)); // the end of the flame
        DrawColoredPolygon(new[]
        {
            new Vector2(-50, -30), new Vector2(-64, -27), new Vector2(tip, -20), new Vector2(-64, -13), new Vector2(-50, -10),
        }, new Color(1f, 0.55f, 0.1f)); // orange outside
        float middleTip = -50 + (tip + 50) * 0.6f;
        DrawColoredPolygon(new[]
        {
            new Vector2(-50, -25), new Vector2(-58, -23), new Vector2(middleTip, -20), new Vector2(-58, -17), new Vector2(-50, -15),
        }, new Color(1f, 0.9f, 0.3f)); // yellow, hotter inside
    }

    /// <summary>A tiny red-and-white parachute over his head, with 4 strings down to the top of his head.</summary>
    void DrawParachute()
    {
        var middle = new Vector2(0, -200);   // the middle of the bottom edge of the parachute
        var radius = new Vector2(62, 38);    // half as wide, and as tall
        var red = new Color(0.9f, 0.2f, 0.2f);

        // The strings (drawn first, so the parachute covers their tops and his head covers their bottoms)
        float[] topX = { -62, -22, 22, 62 };
        float[] bottomX = { -14, -6, 6, 14 };
        for (int i = 0; i < 4; i++)
            DrawLine(new Vector2(topX[i], -200), new Vector2(bottomX[i], -128), new Color(0.3f, 0.3f, 0.35f), 1.5f, true);

        // The top half of an oval, cut into 4 wedges like a pizza: red, white, red, white
        for (int wedge = 0; wedge < 4; wedge++)
        {
            var points = new Vector2[8];
            points[0] = middle;
            for (int i = 0; i < 7; i++)
            {
                float angle = Mathf.Pi + (wedge + i / 6f) * Mathf.Pi / 4; // from the left side (Pi) over the top to the right side (2 Pi)
                points[i + 1] = middle + new Vector2(Mathf.Cos(angle) * radius.X, Mathf.Sin(angle) * radius.Y);
            }
            DrawColoredPolygon(points, wedge % 2 == 0 ? red : Colors.White);
        }
    }

    /// <summary>BOLT MAGNET: a red horseshoe magnet floats over his head (and over his hat), sending out pulsing white lines.</summary>
    void DrawMagnet()
    {
        var bend = new Vector2(0, Mathf.Min(-168, HatTop - 22)); // (a tall hat pushes the magnet up)
        DrawArc(bend, 15, 0, Mathf.Pi, 12, MagnetRed, 9, true); // a U shape (0 to Pi is the bottom half of a circle)
        DrawRect(new Rect2(bend + new Vector2(-19.5f, -8), new Vector2(9, 8)), Silver); // silver tips on the ends of the U
        DrawRect(new Rect2(bend + new Vector2(10.5f, -8), new Vector2(9, 8)), Silver);

        // Magnet power! Three lines pulse out from the top, one after another.
        for (int i = 0; i < 3; i++)
        {
            float pulse = Mathf.PosMod(time * 2f + i / 3f, 1f); // 0 = just started, 1 = faded away
            var white = new Color(1, 1, 1, 1 - pulse);
            float x = (i - 1) * 12;
            DrawLine(bend + new Vector2(x, -12 - pulse * 14), bend + new Vector2(x * 1.4f, -20 - pulse * 14), white, 2.5f, true);
        }
    }

    // ---------- Hats (see Looks.cs) ----------

    static readonly Color PartyPink = new(1f, 0.45f, 0.65f);
    static readonly Color PartyYellow = new(1f, 0.9f, 0.3f);
    static readonly Color CowboyBrown = new(0.55f, 0.35f, 0.18f);
    static readonly Color PirateBlack = new(0.1f, 0.1f, 0.12f);
    static readonly Color CrownGold = new(1f, 0.82f, 0.2f);
    static readonly Color HelmetGlass = new(0.7f, 0.9f, 1f, 0.18f);
    static readonly Color[] CapColors = { new(0.95f, 0.25f, 0.25f), new(1f, 0.85f, 0.2f), new(0.3f, 0.55f, 1f), new(0.35f, 0.85f, 0.4f) };
    static readonly Color[] JewelColors = { new(0.95f, 0.15f, 0.2f), new(0.2f, 0.45f, 1f), new(0.2f, 0.85f, 0.35f) }; // red, blue, green

    static readonly Vector2[] PartyCone = { new(-16, -128), new(16, -128), new(0, -172) };
    static readonly Vector2[] PirateHat = { new(-40, -130), new(40, -130), new(28, -150), new(0, -162), new(-28, -150) };
    static readonly Vector2[] PirateTrim = { new(-40, -130), new(-28, -150), new(0, -162), new(28, -150), new(40, -130) };
    static readonly Vector2[] CrownPoints = { new(-24, -142), new(-8, -142), new(-16, -162),   // left point
                                              new(-8, -142), new(8, -142), new(0, -166),       // middle point (the tallest)
                                              new(8, -142), new(24, -142), new(16, -162) };    // right point
    // Corner points reused every frame (DrawColoredPolygon copies them, so no new lists are made all the time)
    static readonly Vector2[] hatTriangle = new Vector2[3];
    static readonly Vector2[] capWedge = new Vector2[7];

    /// <summary>How high the top of his hat is (the top of his head when there's no hat).</summary>
    float HatTop => Hat switch
    {
        Hat.Party => -178,
        Hat.Cowboy => -158,
        Hat.Pirate => -162,
        Hat.Propeller => -175,
        Hat.Crown => -166,
        Hat.SpaceHelmet => -150,
        _ => -130,
    };

    /// <summary>
    /// The hat he's wearing, sitting on top of his head (the top of his head is at y = -130).
    /// When he blows up, this draws the hat as its own flying piece.
    /// </summary>
    void DrawHat()
    {
        switch (Hat)
        {
            case Hat.None:
                return;

            case Hat.Party:
                // A pink cone with two yellow stripes and a white pom-pom on top
                DrawColoredPolygon(PartyCone, PartyPink);
                DrawLine(new Vector2(-9, -145), new Vector2(9, -145), PartyYellow, 3.5f, true);
                DrawLine(new Vector2(-4.5f, -158), new Vector2(4.5f, -158), PartyYellow, 3.5f, true);
                DrawCircle(new Vector2(0, -172), 6, Colors.White);
                break;

            case Hat.Cowboy:
                // A big cowboy hat: the tall middle (the crown), a dark band around it, and a wide brim in front
                Shapes.RoundRect(this, new Rect2(-22, -158, 44, 30), 10, CowboyBrown);
                DrawRect(new Rect2(-22, -143, 44, 6), CowboyBrown.Darkened(0.45f));
                Shapes.Ellipse(this, new Vector2(0, -130), new Vector2(46, 7), CowboyBrown);
                break;

            case Hat.Pirate:
                // A black pirate hat with gold trim, and a little white skull and crossbones
                DrawColoredPolygon(PirateHat, PirateBlack);
                DrawPolyline(PirateTrim, CrownGold, 2.5f, true);
                DrawLine(new Vector2(-6, -139), new Vector2(6, -132), Colors.White, 2f, true); // the crossbones
                DrawLine(new Vector2(-6, -132), new Vector2(6, -139), Colors.White, 2f, true);
                DrawCircle(new Vector2(0, -146), 5, Colors.White);                             // the skull
                break;

            case Hat.Propeller:
            {
                // A propeller cap: a half-circle in 4 colors, a little stick, and a propeller that spins round and round
                var middle = new Vector2(0, -128);
                for (int wedge = 0; wedge < 4; wedge++)
                {
                    capWedge[0] = middle;
                    for (int i = 0; i < 6; i++)
                    {
                        float angle = Mathf.Pi + (wedge + i / 5f) * Mathf.Pi / 4; // from the left side, over the top, to the right side
                        capWedge[i + 1] = middle + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 30;
                    }
                    DrawColoredPolygon(capWedge, CapColors[wedge]);
                }
                DrawLine(new Vector2(0, -157), new Vector2(0, -170), Ink, 3, true);
                // Seen from the side, a spinning propeller looks like it gets longer and shorter
                float blade = Mathf.Abs(Mathf.Sin(propellerAngle)) * 26;
                var bladeColor = Mathf.Sin(propellerAngle * 2) > 0 ? CapColors[0] : CapColors[2];
                DrawLine(new Vector2(-blade, -171), new Vector2(blade, -171), bladeColor, 5, true);
                DrawCircle(new Vector2(0, -171), 3.5f, CapColors[1]);
                break;
            }

            case Hat.Crown:
                // A golden crown: a band, 3 points, and 3 jewels (red, blue and green)
                DrawRect(new Rect2(-24, -142, 48, 14), CrownGold);
                for (int p = 0; p < 3; p++)
                {
                    hatTriangle[0] = CrownPoints[p * 3];
                    hatTriangle[1] = CrownPoints[p * 3 + 1];
                    hatTriangle[2] = CrownPoints[p * 3 + 2];
                    DrawColoredPolygon(hatTriangle, CrownGold);
                    DrawCircle(new Vector2(-14 + p * 14, -135), 3, JewelColors[p]);
                }
                break;

            case Hat.SpaceHelmet:
                // A see-through space helmet over his whole head, with a white rim and a shine
                DrawCircle(new Vector2(0, -98), 52, HelmetGlass);
                DrawArc(new Vector2(0, -98), 52, 0, Mathf.Tau, 48, Colors.White, 3, true);
                DrawArc(new Vector2(0, -98), 43, Mathf.Pi * 1.1f, Mathf.Pi * 1.4f, 10, new Color(1, 1, 1, 0.8f), 5, true);
                break;
        }
        HatDrawn = Hat;
    }

    // ---------- The trail ----------

    static readonly Color SparkleGold = new(1f, 0.85f, 0.3f);

    /// <summary>The trail bits floating out behind him: twinkly sparkles, pink hearts, or rainbow bubbles.</summary>
    void DrawTrail()
    {
        foreach (var bit in trail)
        {
            var at = (bit.Position - Position) / Size; // the bits live in the world, so undo the robot's own position and size
            at.X *= facing;                            // ...and undo the flip when facing left
            float left = bit.Life / TrailLife;          // 1 = brand new, 0 = gone
            switch (bit.Style)
            {
                case Trail.Sparkles:
                {
                    // A little twinkling star: two crossed lines that turn as they fade
                    var color = new Color(bit.Gold ? SparkleGold : Colors.White, left);
                    var arm = Vector2.Right.Rotated(bit.Life * 8) * (2 + 5 * left);
                    DrawLine(at - arm, at + arm, color, 2f, true);
                    DrawLine(at - arm.Orthogonal(), at + arm.Orthogonal(), color, 2f, true);
                    break;
                }
                case Trail.Hearts:
                    Shapes.HeartEye(this, at, 4 + 4 * left, new Color(HeartPink, left));
                    break;
                case Trail.Rainbow:
                    DrawCircle(at, 3 + 5 * left, Color.FromHsv(bit.Life, 0.75f, 1f, 0.9f * left)); // from blue to red as it fades
                    break;
                default:
                    continue;
            }
            TrailDrawn = bit.Style;
        }
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
        // A light-colored head gets a thin dark outline first, so he's easy to see on snow and candy (see Rim)
        if (Rim)
        {
            Shapes.RoundRect(this, new Rect2(-38.5f, -132.5f, 77, 73), 22.5f, Ink);
            RimDrawn = true;
        }

        // Rounded square head -> ring around the screen -> the screen
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
        else if (faceTimer > 0 && face == RobotFace.Booting)
        {
            DrawBootingFace(leftEye, rightEye);
        }
        else if (faceTimer > 0 && face == RobotFace.Hearts)
        {
            Shapes.HeartEye(this, leftEye, 7, HeartPink);
            Shapes.HeartEye(this, rightEye, 7, HeartPink);
        }
        else if (faceTimer > 0 && face == RobotFace.Stars)
        {
            float turn = time * 4f; // the stars spin round and round
            Shapes.Star(this, leftEye, 8, turn, StarGold);
            Shapes.Star(this, rightEye, 8, turn, StarGold);
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

    /// <summary>The face screen starting up: "loading..." dots light up one by one, then the eyes open.</summary>
    void DrawBootingFace(Vector2 leftEye, Vector2 rightEye)
    {
        if (faceAge < 0.35f)
        {
            for (int i = 0; i < 3; i++)
            {
                bool lit = faceAge >= i * 0.1f; // a new dot every 0.1 seconds
                DrawCircle(new Vector2(-10 + 11 * i, -97), 2.5f, lit ? EyeColor : new Color(EyeColor, 0.25f));
            }
            return;
        }
        float open = Mathf.Lerp(2f, 15f, Mathf.Clamp((faceAge - 0.35f) / 0.25f, 0, 1)); // the eyes open from a thin line
        SquareEye(leftEye, 13, open);
        SquareEye(rightEye, 13, open);
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
            var at = (note.Position - Position) / Size; // notes live in the world, so undo the robot's own position and size
            at.X *= facing;                             // ...and undo the flip when facing left
            var color = new Color(note.Purple ? HeadphoneColor.Lightened(0.35f) : AccentColor, Mathf.Clamp(note.Life, 0, 1));
            DrawCircle(at, 5f, color);
            DrawLine(at + new Vector2(4.5f, 0), at + new Vector2(4.5f, -18), color, 2.5f, true);
            DrawLine(at + new Vector2(4.5f, -18), at + new Vector2(11, -12), color, 2.5f, true);
        }
    }
}
