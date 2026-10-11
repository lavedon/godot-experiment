using Godot;

/// <summary>What hangs in the sky of a world.</summary>
public enum SkyThing { Sun, Moon, Earth, RingedPlanet }

/// <summary>The extra things in a world's background (on the hills and on the ground).</summary>
public enum Scenery
{
    None,       // just the hills
    Lollipops,  // giant lollipops, and rainbow sprinkles on the ground
    City,       // skyscrapers with glowing windows, and a road with yellow dashes
    Mountains,  // snowy mountains and pine trees
    Craters,    // a bumpy grey ground full of craters
    Station,    // a space-station corridor with a floor AND a ceiling (see StationScenery.cs)
    Volcano,    // a smoking volcano, and a river of glowing LAVA where the ground should be (see VolcanoScenery.cs)
}

/// <summary>What twinkles in (or falls from) the sky.</summary>
public enum Weather { None, Stars, Snow, Embers }

/// <summary>What Big Rusty dresses up in, so he fits in with the world.</summary>
public enum BossOutfit { None, PartyHat, Sunglasses, BobbleHat, SpaceHelmet, NeonGoggles, FireHelmet }

/// <summary>
/// One world on the World Tour: its colors (from the top of the sky down to the dirt), what's in the sky and on the hills,
/// the weather, how strong gravity is (1 = normal, less = FLOATY jumps), how fast the music plays,
/// what Big Rusty wears there, and whether it's a dark world (then a soft glow around Bolt-E keeps him easy to see).
/// </summary>
public record World(string Name, Color SkyTop, Color SkyBottom, SkyThing Thing, Color ThingColor, Color Clouds,
                    Color FarHills, Color NearHills, Color Grass, Color Dirt, Color Pebbles,
                    Scenery Scenery, Weather Weather, float Gravity, float MusicSpeed, BossOutfit RustyOutfit, bool Dark);

/// <summary>
/// THE WORLD TOUR! Every time Bolt-E beats Big Rusty, he rides on into the next world. After VOLCANO ISLAND, he's
/// all the way around and it starts again in Sunny Hills. (Main.Worlds.cs changes the worlds and draws them.)
///
/// Make your own world! Every color is (red, green, blue) from 0 to 1, and a 4th number is how see-through it is.
/// Two rules keep Bolt-E easy to see (the self-test checks them): the hills must not be too dark,
/// and a world is "Dark" exactly when the top of its sky is very dark.
/// Keep Gravity between 0.2 (SUPER floaty) and 2 (heavy). (Bolt-E never feels less than 0.1, so 0 can't break the game.)
/// </summary>
public static class Worlds
{
    public static readonly World[] All =
    {
        // 0. SUNNY HILLS: the look Robot Dash always had
        new("SUNNY HILLS",
            SkyTop: new(0.45f, 0.75f, 1f), SkyBottom: new(0.85f, 0.95f, 1f),
            Thing: SkyThing.Sun, ThingColor: new(1f, 0.9f, 0.4f), Clouds: new(1f, 1f, 1f, 0.92f),
            FarHills: new(0.62f, 0.85f, 0.75f), NearHills: new(0.45f, 0.78f, 0.45f),
            Grass: new(0.38f, 0.75f, 0.3f), Dirt: new(0.72f, 0.52f, 0.33f), Pebbles: new(0.6f, 0.42f, 0.27f),
            Scenery: Scenery.None, Weather: Weather.None, Gravity: 1f, MusicSpeed: 1.0f, RustyOutfit: BossOutfit.None, Dark: false),

        // 1. CANDY LAND: a pink sky, giant lollipops, and chocolate ground with pink frosting and rainbow sprinkles
        new("CANDY LAND",
            SkyTop: new(1f, 0.6f, 0.8f), SkyBottom: new(1f, 0.9f, 0.95f),
            Thing: SkyThing.Sun, ThingColor: new(1f, 0.95f, 0.6f), Clouds: new(1f, 0.95f, 0.98f, 0.95f),
            FarHills: new(0.8f, 0.7f, 0.95f), NearHills: new(0.58f, 0.9f, 0.75f),
            Grass: new(1f, 0.55f, 0.75f), Dirt: new(0.48f, 0.3f, 0.2f), Pebbles: new(1f, 1f, 1f), // frosting, chocolate, sprinkles
            Scenery: Scenery.Lollipops, Weather: Weather.None, Gravity: 1f, MusicSpeed: 1.05f, RustyOutfit: BossOutfit.PartyHat, Dark: false),

        // 2. NIGHT CITY: twinkling stars, a moon, skyscrapers with glowing windows, and a road with yellow dashes
        new("NIGHT CITY",
            SkyTop: new(0.07f, 0.08f, 0.22f), SkyBottom: new(0.38f, 0.32f, 0.6f),
            Thing: SkyThing.Moon, ThingColor: new(0.95f, 0.95f, 0.85f), Clouds: new(0.6f, 0.6f, 0.8f, 0.35f),
            FarHills: new(0.4f, 0.4f, 0.62f), NearHills: new(0.42f, 0.46f, 0.68f),
            Grass: new(0.55f, 0.55f, 0.6f), Dirt: new(0.22f, 0.22f, 0.27f), Pebbles: new(1f, 0.85f, 0.2f), // curb, road, road dashes
            Scenery: Scenery.City, Weather: Weather.Stars, Gravity: 1f, MusicSpeed: 0.95f, RustyOutfit: BossOutfit.Sunglasses, Dark: true),

        // 3. SNOWY PEAKS: snowy mountains, pine trees and falling snow
        new("SNOWY PEAKS",
            SkyTop: new(0.5f, 0.72f, 0.95f), SkyBottom: new(0.88f, 0.94f, 1f),
            Thing: SkyThing.Sun, ThingColor: new(1f, 0.97f, 0.8f), Clouds: new(1f, 1f, 1f, 0.92f),
            FarHills: new(0.6f, 0.66f, 0.82f), NearHills: new(0.7f, 0.79f, 0.9f),
            Grass: new(0.96f, 0.98f, 1f), Dirt: new(0.72f, 0.8f, 0.9f), Pebbles: new(0.58f, 0.68f, 0.82f), // snow, icy dirt, icy pebbles
            Scenery: Scenery.Mountains, Weather: Weather.Snow, Gravity: 1f, MusicSpeed: 1.05f, RustyOutfit: BossOutfit.BobbleHat, Dark: false),

        // 4. THE MOON: the Earth in a black starry sky, cratered grey ground, and FLOATY moon jumps
        new("THE MOON",
            SkyTop: new(0.02f, 0.02f, 0.07f), SkyBottom: new(0.12f, 0.1f, 0.22f),
            Thing: SkyThing.Earth, ThingColor: new(0.3f, 0.55f, 1f), Clouds: new(1f, 1f, 1f, 0f), // (no clouds: there's no air on the Moon!)
            FarHills: new(0.42f, 0.42f, 0.48f), NearHills: new(0.56f, 0.56f, 0.62f),
            Grass: new(0.72f, 0.72f, 0.76f), Dirt: new(0.56f, 0.56f, 0.6f), Pebbles: new(0.42f, 0.42f, 0.48f),
            Scenery: Scenery.Craters, Weather: Weather.Stars, Gravity: 0.55f, MusicSpeed: 0.92f, RustyOutfit: BossOutfit.SpaceHelmet, Dark: true),

        // 5. UPSIDE-DOWN STATION: a neon space-station corridor with a FLOOR and a CEILING. Here JUMP flips gravity!
        //    (Its "hills" colors are the corridor's metal walls, and its "grass" is the glowing floor rail: see StationScenery.cs.
        //    Its Gravity is for boss fights: then the ceiling slides open and jumping is normal.)
        new("UPSIDE-DOWN STATION",
            SkyTop: new(0.04f, 0.02f, 0.1f), SkyBottom: new(0.16f, 0.06f, 0.26f),
            Thing: SkyThing.RingedPlanet, ThingColor: new(1f, 0.6f, 0.35f), Clouds: new(1f, 1f, 1f, 0f), // (no clouds in space!)
            FarHills: new(0.4f, 0.38f, 0.56f), NearHills: new(0.5f, 0.48f, 0.66f),
            Grass: new(0.35f, 0.95f, 1f), Dirt: new(0.2f, 0.19f, 0.3f), Pebbles: new(1f, 0.35f, 0.85f), // cyan rail, floor metal, magenta lights
            Scenery: Scenery.Station, Weather: Weather.Stars, Gravity: 1f, MusicSpeed: 1.1f, RustyOutfit: BossOutfit.NeonGoggles, Dark: true),

        // 6. VOLCANO ISLAND: a smoking volcano, glowing embers, and a river of LAVA with floating rocks that SINK!
        //    (Its "grass" and "dirt" are the far bank of the lava river, and its "pebbles" are the lava's glow: see
        //    VolcanoScenery.cs. Some rocks have a HOLE: ride in for a LAVA CAVE! See Main.Volcano.cs and Main.LavaCave.cs.)
        new("VOLCANO ISLAND",
            SkyTop: new(0.2f, 0.04f, 0.05f), SkyBottom: new(0.95f, 0.42f, 0.15f),
            Thing: SkyThing.Sun, ThingColor: new(1f, 0.35f, 0.18f), Clouds: new(0.3f, 0.22f, 0.24f, 0.75f), // a red sun, and smoke clouds
            FarHills: new(0.64f, 0.31f, 0.22f), NearHills: new(0.55f, 0.36f, 0.28f),
            Grass: new(0.36f, 0.2f, 0.17f), Dirt: new(0.3f, 0.14f, 0.1f), Pebbles: new(1f, 0.55f, 0.15f), // the far bank, and the lava's glow
            Scenery: Scenery.Volcano, Weather: Weather.Embers, Gravity: 1f, MusicSpeed: 1.08f, RustyOutfit: BossOutfit.FireHelmet, Dark: true),
    };

    /// <summary>Which world UPSIDE-DOWN STATION is (the 6th one: number 5, counting from 0).</summary>
    public const int StationIndex = 5;

    /// <summary>Which world VOLCANO ISLAND is (the 7th one: number 6, counting from 0).</summary>
    public const int VolcanoIndex = 6;

    /// <summary>How bright a color looks to our eyes (0 = black, 1 = white). Green looks brightest, blue darkest.</summary>
    public static float Brightness(Color color) => 0.299f * color.R + 0.587f * color.G + 0.114f * color.B;

    /// <summary>
    /// The line on the results card about how far Bolt-E got. "reached" counts the worlds he rode through:
    /// 1 = only Sunny Hills, 3 = all the way to Night City, 6 = all the way to UPSIDE-DOWN STATION, 7 = all the way to
    /// VOLCANO ISLAND, 8 or more = all the way around.
    /// </summary>
    public static string WorldLine(int reached)
    {
        if (reached <= 1) return $"World: {All[0].Name}";
        if (reached <= All.Length) return $"You rode all the way to {All[reached - 1].Name}!";
        return "You went all the way around the world!";
    }
}
