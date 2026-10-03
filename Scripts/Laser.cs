using Godot;

/// <summary>
/// Big Rusty's giant laser! First a blinking red line shows where it will go (a warning),
/// then a big beam blasts across at head height. Stay on the ground and it goes right over Bolt-E!
/// Position is where the laser starts (the end of his blaster).
/// </summary>
public partial class Laser : Node2D
{
    public const float WarnTime = 0.9f;  // seconds of blinking warning line
    public const float FireTime = 0.7f;  // seconds the beam is on
    public const float TotalTime = WarnTime + FireTime;

    public float Direction = -1;         // -1 = shoots left, 1 = shoots right
    public event Action? StartedFiring;

    float time;

    public bool Firing => time >= WarnTime && time < TotalTime;
    public bool Finished => time >= TotalTime;

    /// <summary>How long the beam is: all the way to the edge of the screen.</summary>
    float Length => Direction < 0 ? Position.X + 60 : 1340 - Position.X;

    public Rect2 Hitbox => Direction < 0
        ? new Rect2(Position.X - Length, Position.Y - 11, Length, 22)
        : new Rect2(Position.X, Position.Y - 11, Length, 22);

    public override void _Process(double delta)
    {
        bool wasFiring = Firing;
        time += (float)delta;
        if (Firing && !wasFiring) StartedFiring?.Invoke();
        QueueRedraw();
    }

    public override void _Draw()
    {
        var end = new Vector2(Direction * Length, 0);
        if (time < WarnTime)
        {
            // Blinking dashed warning line
            if ((int)(time * 10) % 2 == 0)
                for (float d = 0; d < Length; d += 28)
                    DrawLine(new Vector2(Direction * d, 0), new Vector2(Direction * (d + 14), 0), new Color(1f, 0.2f, 0.2f, 0.8f), 3);
            DrawCircle(Vector2.Zero, 6 + 14 * time / WarnTime, new Color(1f, 0.3f, 0.3f, 0.5f)); // charging up
        }
        else if (Firing)
        {
            float wobble = 1f + 0.15f * Mathf.Sin(time * 60);
            DrawLine(Vector2.Zero, end, new Color(1f, 0.2f, 0.2f, 0.3f), 40 * wobble);  // glow
            DrawLine(Vector2.Zero, end, new Color(1f, 0.25f, 0.2f), 20 * wobble);        // beam
            DrawLine(Vector2.Zero, end, new Color(1f, 0.9f, 0.9f), 7 * wobble);          // hot white middle
            DrawCircle(Vector2.Zero, 18 * wobble, new Color(1f, 0.85f, 0.8f));
        }
    }
}
