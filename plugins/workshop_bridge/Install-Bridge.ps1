# Installs workshop_bridge.dll and workshop_bridge.ini from the loader build
# into the game's tesmioloader\build\plugins folder, hash-checked, with a
# backup of whatever was there. Run after build.bat. -Replace allows
# overwriting an existing DLL (the previous files go to the backup). An
# existing INI is the player's base configuration (workshop_root and friends)
# and stays untouched unless -ReplaceIni is given as well.
param([switch]$Replace, [switch]$ReplaceIni)
$ErrorActionPreference = 'Stop'
$tree = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$build = 'C:\Program Files (x86)\Steam\steamapps\common\SovietRepublic\tesmioloader\build'

function Hash([string]$path) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return 'absent' }
    return (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
}
function Stopped {
    $running = Get-Process -Name rmm,tesmio_autoload,tesmiolauncher,SOVIET64 -ErrorAction SilentlyContinue
    if ($running) { throw ('Close the game, TesmioLauncher and Republic Mod Manager first: ' + ($running.ProcessName -join ', ')) }
}
$files = @('workshop_bridge.dll', 'workshop_bridge.ini')
$sources = @{}; foreach ($f in $files) { $sources[$f] = Join-Path $tree ('build\plugins\' + $f) }
foreach ($f in $files) { if (-not (Test-Path -LiteralPath $sources[$f] -PathType Leaf)) { throw "Missing build output: $($sources[$f]) - run build.bat first." } }
foreach ($f in 'tesmioloader.dll', 'tesmiolauncher.exe') { if (-not (Test-Path -LiteralPath (Join-Path $build $f))) { throw "Not a loader folder: $build" } }
Stopped
$targets = @{}; foreach ($f in $files) { $targets[$f] = Join-Path $build ('plugins\' + $f) }
foreach ($f in $files) { if ((Hash $targets[$f]) -ne 'absent' -and -not $Replace) { throw "Already installed: $($targets[$f]). Use -Replace to overwrite (a backup is kept)." } }
if ((Hash $targets['workshop_bridge.ini']) -ne 'absent' -and -not $ReplaceIni) { Write-Output "Existing workshop_bridge.ini kept (use -ReplaceIni to overwrite it)."; $files = @('workshop_bridge.dll') }

$backup = Join-Path $tree ('_backups\workshop_bridge_deploy_' + [DateTime]::UtcNow.ToString('yyyyMMdd_HHmmss'))
New-Item -ItemType Directory -Path $backup | Out-Null
foreach ($f in $files) { if ((Hash $targets[$f]) -ne 'absent') { Copy-Item -LiteralPath $targets[$f] -Destination (Join-Path $backup $f) } }
$expected = @{}; foreach ($f in $files) { $expected[$f] = Hash $sources[$f] }
foreach ($f in $files) {
    Stopped
    $staged = $targets[$f] + '.' + [Guid]::NewGuid().ToString('N') + '.new'
    Copy-Item -LiteralPath $sources[$f] -Destination $staged
    if ((Hash $staged) -ne $expected[$f]) { throw "Staging mismatch: $f" }
    # [NullString]::Value, not $null: PowerShell turns $null into "" and File.Replace rejects an empty backup path.
    if ((Hash $targets[$f]) -eq 'absent') { [IO.File]::Move($staged, $targets[$f]) } else { [IO.File]::Replace($staged, $targets[$f], [NullString]::Value) }
    if ((Hash $targets[$f]) -ne $expected[$f]) { throw "Installed hash mismatch: $f" }
}
Write-Output "PASS installed workshop_bridge ($($files.Count) files) into $build\plugins"
Write-Output "Backup: $backup"
