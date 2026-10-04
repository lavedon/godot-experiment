using Godot;

/// <summary>The three powers a rainbow ? box can give Bolt-E.</summary>
public enum PowerUp
{
    Mega,    // MEGA BOLT-E: he grows giant and SMASHES things!
    Magnet,  // BOLT MAGNET: every bolt nearby zooms in
    Rocket,  // ROCKET BOARD: he zooms up over everything, then floats down on a parachute
}

/// <summary>
/// Everything about the powers in one place: how long they last, their names, and their little pictures (icons).
/// The boxes, the slot machine and the powers themselves are in Main.PowerUps.cs, PowerBox.cs and PowerRoulette.cs.
/// </summary>
public static class PowerUps
{
    // ---- Powers (try changing these!) ----
    public const float MegaSeconds = 6;           // how long MEGA BOLT-E lasts
    public const float MagnetSeconds = 8;         // how long the BOLT MAGNET lasts
    public const float RocketSeconds = 5;         // how long the ROCKET BOARD flies (then the parachute pops open)
    public const float MegaSize = 1.8f;           // how giant MEGA BOLT-E gets (1 = normal size; try 2.5!)
    public const float FirstBoxAt = 100;          // meters before the first ? box can come
    public const float BoxEveryMin = 300;         // meters between boxes: at least this many...
    public const float BoxEveryMax = 450;         // ...and at most this many
    public const float NoBoxesNearBoss = 150;     // meters: no boxes when Big Rusty is closer than this (so a power is usually over before he comes)
    public const float MagnetReach = 420;         // bolts closer than this (in pixels) zoom over to the magnet
    public const float RocketBoost = 1.5f;        // the world rushes by this much faster on the rocket board
    public const float RocketHeight = 330;        // how high the rocket board flies (pixels above the ground)
    public const float ParachuteFallSpeed = 160;  // how fast he floats down on the parachute (pixels per second)
    public const float WarningSeconds = 1.5f;     // in the last seconds of a power, Bolt-E blinks and the clock ticks

    /// <summary>How many seconds a power lasts.</summary>
    public static float Seconds(PowerUp power) => power switch
    {
        PowerUp.Mega => MegaSeconds,
        PowerUp.Magnet => MagnetSeconds,
        _ => RocketSeconds,
    };

    /// <summary>The big words that show when a power starts.</summary>
    public static string Name(PowerUp power) => power switch
    {
        PowerUp.Mega => "MEGA BOLT-E!",
        PowerUp.Magnet => "BOLT MAGNET!",
        _ => "ROCKET BOARD!",
    };

    // ---- Colors for the icons ----
    static readonly Color Gold = new(1f, 0.8f, 0.15f);
    static readonly Color MagnetRed = new(0.9f, 0.15f, 0.15f);
    static readonly Color Silver = new(0.85f, 0.87f, 0.92f);
    static readonly Color RocketOrange = new(1f, 0.55f, 0.15f);
    static readonly Color RocketRed = new(0.9f, 0.2f, 0.2f);
    static readonly Color FlameYellow = new(1f, 0.9f, 0.3f);

    /// <summary>
    /// Draws a power's little picture with its middle at "at". "size" is about half as wide as the picture.
    /// (The slot machine over Bolt-E's head and the power bar at the top of the screen both use this.)
    /// </summary>
    public static void DrawIcon(CanvasItem c, PowerUp power, Vector2 at, float size)
    {
        switch (power)
        {
            case PowerUp.Mega:
                // A gold star with a white arrow pointing up: GROW!
                Shapes.Star(c, at, size, 0, Gold);
                c.DrawColoredPolygon(new[]
                {
                    at + new Vector2(0, -0.42f * size),           // the tip of the arrow
                    at + new Vector2(0.3f * size, -0.04f * size),
                    at + new Vector2(0.11f * size, -0.04f * size),
                    at + new Vector2(0.11f * size, 0.32f * size),
                    at + new Vector2(-0.11f * size, 0.32f * size),
                    at + new Vector2(-0.11f * size, -0.04f * size),
                    at + new Vector2(-0.3f * size, -0.04f * size),
                }, Colors.White);
                break;

            case PowerUp.Magnet:
            {
                // A red horseshoe magnet: a "U" with straight arms and silver tips
                float radius = 0.6f * size, width = 0.35f * size;
                c.DrawArc(at, radius, 0, Mathf.Pi, 16, MagnetRed, width, true); // (0 to Pi is the bottom half: a U)
                foreach (float side in new[] { -1f, 1f })
                {
                    float x = side * radius - width / 2;
                    c.DrawRect(new Rect2(at + new Vector2(x, -0.45f * size), new Vector2(width, 0.45f * size)), MagnetRed); // arm
                    c.DrawRect(new Rect2(at + new Vector2(x, -0.75f * size), new Vector2(width, 0.3f * size)), Silver);    // tip
                }
                break;
            }

            default:
                // A little rocket pointing up: yellow flame, red fins, orange body, red nose, and a round window
                c.DrawColoredPolygon(new[] { at + new Vector2(-0.2f * size, 0.35f * size), at + new Vector2(0.2f * size, 0.35f * size),
                                             at + new Vector2(0, 0.85f * size) }, FlameYellow);
                foreach (float side in new[] { -1f, 1f })
                    c.DrawColoredPolygon(new[] { at + new Vector2(side * 0.25f * size, 0.02f * size), at + new Vector2(side * 0.5f * size, 0.45f * size),
                                                 at + new Vector2(side * 0.25f * size, 0.35f * size) }, RocketRed);
                Shapes.RoundRect(c, new Rect2(at + new Vector2(-0.27f * size, -0.45f * size), new Vector2(0.54f * size, 0.85f * size)), 0.2f * size, RocketOrange);
                c.DrawColoredPolygon(new[] { at + new Vector2(-0.27f * size, -0.42f * size), at + new Vector2(0.27f * size, -0.42f * size),
                                             at + new Vector2(0, -0.9f * size) }, RocketRed);
                c.DrawCircle(at + new Vector2(0, -0.08f * size), 0.13f * size, new Color(0.6f, 0.85f, 1f));
                break;
        }
    }
}
