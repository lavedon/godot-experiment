using Godot;

/// <summary>
/// WHERE EVERYTHING IS on the WORLD MAP, and HOW IT LOOKS (try changing these!): the 7 worlds' spots, the roads between
/// them, the two islands, each world's little picture, the rides (the rocket, the space bubble, the space capsule and
/// the surfboard), the next boss's badge, and where the GO bubble, the NEXT arrow, the gold flags, the boss badge and
/// "NEW!" go. (The self-test checks that none of them bump into little Bolt-E or into each other, that no road runs under
/// a GO bubble or a NEXT arrow, and that the map stays on the screen.) WorldMap.cs runs the map's show with these, and
/// walks little Bolt-E along the roads.
///
/// The roads and the coasts are smooth curves (Catmull-Rom curves) through the points below. They're worked out once,
/// when the game starts. Every drawing helper here draws with little lists of points that are made once and used again
/// and again (the "scratch" lists), so the map can be drawn 60 times a second without ever making new ones.
/// The map is a loop, clockwise: right along the big island, up by rocket to THE MOON, left through space to UPSIDE-DOWN
/// STATION, down by space capsule to VOLCANO ISLAND, and a short surf home to SUNNY HILLS.
/// </summary>
public static class WorldMapArt
{
    // ---- The 7 worlds: where little Bolt-E's feet stand (try changing these!) ----
    public static readonly Vector2[] Spots =
    {
        new(300, 545),  // 0. SUNNY HILLS
        new(520, 560),  // 1. CANDY LAND
        new(770, 505),  // 2. NIGHT CITY
        new(1020, 430), // 3. SNOWY PEAKS
        new(1085, 128), // 4. THE MOON
        new(640, 128),  // 5. UPSIDE-DOWN STATION
        new(125, 440),  // 6. VOLCANO ISLAND
    };

    // ---- The roads: from each world to the next one (the last one surfs home to Sunny Hills) ----
    static readonly Vector2[][] RoadPoints =
    {
        new Vector2[] { new(300, 545), new(370, 588), new(450, 592), new(520, 560) },
        new Vector2[] { new(520, 560), new(600, 525), new(690, 540), new(770, 505) },
        new Vector2[] { new(770, 505), new(850, 455), new(940, 465), new(1020, 430) },
        new Vector2[] { new(1020, 430), new(1065, 412), new(1105, 392), new(1108, 300), new(1100, 200), new(1085, 128) }, // (the rocket pad, then up)
        new Vector2[] { new(1085, 128), new(1000, 80), new(880, 68), new(760, 80), new(640, 128) }, // (low enough for his space bubble)
        new Vector2[] { new(640, 128), new(560, 150), new(440, 180), new(320, 235), new(232, 300), new(205, 370), new(160, 425), new(125, 440) },
        new Vector2[] { new(125, 440), new(150, 500), new(210, 550), new(300, 545) },
    };

    /// <summary>How little Bolt-E rides each road between levels (the start map just glides, or does a STAR HOP).</summary>
    public static readonly Ride[] Rides = { Ride.Road, Ride.Road, Ride.Road, Ride.Rocket, Ride.Space, Ride.Capsule, Ride.Sea };

    /// <summary>How many seconds each ride takes between levels (try changing these!).</summary>
    public static readonly float[] RideSeconds = { 1.0f, 1.1f, 1.1f, 1.8f, 1.6f, 2.2f, 1.0f };

    public static readonly Vector2 RocketPad = new(1105, 394);    // where the rocket waits on SNOWY PEAKS
    public static readonly Vector2 SplashSpot = new(232, 300);    // where the space capsule splashes down
    const float DotSpacing = 18;                                    // a road has about one dot every this many pixels
    const float DotEndGap = 30;                                     // ...and no dots this close to a world's dot

    // ---- Each world's picture on the map (a cloud covers it while the world is LOCKED) ----

    /// <summary>The area each world's picture covers, with everything in it that moves (the smoke, the snow, the rocket).</summary>
    public static readonly Rect2[] LandmarkBoxes =
    {
        new(234, 419, 186, 163),  // SUNNY HILLS: hills with eyes, flowers, and Bolt-E's garage
        new(416, 481, 228, 106),  // CANDY LAND: a frosting patch with lollipops, gumdrops and sprinkles
        new(677, 405, 206, 130),  // NIGHT CITY: skyscrapers under their own night sky
        new(894, 320, 238, 182),  // SNOWY PEAKS: snowy mountains, pines, falling snow, and the ROCKET PAD
        new(1025, 58, 120, 120),  // THE MOON
        new(524, 72, 232, 58),    // UPSIDE-DOWN STATION
        new(55, 226, 140, 219),   // VOLCANO ISLAND (with its smoke, all the way up)
    };

    /// <summary>Where the giant "?" of a LOCKED world's cloud goes (away from the world's dot).</summary>
    public static readonly Vector2[] QuestionSpots =
    {
        new(327, 465), new(470, 528), new(785, 450), new(968, 388), new(1124, 96), new(640, 100), new(125, 380),
    };

    // ---- Where the words and signs go, from each world's spot (try changing these!) ----

    /// <summary>The GO bubble (150 x 58): above little Bolt-E, to the left of him on THE MOON, and under him on the Station.</summary>
    public static readonly Rect2[] BubbleBoxes =
    {
        new(-75, -160, 150, 58), new(-75, -160, 150, 58), new(-75, -160, 150, 58), new(-75, -160, 150, 58),
        new(-225, -30, 150, 58), new(-30, 30, 150, 58), new(-75, -160, 150, 58),
    };

    /// <summary>Where the GO bubble's little tail points: 10 pixels from Bolt-E's head (or his feet, on the Station).</summary>
    public static readonly Vector2[] TailTips =
    {
        new(0, -88), new(0, -88), new(0, -88), new(0, -88), new(-36, -34), new(10, 18), new(0, -88),
    };

    /// <summary>The bouncing NEXT arrow: its box, and the way the arrow points (down, right or up at the world).</summary>
    public static readonly Rect2[] NextBoxes =
    {
        new(-30, -180, 60, 72), new(-30, -180, 60, 72), new(-30, -180, 60, 72), new(-30, -180, 60, 72),
        new(-150, -40, 64, 80), new(-32, 30, 64, 80), new(-30, -180, 60, 72),
    };
    public static readonly Vector2[] NextPoints =
    {
        Vector2.Down, Vector2.Down, Vector2.Down, Vector2.Down, Vector2.Right, Vector2.Up, Vector2.Down,
    };

    /// <summary>The middle of the next boss's badge (on the side of the world where no road goes).</summary>
    public static readonly Vector2[] TeaserOffsets =
    {
        new(-62, -34), new(62, 28), new(62, 30), new(62, 40), new(82, 48), new(132, 0), new(-56, 50),
    };

    /// <summary>The side of its dot a CLEARED world's gold flag stands on (1 = right, -1 = left): the side no road goes.</summary>
    public static readonly int[] FlagSides = { 1, -1, -1, -1, 1, -1, -1 };
    public const float FlagX = 36;   // the flag's pole stands this far from the dot (so it's beside little Bolt-E, never on him)

    /// <summary>The middle of the "NEW!" words after a cloud puffs away.</summary>
    public static readonly Vector2[] NewOffsets =
    {
        new(56, -40), new(56, -40), new(56, -40), new(-56, -40), new(-40, 62), new(-56, 50), new(56, -40),
    };

    // ---- The panels and the name box ----
    public static readonly Rect2 CornerPanel = new(16, 10, 330, 130);   // RUN INFO (between levels) or OWNER (the start map)
    public static readonly Rect2 NameBox = new(336, 620, 608, 98);      // the world's name, at the bottom (like SMW!)
    public const float NameBoxTextWidth = 560;                         // the words in it shrink to fit this wide
    public const float BigWordsWidth = 900;                            // "SUNNY HILLS CLEAR!" shrinks to fit this wide
    public const float BigWordsBaseline = 300;                         // ...and stands on this line (a second line at 342)

    // ---- Colors ----
    public static readonly Color Ink = new(0.13f, 0.18f, 0.32f);
    public static readonly Color Gold = new(1f, 0.78f, 0.15f);
    public static readonly Color Glow = new(0.35f, 0.95f, 1f);
    public static readonly Color Sea = new(0.22f, 0.52f, 0.92f);
    public static readonly Color DeepSea = new(0.16f, 0.42f, 0.84f);
    public static readonly Color ShallowWater = new(0.45f, 0.75f, 1f);
    public static readonly Color Grass = new(0.42f, 0.78f, 0.38f);
    public static readonly Color GrassEdge = new(0.27f, 0.6f, 0.3f);
    public static readonly Color Beach = new(0.98f, 0.88f, 0.6f);
    public static readonly Color CoastEdge = new(0.66f, 0.52f, 0.34f);
    public static readonly Color VolcanoRock = new(0.36f, 0.24f, 0.2f);
    public static readonly Color VolcanoRockEdge = new(0.25f, 0.16f, 0.14f);
    public static readonly Color DarkSand = new(0.6f, 0.48f, 0.38f);
    public static readonly Color OpenRed = new(0.95f, 0.25f, 0.22f);
    public static readonly Color ClearedBlue = new(0.25f, 0.55f, 1f);
    public static readonly Color StoneGrey = new(0.55f, 0.57f, 0.62f);
    public static readonly Color CloudWhite = new(0.97f, 0.98f, 1f), CloudShade = new(0.8f, 0.84f, 0.93f), CloudEdge = new(0.55f, 0.62f, 0.8f);
    public static readonly Color NebulaPurple = new(0.5f, 0.4f, 0.75f), NebulaShade = new(0.36f, 0.28f, 0.58f), NebulaEdge = new(0.22f, 0.16f, 0.4f);
    public static readonly Color LandDot = new(0.98f, 0.86f, 0.55f), SeaDotRing = new(0.2f, 0.45f, 0.85f);
    public static readonly Color StarDot = new(1f, 0.95f, 0.6f), GoldDot = new(1f, 0.85f, 0.3f), GoldLight = new(1f, 0.93f, 0.5f);
    public static readonly Color DustColor = new(0.93f, 0.84f, 0.62f, 0.85f);
    public static readonly Color NeonPink = new(1f, 0.35f, 0.85f);
    public static readonly Color[] Rainbow =
    {
        new(1f, 0.3f, 0.3f), new(1f, 0.6f, 0.2f), new(1f, 0.9f, 0.25f), new(0.35f, 0.9f, 0.4f), new(0.35f, 0.65f, 1f), new(0.7f, 0.45f, 1f),
    };
    /// <summary>Confetti and fireworks: gold, pink, cyan, white, green, orange and violet.</summary>
    public static readonly Color[] PartyColors =
    {
        new(1f, 0.82f, 0.2f), new(1f, 0.45f, 0.75f), new(0.35f, 0.95f, 1f), new(1f, 1f, 1f), new(0.45f, 0.95f, 0.45f), new(1f, 0.6f, 0.2f),
        new(0.75f, 0.5f, 1f),
    };

    // ---- The words on the map (made once) ----
    public static readonly string[] ClearWords = MakeClearWords();
    static string[] MakeClearWords()
    {
        var words = new string[Worlds.All.Length];
        for (int i = 0; i < words.Length; i++) words[i] = Worlds.All[i].Name + " CLEAR!";
        return words;
    }

    // ---------- Worked out once: the coasts, the roads and their dots ----------

    /// <summary>The big island's coast (a closed loop of points), and the same loop with the first point again at the end.</summary>
    public static readonly Vector2[] MainIsland = ClosedCurve(new Vector2[]
    {
        new(238, 560), new(250, 500), new(290, 445), new(360, 412), new(450, 410), new(540, 425), new(620, 405), new(700, 410),
        new(770, 392), new(850, 382), new(930, 350), new(1000, 318), new(1080, 310), new(1150, 328), new(1186, 380),
        new(1182, 440), new(1142, 492), new(1060, 518), new(960, 542), new(880, 575), new(790, 592), new(690, 602),
        new(600, 608), new(500, 610), new(400, 606), new(320, 598), new(262, 585),
    }, 6);
    public static readonly Vector2[] MainIslandLoop = Loop(MainIsland);

    /// <summary>VOLCANO ISLAND's coast.</summary>
    public static readonly Vector2[] VolcanoIsland = ClosedCurve(new Vector2[]
    {
        new(35, 400), new(50, 345), new(100, 312), new(160, 315), new(212, 348), new(224, 405), new(196, 455), new(130, 480), new(64, 466),
    }, 8);
    public static readonly Vector2[] VolcanoIslandLoop = Loop(VolcanoIsland);

    // Each road, worked out: closely spaced points along it, how far along the road each one is, and its dots
    static readonly Vector2[][] roadSamples = new Vector2[RoadPoints.Length][];
    static readonly float[][] roadDistances = new float[RoadPoints.Length][];
    public static readonly Vector2[][] Dots = new Vector2[RoadPoints.Length][];
    /// <summary>The kind of each dot: 0 = on land (a beige dot), 1 = on the sea (a white dot), 2 = in space (a little gold star).</summary>
    public static readonly int[][] DotKinds = new int[RoadPoints.Length][];
    public static readonly float[] RoadLengths = new float[RoadPoints.Length];
    /// <summary>How far along its road the rocket pad is (road 3) and where the capsule splashes down (road 5).</summary>
    public static readonly float PadDistance, SplashDistance;

    static WorldMapArt()
    {
        for (int road = 0; road < RoadPoints.Length; road++)
        {
            var samples = OpenCurve(RoadPoints[road], 24);
            var distances = new float[samples.Length];
            for (int i = 1; i < samples.Length; i++) distances[i] = distances[i - 1] + samples[i].DistanceTo(samples[i - 1]);
            roadSamples[road] = samples;
            roadDistances[road] = distances;
            float length = distances[^1];
            RoadLengths[road] = length;

            int count = Math.Max(1, Mathf.RoundToInt(length / DotSpacing));
            var dots = new Vector2[count];
            var kinds = new int[count];
            for (int i = 0; i < count; i++)
            {
                float along = count == 1 ? length / 2 : DotEndGap + (length - 2 * DotEndGap) * i / (count - 1);
                dots[i] = PointOnRoad(road, along);
                kinds[i] = dots[i].Y < 240 ? 2 : Geometry2D.IsPointInPolygon(dots[i], MainIsland) ||
                                                 Geometry2D.IsPointInPolygon(dots[i], VolcanoIsland) ? 0 : 1;
            }
            Dots[road] = dots;
            DotKinds[road] = kinds;
        }
        PadDistance = DistanceNearest(3, RocketPad);
        SplashDistance = DistanceNearest(5, SplashSpot);
    }

    /// <summary>How many dots road number "road" has (the self-test reads this).</summary>
    public static int DotCount(int road) => Dots[road].Length;

    /// <summary>The spot "distance" pixels along road number "road" (it never makes anything new: fine every frame).</summary>
    public static Vector2 PointOnRoad(int road, float distance)
    {
        var samples = roadSamples[road];
        var distances = roadDistances[road];
        if (distance <= 0) return samples[0];
        if (distance >= distances[^1]) return samples[^1];
        int low = 0, high = distances.Length - 1;
        while (high - low > 1) // (halving the search each time: it finds the spot quickly)
        {
            int middle = (low + high) / 2;
            if (distances[middle] <= distance) low = middle;
            else high = middle;
        }
        float t = (distance - distances[low]) / Mathf.Max(0.0001f, distances[high] - distances[low]);
        return samples[low].Lerp(samples[high], t);
    }

    /// <summary>Which way road number "road" goes at "distance" pixels along it (a length-1 direction).</summary>
    public static Vector2 WayOfRoad(int road, float distance)
    {
        var way = PointOnRoad(road, distance + 3) - PointOnRoad(road, distance - 3);
        return way.LengthSquared() > 0.0001f ? way.Normalized() : Vector2.Right;
    }

    /// <summary>What's under a spot "distance" pixels along a road: 0 = land, 1 = the sea, 2 = space (from its nearest dot).</summary>
    public static int KindAt(int road, float distance)
    {
        var kinds = DotKinds[road];
        if (kinds.Length == 1) return kinds[0];
        float spacing = (RoadLengths[road] - 2 * DotEndGap) / (kinds.Length - 1);
        return kinds[Mathf.Clamp(Mathf.RoundToInt((distance - DotEndGap) / spacing), 0, kinds.Length - 1)];
    }

    /// <summary>All the points of road number "road" (the self-test checks that no road runs under a GO bubble or a NEXT arrow).</summary>
    public static Vector2[] RoadSamples(int road) => roadSamples[road];

    // ---------- Where each thing goes (the self-test checks that none of these bump into each other) ----------

    /// <summary>A rectangle that is "box" moved to a world's spot.</summary>
    public static Rect2 At(int world, Rect2 box) => new(Spots[world] + box.Position, box.Size);

    /// <summary>Where the GO bubble is when little Bolt-E stands on this world.</summary>
    public static Rect2 BubbleRect(int world) => At(world, BubbleBoxes[world]);

    /// <summary>Where the NEXT arrow is over this world.</summary>
    public static Rect2 NextRect(int world) => At(world, NextBoxes[world]);

    /// <summary>The NEXT arrow over this world with its bounce (it bounces up to 8 pixels toward the world).</summary>
    public static Rect2 NextArrowRect(int world)
    {
        var box = NextRect(world);
        return box.Merge(new Rect2(box.Position + NextPoints[world] * 8, box.Size));
    }

    /// <summary>The box little Bolt-E covers, standing on this world (his party hat and his hover included).</summary>
    public static Rect2 HeroBox(int world) => new(Spots[world] + new Vector2(-32, -78), new Vector2(64, 86));

    /// <summary>The gold flag of a CLEARED world: its pole, its gold ball on top, and the waving flag.</summary>
    public static Rect2 FlagRect(int world) => FlagSides[world] > 0
        ? new Rect2(Spots[world] + new Vector2(FlagX - 3, -50), new Vector2(32, 58))
        : new Rect2(Spots[world] + new Vector2(-FlagX - 29, -50), new Vector2(32, 58));

    /// <summary>The next boss's badge beside a world (its hop, and a party hat poking out of the top, included).</summary>
    public static Rect2 TeaserRect(int world) => new(Spots[world] + TeaserOffsets[world] - new Vector2(28, 40), new Vector2(56, 68));

    /// <summary>The middle of the "NEW!" words over a world (after its cloud puffed away).</summary>
    public static Vector2 NewWordsAt(int world) => Spots[world] + NewOffsets[world];

    /// <summary>The rocket waiting on its pad.</summary>
    public static readonly Rect2 ParkedRocket = new(1086, 333, 38, 63);

    /// <summary>
    /// The area big words really cover with ink, from the top of the capital letters to the bottom of the outline. (Big
    /// words have no letters hanging under the line, like a "g" or a "p": only "The whole map is GOLD now!" does, so
    /// words with small letters reach a bit lower.) The self-test uses this to check the words don't cover a sign.
    /// </summary>
    public static Rect2 InkedWords(string text, float x, float baseline, int size, int outline)
    {
        var font = ThemeDB.FallbackFont;
        float width = font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        float below = text.Any(char.IsLower) ? font.GetDescent(size) : 1;
        return new Rect2(x - width / 2 - outline / 2f, baseline - size * 0.74f - outline / 2f, width + outline, size * 0.74f + below + outline);
    }

    static float DistanceNearest(int road, Vector2 spot)
    {
        var samples = roadSamples[road];
        int best = 0;
        for (int i = 1; i < samples.Length; i++)
            if (samples[i].DistanceSquaredTo(spot) < samples[best].DistanceSquaredTo(spot)) best = i;
        return roadDistances[road][best];
    }

    /// <summary>A smooth closed curve through the points ("perPart" points between each two of them).</summary>
    static Vector2[] ClosedCurve(Vector2[] points, int perPart)
    {
        int n = points.Length;
        var curve = new Vector2[n * perPart];
        for (int i = 0; i < n; i++)
            for (int s = 0; s < perPart; s++)
                curve[i * perPart + s] = CatmullRom(points[(i - 1 + n) % n], points[i], points[(i + 1) % n], points[(i + 2) % n], s / (float)perPart);
        return curve;
    }

    /// <summary>A smooth curve from the first point to the last one, through all of them.</summary>
    static Vector2[] OpenCurve(Vector2[] points, int perPart)
    {
        int n = points.Length;
        var curve = new Vector2[(n - 1) * perPart + 1];
        for (int i = 0; i < n - 1; i++)
            for (int s = 0; s < perPart; s++)
                curve[i * perPart + s] = CatmullRom(points[Math.Max(0, i - 1)], points[i], points[i + 1], points[Math.Min(n - 1, i + 2)], s / (float)perPart);
        curve[^1] = points[^1];
        return curve;
    }

    /// <summary>The Catmull-Rom curve between p1 and p2 (p0 and p3 bend it), at t from 0 (p1) to 1 (p2).</summary>
    static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (3 * p1 - p0 - 3 * p2 + p3) * t3);
    }

    /// <summary>The points of a loop, with the first one again at the end (so a line drawn through them goes all the way round).</summary>
    static Vector2[] Loop(Vector2[] points)
    {
        var loop = new Vector2[points.Length + 1];
        Array.Copy(points, loop, points.Length);
        loop[^1] = points[0];
        return loop;
    }

    /// <summary>An oval as a loop of points (for drawing a ring around it).</summary>
    static Vector2[] OvalLoop(Vector2 middle, Vector2 radius, int count)
    {
        var loop = new Vector2[count + 1];
        for (int i = 0; i <= count; i++)
        {
            float angle = i * Mathf.Tau / count;
            loop[i] = middle + new Vector2(Mathf.Cos(angle) * radius.X, Mathf.Sin(angle) * radius.Y);
        }
        return loop;
    }

    /// <summary>The top half of a circle (the flat side down, at y 0): a bobble hat or a helmet.</summary>
    static Vector2[] HalfDisc(float radius, int count)
    {
        var points = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.Pi + i * Mathf.Pi / (count - 1); // from the left, over the top, to the right
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return points;
    }

    // ---------- Little drawing helpers (they never make new lists, so they're fine every frame) ----------

    static readonly Vector2[] scratch3 = new Vector2[3];
    static readonly Vector2[] scratch4 = new Vector2[4];
    static readonly Vector2[] scratch7 = new Vector2[7];
    static readonly Vector2[] sparkle8 = new Vector2[8];
    static readonly Vector2[][] scratches = new Vector2[48][];

    /// <summary>A list of "count" points to draw with, made the first time it's needed and used again after that.</summary>
    static Vector2[] Scratch(int count) => scratches[count] ??= new Vector2[count];

    /// <summary>Words with their middle at x, standing on "baseline", outlined in ink. Returns the area they cover.</summary>
    public static Rect2 Words(CanvasItem canvas, string text, float x, float baseline, int size, Color color, int outline)
    {
        var font = ThemeDB.FallbackFont;
        var textSize = font.GetStringSize(text, HorizontalAlignment.Left, -1, size);
        var at = new Vector2(x - textSize.X / 2, baseline);
        if (outline > 0) canvas.DrawStringOutline(font, at, text, HorizontalAlignment.Left, -1, size, outline, Ink);
        canvas.DrawString(font, at, text, HorizontalAlignment.Left, -1, size, color);
        return new Rect2(at.X - outline / 2f, baseline - font.GetAscent(size) - outline / 2f, textSize.X + outline, textSize.Y + outline);
    }

    /// <summary>The biggest letter size, from "size" down to "smallest", that fits the words into "width" pixels.</summary>
    public static int FitSize(string text, int size, int smallest, float width)
    {
        var font = ThemeDB.FallbackFont;
        while (size > smallest && font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X > width) size--;
        return size;
    }

    /// <summary>A filled triangle.</summary>
    public static void Triangle(CanvasItem canvas, Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        scratch3[0] = a;
        scratch3[1] = b;
        scratch3[2] = c;
        canvas.DrawColoredPolygon(scratch3, color);
    }

    /// <summary>A filled four-cornered shape.</summary>
    public static void Quad(CanvasItem canvas, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
    {
        scratch4[0] = a;
        scratch4[1] = b;
        scratch4[2] = c;
        scratch4[3] = d;
        canvas.DrawColoredPolygon(scratch4, color);
    }

    /// <summary>A filled shape with a dark outline: "shape" moved to "at" and made "size" times bigger.</summary>
    static void Outlined(CanvasItem canvas, Vector2[] shape, Vector2 at, float size, Color fill, float outline = 2.5f)
    {
        var points = Scratch(shape.Length);
        var loop = Scratch(shape.Length + 1);
        for (int i = 0; i < shape.Length; i++) points[i] = loop[i] = at + shape[i] * size;
        loop[^1] = points[0];
        canvas.DrawColoredPolygon(points, fill);
        canvas.DrawPolyline(loop, Ink, outline);
    }

    /// <summary>A fat arrow (7 corners) pointing "way" with its tip at "tip", "size" long (a dark outline first, then the color).</summary>
    public static void Arrow(CanvasItem canvas, Vector2 tip, Vector2 way, float size, Color color)
    {
        var side = new Vector2(-way.Y, way.X);
        for (int pass = 0; pass < 2; pass++)
        {
            float grow = pass == 0 ? 3 : 0; // (the first pass is the outline, a little bigger)
            float s = size + grow * 2;
            var at = tip + way * grow;
            scratch7[0] = at;
            scratch7[1] = at - way * s * 0.5f + side * s * 0.45f;
            scratch7[2] = at - way * s * 0.5f + side * s * 0.18f;
            scratch7[3] = at - way * s + side * s * 0.18f;
            scratch7[4] = at - way * s - side * s * 0.18f;
            scratch7[5] = at - way * s * 0.5f - side * s * 0.18f;
            scratch7[6] = at - way * s * 0.5f - side * s * 0.45f;
            canvas.DrawColoredPolygon(scratch7, pass == 0 ? Ink : color);
        }
    }

    /// <summary>A twinkle: a thin 4-pointed star.</summary>
    public static void Sparkle(CanvasItem canvas, Vector2 at, float size, Color color)
    {
        size = Mathf.Max(1.5f, size);
        for (int i = 0; i < 8; i++)
            sparkle8[i] = at + Vector2.Up.Rotated(i * Mathf.Pi / 4) * (i % 2 == 0 ? size : size * 0.3f);
        canvas.DrawColoredPolygon(sparkle8, color);
    }

    /// <summary>A rectangle turned "turn" around its middle (the middle is "at"). Returns the box around it.</summary>
    static Rect2 TurnedBox(Vector2 at, float turn, Rect2 local)
    {
        var box = new Rect2(at + local.Position.Rotated(turn), Vector2.Zero);
        box = box.Expand(at + new Vector2(local.End.X, local.Position.Y).Rotated(turn));
        box = box.Expand(at + local.End.Rotated(turn));
        return box.Expand(at + new Vector2(local.Position.X, local.End.Y).Rotated(turn));
    }

    // ---------- The background: the sea, the islands and the bushes ----------

    static readonly Vector2[] seaCorners = { new(0, 230), new(1280, 230), new(1280, 720), new(0, 720) };
    static readonly Color[] seaColors = { Sea, Sea, DeepSea, DeepSea };

    /// <summary>The sea: a little deeper (darker) toward the bottom of the screen.</summary>
    public static void SeaWater(CanvasItem canvas)
    {
        canvas.DrawRect(new Rect2(0, 0, 1280, 720), Sea);
        canvas.DrawPolygon(seaCorners, seaColors);
    }

    /// <summary>An island: shallow water around it, a sandy beach with a dark edge, and the land inside.</summary>
    public static void Island(CanvasItem canvas, Vector2[] coast, Vector2[] loop, Color land, Color landEdge, Color sand, Color sandEdge)
    {
        canvas.DrawPolyline(loop, ShallowWater, 26);
        canvas.DrawPolyline(loop, sandEdge, 17);
        canvas.DrawColoredPolygon(coast, land);
        canvas.DrawPolyline(loop, landEdge, 15);
        canvas.DrawPolyline(loop, sand, 12);
    }

    // Round bushes on the big island (in the spots where no road, world or sign is)
    static readonly Vector2[] BushSpots =
    {
        new(430, 432), new(603, 432), new(644, 456), new(668, 585), new(712, 580), new(884, 522), new(914, 542), new(1160, 416),
    };
    static readonly Color BushGreen = new(0.36f, 0.72f, 0.34f), BushDark = new(0.2f, 0.47f, 0.24f), BushLight = new(0.58f, 0.88f, 0.48f);

    /// <summary>Round bushes here and there on the big island (like Super Mario World's little trees).</summary>
    public static void Bushes(CanvasItem canvas)
    {
        for (int pass = 0; pass < 3; pass++)
            for (int i = 0; i < BushSpots.Length; i++)
            {
                var at = BushSpots[i];
                for (int b = -1; b <= 1; b++)
                {
                    var ball = at + new Vector2(b * 8, b == 0 ? -4 : 0);
                    float r = b == 0 ? 9 : 7;
                    if (pass == 0) canvas.DrawCircle(ball, r + 2, BushDark);
                    else if (pass == 1) canvas.DrawCircle(ball, r, BushGreen);
                    else if (b == 0) canvas.DrawCircle(ball + new Vector2(-3, -3), 2.5f, BushLight);
                }
            }
    }

    /// <summary>A shooting star across space, every 7 seconds (the first one 3 seconds after the map opens).</summary>
    public static void ShootingStar(CanvasItem canvas, float time)
    {
        float t = time + 4;
        int k = (int)(t / 7f);
        float p = (t - k * 7f) / 0.6f;
        if (p > 1) return;
        var head = new Vector2(200 + (k * 331) % 800, 18) + new Vector2(-260, 90) * p;
        var way = new Vector2(-0.945f, 0.327f); // (the way it flies)
        float fade = Mathf.Sin(p * Mathf.Pi);
        canvas.DrawLine(head - way * 70, head, new Color(1, 1, 1, 0.25f * fade), 1.5f);
        canvas.DrawLine(head - way * 36, head, new Color(1, 1, 1, 0.6f * fade), 2.5f);
        canvas.DrawCircle(head, 2.5f, new Color(1, 1, 0.9f, fade));
    }

    // The sailboat: its hull and sail (it's turned to face the way it sails)
    static readonly Vector2[] BoatHull = { new(-15, 0), new(15, 0), new(10, 8), new(-10, 8) };
    static readonly Vector2[] BoatSail = { new(1, -24), new(1, -3), new(15, -3) };
    static readonly Vector2[] BoatJib = { new(-1, -20), new(-1, -3), new(-11, -3) };

    /// <summary>A little sailboat that sails round and round in the sea north of the big island (once every 40 seconds).</summary>
    public static void Sailboat(CanvasItem canvas, float time)
    {
        float a = time * Mathf.Tau / 40f + 1.2f;
        var at = new Vector2(720 + Mathf.Cos(a) * 150, 330 + Mathf.Sin(a) * 14);
        float facing = Mathf.Sin(a) <= 0 ? 1 : -1; // (going right on the far side, left on the near side)
        canvas.DrawSetTransform(at, Mathf.Sin(time * 2.2f) * 0.06f, new Vector2(facing, 1));
        canvas.DrawLine(new Vector2(-12, 9), new Vector2(12, 9), new Color(1, 1, 1, 0.6f), 2);   // a little foam
        Outlined(canvas, BoatHull, Vector2.Zero, 1, new Color(0.65f, 0.38f, 0.2f), 2);
        canvas.DrawLine(new Vector2(0, 0), new Vector2(0, -27), Ink, 2);                          // the mast
        Outlined(canvas, BoatSail, Vector2.Zero, 1, Colors.White, 2);
        Outlined(canvas, BoatJib, Vector2.Zero, 1, new Color(0.9f, 0.92f, 0.98f), 2);
        Triangle(canvas, new Vector2(0, -27), new Vector2(7, -29), new Vector2(0, -31), OpenRed); // a tiny red flag
        canvas.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>A fish jumps out of the sea every 6 seconds (taking turns in two spots), with a splash going in and out.</summary>
    public static void Fish(CanvasItem canvas, float time)
    {
        int jump = (int)(time / 6f);
        float p = (time - jump * 6f) / 0.8f; // 0 to 1 while it jumps
        if (p > 1.4f) return;
        var spot = jump % 2 == 0 ? new Vector2(1010, 612) : new Vector2(560, 352);
        // A splash ring where it jumps out, and where it goes back in
        for (int end = 0; end < 2; end++)
        {
            float age = end == 0 ? p : p - 1;
            if (age < 0 || age > 0.4f) continue;
            var ring = spot + new Vector2(end == 0 ? -20 : 20, 0);
            canvas.DrawSetTransform(ring, 0, new Vector2(1, 0.4f));
            canvas.DrawArc(Vector2.Zero, 4 + age * 30, 0, Mathf.Tau, 20, new Color(1, 1, 1, 1 - age / 0.4f), 2.5f);
            canvas.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        }
        if (p > 1) return;
        var at = spot + new Vector2(-20 + 40 * p, -30 * 4 * p * (1 - p));
        canvas.DrawSetTransform(at, (p - 0.5f) * 1.8f, Vector2.One); // (nose up, then nose down)
        Triangle(canvas, new Vector2(-5, 0), new Vector2(-12, -5), new Vector2(-12, 5), Ink);
        Triangle(canvas, new Vector2(-5.5f, 0), new Vector2(-10.5f, -3.5f), new Vector2(-10.5f, 3.5f), new Color(1f, 0.55f, 0.15f));
        Shapes.Ellipse(canvas, Vector2.Zero, new Vector2(8, 4.8f), Ink);
        Shapes.Ellipse(canvas, Vector2.Zero, new Vector2(6.8f, 3.6f), new Color(1f, 0.6f, 0.18f));
        canvas.DrawCircle(new Vector2(3.5f, -1), 1.2f, Ink);
        canvas.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    // ---------- Flags, clouds and the GOLD MAP's sparkles ----------

    /// <summary>
    /// A CLEARED world's gold flag on its pole, beside the world's dot (on the side where no road goes). "up" = how far up
    /// it has climbed (0 to 1). It waves.
    /// </summary>
    public static void Flag(CanvasItem canvas, int world, float up, float wave)
    {
        var spot = Spots[world];
        float side = FlagSides[world];
        float x = spot.X + side * FlagX, top = spot.Y - 46, bottom = spot.Y + 6;
        canvas.DrawLine(new Vector2(x, bottom), new Vector2(x, top), Ink, 3);
        canvas.DrawCircle(new Vector2(x, top - 1), 3.4f, Ink);
        canvas.DrawCircle(new Vector2(x, top - 1), 2.3f, Gold); // a gold ball on top
        float flagTop = Mathf.Lerp(bottom - 18, top + 3, Mathf.Clamp(up, 0, 1));
        var a = new Vector2(x + side * 1.5f, flagTop);
        Quad(canvas, a + new Vector2(0, -1.5f), a + new Vector2(side * 25, wave - 1.5f), a + new Vector2(side * 25, 17.5f + wave), a + new Vector2(0, 17.5f), Ink);
        Quad(canvas, a, a + new Vector2(side * 23.5f, wave), a + new Vector2(side * 23.5f, 16 + wave), a + new Vector2(0, 16), Gold);
        Shapes.Star(canvas, a + new Vector2(side * 11.5f, 8 + wave * 0.5f), 5, 0, Colors.White);
    }

    /// <summary>Where the top of a world's gold flag is (the confetti and the sparkles come out of it).</summary>
    public static Vector2 FlagTop(int world) => Spots[world] + new Vector2(FlagSides[world] * (FlagX + 12), -40);

    /// <summary>Where the circles of a cloud go in its box (x and y from 0 to 1 across and down, and z = how big).</summary>
    static readonly Vector3[] CloudBits =
    {
        new(0.14f, 0.64f, 0.7f), new(0.3f, 0.48f, 1f), new(0.52f, 0.36f, 1.1f), new(0.72f, 0.48f, 1f), new(0.87f, 0.64f, 0.7f),
        new(0.38f, 0.72f, 0.95f), new(0.64f, 0.72f, 0.95f),
    };
    static readonly Vector2[] NebulaTwinkles = { new(0.25f, 0.35f), new(0.7f, 0.25f), new(0.82f, 0.7f), new(0.3f, 0.8f) };

    /// <summary>
    /// A fluffy cloud over a LOCKED world's picture (or a purple nebula in space), with a giant "?". "puff" goes from 0 to 1
    /// while it puffs away: the circles fly apart and fade. It sways a little. Returns the area it covers.
    /// </summary>
    public static Rect2 Cloud(CanvasItem canvas, int world, float puff, float time, List<string> texts)
    {
        var box = LandmarkBoxes[world];
        bool space = world == 4; // (THE MOON is in space: a nebula instead of a cloud)
        float radius = Mathf.Max(24, Mathf.Min(box.Size.Y, box.Size.X * 0.6f) * 0.3f);
        float alpha = 1 - Mathf.Clamp(puff, 0, 1);
        var area = box;
        var edge = new Color(space ? NebulaEdge : CloudEdge, alpha);
        var shade = new Color(space ? NebulaShade : CloudShade, alpha);
        var fill = new Color(space ? NebulaPurple : CloudWhite, alpha);
        for (int pass = 0; pass < 3; pass++) // the outline, then the shady bottom, then the cloud itself
            for (int i = 0; i < CloudBits.Length; i++)
            {
                var bit = CloudBits[i];
                var middle = box.Position + new Vector2(bit.X, bit.Y) * box.Size + new Vector2(Mathf.Sin(time * 1.3f + i) * 2.5f, 0);
                var away = (middle - box.GetCenter()).Normalized() * puff * 70;
                float r = Mathf.Max(0.5f, radius * bit.Z * (1 - 0.3f * puff));
                var at = middle + away;
                if (pass == 0)
                {
                    canvas.DrawCircle(at + new Vector2(0, 2), r + 3, edge);
                    area = area.Merge(new Rect2(at - new Vector2(r + 3, r + 1), new Vector2(r * 2 + 6, r * 2 + 6)));
                }
                else if (pass == 1) canvas.DrawCircle(at + new Vector2(0, 5), r, shade);
                else canvas.DrawCircle(at, r, fill);
            }
        if (puff > 0) return area;
        if (space)
            for (int i = 0; i < NebulaTwinkles.Length; i++)
                Sparkle(canvas, box.Position + NebulaTwinkles[i] * box.Size, 3 + 1.5f * Mathf.Sin(time * 3 + i * 1.7f), new Color(1, 1, 1, 0.85f));
        var spot = QuestionSpots[world];
        area = area.Merge(Words(canvas, "?", spot.X, spot.Y + 17 + Mathf.Sin(time * 2.5f) * 3, 48, Colors.White, 9));
        texts.Add("?");
        return area;
    }

    /// <summary>The GOLD MAP: a ring of gold sparkles slowly going round a world's dot.</summary>
    public static Rect2 GoldRing(CanvasItem canvas, int world, float time)
    {
        var spot = Spots[world];
        for (int i = 0; i < 5; i++)
        {
            float angle = time * 0.8f + i * Mathf.Tau / 5 + world;
            var at = spot + new Vector2(Mathf.Cos(angle) * 37, Mathf.Sin(angle) * 31 - 4);
            Sparkle(canvas, at, 4.5f + 2 * Mathf.Sin(time * 4 + i + world), GoldLight);
        }
        return new Rect2(spot - new Vector2(44, 42), new Vector2(88, 80));
    }

    // ---------- Each world's little picture ----------

    /// <summary>
    /// Each world's little picture on the map: SUNNY HILLS' hills with eyes and Bolt-E's garage, CANDY LAND's frosting and
    /// lollipops, NIGHT CITY under its own night sky, SNOWY PEAKS with the ROCKET PAD, THE MOON, UPSIDE-DOWN STATION and
    /// VOLCANO ISLAND. "time" makes things blink, puff, fall, glow and swap. "rocketParked" = the rocket waits on its pad
    /// (it's off flying little Bolt-E to THE MOON).
    /// </summary>
    public static void Landmark(CanvasItem canvas, int world, float time, bool rocketParked)
    {
        switch (world)
        {
            case 0: SunnyHills(canvas, time); break;
            case 1: CandyLand(canvas, time); break;
            case 2: NightCity(canvas, time); break;
            case 3: SnowyPeaks(canvas, time, rocketParked); break;
            case 4: TheMoon(canvas, time); break;
            case 5: Station(canvas, time); break;
            default: Volcano(canvas, time); break;
        }
    }

    // ---- SUNNY HILLS: three hills with eyes (like Super Mario World!), flowers, and Bolt-E's garage ----
    static readonly Rect2[] Hills = { new(238, 446, 40, 54), new(280, 422, 48, 72), new(334, 456, 36, 46) };
    static readonly Vector2[] Flowers = { new(262, 522), new(340, 520), new(366, 530), new(276, 574), new(252, 546), new(328, 585) };
    static readonly Color HillGreen = new(0.52f, 0.87f, 0.44f), HillDark = new(0.17f, 0.45f, 0.23f), HillLight = new(0.72f, 0.96f, 0.6f);
    static readonly Vector2[] BoltSign = { new(1, -6), new(-3, 1), new(0, 1), new(-1, 6), new(3, -1), new(0, -1) };

    static void SunnyHills(CanvasItem canvas, float time)
    {
        for (int i = 0; i < Hills.Length; i++)
        {
            var hill = Hills[i];
            Hill(canvas, hill.Grow(3), HillDark); // the dark outline
            Hill(canvas, hill, HillGreen);
            Hill(canvas, new Rect2(hill.Position + new Vector2(6, 8), new Vector2(7, hill.Size.Y * 0.42f)), HillLight); // a shine
            // Two eyes with a white glint. They blink every 3.3 seconds (each hill 1.1 seconds after the one before)
            bool blink = Mathf.PosMod(time - i * 1.1f, 3.3f) < 0.12f;
            for (int side = -1; side <= 1; side += 2)
            {
                var eye = new Vector2(hill.GetCenter().X + side * 8, hill.Position.Y + 22);
                if (blink)
                {
                    canvas.DrawRect(new Rect2(eye.X - 3, eye.Y, 6, 1.6f), Colors.Black);
                    continue;
                }
                Shapes.Ellipse(canvas, eye, new Vector2(2.7f, 5.4f), Colors.Black);
                canvas.DrawCircle(eye + new Vector2(-0.7f, -2.3f), 1.3f, Colors.White);
            }
        }
        for (int i = 0; i < Flowers.Length; i++) Flower(canvas, Flowers[i], i);
        Garage(canvas, time);
    }

    /// <summary>A hill shape: round on top, flat at the bottom.</summary>
    static void Hill(CanvasItem canvas, Rect2 r, Color color)
    {
        float radius = r.Size.X / 2;
        canvas.DrawCircle(new Vector2(r.GetCenter().X, r.Position.Y + radius), radius, color);
        canvas.DrawRect(new Rect2(r.Position.X, r.Position.Y + radius, r.Size.X, r.Size.Y - radius), color);
    }

    static void Flower(CanvasItem canvas, Vector2 at, int which)
    {
        var petals = which % 2 == 0 ? Colors.White : new Color(1f, 0.6f, 0.75f);
        for (int i = 0; i < 4; i++) canvas.DrawCircle(at + Vector2.Right.Rotated(i * Mathf.Pi / 2) * 2.7f, 2.4f, petals);
        canvas.DrawCircle(at, 1.9f, new Color(1f, 0.82f, 0.2f));
    }

    /// <summary>Bolt-E's garage (his house!), with a lightning bolt on it, and smoke puffing out of the chimney every 2.4 seconds.</summary>
    static void Garage(CanvasItem canvas, float time)
    {
        for (int k = 0; k < 2; k++)
        {
            float p = Mathf.PosMod(time + k * 1.2f, 2.4f) / 2.4f;
            canvas.DrawCircle(new Vector2(408 + p * 8, 476 - p * 22), 3 + 4 * p, new Color(0.95f, 0.95f, 0.98f, 0.85f * (1 - p)));
        }
        canvas.DrawRect(new Rect2(403, 474, 8, 14), Ink);                                           // the chimney
        canvas.DrawRect(new Rect2(404.5f, 475.5f, 5, 11), new Color(0.78f, 0.38f, 0.32f));
        canvas.DrawRect(new Rect2(378, 492, 34, 27), Ink);                                          // the house
        canvas.DrawRect(new Rect2(380, 494, 30, 23), new Color(0.66f, 0.72f, 0.9f));
        Triangle(canvas, new Vector2(371, 496), new Vector2(395, 472), new Vector2(419, 496), Ink); // the roof
        Triangle(canvas, new Vector2(376, 493), new Vector2(395, 476), new Vector2(414, 493), new Color(0.93f, 0.3f, 0.24f));
        canvas.DrawRect(new Rect2(388, 503, 14, 14), Ink);                                          // the garage door
        canvas.DrawRect(new Rect2(389.5f, 504.5f, 11, 12.5f), new Color(0.4f, 0.45f, 0.62f));
        for (int i = 0; i < 3; i++) canvas.DrawLine(new Vector2(390, 508 + i * 3.5f), new Vector2(400, 508 + i * 3.5f), Ink, 1);
        var sign = Scratch(BoltSign.Length);                                                         // a lightning bolt: Bolt-E lives here!
        for (int i = 0; i < sign.Length; i++) sign[i] = new Vector2(395, 486) + BoltSign[i];
        canvas.DrawColoredPolygon(sign, Gold);
    }

    // ---- CANDY LAND: a pink frosting patch, lollipops, gumdrops and sprinkles ----
    static readonly Color Frosting = new(1f, 0.66f, 0.83f), FrostingEdge = new(0.92f, 0.42f, 0.64f);
    static readonly Vector2[] Sprinkles =
    {
        new(432, 528), new(450, 552), new(470, 515), new(484, 545), new(503, 505), new(522, 580), new(537, 512), new(548, 540),
        new(566, 562), new(588, 530), new(600, 556), new(578, 580), new(458, 572), new(612, 538),
    };

    static void CandyLand(CanvasItem canvas, float time)
    {
        var middle = new Vector2(515, 536);
        Shapes.Ellipse(canvas, middle + new Vector2(0, 4), new Vector2(99, 45), FrostingEdge); // the thick edge of the frosting
        Shapes.Ellipse(canvas, middle, new Vector2(95, 41), Frosting);
        for (int i = 0; i < 6; i++) // frosting drips over the front edge
        {
            float angle = Mathf.Pi * (0.2f + i * 0.12f);
            canvas.DrawCircle(middle + new Vector2(Mathf.Cos(angle) * 88, Mathf.Sin(angle) * 38 + 2), 5 + (i % 3), Frosting);
        }
        for (int i = 0; i < Sprinkles.Length; i++) // rainbow sprinkles
        {
            var tilt = Vector2.Right.Rotated(i * 1.3f) * 3.2f;
            canvas.DrawLine(Sprinkles[i] - tilt, Sprinkles[i] + tilt, i % 7 == 3 ? Colors.White : Rainbow[i % 6], 2.6f);
        }
        Gumdrop(canvas, new Vector2(470, 574), new Color(0.4f, 0.85f, 0.4f));
        Gumdrop(canvas, new Vector2(536, 579), new Color(1f, 0.6f, 0.2f));
        Gumdrop(canvas, new Vector2(432, 548), new Color(0.7f, 0.45f, 1f));
        Lollipop(canvas, new Vector2(446, 494), new Color(1f, 0.42f, 0.7f), time);
        Lollipop(canvas, new Vector2(582, 500), new Color(1f, 0.85f, 0.25f), time + 1);
        Lollipop(canvas, new Vector2(630, 556), new Color(0.45f, 0.7f, 1f), time + 2);
    }

    /// <summary>A lollipop: a white stick, a round candy, and a white swirl that turns slowly.</summary>
    static void Lollipop(CanvasItem canvas, Vector2 at, Color candy, float time)
    {
        canvas.DrawRect(new Rect2(at.X - 2.5f, at.Y, 5, 20), Ink);
        canvas.DrawRect(new Rect2(at.X - 1.5f, at.Y, 3, 19), Colors.White);
        canvas.DrawCircle(at, 13, Ink);
        canvas.DrawCircle(at, 11.2f, candy);
        canvas.DrawArc(at, 7, time * 1.5f, time * 1.5f + Mathf.Pi * 1.3f, 12, Colors.White, 2.6f);
        canvas.DrawArc(at, 3.2f, time * 1.5f + Mathf.Pi, time * 1.5f + Mathf.Pi * 2.2f, 8, Colors.White, 2);
    }

    /// <summary>A gumdrop: a sugary dome.</summary>
    static void Gumdrop(CanvasItem canvas, Vector2 at, Color color)
    {
        canvas.DrawCircle(at, 9, Ink);
        canvas.DrawRect(new Rect2(at.X - 9, at.Y, 18, 6), Ink);
        canvas.DrawCircle(at, 7.3f, color);
        canvas.DrawRect(new Rect2(at.X - 7.3f, at.Y, 14.6f, 4.3f), color);
        canvas.DrawCircle(at + new Vector2(-3, -3), 1.8f, new Color(1, 1, 1, 0.85f));
    }

    // ---- NIGHT CITY: skyscrapers with blinking windows, under their own little night sky ----
    static readonly Rect2[] Towers = { new(708, 466, 16, 34), new(726, 446, 20, 54), new(750, 460, 18, 40), new(790, 440, 22, 60), new(816, 456, 16, 44), new(836, 470, 20, 30) };
    static readonly Vector2[] NightStars = { new(712, 450), new(735, 424), new(763, 432), new(800, 418), new(857, 458), new(820, 435), new(690, 478), new(868, 486) };
    static readonly Color NightSky = new(0.14f, 0.13f, 0.32f), NightRim = new(0.32f, 0.3f, 0.6f), TowerColor = new(0.32f, 0.3f, 0.55f), TowerEdge = new(0.45f, 0.43f, 0.72f);
    static readonly Color WindowYellow = new(1f, 0.9f, 0.45f), WindowDark = new(0.2f, 0.19f, 0.38f);

    static void NightCity(CanvasItem canvas, float time)
    {
        var middle = new Vector2(780, 470);
        Shapes.Ellipse(canvas, middle, new Vector2(103, 65), NightRim);
        Shapes.Ellipse(canvas, middle, new Vector2(100, 62), NightSky);
        for (int i = 0; i < NightStars.Length; i++)
            canvas.DrawCircle(NightStars[i], 1.4f + (i % 2) * 0.5f, new Color(1, 1, 0.85f, 0.55f + 0.45f * Mathf.Sin(time * 2.2f + i * 1.9f)));
        canvas.DrawCircle(new Vector2(835, 438), 9, new Color(1f, 0.95f, 0.6f)); // a crescent moon
        canvas.DrawCircle(new Vector2(840, 434), 8, NightSky);
        canvas.DrawRect(new Rect2(702, 499, 156, 8), new Color(0.24f, 0.22f, 0.44f)); // the street
        for (int b = 0; b < Towers.Length; b++)
        {
            var tower = Towers[b];
            canvas.DrawRect(tower.Grow(1.5f), Ink);
            canvas.DrawRect(tower, TowerColor);
            canvas.DrawRect(new Rect2(tower.Position, new Vector2(3, tower.Size.Y)), TowerEdge); // the side the moon lights up
            int columns = (int)((tower.Size.X - 3) / 7), rows = (int)((tower.Size.Y - 6) / 9);
            float left = tower.GetCenter().X - (columns * 7 - 4) / 2f + 1;
            for (int row = 0; row < rows; row++)
                for (int column = 0; column < columns; column++)
                {
                    bool on = (b * 7 + row * 3 + column * 5) % 4 != 0;                            // most windows are lit...
                    bool blinks = (b == 1 && row == 2 && column == 0) || (b == 3 && row == 4 && column == 1) || (b == 4 && row == 1 && column == 0);
                    if (blinks) on = Mathf.PosMod(time + b * 0.5f, 1.5f) < 0.75f;                 // ...and 3 switch on and off
                    canvas.DrawRect(new Rect2(left + column * 7, tower.Position.Y + 5 + row * 9, 3, 4), on ? WindowYellow : WindowDark);
                }
        }
        canvas.DrawLine(new Vector2(801, 440), new Vector2(801, 427), Ink, 2); // an antenna with a blinking red light
        canvas.DrawCircle(new Vector2(801, 426), 2.6f, Mathf.PosMod(time, 1.2f) < 0.6f ? new Color(1f, 0.25f, 0.2f) : new Color(0.45f, 0.12f, 0.12f));
    }

    // ---- SNOWY PEAKS: two snowy mountains, pine trees, falling snow, and the ROCKET PAD ----
    static readonly Vector2[] SnowCapOne = SnowCap(new Vector2(950, 382), 46, 448);
    static readonly Vector2[] SnowCapTwo = SnowCap(new Vector2(1010, 352), 58, 448);
    static readonly Vector2[] Pines = { new(905, 428), new(958, 500), new(1040, 470) };
    static readonly Color PineGreen = new(0.16f, 0.5f, 0.33f), PadGrey = new(0.6f, 0.62f, 0.68f);

    /// <summary>A mountain's snowy top: the top 32% of it, with a zig-zag bottom edge.</summary>
    static Vector2[] SnowCap(Vector2 peak, float halfWidth, float baseY)
    {
        float capY = peak.Y + (baseY - peak.Y) * 0.32f, half = halfWidth * 0.32f;
        return new[]
        {
            peak, new Vector2(peak.X + half, capY), new Vector2(peak.X + half * 0.45f, capY - 5), new Vector2(peak.X, capY + 3),
            new Vector2(peak.X - half * 0.45f, capY - 5), new Vector2(peak.X - half, capY),
        };
    }

    static void SnowyPeaks(CanvasItem canvas, float time, bool rocketParked)
    {
        Mountain(canvas, new Vector2(950, 382), 46, 448, new Color(0.6f, 0.66f, 0.84f), SnowCapOne);
        Mountain(canvas, new Vector2(1010, 352), 58, 448, new Color(0.68f, 0.74f, 0.92f), SnowCapTwo);
        for (int i = 0; i < Pines.Length; i++) Pine(canvas, Pines[i]);
        RocketPadAndRocket(canvas, rocketParked, time);
        for (int i = 0; i < 16; i++) // snow falling, wobbling, and starting again at the top
        {
            float x = 902 + (i * 53) % 196 + Mathf.Sin(time * 1.5f + i) * 4;
            float y = 322 + Mathf.PosMod(i * 37 + time * 22, 146);
            canvas.DrawCircle(new Vector2(x, y), 1.9f, new Color(1, 1, 1, 0.92f));
        }
    }

    static void Mountain(CanvasItem canvas, Vector2 peak, float halfWidth, float baseY, Color color, Vector2[] cap)
    {
        Triangle(canvas, new Vector2(peak.X - halfWidth - 3.5f, baseY + 1.5f), peak + new Vector2(0, -4), new Vector2(peak.X + halfWidth + 3.5f, baseY + 1.5f), Ink);
        Triangle(canvas, new Vector2(peak.X - halfWidth, baseY), peak, new Vector2(peak.X + halfWidth, baseY), color);
        Triangle(canvas, new Vector2(peak.X + 5, baseY), peak, new Vector2(peak.X + halfWidth, baseY), color.Darkened(0.14f)); // the shady side
        canvas.DrawColoredPolygon(cap, Colors.White);
    }

    /// <summary>A pine tree: three green layers on a little trunk, with snow on the tips.</summary>
    static void Pine(CanvasItem canvas, Vector2 foot)
    {
        canvas.DrawRect(new Rect2(foot.X - 2.5f, foot.Y - 5, 5, 6), new Color(0.45f, 0.3f, 0.2f));
        for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < 3; i++)
            {
                float y = foot.Y - 4 - i * 7, half = 10 - i * 2.3f, grow = pass == 0 ? 2 : 0;
                Triangle(canvas, new Vector2(foot.X - half - grow, y + grow * 0.6f), new Vector2(foot.X, y - 11 - grow),
                         new Vector2(foot.X + half + grow, y + grow * 0.6f), pass == 0 ? Ink : PineGreen);
            }
        Triangle(canvas, new Vector2(foot.X - 3, foot.Y - 29), new Vector2(foot.X, foot.Y - 33), new Vector2(foot.X + 3, foot.Y - 29), Colors.White);
    }

    /// <summary>The ROCKET PAD: a round pad with yellow-and-black stripes, a launch tower, and the rocket (when it's there).</summary>
    static void RocketPadAndRocket(CanvasItem canvas, bool rocketParked, float time)
    {
        canvas.DrawRect(new Rect2(1123, 349, 8, 47), Ink); // the launch tower
        canvas.DrawRect(new Rect2(1124.5f, 350.5f, 5, 44), new Color(0.78f, 0.8f, 0.86f));
        for (int i = 0; i < 3; i++) canvas.DrawLine(new Vector2(1114, 360 + i * 12), new Vector2(1125, 360 + i * 12), Ink, 2);
        Shapes.Ellipse(canvas, RocketPad + new Vector2(0, 1.5f), new Vector2(27, 8.5f), Ink);
        Shapes.Ellipse(canvas, RocketPad, new Vector2(24.5f, 6.2f), PadGrey);
        for (int i = 0; i < 8; i++) // yellow and black stripes along the front
        {
            float angle = Mathf.Pi * (0.08f + i * 0.12f);
            var at = RocketPad + new Vector2(Mathf.Cos(angle) * 20, Mathf.Sin(angle) * 4.2f);
            canvas.DrawRect(new Rect2(at.X - 2.2f, at.Y - 1.6f, 4.4f, 3.2f), i % 2 == 0 ? Gold : Ink);
        }
        if (rocketParked) Rocket(canvas, RocketPad + new Vector2(0, -2), 0, 0, false, time);
    }

    // ---- THE MOON: a big grey moon with craters, and twinkles beside it ----
    static readonly Vector2[] MoonTwinkles = { new(1036, 70), new(1137, 166), new(1138, 70) };

    static void TheMoon(CanvasItem canvas, float time)
    {
        canvas.DrawCircle(new Vector2(1085, 118), 60.5f, new Color(0.5f, 0.5f, 0.6f));
        canvas.DrawCircle(new Vector2(1085, 118), 58, new Color(0.8f, 0.8f, 0.85f));
        canvas.DrawCircle(new Vector2(1079, 112), 51, new Color(0.88f, 0.88f, 0.92f));
        Crater(canvas, new Vector2(1062, 98), 9);
        Crater(canvas, new Vector2(1110, 138), 12);
        Crater(canvas, new Vector2(1083, 155), 6);
        Crater(canvas, new Vector2(1112, 90), 5);
        Crater(canvas, new Vector2(1048, 135), 4);
        for (int i = 0; i < MoonTwinkles.Length; i++)
            Sparkle(canvas, MoonTwinkles[i], 3.5f + 2 * Mathf.Sin(time * 3 + i * 2.1f), new Color(1f, 1f, 0.85f, 0.9f));
    }

    static void Crater(CanvasItem canvas, Vector2 at, float r)
    {
        canvas.DrawCircle(at, r, new Color(0.66f, 0.66f, 0.73f));
        canvas.DrawArc(at, r - 1, Mathf.Pi * 1.1f, Mathf.Pi * 1.9f, 10, new Color(0.52f, 0.52f, 0.6f), 2);   // a shadow inside the top
        canvas.DrawArc(at, r, Mathf.Pi * 0.15f, Mathf.Pi * 0.85f, 10, new Color(1, 1, 1, 0.6f), 1.8f);       // a light rim at the bottom
    }

    // ---- UPSIDE-DOWN STATION: a ring with a hub, spokes and solar panels, and lights that swap colors ----
    static readonly Vector2[] StationRing = OvalLoop(new Vector2(640, 100), new Vector2(64, 22), 40);
    static readonly Color StationWhite = new(0.85f, 0.87f, 0.94f), StationGrey = new(0.6f, 0.63f, 0.74f);
    static readonly Color PanelBlue = new(0.17f, 0.32f, 0.72f), PanelLines = new(0.55f, 0.75f, 1f);

    static void Station(CanvasItem canvas, float time)
    {
        canvas.DrawLine(new Vector2(572, 100), new Vector2(708, 100), Ink, 5); // the struts that hold the solar panels
        SolarPanel(canvas, new Rect2(528, 92, 46, 16));
        SolarPanel(canvas, new Rect2(706, 92, 46, 16));
        for (int i = 0; i < 4; i++) // the spokes
        {
            float angle = Mathf.Pi / 4 + i * Mathf.Pi / 2;
            var rim = new Vector2(640 + Mathf.Cos(angle) * 64, 100 + Mathf.Sin(angle) * 22);
            canvas.DrawLine(new Vector2(640, 100), rim, Ink, 5);
            canvas.DrawLine(new Vector2(640, 100), rim, StationGrey, 2.5f);
        }
        canvas.DrawPolyline(StationRing, Ink, 11); // the ring
        canvas.DrawPolyline(StationRing, StationWhite, 6.5f);
        canvas.DrawCircle(new Vector2(640, 100), 16, Ink); // the hub
        canvas.DrawCircle(new Vector2(640, 100), 14, StationGrey);
        canvas.DrawCircle(new Vector2(636, 96), 5, StationWhite);
        bool swap = Mathf.PosMod(time, 1f) < 0.5f; // 4 lights, swapping pink and cyan every half second
        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.Pi / 2;
            var at = new Vector2(640 + Mathf.Cos(angle) * 64, 100 + Mathf.Sin(angle) * 22);
            canvas.DrawCircle(at, 5.5f, Ink);
            canvas.DrawCircle(at, 4, (i % 2 == 0) == swap ? NeonPink : Glow);
        }
    }

    static void SolarPanel(CanvasItem canvas, Rect2 panel)
    {
        canvas.DrawRect(panel.Grow(2), Ink);
        canvas.DrawRect(panel, PanelBlue);
        for (int i = 1; i < 4; i++) canvas.DrawLine(new Vector2(panel.Position.X + i * panel.Size.X / 4, panel.Position.Y), new Vector2(panel.Position.X + i * panel.Size.X / 4, panel.End.Y), PanelLines, 1);
        canvas.DrawLine(new Vector2(panel.Position.X, panel.GetCenter().Y), new Vector2(panel.End.X, panel.GetCenter().Y), PanelLines, 1);
    }

    // ---- VOLCANO ISLAND: the volcano, its glowing crater, lava, and smoke ----
    static readonly Vector2[] VolcanoCone = { new(62, 440), new(108, 332), new(142, 332), new(188, 440) };
    static readonly Vector2[] VolcanoConeEdge = { new(56, 443), new(106, 328), new(144, 328), new(194, 443) };
    static readonly Vector2[] VolcanoSunnySide = { new(62, 440), new(108, 332), new(117, 332), new(94, 440) };
    static readonly Vector2[] LavaOne = { new(118, 334), new(108, 360), new(112, 392) };
    static readonly Vector2[] LavaTwo = { new(132, 334), new(146, 368), new(140, 400) };
    static readonly Color LavaOrange = new(1f, 0.52f, 0.12f);

    static void Volcano(CanvasItem canvas, float time)
    {
        for (int k = 0; k < 4; k++) // four smoke puffs, rising into the clouds (one every 0.75 seconds)
        {
            float p = Mathf.PosMod(time + k * 0.75f, 3f) / 3f;
            var at = new Vector2(125, 322).Lerp(new Vector2(150, 248), p) + new Vector2(Mathf.Sin(time * 2 + k) * 3, 0);
            canvas.DrawCircle(at, 6 + 12 * p, new Color(0.35f, 0.3f, 0.32f, 0.7f * (1 - p)));
        }
        float glow = 0.5f + 0.5f * Mathf.Sin(time * Mathf.Tau / 1.6f); // the crater glows brighter and dimmer
        canvas.DrawCircle(new Vector2(125, 333), 22, new Color(1f, 0.45f, 0.1f, 0.25f + 0.2f * glow));
        canvas.DrawColoredPolygon(VolcanoConeEdge, Ink);
        canvas.DrawColoredPolygon(VolcanoCone, new Color(0.3f, 0.14f, 0.11f));
        canvas.DrawColoredPolygon(VolcanoSunnySide, new Color(0.42f, 0.22f, 0.17f));
        canvas.DrawPolyline(LavaOne, LavaOrange, 3.5f);
        canvas.DrawPolyline(LavaTwo, LavaOrange, 3.5f);
        Shapes.Ellipse(canvas, new Vector2(125, 333), new Vector2(18.5f, 5), Ink);
        Shapes.Ellipse(canvas, new Vector2(125, 333), new Vector2(16, 3.6f), new Color(1f, 0.55f + 0.25f * glow, 0.15f));
    }

    // ---------- The rides ----------

    static readonly Color RocketRed = new(0.92f, 0.25f, 0.22f), WindowBlue = new(0.35f, 0.65f, 1f), BoltEyes = new(0.4f, 0.95f, 1f);
    static readonly Color FlameOrange = new(1f, 0.55f, 0.15f), FlameYellow = new(1f, 0.92f, 0.4f);

    /// <summary>
    /// The rocket: on its pad, or flying little Bolt-E to THE MOON (you can see his eyes in its round window). "bottom" =
    /// the middle of its bottom, "turn" = how much it's turned (0 = straight up), "flame" = how big the flame is (0 = none).
    /// Returns the area it covers.
    /// </summary>
    public static Rect2 Rocket(CanvasItem canvas, Vector2 bottom, float turn, float flame, bool boltInside, float time)
    {
        canvas.DrawSetTransform(bottom, turn, Vector2.One);
        float length = 0;
        if (flame > 0)
        {
            length = (12 + 18 * flame) * (0.82f + 0.18f * Mathf.Sin(time * 47));
            Triangle(canvas, new Vector2(-7, -1), new Vector2(0, length), new Vector2(7, -1), FlameOrange);
            Triangle(canvas, new Vector2(-4, -1), new Vector2(0, length * 0.62f), new Vector2(4, -1), FlameYellow);
        }
        for (int side = -1; side <= 1; side += 2) // the fins
        {
            Triangle(canvas, new Vector2(side * 6, -22), new Vector2(side * 19.5f, 2), new Vector2(side * 6, -0.5f), Ink);
            Triangle(canvas, new Vector2(side * 7.5f, -17.5f), new Vector2(side * 16, 0), new Vector2(side * 7.5f, -2.5f), RocketRed);
        }
        Shapes.RoundRect(canvas, new Rect2(-10, -42, 20, 42), 7, Ink); // the body
        Shapes.RoundRect(canvas, new Rect2(-8, -40, 16, 38), 6, Colors.White);
        canvas.DrawRect(new Rect2(-8, -13, 16, 4), RocketRed);
        Triangle(canvas, new Vector2(-10.5f, -37), new Vector2(0, -58.5f), new Vector2(10.5f, -37), Ink); // the nose
        Triangle(canvas, new Vector2(-8, -38), new Vector2(0, -55), new Vector2(8, -38), RocketRed);
        canvas.DrawCircle(new Vector2(0, -26), 6.5f, Ink); // the round window
        canvas.DrawCircle(new Vector2(0, -26), 5, WindowBlue);
        if (boltInside)
        {
            canvas.DrawCircle(new Vector2(-1.9f, -26), 1.3f, BoltEyes);
            canvas.DrawCircle(new Vector2(1.9f, -26), 1.3f, BoltEyes);
        }
        else canvas.DrawCircle(new Vector2(-1.8f, -27.8f), 1.6f, new Color(1, 1, 1, 0.85f));
        canvas.DrawRect(new Rect2(-5, -2, 10, 3), Ink); // the nozzle
        canvas.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        return TurnedBox(bottom, turn, new Rect2(-20, -59, 40, 61 + length));
    }

    static readonly Color ChuteRed = new(0.93f, 0.25f, 0.22f), CapsuleSilver = new(0.9f, 0.92f, 0.97f), CapsuleShield = new(0.36f, 0.38f, 0.46f);

    /// <summary>
    /// The round space capsule (with little Bolt-E looking out of its window, if he's in it), under its red-and-white
    /// parachute ("chute": 0 = still folded, 1 = wide open). It swings gently under the parachute. "sink" (0 to 1) =
    /// how far it has sunk into the sea after he hopped out. Returns the area it covers.
    /// </summary>
    public static Rect2 Capsule(CanvasItem canvas, Vector2 middle, float chute, float sink, bool boltInside, float time)
    {
        float c = Mathf.Clamp(chute, 0, 1), size = CapsuleSize * (1 - Mathf.Clamp(sink, 0, 0.9f));
        canvas.DrawSetTransform(middle, Mathf.Sin(time * 3) * 0.12f * c, new Vector2(size, size));
        if (c > 0.1f)
        {
            var top = new Vector2(0, -16 - 22 * c); // the middle of the parachute's flat side
            float r = 26 * c;
            for (int i = 0; i < 4; i++) // the strings
                canvas.DrawLine(new Vector2(i < 2 ? -5 : 5, -11), top + new Vector2((-1 + i * 2 / 3f) * r, 0), Ink, 1.3f);
            for (int i = 0; i < 4; i++) // the canopy: 4 slices, red and white
                Slice(canvas, top, r, Mathf.Pi + i * Mathf.Pi / 4, Mathf.Pi + (i + 1) * Mathf.Pi / 4, i % 2 == 0 ? ChuteRed : Colors.White);
            canvas.DrawArc(top, r, Mathf.Pi, Mathf.Tau, 18, Ink, 2);
            canvas.DrawLine(top + new Vector2(-r, 0), top + new Vector2(r, 0), Ink, 2);
        }
        canvas.DrawCircle(Vector2.Zero, 15, Ink); // the capsule
        canvas.DrawCircle(Vector2.Zero, 13, CapsuleSilver);
        canvas.DrawArc(Vector2.Zero, 10.5f, Mathf.Pi * 0.18f, Mathf.Pi * 0.82f, 10, CapsuleShield, 5); // the heat shield underneath
        canvas.DrawCircle(new Vector2(0, -2.5f), 6.2f, Ink);
        canvas.DrawCircle(new Vector2(0, -2.5f), 4.8f, WindowBlue);
        if (boltInside)
        {
            canvas.DrawCircle(new Vector2(-1.8f, -2.5f), 1.25f, BoltEyes);
            canvas.DrawCircle(new Vector2(1.8f, -2.5f), 1.25f, BoltEyes);
        }
        canvas.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        return new Rect2(middle - new Vector2(30, 66) * CapsuleSize, new Vector2(60, 82) * CapsuleSize);
    }

    const float CapsuleSize = 1.25f; // (the capsule and its parachute, a bit bigger than they're drawn above)

    /// <summary>The empty capsule floating in the sea after SPLOOSH: the sea covers its bottom, with a ring of foam.</summary>
    public static void Afloat(CanvasItem canvas, Vector2 waterLine)
    {
        Shapes.Ellipse(canvas, waterLine + new Vector2(0, 7), new Vector2(23, 9), new Color(Sea, 0.85f));
        canvas.DrawSetTransform(waterLine, 0, new Vector2(1, 0.35f));
        canvas.DrawArc(Vector2.Zero, 22, 0, Mathf.Tau, 24, new Color(1, 1, 1, 0.85f), 3);
        canvas.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>One slice of a round parachute (a piece of pie), from angle "from" to angle "to".</summary>
    static void Slice(CanvasItem canvas, Vector2 middle, float r, float from, float to, Color color)
    {
        var points = Scratch(6);
        points[0] = middle;
        for (int i = 0; i < 5; i++) points[i + 1] = middle + Vector2.Right.Rotated(Mathf.Lerp(from, to, i / 4f)) * r;
        canvas.DrawColoredPolygon(points, color);
    }

    /// <summary>The glass bubble little Bolt-E floats in through space (it's drawn over him: he's inside it). Returns its area.</summary>
    public static Rect2 SpaceBubble(CanvasItem canvas, Vector2 middle, float time)
    {
        float r = 34 * (1 + 0.03f * Mathf.Sin(time * 6)); // (it wobbles a tiny bit)
        canvas.DrawCircle(middle, r, new Color(0.75f, 0.92f, 1f, 0.15f));
        canvas.DrawArc(middle, r, 0, Mathf.Tau, 40, new Color(1, 1, 1, 0.9f), 2);
        canvas.DrawArc(middle, r - 6, Mathf.Pi * 1.1f, Mathf.Pi * 1.45f, 10, new Color(1, 1, 1, 0.8f), 3); // a shine
        canvas.DrawCircle(middle + new Vector2(r * 0.42f, -r * 0.58f), 2.5f, new Color(1, 1, 1, 0.85f));
        return new Rect2(middle - new Vector2(r + 2, r + 2), new Vector2(r * 2 + 4, r * 2 + 4));
    }

    /// <summary>
    /// SPLOOSH! Where the capsule came down ("age" = seconds since): a crown of water shoots up and falls back, and white
    /// rings spread on the water. Returns the area it covers.
    /// </summary>
    public static Rect2 Splash(CanvasItem canvas, Vector2 at, float age)
    {
        for (int i = 0; i < 3; i++) // the rings
        {
            float a = age - i * 0.14f;
            if (a < 0 || a > 0.8f) continue;
            float p = a / 0.8f;
            canvas.DrawSetTransform(at, 0, new Vector2(1, 0.42f)); // (flat: they're on the water)
            canvas.DrawArc(Vector2.Zero, 10 + 52 * p, 0, Mathf.Tau, 36, new Color(1, 1, 1, 1 - p), 6 * (1 - p) + 1.5f);
            canvas.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        }
        if (age < 0.5f) // a crown of water around it: white spouts on both sides, up and down again
        {
            float height = 36 * Mathf.Sin(Mathf.Pi * age / 0.5f);
            for (int i = 0; i < 6; i++)
            {
                float side = i < 3 ? -1 : 1, step = i % 3;
                var foot = at + new Vector2(side * (14 + step * 7), 0);
                var top = foot + new Vector2(side * (4 + step * 6), -Mathf.Max(1, height * (1 - step * 0.22f)));
                canvas.DrawLine(foot, top, new Color(0.85f, 0.95f, 1f), 5);
                canvas.DrawCircle(top, 3.4f, Colors.White);
            }
        }
        return new Rect2(at - new Vector2(64, 40), new Vector2(128, 66));
    }

    /// <summary>The white V of foam behind little Bolt-E's board while he surfs ("way" = the way he's going).</summary>
    public static void Wake(CanvasItem canvas, Vector2 feet, Vector2 way, float time)
    {
        var side = new Vector2(-way.Y, way.X);
        var back = feet - way * 12 + new Vector2(0, 2);
        float wiggle = Mathf.Sin(time * 18) * 1.5f;
        Shapes.Ellipse(canvas, feet + new Vector2(0, 3), new Vector2(22, 5), new Color(1, 1, 1, 0.5f)); // foam under the board
        canvas.DrawLine(back, back - way * 32 + side * (12 + wiggle), new Color(1, 1, 1, 0.9f), 3);
        canvas.DrawLine(back, back - way * 32 - side * (12 - wiggle), new Color(1, 1, 1, 0.9f), 3);
        canvas.DrawLine(back - way * 10, back - way * 44 + side * 18, new Color(1, 1, 1, 0.45f), 2);
        canvas.DrawLine(back - way * 10, back - way * 44 - side * 18, new Color(1, 1, 1, 0.45f), 2);
    }

    // ---------- The next boss's badge ----------

    static readonly Color BadgeRed = new(0.6f, 0.1f, 0.12f), BadgeCream = new(1f, 0.94f, 0.82f);

    /// <summary>
    /// The next boss's badge: a round red medallion with his face (BIG RUSTY wears the next world's outfit, and his party
    /// hat pokes out of the top). "scale" makes it pop in. Returns the area it covers.
    /// </summary>
    public static Rect2 Teaser(CanvasItem canvas, Vector2 middle, float scale, BossKind boss, BossOutfit outfit, float time)
    {
        float s = Mathf.Max(0.05f, scale);
        canvas.DrawSetTransform(middle, 0, new Vector2(s, s));
        canvas.DrawCircle(Vector2.Zero, 26, Ink);
        canvas.DrawCircle(Vector2.Zero, 24, BadgeRed);
        canvas.DrawCircle(Vector2.Zero, 19.5f, BadgeCream);
        canvas.DrawArc(Vector2.Zero, 22, Mathf.Pi * 1.1f, Mathf.Pi * 1.55f, 10, new Color(1, 1, 1, 0.35f), 2.5f); // a shine
        switch (boss)
        {
            case BossKind.Vroomba: VroombaBadge(canvas, time); break;
            case BossKind.Dronezilla: DronezillaBadge(canvas, time); break;
            case BossKind.OptiMax: OptiMaxBadge(canvas, time); break;
            default: RustyBadge(canvas, outfit, time); break;
        }
        canvas.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        return new Rect2(middle - new Vector2(28, 36) * s, new Vector2(56, 64) * s);
    }

    // BIG RUSTY's colors, and his outfits' shapes (made once)
    static readonly Color RustyOrange = new(0.97f, 0.58f, 0.12f), RustyDark = new(0.85f, 0.45f, 0.08f), RustyBrown = new(0.45f, 0.28f, 0.17f);
    static readonly Color RustyScreen = new(0.05f, 0.04f, 0.03f), EyeYellow = new(1f, 0.9f, 0.15f);
    static readonly Color PartyPink = new(1f, 0.4f, 0.7f), PartyYellow = new(1f, 0.9f, 0.3f), ShadesBlack = new(0.08f, 0.08f, 0.12f);
    static readonly Color BobbleRed = new(0.9f, 0.2f, 0.25f), GoggleStrap = new(0.15f, 0.13f, 0.22f), FireRed = new(0.9f, 0.12f, 0.1f);
    static readonly Vector2[] PartyHatShape = { new(-7.5f, -6), new(7.5f, -6), new(0, -27) };
    static readonly Vector2[] WoollyHat = HalfDisc(13, 13);

    static void RustyBadge(CanvasItem canvas, BossOutfit outfit, float time)
    {
        var head = new Vector2(0, 3);
        for (int side = -1; side <= 1; side += 2) // his ear discs
        {
            canvas.DrawCircle(head + new Vector2(side * 14, -1), 5.5f, Ink);
            canvas.DrawCircle(head + new Vector2(side * 14, -1), 4, RustyDark);
        }
        canvas.DrawCircle(head, 13.5f, Ink);
        canvas.DrawCircle(head, 12, RustyOrange);
        canvas.DrawArc(head, 10, Mathf.Pi * 1.3f, Mathf.Pi * 1.7f, 8, RustyBrown, 3); // the brown stripe on top
        Shapes.RoundRect(canvas, new Rect2(-10, -2, 18, 12), 5, RustyScreen);        // his face screen
        AngryEye(canvas, head + new Vector2(-5.5f, 1), innerOnRight: true);
        AngryEye(canvas, head + new Vector2(2.5f, 1), innerOnRight: false);
        switch (outfit)
        {
            case BossOutfit.PartyHat:
                Outlined(canvas, PartyHatShape, Vector2.Zero, 1, PartyPink, 2);
                canvas.DrawLine(new Vector2(-4.4f, -13), new Vector2(4.4f, -13), PartyYellow, 2.5f);
                canvas.DrawLine(new Vector2(-2.2f, -20), new Vector2(2.2f, -20), PartyYellow, 2.5f);
                canvas.DrawCircle(new Vector2(0, -28), 4.5f, Ink);
                canvas.DrawCircle(new Vector2(0, -28), 3.3f, Colors.White);
                break;
            case BossOutfit.Sunglasses:
                Shapes.RoundRect(canvas, new Rect2(-11, 0.5f, 9, 7), 2.5f, ShadesBlack);
                Shapes.RoundRect(canvas, new Rect2(-1, 0.5f, 9, 7), 2.5f, ShadesBlack);
                canvas.DrawLine(new Vector2(-3, 2.5f), new Vector2(0, 2.5f), ShadesBlack, 2);
                canvas.DrawLine(new Vector2(-9, 6), new Vector2(-6, 2), new Color(1, 1, 1, 0.8f), 1.5f);
                canvas.DrawLine(new Vector2(1, 6), new Vector2(4, 2), new Color(1, 1, 1, 0.8f), 1.5f);
                break;
            case BossOutfit.BobbleHat:
                Outlined(canvas, WoollyHat, new Vector2(0, -4), 1, BobbleRed, 2);
                Shapes.RoundRect(canvas, new Rect2(-14, -7, 28, 6), 3, Colors.White);
                canvas.DrawCircle(new Vector2(0, -20), 5.2f, Ink);
                canvas.DrawCircle(new Vector2(0, -20), 4, Colors.White);
                break;
            case BossOutfit.SpaceHelmet:
                canvas.DrawCircle(head, 19, new Color(0.7f, 0.9f, 1f, 0.3f));
                canvas.DrawArc(head, 19, 0, Mathf.Tau, 32, Colors.White, 2);
                canvas.DrawArc(head, 15, Mathf.Pi * 1.1f, Mathf.Pi * 1.45f, 8, new Color(1, 1, 1, 0.85f), 2.5f);
                break;
            case BossOutfit.NeonGoggles:
            {
                float glow = 0.6f + 0.4f * Mathf.Sin(time * 6);
                canvas.DrawLine(new Vector2(-13, -5), new Vector2(13, -5), GoggleStrap, 3);
                for (int side = -1; side <= 1; side += 2)
                {
                    var lens = new Vector2(side * 5.5f, -6);
                    canvas.DrawCircle(lens, 5, Ink);
                    canvas.DrawCircle(lens, 3.6f, new Color(0.25f, 0.9f, 1f));
                    canvas.DrawArc(lens, 4.6f, 0, Mathf.Tau, 14, new Color(NeonPink, glow), 1.6f);
                }
                break;
            }
            case BossOutfit.FireHelmet:
                Shapes.Ellipse(canvas, new Vector2(2, -6), new Vector2(17, 3), FireRed.Darkened(0.3f)); // the brim
                Outlined(canvas, WoollyHat, new Vector2(0, -6), 1, FireRed, 2);
                canvas.DrawArc(new Vector2(0, -6), 9, Mathf.Pi * 1.2f, Mathf.Pi * 1.8f, 8, FireRed.Lightened(0.35f), 2.5f); // the ridge
                canvas.DrawCircle(new Vector2(-6, -11), 3.6f, Gold); // the badge
                canvas.DrawCircle(new Vector2(-6, -11), 1.5f, FireRed);
                break;
        }
    }

    /// <summary>A little angry yellow eye (a slanted brow cut out of its inner corner).</summary>
    static void AngryEye(CanvasItem canvas, Vector2 at, bool innerOnRight)
    {
        canvas.DrawRect(new Rect2(at.X - 2.5f, at.Y - 2, 5, 4), EyeYellow);
        float inner = innerOnRight ? at.X + 2.6f : at.X - 2.6f, outer = innerOnRight ? at.X - 2.6f : at.X + 2.6f;
        Triangle(canvas, new Vector2(outer, at.Y - 2.2f), new Vector2(inner, at.Y - 2.2f), new Vector2(inner, at.Y + 0.6f), RustyScreen);
    }

    static readonly Color VroombaSide = new(0.27f, 0.28f, 0.33f), VroombaSilver = new(0.8f, 0.82f, 0.88f), VroombaLid = new(0.17f, 0.18f, 0.22f);
    static readonly Color VroombaBlue = new(0.35f, 0.8f, 1f), Bristles = new(1f, 0.8f, 0.25f);

    /// <summary>VROOMBA: a round robot vacuum with blue sensor eyes and a spinning side brush.</summary>
    static void VroombaBadge(CanvasItem canvas, float time)
    {
        Shapes.Ellipse(canvas, new Vector2(0, 5), new Vector2(18.5f, 12.5f), Ink);
        Shapes.Ellipse(canvas, new Vector2(0, 5), new Vector2(17, 11), VroombaSide);
        Shapes.Ellipse(canvas, new Vector2(0, 0), new Vector2(16, 8.5f), VroombaSilver);
        Shapes.Ellipse(canvas, new Vector2(0, -0.5f), new Vector2(13, 6.5f), VroombaLid);
        canvas.DrawCircle(new Vector2(0, -2), 2.2f, VroombaBlue); // the CLEAN button
        for (int side = -1; side <= 1; side += 2)
        {
            canvas.DrawCircle(new Vector2(side * 6, 9), 3, Ink);
            canvas.DrawCircle(new Vector2(side * 6, 9), 2.1f, VroombaBlue);
        }
        var brush = new Vector2(-14, 13);
        for (int i = 0; i < 3; i++) canvas.DrawLine(brush, brush + Vector2.Right.Rotated(time * 9 + i * Mathf.Tau / 3) * 6.5f, Bristles, 1.8f);
    }

    static readonly Color Gunmetal = new(0.42f, 0.45f, 0.52f), DroneVisor = new(0.06f, 0.08f, 0.14f), DroneEyes = new(0.45f, 1f, 0.55f);
    static readonly Color DroneYellow = new(1f, 0.82f, 0.15f), HazardBlack = new(0.1f, 0.1f, 0.12f), CameraRed = new(1f, 0.15f, 0.12f);

    /// <summary>DRONEZILLA: a big drone with green LED eyes, hazard stripes and two spinning rotors.</summary>
    static void DronezillaBadge(CanvasItem canvas, float time)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            canvas.DrawLine(new Vector2(side * 9, -3), new Vector2(side * 17, -10), Ink, 3);
            var rotor = new Vector2(side * 17, -12);
            float spin = Mathf.Abs(Mathf.Sin(time * 14 + side));
            Shapes.Ellipse(canvas, rotor, new Vector2(2 + 8 * spin, 2.2f), new Color(0.9f, 0.92f, 0.96f));
            canvas.DrawCircle(rotor, 2.2f, Ink);
        }
        Shapes.RoundRect(canvas, new Rect2(-15, -9, 30, 22), 5, Ink);
        Shapes.RoundRect(canvas, new Rect2(-13, -7, 26, 18), 4, Gunmetal);
        Shapes.RoundRect(canvas, new Rect2(-10, -5, 15, 8), 3, DroneVisor);
        canvas.DrawCircle(new Vector2(-6, -1), 2, DroneEyes);
        canvas.DrawCircle(new Vector2(1, -1), 2, DroneEyes);
        canvas.DrawRect(new Rect2(7, -5, 4, 3), DroneEyes);
        canvas.DrawRect(new Rect2(-13, 5, 26, 5), DroneYellow);
        for (int i = 0; i < 4; i++)
            Quad(canvas, new Vector2(-12 + i * 7, 10), new Vector2(-9.5f + i * 7, 5), new Vector2(-6.5f + i * 7, 5), new Vector2(-9 + i * 7, 10), HazardBlack);
        canvas.DrawCircle(new Vector2(0, 14), 3.2f, Ink); // his red camera eye underneath
        canvas.DrawCircle(new Vector2(0, 14), 2, CameraRed);
    }

    static readonly Color OptiBlack = new(0.05f, 0.05f, 0.07f), OptiTeal = new(0.25f, 0.95f, 0.88f), OptiWhite = new(0.93f, 0.93f, 0.95f);

    /// <summary>OPTI-MAX: a glossy black head with a glowing teal visor and a white jaw.</summary>
    static void OptiMaxBadge(CanvasItem canvas, float time)
    {
        canvas.DrawCircle(new Vector2(0, 1), 15, Ink);
        canvas.DrawCircle(new Vector2(0, 1), 13.5f, OptiBlack);
        canvas.DrawArc(new Vector2(0, 1), 10, Mathf.Pi * 1.15f, Mathf.Pi * 1.45f, 8, new Color(1, 1, 1, 0.5f), 2.5f); // a shine
        float glow = 0.75f + 0.25f * Mathf.Sin(time * 5);
        Shapes.RoundRect(canvas, new Rect2(-10, -3, 20, 5), 2.5f, OptiTeal.Darkened(0.35f * (1 - glow)));            // the visor
        Shapes.RoundRect(canvas, new Rect2(-7, 6, 14, 6.5f), 2.5f, OptiWhite);                                         // the jaw
        canvas.DrawLine(new Vector2(-4, 9.2f), new Vector2(4, 9.2f), new Color(0.7f, 0.71f, 0.76f), 1);
    }

    // ---------- The crown (the GOLD MAP) ----------
    static readonly Vector2[] CrownShape = { new(0, 0), new(-2, -15), new(6, -7), new(12, -18), new(18, -7), new(26, -15), new(24, 0) };

    /// <summary>A gold crown with 3 jewels (for a player who CLEARED all 7 worlds). "bottomLeft" = its bottom-left corner.</summary>
    public static Rect2 Crown(CanvasItem canvas, Vector2 bottomLeft)
    {
        Outlined(canvas, CrownShape, bottomLeft, 1, Gold, 2.5f);
        canvas.DrawCircle(bottomLeft + new Vector2(5, -4), 2.2f, new Color(0.4f, 0.65f, 1f));
        canvas.DrawCircle(bottomLeft + new Vector2(12, -5), 2.7f, OpenRed);
        canvas.DrawCircle(bottomLeft + new Vector2(19, -4), 2.2f, new Color(0.4f, 0.9f, 0.5f));
        return new Rect2(bottomLeft + new Vector2(-4, -21), new Vector2(32, 23));
    }
}
