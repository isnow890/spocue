[CmdletBinding()]
param(
    [string]$Version,
    [string]$MakeAppx
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src/TrackRelay/TrackRelay.csproj'
$projectVersion = ([xml](Get-Content -Raw $project)).Project.PropertyGroup.Version
if (!$Version) { $Version = $projectVersion }
$Version = $Version.TrimStart('v')
if ($Version -cne $projectVersion -or $Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Expected the project release version $projectVersion, got $Version."
}

if (!$MakeAppx) {
    $MakeAppx = (Get-Command makeappx.exe -ErrorAction SilentlyContinue).Source
    if (!$MakeAppx) {
        $kit = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots' -ErrorAction SilentlyContinue).KitsRoot10
        if ($kit) {
            $MakeAppx = Get-ChildItem (Join-Path $kit 'bin/*/x64/makeappx.exe') |
                Sort-Object { [version]$_.Directory.Parent.Name } -Descending |
                Select-Object -First 1 -ExpandProperty FullName
        }
    }
}
if (!$MakeAppx) { throw 'Install Windows SDK or specify -MakeAppx with the path to makeappx.exe.' }

$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')
$work = Join-Path $tempRoot ("spocue-msix-" + [guid]::NewGuid())
$stage = Join-Path $work 'payload'
$outDir = Join-Path $root 'artifacts/store'
$output = Join-Path $outDir "SpoCue-$Version-x64-submission.msix"
New-Item -ItemType Directory -Path $stage | Out-Null
try {
    dotnet publish $project -c Release -r win-x64 --self-contained true --artifacts-path (Join-Path $work 'build') -p:StoreBuild=true -p:DebugType=None -p:DebugSymbols=false -o $stage
    if ($LASTEXITCODE) { throw 'Store publish failed.' }

    $manifest = [xml](Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'MSIX/AppxManifest.xml'))
    $manifest.Package.Identity.Version = "$Version.0"
    $manifest.Save((Join-Path $stage 'AppxManifest.xml'))
    Copy-Item (Join-Path $PSScriptRoot 'MSIX/Assets') $stage -Recurse
    $package = Join-Path $work 'submission.msix'
    & $MakeAppx pack /d $stage /p $package
    if ($LASTEXITCODE) { throw 'MSIX packaging failed.' }
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    Copy-Item -LiteralPath $package -Destination $output -Force
    Write-Output $output
} finally {
    if ((Split-Path -Parent $work) -eq $tempRoot -and (Split-Path -Leaf $work) -match '^spocue-msix-[0-9a-f-]{36}$') {
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
