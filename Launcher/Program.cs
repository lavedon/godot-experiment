using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

// Robot Dash launcher: the whole game is zipped up inside this exe.
// The first time it runs (or after a new build), it unpacks the game to
// %LOCALAPPDATA%\RobotDash\game-<version>, then starts it.
try
{
    using var zip = Assembly.GetExecutingAssembly().GetManifestResourceStream("game.zip")
        ?? throw new InvalidOperationException("The game data is missing from this exe.");

    string version = Convert.ToHexString(SHA256.HashData(zip))[..12];
    zip.Position = 0;

    string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RobotDash");
    string gameDir = Path.Combine(root, "game-" + version);
    string gameExe = Path.Combine(gameDir, "RobotDash.exe");

    if (!File.Exists(gameExe))
    {
        // Unpack into a temporary folder first, then rename, so a half-finished unpack is never used.
        string temp = gameDir + ".unpacking";
        if (Directory.Exists(temp)) Directory.Delete(temp, true);
        ZipFile.ExtractToDirectory(zip, temp);
        Directory.Move(temp, gameDir);

        // Tidy up older versions.
        foreach (var old in Directory.GetDirectories(root, "game-*"))
            if (old != gameDir)
                try { Directory.Delete(old, true); } catch { /* still running? leave it */ }
    }

    var start = new ProcessStartInfo(gameExe) { WorkingDirectory = gameDir };
    foreach (var arg in args) start.ArgumentList.Add(arg);
    Process.Start(start);
}
catch (Exception e)
{
    MessageBoxW(IntPtr.Zero, "Robot Dash could not start:\n\n" + e.Message, "Robot Dash", 0x10);
}

[DllImport("user32.dll", CharSet = CharSet.Unicode)]
static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
