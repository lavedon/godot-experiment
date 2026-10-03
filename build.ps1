<#
.SYNOPSIS
    Builds Robot Dash into ONE file: dist\RobotDash.exe

.DESCRIPTION
    1. Compiles the C# game code.
    2. Asks Godot to export the game (this makes an exe plus a data folder).
    3. Zips that export and bakes it into a small launcher, so you get a single exe.

    Run it from PowerShell:   .\build.ps1
    Use another Godot:        .\build.ps1 -GodotDir 'D:\Godot_v4.7.2-stable_mono_win64'
#>
[CmdletBinding()]
param(
    # Folder holding the Godot 4.7.2 .NET editor and its export templates (.tpz)
    [string]$GodotDir = 'C:\my-coding-projects\godotPalaceRoomViewer\.tools'
)
$ErrorActionPreference = 'Stop'
$project = $PSScriptRoot
$godot = Join-Path $GodotDir 'Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
if (-not (Test-Path $godot)) { throw "Godot not found at $godot. Pass -GodotDir." }

function Step($text) { Write-Host "`n==> $text" -ForegroundColor Cyan }

# Don't let .NET leave helper programs (build servers) running after it finishes.
# Otherwise they can keep this script waiting forever.
$env:MSBUILDDISABLENODEREUSE = '1'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:UseSharedCompilation = 'false'

# Runs Godot, waits for Godot itself to finish (not any helpers it started), and writes its output to a log.
function Invoke-Godot([string[]]$arguments, [string]$log) {
    $proc = Start-Process -FilePath $godot -ArgumentList $arguments -NoNewWindow -PassThru `
        -RedirectStandardOutput $log -RedirectStandardError "$log.err"
    if (-not $proc.WaitForExit(10 * 60 * 1000)) { $proc.Kill(); throw "Godot took too long. See $log" }
    return $proc.ExitCode
}

# Godot needs "export templates" (the pre-built game engine) to make an exe.
# Install them once into Godot's standard spot if they aren't there yet.
$templates = Join-Path $env:APPDATA 'Godot\export_templates\4.7.2.stable.mono'
if (-not (Test-Path (Join-Path $templates 'windows_release_x86_64.exe'))) {
    Step 'Installing Godot export templates (one time only)'
    $tpz = Join-Path $GodotDir 'Godot_v4.7.2-stable_mono_export_templates.tpz'
    if (-not (Test-Path $tpz)) { throw "Export templates not found at $tpz" }
    $unzipTo = Join-Path $env:TEMP 'robotdash-templates'
    Remove-Item $unzipTo -Recurse -Force -ErrorAction SilentlyContinue
    Expand-Archive -LiteralPath $tpz -DestinationPath $unzipTo -Force # .tpz is just a zip
    New-Item -ItemType Directory -Force (Split-Path $templates) | Out-Null
    Remove-Item $templates -Recurse -Force -ErrorAction SilentlyContinue
    Move-Item (Join-Path $unzipTo 'templates') $templates
    Remove-Item $unzipTo -Recurse -Force
}

Push-Location $project
try {
    Step 'Compiling the game code'
    dotnet build RobotDash.csproj -c Release --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'Game build failed.' }

    Step 'Exporting from Godot'
    # If someone is playing the copy in build\game right now, deleting it would break their game.
    $buildGame = (Resolve-Path -LiteralPath 'build').Path + '\game\'
    $running = Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.Path -and $_.Path.StartsWith($buildGame, [StringComparison]::OrdinalIgnoreCase) }
    if ($running) { throw "Robot Dash is running from build\game (someone is playing!). Close the game, then run build.ps1 again." }
    Remove-Item build\game, build\game.zip -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force build\game | Out-Null
    $null = Invoke-Godot @('--headless', '--path', '.', '--import') (Join-Path $project 'build\import.log')
    $log = Join-Path $project 'build\export.log'
    $exitCode = Invoke-Godot @('--headless', '--path', '.', '--export-release', '"Windows Desktop"', 'build/game/RobotDash.exe') $log
    if ($exitCode -ne 0 -or -not (Test-Path build\game\RobotDash.exe)) { throw "Godot export failed. See $log" }
    if (-not (Get-ChildItem build\game -Recurse -Filter e_sqlite3.dll)) { throw 'SQLite DLL is missing from the export.' }

    Step 'Packing everything into one exe'
    Compress-Archive -Path build\game\* -DestinationPath build\game.zip -CompressionLevel Optimal
    Remove-Item dist -Recurse -Force -ErrorAction SilentlyContinue
    dotnet publish Launcher\Launcher.csproj -c Release -o dist --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'Launcher build failed.' }
    Get-ChildItem dist -Exclude RobotDash.exe | Remove-Item -Recurse -Force # leave only the exe

    $size = [math]::Round((Get-Item dist\RobotDash.exe).Length / 1MB, 1)
    Write-Host "`nDone! dist\RobotDash.exe ($size MB) - copy it anywhere and double-click to play." -ForegroundColor Green
}
finally { Pop-Location }
