using Godot;

public enum EnemyKind
{
    PupBot,  // a robot puppy on wheels that zooms toward you (from enemy-1.png)
    EggBot,  // a hoppy egg-shaped robot (from enemy-2.png)
}

/// <summary>
/// A bad robot! Touching it from the side is a crash, but landing on its head squashes it (like in Mario).
/// Position is the middle of its bottom, on the ground.
/// </summary>
public partial class Enemy : Node2D
{
    public EnemyKind Kind;

    /// <summary>Big Rusty's helpers chase Bolt-E (left or right) instead of just coming from the right.</summary>
    public bool Chaser;
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
    public float ExtraSpeed => Squashed ? 0 : Kind == EnemyKind.PupBot ? 140f : 30f;

    public Rect2 Hitbox => Kind == EnemyKind.PupBot
        ? new Rect2(Position + new Vector2(-40, -80), new Vector2(72, 80))
        : new Rect2(Position + new Vector2(-24, -110 - HopHeight), new Vector2(48, 108));

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

    float time = (float)GD.RandRange(0.0, 1.0);
    float squashTime;
    Transform2D drawBase = Transform2D.Identity; // where (and how squished) we're drawing right now

    /// <summary>Egg-Bot bounces up and down. This is how high off the ground it is right now.</summary>
    float HopHeight
    {
        get
        {
            if (Kind != EnemyKind.EggBot || Squashed) return 0;
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

        // Falling down after being dropped
        if (GroundY > 0 && Position.Y < GroundY)
        {
            fallSpeed += 2000f * dt;
            Position = new Vector2(Position.X, Mathf.Min(GroundY, Position.Y + fallSpeed * dt));
        }

        Scale = new Vector2(Facing < 0 ? 1 : -1, 1); // turn around to face the other way
        QueueRedraw();
    }

    public override void _Draw()
    {
        float hop = HopHeight;

        // Shadow on the ground (smaller when hopping high)
        float shadow = 1f - hop / 200f;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(shadow, 0.2f * shadow));
        DrawCircle(Vector2.Zero, Kind == EnemyKind.PupBot ? 42 : 26, new Color(0, 0, 0, 0.18f));

        // Squashed flat like a pancake after a stomp! (it also fades away)
        if (Squashed)
        {
            Modulate = new Color(1, 1, 1, 1f - Mathf.Clamp((squashTime - 0.25f) / 0.2f, 0, 1));
            drawBase = new Transform2D(0, new Vector2(1.35f, 0.3f), 0, Vector2.Zero);
        }
        else if (Kind == EnemyKind.EggBot)
        {
            // Stretch while flying, squish when touching the ground
            float stretch = hop < 8 ? 0.85f : 1.05f;
            drawBase = new Transform2D(0, new Vector2(2f - stretch, stretch), 0, new Vector2(0, -hop));
        }
        else
        {
            drawBase = Transform2D.Identity;
        }
        DrawSetTransformMatrix(drawBase);

        if (Kind == EnemyKind.PupBot) DrawPupBot();
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

    void DrawArm(Vector2 shoulder, float angle)
    {
        // A teal mitten arm (an oval, made by squishing a circle)
        DrawSetTransformMatrix(drawBase * new Transform2D(angle, shoulder) * Transform2D.Identity.Scaled(new Vector2(0.55f, 1f)).Translated(new Vector2(0, -10)));
        DrawCircle(Vector2.Zero, 13, EggTeal);
        DrawSetTransformMatrix(drawBase);
    }

}
