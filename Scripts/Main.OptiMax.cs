using Godot;

/// <summary>
/// OPTI-MAX, the fourth boss! (This is more of Main: "partial" means one class spread over several files.)
/// The bosses take turns: BIG RUSTY, VROOMBA, DRONEZILLA, OPTI-MAX (see BossForFight in Main.Vroomba.cs).
/// (OPTI-MAX himself, his moves and his drawing, is in OptiMax.cs. His boxes are ThrownBox.cs, his soccer ball is
/// KickBall.cs, his shockwaves are StompWave.cs and his box stacks are BoxStack.cs.) Here: what happens when OPTI-MAX
/// does something (sounds, shakes, dust, popups...), his boxes, balls and shockwaves, and checking if any of them
/// bumped into Bolt-E.
/// </summary>
public partial class Main
{
    // ---- OPTI-MAX (try changing these!) ----
    static readonly Color OptiTeal = OptiMax.Teal;
    static readonly Color DingGreen = new(0.45f, 1f, 0.55f);
    /// <summary>The tip when he walks in (the arrows are the same as in the other bosses' tips).</summary>
    const string OptiMaxHint = "Jump the boxes and waves! STOMP him while he UPDATES!  ← →  / stick to ride";

    readonly List<ThrownBox> thrownBoxes = new();  // his cardboard boxes (flying or bouncing)
    readonly List<KickBall> kickBalls = new();     // his soccer balls
    readonly List<StompWave> stompWaves = new();   // the teal shockwaves from his SUPER STOMP
    readonly List<BoxStack> boxStacks = new();     // his two little stacks of boxes, at the ends of the arena
    bool optiSoundsOn;                             // true while his fight is on (his sounds may be playing)

    /// <summary>All of OPTI-MAX's sounds (they all stop when his fight is over, so nothing keeps playing).</summary>
    static readonly Sfx[] OptiSounds =
    {
        Sfx.RobotStep, Sfx.BoxWhoosh, Sfx.BoxCrunch, Sfx.Boing, Sfx.Kick, Sfx.Chirp, Sfx.Ding, Sfx.ErrorBuzz, Sfx.DanceBeat,
    };

    /// <summary>OPTI-MAX walks in! (Everything he does that the game needs to hear about is hooked up here.)</summary>
    OptiMax MakeOptiMax()
    {
        var opti = new OptiMax { GroundY = GroundY, ZIndex = 5 };

        // His two stacks of boxes, one at each end of the arena
        foreach (float x in new[] { OptiMax.StackLeftX, OptiMax.StackRightX })
        {
            var stack = new BoxStack { Position = new Vector2(x, GroundY), ZIndex = 2 };
            AddChild(stack);
            boxStacks.Add(stack);
        }

        opti.Laughed += () =>
        {
            popups.Add("BEEP BOP DANCE!", opti.Position + new Vector2(0, -330), OptiTeal, 34);
            sounds.Play(Sfx.DanceBeat);
        };
        opti.Footstep += at =>
        {
            // Clank! Heavier when he walks in, quicker and lighter when he runs (and quiet once the fight is over)
            if (state is not (GameState.Playing or GameState.Rebuilding)) return;
            bool running = opti.CurrentMove is "Running" or "FlyingAway";
            sounds.Play(Sfx.RobotStep, running ? Rand(1.15f, 1.3f) : Rand(0.9f, 1.05f));
            if (opti.CurrentMove == "Entering") Rumble(0.2f, 0.15f, 0.06f);
        };
        opti.BoxPickedUp += x =>
        {
            foreach (var stack in boxStacks)
                if (Mathf.Abs(stack.Position.X - x) < 1) stack.TakeTopBox();
        };
        opti.BoxThrown += AddThrownBox;
        opti.Jumped += () => sounds.Play(Sfx.Jump, 0.5f);
        opti.Stomped += x =>
        {
            // STOMP! Two glowing shockwaves run away from him, both ways
            AddStompWave(x - 40, -1);
            AddStompWave(x + 40, 1);
            sounds.Play(Sfx.Slam, 0.85f);
            shake = 0.45f;
            Rumble(0.7f, 0.9f, 0.35f);
            Burst(new Vector2(x, GroundY), 22, DustColor, 300f);
        };
        opti.BallKicked += AddKickBall;
        opti.UpdateStarted += at =>
        {
            popups.Add("UPDATING...", at + new Vector2(0, -120), OptiTeal, 30);
            sounds.Play(Sfx.Chirp, 0.8f);
        };
        opti.Chirped += () => sounds.Play(Sfx.Chirp, Rand(0.9f, 1.4f));
        opti.UpdateDone += at =>
        {
            popups.Add("DING!", at + new Vector2(0, -90), DingGreen, 34);
            sounds.Play(Sfx.Ding);
        };
        opti.Errored += at =>
        {
            sounds.Play(Sfx.ErrorBuzz);
            Burst(at, 14, new Color(1f, 0.92f, 0.35f), 280f); // sparks fly from his head
        };
        opti.FellOver += at =>
        {
            // CLANK! Down he goes, flat on his back
            sounds.Play(Sfx.Slam, 1.3f);
            sounds.Play(Sfx.Clink, 0.5f);
            shake = 0.35f;
            Rumble(0.5f, 0.6f, 0.3f);
            Burst(new Vector2(at.X, GroundY), 18, DustColor, 260f);
        };
        return opti;
    }

    /// <summary>A cardboard box flies from OPTI-MAX's hands in a high arc, to land at landX (its shadow shows where).</summary>
    void AddThrownBox(Vector2 from, float landX, float seconds)
    {
        var box = new ThrownBox { GroundY = GroundY, ZIndex = 6 };
        box.Throw(from, landX, seconds);
        box.Landed += at =>
        {
            sounds.Play(Sfx.BoxCrunch, 1.2f);
            Burst(at, 6, DustColor, 140f);
        };
        box.Broke += at =>
        {
            // Crunch! A puff of cardboard bits
            sounds.Play(Sfx.BoxCrunch, 0.85f);
            Burst(at + new Vector2(0, -18), 14, ThrownBox.Cardboard, 240f);
            Burst(at + new Vector2(0, -18), 6, Colors.White, 160f);
        };
        AddChild(box);
        thrownBoxes.Add(box);
        sounds.Play(Sfx.BoxWhoosh);
    }

    /// <summary>KICK! A soccer ball bounces off toward Bolt-E (direction -1 = left, 1 = right).</summary>
    void AddKickBall(Vector2 at, float direction)
    {
        var ball = new KickBall { Position = at, GroundY = GroundY, ZIndex = 6 };
        ball.Kick(direction);
        ball.Bounced += _ => sounds.Play(Sfx.Boing, Rand(0.95f, 1.1f));
        ball.HitWall += _ => sounds.Play(Sfx.Boing, 1.4f);
        ball.Popped += where =>
        {
            sounds.Play(Sfx.Spit, 1.8f); // pop!
            Burst(where, 12, Colors.White, 220f);
        };
        AddChild(ball);
        kickBalls.Add(ball);
        sounds.Play(Sfx.Kick);
    }

    /// <summary>A teal shockwave runs along the ground from here (direction -1 = left, 1 = right).</summary>
    void AddStompWave(float x, float direction)
    {
        var wave = new StompWave { Position = new Vector2(x, GroundY), Direction = direction, ZIndex = 6 };
        AddChild(wave);
        stompWaves.Add(wave);
    }

    /// <summary>
    /// Every frame (in every game state): boxes, balls and waves that are done are tidied away, everything OPTI-MAX
    /// made (and his box stacks) puffs away when he's beaten, and the stacks go when he's gone. When his fight is over
    /// (he's beaten, or it's game over), his boxes, balls and waves puff away and all his sounds stop.
    /// </summary>
    void UpdateOptiMax(float dt)
    {
        RemoveFinished(thrownBoxes, box => box.Finished);
        RemoveFinished(kickBalls, ball => ball.Finished);
        RemoveFinished(stompWaves, wave => wave.Finished);

        var opti = boss as OptiMax;
        bool fightOn = opti is { IsBeaten: false } && state is GameState.Playing or GameState.Rebuilding;
        if (fightOn)
        {
            optiSoundsOn = true;
        }
        else if (optiSoundsOn)
        {
            optiSoundsOn = false;
            ClearOptiDangers();
            StopOptiSounds();
        }
        if (opti is { IsBeaten: true } || (opti is null && boxStacks.Count > 0))
            PuffAway(boxStacks, _ => true); // (he's beaten, or he left)
    }

    void StopOptiSounds()
    {
        foreach (var sound in OptiSounds) sounds.Stop(sound);
    }

    /// <summary>Takes away the things in a list that are done (no puff: they already broke, popped or left the screen).</summary>
    static void RemoveFinished<T>(List<T> things, Func<T, bool> done) where T : Node2D
    {
        for (int i = things.Count - 1; i >= 0; i--)
        {
            if (!done(things[i])) continue;
            things[i].QueueFree();
            things.RemoveAt(i);
        }
    }

    /// <summary>Poof! Every box, ball and shockwave puffs away (after a rebuild, and when OPTI-MAX is beaten).</summary>
    void ClearOptiDangers()
    {
        PuffAway(thrownBoxes, _ => true);
        PuffAway(kickBalls, _ => true);
        PuffAway(stompWaves, _ => true);
    }

    /// <summary>A fresh start (see ClearWorld): no boxes, balls, waves or box stacks, and no dance beat.</summary>
    void ClearOptiThings()
    {
        foreach (var box in thrownBoxes) box.QueueFree();
        foreach (var ball in kickBalls) ball.QueueFree();
        foreach (var wave in stompWaves) wave.QueueFree();
        foreach (var stack in boxStacks) stack.QueueFree();
        thrownBoxes.Clear();
        kickBalls.Clear();
        stompWaves.Clear();
        boxStacks.Clear();
        optiSoundsOn = false;
        StopOptiSounds();
    }

    /// <summary>Did a box, a ball or a shockwave bump into Bolt-E? (True if he really crashed.)</summary>
    bool CheckOptiBumps(Rect2 robotBox)
    {
        foreach (var box in thrownBoxes)
        {
            if (box.Dangerous && robotBox.Intersects(box.Hitbox) && Bonk(box)) return true;
        }
        foreach (var ball in kickBalls)
        {
            if (ball.Dangerous && robotBox.Intersects(ball.Hitbox) && Bonk(ball)) return true;
        }
        foreach (var wave in stompWaves)
        {
            if (robotBox.Intersects(wave.Hitbox) && Bonk(wave)) return true;
        }
        return false;
    }
}
