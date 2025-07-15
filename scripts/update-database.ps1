#!/usr/bin/env pwsh
# Database Update Tool - Find SQL files in Database directory and update database

# Save original directory
$originalDir = Get-Location

# Ensure we return to original directory on script exit
function Restore-Directory {
    Set-Location $originalDir
}

# Set up trap to restore directory on any exit
trap {
    Restore-Directory
    break
}

Write-Host "=== Database Update Tool ===" -ForegroundColor Green

try {
    # Check if Docker is running
    Write-Host "Checking Docker status..." -ForegroundColor Yellow
    try {
        docker version | Out-Null
    }
    catch {
        Write-Host "Error: Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
        exit 1
    }

    # Check if Oracle container is running
    Write-Host "Checking Oracle database container status..." -ForegroundColor Yellow
    $containerRunning = docker ps --filter "name=campus-trade-oracle" --filter "status=running" --quiet 2>$null
    
    if (-not $containerRunning) {
        Write-Host "Oracle database container is not running." -ForegroundColor Yellow
        Write-Host "This script requires the Oracle database container to be active." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "To start the database container, run:" -ForegroundColor Cyan
        Write-Host "  docker-compose up -d oracle-db" -ForegroundColor White
        Write-Host ""
        Write-Host "Or start the complete backend with:" -ForegroundColor Cyan
        Write-Host "  .\scripts\start-backend.ps1" -ForegroundColor White
        Write-Host ""
        
        $startChoice = Read-Host "Would you like to start the Oracle database now? (y/n)"
        if ($startChoice -eq 'y' -or $startChoice -eq 'Y') {
            Write-Host "Starting Oracle database container..." -ForegroundColor Yellow
            docker-compose up -d oracle-db
            
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Error: Failed to start Oracle database container." -ForegroundColor Red
                exit 1
            }
            
            Write-Host "Waiting for database to initialize..." -ForegroundColor Yellow
            Start-Sleep -Seconds 30
            
            # Wait for database to be ready
            $maxWait = 60
            $waited = 0
            $dbReady = $false
            
            while ($waited -lt $maxWait -and -not $dbReady) {
                try {
                    $listenerCheck = docker exec campus-trade-oracle lsnrctl status 2>$null
                    if ($listenerCheck -match "XEPDB1.*READY") {
                        Write-Host "Database is ready!" -ForegroundColor Green
                        $dbReady = $true
                        break
                    }
                }
                catch {
                    # Continue waiting
                }
                
                Write-Host "Database still initializing... (waited $waited seconds)" -ForegroundColor Yellow
                Start-Sleep -Seconds 10
                $waited += 10
            }
            
            if (-not $dbReady) {
                Write-Host "Warning: Database may not be fully ready yet. Continuing anyway..." -ForegroundColor Yellow
            }
        }
        else {
            Write-Host "Exiting. Please start the Oracle database container first." -ForegroundColor Yellow
            exit 0
        }
    }
    else {
        Write-Host "Oracle database container is running!" -ForegroundColor Green
    }

    # Check Database directory
    $databaseDir = "Database"
    if (-not (Test-Path $databaseDir)) {
        Write-Host "Error: Database directory not found at $databaseDir" -ForegroundColor Red
        exit 1
    }

    # Find SQL files
    Write-Host "Looking for SQL files..." -ForegroundColor Yellow
    $sqlFiles = Get-ChildItem -Path $databaseDir -Filter "*.sql" | Sort-Object Name

    if ($sqlFiles.Count -eq 0) {
        Write-Host "No SQL files found in $databaseDir directory" -ForegroundColor Red
        exit 1
    }

    Write-Host "Found the following SQL files:" -ForegroundColor Green
    for ($i = 0; $i -lt $sqlFiles.Count; $i++) {
        $file = $sqlFiles[$i]
        $size = [math]::Round($file.Length / 1KB, 2)
        Write-Host "  [$($i+1)] $($file.Name) ($size KB)" -ForegroundColor White
    }

    # Choose operation
    Write-Host "`nPlease choose an operation:" -ForegroundColor Yellow
    Write-Host "1. Reinitialize database (delete existing data, recreate)" -ForegroundColor White
    Write-Host "2. Execute specific SQL file" -ForegroundColor White
    Write-Host "3. Connect to database for custom SQL" -ForegroundColor White
    Write-Host "4. View SQL file content" -ForegroundColor White
    $choice = Read-Host "Enter your choice (1-4)"

    switch ($choice) {
        "1" {
            Write-Host "Reinitializing database..." -ForegroundColor Yellow
            Write-Host "Warning: This will delete all existing data!" -ForegroundColor Red
            $confirm = Read-Host "Confirm to continue? (y/N)"
            
            if ($confirm -eq "y" -or $confirm -eq "Y") {
                # Stop and remove database container
                Write-Host "Stopping database container..." -ForegroundColor Yellow
                docker-compose down oracle-db
                
                # Remove data volume
                Write-Host "Removing data volume..." -ForegroundColor Yellow
                docker volume rm campustradingplatform_oracle_data -f
                
                # Restart database container
                Write-Host "Starting fresh database container..." -ForegroundColor Yellow
                docker-compose up -d oracle-db
                
                # Wait for initialization
                Write-Host "Waiting for database to initialize (this may take a few minutes)..." -ForegroundColor Yellow
                Start-Sleep -Seconds 60
                
                Write-Host "Database reinitialization complete!" -ForegroundColor Green
            } else {
                Write-Host "Operation cancelled" -ForegroundColor Yellow
            }
        }
        
        "2" {
            Write-Host "Choose SQL file to execute:" -ForegroundColor Yellow
            for ($i = 0; $i -lt $sqlFiles.Count; $i++) {
                Write-Host "  [$($i+1)] $($sqlFiles[$i].Name)" -ForegroundColor White
            }
            
            $fileChoice = Read-Host "Enter file number (1-$($sqlFiles.Count))"
            $fileIndex = [int]$fileChoice - 1
            
            if ($fileIndex -ge 0 -and $fileIndex -lt $sqlFiles.Count) {
                $selectedFile = $sqlFiles[$fileIndex]
                $filePath = Join-Path $databaseDir $selectedFile.Name
                
                Write-Host "Executing SQL file: $($selectedFile.Name)" -ForegroundColor Yellow
                
                # Copy file to container and execute
                docker cp $filePath campus-trade-oracle:/tmp/execute.sql
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "File copied successfully, executing SQL..." -ForegroundColor Yellow
                    docker exec -i campus-trade-oracle sqlplus "CAMPUS_TRADE_USER/CampusTrade123!@//localhost:1521/XEPDB1" "@/tmp/execute.sql"
                    
                    if ($LASTEXITCODE -eq 0) {
                        Write-Host "SQL file execution complete!" -ForegroundColor Green
                    } else {
                        Write-Host "Error: SQL file execution failed!" -ForegroundColor Red
                    }
                } else {
                    Write-Host "Error: Failed to copy file to container!" -ForegroundColor Red
                }
            } else {
                Write-Host "Invalid file number" -ForegroundColor Red
            }
        }
        
        "3" {
            Write-Host "Connecting to Oracle database..." -ForegroundColor Yellow
            Write-Host "Enter SQL commands, type 'exit' to quit" -ForegroundColor Cyan
            Write-Host "Database information:" -ForegroundColor White
            Write-Host "  User: CAMPUS_TRADE_USER" -ForegroundColor White
            Write-Host "  Database: XEPDB1" -ForegroundColor White
            
            # Start interactive SQL session
            Write-Host "Starting interactive SQL session..." -ForegroundColor Yellow
            docker exec -it campus-trade-oracle sqlplus "CAMPUS_TRADE_USER/CampusTrade123!@//localhost:1521/XEPDB1"
            
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Error: Failed to connect to database!" -ForegroundColor Red
            }
        }
        
        "4" {
            Write-Host "Choose SQL file to view:" -ForegroundColor Yellow
            for ($i = 0; $i -lt $sqlFiles.Count; $i++) {
                Write-Host "  [$($i+1)] $($sqlFiles[$i].Name)" -ForegroundColor White
            }
            
            $fileChoice = Read-Host "Enter file number (1-$($sqlFiles.Count))"
            $fileIndex = [int]$fileChoice - 1
            
            if ($fileIndex -ge 0 -and $fileIndex -lt $sqlFiles.Count) {
                $selectedFile = $sqlFiles[$fileIndex]
                $filePath = Join-Path $databaseDir $selectedFile.Name
                
                Write-Host "=== $($selectedFile.Name) Content ===" -ForegroundColor Green
                Get-Content $filePath | Write-Host
                Write-Host "=== End of File ===" -ForegroundColor Green
            } else {
                Write-Host "Invalid file number" -ForegroundColor Red
            }
        }
        
        default {
            Write-Host "Invalid choice, exiting" -ForegroundColor Red
            exit 1
        }
    }
}
finally {
    # Always restore original directory
    Restore-Directory
}

Write-Host "Database operation complete!" -ForegroundColor Green 