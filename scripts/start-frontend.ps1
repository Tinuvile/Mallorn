#!/usr/bin/env pwsh
# Frontend Development Server Startup Script (with hot reload)

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

Write-Host "=== Starting Frontend Development Server ===" -ForegroundColor Green

try {
    # Navigate to frontend directory
    $frontendPath = "Frontend/campus-trade-web"
    if (-not (Test-Path $frontendPath)) {
        Write-Host "Error: Frontend project not found at $frontendPath" -ForegroundColor Red
        exit 1
    }
    
    Set-Location $frontendPath

    # Check if Node.js is installed
    try {
        $nodeVersion = node --version
        $npmVersion = npm --version
        Write-Host "Node.js version: $nodeVersion" -ForegroundColor Green
        Write-Host "npm version: $npmVersion" -ForegroundColor Green
    } catch {
        Write-Host "Error: Node.js or npm is not installed or not in PATH" -ForegroundColor Red
        Write-Host "Please install Node.js first: https://nodejs.org/" -ForegroundColor Yellow
        exit 1
    }

    # Check if package.json exists
    if (-not (Test-Path "package.json")) {
        Write-Host "Error: package.json not found in frontend directory" -ForegroundColor Red
        exit 1
    }

    # Install dependencies if node_modules doesn't exist or package.json was updated
    if (-not (Test-Path "node_modules") -or 
        (Get-Item "package.json").LastWriteTime -gt (Get-Item "node_modules" -ErrorAction SilentlyContinue).LastWriteTime) {
        Write-Host "Installing/updating frontend dependencies..." -ForegroundColor Yellow
        npm install
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Error: Failed to install dependencies" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "Dependencies are up to date" -ForegroundColor Green
    }

    # Check if backend API is accessible
    Write-Host "Checking backend API connection..." -ForegroundColor Yellow
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:5085/api/home/health" -Method GET -TimeoutSec 5
        Write-Host "Backend API is accessible at http://localhost:5085" -ForegroundColor Green
    } catch {
        Write-Host "Warning: Backend API at http://localhost:5085 is not responding" -ForegroundColor Yellow
        Write-Host "Make sure to start the backend first with: .\scripts\start-backend.ps1" -ForegroundColor Cyan
        
        $continueChoice = Read-Host "Continue starting frontend anyway? (y/n)"
        if ($continueChoice -ne 'y' -and $continueChoice -ne 'Y') {
            Write-Host "Exiting..." -ForegroundColor Yellow
            exit 0
        }
    }

    # Start development server with hot reload
    Write-Host "Starting frontend development server with hot reload..." -ForegroundColor Green
    Write-Host "Development server will be available at: http://localhost:5173" -ForegroundColor Cyan
    Write-Host "Press Ctrl+C to stop the development server" -ForegroundColor Yellow
    
    npm run dev
}
finally {
    # Always restore original directory
    Restore-Directory
}

Write-Host "Frontend development server stopped" -ForegroundColor Green 