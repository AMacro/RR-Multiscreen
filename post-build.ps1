param
(   
    [switch]$NoArchive,
    [string]$GameDir,
    [string]$Target,
    [string]$Ver
)

$rootDir = $PSScriptRoot
$buildDir = Join-Path $rootDir "build"

# Update the JSON
$jsonPath = Join-Path $rootDir "info.json"
$json = Get-Content $jsonPath -Raw | ConvertFrom-Json
$json.Version = $Ver
$modId = $json.Id
$json | ConvertTo-Json -Depth 32 | Set-Content $jsonPath

# Copy files to Build Dir
Copy-Item $jsonPath -Destination $buildDir
Copy-Item (Join-Path $rootDir "LICENSE") -Destination $buildDir

# Copy files to Game Dir
if (!(Test-Path $GameDir)) {
    New-Item -ItemType Directory -Path $GameDir -Force
}
Copy-Item "$buildDir\*" -Destination $GameDir -Recurse -Force

# Prepare for compression
$compressPath = Join-Path $rootDir "Releases\$modId $Ver.zip"
$compress = @{
    Path = "$buildDir\*"
    CompressionLevel = "Fastest"
    DestinationPath = $compressPath
}

# Create release package if not in debug mode
if (!$NoArchive) {
    $releaseDir = Join-Path $rootDir "Releases"
    if (!(Test-Path $releaseDir)) {
        New-Item -ItemType Directory -Path $releaseDir -Force
    }
    
    Write-Host "Creating release package: $compressPath"
    Compress-Archive @compress -Force
}
