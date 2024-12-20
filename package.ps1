param (
    [switch]$NoArchive,
    [string]$OutputDirectory = $PSScriptRoot
)

Set-Location "$PSScriptRoot"

# Expanded list of files to include
$FilesToInclude = @(
    "info.json"
)

# Get mod info
$modInfo = Get-Content -Raw -Path "info.json" | ConvertFrom-Json
$modId = $modInfo.Id
$modVersion = $modInfo.Version

# Setup directories
$DistDir = "$OutputDirectory/dist"
$ZipWorkDir = if ($NoArchive) { $OutputDirectory } else { "$DistDir/tmp" }
$ZipOutDir = "$ZipWorkDir/$modId"

# Create and clean output directory
if (Test-Path "$ZipOutDir") {
    Remove-Item -Path "$ZipOutDir" -Recurse -Force
}
New-Item "$ZipOutDir" -ItemType Directory -Force | Out-Null

# Copy all required files
foreach ($file in $FilesToInclude) {
    if (Test-Path $file) {
        Copy-Item -Force -Path $file -Destination "$ZipOutDir" -Recurse
    } else {
        Write-Warning "File not found: $file"
    }
}

# Create archive if requested
if (!$NoArchive) {
    $FILE_NAME = "$DistDir/${modId}_v$modVersion.zip"
    if (Test-Path $FILE_NAME) {
        Remove-Item -Path $FILE_NAME -Force
    }
    Compress-Archive -CompressionLevel Fastest -Path "$ZipOutDir/*" -DestinationPath "$FILE_NAME"
    
    # Clean up temp directory
    Remove-Item -Path "$ZipWorkDir" -Recurse -Force
}

Write-Host "Package created successfully!" -ForegroundColor Green
