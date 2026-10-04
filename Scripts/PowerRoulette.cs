using Godot;

/// <summary>
/// The slot machine over Bolt-E's head! After he jumps into a ? box, the window flicks through the powers
/// (tick-tick-tick), slowing down like a real slot machine, and lands on one. The winner pops big for a moment,
/// then the power starts. It counts the time itself every frame (no timers), so Stop() can always cancel it.
/// </summary>
public partial class PowerRoulette : Node2D
{
    // How long to wait before each flick of the window: it slows down at the end (try changing these!)
    static readonly float[] FlickDelays = { 0.06f, 0.06f, 0.07f, 0.08f, 0.1f, 0.12f, 0.15f, 0.2f };
    const float HoldSeconds = 0.35f;  // how long the winner shows (with a pop!) before the power starts
    const float PopSize = 1.4f;       // how big the window pops when it lands

    static readonly Color Ink = new(0.13f, 0.18f, 0.32f);

    /// <summary>True while the window is flicking (or showing the winner).</summary>
    public bool Spinning { get; private set; }
    /// <summary>The power in the window right now.</summary>
    public PowerUp Showing { get; private set; }

    /// <summary>Each flick of the window (0 = the first one, 7 = the last one with 8 FlickDelays). Main plays a "tick" for each.</summary>
    public event Action<int>? Ticked;
    /// <summary>The window landed on this power: time to start it!</summary>
    public event Action<PowerUp>? Picked;

    PowerUp result;   // the power it will land on
    int flicks;       // how many flicks so far
    float timer;      // seconds until the next flick (or until the winner has shown long enough)

    public PowerRoulette()
    {
        ZIndex = 20;     // in front of everything in the world
        Visible = false; // hidden until a box is opened
    }

    /// <summary>Starts spinning. It will land on "landOn" after one flick for each of the FlickDelays (8 of them now).</summary>
    public void Start(PowerUp landOn)
    {
        result = landOn;
        // The powers take turns (Mega, Magnet, Rocket, Mega...). Start as many steps before the winner as there are
        // flicks, so the last flick always shows the winner, however many FlickDelays there are.
        Showing = (PowerUp)Mathf.PosMod((int)landOn - FlickDelays.Length, 3);
        flicks = 0;
        timer = FlickDelays[0];
        Scale = Vector2.One;
        Spinning = true;
        Visible = true;
        QueueRedraw();
    }

    /// <summary>Stops right away and hides, without giving a power (for example when Bolt-E crashes).</summary>
    public void Stop()
    {
        Spinning = false;
        Visible = false;
    }

    static PowerUp NextPower(PowerUp power) => (PowerUp)(((int)power + 1) % 3);

    public override void _Process(double delta)
    {
        if (!Spinning) return;
        timer -= (float)delta;

        if (flicks < FlickDelays.Length)
        {
            if (timer <= 0)
            {
                // Flick! The next power shows in the window.
                Showing = NextPower(Showing);
                Ticked?.Invoke(flicks);
                flicks++;
                if (flicks < FlickDelays.Length)
                {
                    timer += FlickDelays[flicks];
                }
                else
                {
                    timer = HoldSeconds; // that was the last flick: the winner pops big
                    Scale = Vector2.One * PopSize;
                }
                QueueRedraw();
            }
            return;
        }

        // Showing the winner: the pop shrinks back to normal size...
        Scale = Vector2.One * Mathf.Lerp(1f, PopSize, Mathf.Clamp(timer / HoldSeconds, 0, 1));
        if (timer <= 0)
        {
            // ...and then the power starts! (Hide first, so it's all tidy before the power begins.)
            Stop();
            Picked?.Invoke(result);
        }
    }

    public override void _Draw()
    {
        // A white window with a dark edge, and the power's picture inside
        var window = new Rect2(-32, -32, 64, 64);
        Shapes.RoundRect(this, window, 14, Ink);
        Shapes.RoundRect(this, window.Grow(-3), 11, Colors.White);
        PowerUps.DrawIcon(this, Showing, Vector2.Zero, 24);
    }
}
