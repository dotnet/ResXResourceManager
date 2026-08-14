#Requires -Version 5.1
<#
.SYNOPSIS
  Updates the winget manifest templates with the current version and SHA-256.

.DESCRIPTION
  Reads the version from Directory.Build.props, downloads the release ZIP from GitHub,
  computes its SHA-256, and writes updated copies of the three manifest YAML files into
  a 'manifests' sub-folder ready for a manual PR to microsoft/winget-pkgs.

.EXAMPLE
  .\Update-WingetManifest.ps1

  Uses the version from Directory.Build.props.

.EXAMPLE
  .\Update-WingetManifest.ps1 -Version 1.108.0
#>
param(
    [string] $Version
)

$wingetRoot = "D:\dev\GitHub\winget-pkgs"
$outRoot = "$wingetRoot\manifests\d\dotnet\ResXResourceManager"

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $outRoot -PathType Container)) {
    Write-Host "Fork the winget-pkgs into $wingetRoot first."
    exit 1
}

$scriptDir   = $PSScriptRoot
$repoRoot    = Resolve-Path "$scriptDir\..\.."
$propsFile   = "$repoRoot\src\Directory.Build.props"

# --- Resolve version -----------------------------------------------------------
if (-not $Version) {
    [xml]$props = Get-Content $propsFile
    $raw = ($props.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
    # Strip trailing .0 build component: "1.107.0.0" -> "1.107.0"
    $Version = ($raw -replace '\.\d+$', '')
}

$tag         = ($Version -replace '\.\d+$', '')
$zipUrl      = "https://github.com/dotnet/ResXResourceManager/releases/download/$tag/ResXManager.zip"

Write-Host "Version : $Version"
Write-Host "Tag     : $tag"
Write-Host "ZIP URL : $zipUrl"

$outDir      = "$outRoot\$Version\"

# --- Download and hash ---------------------------------------------------------
$tmpZip = [System.IO.Path]::GetTempFileName() + '.zip'
try {
    Write-Host "Downloading ZIP for SHA-256 computation..."
    Invoke-WebRequest -Uri $zipUrl -OutFile $tmpZip -UseBasicParsing
    $sha256 = (Get-FileHash $tmpZip -Algorithm SHA256).Hash
}
finally {
    Remove-Item $tmpZip -ErrorAction SilentlyContinue
}

Write-Host "SHA-256 : $sha256"

# --- Write updated manifests ---------------------------------------------------
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$templates = Get-ChildItem "$scriptDir\*.yaml"
foreach ($template in $templates) {
    $content = Get-Content $template.FullName -Raw
    $content = $content -replace 'PackageVersion: .+',      "PackageVersion: $Version"
    $content = $content -replace 'InstallerUrl: .+',        "InstallerUrl: $zipUrl"
    $content = $content -replace 'InstallerSha256: .+',     "InstallerSha256: $sha256"
    $dest = Join-Path $outDir $template.Name
    Set-Content -Path $dest -Value $content.TrimEnd() -Encoding UTF8
    Write-Host "Written : $dest"
}

Write-Host ""
Write-Host "Manifests are ready in: $outDir"
