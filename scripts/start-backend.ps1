#!/usr/bin/env pwsh
# Backend API Startup Script

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

Write-Host "=== Starting Backend API Project ===" -ForegroundColor Green

# Choose startup method
Write-Host "Please choose startup method:" -ForegroundColor Yellow
Write-Host "1. Docker mode (includes database)" -ForegroundColor White
Write-Host "2. Local .NET mode (requires external database)" -ForegroundColor White
$choice = Read-Host "Enter your choice (1 or 2)"

try {
    switch ($choice) {
        "1" {
            Write-Host "Using Docker mode to start backend..." -ForegroundColor Yellow
            
            # Check if Docker is running
            try {
                docker version | Out-Null
            }
            catch {
                Write-Host "Error: Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
                exit 1
            }
            
            # Start database and backend API
            docker-compose up -d oracle-db campus-trade-api
            
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Error: Failed to start Docker containers." -ForegroundColor Red
                exit 1
            }
            
            Write-Host "Backend API is starting..." -ForegroundColor Yellow
            Start-Sleep -Seconds 30
            
            # Check if backend is responding
            Write-Host "Checking backend health..." -ForegroundColor Yellow
            $maxRetries = 12
            $retryCount = 0
            $backendReady = $false
            
            while ($retryCount -lt $maxRetries -and -not $backendReady) {
                try {
                    $response = Invoke-RestMethod -Uri "http://localhost:5085/api/home/health" -Method GET -TimeoutSec 5
                    if ($response) {
                        Write-Host "Backend is ready and responding!" -ForegroundColor Green
                        $backendReady = $true
                    }
                }
                catch {
                    $retryCount++
                    Write-Host "Backend not ready yet... (attempt $retryCount/$maxRetries)" -ForegroundColor Yellow
                    Start-Sleep -Seconds 5
                }
            }
            
            if ($backendReady) {
                Write-Host "`nBackend started successfully!" -ForegroundColor Green
                Write-Host "API: http://localhost:5085" -ForegroundColor Cyan
                Write-Host "Health check: http://localhost:5085/api/home/health" -ForegroundColor Cyan
                Write-Host "Database: Oracle XE on localhost:1521" -ForegroundColor Cyan
                Write-Host "`nPress Ctrl+C to stop the services" -ForegroundColor Yellow
                
                # Show logs
                Write-Host "=== Backend API Logs ===" -ForegroundColor Green
                docker-compose logs -f campus-trade-api
            }
            else {
                Write-Host "Warning: Backend may not be fully ready. Check logs:" -ForegroundColor Yellow
                Write-Host "docker-compose logs campus-trade-api" -ForegroundColor Cyan
            }
        }
        
        "2" {
            Write-Host "Using local .NET mode to start backend..." -ForegroundColor Yellow
            
            # Navigate to backend API directory
            $apiPath = "Backend/CampusTradeSystem/CampusTrade.API"
            if (-not (Test-Path $apiPath)) {
                Write-Host "Error: API project not found at $apiPath" -ForegroundColor Red
                exit 1
            }
            
            Set-Location $apiPath
            
            # Check if .NET is installed
            try {
                $dotnetVersion = dotnet --version
                Write-Host ".NET version: $dotnetVersion" -ForegroundColor Green
            } catch {
                Write-Host "Error: .NET SDK not found. Please install .NET 8 SDK." -ForegroundColor Red
                exit 1
            }
            
            # Restore NuGet packages
            Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
            dotnet restore
            
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Error: Failed to restore NuGet packages." -ForegroundColor Red
                exit 1
            }
            
            # Build project
            Write-Host "Building project..." -ForegroundColor Yellow
            dotnet build
            
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Error: Failed to build project." -ForegroundColor Red
                exit 1
            }
            
            # Check if Oracle database is available
            Write-Host "Checking if Oracle database is accessible..." -ForegroundColor Yellow
            Write-Host "Note: Please ensure Oracle database is accessible at localhost:1521" -ForegroundColor Yellow
            Write-Host "You can start it with: docker-compose up -d oracle-db" -ForegroundColor Cyan
            
            # Run project with hot reload
            Write-Host "Starting backend API with hot reload..." -ForegroundColor Green
            dotnet watch run
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

Write-Host "=== Backend API startup complete ===" -ForegroundColor Green 