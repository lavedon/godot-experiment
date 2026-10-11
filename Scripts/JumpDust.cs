using Godot;

/// <summary>A jump leaves a little cloud behind. It settles on the floor until VROOMBA eats it or it fades.</summary>
public partial class JumpDust : Node2D
{
    public const float Lifetime = 8f;
    public float GroundY;
    public float Age { get; private set; }
    public bool Consumed { get; private set; }
    public bool Available => !Consumed && Age < Lifetime && !IsQueuedForDeletion();
    public float VacuumProgress;

    static readonly Color Dust = new(0.83f, 0.78f, 0.67f);
    static readonly Color Outline = new(0.38f, 0.33f, 0.28f);

    public void Consume() => Consumed = true;

    public void Update(float dt)
    {
        Age += dt;
        if (VacuumProgress <= 0)
            Position = new Vector2(Position.X, Mathf.MoveToward(Position.Y, GroundY - 12, 260f * dt));
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!Available) return;
        float fade = Mathf.Clamp((Lifetime - Age) / 1.2f, 0, 1);
        float size = (0.65f + 0.35f * Mathf.Min(1, Age / 0.2f)) * (1 - VacuumProgress * 0.8f);
        for (int i = 0; i < 5; i++)
        {
            var at = new Vector2((i - 2) * 12, -Mathf.Sin(i * 1.7f + Age * 3) * 3) * size;
            float radius = (i % 2 == 0 ? 11 : 15) * size;
            DrawCircle(at, radius + 2, new Color(Outline, 0.65f * fade));
            DrawCircle(at, radius, new Color(Dust, 0.9f * fade));
        }
        DrawArc(new Vector2(0, -2) * size, 7 * size, Age * 2, Age * 2 + Mathf.Pi * 1.5f, 12,
                new Color(Outline, 0.6f * fade), 2, true);
    }
}
