using Godot;

/// <summary>
/// A little stack of three cardboard boxes at the end of the arena, where OPTI-MAX gets boxes to throw. When he takes
/// the top box, a new one pops back on a moment later (it's a factory!). It's only scenery: it never hurts Bolt-E.
/// Position is the bottom-middle of the stack.
/// </summary>
public partial class BoxStack : Node2D
{
    const float RefillSeconds = 0.9f; // how long until a new top box pops back on (try changing this!)
    float missingTime;                // the top box is gone for this many more seconds

    /// <summary>True while the top box is there (the self-test reads this).</summary>
    public bool TopBoxThere => missingTime <= 0;

    /// <summary>OPTI-MAX takes the top box.</summary>
    public void TakeTopBox()
    {
        missingTime = RefillSeconds;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (missingTime <= 0) return;
        missingTime -= (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Shapes.Ellipse(this, new Vector2(0, 1), new Vector2(52, 6), new Color(0, 0, 0, 0.2f));
        ThrownBox.DrawBox(this, new Vector2(-23, -ThrownBox.Height / 2));
        ThrownBox.DrawBox(this, new Vector2(23, -ThrownBox.Height / 2));
        if (TopBoxThere) ThrownBox.DrawBox(this, new Vector2(0, -ThrownBox.Height * 1.5f));
    }
}
