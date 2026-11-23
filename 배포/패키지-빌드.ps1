param(
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = 'Stop'
$Version = $Version.TrimStart('v')

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    $releaseDir = Join-Path $repoRoot "artifacts\release"
    $publishDir = Join-Path $releaseDir "spocue-win-x64"
    $zipName = "spocue-v$Version-win-x64.zip"
    $zipPath = Join-Path $releaseDir $zipName
    $setupName = "spocue-v$Version-win-x64-setup.exe"
    $setupPath = Join-Path $releaseDir $setupName
    $sumsPath = Join-Path $releaseDir "SHA256SUMS.txt"

    if (Test-Path -LiteralPath $publishDir) {
        Remove-Item -LiteralPath $publishDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null

    Write-Host "==> [1/4] Publishing self-contained win-x64 build (v$Version)..."
    dotnet publish src/TrackRelay/TrackRelay.csproj `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:Version=$Version `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -o $publishDir `
        --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

    Write-Host "==> [2/4] Creating portable ZIP ($zipName)..."
    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::CreateFromDirectory(
        $publishDir,
        $zipPath,
        [IO.Compression.CompressionLevel]::Optimal,
        $true
    )

    Write-Host "==> [3/4] Compiling Windows Setup installer ($setupName)..."
    $isccCandidates = @(
        @(
            (Get-Command iscc -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
            "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
            "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
            "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
        ) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }
    )

    if ($isccCandidates.Count -eq 0) {
        throw "Inno Setup 6 (ISCC.exe) not found."
    }
    $iscc = [string]$isccCandidates[0]
    $issPath = Join-Path $PSScriptRoot "설치스크립트.iss"
    & $iscc "/DMyAppVersion=$Version" "/DPublishDir=$publishDir" "/DOutputDir=$releaseDir" $issPath
    if ($LASTEXITCODE -ne 0) { throw "ISCC failed with exit code $LASTEXITCODE" }

    Write-Host "==> [4/4] Generating SHA256SUMS.txt..."
    $lines = foreach ($file in @($setupPath, $zipPath)) {
        $hash = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
        $leaf = Split-Path -Leaf $file
        "$hash  $leaf"
    }
    [IO.File]::WriteAllLines($sumsPath, $lines, [Text.UTF8Encoding]::new($false))

    Write-Host "==> Done! Artifacts:"
    Get-ChildItem -LiteralPath $setupPath, $zipPath, $sumsPath | Select-Object Name, Length
} finally {
    Pop-Location
}
