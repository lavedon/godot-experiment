using Godot;

/// <summary>
/// COZMOS and VECKTOR: two little tank-tread robots! (This is more of Robot: "partial" means one class spread over
/// several files. Bolt-E himself is drawn in Robot.cs.)
///
/// They came from two photos: a white robot with a red disc on the side of his head and blue eyes (COZMOS), and his
/// shiny black cousin with a gold panel and green eyes (VECKTOR). Seen from the side, facing right, each one has:
///   - a long rounded tread track at the bottom: a black rubber belt with little cleats that roll along, and big round
///     wheels (hubs) inside,
///   - a chunky box body on top of the treads,
///   - a big rounded head with a black screen face and two big glowing eyes,
///   - a forklift arm that reaches from the side of his head, forward and down, to a flat lift plate at the front.
/// The far-side tread and arm peek out behind, a bit darker (they're in the shadow).
/// Their colors come from the look (see Looks.cs). They're just as big as Bolt-E (130 px from the ground to the top of
/// the head), so they jump, flip and crash exactly like him.
/// </summary>
public partial class Robot
{
    // ---- COZMOS and VECKTOR's shapes (try changing these!) ----
    const float TreadBack = -48, TreadFront = 42;     // where the tread track starts and ends (left and right)
    const float TreadTop = -31, TreadBottom = -3;      // its top and bottom (the cleats stick out down to the ground)
    const float CleatLength = 3;                       // how far the little rubber cleats stick out
    const int CleatCount = 18;                         // how many cleats go round the belt
    const float HubRadius = 11;                        // how big the round wheels inside the tread are
    static readonly Vector2 BackHub = new(-32, -17), FrontHub = new(26, -17);  // where those two wheels are
    static readonly Rect2 TankBox = new(-36, -66, 66, 40);                     // the chunky box body
    static readonly Rect2 TankHeadBox = new(-36, -130, 78, 66);                // the big rounded head (its top is at -130, like Bolt-E's)
    static readonly Rect2 TankScreen = new(-10, -123, 47, 42);                 // the black screen face, on the front of the head
    static readonly Vector2 TankLeftEye = new(3, -102), TankRightEye = new(24, -102); // where the eyes sit on the screen
    static readonly Vector2 SideBadge = new(-22, -99);                         // the red disc (COZMOS) or gold panel (VECKTOR)
    static readonly Vector2 FarSide = new(5, -4);      // the far-side tread and arm peek out this much up and to the right
    static readonly Color Rubber = new(0.08f, 0.08f, 0.09f);       // the black rubber of the tread belt
    static readonly Color CleatGrey = new(0.2f, 0.2f, 0.22f);      // the cleats are a little lighter, so you can see them roll

    /// <summary>The forklift arm: from the side of his head, down to his chin, forward under his face, then down to the lift plate.</summary>
    static readonly Vector2[] ArmPath = { new(-22, -99), new(-8, -74), new(40, -68), new(47, -57) };
    static readonly Vector2[] farArmPath = new Vector2[ArmPath.Length]; // (the same arm on the far side, made once)

    /// <summary>The flat lift plate at the front, and the little lip at its bottom.</summary>
    static readonly Rect2 LiftPlate = new(43, -62, 9, 52), LiftLip = new(43, -15, 13, 5);

    float treadRoll;   // how far the treads have rolled (in pixels): the cleats and the wheels move along with it

    /// <summary>How far the treads have rolled, in pixels (the self-test checks they roll).</summary>
    public float TreadRoll => treadRoll;

    /// <summary>True for COZMOS and VECKTOR: tank treads instead of a hoverboard and legs.</summary>
    public bool OnTreads => BodyKind != RobotBody.BoltE;

    /// <summary>True if treads were drawn the last time he was drawn (the self-test checks this).</summary>
    public bool TreadsDrawn { get; private set; }

    /// <summary>Where the face screen is (in the robot's own drawing space). The self-test checks the eyes fit on it.</summary>
    public Rect2 FaceScreen => OnTreads ? TankScreen : new Rect2(-27, -121, 54, 48);

    /// <summary>Where the middle of each eye is: 0 = the back eye, 1 = the front eye.</summary>
    public Vector2 EyeSpot(int which) => OnTreads ? (which == 0 ? TankLeftEye : TankRightEye)
                                                  : (which == 0 ? new Vector2(-10, -97) : new Vector2(12, -97));

    /// <summary>How big an open eye is (width and height). VECKTOR's eyes are big rounded squares.</summary>
    public Vector2 EyeSize => BodyKind switch
    {
        RobotBody.Cozmos => new Vector2(14, 16),
        RobotBody.Vecktor => new Vector2(16, 15),
        _ => new Vector2(13, 15),
    };

    /// <summary>The color of the forklift arm: COZMOS's is white, VECKTOR's is dark gunmetal.</summary>
    Color ArmColor => BodyColor.Lerp(BezelColor, 0.35f);

    /// <summary>A tank robot draws itself, back to front (the parachute, flames, hat and magnet are drawn by Robot.cs).</summary>
    void DrawTankRobot()
    {
        DrawTread(FarSide, far: true);
        DrawTankBody();
        DrawTread(Vector2.Zero, far: false);
        DrawTankHead();
        DrawLiftArm();
    }

    /// <summary>The parts a tank robot blows apart into (back to front). The head goes back on last!</summary>
    List<RobotPart> TankParts() => new()
    {
        new RobotPart("right tread", new Vector2(-3, -17) + FarSide, 16, () => DrawTread(FarSide, far: true), FlatEvery: Mathf.Pi),
        new RobotPart("body", new Vector2(-3, -46), 22, DrawTankBody),
        new RobotPart("left tread", new Vector2(-3, -17), 16, () => DrawTread(Vector2.Zero, far: false), FlatEvery: Mathf.Pi),
        new RobotPart("head", new Vector2(3, -97), 35, DrawTankHead),
        new RobotPart("lift arm", new Vector2(16, -62), 18, DrawLiftArm, FlatEvery: Mathf.Pi),
    };

    // ---------- The treads ----------

    /// <summary>
    /// A tread track: the rubber belt (a long rounded shape), cleats that roll round it, and (on the near side) the side
    /// panel with the two big wheels. "shift" moves it (the far tread peeks out behind), and the far one is darker.
    /// </summary>
    void DrawTread(Vector2 shift, bool far)
    {
        float radius = (TreadBottom - TreadTop) / 2;
        var belt = new Rect2(TreadBack + shift.X, TreadTop + shift.Y, TreadFront - TreadBack, TreadBottom - TreadTop);
        if (Rim && !far) Shapes.RoundRect(this, belt.Grow(1.5f), radius + 1.5f, Ink); // (the dark outline, for light looks)
        DrawCleats(shift, far ? CleatGrey.Darkened(0.4f) : CleatGrey);
        Shapes.RoundRect(this, belt, radius, far ? Rubber.Darkened(0.3f) : Rubber);
        TreadsDrawn = true;
        if (far) return; // (the far side's wheels are hidden behind the near tread)

        // The side panel inside the belt, a little connector in the middle, and the two big wheels
        Shapes.RoundRect(this, new Rect2(TreadBack + 3, TreadTop + 3, TreadFront - TreadBack - 6, TreadBottom - TreadTop - 6), radius - 3, BezelColor);
        Shapes.RoundRect(this, new Rect2(-18, -22, 32, 10), 4, BezelColor.Lightened(0.15f));
        DrawLine(new Vector2(-10, -19), new Vector2(-10, -15), BezelColor.Darkened(0.3f), 2);
        DrawLine(new Vector2(6, -19), new Vector2(6, -15), BezelColor.Darkened(0.3f), 2);
        float spin = treadRoll / HubRadius; // a wheel turns once every time it rolls its own way round
        DrawHub(BackHub, spin);
        DrawHub(FrontHub, spin);
    }

    /// <summary>The little rubber cleats round the belt. They move along it as the treads roll (forward on top, back underneath).</summary>
    void DrawCleats(Vector2 shift, Color color)
    {
        float radius = (TreadBottom - TreadTop) / 2;
        float middleY = TreadTop + radius;
        float backX = TreadBack + radius, frontX = TreadFront - radius; // the middles of the two round ends
        float straight = frontX - backX;
        float around = 2 * straight + Mathf.Tau * radius;               // all the way round the belt
        float gap = around / CleatCount;

        for (int i = 0; i < CleatCount; i++)
        {
            float s = Mathf.PosMod(i * gap + treadRoll, around); // how far along the belt this cleat is (starting top left)
            Vector2 at, outward;
            if (s < straight) // along the top, going forward
            {
                at = new Vector2(backX + s, TreadTop);
                outward = Vector2.Up;
            }
            else if (s < straight + Mathf.Pi * radius) // round the front end, going down
            {
                float angle = -Mathf.Pi / 2 + (s - straight) / radius;
                outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                at = new Vector2(frontX, middleY) + outward * radius;
            }
            else if (s < 2 * straight + Mathf.Pi * radius) // along the bottom, going back
            {
                at = new Vector2(frontX - (s - straight - Mathf.Pi * radius), TreadBottom);
                outward = Vector2.Down;
            }
            else // round the back end, going up
            {
                float angle = Mathf.Pi / 2 + (s - 2 * straight - Mathf.Pi * radius) / radius;
                outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                at = new Vector2(backX, middleY) + outward * radius;
            }
            DrawLine(at + shift - outward, at + shift + outward * CleatLength, color, 4);
        }
    }

    /// <summary>One big round wheel inside the tread, turning as he rolls. COZMOS's are white with red dots, VECKTOR's dark grey with spokes.</summary>
    void DrawHub(Vector2 at, float spin)
    {
        DrawCircle(at, HubRadius + 1.5f, Rubber);
        DrawCircle(at, HubRadius, BoardColor);
        DrawArc(at, HubRadius - 3, 0, Mathf.Tau, 20, BoardColor.Darkened(0.18f), 1.5f, true);
        for (int i = 0; i < 3; i++)
        {
            var spot = at + Vector2.Right.Rotated(spin + i * Mathf.Tau / 3) * (HubRadius - 5);
            if (BodyKind == RobotBody.Cozmos) DrawCircle(spot, 2.3f, HeadphoneColor);   // a red dot
            else DrawLine(at, spot, BodyColor, 3, true);                                 // a dark spoke
        }
        DrawCircle(at, 3, BoardColor.Darkened(0.3f));
    }

    // ---------- The body and the forklift arm ----------

    /// <summary>The chunky box body (and, behind it, the far side's forklift arm and lift plate).</summary>
    void DrawTankBody()
    {
        // The far-side arm and plate first, so the body covers them
        for (int i = 0; i < ArmPath.Length; i++) farArmPath[i] = ArmPath[i] + FarSide;
        var farColor = ArmColor.Darkened(0.35f);
        DrawPolyline(farArmPath, farColor, 7, true);
        Shapes.RoundRect(this, new Rect2(LiftPlate.Position + FarSide, LiftPlate.Size), 3, farColor);

        if (Rim)
        {
            Shapes.RoundRect(this, TankBox.Grow(1.5f), 10.5f, Ink);
            RimDrawn = true;
        }
        Shapes.RoundRect(this, TankBox, 9, BodyColor.Darkened(0.07f));
        Shapes.RoundRect(this, new Rect2(6, -64, 24, 36), 9, BezelColor); // the round shiny drum at the front
        DrawLine(new Vector2(12, -60), new Vector2(12, -33), BezelColor.Lightened(0.3f), 2.5f, true); // its shine
        for (int i = 0; i < 3; i++) // three little air vents
            DrawLine(new Vector2(-29, -50 + i * 5), new Vector2(-15, -50 + i * 5), BezelColor.Darkened(0.2f), 2, true);
    }

    /// <summary>The near-side forklift arm (a bendy bar with round joints) and the flat lift plate at the front.</summary>
    void DrawLiftArm()
    {
        const float width = 10;
        var color = ArmColor;
        if (Rim)
        {
            DrawPolyline(ArmPath, Ink, width + 3, true);
            for (int i = 1; i < ArmPath.Length - 1; i++) DrawCircle(ArmPath[i], (width + 3) / 2, Ink);
            Shapes.RoundRect(this, LiftPlate.Grow(1.5f), 4.5f, Ink);
            Shapes.RoundRect(this, LiftLip.Grow(1.5f), 3.5f, Ink);
        }
        DrawPolyline(ArmPath, color, width, true);
        for (int i = 1; i < ArmPath.Length - 1; i++) DrawCircle(ArmPath[i], width / 2, color); // round elbows
        DrawPolyline(ArmPath, BezelColor.Lightened(0.2f), 1.5f, true);                         // a thin line along the middle

        // The lift plate and its lip (for picking up cubes!)
        Shapes.RoundRect(this, LiftPlate, 3, color);
        Shapes.RoundRect(this, LiftLip, 2, color);
        DrawLine(new Vector2(47.5f, -57), new Vector2(47.5f, -18), BezelColor, 2, true);

        // Where the arm joins the side of his head: a little round cap
        DrawCircle(ArmPath[0], 5, BezelColor);
        DrawCircle(ArmPath[0], 2, BodyColor.Darkened(0.4f));
    }

    // ---------- The head ----------

    /// <summary>The big rounded head: a chin, the side badge (red disc or gold panel), a shine on top, and the screen face.</summary>
    void DrawTankHead()
    {
        if (Rim)
        {
            Shapes.RoundRect(this, TankHeadBox.Grow(2.5f), 22.5f, Ink);
            RimDrawn = true;
        }
        Shapes.RoundRect(this, TankHeadBox, 20, BodyColor);
        Shapes.RoundRect(this, new Rect2(-32, -73, 70, 9), 4.5f, BodyColor.Darkened(0.12f)); // the chin

        if (BodyKind == RobotBody.Vecktor)
        {
            // VECKTOR: a gold panel on the side of his head, and a glossy shine (his head is shiny black)
            Shapes.RoundRect(this, new Rect2(-31, -116, 16, 30), 6, HeadphoneColor);
            Shapes.RoundRect(this, new Rect2(-29, -114, 11, 25), 5, HeadphoneColor.Lightened(0.18f));
            Shapes.RoundRect(this, new Rect2(-22, -127, 48, 4), 2, new Color(1, 1, 1, 0.22f));
        }
        else
        {
            // COZMOS: a red disc on the side of his head, with a white middle
            DrawCircle(SideBadge, 11, HeadphoneColor);
            DrawCircle(SideBadge, 6.5f, BodyColor);
            DrawCircle(SideBadge, 3, BezelColor);
        }

        // The black screen face, with a faint reflection along its top
        Shapes.RoundRect(this, TankScreen, 12, ScreenColor);
        DrawLine(new Vector2(0, -120), new Vector2(26, -120), new Color(1, 1, 1, 0.08f), 2, true);

        DrawFace(TankLeftEye, TankRightEye);
    }
}
