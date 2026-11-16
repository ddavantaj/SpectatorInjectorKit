# Spectator Studio - Installer grafic
# Ruleaza prin "Instaleaza.cmd" (dublu-click). Nu necesita drepturi de administrator
# decat daca jocul este in Program Files.
param([string]$Check)   # mod de test fara fereastra: -Check "C:\cale\joc"

$ErrorActionPreference = 'Stop'
$Kit = Split-Path -Parent $MyInvocation.MyCommand.Path
$Injector = Join-Path $Kit 'InjectSpectatorCamera.exe'
$FfmpegUrl = 'https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip'
$SettingsFile = Join-Path $env:APPDATA 'SpectatorStudioInstaller.txt'

# ---------- functii de baza ----------
function Get-GameInfo([string]$dir) {
    $r = [ordered]@{ Valid = $false; Exe = $null; Data = $null; Backend = $null; Installed = $false; Ffmpeg = $false; Message = '' }
    if ([string]::IsNullOrWhiteSpace($dir) -or -not (Test-Path -LiteralPath $dir -PathType Container)) {
        $r.Message = 'Alege folderul jocului.'; return [pscustomobject]$r
    }
    $dataDir = Get-ChildItem -LiteralPath $dir -Directory -Filter '*_Data' -ErrorAction SilentlyContinue | Select-Object -First 1
    $exe = Get-ChildItem -LiteralPath $dir -File -Filter '*.exe' -ErrorAction SilentlyContinue |
           Where-Object { $_.Name -notmatch '^(UnityCrashHandler|ffmpeg)' } | Select-Object -First 1
    if (-not $dataDir -or -not $exe) {
        $r.Message = 'Acesta nu pare un joc Unity. Alege folderul unde se afla fisierul .exe al jocului (langa folderul *_Data).'
        return [pscustomobject]$r
    }
    $r.Valid = $true; $r.Exe = $exe.FullName; $r.Data = $dataDir.FullName
    $r.Backend = if (Test-Path -LiteralPath (Join-Path $dir 'GameAssembly.dll')) { 'IL2CPP' } else { 'Mono' }
    $r.Installed = Test-Path -LiteralPath (Join-Path $dataDir.FullName 'Managed\SpectatorPlugin.dll')
    $r.Ffmpeg = Test-Path -LiteralPath (Join-Path $dir 'ffmpeg.exe')
    $r.Message = "Joc gasit: $($exe.Name)  |  Unity $($r.Backend)"
    return [pscustomobject]$r
}

function Test-GameRunning([string]$dir) {
    $full = [IO.Path]::GetFullPath($dir).TrimEnd('\') + '\'
    foreach ($p in Get-Process -ErrorAction SilentlyContinue) {
        try { if ($p.Path -and $p.Path.StartsWith($full, 'OrdinalIgnoreCase')) { return $p.ProcessName } } catch {}
    }
    return $null
}

# Ruleaza injectorul si returneaza liniile de iesire utile + codul de iesire
function Invoke-Injector([string]$dir, [string]$arg) {
    $psi = New-Object Diagnostics.ProcessStartInfo
    $psi.FileName = $Injector
    $psi.Arguments = ('"{0}"' -f $dir.TrimEnd('\')) + $(if ($arg) { " $arg" } else { '' })
    $psi.WorkingDirectory = $Kit
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true; $psi.RedirectStandardError = $true; $psi.RedirectStandardInput = $true
    $psi.StandardOutputEncoding = [Text.Encoding]::UTF8
    $psi.CreateNoWindow = $true
    $p = [Diagnostics.Process]::Start($psi)
    $p.StandardInput.Close()
    $out = $p.StandardOutput.ReadToEnd() + $p.StandardError.ReadToEnd()
    if (-not $p.WaitForExit(120000)) { try { $p.Kill() } catch {}; return @{ Code = -1; Lines = @('Injectorul nu a raspuns (timeout).') } }
    $lines = $out -split "`r?`n" | Where-Object { $_ -match '^\s*\[' } | ForEach-Object { $_.Trim() }
    return @{ Code = $p.ExitCode; Lines = $lines; Raw = $out }
}

# ---------- mod test (fara GUI) ----------
if ($Check) { Get-GameInfo $Check | Format-List; return }

# ---------- GUI ----------
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
[Windows.Forms.Application]::EnableVisualStyles()

$bg = [Drawing.Color]::FromArgb(24, 26, 27); $panel = [Drawing.Color]::FromArgb(34, 37, 38)
$fg = [Drawing.Color]::FromArgb(235, 238, 240); $muted = [Drawing.Color]::FromArgb(150, 158, 162)
$accent = [Drawing.Color]::FromArgb(190, 242, 80); $ok = [Drawing.Color]::FromArgb(110, 220, 130)
$bad = [Drawing.Color]::FromArgb(255, 110, 110)

function New-Label($text, $x, $y, $w, $h, $size = 10, $style = 'Regular', $color = $fg) {
    $l = New-Object Windows.Forms.Label; $l.Text = $text; $l.Location = "$x,$y"; $l.Size = "$w,$h"
    $l.Font = New-Object Drawing.Font('Segoe UI', $size, [Drawing.FontStyle]::$style); $l.ForeColor = $color; $l.BackColor = [Drawing.Color]::Transparent
    return $l
}
function New-Button($text, $x, $y, $w, $h, $primary = $false) {
    $b = New-Object Windows.Forms.Button; $b.Text = $text; $b.Location = "$x,$y"; $b.Size = "$w,$h"
    $b.FlatStyle = 'Flat'; $b.Font = New-Object Drawing.Font('Segoe UI', 10, [Drawing.FontStyle]::Bold); $b.Cursor = 'Hand'
    $b.FlatAppearance.BorderSize = 0
    if ($primary) { $b.BackColor = $accent; $b.ForeColor = [Drawing.Color]::FromArgb(20, 24, 10) }
    else { $b.BackColor = [Drawing.Color]::FromArgb(52, 56, 58); $b.ForeColor = $fg }
    return $b
}

$form = New-Object Windows.Forms.Form
$form.Text = 'Spectator Studio - Installer'; $form.ClientSize = '640,590'; $form.StartPosition = 'CenterScreen'
$form.BackColor = $bg; $form.FormBorderStyle = 'FixedSingle'; $form.MaximizeBox = $false; $form.AllowDrop = $true

$form.Controls.Add((New-Label 'Spectator Studio' 24 18 592 34 20 'Bold' $accent))
$form.Controls.Add((New-Label 'Camera spectator, capturi si panou web pentru jocul tau Unity. Instalare in 3 pasi.' 24 54 592 22 10 'Regular' $muted))

# Pas 1
$form.Controls.Add((New-Label '1.  Alege folderul jocului' 24 92 592 24 11 'Bold'))
$tb = New-Object Windows.Forms.TextBox; $tb.Location = '24,120'; $tb.Size = '470,28'; $tb.Font = New-Object Drawing.Font('Segoe UI', 10)
$tb.BackColor = $panel; $tb.ForeColor = $fg; $tb.BorderStyle = 'FixedSingle'; $form.Controls.Add($tb)
$btnBrowse = New-Button 'Rasfoieste...' 504 118 112 32; $form.Controls.Add($btnBrowse)
$form.Controls.Add((New-Label 'Sfat: trage folderul jocului direct in aceasta fereastra, sau alege folderul unde se afla fisierul .exe.' 24 154 592 20 9 'Regular' $muted))

# Stare
$statusBox = New-Object Windows.Forms.Panel; $statusBox.Location = '24,184'; $statusBox.Size = '592,92'; $statusBox.BackColor = $panel; $form.Controls.Add($statusBox)
$lblGame = New-Label '' 14 8 566 22 10 'Regular'; $lblInst = New-Label '' 14 32 566 22 10 'Bold'; $lblFf = New-Label '' 14 58 566 22 10 'Regular'
$statusBox.Controls.AddRange(@($lblGame, $lblInst, $lblFf))

# Pas 2
$form.Controls.Add((New-Label '2.  Instaleaza' 24 292 592 24 11 'Bold'))
$btnInstall = New-Button 'Instaleaza / Actualizeaza' 24 320 290 46 $true; $form.Controls.Add($btnInstall)
$btnUninstall = New-Button 'Dezinstaleaza' 324 320 140 46; $form.Controls.Add($btnUninstall)
$btnFfmpeg = New-Button 'Instaleaza ffmpeg' 474 320 142 46; $form.Controls.Add($btnFfmpeg)

# Pas 3
$form.Controls.Add((New-Label '3.  Porneste jocul si foloseste-l' 24 382 592 24 11 'Bold'))
$form.Controls.Add((New-Label 'F1 = panou in joc   |   F9 = screenshot   |   F10 = video   |   Panou web: 127.0.0.1:47830' 24 408 592 22 9 'Regular' $muted))
$btnLaunch = New-Button 'Porneste jocul' 24 434 190 38; $btnWeb = New-Button 'Deschide panoul web' 224 434 190 38; $btnFolder = New-Button 'Deschide folderul' 424 434 192 38
$form.Controls.AddRange(@($btnLaunch, $btnWeb, $btnFolder))

# Jurnal
$log = New-Object Windows.Forms.TextBox; $log.Location = '24,484'; $log.Size = '592,84'; $log.Multiline = $true; $log.ReadOnly = $true; $log.ScrollBars = 'Vertical'
$log.BackColor = [Drawing.Color]::FromArgb(14, 15, 16); $log.ForeColor = $muted; $log.Font = New-Object Drawing.Font('Consolas', 9); $log.BorderStyle = 'None'
$form.Controls.Add($log)
$prog = New-Object Windows.Forms.ProgressBar; $prog.Location = '24,572'; $prog.Size = '592,8'; $prog.Visible = $false; $form.Controls.Add($prog)

function Write-Log([string]$t) { $log.AppendText($t + "`r`n"); [Windows.Forms.Application]::DoEvents() }
function Set-Busy([bool]$b) { foreach ($c in @($btnInstall, $btnUninstall, $btnFfmpeg, $btnBrowse)) { $c.Enabled = -not $b }; $form.UseWaitCursor = $b; [Windows.Forms.Application]::DoEvents() }

function Update-State {
    $g = Get-GameInfo $tb.Text.Trim('" ')
    $lblGame.Text = $g.Message; $lblGame.ForeColor = if ($g.Valid) { $fg } else { $bad }
    if ($g.Valid) {
        $lblInst.Text = if ($g.Installed) { 'Spectator Studio: INSTALAT' } else { 'Spectator Studio: neinstalat' }
        $lblInst.ForeColor = if ($g.Installed) { $ok } else { $muted }
        $lblFf.Text = if ($g.Ffmpeg) { 'ffmpeg (video F10): gasit' } else { 'ffmpeg (video F10): lipseste - apasa "Instaleaza ffmpeg" pentru inregistrari video' }
        $lblFf.ForeColor = if ($g.Ffmpeg) { $ok } else { $muted }
    } else { $lblInst.Text = ''; $lblFf.Text = '' }
    $btnInstall.Enabled = $g.Valid; $btnUninstall.Enabled = $g.Valid -and $g.Installed; $btnFfmpeg.Enabled = $g.Valid -and -not $g.Ffmpeg
    $btnInstall.Text = if ($g.Installed) { 'Actualizeaza' } else { 'Instaleaza' }
    $btnLaunch.Enabled = $g.Valid; $btnFolder.Enabled = $g.Valid
    return $g
}

function Set-Folder([string]$p) {
    $p = $p.Trim('" ')
    if (Test-Path -LiteralPath $p -PathType Leaf) { $p = Split-Path -Parent $p }
    $tb.Text = $p; $null = Update-State
    try { Set-Content -LiteralPath $SettingsFile -Value $p -Encoding UTF8 } catch {}
}

function Assert-NotRunning($g) {
    $name = Test-GameRunning (Split-Path -Parent $g.Exe)
    if ($name) {
        [Windows.Forms.MessageBox]::Show("Jocul ($name) ruleaza. Inchide-l si incearca din nou.", 'Spectator Studio', 'OK', 'Warning') | Out-Null
        return $false
    }
    return $true
}

$btnBrowse.Add_Click({
    $d = New-Object Windows.Forms.FolderBrowserDialog; $d.Description = 'Alege folderul jocului (unde se afla fisierul .exe)'
    if ($tb.Text -and (Test-Path -LiteralPath $tb.Text)) { $d.SelectedPath = $tb.Text }
    if ($d.ShowDialog() -eq 'OK') { Set-Folder $d.SelectedPath }
})
$tb.Add_Leave({ Set-Folder $tb.Text })
$tb.Add_KeyDown({ if ($_.KeyCode -eq 'Enter') { Set-Folder $tb.Text; $_.SuppressKeyPress = $true } })
$form.Add_DragEnter({ if ($_.Data.GetDataPresent([Windows.Forms.DataFormats]::FileDrop)) { $_.Effect = 'Copy' } })
$form.Add_DragDrop({ $f = $_.Data.GetData([Windows.Forms.DataFormats]::FileDrop); if ($f) { Set-Folder $f[0] } })

function Run-Injector([string]$arg, [string]$title) {
    $g = Update-State
    if (-not $g.Valid -or -not (Assert-NotRunning $g)) { return }
    $dir = Split-Path -Parent $g.Exe
    Set-Busy $true; Write-Log "== $title =="
    try {
        if (-not (Test-Path -LiteralPath $Injector)) { throw "Lipseste InjectSpectatorCamera.exe din folderul kitului." }
        $r = Invoke-Injector $dir $arg
        foreach ($l in $r.Lines) { Write-Log $l }
        $g2 = Get-GameInfo $dir
        $success = if ($arg -eq '--uninstall') { -not $g2.Installed } else { $g2.Installed }
        if ($success) { Write-Log 'Gata!'; [Windows.Forms.MessageBox]::Show($(if ($arg -eq '--uninstall') { 'Dezinstalat. Jocul este din nou original.' } else { "Instalat cu succes!`n`nPorneste jocul si apasa F1." }), 'Spectator Studio', 'OK', 'Information') | Out-Null }
        else { Write-Log 'Operatia nu a reusit. Vezi mesajele de mai sus.'; [Windows.Forms.MessageBox]::Show('Operatia nu a reusit. Detalii in jurnal.', 'Spectator Studio', 'OK', 'Error') | Out-Null }
    } catch { Write-Log "EROARE: $($_.Exception.Message)" }
    finally { Set-Busy $false; $null = Update-State }
}

$btnInstall.Add_Click({ Run-Injector '' 'Instalare' })
$btnUninstall.Add_Click({
    if ([Windows.Forms.MessageBox]::Show('Elimin Spectator Studio si restaurez fisierele originale ale jocului?', 'Dezinstalare', 'YesNo', 'Question') -eq 'Yes') { Run-Injector '--uninstall' 'Dezinstalare' }
})

$btnFfmpeg.Add_Click({
    $g = Update-State; if (-not $g.Valid) { return }
    $dir = Split-Path -Parent $g.Exe; $dest = Join-Path $dir 'ffmpeg.exe'
    Set-Busy $true
    try {
        $local = Join-Path $Kit 'ffmpeg.exe'
        if (Test-Path -LiteralPath $local) {
            Write-Log 'Copiez ffmpeg.exe din kit...'; Copy-Item -LiteralPath $local -Destination $dest -Force
        } else {
            $tmp = Join-Path ([IO.Path]::GetTempPath()) ('ffmpeg_' + [guid]::NewGuid().ToString('N')); New-Item -ItemType Directory -Path $tmp | Out-Null
            $zip = Join-Path $tmp 'ffmpeg.zip'
            Write-Log 'Descarc ffmpeg (aprox. 90 MB, o singura data)...'
            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
            $wc = New-Object Net.WebClient; $prog.Visible = $true; $prog.Value = 0
            $wc.Add_DownloadProgressChanged({ $prog.Value = [Math]::Min(100, $_.ProgressPercentage) })
            $task = $wc.DownloadFileTaskAsync($FfmpegUrl, $zip)
            while (-not $task.IsCompleted) { [Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 50 }
            if ($task.IsFaulted) { throw $task.Exception.GetBaseException().Message }
            Write-Log 'Extrag ffmpeg.exe...'
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            $z = [IO.Compression.ZipFile]::OpenRead($zip)
            try {
                $e = $z.Entries | Where-Object { $_.FullName -match '(^|/)bin/ffmpeg\.exe$' } | Select-Object -First 1
                if (-not $e) { throw 'ffmpeg.exe nu a fost gasit in arhiva.' }
                [IO.Compression.ZipFileExtensions]::ExtractToFile($e, $dest, $true)
            } finally { $z.Dispose() }
            Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue
        }
        Write-Log 'ffmpeg instalat. Inregistrarea video (F10) este disponibila.'
    } catch { Write-Log "EROARE ffmpeg: $($_.Exception.Message)" }
    finally { $prog.Visible = $false; Set-Busy $false; $null = Update-State }
})

$btnLaunch.Add_Click({ $g = Update-State; if ($g.Valid) { Start-Process -FilePath $g.Exe -WorkingDirectory (Split-Path -Parent $g.Exe) } })
$btnWeb.Add_Click({ Start-Process 'http://127.0.0.1:47830/' })
$btnFolder.Add_Click({ $g = Update-State; if ($g.Valid) { Start-Process explorer.exe (Split-Path -Parent $g.Exe) } })

# folder initial: ultimul folosit, altfel jocul Unity care ruleaza
$start = ''
try { if (Test-Path $SettingsFile) { $start = (Get-Content -LiteralPath $SettingsFile -Raw).Trim() } } catch {}
if (-not $start -or -not (Test-Path -LiteralPath $start)) {
    foreach ($p in Get-Process -ErrorAction SilentlyContinue) {
        try { if ($p.Path -and (Test-Path (Join-Path (Split-Path -Parent $p.Path) 'UnityPlayer.dll'))) { $start = Split-Path -Parent $p.Path; break } } catch {}
    }
}
if ($start) { Set-Folder $start } else { $null = Update-State }
$form.ActiveControl = $btnInstall
if ($env:SPECTATOR_INSTALLER_SHOT) {   # mod de test: salveaza o imagine a ferestrei si iese
    $form.Add_Shown({
        $bmp = New-Object Drawing.Bitmap $form.ClientSize.Width, $form.ClientSize.Height
        $form.DrawToBitmap($bmp, (New-Object Drawing.Rectangle 0, 0, $form.ClientSize.Width, $form.ClientSize.Height))
        $bmp.Save($env:SPECTATOR_INSTALLER_SHOT); $form.Close()
    })
}
[void]$form.ShowDialog()
