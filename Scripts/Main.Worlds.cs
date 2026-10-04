using Godot;

/// <summary>
/// THE WORLD TOUR! (This is more of Main: "partial" means one class spread over several files.)
///
/// Every time Bolt-E beats Big Rusty, "YOU BEAT BIG RUSTY!" shows first, and a moment later he rides into a new world:
/// the sky, the sun, the clouds, the hills and the ground all change color smoothly over 3 seconds.
/// Sunny Hills, Candy Land, Night City, Snowy Peaks, The Moon... then all the way around to Sunny Hills again!
/// The worlds themselves (their colors, gravity, music speed and Big Rusty's outfits) are in Worlds.cs.
/// This file changes the worlds and draws the special things in them: stars, the moon, the Earth, skyscrapers,
/// mountains, lollipops, pine trees, sprinkles, road dashes, craters, falling snow, and a glow around Bolt-E at night.
/// </summary>
public partial class Main
{
    // ---- The World Tour (try changing these!) ----
    const float WorldChangeSeconds = 3f;   // how long the colors take to change into the new world
    const float NewWorldDelay = 2.2f;      // seconds after beating Big Rusty before the new world comes ("YOU BEAT BIG RUSTY!" shows first)

    int worldNumber;                       // 0 = Sunny Hills, 1 = Candy Land, 2 = Night City... (it keeps counting after the Moon)
    int worldsReached = 1;                 // how many worlds this game has ridden through (it's saved with the score)
    World fromWorld = Worlds.All[0];       // the world we're changing from...
    World toWorld = Worlds.All[0];         // ...and the world we're changing to
    float worldBlend = 1;                  // 0 = it still looks like fromWorld, 1 = it looks just like toWorld
    float worldTime;                       // seconds, for twinkling stars and wobbly snowflakes

    /// <summary>The names of the world things drawn in the last frame, like "stars" or "city" (the self-test checks these).</summary>
    readonly HashSet<string> drawnWorldThings = new();
    /// <summary>The colors of the sky, hills and ground drawn in the last frame, like "grass" (the self-test checks these).</summary>
    readonly Dictionary<string, Color> drawnColors = new();

    /// <summary>The world we're in now. (After the Moon it goes around again: world 5 is Sunny Hills.)</summary>
    World CurrentWorld => Worlds.All[worldNumber % Worlds.All.Length];

    /// <summary>A color part-way between the old world's color and the new world's color (for the smooth change).</summary>
    Color Mix(Func<World, Color> pick) => pick(fromWorld).Lerp(pick(toWorld), worldBlend);

    /// <summary>How much the world has something: 1 = yes, 0 = no, and in between while the worlds change.</summary>
    float MixAmount(Func<World, bool> has) => Mathf.Lerp(has(fromWorld) ? 1f : 0f, has(toWorld) ? 1f : 0f, worldBlend);

    // ---------- Changing worlds ----------

    /// <summary>A fresh start (a new game, or the title screen): back in Sunny Hills, with normal gravity.</summary>
    void ResetWorlds()
    {
        worldNumber = 0;
        worldsReached = 1;
        fromWorld = toWorld = Worlds.All[0];
        worldBlend = 1;
        robot.SetGravityScale(1);
    }

    /// <summary>
    /// After beating Big Rusty: the next world comes a little later, so "YOU BEAT BIG RUSTY!" shows first.
    /// It doesn't come after a game over, and Later skips it if a new game started in the meantime.
    /// </summary>
    void NextWorldSoon()
    {
        Later(NewWorldDelay, () =>
        {
            if (state is GameState.Playing or GameState.Rebuilding) ChangeWorld(worldNumber + 1);
        });
    }

    /// <summary>
    /// Welcome to world number n! A banner and a sparkly jingle, and the colors start changing (over 3 seconds).
    /// Gravity and the music speed change right away. (In the middle of a jump, the jump still goes just as high:
    /// see Robot.SetGravityScale.)
    /// </summary>
    void ChangeWorld(int n)
    {
        fromWorld = toWorld; // (if the last change hasn't finished yet, it jumps to its end first)
        worldNumber = n;
        toWorld = CurrentWorld;
        worldBlend = 0;
        worldsReached = Math.Max(worldsReached, n + 1);

        var gold = new Color(1f, 0.85f, 0.2f);
        if (n % Worlds.All.Length == 0) hud.ShowBanner("YOU WENT ALL THE WAY AROUND!", gold);
        else hud.ShowBanner($"WELCOME TO {toWorld.Name}!", gold);
        if (toWorld.Gravity < 1) hud.ShowHint("Moon jumps are FLOATY!"); // (only the Moon has low gravity)

        sounds.Play(Sfx.NewWorld);
        robot.SetGravityScale(toWorld.Gravity);
        sounds.SetMusicSpeed(toWorld.MusicSpeed);
    }

    /// <summary>Every frame, on every screen: the colors keep changing into the new world, and the snow keeps falling.</summary>
    void UpdateWorlds(float dt)
    {
        worldBlend = Mathf.MoveToward(worldBlend, 1, dt / WorldChangeSeconds);
        worldTime += dt;

        foreach (var flake in snowflakes)
        {
            flake.At += new Vector2(-(flake.Speed * 0.3f + 25), flake.Speed) * dt; // falling, and drifting left in the wind
            if (flake.At.Y > ScreenHeight + 10) flake.At.Y -= ScreenHeight + 20;  // back up to the top
            if (flake.At.X < -10) flake.At.X += ScreenWidth + 20;                  // back to the right side
        }
    }

    /// <summary>
    /// How much farther apart obstacles come: on the Moon, floaty jumps take longer, so the gaps stretch to match
    /// (1 everywhere else). (Gravity never counts as less than 0.1 here, so a Gravity of 0 can't stop things coming.)
    /// </summary>
    float GapStretch => 1 / Mathf.Sqrt(Mathf.Max(Robot.LeastGravity, robot.GravityScale));

    // ---------- The stars and the snowflakes (made once, always the same "random" ones) ----------

    /// <summary>One star in the night sky: where it is, and how big.</summary>
    readonly record struct SkyStar(Vector2 At, float Size);

    static readonly SkyStar[] NightStars = MakeStars();

    static SkyStar[] MakeStars()
    {
        var random = new Random(42);
        var stars = new SkyStar[45];
        for (int i = 0; i < stars.Length; i++)
            stars[i] = new SkyStar(new Vector2(random.NextSingle() * ScreenWidth, 10 + random.NextSingle() * 410),
                                   1.5f + random.NextSingle() * 1.5f); // 1.5 to 3 pixels
        return stars;
    }

    class Snowflake
    {
        public Vector2 At;
        public float Speed;  // how fast it falls (pixels per second)
        public float Size;
    }

    readonly Snowflake[] snowflakes = MakeSnowflakes();

    static Snowflake[] MakeSnowflakes()
    {
        var random = new Random(7);
        var flakes = new Snowflake[70];
        for (int i = 0; i < flakes.Length; i++)
        {
            flakes[i] = new Snowflake
            {
                At = new Vector2(random.NextSingle() * ScreenWidth, random.NextSingle() * ScreenHeight),
                Speed = 40 + random.NextSingle() * 50, // 40 to 90
                Size = 2 + random.NextSingle() * 2,    // 2 to 4
            };
        }
        return flakes;
    }

    // ---------- Drawing the worlds (Main._Draw calls these) ----------

    static readonly Color SunGlow = new(1f, 0.95f, 0.6f, 0.3f);
    static readonly Color WindowLight = new(1f, 0.9f, 0.45f);
    static readonly Color[] LollipopColors = { new(1f, 0.45f, 0.7f), new(1f, 0.85f, 0.25f), new(0.45f, 0.7f, 1f) }; // pink, yellow, blue
    static readonly Color PineGreen = new(0.2f, 0.45f, 0.35f);
    static readonly Color TrunkBrown = new(0.45f, 0.3f, 0.18f);

    // Corner points, reused every frame (DrawColoredPolygon copies them, so the game doesn't make new lists all the time)
    static readonly Vector2[] triangle = new Vector2[3];
    static readonly Vector2[] snowCap = new Vector2[6];
    static readonly Vector2[] craterRim = new Vector2[9];

    /// <summary>Twinkling stars in the night sky (Night City and the Moon).</summary>
    void DrawStars()
    {
        float stars = MixAmount(w => w.Weather == Weather.Stars);
        if (stars < 0.02f) return;
        for (int i = 0; i < NightStars.Length; i++)
        {
            float twinkle = 0.5f + 0.5f * Mathf.Sin(3 * worldTime + i);
            DrawCircle(NightStars[i].At, NightStars[i].Size, new Color(1, 1, 1, twinkle * stars));
        }
        drawnWorldThings.Add("stars");
    }

    /// <summary>A world's sun, moon or Earth, high up on the right. "alpha" fades it in or out while the worlds change.</summary>
    void DrawSkyThing(World world, float alpha)
    {
        if (alpha < 0.02f) return;
        var at = new Vector2(1080, 120);
        var color = new Color(world.ThingColor, alpha);
        switch (world.Thing)
        {
            case SkyThing.Sun:
                DrawCircle(at, 80, new Color(SunGlow, SunGlow.A * alpha)); // its warm glow
                DrawCircle(at, 55, color);
                drawnWorldThings.Add("sun");
                break;

            case SkyThing.Moon:
                DrawCircle(at, 75, new Color(world.ThingColor, 0.12f * alpha)); // a soft glow
                DrawCircle(at, 50, color);
                var crater = new Color(world.ThingColor.Darkened(0.15f), alpha);
                DrawCircle(at + new Vector2(-16, -12), 10, crater);
                DrawCircle(at + new Vector2(17, 6), 7, crater);
                DrawCircle(at + new Vector2(-4, 22), 5, crater);
                drawnWorldThings.Add("moon");
                break;

            case SkyThing.Earth:
                DrawCircle(at, 62, new Color(0.5f, 0.75f, 1f, 0.15f * alpha)); // the air around the Earth glows a little
                DrawCircle(at, 52, color);                                      // the blue sea
                var land = new Color(0.35f, 0.75f, 0.35f, alpha);
                Shapes.Ellipse(this, at + new Vector2(-16, -14), new Vector2(17, 10), land, -0.5f);
                Shapes.Ellipse(this, at + new Vector2(18, 8), new Vector2(11, 17), land, 0.3f);
                Shapes.Ellipse(this, at + new Vector2(-10, 27), new Vector2(14, 6), land);
                DrawArc(at, 38, Mathf.Pi * 1.05f, Mathf.Pi * 1.55f, 12, new Color(1, 1, 1, 0.85f * alpha), 6, true); // a white cloud
                drawnWorldThings.Add("earth");
                break;
        }
    }

    /// <summary>Night City: 8 skyscrapers (the same color as the far hills) with glowing windows.</summary>
    void DrawCity(Color wallColor)
    {
        float city = MixAmount(w => w.Scenery == Scenery.City);
        if (city < 0.02f) return;
        var wall = new Color(wallColor, city);
        var lit = new Color(WindowLight, city);
        var dark = new Color(wallColor.Darkened(0.3f), city);
        for (int i = 0; i < 8; i++)
        {
            float x = Scrolled(i * 220f, farHillScroll, 1760, 200);
            float width = 120 + i * 37 % 40, height = 160 + i * 71 % 140;
            float left = x - width / 2, top = GroundY - height;
            DrawRect(new Rect2(left, top, width, height), wall);

            // Windows 10 x 14, every 26 pixels, in the middle of the building. Most of them are lit up.
            int columns = (int)((width - 14) / 26);
            float firstX = left + (width - (columns * 26 - 16)) / 2;
            for (int row = 0; top + 14 + row * 26 + 14 <= GroundY - 30; row++)
                for (int col = 0; col < columns; col++)
                    DrawRect(new Rect2(firstX + col * 26, top + 14 + row * 26, 10, 14), (i * 7 + row * 3 + col) % 3 != 0 ? lit : dark);
        }
        drawnWorldThings.Add("city");
    }

    /// <summary>Snowy Peaks: 7 big mountains (the same color as the far hills) with snow on top.</summary>
    void DrawMountains(Color rockColor)
    {
        float snowy = MixAmount(w => w.Scenery == Scenery.Mountains);
        if (snowy < 0.02f) return;
        var rock = new Color(rockColor, snowy);
        var snow = new Color(1, 1, 1, snowy);
        const float halfBase = 180, height = 220; // 360 pixels wide at the bottom, the peak 220 above the ground
        for (int i = 0; i < 7; i++)
        {
            float x = Scrolled(i * 260f, farHillScroll, 1820, 260);
            float peakY = GroundY - height;
            triangle[0] = new Vector2(x, peakY);
            triangle[1] = new Vector2(x + halfBase, GroundY);
            triangle[2] = new Vector2(x - halfBase, GroundY);
            DrawColoredPolygon(triangle, rock);

            // The snow cap: the top 30% of the mountain, with a wiggly bottom edge
            float capX = halfBase * 0.3f, capY = peakY + height * 0.3f;
            snowCap[0] = new Vector2(x, peakY);
            snowCap[1] = new Vector2(x + capX, capY);
            snowCap[2] = new Vector2(x + capX / 2, capY - 10);
            snowCap[3] = new Vector2(x, capY + 2);
            snowCap[4] = new Vector2(x - capX / 2, capY - 10);
            snowCap[5] = new Vector2(x - capX, capY);
            DrawColoredPolygon(snowCap, snow);
        }
        drawnWorldThings.Add("mountains");
    }

    /// <summary>Candy Land: 6 giant lollipops on the hills: a white stick and a round candy with a white swirl.</summary>
    void DrawLollipops()
    {
        float candy = MixAmount(w => w.Scenery == Scenery.Lollipops);
        if (candy < 0.02f) return;
        var white = new Color(1, 1, 1, candy);
        for (int i = 0; i < 6; i++)
        {
            float x = Scrolled(i * 300f + 60, nearHillScroll, 1800, 160);
            DrawRect(new Rect2(x - 3, GroundY - 110, 6, 110), white); // the stick
            var middle = new Vector2(x, GroundY - 130);
            DrawCircle(middle, 26, new Color(LollipopColors[i % 3], candy));
            DrawArc(middle, 17, 0, Mathf.Pi * 1.6f, 20, white, 4, true);          // the swirl
            DrawArc(middle, 8, Mathf.Pi, Mathf.Pi * 2.5f, 12, white, 3.5f, true);
        }
        drawnWorldThings.Add("lollipops");
    }

    /// <summary>Snowy Peaks: 7 pine trees: a brown trunk and 3 dark green triangles, each with a snowy white tip.</summary>
    void DrawPineTrees()
    {
        float snowy = MixAmount(w => w.Scenery == Scenery.Mountains);
        if (snowy < 0.02f) return;
        var trunk = new Color(TrunkBrown, snowy);
        var green = new Color(PineGreen, snowy);
        var snow = new Color(1, 1, 1, snowy);
        for (int i = 0; i < 7; i++)
        {
            float x = Scrolled(i * 240f + 80, nearHillScroll, 1680, 120);
            DrawRect(new Rect2(x - 6, GroundY - 34, 12, 34), trunk);

            // From the top layer down, so each lower layer (and its snowy tip) is in front of the one above it
            for (int layer = 2; layer >= 0; layer--)
            {
                float bottom = GroundY - 26 - layer * 34;  // each layer sits higher...
                float half = 45 - layer * 9;               // ...and is a little narrower
                const float tall = 64;
                triangle[0] = new Vector2(x, bottom - tall);
                triangle[1] = new Vector2(x + half, bottom);
                triangle[2] = new Vector2(x - half, bottom);
                DrawColoredPolygon(triangle, green);

                triangle[1] = new Vector2(x + half * 0.3f, bottom - tall * 0.7f); // the white tip: the top 30%
                triangle[2] = new Vector2(x - half * 0.3f, bottom - tall * 0.7f);
                DrawColoredPolygon(triangle, snow);
            }
        }
        drawnWorldThings.Add("pines");
    }

    /// <summary>
    /// Little things on the ground: pebbles, rainbow sprinkles on the chocolate (Candy Land), yellow dashes on the road
    /// (Night City), or craters (the Moon). They fade in and out while the worlds change.
    /// </summary>
    void DrawGroundDots()
    {
        var pebbles = Mix(w => w.Pebbles);

        float plain = MixAmount(w => w.Scenery is Scenery.None or Scenery.Mountains);
        if (plain > 0.02f)
        {
            for (int i = 0; i < 18; i++)
            {
                float x = Scrolled(i * 80f, groundScroll, 80f * 18, 20);
                DrawCircle(new Vector2(x + 40, GroundY + 60 + (i % 3) * 18), 5, new Color(pebbles, pebbles.A * plain));
            }
            drawnWorldThings.Add("pebbles");
        }

        float candy = MixAmount(w => w.Scenery == Scenery.Lollipops);
        if (candy > 0.02f)
        {
            for (int i = 0; i < 36; i++)
            {
                float x = Scrolled(i * 40f + 15, groundScroll, 40f * 36, 20);
                var color = i % 4 == 3 ? pebbles : LollipopColors[i % 4]; // pink, yellow, blue, and the world's pebble color
                DrawCircle(new Vector2(x, GroundY + 40 + (i * 37 % 5) * 15), 4, new Color(color, candy));
            }
            drawnWorldThings.Add("sprinkles");
        }

        float city = MixAmount(w => w.Scenery == Scenery.City);
        if (city > 0.02f)
        {
            for (int i = 0; i < 14; i++)
            {
                float x = Scrolled(i * 110f, groundScroll, 110f * 14, 60);
                DrawRect(new Rect2(x - 24, GroundY + 53, 48, 6), new Color(pebbles, city)); // 48 x 6, at GroundY + 56
            }
            drawnWorldThings.Add("road dashes");
        }

        float moon = MixAmount(w => w.Scenery == Scenery.Craters);
        if (moon > 0.02f)
        {
            var rim = Mix(w => w.Grass);
            for (int i = 0; i < 9; i++)
            {
                float x = Scrolled(i * 170f + 50, groundScroll, 170f * 9, 40);
                var at = new Vector2(x, GroundY + 52 + (i % 3) * 20);
                Shapes.Ellipse(this, at, new Vector2(22, 6), new Color(pebbles, moon)); // a dark crater...

                // ...with a light rim along its front edge
                for (int p = 0; p < craterRim.Length; p++)
                {
                    float angle = Mathf.Pi * (0.1f + 0.8f * p / (craterRim.Length - 1));
                    craterRim[p] = at + new Vector2(Mathf.Cos(angle) * 22, Mathf.Sin(angle) * 6 + 1);
                }
                DrawPolyline(craterRim, new Color(rim, moon), 2.5f, true);
            }
            drawnWorldThings.Add("craters");
        }
    }

    /// <summary>Snowy Peaks: snowflakes falling and wobbling from side to side.</summary>
    void DrawSnow()
    {
        float snow = MixAmount(w => w.Weather == Weather.Snow);
        if (snow < 0.02f) return;
        var white = new Color(1, 1, 1, 0.85f * snow);
        for (int i = 0; i < snowflakes.Length; i++)
        {
            float wobble = Mathf.Sin(worldTime * 2 + i) * 6;
            DrawCircle(snowflakes[i].At + new Vector2(wobble, 0), snowflakes[i].Size, white);
        }
        drawnWorldThings.Add("snow");
    }

    /// <summary>In the dark worlds (Night City and the Moon), a soft glow around Bolt-E keeps him easy to see.</summary>
    void DrawNightGlow()
    {
        float dark = MixAmount(w => w.Dark);
        if (dark < 0.02f) return;
        DrawCircle(robot.Position + new Vector2(0, -60 * robot.Size), 95 * robot.Size, new Color(1, 1, 1, 0.12f * dark));
        drawnWorldThings.Add("night glow");
    }
}
