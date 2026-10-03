using Godot;

/// <summary>A wave of rumbling ground that rolls away from Big Rusty's ground pound. Jump over it!</summary>
public partial class Shockwave : Node2D
{
    const float Speed = 470f;
    public float Direction = 1; // -1 = rolls left, 1 = rolls right
    float time;

    public Rect2 Hitbox => new(Position + new Vector2(-14, -32), new Vector2(28, 32));

    public void Advance(float dt, float worldSpeed)
    {
        time += dt;
        Position += new Vector2(Direction * Speed - worldSpeed, 0) * dt;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float d = Direction;
        float wobble = Mathf.Sin(time * 30) * 2;
        // A curly wave shape leaning the way it's going
        DrawColoredPolygon(new[]
        {
            new Vector2(-22 * d, 0), new Vector2(-8 * d, -24 - wobble), new Vector2(6 * d, -36 - wobble),
            new Vector2(14 * d, -22), new Vector2(20 * d, 0),
        }, new Color(1f, 0.62f, 0.15f, 0.9f));
        DrawColoredPolygon(new[]
        {
            new Vector2(-12 * d, 0), new Vector2(-2 * d, -16 - wobble), new Vector2(8 * d, -24 - wobble), new Vector2(12 * d, 0),
        }, new Color(1f, 0.9f, 0.4f));
        // Dust kicked up behind it
        for (int i = 1; i <= 3; i++)
            DrawCircle(new Vector2(-d * (18 + i * 12), -6 - i * 2), 7 - i, new Color(0.85f, 0.75f, 0.6f, 0.5f - i * 0.12f));
    }
}
