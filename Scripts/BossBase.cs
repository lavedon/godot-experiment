using Godot;

/// <summary>
/// Which boss comes: BIG RUSTY (Boss.cs), VROOMBA, the giant robot vacuum (Vroomba.cs), or DRONEZILLA, the giant attack
/// drone (Dronezilla.cs). (They take turns in this order: see BossForFight in Main.Vroomba.cs.)
/// </summary>
public enum BossKind { BigRusty, Vroomba, Dronezilla }

/// <summary>
/// What every boss has, so Main can fight any of them the same way: health (6, with ANGRY and FURIOUS phases),
/// a bump box, when he can be stomped and when bumping into him hurts, flying away when Bolt-E runs out of
/// batteries, the "ha ha!" when Bolt-E crashes, and blowing up at the end.
/// The bosses themselves are BIG RUSTY (Boss.cs), VROOMBA (Vroomba.cs) and DRONEZILLA (Dronezilla.cs). They take turns:
/// see Main.Vroomba.cs.
/// ("abstract" means there's never a plain BossBase: it's always one of the real bosses.)
/// Position is the middle of the bottom of the boss.
/// </summary>
public abstract partial class BossBase : Node2D
{
    /// <summary>How many stomps it takes to beat a boss.</summary>
    public const int MaxHealth = 6;

    /// <summary>The boss's name, for his health bar and the big words, like "BIG RUSTY", "VROOMBA" or "DRONEZILLA".</summary>
    public abstract string DisplayName { get; }

    public float GroundY;
    public float WorldSpeed;   // so his pieces slide along with the ground after he blows up
    public float TargetX;      // where Bolt-E is, so he can aim at him
    public int Health { get; protected set; } = MaxHealth;

    /// <summary>1 = normal, 2 = ANGRY, 3 = FURIOUS</summary>
    public int Phase => Health > 4 ? 1 : Health > 2 ? 2 : 3;

    /// <summary>His bump box: touching it is a stomp (from above, when he can be stomped), a crash, or nothing.</summary>
    public abstract Rect2 Hitbox { get; }
    /// <summary>The top of his bump box (land here to stomp him!)</summary>
    public float Top => Hitbox.Position.Y;

    /// <summary>True when landing on top of him stomps him (when he's dizzy).</summary>
    public abstract bool CanBeStomped { get; }
    /// <summary>True when bumping into him is a crash.</summary>
    public abstract bool IsDangerous { get; }
    /// <summary>True once his health reaches 0 (he's breaking down or blown up).</summary>
    public abstract bool IsBeaten { get; }
    /// <summary>True once he blew up into pieces.</summary>
    public abstract bool HasBlownUp { get; }
    /// <summary>True when he's gone: his pieces are done bouncing, or he flew (or rolled) off the screen.</summary>
    public abstract bool Finished { get; }
    /// <summary>What he's doing right now, like "Dizzy" (the self-test reads this).</summary>
    public abstract string CurrentMove { get; }

    /// <summary>Ouch! Called when Bolt-E lands on him. It only counts while he can be stomped.</summary>
    public void Stomp()
    {
        if (!CanBeStomped) return;
        LoseHealth(1);
    }

    /// <summary>
    /// Ouch! He loses some health and gets hurt. With no health left he shakes, pops and blows up.
    /// (A stomp takes 1. The self-test calls this too, so it doesn't have to wait for him to get dizzy 6 times.)
    /// </summary>
    public abstract void LoseHealth(int amount);

    /// <summary>Bolt-E ran out of batteries: the boss leaves (flies or rolls away).</summary>
    public abstract void FlyAway();

    /// <summary>
    /// Bolt-E crashed but has a spare battery: the boss laughs at him, and waits holdSeconds before attacking again,
    /// so Bolt-E has time to put himself back together. (Never while he's dizzy: he never steals your stomp chance!)
    /// </summary>
    public abstract void Taunt(float holdSeconds);

    // ---- Being driven by Dad with a second controller (only Big Rusty can be: see Boss.cs and Main.Driver.cs) ----

    /// <summary>True once Dad (or Mom) took over with the second controller.</summary>
    public bool DriverControlled { get; protected set; }
    /// <summary>Can Dad take over (or give an order) right now? A boss that can't be driven always says no.</summary>
    public virtual bool CanBeDriven => false;
    /// <summary>Dad takes over. (A boss that can't be driven ignores it.)</summary>
    public virtual void TakeControl() { }
    /// <summary>Dad asks for a move. (A boss that can't be driven is always "Busy".)</summary>
    public virtual DriveAnswer Request(BossMove move) => DriveAnswer.Busy;

    // ---- Things every boss does that the rest of the game needs to know about ----
    public event Action<int>? GotAngry;         // he moved to phase 2 or 3
    public event Action? Laughed;               // HA HA HA! (or BEEP BOOP! or BZZT BZZT!)
    public event Action<Vector2>? Popped;       // a little explosion while he's breaking down
    public event Action? BlewUp;                // the big KABOOM at the very end
    public event Action<bool>? PieceBounced;    // one of his pieces hit the ground (true = a big piece)

    // (Only BossBase itself may make its events happen, so the bosses call these.)
    protected void TellGotAngry(int phase) => GotAngry?.Invoke(phase);
    protected void TellLaughed() => Laughed?.Invoke();
    protected void TellPopped(Vector2 at) => Popped?.Invoke(at);
    protected void TellBlewUp() => BlewUp?.Invoke();
    protected void TellPieceBounced(bool bigPiece) => PieceBounced?.Invoke(bigPiece);

    /// <summary>Makes movement start and stop gently instead of jerking.</summary>
    protected static float Smooth(float t)
    {
        t = Mathf.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }
}
