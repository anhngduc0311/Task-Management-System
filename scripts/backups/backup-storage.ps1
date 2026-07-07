param(
    [string]$UploadDir = "TaskManagement.API/Uploads",
    [string]$BackupDir = "Backups"
)

$projectRoot = Resolve-Path "$PSScriptRoot\..\.."
$sourcePath = Join-Path $projectRoot $UploadDir
$backupPath = Join-Path $projectRoot $BackupDir

# Check if upload directory exists
if (!(Test-Path $sourcePath)) {
    Write-Warning "Source upload directory does not exist: $sourcePath"
    exit
}

# Create backup directory if it doesn't exist
if (!(Test-Path $backupPath)) {
    New-Item -ItemType Directory -Force -Path $backupPath | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$zipFileName = "Uploads_${timestamp}.zip"
$destinationZip = Join-Path $backupPath $zipFileName

Write-Host "Backing up file storage from: $sourcePath"
Write-Host "Compressing to: $destinationZip"

try {
    # Compress the folder to zip
    Compress-Archive -Path "$sourcePath\*" -DestinationPath $destinationZip -Force
    Write-Host "Storage backup completed successfully!"
} catch {
    Write-Error "Failed to compress storage directory. Exception: $_"
}
