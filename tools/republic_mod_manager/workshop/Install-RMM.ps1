# Republic Mod Manager - installer of the Steam Workshop package.
#
# Copies rmm.exe, its settings schemas and the Workshop Bridge from
# "Manual Installation\tesmioloader\build" into <game>\tesmioloader\build,
# switches the bridge on in tesmioloader.ini and can create a desktop shortcut.
# It never touches the TesmioLoader itself (tesmioloader.dll, tesmiolauncher.exe):
# the loader must already be installed. -Uninstall removes what this script
# installed and leaves your personal settings (user_config) alone.
#
# Start it through Install-RMM.bat (double-click). Options for the console:
#   -GamePath "C:\...\SovietRepublic"   game folder when it is not found automatically
#   -Shortcut / -NoShortcut             desktop shortcut without asking
#   -Uninstall                          remove the installed files
param(
    [string]$GamePath = '',
    [switch]$Uninstall,
    [switch]$Shortcut,
    [switch]$NoShortcut
)
$ErrorActionPreference = 'Stop'
try { [Console]::OutputEncoding = [Text.Encoding]::UTF8 } catch { }

$german = $false
try { $german = (Get-UICulture).TwoLetterISOLanguageName -eq 'de' } catch { }
function T([string]$de, [string]$en) { if ($german) { return $de } else { return $en } }
function Step([string]$text) { Write-Host ('  ' + $text) }
function Ok([string]$text) { Write-Host ('  [OK] ' + $text) -ForegroundColor Green }
function Note([string]$text) { Write-Host ('  [--] ' + $text) -ForegroundColor DarkGray }
function Fail([string]$what, [string]$why) {
    Write-Host ''
    Write-Host ((T 'FEHLER: ' 'ERROR: ') + $what) -ForegroundColor Red
    Write-Host ((T 'Grund:  ' 'Reason: ') + $why) -ForegroundColor Yellow
    exit 1
}
function Hash([string]$path) { return (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash }
function Interactive() {
    try { return ([Environment]::UserInteractive -and -not [Console]::IsInputRedirected) } catch { return $false }
}

# ---------------------------------------------------------------- package
$source = Join-Path $PSScriptRoot 'Manual Installation\tesmioloader\build'
if (-not (Test-Path -LiteralPath (Join-Path $source 'rmm.exe') -PathType Leaf)) {
    Fail (T 'Das Paket ist unvollständig.' 'The package is incomplete.') `
         (T ('rmm.exe fehlt unter ' + $source + '. Bitte das Workshop-Objekt in Steam einmal abbestellen und neu abonnieren.') `
            ('rmm.exe is missing under ' + $source + '. Please unsubscribe and resubscribe the Workshop item in Steam.'))
}
$files = Get-ChildItem -LiteralPath $source -Recurse -File | ForEach-Object { $_.FullName.Substring($source.Length + 1) }
# Files with the player's own values: written once, never overwritten by an update.
$keep = @('rmm.ini', 'plugins\workshop_bridge.ini')

# ---------------------------------------------------------------- game folder
function SteamLibraries() {
    $result = @()
    try {
        $steam = (Get-ItemProperty -Path 'HKCU:\Software\Valve\Steam' -ErrorAction Stop).SteamPath
        if ($steam) {
            $steam = $steam.Replace('/', '\')
            $result += $steam
            $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
            if (Test-Path -LiteralPath $vdf) {
                foreach ($line in Get-Content -LiteralPath $vdf) {
                    if ($line -match '^\s*"path"\s+"(.+)"\s*$') { $result += $Matches[1].Replace('\\', '\') }
                }
            }
        }
    } catch { }
    return $result
}
$game = ''
if ($GamePath -ne '') {
    $game = $GamePath.TrimEnd('\')
} else {
    # The Workshop item lives in <library>\steamapps\workshop\content\784150\<item>;
    # the game in <library>\steamapps\common\SovietRepublic.
    $steamapps = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..\..'))
    $candidate = Join-Path $steamapps 'common\SovietRepublic'
    if (Test-Path -LiteralPath (Join-Path $candidate 'SOVIET64.exe') -PathType Leaf) { $game = $candidate }
    if ($game -eq '') {
        foreach ($library in SteamLibraries) {
            $candidate = Join-Path $library 'steamapps\common\SovietRepublic'
            if (Test-Path -LiteralPath (Join-Path $candidate 'SOVIET64.exe') -PathType Leaf) { $game = $candidate; break }
        }
    }
    if ($game -eq '' -and (Interactive)) {
        Write-Host (T 'Der Spielordner wurde nicht gefunden.' 'The game folder was not found.') -ForegroundColor Yellow
        $game = (Read-Host (T 'Bitte den Ordner mit SOVIET64.exe eingeben (z. B. C:\Program Files (x86)\Steam\steamapps\common\SovietRepublic)' `
                              'Please enter the folder with SOVIET64.exe (e.g. C:\Program Files (x86)\Steam\steamapps\common\SovietRepublic)')).Trim().Trim('"').TrimEnd('\')
    }
}
if ($game -eq '' -or -not (Test-Path -LiteralPath (Join-Path $game 'SOVIET64.exe') -PathType Leaf)) {
    Fail (T 'Der Spielordner wurde nicht gefunden.' 'The game folder was not found.') `
         (T ('Unter "' + $game + '" liegt keine SOVIET64.exe. Starte Install-RMM.bat aus dem abonnierten Workshop-Ordner oder gib den Ordner an: Install-RMM.bat -GamePath "C:\...\SovietRepublic".') `
            ('There is no SOVIET64.exe under "' + $game + '". Run Install-RMM.bat from the subscribed Workshop folder or pass the folder: Install-RMM.bat -GamePath "C:\...\SovietRepublic".'))
}
$build = Join-Path $game 'tesmioloader\build'
Ok ((T 'Spielordner: ' 'Game folder: ') + $game)

# ---------------------------------------------------------------- loader
$loaderDll = Join-Path $build 'tesmioloader.dll'
$loaderIni = Join-Path $build 'tesmioloader.ini'
if (-not (Test-Path -LiteralPath $loaderDll -PathType Leaf) -or -not (Test-Path -LiteralPath $loaderIni -PathType Leaf)) {
    Fail (T 'Der TesmioLoader ist nicht installiert.' 'The TesmioLoader is not installed.') `
         (T ('Es fehlt ' + $build + '\tesmioloader.dll oder tesmioloader.ini. Republic Mod Manager braucht den TesmioLoader von MaxLegend: im Steam-Workshop abonnieren und nach seiner Anleitung installieren (Quelle: https://github.com/MaxLegend/TesmioLoader). Danach Install-RMM.bat erneut starten.') `
            ('Missing ' + $build + '\tesmioloader.dll or tesmioloader.ini. Republic Mod Manager needs the TesmioLoader by MaxLegend: subscribe to it in the Steam Workshop and install it as its guide says (source: https://github.com/MaxLegend/TesmioLoader). Then run Install-RMM.bat again.'))
}
Ok (T 'TesmioLoader gefunden.' 'TesmioLoader found.')

# ---------------------------------------------------------------- running programs
$running = @(Get-Process -Name rmm, tesmiolauncher, SOVIET64 -ErrorAction SilentlyContinue)
if ($running.Count -gt 0) {
    Fail (T 'Das Spiel oder der Republic Mod Manager läuft noch.' 'The game or Republic Mod Manager is still running.') `
         (T ('Bitte zuerst beenden: ' + (($running | ForEach-Object { $_.ProcessName } | Sort-Object -Unique) -join ', ') + '. Dateien, die gerade benutzt werden, lassen sich nicht ersetzen.') `
            ('Please close first: ' + (($running | ForEach-Object { $_.ProcessName } | Sort-Object -Unique) -join ', ') + '. Files in use cannot be replaced.'))
}

# ---------------------------------------------------------------- tesmioloader.ini
function SetBridgeSwitch([string]$path, [string]$value) {
    $text = [IO.File]::ReadAllText($path)
    $nl = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $lines = New-Object System.Collections.Generic.List[string]
    foreach ($l in ($text -split "`r?`n")) { $lines.Add($l) }
    # drop a trailing empty element produced by a final newline
    if ($lines.Count -gt 0 -and $lines[$lines.Count - 1] -eq '') { $lines.RemoveAt($lines.Count - 1) }
    $section = -1; $end = -1; $found = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $l = $lines[$i]
        if ($l -match '^\s*\[(.+)\]\s*$') {
            if ($section -ge 0 -and $end -lt 0) { $end = $i }
            if ($Matches[1].Trim().ToLowerInvariant() -eq 'plugins') { $section = $i; $end = -1 }
            continue
        }
        if ($section -ge 0 -and $end -lt 0 -and $l -match '^\s*workshop_bridge\s*=') { $found = $i }
    }
    if ($found -ge 0) {
        if ($lines[$found] -match ('^\s*workshop_bridge\s*=\s*' + [regex]::Escape($value) + '\s*$')) { return $false }
        $lines[$found] = 'workshop_bridge=' + $value
    } elseif ($section -ge 0) {
        if ($end -lt 0) { $end = $lines.Count }
        # insert after the last non-empty line of the section
        $at = $end
        while ($at -gt $section + 1 -and $lines[$at - 1].Trim() -eq '') { $at-- }
        $lines.Insert($at, 'workshop_bridge=' + $value)
    } else {
        if ($lines.Count -gt 0 -and $lines[$lines.Count - 1].Trim() -ne '') { $lines.Add('') }
        $lines.Add('[plugins]'); $lines.Add('workshop_bridge=' + $value)
    }
    # UTF-8 without a BOM: the loader reads the file byte by byte.
    [IO.File]::WriteAllText($path, (($lines -join $nl) + $nl), (New-Object Text.UTF8Encoding($false)))
    return $true
}

# ================================================================ uninstall
if ($Uninstall) {
    Write-Host (T 'Entferne Republic Mod Manager ...' 'Removing Republic Mod Manager ...')
    $removed = 0
    foreach ($rel in $files) {
        $target = Join-Path $build $rel
        if (Test-Path -LiteralPath $target -PathType Leaf) { Remove-Item -LiteralPath $target -Force; $removed++ }
    }
    foreach ($dir in @('settings_schemas\languages', 'settings_schemas')) {
        $d = Join-Path $build $dir
        if ((Test-Path -LiteralPath $d) -and -not (Get-ChildItem -LiteralPath $d -Force | Select-Object -First 1)) { Remove-Item -LiteralPath $d -Force }
    }
    Ok ((T 'Dateien entfernt: ' 'Files removed: ') + $removed)
    if (SetBridgeSwitch $loaderIni '0') { Ok (T 'tesmioloader.ini: workshop_bridge=0' 'tesmioloader.ini: workshop_bridge=0') }
    $lnk = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Republic Mod Manager.lnk'
    if (Test-Path -LiteralPath $lnk) { Remove-Item -LiteralPath $lnk -Force; Ok (T 'Desktop-Verknüpfung entfernt.' 'Desktop shortcut removed.') }
    Note (T ('Deine persönlichen Einstellungen unter ' + $build + '\user_config bleiben erhalten.') ('Your personal settings under ' + $build + '\user_config are kept.'))
    Write-Host ''
    Write-Host (T 'Republic Mod Manager wurde entfernt.' 'Republic Mod Manager has been removed.') -ForegroundColor Green
    exit 0
}

# ================================================================ install
Write-Host (T 'Installiere Republic Mod Manager ...' 'Installing Republic Mod Manager ...')
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backup = Join-Path $game ('tesmioloader\rmm_install_backup\' + $stamp)
$copied = 0; $kept = 0; $replaced = 0
foreach ($rel in $files) {
    $from = Join-Path $source $rel
    $to = Join-Path $build $rel
    $exists = Test-Path -LiteralPath $to -PathType Leaf
    if ($exists -and ($keep -contains $rel)) { $kept++; Note ((T 'behalten: ' 'kept: ') + $rel); continue }
    if ($exists -and (Hash $from) -eq (Hash $to)) { Note ((T 'unverändert: ' 'unchanged: ') + $rel); continue }
    $parent = Split-Path -Parent $to
    if (-not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
    if ($exists) {
        $bparent = Split-Path -Parent (Join-Path $backup $rel)
        if (-not (Test-Path -LiteralPath $bparent)) { New-Item -ItemType Directory -Path $bparent -Force | Out-Null }
        Copy-Item -LiteralPath $to -Destination (Join-Path $backup $rel) -Force
        $replaced++
    }
    try {
        Copy-Item -LiteralPath $from -Destination $to -Force
    } catch {
        Fail ((T 'Datei konnte nicht kopiert werden: ' 'File could not be copied: ') + $rel) `
             ((T 'Windows meldet: ' 'Windows says: ') + $_.Exception.Message + (T ' Läuft das Spiel noch, oder fehlen Schreibrechte im Spielordner?' ' Is the game still running, or is the game folder write-protected?'))
    }
    if ((Hash $from) -ne (Hash $to)) {
        Fail ((T 'Datei ist nach dem Kopieren nicht identisch: ' 'File differs after copying: ') + $rel) `
             (T 'Der Kopiervorgang wurde gestört (Virenscanner, volle Platte?). Bitte erneut starten.' 'The copy was disturbed (antivirus, full disk?). Please run again.')
    }
    $copied++
}
Ok ((T 'Dateien kopiert: ' 'Files copied: ') + $copied + (T ', behalten: ' ', kept: ') + $kept + (T ', ersetzt: ' ', replaced: ') + $replaced)
if ($replaced -gt 0) { Note ((T 'Sicherung der ersetzten Dateien: ' 'Backup of the replaced files: ') + $backup) }

if (SetBridgeSwitch $loaderIni '1') { Ok (T 'tesmioloader.ini: Workshop Bridge eingeschaltet (workshop_bridge=1).' 'tesmioloader.ini: Workshop Bridge switched on (workshop_bridge=1).') }
else { Ok (T 'tesmioloader.ini: Workshop Bridge war schon eingeschaltet.' 'tesmioloader.ini: Workshop Bridge was already on.') }

# ---------------------------------------------------------------- shortcut
$want = $false
if ($Shortcut) { $want = $true }
elseif ($NoShortcut) { $want = $false }
elseif (Interactive) {
    $answer = Read-Host (T 'Verknüpfung zu rmm.exe auf dem Desktop anlegen? (J/N)' 'Create a shortcut to rmm.exe on the desktop? (Y/N)')
    $want = ($answer.Trim().ToLowerInvariant() -in @('j', 'ja', 'y', 'yes'))
}
if ($want) {
    try {
        $lnk = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Republic Mod Manager.lnk'
        $shell = New-Object -ComObject WScript.Shell
        $sc = $shell.CreateShortcut($lnk)
        $sc.TargetPath = Join-Path $build 'rmm.exe'
        $sc.WorkingDirectory = $build
        $sc.IconLocation = (Join-Path $build 'rmm.exe') + ',0'
        $sc.Description = 'Republic Mod Manager'
        $sc.Save()
        Ok ((T 'Desktop-Verknüpfung angelegt: ' 'Desktop shortcut created: ') + $lnk)
    } catch {
        Note ((T 'Desktop-Verknüpfung konnte nicht angelegt werden: ' 'Desktop shortcut could not be created: ') + $_.Exception.Message)
    }
}

Write-Host ''
Write-Host (T 'Republic Mod Manager ist installiert.' 'Republic Mod Manager is installed.') -ForegroundColor Green
Write-Host ((T 'Starten: ' 'Start: ') + (Join-Path $build 'rmm.exe'))
Write-Host (T 'Nach einem Update des Workshop-Objekts einfach Install-RMM.bat erneut ausführen.' 'After an update of the Workshop item just run Install-RMM.bat again.')
exit 0
