# Set console UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "==================================================================" -ForegroundColor Red
Write-Host "   DANG DUNG TOAN BO HE THONG DAU THAU..." -ForegroundColor Red
Write-Host "==================================================================" -ForegroundColor Red
Write-Host ""

# 1. Tim va dung cac tien trinh tren cong 5225 va 5173
$pids = Get-NetTCPConnection -LocalPort 5225, 5173 -State Listen -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique
if ($pids) {
    foreach ($p in $pids) {
        $proc = Get-Process -Id $p -ErrorAction SilentlyContinue
        $pName = if ($proc) { $proc.ProcessName } else { "Unknown" }
        Write-Host " - Dang tat tien trinh $pName (PID: $p)..." -ForegroundColor Yellow
        Stop-Process -Id $p -Force -ErrorAction SilentlyContinue
    }
} else {
    Write-Host " - Khong co tien trinh nao dang chiem cong 5225 va 5173." -ForegroundColor Gray
}

# 2. Dong cac cua so PowerShell chay backend / frontend
Get-Process -Name "powershell" -ErrorAction SilentlyContinue | Where-Object {
    $_.MainWindowTitle -like "*BACKEND API*" -or $_.MainWindowTitle -like "*FRONTEND SPA*"
} | ForEach-Object {
    Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "==================================================================" -ForegroundColor Green
Write-Host "   DA DUNG THANH CONG TOAN BO TIEN TRINH HE THONG!" -ForegroundColor Green
Write-Host "==================================================================" -ForegroundColor Green
Start-Sleep -Seconds 3
