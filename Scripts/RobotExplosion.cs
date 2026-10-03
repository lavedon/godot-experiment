using Godot;

/// <summary>One part of the robot that can fly off when it blows apart.</summary>
/// <param name="Center">Where the middle of this part is on the whole robot.</param>
/// <param name="Radius">About how far the part reaches from its middle (used for bouncing on the ground).</param>
/// <param name="Draw">Code that draws this part in its normal spot on the robot.</param>
/// <param name="FlatEvery">The part settles to lie flat at multiples of this angle (a square head can lie on any side).</param>
public record RobotPart(string Name, Vector2 Center, float Radius, Action Draw, float FlatEvery = Mathf.Pi / 2);

/// <summary>
/// KABOOM! When Bolt-E crashes, it bursts into pieces: the parts fly off spinning, bounce on the ground,
/// and skid to a stop. Plus a flash, a shockwave ring, sparks, smoke, gears and screws, and dizzy stars.
/// Everything here is in the robot's own drawing space (0,0 = where the robot's feet were).
/// </summary>
public class RobotExplosion
{
    const float Gravity = 1800f;
    const float Bounciness = 0.45f; // 1 = bounces forever, 0 = goes splat

    enum Debris { None, Gear, Screw, Spring }

    class Piece
    {
        public RobotPart? Part;   // a real robot part, or null for a little gear/screw/spring
        public Debris Debris;
        public Vector2 Center, Position, Velocity;
        public float Rotation, Spin, Radius, FlatEvery;
    }

    class Spark
    {
        public Vector2 Position, Velocity;
        public float Life, MaxLife;
        public Color Color;
    }

    class Puff
    {
        public Vector2 Position, Velocity;
        public float Life, MaxLife, Size;
    }

    readonly List<Piece> pieces = new();
    readonly List<Spark> sparks = new();
    readonly List<Puff> puffs = new();
    readonly float groundY;
    readonly Color glowColor;
    readonly Piece? head;
    readonly Action<bool>? onBounce;
    Vector2 boomCenter;
    float time;

    static readonly Color Metal = new(0.62f, 0.64f, 0.7f);

    /// <param name="parts">The robot's parts, drawn back to front.</param>
    /// <param name="pivot">The point the robot was tilting/spinning around.</param>
    /// <param name="bodyAngle">How tilted the robot was at the moment of the crash.</param>
    /// <param name="groundY">Where the ground is, measured from the robot's position.</param>
    /// <param name="onBounce">Called when a piece hits the ground hard (true = a big robot part, false = a little gear or screw).</param>
    public RobotExplosion(IEnumerable<RobotPart> parts, Vector2 pivot, float bodyAngle, float groundY, Color glowColor,
                          Action<bool>? onBounce = null)
    {
        this.groundY = groundY;
        this.glowColor = glowColor;
        this.onBounce = onBounce;
        boomCenter = pivot + (new Vector2(0, -75) - pivot).Rotated(bodyAngle);

        // 1. The big parts: start exactly where they were, then fly away from the middle of the boom.
        foreach (var part in parts)
        {
            var start = pivot + (part.Center - pivot).Rotated(bodyAngle);
            var away = start - boomCenter;
            var direction = away.LengthSquared() < 1 ? Vector2.Up : away.Normalized();
            var piece = new Piece
            {
                Part = part,
                Center = part.Center,
                Position = start,
                Velocity = direction * Rand(250, 450) + new Vector2(Rand(-120, 120), Rand(-550, -350)),
                Rotation = bodyAngle,
                Spin = Rand(-12, 12),
                Radius = part.Radius,
                FlatEvery = part.FlatEvery,
            };
            if (part.Name == "head")
            {
                // The head pops way up high, spinning, so you can see its X eyes.
                piece.Velocity = new Vector2(Rand(40, 140), -820);
                piece.Spin = Rand(-6, 6);
                head = piece;
            }
            pieces.Add(piece);
        }

        // 2. Little gears, screws and springs
        for (int i = 0; i < 12; i++)
        {
            pieces.Add(new Piece
            {
                Debris = (Debris)(1 + i % 3),
                Position = boomCenter + RandomDirection() * Rand(0, 20),
                Velocity = RandomDirection() * Rand(300, 700) + new Vector2(0, -300),
                Spin = Rand(-20, 20),
                Radius = 5,
                FlatEvery = Mathf.Pi,
            });
        }

        // 3. Sparks shooting out in every direction
        Color[] sparkColors = { new(1f, 0.95f, 0.5f), new(1f, 0.6f, 0.2f), Colors.White, glowColor };
        for (int i = 0; i < 45; i++)
        {
            float life = Rand(0.3f, 0.8f);
            sparks.Add(new Spark
            {
                Position = boomCenter,
                Velocity = RandomDirection() * Rand(300, 950),
                Life = life,
                MaxLife = life,
                Color = sparkColors[i % sparkColors.Length],
            });
        }

        // 4. Puffs of smoke
        for (int i = 0; i < 12; i++)
            AddPuff(boomCenter + RandomDirection() * Rand(0, 25), RandomDirection() * Rand(30, 130) + new Vector2(0, -40), Rand(14, 28));
    }

    public void Update(float dt, float worldSpeed)
    {
        time += dt;
        // The world is still skidding to a stop, so everything slides back with the ground.
        var drift = new Vector2(-worldSpeed * dt, 0);
        boomCenter += drift;

        foreach (var piece in pieces)
        {
            piece.Velocity.Y += Gravity * dt;
            piece.Position += piece.Velocity * dt + drift;
            piece.Rotation += piece.Spin * dt;

            float floor = groundY - piece.Radius;
            if (piece.Position.Y >= floor)
            {
                piece.Position.Y = floor;
                if (piece.Velocity.Y > 150)
                {
                    // Boing! Bounce, slow down a bit, and kick up some dust.
                    if (piece.Velocity.Y > 400 && piece.Part is not null)
                        AddPuff(new Vector2(piece.Position.X, groundY - 6), new Vector2(Rand(-40, 40), -30), 10);
                    if (piece.Velocity.Y > 300) onBounce?.Invoke(piece.Part is not null);
                    piece.Velocity.Y *= -Bounciness;
                    piece.Velocity.X *= 0.7f;
                    piece.Spin *= 0.6f;
                }
                else
                {
                    // Resting on the ground: skid to a stop and settle down flat.
                    piece.Velocity.Y = 0;
                    piece.Velocity.X = Mathf.MoveToward(piece.Velocity.X, 0, 700 * dt);
                    piece.Spin = Mathf.MoveToward(piece.Spin, 0, 30 * dt);
                    float flat = Mathf.Round(piece.Rotation / piece.FlatEvery) * piece.FlatEvery;
                    piece.Rotation = Mathf.LerpAngle(piece.Rotation, flat, dt * 6);
                }
            }
        }

        for (int i = sparks.Count - 1; i >= 0; i--)
        {
            var spark = sparks[i];
            spark.Life -= dt;
            if (spark.Life <= 0) { sparks.RemoveAt(i); continue; }
            spark.Velocity.Y += Gravity * 0.5f * dt;
            spark.Velocity *= 1f - 1.5f * dt; // air slows them down
            spark.Position += spark.Velocity * dt + drift;
        }

        for (int i = puffs.Count - 1; i >= 0; i--)
        {
            var puff = puffs[i];
            puff.Life -= dt;
            if (puff.Life <= 0) { puffs.RemoveAt(i); continue; }
            puff.Position += puff.Velocity * dt + drift;
            puff.Velocity *= 1f - 2f * dt;
        }
    }

    public void Draw(CanvasItem canvas)
    {
        // Smoke goes behind everything
        canvas.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        foreach (var puff in puffs)
        {
            float left = puff.Life / puff.MaxLife; // 1 = brand new, 0 = gone
            canvas.DrawCircle(puff.Position, puff.Size * (1.8f - 0.8f * left), new Color(0.5f, 0.5f, 0.56f, 0.55f * left));
        }

        // Each piece: move it to where it is now, turn it, then draw it as if it were still on the robot.
        foreach (var piece in pieces)
        {
            canvas.DrawSetTransform(piece.Position - piece.Center.Rotated(piece.Rotation), piece.Rotation, Vector2.One);
            if (piece.Part is not null) piece.Part.Draw();
            else DrawDebris(canvas, piece.Debris);
        }
        canvas.DrawSetTransform(Vector2.Zero, 0, Vector2.One);

        // Sparks are little streaks pointing the way they're flying
        foreach (var spark in sparks)
        {
            var color = new Color(spark.Color, spark.Life / spark.MaxLife);
            canvas.DrawLine(spark.Position, spark.Position - spark.Velocity * 0.03f, color, 3f, true);
        }

        // A bright flash...
        if (time < 0.25f)
        {
            float t = time / 0.25f;
            canvas.DrawCircle(boomCenter, 30 + 110 * t, new Color(1f, 1f, 0.9f, 0.85f * (1 - t)));
        }

        // ...and a shockwave ring that grows and fades
        if (time < 0.5f)
        {
            float t = time / 0.5f;
            canvas.DrawArc(boomCenter, 20 + 220 * t, 0, Mathf.Tau, 64, new Color(glowColor, 1 - t), 1 + 9 * (1 - t), true);
        }

        // Dizzy stars spinning above the head after it lands
        if (head is not null && time > 0.9f)
        {
            float fadeIn = Mathf.Min(1, (time - 0.9f) * 3);
            for (int i = 0; i < 3; i++)
            {
                float angle = time * 4f + i * Mathf.Tau / 3f;
                var at = head.Position + new Vector2(Mathf.Cos(angle) * 32, Mathf.Sin(angle) * 9 - 50);
                Shapes.Star(canvas, at, 8, time * 3f, new Color(1f, 0.88f, 0.25f, fadeIn));
            }
        }
    }

    static void DrawDebris(CanvasItem canvas, Debris debris)
    {
        switch (debris)
        {
            case Debris.Gear:
                for (int i = 0; i < 6; i++)
                    canvas.DrawLine(Vector2.Zero, Vector2.Right.Rotated(i * Mathf.Tau / 6) * 8.5f, Metal, 3.5f);
                canvas.DrawCircle(Vector2.Zero, 6.5f, Metal);
                canvas.DrawCircle(Vector2.Zero, 2.5f, Metal.Darkened(0.5f));
                break;
            case Debris.Screw:
                canvas.DrawLine(new Vector2(-6, 0), new Vector2(6, 0), Metal, 2.5f, true);
                canvas.DrawRect(new Rect2(-8, -4, 3, 8), Metal.Lightened(0.2f));
                break;
            case Debris.Spring:
                canvas.DrawPolyline(new[] { new Vector2(-8, 0), new Vector2(-6, -4), new Vector2(-3, 4), new Vector2(0, -4),
                                            new Vector2(3, 4), new Vector2(6, -4), new Vector2(8, 0) }, Metal, 2f, true);
                break;
        }
    }

    void AddPuff(Vector2 at, Vector2 velocity, float size)
    {
        float life = Rand(0.8f, 1.4f);
        puffs.Add(new Puff { Position = at, Velocity = velocity, Life = life, MaxLife = life, Size = size });
    }

    static float Rand(float min, float max) => (float)GD.RandRange(min, max);
    static Vector2 RandomDirection() => Vector2.Right.Rotated(Rand(0, Mathf.Tau));
}
