# Génère les sons de l'app (WAV 16 bits mono) par synthèse : pas de fichier sous licence à embarquer.
# Usage : powershell -File tools/GenerateurSons.ps1
param([string] $OutDir = (Join-Path $PSScriptRoot "..\src\QuizzWare\Resources\Raw\sons"))

$rate = 44100

# Une note : fondamentale + harmoniques, attaque courte puis décroissance exponentielle.
function New-Note([double] $freq, [double] $duration, [double] $volume = 0.5, [double] $decay = 6, [double[]] $harmonics = @(1, 0.35, 0.12)) {
    $n = [int]($rate * $duration)
    $samples = New-Object double[] $n
    $attack = [int]($rate * 0.005)
    for ($i = 0; $i -lt $n; $i++) {
        $t = $i / $rate
        $v = 0.0
        for ($h = 0; $h -lt $harmonics.Length; $h++) {
            $v += $harmonics[$h] * [Math]::Sin(2 * [Math]::PI * $freq * ($h + 1) * $t)
        }
        $env = [Math]::Exp(-$decay * $t)
        if ($i -lt $attack) { $env *= $i / $attack }
        $samples[$i] = $v * $env * $volume
    }
    , $samples
}

# Place des notes sur une piste : chaque élément = @(début en secondes, échantillons).
function Join-Notes([object[]] $notes, [double] $total) {
    $mix = New-Object double[] ([int]($rate * $total))
    foreach ($note in $notes) {
        $start = [int]($rate * $note[0])
        $samples = $note[1]
        for ($i = 0; $i -lt $samples.Length -and $start + $i -lt $mix.Length; $i++) { $mix[$start + $i] += $samples[$i] }
    }
    , $mix
}

function Save-Wav([double[]] $samples, [string] $path) {
    # Normalisation douce pour éviter la saturation.
    $peak = ($samples | ForEach-Object { [Math]::Abs($_) } | Measure-Object -Maximum).Maximum
    $gain = if ($peak -gt 0.9) { 0.9 / $peak } else { 1.0 }
    $data = New-Object byte[] ($samples.Length * 2)
    for ($i = 0; $i -lt $samples.Length; $i++) {
        $s = [int16][Math]::Max(-32767, [Math]::Min(32767, $samples[$i] * $gain * 32767))
        $b = [BitConverter]::GetBytes($s)
        $data[2 * $i] = $b[0]; $data[2 * $i + 1] = $b[1]
    }
    $stream = [IO.File]::Create($path)
    $w = New-Object IO.BinaryWriter $stream
    $w.Write([Text.Encoding]::ASCII.GetBytes("RIFF")); $w.Write([int](36 + $data.Length))
    $w.Write([Text.Encoding]::ASCII.GetBytes("WAVEfmt ")); $w.Write([int]16); $w.Write([int16]1); $w.Write([int16]1)
    $w.Write([int]$rate); $w.Write([int]($rate * 2)); $w.Write([int16]2); $w.Write([int16]16)
    $w.Write([Text.Encoding]::ASCII.GetBytes("data")); $w.Write([int]$data.Length); $w.Write($data)
    $w.Close()
    "ok : $path"
}

New-Item -ItemType Directory -Force $OutDir | Out-Null

# Bonne réponse : deux notes montantes, claires (do6 → mi6).
Save-Wav (Join-Notes @(
        @(0.00, (New-Note 1046.5 0.25 0.45 14)),
        @(0.08, (New-Note 1318.5 0.40 0.50 9))
    ) 0.5) (Join-Path $OutDir "correct.wav")

# Mauvaise réponse : deux notes graves descendantes, plus douces.
Save-Wav (Join-Notes @(
        @(0.00, (New-Note 311.1 0.22 0.40 10 @(1, 0.5, 0.3, 0.15))),
        @(0.12, (New-Note 233.1 0.35 0.40 8 @(1, 0.5, 0.3, 0.15)))
    ) 0.5) (Join-Path $OutDir "wrong.wav")

# Fin de leçon : arpège do-mi-sol-do.
Save-Wav (Join-Notes @(
        @(0.00, (New-Note 523.3 0.40 0.35 7)),
        @(0.10, (New-Note 659.3 0.40 0.35 7)),
        @(0.20, (New-Note 784.0 0.40 0.35 7)),
        @(0.30, (New-Note 1046.5 0.80 0.45 4))
    ) 1.1) (Join-Path $OutDir "complete.wav")
