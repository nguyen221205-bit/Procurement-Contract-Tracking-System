# Set console UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$projectDir = "D:\Thuc_Tap_backend\Procurement & Contract Tracking System"
$backendDir = Join-Path $projectDir "ProcurementSystem.API"
$frontendDir = Join-Path $projectDir "frontend"

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "   HE THONG MUA SAM & DAU THAU DIEN TU (PROCUREMENT SYSTEM)" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host ""

# 0. Kiem tra dich vu SQL Server
Write-Host "[1/5] Kiem tra dich vu SQL Server (MSSQLSERVER)..." -ForegroundColor Yellow
$sqlService = Get-Service -Name MSSQLSERVER -ErrorAction SilentlyContinue
if ($sqlService -and $sqlService.Status -ne 'Running') {
    Write-Host "      [CANH BAO] Dich vu SQL Server dang TAT (Stopped)!" -ForegroundColor Red
    Write-Host "      Dang mo hop thoai yeu cau bat SQL Server..." -ForegroundColor Yellow
    $batSql = Join-Path $projectDir "Bat-SQL-Server.bat"
    Start-Process -FilePath $batSql -Wait
    Start-Sleep -Seconds 2
} else {
    Write-Host "      + SQL Server da san sang." -ForegroundColor Green
}

# 1. Kiem tra va don dep cong neu bi treo
Write-Host "[2/5] Kiem tra va don dep cong ket noi 5225 & 5173..." -ForegroundColor Yellow
$pids = Get-NetTCPConnection -LocalPort 5225, 5173 -State Listen -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique
if ($pids) {
    foreach ($p in $pids) {
        Write-Host "      - Dang tat tien trinh cu (PID: $p)..." -ForegroundColor Gray
        Stop-Process -Id $p -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 1
}

# 2. Khoi dong Backend API trong cua so moi
Write-Host "[3/5] Dang khoi dong Backend API (.NET 8 - Port 5225)..." -ForegroundColor Yellow
Start-Process -FilePath "powershell.exe" -ArgumentList "-NoExit -Command & { Write-Host '=== BACKEND API (Port 5225) ===' -ForegroundColor Cyan; cd '$backendDir'; dotnet run --launch-profile http }" -WorkingDirectory $backendDir

# 3. Khoi dong Frontend SPA trong cua so moi
Write-Host "[4/5] Dang khoi dong Frontend SPA (React Vite - Port 5173)..." -ForegroundColor Yellow
Start-Process -FilePath "powershell.exe" -ArgumentList "-NoExit -Command & { Write-Host '=== FRONTEND SPA (Port 5173) ===' -ForegroundColor Green; cd '$frontendDir'; npm run dev }" -WorkingDirectory $frontendDir

# 4. Kiem tra ket noi den khi ca 2 san sang
Write-Host "[5/5] Dang kiem tra ket noi..." -ForegroundColor Yellow
$backendReady = $false
$frontendReady = $false

for ($i = 1; $i -le 30; $i++) {
    Start-Sleep -Seconds 1
    if (-not $backendReady) {
        $checkBackend = Get-NetTCPConnection -LocalPort 5225 -State Listen -ErrorAction SilentlyContinue
        if ($checkBackend) {
            $backendReady = $true
            Write-Host "      + Backend API da san sang tren http://localhost:5225" -ForegroundColor Green
        }
    }
    if (-not $frontendReady) {
        $checkFrontend = Get-NetTCPConnection -LocalPort 5173 -State Listen -ErrorAction SilentlyContinue
        if ($checkFrontend) {
            $frontendReady = $true
            Write-Host "      + Frontend SPA da san sang tren http://localhost:5173" -ForegroundColor Green
        }
    }
    if ($backendReady -and $frontendReady) {
        break
    }
}

Write-Host ""
Write-Host "==================================================================" -ForegroundColor Green
Write-Host "   KHOI DONG THANH CONG TOAN BO HE THONG!" -ForegroundColor Green
Write-Host "   - Backend API : http://localhost:5225 (Swagger: /swagger)" -ForegroundColor White
Write-Host "   - Frontend Web: http://localhost:5173" -ForegroundColor White
Write-Host "==================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Dang mo trinh duyet truy cap: http://localhost:5173 ..." -ForegroundColor Cyan
Start-Process "http://localhost:5173"

Write-Host ""
Write-Host "Meo: De dung he thong, hay chay shortcut 'Dung He Thong' hoac stop-system.bat" -ForegroundColor Gray
Write-Host "Cua so dieu khien nay se tu dong dong sau 6 giay..." -ForegroundColor Gray
Start-Sleep -Seconds 6
