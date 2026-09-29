# Security & RBAC Penetration / Audit Test Suite
# Project: Procurement & Contract Tracking System
# Date: 22/09/2026 (Track 1 - Dev 1)

$baseUrl = "http://localhost:5225"
$results = @()

function Record-TestResult {
    param(
        [string]$TestId,
        [string]$Category,
        [string]$Description,
        [string]$ExpectedStatus,
        [string]$ActualStatus,
        [bool]$Passed,
        [string]$Details
    )
    $obj = [PSCustomObject]@{
        TestId         = $TestId
        Category       = $Category
        Description    = $Description
        ExpectedStatus = $ExpectedStatus
        ActualStatus   = $ActualStatus
        Passed         = $Passed
        Details        = $Details
    }
    $global:results += $obj
    
    $color = if ($Passed) { "Green" } else { "Red" }
    $statusText = if ($Passed) { "[PASSED]" } else { "[FAILED]" }
    Write-Host "$statusText $TestId - $Description" -ForegroundColor $color
    Write-Host "   Expected: $ExpectedStatus | Actual: $ActualStatus | $Details" -ForegroundColor Gray
}

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host " STARTING SECURITY & RBAC AUDIT TEST SUITE (TRACK 1 - DEV 1)" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

# -------------------------------------------------------------
# STEP 0: ACQUIRE TOKENS
# -------------------------------------------------------------
Write-Host "`n[AUTHENTICATION SETUP]" -ForegroundColor Yellow

# 0.1 Admin Token
$adminLoginBody = @{ email = "admin@procurement.com"; password = "Admin@123" } | ConvertTo-Json
$adminLoginRes = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $adminLoginBody -ContentType "application/json"
$adminToken = $adminLoginRes.data.token
$adminHeaders = @{ Authorization = "Bearer $adminToken" }
Write-Host "  -> Admin token acquired: OK" -ForegroundColor Green

# 0.2 Contractor 1 Token
$cont1LoginBody = @{ email = "contractor1@test.com"; password = "Admin@123" } | ConvertTo-Json
$cont1LoginRes = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $cont1LoginBody -ContentType "application/json"
$cont1Token = $cont1LoginRes.data.token
$cont1Headers = @{ Authorization = "Bearer $cont1Token" }
Write-Host "  -> Contractor 1 token acquired: OK (ContractorId: $($cont1LoginRes.data.user.contractorId))" -ForegroundColor Green

# 0.3 Contractor 2 Token (Pure Contractor Role)
$cont2LoginBody = @{ email = "contractor2@test.com"; password = "Admin@123" } | ConvertTo-Json
$cont2LoginRes = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $cont2LoginBody -ContentType "application/json"
$cont2Token = $cont2LoginRes.data.token
$cont2Headers = @{ Authorization = "Bearer $cont2Token" }
Write-Host "  -> Contractor 2 token acquired: OK (ContractorId: $($cont2LoginRes.data.user.contractorId))" -ForegroundColor Green

# -------------------------------------------------------------
# 1. UNAUTHENTICATED ACCESS TESTS (EXPECT 401 UNAUTHORIZED)
# -------------------------------------------------------------
Write-Host "`n[GROUP 1: UNAUTHENTICATED ACCESS AUDIT (401)]" -ForegroundColor Yellow

# SEC-01: Call Dashboard without token
try {
    $res = Invoke-WebRequest -Uri "$baseUrl/api/reports/dashboard" -Method Get -UseBasicParsing
    Record-TestResult "SEC-01" "Auth" "Call Dashboard without token" "401 Unauthorized" "$($res.StatusCode)" ($res.StatusCode -eq 401) "Unexpected success"
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    Record-TestResult "SEC-01" "Auth" "Call Dashboard without token" "401 Unauthorized" "$code" ($code -eq 401) "Rejected cleanly by JWT middleware"
}

# SEC-02: Create Bid Package without token
try {
    $body = @{ name = "Illegal Package"; budget = 1000000; deadline = "2026-12-31T00:00:00Z" } | ConvertTo-Json
    $res = Invoke-WebRequest -Uri "$baseUrl/api/bid-packages" -Method Post -Body $body -ContentType "application/json" -UseBasicParsing
    Record-TestResult "SEC-02" "Auth" "Create Package without token" "401 Unauthorized" "$($res.StatusCode)" ($res.StatusCode -eq 401) "Unexpected success"
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    Record-TestResult "SEC-02" "Auth" "Create Package without token" "401 Unauthorized" "$code" ($code -eq 401) "Protected by Authorize attribute"
}

# SEC-03: Download submission file without token
try {
    $res = Invoke-WebRequest -Uri "$baseUrl/api/submissions/files/4/download" -Method Get -UseBasicParsing
    Record-TestResult "SEC-03" "Auth" "Download file without token" "401 Unauthorized" "$($res.StatusCode)" ($res.StatusCode -eq 401) "Unexpected success"
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    Record-TestResult "SEC-03" "Auth" "Download file without token" "401 Unauthorized" "$code" ($code -eq 401) "Protected by Authorize attribute"
}

# SEC-04: Call API with Malformed / Tampered Token
try {
    $fakeHeaders = @{ Authorization = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.tampered.token" }
    $res = Invoke-WebRequest -Uri "$baseUrl/api/reports/dashboard" -Method Get -Headers $fakeHeaders -UseBasicParsing
    Record-TestResult "SEC-04" "Auth" "Call API with Tampered JWT Token" "401 Unauthorized" "$($res.StatusCode)" ($res.StatusCode -eq 401) "Unexpected success"
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    Record-TestResult "SEC-04" "Auth" "Call API with Tampered JWT Token" "401 Unauthorized" "$code" ($code -eq 401) "Cryptographic signature validation failed"
}

# -------------------------------------------------------------
# 2. VERTICAL PRIVILEGE ESCALATION AUDIT (EXPECT 403 FORBIDDEN)
# -------------------------------------------------------------
Write-Host "`n[GROUP 2: VERTICAL PRIVILEGE ESCALATION AUDIT (403)]" -ForegroundColor Yellow

# SEC-05: Contractor 2 tries to create Bid Package (Admin/Procurement only)
try {
    $body = @{ name = "Contractor Hacker Package"; budget = 500000000; deadline = "2026-12-31T00:00:00Z"; type = 0; description = "Hack" } | ConvertTo-Json
    $res = Invoke-WebRequest -Uri "$baseUrl/api/bid-packages" -Method Post -Body $body -Headers $cont2Headers -ContentType "application/json" -UseBasicParsing
    Record-TestResult "SEC-05" "RBAC" "Contractor creates Bid Package" "403 Forbidden" "$($res.StatusCode)" ($res.StatusCode -eq 403) "Unexpected success"
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    Record-TestResult "SEC-05" "RBAC" "Contractor creates Bid Package" "403 Forbidden" "$code" ($code -eq 403) "Blocked by Roles='Admin,Procurement'"
}

# SEC-06: Contractor 2 tries to change Bid Package status
try {
    $body = @{ newStatus = 2 } | ConvertTo-Json
    $res = Invoke-WebRequest -Uri "$baseUrl/api/bid-packages/3/status" -Method Put -Body $body -Headers $cont2Headers -ContentType "application/json" -UseBasicParsing
    Record-TestResult "SEC-06" "RBAC" "Contractor changes Package Status" "403 Forbidden" "$($res.StatusCode)" ($res.StatusCode -eq 403) "Unexpected success"
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    Record-TestResult "SEC-06" "RBAC" "Contractor changes Package Status" "403 Forbidden" "$code" ($code -eq 403) "Blocked by Roles='Admin,Procurement'"
}

# SEC-07: Contractor 2 tries to create Evaluation Criteria
try {
    $body = @{ name = "Self Score Criteria"; weight = 50; maxScore = 100 } | ConvertTo-Json
    $res = Invoke-WebRequest -Uri "$baseUrl/api/evaluations/packages/3/criteria" -Method Post -Body $body -Headers $cont2Headers -ContentType "application/json" -UseBasicParsing
    Record-TestResult "SEC-07" "RBAC" "Contractor creates Evaluation Criteria" "403 Forbidden" "$($res.StatusCode)" ($res.StatusCode -eq 403) "Unexpected success"
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    Record-TestResult "SEC-07" "RBAC" "Contractor creates Evaluation Criteria" "403 Forbidden" "$code" ($code -eq 403) "Blocked by Roles='Admin,Procurement'"
}

# SEC-08: Contractor 2 tries to score a Submission (Evaluator/Admin only)
try {
    $body = @{ scores = @( @{ criteriaId = 6; score = 100; comment = "Self-eval" } ) } | ConvertTo-Json
    $res = Invoke-WebRequest -Uri "$baseUrl/api/evaluations/submissions/4/scores" -Method Post -Body $body -Headers $cont2Headers -ContentType "application/json" -UseBasicParsing
    Record-TestResult "SEC-08" "RBAC" "Contractor scores Submission" "403 Forbidden" "$($res.StatusCode)" ($res.StatusCode -eq 403) "Unexpected success"
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    Record-TestResult "SEC-08" "RBAC" "Contractor scores Submission" "403 Forbidden" "$code" ($code -eq 403) "Blocked by Roles='Admin,Evaluator'"
}

# SEC-09: Contractor 2 tries to Finalize & Award a Package
try {
    $res = Invoke-WebRequest -Uri "$baseUrl/api/evaluations/packages/3/finalize?selectedSubmissionId=4" -Method Post -Headers $cont2Headers -UseBasicParsing
    Record-TestResult "SEC-09" "RBAC" "Contractor awards Package" "403 Forbidden" "$($res.StatusCode)" ($res.StatusCode -eq 403) "Unexpected success"
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    Record-TestResult "SEC-09" "RBAC" "Contractor awards Package" "403 Forbidden" "$code" ($code -eq 403) "Blocked by Roles='Admin,Procurement'"
}

# SEC-10: Contractor 2 tries to access Management Dashboard
try {
    $res = Invoke-WebRequest -Uri "$baseUrl/api/reports/dashboard" -Method Get -Headers $cont2Headers -UseBasicParsing
    Record-TestResult "SEC-10" "RBAC" "Contractor accesses Executive Dashboard" "403 Forbidden" "$($res.StatusCode)" ($res.StatusCode -eq 403) "Unexpected success"
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    Record-TestResult "SEC-10" "RBAC" "Contractor accesses Executive Dashboard" "403 Forbidden" "$code" ($code -eq 403) "Blocked by Roles='Admin,Procurement'"
}

# -------------------------------------------------------------
# 3. HORIZONTAL PRIVILEGE ESCALATION / IDOR AUDIT (EXPECT 403)
# -------------------------------------------------------------
Write-Host "`n[GROUP 3: HORIZONTAL PRIVILEGE ESCALATION & IDOR AUDIT (403)]" -ForegroundColor Yellow

# SEC-11: Contractor 2 tries to view detail of Contractor 1's submission (Submission #4)
try {
    $res = Invoke-WebRequest -Uri "$baseUrl/api/submissions/4" -Method Get -Headers $cont2Headers -UseBasicParsing
    Record-TestResult "SEC-11" "IDOR" "Contractor 2 views Contractor 1's Submission" "403 Forbidden" "$($res.StatusCode)" ($res.StatusCode -eq 403) "Unexpected success"
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    Record-TestResult "SEC-11" "IDOR" "Contractor 2 views Contractor 1's Submission" "403 Forbidden" "$code" ($code -eq 403) "Prevented: User is not owner and not internal staff"
}

# SEC-12: Contractor 2 tries to download Contractor 1's technical proposal file (File #4)
try {
    $res = Invoke-WebRequest -Uri "$baseUrl/api/submissions/files/4/download" -Method Get -Headers $cont2Headers -UseBasicParsing
    Record-TestResult "SEC-12" "IDOR" "Contractor 2 downloads Contractor 1's Proposal File" "403 Forbidden" "$($res.StatusCode)" ($res.StatusCode -eq 403) "Unexpected leak"
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    Record-TestResult "SEC-12" "IDOR" "Contractor 2 downloads Contractor 1's Proposal File" "403 Forbidden" "$code" ($code -eq 403) "Prevented: IDOR check intercepted download request"
}

# -------------------------------------------------------------
# 4. LEGITIMATE ACCESS VERIFICATION (EXPECT 200 OK)
# -------------------------------------------------------------
Write-Host "`n[GROUP 4: LEGITIMATE AUTHORIZED ACCESS VERIFICATION (200 OK)]" -ForegroundColor Yellow

# SEC-13: Contractor 1 views own submission (Submission #4)
try {
    $res = Invoke-WebRequest -Uri "$baseUrl/api/submissions/4" -Method Get -Headers $cont1Headers -UseBasicParsing
    Record-TestResult "SEC-13" "Access" "Contractor 1 views own Submission" "200 OK" "$($res.StatusCode)" ($res.StatusCode -eq 200) "Owner authorized"
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    Record-TestResult "SEC-13" "Access" "Contractor 1 views own Submission" "200 OK" "$code" $false "$($_.Exception.Message)"
}

# SEC-14: Contractor 1 downloads own technical proposal file (File #4)
try {
    $res = Invoke-WebRequest -Uri "$baseUrl/api/submissions/files/4/download" -Method Get -Headers $cont1Headers -UseBasicParsing
    Record-TestResult "SEC-14" "Access" "Contractor 1 downloads own Proposal File" "200 OK" "$($res.StatusCode)" ($res.StatusCode -eq 200) "Owner download authorized (Length: $($res.RawContentLength) bytes)"
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    Record-TestResult "SEC-14" "Access" "Contractor 1 downloads own Proposal File" "200 OK" "$code" $false "$($_.Exception.Message)"
}

# SEC-15: Admin downloads technical proposal file for evaluation
try {
    $res = Invoke-WebRequest -Uri "$baseUrl/api/submissions/files/4/download" -Method Get -Headers $adminHeaders -UseBasicParsing
    Record-TestResult "SEC-15" "Access" "Admin downloads Proposal File for evaluation" "200 OK" "$($res.StatusCode)" ($res.StatusCode -eq 200) "Internal staff authorized (Length: $($res.RawContentLength) bytes)"
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    Record-TestResult "SEC-15" "Access" "Admin downloads Proposal File for evaluation" "200 OK" "$code" $false "$($_.Exception.Message)"
}

# -------------------------------------------------------------
# SUMMARY REPORT
# -------------------------------------------------------------
Write-Host "`n================================================================" -ForegroundColor Cyan
Write-Host " SECURITY & RBAC AUDIT SUMMARY REPORT" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

$totalTests = $results.Count
$passedCount = ($results | Where-Object { $_.Passed -eq $true }).Count
$failedCount = $totalTests - $passedCount

Write-Host "Total Tests Run : $totalTests" -ForegroundColor White
Write-Host "Passed Tests    : $passedCount" -ForegroundColor Green
Write-Host "Failed Tests    : $failedCount" -ForegroundColor $(if ($failedCount -gt 0) { "Red" } else { "Green" })

if ($failedCount -eq 0) {
    Write-Host "`n[AUDIT RESULT] 100% SECURITY & RBAC CONSTRAINTS MET! ZERO VULNERABILITIES DETECTED." -ForegroundColor Green
} else {
    Write-Host "`n[AUDIT RESULT] VULNERABILITIES DETECTED! Review failed test cases above." -ForegroundColor Red
}

$results | Format-Table TestId, Category, ExpectedStatus, ActualStatus, Passed -AutoSize
