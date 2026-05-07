# Reads x64Migration/phase4-logs/perf-baseline/frames.csv and emits per-scene
# percentile breakdown. Scenes are TopScreen, with UniverseScreen optionally
# split into "Universe (idle)" and "Combat" by a wall-time threshold.
#
# Used in §4.1 to produce phase4-logs/perf-baseline.md and again in §4.4 to
# compare deltas vs the §4.1 baseline.
param(
    [string]$Csv = "x64Migration/phase4-logs/perf-baseline/frames.csv",
    [int]$WarmupFrames = 60,
    [double]$CombatStartSec = 0
)

if (-not (Test-Path $Csv)) {
    Write-Error "CSV not found: $Csv"
    exit 1
}

$rows = Import-Csv $Csv
Write-Host "Loaded $($rows.Count) frames from $Csv"

function Percentiles($samples, [int]$warmup) {
    $sorted = ($samples | Select-Object -Skip $warmup | ForEach-Object { [double]$_ } | Sort-Object)
    if ($sorted.Count -lt 30) { return $null }
    $count = $sorted.Count
    return @{
        Count = $count
        P50   = $sorted[[int]([math]::Floor($count * 0.50))]
        P95   = $sorted[[int]([math]::Floor($count * 0.95))]
        P99   = $sorted[[int]([math]::Floor($count * 0.99))]
        Mean  = ($sorted | Measure-Object -Average).Average
    }
}

function ReportScene([string]$label, $frames, [int]$warmup) {
    $tot = Percentiles ($frames | ForEach-Object { $_.TotalMs })  $warmup
    if ($null -eq $tot) { return $null }
    $upd = Percentiles ($frames | ForEach-Object { $_.UpdateMs }) $warmup
    $drw = Percentiles ($frames | ForEach-Object { $_.DrawMs })   $warmup
    $fps = if ($tot.Mean -gt 0) { 1000.0 / $tot.Mean } else { 0 }
    return [PSCustomObject]@{
        Scene      = $label
        Frames     = $tot.Count
        FPS_avg    = "{0:N1}" -f $fps
        Total_p50  = "{0:N2}" -f $tot.P50
        Total_p95  = "{0:N2}" -f $tot.P95
        Total_p99  = "{0:N2}" -f $tot.P99
        Update_p50 = "{0:N2}" -f $upd.P50
        Draw_p50   = "{0:N2}" -f $drw.P50
    }
}

$results = @()
foreach ($g in ($rows | Group-Object -Property TopScreen)) {
    if ($g.Name -eq "UniverseScreen" -and $CombatStartSec -gt 0) {
        $idle = $g.Group | Where-Object { [double]$_.WallSec -lt $CombatStartSec }
        $cmb  = $g.Group | Where-Object { [double]$_.WallSec -ge $CombatStartSec }
        $r1 = ReportScene "Universe (idle)" $idle $WarmupFrames
        $r2 = ReportScene "Combat"          $cmb  $WarmupFrames
        if ($r1) { $results += $r1 }
        if ($r2) { $results += $r2 }
    } else {
        $r = ReportScene $g.Name $g.Group $WarmupFrames
        if ($r) { $results += $r }
    }
}

$results | Format-Table -AutoSize
