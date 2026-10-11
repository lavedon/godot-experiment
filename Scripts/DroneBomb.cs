using Godot;

/// <summary>
/// One of DRONEZILLA's bombs! It falls straight down from his belly with a whistle. A blinking red ring on the ground
/// shows where it will land (it blinks faster as the bomb gets closer: ride out of the ring!). On the ground it goes
/// BOOM with a small blast. Touching the falling bomb or its blast is a crash.
/// Position is the bottom of the bomb (on the ground once it has landed).
/// </summary>
public partial class DroneBomb : Node2D
{
    // ---- Bomb numbers (try changing these!) ----
    public const float Gravity = 1100f;          // how fast it falls (Bolt-E falls with 2600)
    public const float BoomSeconds = 0.25f;      // how long the blast can hurt Bolt-E
    public const float BlastHalfWidth = 45f;     // the blast (and the red ring) is 90 pixels wide...
    public const float BlastHeight = 80f;        // ...and 80 pixels tall
    const float FadeSeconds = 0.35f;             // the blast fades away (harmless) after that

    public float GroundY;
    /// <summary>BOOM! (where it hit the ground)</summary>
    public event Action<Vector2>? Boomed;

    float fallSpeed, time;
    float boomTime = -1; // (less than 0 = still falling)

    /// <summary>True while it's still falling.</summary>
    public bool Falling => boomTime < 0;
    /// <summary>True while the blast can hurt Bolt-E.</summary>
    public bool Booming => boomTime >= 0 && boomTime < BoomSeconds;
    /// <summary>True when falling or booming: touching it is a crash.</summary>
    public bool Dangerous => Falling || Booming;
    /// <summary>True when the blast has faded away and the bomb can be removed.</summary>
    public bool Finished => boomTime >= BoomSeconds + FadeSeconds;
    /// <summary>True if the red target ring was drawn in the last frame (the self-test reads this).</summary>
    public bool RingDrawn { get; private set; }

    public Rect2 Hitbox => Falling
        ? new Rect2(Position + new Vector2(-12, -38), new Vector2(24, 38))
        : new Rect2(new Vector2(Position.X - BlastHalfWidth, GroundY - BlastHeight), new Vector2(BlastHalfWidth * 2, BlastHeight));

    // ---- Colors and shapes (made once) ----
    static readonly Color BombBlack = new(0.14f, 0.14f, 0.17f);
    static readonly Color FinGrey = new(0.4f, 0.42f, 0.47f);
    static readonly Color RingRed = new(1f, 0.15f, 0.1f);
    static readonly Color Stripe = new(1f, 0.82f, 0.15f);
    static readonly Vector2[] Fins = { new(-11, -42), new(11, -42), new(6, -30), new(-6, -30) };

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        time += dt;
        if (Falling)
        {
            fallSpeed += Gravity * dt;
            Position = new Vector2(Position.X, Mathf.Min(GroundY, Position.Y + fallSpeed * dt));
            if (Position.Y >= GroundY)
            {
                boomTime = 0;
                Boomed?.Invoke(Position); // BOOM!
            }
        }
        else
        {
            boomTime += dt;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        RingDrawn = false;
        if (Falling)
        {
            // The red target ring on the ground, right under the bomb. It blinks faster as the bomb gets closer.
            float toGround = GroundY - Position.Y;
            float near = Mathf.Clamp(1 - toGround / 400f, 0, 1); // 0 = just dropped, 1 = about to land
            bool bright = (int)(time * (5 + 12 * near)) % 2 == 0;
            var ring = new Color(RingRed, bright ? 0.95f : 0.45f);
            DrawSetTransform(new Vector2(0, toGround), 0, new Vector2(1, 0.25f));
            DrawCircle(Vector2.Zero, BlastHalfWidth, new Color(RingRed, 0.1f + 0.15f * near));
            DrawArc(Vector2.Zero, BlastHalfWidth, 0, Mathf.Tau, 32, ring, 6, true);
            DrawArc(Vector2.Zero, BlastHalfWidth * 0.45f, 0, Mathf.Tau, 20, ring, 4, true);
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            RingDrawn = true;

            // Whistle lines above it, then the bomb: a round black bomb with fins, a yellow stripe and a blinking red nose
            for (int i = 0; i < 2; i++)
                DrawLine(new Vector2(-5 + i * 10, -48), new Vector2(-5 + i * 10, -64), new Color(1, 1, 1, 0.45f), 2, true);
            DrawColoredPolygon(Fins, FinGrey);
            DrawLine(new Vector2(0, -42), new Vector2(0, -30), BombBlack, 3, true);
            DrawCircle(new Vector2(0, -17), 13, BombBlack);
            DrawRect(new Rect2(-11, -28, 22, 4), Stripe);
            DrawCircle(new Vector2(-4, -21), 3.5f, new Color(1, 1, 1, 0.35f)); // a shine
            bool noseOn = (int)(time * 10) % 2 == 0;
            DrawCircle(new Vector2(0, -5), 3.5f, noseOn ? RingRed : RingRed.Darkened(0.5f));
            return;
        }

        // BOOM! A flash of fire that grows and fades away
        float t = Mathf.Min(1, boomTime / (BoomSeconds + FadeSeconds));
        var middle = new Vector2(0, -30);
        DrawCircle(middle, 30 + 40 * t, new Color(1f, 0.55f, 0.15f, 0.75f * (1 - t)));
        DrawCircle(middle, 18 + 26 * t, new Color(1f, 0.92f, 0.45f, 0.9f * (1 - t)));
        DrawArc(middle, 40 + 50 * t, 0, Mathf.Tau, 28, new Color(1, 1, 1, 0.7f * (1 - t)), 4, true);
    }
}
