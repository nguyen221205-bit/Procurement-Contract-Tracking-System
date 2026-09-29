# Test E2E Integration Suite (10-Step Full Procurement & Contract Lifecycle)
$baseUrl = "http://localhost:5225"
$ErrorActionPreference = "Stop"

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host " STARTING E2E INTEGRATION TEST SUITE (10 STEPS)" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

# Step 1: Admin Login
Write-Host "`n[STEP 1] Logging in as Admin..." -ForegroundColor Yellow
$adminLoginBody = @{ email = "admin@procurement.com"; password = "Admin@123" } | ConvertTo-Json
$adminLoginRes = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $adminLoginBody -ContentType "application/json"
$adminToken = $adminLoginRes.data.token
$adminHeaders = @{ Authorization = "Bearer $adminToken" }
Write-Host "  -> Admin login OK! Token obtained." -ForegroundColor Green

# Step 2: Contractor Login
Write-Host "`n[STEP 2] Logging in as Contractor 1..." -ForegroundColor Yellow
$contLoginBody = @{ email = "contractor1@test.com"; password = "Admin@123" } | ConvertTo-Json
$contLoginRes = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $contLoginBody -ContentType "application/json"
$contToken = $contLoginRes.data.token
$contHeaders = @{ Authorization = "Bearer $contToken" }
Write-Host "  -> Contractor 1 login OK! Token obtained." -ForegroundColor Green

# Step 3: Create Bid Package
Write-Host "`n[STEP 3] Admin creates new Bid Package..." -ForegroundColor Yellow
$deadline = (Get-Date).ToUniversalTime().AddDays(30).ToString("yyyy-MM-ddTHH:mm:ssZ")
$pkgBody = @{
    name = "Goi thau He thong ERP va CSDL Trung tam 2026"
    type = 0
    budget = 1200000000
    deadline = $deadline
    description = "Trien khai phan he ERP doanh nghiep va he thong CSDL SQL Server tap trung"
} | ConvertTo-Json
$pkgRes = Invoke-RestMethod -Uri "$baseUrl/api/bid-packages" -Method Post -Body $pkgBody -Headers $adminHeaders -ContentType "application/json"
$pkgId = $pkgRes.data.id
$pkgCode = $pkgRes.data.code
Write-Host "  -> Bid Package created! ID: $pkgId, Code: $pkgCode, Status: $($pkgRes.data.status)" -ForegroundColor Green

# Step 4: Create Evaluation Criteria (Total weight = 100%)
Write-Host "`n[STEP 4] Admin creates 2 Evaluation Criteria..." -ForegroundColor Yellow
$crit1Body = @{
    name = "Nang luc giai phap cong nghe va kien truc"
    description = "Dap ung tieu chuan bao mat va kha nang mo rong he thong"
    weight = 60
    maxScore = 100
} | ConvertTo-Json
$crit1Res = Invoke-RestMethod -Uri "$baseUrl/api/evaluations/packages/$pkgId/criteria" -Method Post -Body $crit1Body -Headers $adminHeaders -ContentType "application/json"
$crit1Id = $crit1Res.data.id
Write-Host "  -> Criteria 1 created! ID: $crit1Id (Weight: 60%)" -ForegroundColor Green

$crit2Body = @{
    name = "Tien do cam ket va chi phi gia du thau"
    description = "Ke hoach trien khai ro rang va don gia canh tranh"
    weight = 40
    maxScore = 100
} | ConvertTo-Json
$crit2Res = Invoke-RestMethod -Uri "$baseUrl/api/evaluations/packages/$pkgId/criteria" -Method Post -Body $crit2Body -Headers $adminHeaders -ContentType "application/json"
$crit2Id = $crit2Res.data.id
Write-Host "  -> Criteria 2 created! ID: $crit2Id (Weight: 40%)" -ForegroundColor Green

# Step 5: Contractor Submits Bid
Write-Host "`n[STEP 5] Contractor 1 submits Bid with Proposal file..." -ForegroundColor Yellow
# Create a dummy proposal file
$dummyFilePath = "$env:TEMP\de_xuat_ky_thuat_erp.pdf"
[System.IO.File]::WriteAllText($dummyFilePath, "%PDF-1.4 dummy ERP proposal content")

# Use curl.exe for multipart/form-data upload
$curlCmd = "curl.exe -s -X POST `"$baseUrl/api/bid-packages/$pkgId/submissions`" -H `"Authorization: Bearer $contToken`" -F `"Files=@$dummyFilePath`" -F `"FileTypes=0`""
$subJson = Invoke-Expression $curlCmd
$subRes = $subJson | ConvertFrom-Json
if (-not $subRes.success) {
    throw "Submission failed: $($subRes.message)"
}
$subId = $subRes.data.id
Write-Host "  -> Bid Submission created! Submission ID: $subId, Status: $($subRes.data.status)" -ForegroundColor Green

# Step 6: Close Bidding & Set to Evaluating
Write-Host "`n[STEP 6] Admin transitions status: Open -> Closed -> Evaluating..." -ForegroundColor Yellow
$closeBody = @{ newStatus = 1 } | ConvertTo-Json
$closeRes = Invoke-RestMethod -Uri "$baseUrl/api/bid-packages/$pkgId/status" -Method Put -Body $closeBody -Headers $adminHeaders -ContentType "application/json"
Write-Host "  -> Status transitioned to: $($closeRes.data.status)" -ForegroundColor Green

$evalBody = @{ newStatus = 2 } | ConvertTo-Json
$evalRes = Invoke-RestMethod -Uri "$baseUrl/api/bid-packages/$pkgId/status" -Method Put -Body $evalBody -Headers $adminHeaders -ContentType "application/json"
Write-Host "  -> Status transitioned to: $($evalRes.data.status)" -ForegroundColor Green

# Step 7: Evaluator Scores the Submission
Write-Host "`n[STEP 7] Evaluator scores submission (Crit 1: 95, Crit 2: 90)..." -ForegroundColor Yellow
$scoreBody = @{
    scores = @(
        @{ criteriaId = $crit1Id; score = 95; comment = "Giai phap kien truc microservices rat tot" },
        @{ criteriaId = $crit2Id; score = 90; comment = "Tien do 6 thang kha thi" }
    )
} | ConvertTo-Json
$scoreRes = Invoke-RestMethod -Uri "$baseUrl/api/evaluations/submissions/$subId/scores" -Method Post -Body $scoreBody -Headers $adminHeaders -ContentType "application/json"
Write-Host "  -> Submission scored successfully!" -ForegroundColor Green

# Step 8: Check Evaluation Summary & Rankings
Write-Host "`n[STEP 8] Admin checks Evaluation Summary & Rankings..." -ForegroundColor Yellow
$summaryRes = Invoke-RestMethod -Uri "$baseUrl/api/evaluations/packages/$pkgId/summary" -Method Get -Headers $adminHeaders
Write-Host "  -> Package: $($summaryRes.data.bidPackageName)" -ForegroundColor Green
Write-Host "  -> Total Weight: $($summaryRes.data.totalWeight)%" -ForegroundColor Green
Write-Host "  -> Evaluated Submissions: $($summaryRes.data.evaluatedSubmissions)/$($summaryRes.data.totalSubmissions)" -ForegroundColor Green
Write-Host "  -> Average Score: $($summaryRes.data.averageScore)" -ForegroundColor Green

# Step 9: Finalize / Select Winning Bid
Write-Host "`n[STEP 9] Admin Finalizes & Approves Winning Bid..." -ForegroundColor Yellow
$finalizeRes = Invoke-RestMethod -Uri "$baseUrl/api/evaluations/packages/$pkgId/finalize?selectedSubmissionId=$subId" -Method Post -Headers $adminHeaders
Write-Host "  -> Finalize Result: $($finalizeRes.message)" -ForegroundColor Green

# Step 10: Contract Creation, Contractor History & Dashboard Verification
Write-Host "`n[STEP 10] Contract Integration, Contractor History & Dashboard Verification..." -ForegroundColor Yellow

# 10.1 Check Awarded Bid Export
$awardedRes = Invoke-RestMethod -Uri "$baseUrl/api/evaluations/packages/$pkgId/awarded-bid" -Method Get -Headers $adminHeaders
Write-Host "  -> Awarded Bid Winner: $($awardedRes.data.companyName), Rank: $($awardedRes.data.rank), ReadyForContract: $($awardedRes.data.isReadyForContract)" -ForegroundColor Green

# 10.2 Create Contract
$contractStartDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
$contractEndDate = (Get-Date).ToUniversalTime().AddMonths(6).ToString("yyyy-MM-ddTHH:mm:ssZ")
$ctrNum = "CTR-20260921-" + (Get-Random -Minimum 1000 -Maximum 9999)
$ctrBody = @{
    bidPackageId = $pkgId
    contractorId = $awardedRes.data.contractorId
    contractNumber = $ctrNum
    value = 1150000000
    terms = "Bao hanh 24 thang, ho tro 24/7 sau khi ban giao"
    startDate = $contractStartDate
    endDate = $contractEndDate
} | ConvertTo-Json

$ctrRes = Invoke-RestMethod -Uri "$baseUrl/api/contracts" -Method Post -Body $ctrBody -Headers $adminHeaders -ContentType "application/json"
$contractId = $ctrRes.data.id
Write-Host "  -> Contract created! ID: $contractId, Number: $($ctrRes.data.contractNumber), Value: $($ctrRes.data.value) VND" -ForegroundColor Green

# 10.3 Create Milestone
$msDueDate = (Get-Date).ToUniversalTime().AddMonths(2).ToString("yyyy-MM-ddTHH:mm:ssZ")
$msBody = @{
    title = "Nghiem thu Giai doan 1: Kien truc va CSDL"
    dueDate = $msDueDate
    amount = 500000000
} | ConvertTo-Json
$msRes = Invoke-RestMethod -Uri "$baseUrl/api/contracts/$contractId/milestones" -Method Post -Body $msBody -Headers $adminHeaders -ContentType "application/json"
Write-Host "  -> Milestone 1 created! ID: $($msRes.data.id), Amount: $($msRes.data.amount) VND" -ForegroundColor Green

# 10.4 Verify Teammate's new API: GET /api/contracts/contractor/{id}
Write-Host "  -> Verifying Teammate's API: GET /api/contracts/contractor/1..." -ForegroundColor Cyan
$contHistory = Invoke-RestMethod -Uri "$baseUrl/api/contracts/contractor/1" -Method Get -Headers $adminHeaders
Write-Host "  -> Contractor 1 has $($contHistory.data.Count) contract(s) in history!" -ForegroundColor Green
foreach ($c in $contHistory.data) {
    Write-Host "     * Contract: $($c.contractNumber) | Package: $($c.bidPackageCode) | Value: $($c.value) VND | Milestones: $($c.milestoneCount)" -ForegroundColor Gray
}

# 10.5 Verify Management Dashboard
Write-Host "  -> Verifying Management Dashboard API: GET /api/reports/dashboard..." -ForegroundColor Cyan
$dashRes = Invoke-RestMethod -Uri "$baseUrl/api/reports/dashboard" -Method Get -Headers $adminHeaders
Write-Host "  -> Dashboard Metrics:" -ForegroundColor Green
Write-Host "     * Total Packages: $($dashRes.data.totalPackages)" -ForegroundColor Gray
Write-Host "     * Contracted Packages: $($dashRes.data.contractedPackages)" -ForegroundColor Gray
Write-Host "     * Total Submissions: $($dashRes.data.totalSubmissions)" -ForegroundColor Gray
Write-Host "     * Total Budget: $($dashRes.data.totalEstimatedBudget) VND" -ForegroundColor Gray
Write-Host "     * Total Contract Value: $($dashRes.data.totalContractValue) VND" -ForegroundColor Gray
Write-Host "     * Total Savings: $($dashRes.data.totalSavings) VND (Rate: $($dashRes.data.savingsRate)%)" -ForegroundColor Gray
Write-Host "     * Total Contracts: $($dashRes.data.totalContracts)" -ForegroundColor Gray
Write-Host "     * Total Milestones: $($dashRes.data.totalMilestones)" -ForegroundColor Gray

Write-Host "`n================================================================" -ForegroundColor Cyan
Write-Host " ALL 10 STEPS PASSED SUCCESSFULLY! ZERO ERRORS." -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Cyan
