using Godot;

/// <summary>
/// Drawing VOLCANO ISLAND and its LAVA CAVES! (This is more of Main: "partial" means one class spread over several files.)
///
/// The surface: a dark red sky with smoke clouds, a big volcano far away that puffs smoke and sometimes ERUPTS, black
/// rock spires, a glowing LAVA RIVER where the ground should be (with a wavy shiny top, dark crusts drifting along and
/// bubbles plopping), and glowing embers falling everywhere.
/// The caves: dark rock walls with glowing purple and cyan crystals, lavafalls pouring down, a ceiling full of
/// stalactites with glowing drips, little bats flying far away, and a solid rock floor with glowing cracks.
/// The HARD cave is darker and redder. Everything is drawn with code, and it fades in and out while the worlds change.
/// </summary>
public partial class Main
{
    // ---- The volcano's look (try changing these!) ----
    const float VolcanoPeakY = 248;          // the top of the big volcano (its crater)
    const float VolcanoHalfWidth = 440;      // half as wide as the volcano is at the bottom
    const int EmberCount = 46;               // how many glowing embers fall

    static readonly Color VolcanoRock = new(0.2f, 0.08f, 0.07f);
    static readonly Color SpireRock = new(0.13f, 0.06f, 0.06f);
    static readonly Color LavaBright = new(1f, 0.62f, 0.12f);
    static readonly Color LavaDeep = new(0.82f, 0.18f, 0.04f);
    static readonly Color LavaShineColor = new(1f, 0.92f, 0.5f);
    static readonly Color Crust = new(0.42f, 0.11f, 0.04f);
    static readonly Color Smoke = new(0.25f, 0.2f, 0.22f);
    static readonly Color[] CrystalGlow = { new(0.72f, 0.4f, 1f), new(0.35f, 0.95f, 1f) }; // purple and cyan
    static readonly float[] SpireHeights = { 190, 150, 230, 170, 210, 140, 200 };
    static readonly float[] CrystalTilt = { -0.5f, 0f, 0.45f };
    static readonly float[] CrystalLength = { 30, 40, 24 };

    // The caves' colors, by hardness { EASY, MEDIUM, HARD }: the walls from top to bottom, and the ceiling
    static readonly Color[] CaveWallTop = { new(0.09f, 0.06f, 0.13f), new(0.1f, 0.05f, 0.09f), new(0.1f, 0.02f, 0.02f) };
    static readonly Color[] CaveWallBottom = { new(0.2f, 0.11f, 0.13f), new(0.24f, 0.1f, 0.08f), new(0.32f, 0.07f, 0.03f) };
    static readonly Color[] CaveCeiling = { new(0.13f, 0.08f, 0.1f), new(0.14f, 0.07f, 0.07f), new(0.16f, 0.04f, 0.03f) };
    static readonly int[] Lavafalls = { 2, 3, 4 };   // more lavafalls in the harder caves

    // Corner points and colors, made once and reused every frame (so the game doesn't make new lists all the time)
    static readonly Vector2[] volcanoShape = new Vector2[8];
    static readonly Vector2[] volcanoShade = new Vector2[6];
    static readonly Vector2[] spire = new Vector2[5];
    static readonly Vector2[] streak = new Vector2[5];
    static readonly Vector2[] lavaWave = new Vector2[34];
    static readonly Vector2[] quad = new Vector2[4];
    readonly Color[] quadColors = new Color[4];
    static readonly Vector2[] gem = new Vector2[6];
    static readonly Vector2[] crackLine = new Vector2[4];

    /// <summary>The eruption's lava blobs: how fast each one flies (sideways and up), and when it starts (made once).</summary>
    static readonly (float Vx, float Vy, float Delay)[] EruptBlobs = MakeEruptBlobs();

    static (float, float, float)[] MakeEruptBlobs()
    {
        var random = new Random(77);
        var blobs = new (float, float, float)[16];
        for (int i = 0; i < blobs.Length; i++)
            blobs[i] = (random.NextSingle() * 320 - 160, -420 - random.NextSingle() * 220, random.NextSingle() * 0.9f);
        return blobs;
    }

    /// <summary>A glowing ember: where it is, how fast it falls, and how big it is.</summary>
    class Ember
    {
        public Vector2 At;
        public float Speed, Size, Flicker;
    }

    readonly Ember[] embers = MakeEmbers();

    static Ember[] MakeEmbers()
    {
        var random = new Random(9);
        var list = new Ember[EmberCount];
        for (int i = 0; i < list.Length; i++)
        {
            list[i] = new Ember
            {
                At = new Vector2(random.NextSingle() * ScreenWidth, random.NextSingle() * ScreenHeight),
                Speed = 30 + random.NextSingle() * 50,
                Size = 1.5f + random.NextSingle() * 2,
                Flicker = random.NextSingle() * 10,
            };
        }
        return list;
    }

    /// <summary>The embers fall slowly and drift back as the world goes by (they wrap around the screen).</summary>
    void UpdateEmbers(float dt)
    {
        if (!InVolcano && fromWorld.Weather != Weather.Embers) return;
        foreach (var ember in embers)
        {
            ember.At += new Vector2(-(18 + speed * boost * 0.12f) + Mathf.Sin(worldTime * 1.5f + ember.Flicker) * 15, ember.Speed) * dt;
            if (ember.At.Y > ScreenHeight + 10) ember.At.Y -= ScreenHeight + 20;
            if (ember.At.X < -10) ember.At.X += ScreenWidth + 20;
        }
    }

    /// <summary>A four-cornered shape with a color at the top and another at the bottom (a "gradient").</summary>
    void DrawGradient(float top, float bottom, Color topColor, Color bottomColor)
    {
        quad[0] = new Vector2(-40, top);
        quad[1] = new Vector2(ScreenWidth + 40, top);
        quad[2] = new Vector2(ScreenWidth + 40, bottom);
        quad[3] = new Vector2(-40, bottom);
        quadColors[0] = quadColors[1] = topColor;
        quadColors[2] = quadColors[3] = bottomColor;
        DrawPolygon(quad, quadColors);
    }

    // ---------- The surface ----------

    /// <summary>Behind the hills: the big volcano (smoking, sometimes ERUPTING) and black rock spires.</summary>
    void DrawVolcanoBackdrop()
    {
        float a = MixAmount(w => w.Scenery == Scenery.Volcano);
        if (a < 0.02f || InCave) return;
        DrawBigVolcano(Scrolled(900, cloudScroll * 0.6f, 2600, 520), a);

        // Black rock spires on the far horizon (the hills cover their bottoms)
        var dark = new Color(SpireRock, a);
        for (int i = 0; i < SpireHeights.Length; i++)
        {
            float x = Scrolled(i * 240f + 60, farHillScroll * 0.7f, 240f * SpireHeights.Length, 80);
            float h = SpireHeights[i];
            spire[0] = new Vector2(x - 62, GroundY);
            spire[1] = new Vector2(x - 26, GroundY - h * 0.55f);
            spire[2] = new Vector2(x - 6, GroundY - h);
            spire[3] = new Vector2(x + 14, GroundY - h * 0.7f);
            spire[4] = new Vector2(x + 58, GroundY);
            DrawColoredPolygon(spire, dark);
        }
        drawnWorldThings.Add("rock spires");
    }

    /// <summary>The big volcano, its glowing crater, lava streaks down its sides, smoke puffs, and the eruption.</summary>
    void DrawBigVolcano(float x, float a)
    {
        var crater = new Vector2(x, VolcanoPeakY);
        float pulse = 0.5f + 0.5f * Mathf.Sin(worldTime * 2);
        bool erupting = eruptTime >= 0;
        DrawCircle(crater + new Vector2(0, -8), erupting ? 150 : 95, new Color(1f, 0.45f, 0.1f, (erupting ? 0.3f : 0.16f) * a)); // the glow over the crater

        volcanoShape[0] = new Vector2(x - VolcanoHalfWidth, GroundY);
        volcanoShape[1] = new Vector2(x - 250, 430);
        volcanoShape[2] = new Vector2(x - 120, 292);
        volcanoShape[3] = new Vector2(x - 66, VolcanoPeakY);
        volcanoShape[4] = new Vector2(x + 66, VolcanoPeakY);
        volcanoShape[5] = new Vector2(x + 130, 300);
        volcanoShape[6] = new Vector2(x + 260, 440);
        volcanoShape[7] = new Vector2(x + VolcanoHalfWidth, GroundY);
        DrawColoredPolygon(volcanoShape, new Color(VolcanoRock, a));
        volcanoShade[0] = new Vector2(x + 10, VolcanoPeakY);
        volcanoShade[1] = new Vector2(x + 66, VolcanoPeakY);
        volcanoShade[2] = new Vector2(x + 130, 300);
        volcanoShade[3] = new Vector2(x + 260, 440);
        volcanoShade[4] = new Vector2(x + VolcanoHalfWidth, GroundY);
        volcanoShade[5] = new Vector2(x + 60, GroundY);
        DrawColoredPolygon(volcanoShade, new Color(0, 0, 0, 0.25f * a)); // its shady side

        // Glowing lava streaks running down from the crater
        for (int s = 0; s < 3; s++)
        {
            float side = s == 0 ? -1 : s == 1 ? 1 : -0.4f;
            float reach = s == 2 ? 0.45f : 1f;
            for (int p = 0; p < streak.Length; p++)
            {
                float f = p / (float)(streak.Length - 1) * reach;
                streak[p] = new Vector2(x + side * (12 + f * 150) + Mathf.Sin(p * 2.1f + s) * 8, VolcanoPeakY + 4 + f * 180);
            }
            float glow = 0.55f + 0.45f * Mathf.Sin(worldTime * 1.7f + s * 2);
            DrawPolyline(streak, new Color(1f, 0.45f, 0.1f, 0.25f * glow * a), 14);
            DrawPolyline(streak, new Color(1f, 0.6f, 0.15f, glow * a), s == 0 ? 5 : 3.5f);
        }
        Shapes.Ellipse(this, crater + new Vector2(0, 2), new Vector2(64, 10), new Color(1f, 0.55f, 0.15f, (0.8f + 0.2f * pulse) * a));
        Shapes.Ellipse(this, crater + new Vector2(0, 2), new Vector2(40, 5), new Color(LavaShineColor, a));

        // Smoke puffs rising out of the crater, drifting in the wind
        for (int i = 0; i < 7; i++)
        {
            float age = Mathf.PosMod(worldTime * 0.22f + i / 7f, 1);
            var at = crater + new Vector2(Mathf.Sin(worldTime + i * 1.3f) * 20 + age * 120, -20 - age * 250);
            DrawCircle(at, 20 + age * 48, new Color(Smoke, (1 - age) * 0.6f * a));
        }
        drawnWorldThings.Add("volcano");
        drawnWorldThings.Add("smoke");

        if (!erupting) return;
        // ERUPTING! A fountain of glowing lava blobs flying up out of the crater and falling back down its sides
        if (eruptTime < 0.7f)
        {
            float rise = Mathf.Min(1, eruptTime / 0.25f) * (1 - eruptTime / 0.7f);
            DrawRect(new Rect2(x - 18, VolcanoPeakY - 220 * rise, 36, 220 * rise), new Color(1f, 0.7f, 0.2f, 0.8f * a));
        }
        foreach (var (vx, vy, delay) in EruptBlobs)
        {
            float t = eruptTime - delay;
            if (t <= 0 || t > 1.7f) continue;
            var at = crater + new Vector2(vx * t, vy * t + 480 * t * t);
            if (at.Y > GroundY) continue;
            DrawCircle(at, 11, new Color(1f, 0.45f, 0.1f, 0.35f * a));
            DrawCircle(at, 6, new Color(1f, 0.8f, 0.3f, a));
        }
        drawnWorldThings.Add("eruption");
    }

    /// <summary>
    /// The LAVA RIVER where the ground should be: a hot glow over the far bank, the glowing lava with its wavy shiny top,
    /// dark crusts drifting along, and bubbles plopping. (In a cave, the cave is drawn instead.)
    /// </summary>
    void DrawVolcanoGround()
    {
        if (InCave)
        {
            DrawCave();
            return;
        }
        float a = MixAmount(w => w.Scenery == Scenery.Volcano);
        if (a < 0.02f) return;

        DrawGradient(LavaSurfaceY - 80, LavaSurfaceY, new Color(1f, 0.45f, 0.1f, 0), new Color(1f, 0.45f, 0.1f, 0.4f * a)); // the heat glow
        DrawGradient(LavaSurfaceY, ScreenHeight + 40, new Color(LavaBright, a), new Color(LavaDeep, a));

        // Dark crusts drifting slowly along (the river flows!), with bright cracks in them
        float flow = groundScroll + worldTime * 25;
        for (int i = 0; i < 10; i++)
        {
            float x = Scrolled(i * 150f + (i * 37 % 60), flow, 1500, 90);
            float y = LavaSurfaceY + 24 + (i * 37 % 50);
            float w = 40 + (i * 13 % 25);
            Shapes.Ellipse(this, new Vector2(x, y), new Vector2(w, 7), new Color(Crust, 0.75f * a));
            DrawLine(new Vector2(x - w * 0.5f, y), new Vector2(x + w * 0.45f, y + 2), new Color(LavaShineColor, 0.9f * a), 2);
        }

        // Bubbles growing... and POP!
        for (int i = 0; i < 9; i++)
        {
            float cycle = 1.6f + (i % 3) * 0.4f;
            float phase = Mathf.PosMod(worldTime + i * 0.37f, cycle) / cycle;
            float x = Scrolled(i * 170f + 40, groundScroll, 1530, 40);
            float y = LavaSurfaceY + 16 + (i * 23 % 40);
            if (phase < 0.75f)
            {
                float r = 2 + phase * 10;
                DrawCircle(new Vector2(x, y), r, new Color(1f, 0.82f, 0.32f, a));
                DrawCircle(new Vector2(x - r * 0.35f, y - r * 0.35f), r * 0.3f, new Color(1, 1, 1, 0.7f * a));
            }
            else
            {
                float pop = (phase - 0.75f) / 0.25f;
                DrawArc(new Vector2(x, y), 9 + pop * 14, 0, Mathf.Tau, 16, new Color(LavaShineColor, (1 - pop) * a), 2);
            }
        }

        // The wavy, shiny top of the lava
        for (int i = 0; i < lavaWave.Length; i++)
        {
            float x = -40 + i * 40;
            lavaWave[i] = new Vector2(x, LavaSurfaceY + 3 * Mathf.Sin((x + groundScroll) * 0.018f + worldTime * 2.2f));
        }
        DrawPolyline(lavaWave, new Color(1f, 0.7f, 0.2f, 0.4f * a), 10);
        DrawPolyline(lavaWave, new Color(LavaShineColor, a), 4);
        drawnWorldThings.Add("lava river");
    }

    /// <summary>Glowing embers falling everywhere (on VOLCANO ISLAND's surface).</summary>
    void DrawEmbers()
    {
        float a = MixAmount(w => w.Weather == Weather.Embers);
        if (a < 0.02f || InCave) return;
        for (int i = 0; i < embers.Length; i++)
        {
            var ember = embers[i];
            float flicker = 0.6f + 0.4f * Mathf.Sin(worldTime * 7 + ember.Flicker);
            var color = i % 3 == 0 ? new Color(1f, 0.85f, 0.35f) : new Color(1f, 0.5f, 0.12f);
            DrawCircle(ember.At, ember.Size * 2.4f, new Color(color, 0.22f * flicker * a));
            DrawCircle(ember.At, ember.Size, new Color(color, flicker * a));
        }
        drawnWorldThings.Add("embers");
    }

    // ---------- The LAVA CAVES ----------

    /// <summary>
    /// A LAVA CAVE, over everything else: dark rock walls, lavafalls, glowing crystals, drips, little bats far away, the
    /// ceiling full of stalactites, and the solid rock floor with glowing cracks. HARD is darker and redder.
    /// </summary>
    void DrawCave()
    {
        int h = Math.Clamp(caveHardness, 1, 3) - 1;
        DrawGradient(-40, ScreenHeight + 40, CaveWallTop[h], CaveWallBottom[h]);

        // Big boulders in the back wall (they slide by slowly: they're far away)
        var boulder = CaveWallBottom[h].Lightened(0.06f);
        for (int i = 0; i < 8; i++)
        {
            float x = Scrolled(i * 230f, farHillScroll, 1840, 160);
            DrawCircle(new Vector2(x, 250 + (i * 71 % 230)), 90 + (i * 37 % 60), new Color(boulder, 0.5f));
        }

        // Lavafalls pouring down the walls into glowing pools
        for (int i = 0; i < Lavafalls[h]; i++)
        {
            float x = Scrolled(i * (1680f / Lavafalls[h]) + 200, nearHillScroll * 0.8f, 1680, 90);
            DrawCircle(new Vector2(x, 560), 130, new Color(1f, 0.4f, 0.1f, 0.1f));
            DrawRect(new Rect2(x - 17, CaveCeilingY - 10, 34, 580 - CaveCeilingY), new Color(1f, 0.48f, 0.1f, 0.9f));
            DrawRect(new Rect2(x - 6, CaveCeilingY - 10, 12, 580 - CaveCeilingY), new Color(1f, 0.82f, 0.3f));
            for (int k = 0; k < 6; k++)
            {
                float y = CaveCeilingY + Mathf.PosMod(worldTime * 260 + k * 80, 460);
                DrawRect(new Rect2(x - 7, y, 14, 26), new Color(LavaShineColor, 0.8f));
            }
            Shapes.Ellipse(this, new Vector2(x, 578), new Vector2(70, 12), new Color(1f, 0.55f, 0.12f));
        }
        drawnWorldThings.Add("lavafalls");

        // Glowing crystals in the walls (purple and cyan), sparkling
        for (int i = 0; i < 10; i++)
        {
            float x = Scrolled(i * 190f + 60, nearHillScroll, 1900, 60);
            float y = i % 2 == 0 ? CaveCeilingY + 70 + (i * 29 % 60) : 470 + (i * 31 % 70);
            var color = CrystalGlow[i % 2];
            float pulse = 0.6f + 0.4f * Mathf.Sin(worldTime * 2.5f + i);
            DrawCircle(new Vector2(x, y - 14), 40, new Color(color, 0.16f * pulse));
            for (int c = 0; c < 3; c++) DrawGem(new Vector2(x + (c - 1) * 12, y), CrystalTilt[c], CrystalLength[c], color);
            if (Mathf.Sin(worldTime * 3 + i * 2) > 0.7f) // a sparkle
            {
                var tip = new Vector2(x, y - CrystalLength[1] - 4);
                DrawLine(tip + new Vector2(-6, 0), tip + new Vector2(6, 0), Colors.White, 2);
                DrawLine(tip + new Vector2(0, -6), tip + new Vector2(0, 6), Colors.White, 2);
            }
        }
        drawnWorldThings.Add("crystals");

        // Little bats flying far away
        var batColor = new Color(0.05f, 0.02f, 0.04f);
        for (int k = 0; k < 4; k++)
        {
            float x = ScreenWidth + 60 - Mathf.PosMod(worldTime * 90 + k * 380, 1500);
            float y = 190 + k * 38 + Mathf.Sin(worldTime * 3 + k) * 12;
            float flap = Mathf.Sin(worldTime * 16 + k) * 6;
            DrawLine(new Vector2(x, y), new Vector2(x - 12, y - flap), batColor, 3);
            DrawLine(new Vector2(x, y), new Vector2(x + 12, y - flap), batColor, 3);
            DrawCircle(new Vector2(x, y), 3.5f, batColor);
        }
        drawnWorldThings.Add("bats");

        // The ceiling, with stalactites hanging down (and glowing drips falling from them)
        var ceiling = CaveCeiling[h];
        DrawRect(new Rect2(-40, -40, ScreenWidth + 80, CaveCeilingY + 40), ceiling);
        for (int i = 0; i < 26; i++)
        {
            float x = Scrolled(i * 52f, groundScroll * 0.9f, 1352, 40);
            float length = 18 + (i * 29 % 46);
            triangle[0] = new Vector2(x - 14, CaveCeilingY - 2);
            triangle[1] = new Vector2(x + 14, CaveCeilingY - 2);
            triangle[2] = new Vector2(x + (i % 3 - 1) * 3, CaveCeilingY + length);
            DrawColoredPolygon(triangle, ceiling);
        }
        for (int k = 0; k < 5; k++)
        {
            float x = Scrolled(k * 270f + 120, groundScroll * 0.9f, 1350, 40);
            float y = CaveCeilingY + 30 + Mathf.PosMod(worldTime * 200 + k * 97, 420);
            DrawCircle(new Vector2(x, y), 6, new Color(1f, 0.5f, 0.1f, 0.25f));
            DrawCircle(new Vector2(x, y), 3, new Color(1f, 0.7f, 0.2f));
        }
        drawnWorldThings.Add("stalactites");

        // The solid rock floor, with glowing cracks
        if (h == 2) DrawGradient(CaveFloorY - 150, CaveFloorY, new Color(1f, 0.15f, 0.05f, 0), new Color(1f, 0.15f, 0.05f, 0.22f)); // a red glow: HARD!
        DrawRect(new Rect2(-40, CaveFloorY, ScreenWidth + 80, ScreenHeight - CaveFloorY + 40), new Color(0.2f, 0.13f, 0.13f));
        DrawRect(new Rect2(-40, CaveFloorY, ScreenWidth + 80, 8), new Color(0.42f, 0.3f, 0.27f));
        float glow = 0.55f + 0.25f * Mathf.Sin(worldTime * 3);
        for (int i = 0; i < 12; i++)
        {
            float x = Scrolled(i * 120f + (i * 41 % 50), groundScroll, 1440, 60);
            crackLine[0] = new Vector2(x, CaveFloorY + 12);
            crackLine[1] = new Vector2(x + 14, CaveFloorY + 30);
            crackLine[2] = new Vector2(x + 6, CaveFloorY + 52);
            crackLine[3] = new Vector2(x + 22, CaveFloorY + 84);
            DrawPolyline(crackLine, new Color(1f, 0.45f, 0.1f, glow), 3);
        }
        drawnWorldThings.Add("cave");
        drawnWorldThings.Add("cave floor");
        if (h == 2) drawnWorldThings.Add("hard cave");
    }

    /// <summary>One crystal: a long six-sided gem standing at "bottom", tilted, with a bright stripe down its middle.</summary>
    void DrawGem(Vector2 bottom, float tilt, float length, Color color)
    {
        var up = Vector2.Up.Rotated(tilt);
        var side = new Vector2(-up.Y, up.X);
        float w = length * 0.22f;
        gem[0] = bottom;
        gem[1] = bottom + side * w + up * length * 0.2f;
        gem[2] = bottom + side * w + up * length * 0.8f;
        gem[3] = bottom + up * length;
        gem[4] = bottom - side * w + up * length * 0.8f;
        gem[5] = bottom - side * w + up * length * 0.2f;
        DrawColoredPolygon(gem, color);
        DrawLine(bottom + up * 3, bottom + up * (length - 3), color.Lightened(0.55f), 2);
    }
}
