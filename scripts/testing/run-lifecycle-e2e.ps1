param(
    [string]$RunId = "",
    [int]$ApiPort = 5225,
    [int]$FrontendPort = 5173,
    [int]$SmtpPort = 2525,
    [switch]$DropDatabaseOnSuccess = $false
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RunId)) {
    $RunId = "run_" + (Get-Date -Format "yyyyMMdd_HHmmss")
}

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$SuiteDir = Join-Path $PSScriptRoot "lifecycle-e2e"
$RunDir = Join-Path $SuiteDir "runs\$RunId"
$UploadsDir = Join-Path $RunDir "uploads"
$LogsDir = Join-Path $RunDir "logs"
$DbName = "procurement_db_e2e_$RunId"

New-Item -ItemType Directory -Force -Path $RunDir | Out-Null
New-Item -ItemType Directory -Force -Path $UploadsDir | Out-Null
New-Item -ItemType Directory -Force -Path $LogsDir | Out-Null

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " REPRODUCIBLE ALL-WORKFLOWS & ALL-ROLES E2E RUNNER" -ForegroundColor Cyan
Write-Host " RunId       : $RunId" -ForegroundColor White
Write-Host " Database    : $DbName" -ForegroundColor White
Write-Host " SMTP Catcher: 127.0.0.1:$SmtpPort" -ForegroundColor White
Write-Host " Output Dir  : $RunDir" -ForegroundColor White
Write-Host "=================================================================" -ForegroundColor Cyan

# 1. Pre-flight check: Ensure ports are not already in use by external processes
$apiBusy = Get-NetTCPConnection -LocalPort $ApiPort -State Listen -ErrorAction SilentlyContinue
if ($apiBusy) {
    Write-Host "[BLOCKED] Port $ApiPort is already in use. Refusing to connect to pre-existing API instance." -ForegroundColor Red
    exit 2
}

$feBusy = Get-NetTCPConnection -LocalPort $FrontendPort -State Listen -ErrorAction SilentlyContinue
if ($feBusy) {
    Write-Host "[BLOCKED] Port $FrontendPort is already in use. Please free port $FrontendPort before running E2E." -ForegroundColor Red
    exit 2
}

$smtpBusy = Get-NetTCPConnection -LocalPort $SmtpPort -State Listen -ErrorAction SilentlyContinue
if ($smtpBusy) {
    Write-Host "[BLOCKED] Port $SmtpPort is already in use. Please free port $SmtpPort before running E2E." -ForegroundColor Red
    exit 2
}

# 2. Provision isolated SQL Server Database
Write-Host "`n[1/5] Creating isolated SQL Server database: $DbName..." -ForegroundColor Yellow
python -c "import sys; sys.path.insert(0, r'$SuiteDir'); from db_helper import create_database; create_database('$DbName')"
if ($LASTEXITCODE -ne 0) {
    Write-Host "[FAIL] Failed to create isolated database $DbName." -ForegroundColor Red
    exit 1
}

# 3. Build & Start Isolated Backend API Process
Write-Host "`n[2/5] Starting isolated Backend API on port $ApiPort..." -ForegroundColor Yellow
$apiProject = Join-Path $RepoRoot "ProcurementSystem.API\ProcurementSystem.API.csproj"
dotnet build $apiProject -c Debug --nologo -v q
if ($LASTEXITCODE -ne 0) {
    Write-Host "[FAIL] Backend build failed." -ForegroundColor Red
    exit 1
}

$apiOutLog = Join-Path $LogsDir "api_stdout.log"
$apiErrLog = Join-Path $LogsDir "api_stderr.log"

$env:DB_CONNECTION_STRING = "Server=localhost;Database=$DbName;User Id=sa;Password=123;TrustServerCertificate=True;MultipleActiveResultSets=true"
$env:ASPNETCORE_URLS = "http://localhost:$ApiPort"
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:StorageSettings__Provider = "Local"
$env:StorageSettings__LocalBasePath = $UploadsDir
$env:StorageSettings__Supabase__Endpoint = ""
$env:StorageSettings__Supabase__AccessKey = ""
$env:StorageSettings__Supabase__SecretKey = ""
$env:SMTP_HOST = "127.0.0.1"
$env:SMTP_PORT = "$SmtpPort"
$env:SMTP_USERNAME = "e2e@local.test"
$env:SMTP_PASSWORD = "e2epass"
$env:SMTP_SENDER_EMAIL = "e2e@local.test"
$env:Smtp__Host = "127.0.0.1"
$env:Smtp__Port = "$SmtpPort"
$env:Smtp__EnableSsl = "false"
$env:Smtp__Username = "e2e@local.test"
$env:Smtp__Password = "e2epass"
$env:Smtp__SenderEmail = "e2e@local.test"

$apiProc = Start-Process -FilePath "dotnet" `
    -ArgumentList "run --no-build --no-launch-profile --project `"$apiProject`"" `
    -WorkingDirectory (Join-Path $RepoRoot "ProcurementSystem.API") `
    -RedirectStandardOutput $apiOutLog `
    -RedirectStandardError $apiErrLog `
    -PassThru -NoNewWindow

# 4. Start Frontend Vite Server
Write-Host "`n[3/5] Starting Frontend server on port $FrontendPort..." -ForegroundColor Yellow
$feDir = Join-Path $RepoRoot "frontend"
$feOutLog = Join-Path $LogsDir "fe_stdout.log"
$feErrLog = Join-Path $LogsDir "fe_stderr.log"

$feProc = Start-Process -FilePath "node" `
    -ArgumentList "./node_modules/vite/bin/vite.js --port $FrontendPort --strictPort" `
    -WorkingDirectory $feDir `
    -RedirectStandardOutput $feOutLog `
    -RedirectStandardError $feErrLog `
    -PassThru -NoNewWindow

$suiteExitCode = 1

try {
    # Wait for API Health
    Write-Host "Waiting for Backend API migration, seed & health check..." -ForegroundColor Gray
    $apiReady = $false
    for ($i = 0; $i -lt 45; $i++) {
        Start-Sleep -Seconds 2
        try {
            $health = Invoke-RestMethod -Uri "http://localhost:$ApiPort/api/health" -Method Get -TimeoutSec 3
            if ($health.success -eq $true -and $health.data.databaseConnected -eq $true) {
                $apiReady = $true
                Write-Host "  -> Backend API is healthy (Seeded Users: $($health.data.usersCount), Roles: $($health.data.rolesCount))" -ForegroundColor Green
                break
            }
        } catch {
            # Still starting up
        }
    }

    if (-not $apiReady) {
        Write-Host "[FAIL] Backend API did not become healthy within timeout. Check logs at $apiErrLog" -ForegroundColor Red
        throw "Backend startup timeout"
    }

    # Wait for Frontend Ready
    Write-Host "Waiting for Frontend server..." -ForegroundColor Gray
    $feReady = $false
    for ($i = 0; $i -lt 20; $i++) {
        Start-Sleep -Seconds 1
        try {
            $resp = Invoke-WebRequest -Uri "http://localhost:$FrontendPort/login" -UseBasicParsing -TimeoutSec 3
            if ($resp.StatusCode -eq 200) {
                $feReady = $true
                Write-Host "  -> Frontend server is ready at http://localhost:$FrontendPort" -ForegroundColor Green
                break
            }
        } catch {
            # Still starting up
        }
    }

    if (-not $feReady) {
        Write-Host "[FAIL] Frontend server did not become ready within timeout." -ForegroundColor Red
        throw "Frontend startup timeout"
    }

    # 5. Execute Full E2E Suite
    Write-Host "`n[4/5] Running Playwright Browser Lifecycle, Negative Rules & All-Workflows Suite..." -ForegroundColor Yellow
    $runSuiteScript = Join-Path $SuiteDir "run_suite.py"
    python $runSuiteScript $RunId $DbName "http://localhost:$FrontendPort" "http://localhost:$ApiPort" "$SmtpPort"
    $suiteExitCode = $LASTEXITCODE
}
finally {
    Write-Host "`n[5/5] Tearing down isolated processes..." -ForegroundColor Yellow
    if ($feProc -and -not $feProc.HasExited) {
        Stop-Process -Id $feProc.Id -Force -ErrorAction SilentlyContinue
        Write-Host "  -> Stopped Frontend process (PID: $($feProc.Id))" -ForegroundColor Gray
    }
    if ($apiProc -and -not $apiProc.HasExited) {
        Stop-Process -Id $apiProc.Id -Force -ErrorAction SilentlyContinue
        Write-Host "  -> Stopped Backend API process (PID: $($apiProc.Id))" -ForegroundColor Gray
    }

    if ($suiteExitCode -eq 0 -and $DropDatabaseOnSuccess) {
        Write-Host "  -> Dropping temporary test database $DbName..." -ForegroundColor Gray
        python -c "import sys; sys.path.insert(0, r'$SuiteDir'); from db_helper import drop_database; drop_database('$DbName')"
    } else {
        Write-Host "  -> Isolated database retained for inspection: $DbName" -ForegroundColor Gray
    }
}

if ($suiteExitCode -eq 0) {
    Write-Host "`n=================================================================" -ForegroundColor Green
    Write-Host " E2E ACCEPTANCE SUITE COMPLETED (RunId: $RunId)" -ForegroundColor Green
    Write-Host " Manifest    : $RunDir\manifest.json" -ForegroundColor Green
    Write-Host " Case Results: $RunDir\case_results.json" -ForegroundColor Green
    Write-Host "=================================================================" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n=================================================================" -ForegroundColor Red
    Write-Host " E2E ACCEPTANCE SUITE FAILED (RunId: $RunId)" -ForegroundColor Red
    Write-Host " Check logs & failure artifacts in: $RunDir" -ForegroundColor Red
    Write-Host "=================================================================" -ForegroundColor Red
    exit $suiteExitCode
}
