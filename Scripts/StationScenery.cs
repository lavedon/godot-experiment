using Godot;

/// <summary>
/// Drawing UPSIDE-DOWN STATION! (This is more of Main: "partial" means one class spread over several files.)
///
/// A space-station corridor: deep purple space with drifting stars and a big ringed planet outside the windows, metal
/// walls with seams, rivets, blinking lights, glowing arrow signs and hazard stripes, and two tracks: the FLOOR with a
/// glowing cyan edge, and the CEILING with a glowing magenta edge. Light dashes zoom along both, so you can see how fast
/// you're going. The ceiling slides up off the screen when a boss comes. Everything is drawn with code, and it fades
/// in and out while the worlds change (like every world's things).
/// </summary>
public partial class Main
{
    // ---- The station's look (try changing these!) ----
    const float WindowTop = 236, WindowBottom = 436;  // the big windows in the back wall
    const float PillarSpacing = 480;                   // from one wall pillar to the next (the windows are in between)
    const float PillarWidth = 130;
    const float DashSpacing = 90;                      // the light dashes along the floor and the ceiling

    static readonly Color WallSeam = new(0.28f, 0.26f, 0.4f);
    static readonly Color Rivet = new(0.62f, 0.6f, 0.75f);
    static readonly Color HazardYellow = new(1f, 0.8f, 0.1f);
    static readonly Color HazardBlack = new(0.1f, 0.08f, 0.12f);
    static readonly Color RailMetal = new(0.42f, 0.42f, 0.55f);
    static readonly Color PipeMetal = new(0.33f, 0.32f, 0.46f);
    static readonly Color[] LightColors = { new(0.3f, 1f, 0.45f), new(1f, 0.3f, 0.3f), new(0.35f, 0.95f, 1f), new(1f, 0.85f, 0.2f) };

    // Corner points and colors, made once and reused every frame (so the game doesn't make new lists all the time)
    static readonly Vector2[] stationSpace =
    {
        new(-40, -40), new(ScreenWidth + 40, -40), new(ScreenWidth + 40, ScreenHeight + 40), new(-40, ScreenHeight + 40),
    };
    readonly Color[] stationSpaceColors = new Color[4];
    static readonly Vector2[] chevron = new Vector2[6];
    static readonly Vector2[] stripe = new Vector2[4];

    /// <summary>The stars outside the station (made once, always the same "random" ones).</summary>
    static readonly SkyStar[] StationStars = MakeStationStars();

    static SkyStar[] MakeStationStars()
    {
        var random = new Random(55);
        var stars = new SkyStar[70];
        for (int i = 0; i < stars.Length; i++)
            stars[i] = new SkyStar(new Vector2(random.NextSingle() * 1600, 20 + random.NextSingle() * 560), 1f + random.NextSingle() * 2f);
        return stars;
    }

    /// <summary>The whole station, over the hills and the ground (it fades in and out while the worlds change).</summary>
    void DrawStation()
    {
        float a = MixAmount(w => w.Scenery == Scenery.Station);
        if (a < 0.02f) return;
        var station = Worlds.All[Worlds.StationIndex];

        DrawStationSpace(station, a);
        DrawStationWall(station, a);
        DrawStationFloor(station, a);
        DrawStationCeiling(station, a);
        drawnWorldThings.Add("station");
    }

    /// <summary>Deep space: a purple glow, stars drifting by slowly, and a big ringed planet.</summary>
    void DrawStationSpace(World station, float a)
    {
        stationSpaceColors[0] = stationSpaceColors[1] = new Color(station.SkyTop, a);
        stationSpaceColors[2] = stationSpaceColors[3] = new Color(station.SkyBottom, a);
        DrawPolygon(stationSpace, stationSpaceColors);

        for (int i = 0; i < StationStars.Length; i++)
        {
            float x = Scrolled(StationStars[i].At.X, cloudScroll * 0.6f, 1600, 20);
            float twinkle = 0.55f + 0.45f * Mathf.Sin(2.5f * worldTime + i * 1.7f);
            DrawCircle(new Vector2(x, StationStars[i].At.Y), StationStars[i].Size, new Color(1, 1, 1, twinkle * a));
        }

        // The ringed planet drifts by very slowly (it's very far away)
        float planetX = Scrolled(980, cloudScroll * 0.12f, 2200, 300);
        DrawRingedPlanet(new Vector2(planetX, 330), 78, station.ThingColor, a);
    }

    /// <summary>A big ringed planet: the back of its ring, the planet with a shadow and stripes, then the front of the ring.</summary>
    void DrawRingedPlanet(Vector2 at, float radius, Color color, float a)
    {
        var ring = new Color(1f, 0.9f, 0.7f, 0.85f * a);
        const float tilt = -0.25f;
        DrawSetTransform(at, tilt, new Vector2(1, 0.28f));
        DrawArc(Vector2.Zero, radius * 1.65f, Mathf.Pi, Mathf.Tau, 32, ring, 9, true);   // the back half of the ring
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);

        DrawCircle(at, radius, new Color(color, a));
        DrawCircle(at + new Vector2(-radius * 0.2f, -radius * 0.25f), radius * 0.72f, new Color(color.Lightened(0.2f), a)); // light side
        DrawArc(at, radius * 0.55f, Mathf.Pi * 0.15f, Mathf.Pi * 0.85f, 16, new Color(color.Darkened(0.25f), a), 7, true); // stripes
        DrawArc(at, radius * 0.85f, Mathf.Pi * 0.1f, Mathf.Pi * 0.9f, 16, new Color(color.Darkened(0.35f), a), 5, true);

        DrawSetTransform(at, tilt, new Vector2(1, 0.28f));
        DrawArc(Vector2.Zero, radius * 1.65f, 0, Mathf.Pi, 32, ring, 9, true);            // the front half of the ring
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        drawnWorldThings.Add("ringed planet");
    }

    /// <summary>
    /// The back wall: metal panels above and below the big windows, pillars with round portholes between them, seams,
    /// rivets, blinking lights, glowing arrow signs, and yellow-and-black hazard stripes along the floor.
    /// </summary>
    void DrawStationWall(World station, float a)
    {
        var wall = new Color(station.FarHills, a);
        var trim = new Color(station.NearHills, a);
        var seam = new Color(WallSeam, a);
        var rivet = new Color(Rivet, a);
        float wallScroll = nearHillScroll; // (the wall is a little farther away than the floor, so it slides slower)

        DrawRect(new Rect2(-40, -40, ScreenWidth + 80, WindowTop + 40), wall);                    // above the windows
        DrawRect(new Rect2(-40, WindowBottom, ScreenWidth + 80, GroundY - WindowBottom), wall);  // below the windows
        DrawRect(new Rect2(-40, WindowTop - 6, ScreenWidth + 80, 6), trim);                       // the window sills
        DrawRect(new Rect2(-40, WindowBottom, ScreenWidth + 80, 6), trim);

        // The pillars between the windows, each with a round porthole
        for (int i = 0; i < 4; i++)
        {
            float x = Scrolled(i * PillarSpacing, wallScroll, PillarSpacing * 4, PillarWidth + 20);
            DrawRect(new Rect2(x, WindowTop, PillarWidth, WindowBottom - WindowTop), wall);
            DrawRect(new Rect2(x, WindowTop, 6, WindowBottom - WindowTop), trim);
            DrawRect(new Rect2(x + PillarWidth - 6, WindowTop, 6, WindowBottom - WindowTop), trim);
            var porthole = new Vector2(x + PillarWidth / 2, (WindowTop + WindowBottom) / 2);
            DrawCircle(porthole, 38, trim);
            DrawCircle(porthole, 30, new Color(station.SkyTop, a));
            DrawCircle(porthole + new Vector2(-10, -8), 2, new Color(1, 1, 1, a));
            DrawCircle(porthole + new Vector2(12, 9), 1.5f, new Color(1, 1, 1, 0.7f * a));
            DrawArc(porthole, 24, Mathf.Pi * 1.1f, Mathf.Pi * 1.45f, 8, new Color(1, 1, 1, 0.35f * a), 4, true); // a shine on the glass
            for (int r = 0; r < 4; r++) // 4 rivets around it
                DrawCircle(porthole + Vector2.Right.Rotated(r * Mathf.Pi / 2 + Mathf.Pi / 4) * 34, 2.5f, rivet);
        }
        drawnWorldThings.Add("portholes");

        // Panel seams and rivets on the wall (every 160 pixels)
        for (int i = 0; i < 10; i++)
        {
            float x = Scrolled(i * 160f, wallScroll, 1600, 20);
            DrawLine(new Vector2(x, -40), new Vector2(x, WindowTop - 6), seam, 2);
            DrawLine(new Vector2(x, WindowBottom + 6), new Vector2(x, GroundY), seam, 2);
            DrawCircle(new Vector2(x + 10, WindowBottom + 18), 2.5f, rivet);
            DrawCircle(new Vector2(x + 10, GroundY - 34), 2.5f, rivet);
            DrawCircle(new Vector2(x + 10, WindowTop - 20), 2.5f, rivet);

            // A blinking indicator light on every other panel, under the ceiling
            if (i % 2 == 0)
            {
                bool on = Mathf.Sin(worldTime * 3 + i * 1.3f) > -0.2f;
                var light = LightColors[i / 2 % LightColors.Length];
                DrawCircle(new Vector2(x + 80, WindowTop - 26), 6, new Color(light, (on ? 0.3f : 0.08f) * a));
                DrawCircle(new Vector2(x + 80, WindowTop - 26), 3.5f, new Color(on ? light : light.Darkened(0.6f), a));
            }
        }

        // Glowing arrow signs on the lower wall: >>> pointing the way Bolt-E rides, lighting up one after another
        for (int i = 0; i < 3; i++)
        {
            float x = Scrolled(i * 640f + 300, wallScroll, 1920, 120);
            DrawRect(new Rect2(x - 6, 488, 108, 44), new Color(HazardBlack, 0.9f * a));
            for (int c = 0; c < 3; c++)
            {
                float glow = Mathf.PosMod(worldTime * 2 - c * 0.33f, 1) < 0.5f ? 1f : 0.35f;
                DrawChevron(new Vector2(x + 18 + c * 30, 510), new Color(NeonCyan, glow * a));
            }
        }
        drawnWorldThings.Add("arrow signs");

        DrawHazardStripes(GroundY - 16, 16, groundScroll, a); // along the floor
    }

    /// <summary>A glowing ">" arrow (its middle at "at").</summary>
    void DrawChevron(Vector2 at, Color color)
    {
        chevron[0] = at + new Vector2(-10, -14);
        chevron[1] = at + new Vector2(0, -14);
        chevron[2] = at + new Vector2(12, 0);
        chevron[3] = at + new Vector2(0, 14);
        chevron[4] = at + new Vector2(-10, 14);
        chevron[5] = at + new Vector2(2, 0);
        DrawColoredPolygon(chevron, color);
    }

    /// <summary>A band of yellow-and-black hazard stripes, from y "top", "height" pixels tall, sliding along with "scroll".</summary>
    void DrawHazardStripes(float top, float height, float scroll, float a)
    {
        DrawRect(new Rect2(-40, top, ScreenWidth + 80, height), new Color(HazardYellow, a));
        var black = new Color(HazardBlack, a);
        for (int i = 0; i < 28; i++)
        {
            float x = Scrolled(i * 50f, scroll, 1400, 60);
            stripe[0] = new Vector2(x, top + height);
            stripe[1] = new Vector2(x + height, top);
            stripe[2] = new Vector2(x + height + 20, top);
            stripe[3] = new Vector2(x + 20, top + height);
            DrawColoredPolygon(stripe, black);
        }
        drawnWorldThings.Add("hazard stripes");
    }

    /// <summary>The floor track: dark metal, a rail with a glowing CYAN edge, light dashes zooming along, and a grate.</summary>
    void DrawStationFloor(World station, float a)
    {
        var neon = new Color(station.Grass, a);
        DrawRect(new Rect2(-40, GroundY, ScreenWidth + 80, ScreenHeight - GroundY + 40), new Color(station.Dirt, a));
        DrawRect(new Rect2(-40, GroundY, ScreenWidth + 80, 16), new Color(RailMetal, a));
        for (int i = 0; i < 34; i++) // the grate under the rail
        {
            float x = Scrolled(i * 40f, groundScroll, 1360, 40);
            DrawLine(new Vector2(x, GroundY + 24), new Vector2(x, ScreenHeight + 40), new Color(WallSeam, a), 3);
        }
        DrawLine(new Vector2(-40, GroundY + 1), new Vector2(ScreenWidth + 40, GroundY + 1), new Color(station.Grass, 0.3f * a), 14);
        DrawLine(new Vector2(-40, GroundY + 1), new Vector2(ScreenWidth + 40, GroundY + 1), neon, 4);
        DrawSpeedDashes(GroundY + 9, station.Grass, a);
        drawnWorldThings.Add("floor rail");
    }

    /// <summary>
    /// The ceiling track: like the floor, but upside-down, with a glowing MAGENTA edge, and pipes running along it.
    /// When a boss comes it slides up, off the top of the screen.
    /// </summary>
    void DrawStationCeiling(World station, float a)
    {
        float y = CeilingY; // (its surface: it slides up when the ceiling opens)
        if (y < -45) return;
        var neon = new Color(station.Pebbles, a);
        DrawRect(new Rect2(-40, -40, ScreenWidth + 80, y + 40), new Color(station.Dirt, a));
        DrawRect(new Rect2(-40, y - 16, ScreenWidth + 80, 16), new Color(RailMetal, a));

        // Two pipes along the ceiling, with a ring around them every 200 pixels
        foreach (float pipeY in PipeHeights)
        {
            DrawRect(new Rect2(-40, y + pipeY - 7, ScreenWidth + 80, 14), new Color(PipeMetal, a));
            DrawLine(new Vector2(-40, y + pipeY - 3), new Vector2(ScreenWidth + 40, y + pipeY - 3), new Color(1, 1, 1, 0.15f * a), 2);
            for (int i = 0; i < 8; i++)
            {
                float x = Scrolled(i * 200f + pipeY * 3, groundScroll, 1600, 30);
                DrawRect(new Rect2(x, y + pipeY - 10, 10, 20), new Color(RailMetal, a));
            }
        }

        DrawLine(new Vector2(-40, y - 1), new Vector2(ScreenWidth + 40, y - 1), new Color(station.Pebbles, 0.3f * a), 14);
        DrawLine(new Vector2(-40, y - 1), new Vector2(ScreenWidth + 40, y - 1), neon, 4);
        DrawSpeedDashes(y - 9, station.Pebbles, a);
        drawnWorldThings.Add("ceiling");
    }

    static readonly float[] PipeHeights = { -60, -100 }; // (how far above the ceiling's surface the pipes are)

    /// <summary>Little bright dashes zooming along a track: the faster Bolt-E goes, the faster they zoom.</summary>
    void DrawSpeedDashes(float y, Color color, float a)
    {
        var bright = new Color(color.Lightened(0.5f), a);
        for (int i = 0; i < 16; i++)
        {
            float x = Scrolled(i * DashSpacing, groundScroll, DashSpacing * 16, 40);
            DrawRect(new Rect2(x, y - 2, 26, 4), bright);
        }
    }
}
