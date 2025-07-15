# Campus Trading Platform Startup Script
# Mixed mode: Database and backend in Docker, frontend locally

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

Write-Host "=== Starting Campus Trading Platform ===" -ForegroundColor Green

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

    # Start database and backend API
    Write-Host "Starting database and backend API..." -ForegroundColor Yellow
    docker-compose up -d oracle-db campus-trade-api

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Failed to start Docker containers." -ForegroundColor Red
        exit 1
    }

    # Wait for backend API to start
    Write-Host "Waiting for backend API to start..." -ForegroundColor Yellow
    Start-Sleep -Seconds 30

    # Check if backend API is running
    Write-Host "Checking backend API status..." -ForegroundColor Yellow
    $maxRetries = 12
    $apiStarted = $false

    for ($i = 1; $i -le $maxRetries; $i++) {
        Write-Host "Trying to connect to backend API... ($i/$maxRetries)" -ForegroundColor Yellow
        
        try {
            $response = Invoke-RestMethod -Uri "http://localhost:5085/api/home/health" -Method GET -TimeoutSec 5
            if ($response) {
                Write-Host "Backend API started successfully!" -ForegroundColor Green
                $apiStarted = $true
                break
            }
        }
        catch {
            # Continue retrying
        }
        
        if ($i -lt $maxRetries) {
            Start-Sleep -Seconds 5
        }
    }

    if (-not $apiStarted) {
        Write-Host "Backend API startup timeout. Check logs:" -ForegroundColor Red
        Write-Host "docker-compose logs campus-trade-api" -ForegroundColor Cyan
        Write-Host "Continuing with frontend startup..." -ForegroundColor Yellow
    }

    # Start frontend
    Write-Host "Starting frontend development server..." -ForegroundColor Yellow

    # Check if frontend directory exists
    $frontendPath = "Frontend/campus-trade-web"
    if (-not (Test-Path $frontendPath)) {
        Write-Host "Error: Frontend directory not found: $frontendPath" -ForegroundColor Red
        exit 1
    }

    Set-Location $frontendPath

    # Check if Node.js is installed
    try {
        $nodeVersion = node --version
        Write-Host "Node.js version: $nodeVersion" -ForegroundColor Green
    }
    catch {
        Write-Host "Error: Node.js is not installed. Please install Node.js first." -ForegroundColor Red
        exit 1
    }

    # Install dependencies if needed
    if (-not (Test-Path "node_modules")) {
        Write-Host "Installing frontend dependencies..." -ForegroundColor Yellow
        npm install
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Error: Failed to install frontend dependencies." -ForegroundColor Red
            exit 1
        }
    }

    # Start frontend development server
    Write-Host "Starting frontend development server with hot reload..." -ForegroundColor Green
    Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Yellow

    Write-Host "=== Project startup complete ===" -ForegroundColor Green
    Write-Host "Frontend URL: http://localhost:5173" -ForegroundColor Cyan
    Write-Host "Backend API URL: http://localhost:5085" -ForegroundColor Cyan
    Write-Host "Health check: http://localhost:5085/api/home/health" -ForegroundColor Cyan

    # Start the development server
    npm run dev
}
finally {
    # Always restore original directory
    Restore-Directory
}

Write-Host "Platform services stopped" -ForegroundColor Green 