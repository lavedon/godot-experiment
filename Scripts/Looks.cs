using Godot;

/// <summary>Hats Bolt-E can wear (Robot.DrawHat draws them).</summary>
public enum Hat { None, Party, Cowboy, Pirate, Propeller, Crown, SpaceHelmet }

/// <summary>What Bolt-E rides on (Robot.DrawHoverboard draws them).</summary>
public enum Board { Classic, Skateboard, HotDog, Rainbow, Cloud }

/// <summary>What floats out behind Bolt-E while he rides (Robot.DrawTrail draws them).</summary>
public enum Trail { None, Sparkles, Hearts, Rainbow }

/// <summary>How a look is won: bolts in the Bolt Bank, Big Rusty wins (all games together), or the farthest world reached.</summary>
public enum UnlockBy { Bolts, BossesBeaten, World }

/// <summary>
/// One complete look for Bolt-E: its name, 7 colors, a hat, a board, a trail, rainbow eyes (or not),
/// and what it takes to win it ("Need": how many bolts, Big Rusty wins, or which world).
/// </summary>
public record Look(string Id, string Name, Color Body, Color Bezel, Color Screen, Color Eyes, Color Headphones, Color Accent,
                   Color BoardColor, Hat Hat, Board Board, Trail Trail, bool RainbowEyes, UnlockBy By, int Need);

/// <summary>
/// ALL OF BOLT-E'S LOOKS! Invent your own look!
///
/// Every color is (red, green, blue) from 0 to 1. The 7 colors of a look are, in order:
///   body (head and legs), bezel (the ring around his face screen), screen, eyes, headphones (and feet),
///   accent (the stripes and little lights), and the board.
/// Two rules keep his face easy to read (the self-test checks them): the eyes must be bright
/// (brightness 0.6 or more), and the screen must be very dark (0.1 or less).
/// A light body (brightness over 0.55) gets a thin dark outline by itself, so he still shows up on snow and candy.
///
/// The looks are in the order the picker shows them. "Need" is how many bolts it takes to win one
/// (try smaller numbers to win them faster!), except for RUSTY JR. (Big Rusty wins) and ASTRONAUT (a world).
/// </summary>
public static class Looks
{
    /// <summary>CLASSIC: the colors Bolt-E always had.</summary>
    public static readonly Look Classic = new("classic", "CLASSIC",
        Body: new(0.23f, 0.24f, 0.3f),        // dark grey head and legs
        Bezel: new(0.66f, 0.68f, 0.75f),      // silver ring around the face screen
        Screen: new(0.04f, 0.05f, 0.07f),     // black face screen
        Eyes: new(0.35f, 0.95f, 0.95f),       // glowing cyan eyes
        Headphones: new(0.4f, 0.35f, 0.7f),   // purple headphones and feet
        Accent: new(0.3f, 0.92f, 0.85f),      // teal stripes and lights
        BoardColor: new(0.2f, 0.21f, 0.26f),  // the hoverboard
        Hat: Hat.None, Board: Board.Classic, Trail: Trail.None, RainbowEyes: false,
        By: UnlockBy.Bolts, Need: 0);

    /// <summary>GOLD BOLT-E: shiny gold all over. (KING BOLT-E wears these colors too.)</summary>
    static readonly Look Gold = Paint("gold", "GOLD BOLT-E", 4000,
        new(0.86f, 0.66f, 0.16f), new(1f, 0.9f, 0.55f), new(0.05f, 0.04f, 0.02f), new(0.35f, 0.95f, 0.95f),
        new(0.72f, 0.48f, 0.1f), new(1f, 0.95f, 0.6f), new(0.9f, 0.72f, 0.22f));

    /// <summary>Every look, in the order the picker shows them.</summary>
    public static readonly Look[] All =
    {
        // 1. CLASSIC (everyone has this one)
        Classic,

        // 2. PARTY BOLT-E: the classic colors and a party hat
        Classic with { Id = "party", Name = "PARTY BOLT-E", Hat = Hat.Party, Need = 100 },

        // 3. BUBBLEGUM: pink, with a trail of hearts
        Paint("bubblegum", "BUBBLEGUM", 250,
              new(0.93f, 0.5f, 0.72f), new(1f, 0.85f, 0.93f), new(0.06f, 0.04f, 0.07f), new(0.4f, 0.95f, 1f),
              new(0.55f, 0.32f, 0.8f), new(1f, 0.95f, 0.55f), new(0.9f, 0.38f, 0.62f))
            with { Trail = Trail.Hearts },

        // 4. FIRE TRUCK: red and shiny silver, with yellow lights
        Paint("firetruck", "FIRE TRUCK", 450,
              new(0.82f, 0.14f, 0.12f), new(0.88f, 0.88f, 0.92f), new(0.04f, 0.05f, 0.07f), new(1f, 0.92f, 0.35f),
              new(1f, 0.78f, 0.1f), new(1f, 0.85f, 0.25f), new(0.25f, 0.25f, 0.28f)),

        // 5. COWBOY: a cowboy hat, and a skateboard instead of a hoverboard. Yee-haw!
        Paint("cowboy", "COWBOY", 700,
              new(0.55f, 0.38f, 0.22f), new(0.86f, 0.76f, 0.55f), new(0.05f, 0.04f, 0.03f), new(1f, 0.85f, 0.4f),
              new(0.72f, 0.2f, 0.15f), new(0.95f, 0.75f, 0.3f), new(0.45f, 0.3f, 0.18f))
            with { Hat = Hat.Cowboy, Board = Board.Skateboard },

        // 6. NINJA: black, with glowing red eyes
        Paint("ninja", "NINJA", 1000,
              new(0.1f, 0.1f, 0.12f), new(0.32f, 0.32f, 0.38f), new(0.02f, 0.02f, 0.03f), new(1f, 0.45f, 0.45f),
              new(0.8f, 0.1f, 0.15f), new(1f, 0.3f, 0.3f), new(0.12f, 0.12f, 0.14f)),

        // 7. OCEAN: deep blue, with a trail of sparkles
        Paint("ocean", "OCEAN", 1400,
              new(0.12f, 0.38f, 0.72f), new(0.72f, 0.9f, 1f), new(0.03f, 0.05f, 0.08f), new(0.6f, 1f, 1f),
              new(0.1f, 0.7f, 0.75f), new(0.5f, 0.95f, 1f), new(0.98f, 0.85f, 0.45f))
            with { Trail = Trail.Sparkles },

        // 8. HOT DOG: the classic colors, a propeller cap, and he rides on a HOT DOG!
        Classic with { Id = "hotdog", Name = "HOT DOG", Hat = Hat.Propeller, Board = Board.HotDog, Need = 1900 },

        // 9. PIRATE: brown and gold, with a pirate hat. Arrr!
        Paint("pirate", "PIRATE", 2500,
              new(0.36f, 0.26f, 0.2f), new(0.85f, 0.72f, 0.35f), new(0.05f, 0.04f, 0.03f), new(1f, 0.9f, 0.4f),
              new(0.75f, 0.12f, 0.12f), new(1f, 0.8f, 0.3f), new(0.4f, 0.28f, 0.18f))
            with { Hat = Hat.Pirate },

        // 10. GALAXY: purple like outer space, with a trail of sparkles
        Paint("galaxy", "GALAXY", 3200,
              new(0.26f, 0.13f, 0.46f), new(0.72f, 0.62f, 0.96f), new(0.03f, 0.02f, 0.06f), new(0.95f, 0.85f, 1f),
              new(0.95f, 0.42f, 0.85f), new(0.6f, 0.9f, 1f), new(0.18f, 0.1f, 0.32f))
            with { Trail = Trail.Sparkles },

        // 11. GOLD BOLT-E
        Gold,

        // 12. KING BOLT-E: gold, a crown, a rainbow board and a rainbow trail
        Gold with { Id = "king", Name = "KING BOLT-E", Hat = Hat.Crown, Board = Board.Rainbow, Trail = Trail.Rainbow, Need = 5500 },

        // 13. RAINBOW: the classic colors, but his eyes change color, on a rainbow board with a rainbow trail
        Classic with { Id = "rainbow", Name = "RAINBOW", RainbowEyes = true, Board = Board.Rainbow, Trail = Trail.Rainbow, Need = 7500 },

        // 14. RUSTY JR.: dressed like Big Rusty! Won by beating Big Rusty 3 times (all games together)
        Paint("rustyjr", "RUSTY JR.", 3,
              new(0.97f, 0.58f, 0.12f), new(0.85f, 0.45f, 0.08f), new(0.05f, 0.04f, 0.03f), new(1f, 0.9f, 0.15f),
              new(0.45f, 0.28f, 0.17f), new(1f, 0.75f, 0.3f), new(0.45f, 0.28f, 0.17f))
            with { By = UnlockBy.BossesBeaten },

        // 15. ASTRONAUT: a space helmet, riding on a cloud. Won by riding all the way to THE MOON (world 5)
        Paint("astronaut", "ASTRONAUT", 5,
              new(0.92f, 0.93f, 0.96f), new(0.6f, 0.62f, 0.7f), new(0.04f, 0.05f, 0.07f), new(0.35f, 0.95f, 0.95f),
              new(0.95f, 0.5f, 0.15f), new(0.3f, 0.6f, 1f), new(0.85f, 0.86f, 0.9f))
            with { Hat = Hat.SpaceHelmet, Board = Board.Cloud, By = UnlockBy.World },
    };

    /// <summary>
    /// A new look with its own 7 colors (body, bezel, screen, eyes, headphones, accent, board), won with "need" bolts.
    /// It starts with no hat, the classic board and no trail ("with { ... }" adds those).
    /// </summary>
    static Look Paint(string id, string name, int need, Color body, Color bezel, Color screen, Color eyes,
                      Color headphones, Color accent, Color board) =>
        Classic with
        {
            Id = id, Name = name, Need = need,
            Body = body, Bezel = bezel, Screen = screen, Eyes = eyes, Headphones = headphones, Accent = accent, BoardColor = board,
        };

    /// <summary>Dresses Bolt-E in a look: all 7 colors, the hat, the board, the trail and the eyes.</summary>
    public static void Apply(Robot robot, Look look)
    {
        robot.BodyColor = look.Body;
        robot.BezelColor = look.Bezel;
        robot.ScreenColor = look.Screen;
        robot.EyeColor = look.Eyes;
        robot.HeadphoneColor = look.Headphones;
        robot.AccentColor = look.Accent;
        robot.BoardColor = look.BoardColor;
        robot.Hat = look.Hat;
        robot.BoardStyle = look.Board;
        robot.TrailStyle = look.Trail;
        robot.RainbowEyes = look.RainbowEyes;
    }

    /// <summary>Where a look is in the list (-1 if there's no look with that id).</summary>
    public static int IndexOf(string? id) => Array.FindIndex(All, look => look.Id == id);

    /// <summary>The next look this many bolts are saving up for (null when every bolt look is won).</summary>
    public static Look? NextBoltPrize(int bank) =>
        All.Where(look => look.By == UnlockBy.Bolts && look.Need > bank).OrderBy(look => look.Need).FirstOrDefault();

    /// <summary>The bolts the last bolt look won before this many bolts needed (0 if none was won yet).</summary>
    public static int LastBoltPrizeNeed(int bank) =>
        All.Where(look => look.By == UnlockBy.Bolts && look.Need <= bank).Select(look => look.Need).DefaultIfEmpty(0).Max();
}
