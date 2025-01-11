param (
    [switch]$NoArchive,
    [string]$OutputDirectory = $PSScriptRoot,
    [string]$Configuration = "Release"
)

Set-Location "$PSScriptRoot"

# Source directory for built files
$BuildDir = "$PSScriptRoot/bin/$Configuration/net48"

# Files to include from build directory
$FilesToInclude = @(
    "info.json",
    "TwitchChat.dll"
)

# Get mod info from build output
$modInfo = Get-Content -Raw -Path "$BuildDir/info.json" | ConvertFrom-Json
$modId = $modInfo.Id
$modVersion = $modInfo.Version

# Setup directories
$DistDir = "$OutputDirectory/dist"
$ZipWorkDir = if ($NoArchive) { $OutputDirectory } else { "$DistDir/tmp" }
$ModDir = "$ZipWorkDir/TwitchChat"    # New line - create mod-named subfolder

# Create and clean output directory
if (Test-Path "$ZipWorkDir") {
    Remove-Item -Path "$ZipWorkDir" -Recurse -Force
}
New-Item "$ModDir" -ItemType Directory -Force | Out-Null    # Changed to create mod directory

# Copy required files from build directory
foreach ($file in $FilesToInclude) {
    $sourcePath = "$BuildDir/$file"
    if (Test-Path $sourcePath) {
        Copy-Item -Force -Path $sourcePath -Destination "$ModDir"    # Changed destination
    } else {
        Write-Warning "Build file not found: $sourcePath"
    }
}

# Create archive if requested
if (!$NoArchive) {
    $FILE_NAME = "$DistDir/${modId}_v$modVersion.zip"
    if (Test-Path $FILE_NAME) {
        Remove-Item -Path $FILE_NAME -Force
    }
    Compress-Archive -CompressionLevel Fastest -Path "$ModDir" -DestinationPath "$FILE_NAME"    # Changed path
    
    # Clean up temp directory
    Remove-Item -Path "$ZipWorkDir" -Recurse -Force
}

Write-Host "Package created successfully!" -ForegroundColor Green
