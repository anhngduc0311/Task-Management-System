param(
    [string]$ContainerName = "task-management-db",
    [string]$DatabaseName = "TaskManagementDb",
    [string]$BackupDir = "Backups"
)

$projectRoot = Resolve-Path "$PSScriptRoot\..\.."
$backupPath = Join-Path $projectRoot $BackupDir

if (!(Test-Path $backupPath)) {
    New-Item -ItemType Directory -Force -Path $backupPath | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFileName = "${DatabaseName}_${timestamp}.bak"

# Check if Docker container is running
$dockerStatus = & docker inspect --format="{{.State.Status}}" $ContainerName 2>$null
if ($dockerStatus -eq "running") {
    Write-Host "Docker container '$ContainerName' is running. Executing backup inside container..."
    
    # Run backup command in MSSQL container
    $sqlCmd = "BACKUP DATABASE [$DatabaseName] TO DISK = N'/var/opt/mssql/${backupFileName}' WITH NOFORMAT, NOINIT, NAME = '${DatabaseName}-Full Database Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10"
    & docker exec -t $ContainerName /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "sa_Password123!" -Q $sqlCmd -C | Out-Null
    
    # Copy from container to host backup dir
    $containerBackupPath = "/var/opt/mssql/${backupFileName}"
    $hostBackupFile = Join-Path $backupPath $backupFileName
    & docker cp "${ContainerName}:${containerBackupPath}" $hostBackupFile
    
    # Clean up inside container
    & docker exec -t $ContainerName rm -f $containerBackupPath
    
    Write-Host "Database backup completed successfully! Saved to: $hostBackupFile"
}
else {
    # Attempt local host backup
    Write-Host "Docker container is not running. Attempting host SQL Server backup..."
    $hostBackupFile = Join-Path $backupPath $backupFileName
    $sqlCmd = "BACKUP DATABASE [$DatabaseName] TO DISK = N'${hostBackupFile}' WITH NOFORMAT, INIT, NAME = '${DatabaseName}-Full Database Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10"
    
    try {
        & sqlcmd -S "localhost" -U "sa" -P "sa_Password123!" -Q $sqlCmd -C | Out-Null
        Write-Host "Database backup completed successfully on host! Saved to: $hostBackupFile"
    } catch {
        Write-Error "Failed to perform database backup. Make sure SQL Server is running (either locally or in Docker)."
    }
}
