#!/usr/bin/env pwsh
# 停止校园交易平台所有服务

Write-Host "=== 停止校园交易平台所有服务 ===" -ForegroundColor Yellow

# 停止Docker服务
Write-Host "停止Docker容器..." -ForegroundColor Yellow
docker-compose down

# 检查并停止可能在运行的前端进程
Write-Host "检查前端开发服务器..." -ForegroundColor Yellow
$viteProcesses = Get-Process -Name "node" -ErrorAction SilentlyContinue | Where-Object {
    $_.CommandLine -like "*vite*" -or $_.CommandLine -like "*dev*"
}

if ($viteProcesses) {
    Write-Host "停止前端开发服务器..." -ForegroundColor Yellow
    $viteProcesses | Stop-Process -Force
    Write-Host "前端开发服务器已停止" -ForegroundColor Green
}

# 检查并停止可能在运行的后端进程
Write-Host "检查后端API进程..." -ForegroundColor Yellow
$dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
    $_.CommandLine -like "*CampusTrade.API*"
}

if ($dotnetProcesses) {
    Write-Host "停止后端API..." -ForegroundColor Yellow
    $dotnetProcesses | Stop-Process -Force
    Write-Host "后端API已停止" -ForegroundColor Green
}

# 显示端口占用情况
Write-Host "`n检查端口占用情况..." -ForegroundColor Yellow
$ports = @(5085, 5173, 1521, 3000)
foreach ($port in $ports) {
    $connection = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    if ($connection) {
        Write-Host "端口 $port 仍被占用，进程ID: $($connection.OwningProcess)" -ForegroundColor Red
        $process = Get-Process -Id $connection.OwningProcess -ErrorAction SilentlyContinue
        if ($process) {
            Write-Host "  进程名: $($process.ProcessName)" -ForegroundColor Red
        }
    } else {
        Write-Host "端口 $port 已释放" -ForegroundColor Green
    }
}

Write-Host "`n=== 所有服务已停止 ===" -ForegroundColor Green
Write-Host "如需清理Docker资源，可以运行：" -ForegroundColor Cyan
Write-Host "  docker system prune -f" -ForegroundColor White
Write-Host "如需删除数据卷，可以运行：" -ForegroundColor Cyan
Write-Host "  docker volume rm campustradingplatform_oracle_data" -ForegroundColor White 