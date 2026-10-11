using Godot;

public enum EnemyKind
{
    PupBot,  // a robot puppy on wheels that zooms toward you (from enemy-1.png)
    EggBot,  // a hoppy egg-shaped robot (from enemy-2.png)
    DustBunny, // a fluffy grey ball of dust with big eyes: VROOMBA spits them out, and they hop after you
    MiniDrone, // a little buzzing drone with one rotor and one eye: DRONEZILLA drops them, and they drift after you
    FireBat,   // a fiery bat that swoops up and down: it lives in the LAVA CAVES (see VolcanoThings.cs)
}

/// <summary>
/// A bad robot! Touching it from the side is a crash, but landing on its head squashes it (like in Mario).
/// Position is the middle of its bottom, on the ground. (A mini-drone flies: its Position is the bottom of the mini-drone,
/// up at head height.)
/// </summary>
public partial class Enemy : Node2D
{
    public EnemyKind Kind;

    /// <summary>Big Rusty's helpers (and VROOMBA's dust bunnies and DRONEZILLA's mini-drones) chase Bolt-E (left or right) instead of just coming from the right.</summary>
    public bool Chaser;
    /// <summary>How fast a chaser goes after Bolt-E (pixels per second).</summary>
    public float ChaseSpeed = 150f;
    /// <summary>Set this to throw it (VROOMBA spits out dust bunnies): it flies with this speed, then falls down to GroundY.</summary>
    public Vector2 Thrown;
    float flyX;  // how fast it flies sideways while it's thrown
    /// <summary>-1 = facing left (the way it's drawn), 1 = facing right.</summary>
    public float Facing = -1;
    /// <summary>If set, the enemy falls down until it lands on the ground here (helpers are dropped from the sky).</summary>
    public float GroundY;
    float fallSpeed;

    /// <summary>True after it's been stomped.</summary>
    public bool Squashed { get; private set; }

    /// <summary>True when the squash animation is over and it can be removed.</summary>
    public bool Finished => Squashed && squashTime > 0.45f;

    /// <summary>How much faster than the ground it moves toward Bolt-E (pixels per second).</summary>
    public float ExtraSpeed => Squashed ? 0 : Kind == EnemyKind.PupBot ? 140f : Kind == EnemyKind.FireBat ? BatExtraSpeed : 30f;

    /// <summary>How high a mini-drone hovers: the bottom of it, in pixels above the ground (try changing this!)</summary>
    public const float MiniDroneHover = 75f;

    public Rect2 Hitbox => Kind switch
    {
        EnemyKind.MiniDrone => new Rect2(Position + new Vector2(-24, -40 + MiniBob), new Vector2(48, 36)),
        EnemyKind.PupBot => new Rect2(Position + new Vector2(-40, -80), new Vector2(72, 80)),
        EnemyKind.FireBat => BatHitbox, // (its Position is the middle of its body: see VolcanoThings.cs)
        EnemyKind.DustBunny => new Rect2(Position + new Vector2(-25, -50 - HopHeight), new Vector2(50, 48)),
        _ => new Rect2(Position + new Vector2(-24, -110 - HopHeight), new Vector2(48, 108)),
    };

    /// <summary>The top of its head (land here to stomp it!)</summary>
    public float Top => Hitbox.Position.Y;

    // ---- Colors ----
    static readonly Color Screen = new(0.06f, 0.06f, 0.08f);
    static readonly Color PupWhite = new(0.94f, 0.95f, 0.96f);
    static readonly Color PupGrey = new(0.72f, 0.74f, 0.78f);
    static readonly Color PupEyes = new(1f, 0.62f, 0.18f);
    static readonly Color EggWhite = new(0.97f, 0.98f, 0.99f);
    static readonly Color EggTeal = new(0.33f, 0.7f, 0.64f);
    static readonly Color EggEyes = new(0.35f, 0.95f, 0.98f);
    static readonly Color Fluff = new(0.7f, 0.7f, 0.74f);       // the dust bunny's fluffy grey
    static readonly Color DarkFluff = new(0.55f, 0.55f, 0.6f);

    // The dust bunny's fluffy tufts all around its round body (made once, not every time it's drawn)
    static readonly Vector2[] FluffTufts = MakeFluffTufts();

    static Vector2[] MakeFluffTufts()
    {
        var tufts = new Vector2[11];
        for (int i = 0; i < tufts.Length; i++)
            tufts[i] = new Vector2(0, -24) + Vector2.Right.Rotated(i * Mathf.Tau / tufts.Length) * 19;
        return tufts;
    }

    float time = (float)GD.RandRange(0.0, 1.0);
    float squashTime;
    float rotorAngle; // a mini-drone's spinning rotor
    Transform2D drawBase = Transform2D.Identity; // where (and how squished) we're drawing right now

    /// <summary>A mini-drone bobs gently up and down in the air (this is how far down it is right now).</summary>
    float MiniBob => Kind == EnemyKind.MiniDrone && !Squashed ? Mathf.Sin(time * 4f) * 6f : 0;

    /// <summary>Egg-Bot (and a dust bunny) bounces up and down. This is how high off the ground it is right now.</summary>
    float HopHeight
    {
        get
        {
            if (Squashed) return 0;
            if (Kind == EnemyKind.DustBunny)
            {
                if (Position.Y < GroundY - 1) return 0; // (still flying after VROOMBA spat it out)
                float bunnyHop = time / 0.6f % 1f;
                return 4f * 42f * bunnyHop * (1f - bunnyHop); // little hops, up to 42 pixels high
            }
            if (Kind != EnemyKind.EggBot) return 0;
            float p = time / 0.9f % 1f;      // 0 to 1 through each hop
            return 4f * 80f * p * (1f - p);  // up to 80 pixels high (a nice curvy hop)
        }
    }

    public void Stomp()
    {
        Squashed = true;
        squashTime = 0;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        time += dt;
        if (Squashed) squashTime += dt;

        // Thrown (a dust bunny spat out by VROOMBA): it flies up and sideways first
        if (Thrown != Vector2.Zero)
        {
            fallSpeed = Thrown.Y;
            flyX = Thrown.X;
            Thrown = Vector2.Zero;
        }

        // A mini-drone glides down to head height and hovers there (squashed, it drops like a stone)
        if (Kind == EnemyKind.MiniDrone)
        {
            if (Squashed)
            {
                fallSpeed += 2000f * dt;
                Position = new Vector2(Position.X, Mathf.Min(GroundY, Position.Y + fallSpeed * dt));
            }
            else
            {
                Position = new Vector2(Position.X, Mathf.MoveToward(Position.Y, GroundY - MiniDroneHover, 260f * dt));
            }
            rotorAngle += (Squashed ? 6f : 45f) * dt;
        }
        // A fire bat swoops up and down (see VolcanoThings.cs)
        else if (Kind == EnemyKind.FireBat)
        {
            FlyLikeABat(dt);
        }
        // Falling down after being dropped (or thrown)
        else if (GroundY > 0 && (Position.Y < GroundY || fallSpeed < 0))
        {
            fallSpeed += 2000f * dt;
            Position = new Vector2(Position.X + flyX * dt, Mathf.Min(GroundY, Position.Y + fallSpeed * dt));
            if (Position.Y >= GroundY) flyX = 0; // landed
        }

        Scale = new Vector2(Facing < 0 ? 1 : -1, 1); // turn around to face the other way
        QueueRedraw();
    }

    public override void _Draw()
    {
        float hop = HopHeight;

        // Shadow on the ground (smaller when hopping high). A dust bunny's shadow stays on the ground while it flies.
        float flying = Kind is EnemyKind.DustBunny or EnemyKind.MiniDrone or EnemyKind.FireBat ? Mathf.Max(0, GroundY - Position.Y) : 0;
        float shadow = Mathf.Max(0.3f, 1f - (hop + flying) / 200f);
        DrawSetTransform(new Vector2(0, flying), 0, new Vector2(shadow, 0.2f * shadow));
        DrawCircle(Vector2.Zero, Kind == EnemyKind.PupBot ? 42 : 26, new Color(0, 0, 0, 0.18f));

        // Squashed flat like a pancake after a stomp! (it also fades away)
        if (Squashed)
        {
            Modulate = new Color(1, 1, 1, 1f - Mathf.Clamp((squashTime - 0.25f) / 0.2f, 0, 1));
            drawBase = new Transform2D(0, new Vector2(1.35f, 0.3f), 0, Vector2.Zero);
        }
        else if (Kind is EnemyKind.EggBot or EnemyKind.DustBunny)
        {
            // Stretch while flying, squish when touching the ground
            float stretch = hop < 8 ? 0.85f : 1.05f;
            drawBase = new Transform2D(0, new Vector2(2f - stretch, stretch), 0, new Vector2(0, -hop));
        }
        else if (Kind == EnemyKind.MiniDrone)
        {
            drawBase = Transform2D.Identity.Translated(new Vector2(0, MiniBob)); // bobbing in the air
        }
        else
        {
            drawBase = Transform2D.Identity;
        }
        DrawSetTransformMatrix(drawBase);

        if (Kind == EnemyKind.PupBot) DrawPupBot();
        else if (Kind == EnemyKind.DustBunny) DrawDustBunny();
        else if (Kind == EnemyKind.MiniDrone) DrawMiniDrone();
        else if (Kind == EnemyKind.FireBat) DrawFireBat();
        else DrawEggBot(hop);

        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    void DrawPupBot()
    {
        // Wheels (they spin!)
        foreach (float x in new[] { 22f, -22f })
        {
            var hub = new Vector2(x, -15);
            DrawCircle(hub, 15, new Color(0.42f, 0.43f, 0.48f));
            DrawCircle(hub, 9, new Color(0.78f, 0.79f, 0.83f));
            var spoke = Vector2.Right.Rotated(-time * 14f) * 8;
            DrawLine(hub - spoke, hub + spoke, new Color(0.5f, 0.52f, 0.56f), 3, true);
        }

        float bob = Squashed ? 0 : Mathf.Abs(Mathf.Sin(time * 12f)) * -2f;

        // Body
        Shapes.RoundRect(this, new Rect2(-30, -44 + bob, 64, 28), 13, PupGrey);
        Shapes.RoundRect(this, new Rect2(-28, -44 + bob, 60, 18), 9, PupWhite);

        // Round ear on the back of the head, tall pointy ear on top
        DrawCircle(new Vector2(4, -68 + bob), 11, PupGrey);
        DrawCircle(new Vector2(4, -68 + bob), 6, PupWhite, false, 2.5f, true);
        DrawColoredPolygon(new[] { new Vector2(-38, -82 + bob), new Vector2(-26, -82 + bob), new Vector2(-36, -110 + bob) }, PupWhite);
        DrawColoredPolygon(new[] { new Vector2(-35, -84 + bob), new Vector2(-29, -84 + bob), new Vector2(-34.5f, -100 + bob) }, PupGrey);

        // Head with a black face screen (facing left, toward Bolt-E)
        Shapes.RoundRect(this, new Rect2(-50, -88 + bob, 54, 50), 24, PupWhite);
        Shapes.RoundRect(this, new Rect2(-46, -83 + bob, 44, 40), 19, Screen);

        var leftEye = new Vector2(-34, -63 + bob);
        var rightEye = new Vector2(-20, -63 + bob);
        if (Squashed)
        {
            Shapes.XEye(this, leftEye, 4, PupEyes);
            Shapes.XEye(this, rightEye, 4, PupEyes);
        }
        else
        {
            Shapes.AngryEye(this, leftEye, new Vector2(11, 13), PupEyes, Screen, innerOnRight: true);
            Shapes.AngryEye(this, rightEye, new Vector2(11, 13), PupEyes, Screen, innerOnRight: false);
        }
    }

    void DrawEggBot(float hop)
    {
        // Arms wave up when hopping
        float wave = Squashed ? 0 : (hop > 8 ? -0.6f : 0.2f) + Mathf.Sin(time * 10f) * 0.15f;
        DrawArm(new Vector2(-24, -56), -0.5f + wave);
        DrawArm(new Vector2(24, -56), 0.5f - wave);

        // Egg-shaped body
        DrawSetTransformMatrix(drawBase * Transform2D.Identity.Scaled(new Vector2(1, 1.25f)).Translated(new Vector2(0, -32)));
        DrawCircle(Vector2.Zero, 24, EggWhite);
        DrawCircle(new Vector2(-8, -8), 9, new Color(1, 1, 1, 0.6f));
        DrawSetTransformMatrix(drawBase);

        // Teal collar with drips
        Shapes.RoundRect(this, new Rect2(-17, -62, 34, 9), 4.5f, EggTeal);
        DrawCircle(new Vector2(-10, -53), 4, EggTeal);
        DrawCircle(new Vector2(10, -54), 3.5f, EggTeal);

        // Round head with ear-discs and a teal stripe on top
        DrawCircle(new Vector2(-26, -84), 8, new Color(0.72f, 0.75f, 0.78f));
        DrawCircle(new Vector2(26, -84), 8, new Color(0.72f, 0.75f, 0.78f));
        DrawCircle(new Vector2(0, -84), 27, EggWhite);
        DrawArc(new Vector2(0, -84), 24, Mathf.Pi * 1.35f, Mathf.Pi * 1.65f, 10, EggTeal, 6, true);

        // Black face screen
        Shapes.RoundRect(this, new Rect2(-20, -101, 40, 32), 15, Screen);
        var leftEye = new Vector2(-9, -88);
        var rightEye = new Vector2(9, -88);
        if (Squashed)
        {
            Shapes.XEye(this, leftEye, 4, EggEyes);
            Shapes.XEye(this, rightEye, 4, EggEyes);
        }
        else
        {
            // Sneaky curved eyes and a little "w" cat mouth
            DrawArc(leftEye + new Vector2(0, 3), 6, Mathf.Pi * 1.1f, Mathf.Pi * 1.9f, 10, EggEyes, 4, true);
            DrawArc(rightEye + new Vector2(0, 3), 6, Mathf.Pi * 1.1f, Mathf.Pi * 1.9f, 10, EggEyes, 4, true);
            DrawArc(new Vector2(-2.5f, -80), 2.5f, 0.1f, Mathf.Pi - 0.1f, 6, EggEyes, 2, true);
            DrawArc(new Vector2(2.5f, -80), 2.5f, 0.1f, Mathf.Pi - 0.1f, 6, EggEyes, 2, true);
        }
    }

    /// <summary>A fluffy grey ball of dust with two big eyes, two fluffy ears and little feet (it's looking left, at Bolt-E).</summary>
    void DrawDustBunny()
    {
        var body = new Vector2(0, -24);

        // Fluffy ears, then fluffy tufts all around, then the round body on top
        DrawCircle(new Vector2(-9, -47), 7, DarkFluff);
        DrawCircle(new Vector2(8, -48), 7, DarkFluff);
        for (int i = 0; i < FluffTufts.Length; i++)
            DrawCircle(FluffTufts[i], 8 + i % 3, i % 2 == 0 ? Fluff : DarkFluff);
        DrawCircle(body, 20, Fluff);
        DrawCircle(new Vector2(-7, -33), 6, new Color(1, 1, 1, 0.25f)); // a soft shine
        // Little specks of dust
        DrawCircle(new Vector2(10, -16), 1.6f, DarkFluff.Darkened(0.3f));
        DrawCircle(new Vector2(-12, -12), 1.4f, DarkFluff.Darkened(0.3f));
        DrawCircle(new Vector2(13, -30), 1.2f, DarkFluff.Darkened(0.3f));

        // Two tiny feet
        DrawCircle(new Vector2(-8, -3), 4.5f, DarkFluff);
        DrawCircle(new Vector2(8, -3), 4.5f, DarkFluff);

        // Two big eyes (X eyes when it's squashed)
        foreach (var eye in BunnyEyes)
        {
            if (Squashed)
            {
                Shapes.XEye(this, eye, 4, Screen);
                continue;
            }
            DrawCircle(eye, 6.5f, Colors.White);
            DrawCircle(eye + new Vector2(-2, 1), 3.5f, Screen);                // looking at Bolt-E
            DrawCircle(eye + new Vector2(-3, -0.5f), 1.2f, Colors.White);      // a sparkle
        }
    }

    static readonly Vector2[] BunnyEyes = { new(-9, -27), new(5, -27) };

    // ---- The mini-drone's colors ----
    static readonly Color MiniMetal = new(0.36f, 0.39f, 0.45f);
    static readonly Color MiniDark = new(0.2f, 0.22f, 0.26f);
    static readonly Color MiniYellow = new(1f, 0.82f, 0.15f);
    static readonly Color MiniRed = new(1f, 0.2f, 0.15f);

    /// <summary>
    /// A little round drone (it's looking left, at Bolt-E): one big eye, a yellow stripe, little legs, and one rotor on top
    /// spinning so fast it's a blur. Like a baby DRONEZILLA!
    /// </summary>
    void DrawMiniDrone()
    {
        var body = new Vector2(0, -21);

        // The rotor on top: a see-through blur, and a blade that turns (it looks longer and shorter as it spins)
        DrawLine(new Vector2(0, -36), new Vector2(0, -44), MiniDark, 3, true);
        if (!Squashed) Shapes.Ellipse(this, new Vector2(0, -45), new Vector2(26, 4), new Color(1, 1, 1, 0.3f));
        float reach = Mathf.Cos(rotorAngle) * 24;
        if (Mathf.Abs(reach) < 2) reach = 2;
        DrawLine(new Vector2(-reach, -45), new Vector2(reach, -45), new Color(0.88f, 0.9f, 0.95f, Squashed ? 1f : 0.7f), 3.5f, true);
        DrawCircle(new Vector2(0, -45), 3, MiniDark);

        // Two little legs, the round body, a yellow stripe and a shine
        DrawLine(new Vector2(-9, -8), new Vector2(-13, -1), MiniDark, 3, true);
        DrawLine(new Vector2(9, -8), new Vector2(13, -1), MiniDark, 3, true);
        DrawCircle(body, 17, MiniMetal);
        DrawRect(new Rect2(-15, -16, 30, 5), MiniYellow);
        DrawCircle(new Vector2(-6, -30), 4, new Color(1, 1, 1, 0.25f));

        // A blinking red light on its back
        if ((int)(time * 3) % 2 == 0 && !Squashed) DrawCircle(new Vector2(14, -30), 2.5f, MiniRed);

        // One big eye, looking at Bolt-E (an X when it's squashed)
        var eye = new Vector2(-5, -24);
        if (Squashed)
        {
            Shapes.XEye(this, eye, 4, Screen);
            return;
        }
        DrawCircle(eye, 8, Colors.White);
        DrawCircle(eye + new Vector2(-2.5f, 0.5f), 4.2f, MiniRed);
        DrawCircle(eye + new Vector2(-2.5f, 0.5f), 2f, Screen);
        DrawCircle(eye + new Vector2(-4, -1.5f), 1.3f, Colors.White); // a sparkle
    }

    void DrawArm(Vector2 shoulder, float angle)
    {
        // A teal mitten arm (an oval, made by squishing a circle)
        DrawSetTransformMatrix(drawBase * new Transform2D(angle, shoulder) * Transform2D.Identity.Scaled(new Vector2(0.55f, 1f)).Translated(new Vector2(0, -10)));
        DrawCircle(Vector2.Zero, 13, EggTeal);
        DrawSetTransformMatrix(drawBase);
    }

}
