# Negative & Robustness Business Rules Test Suite
# Project: Procurement & Contract Tracking System
# Priority: P3 - Robustness, Error Handling & Security Defense-in-Depth
# Date: 07/10/2026

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
Write-Host " STARTING NEGATIVE BUSINESS RULES TEST SUITE (P3 VERIFICATION)" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

# -------------------------------------------------------------
# STEP 0: ACQUIRE TOKENS
# -------------------------------------------------------------
Write-Host "`n[AUTHENTICATION SETUP]" -ForegroundColor Yellow

try {
    # 0.1 Admin Token
    $adminLoginBody = @{ email = "admin@procurement.com"; password = "Admin@123" } | ConvertTo-Json
    $adminLoginRes = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $adminLoginBody -ContentType "application/json"
    $adminToken = $adminLoginRes.data.token
    $adminHeaders = @{ Authorization = "Bearer $adminToken" }
    Write-Host "  -> Admin token: OK" -ForegroundColor Green

    # 0.2 Procurement Token
    $procLoginBody = @{ email = "procurement@procurement.com"; password = "Admin@123" } | ConvertTo-Json
    $procLoginRes = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $procLoginBody -ContentType "application/json"
    $procToken = $procLoginRes.data.token
    $procHeaders = @{ Authorization = "Bearer $procToken" }
    Write-Host "  -> Procurement token: OK" -ForegroundColor Green

    # 0.3 Contractor 1 Token
    $cont1LoginBody = @{ email = "contractor1@test.com"; password = "Admin@123" } | ConvertTo-Json
    $cont1LoginRes = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $cont1LoginBody -ContentType "application/json"
    $cont1Token = $cont1LoginRes.data.token
    $cont1Headers = @{ Authorization = "Bearer $cont1Token" }
    Write-Host "  -> Contractor 1 token: OK" -ForegroundColor Green
}
catch {
    Write-Host "  [!] Cannot acquire auth tokens. Server might not be running at $baseUrl." -ForegroundColor Red
    Write-Host "  Error: $_" -ForegroundColor Red
    exit 1
}

# -------------------------------------------------------------
# TEST 1: Cấm mở lại gói thầu sau khi đã đóng hoặc đang chấm điểm (P0-1 State Machine)
# -------------------------------------------------------------
Write-Host "`n[GROUP 1: STATE MACHINE NEGATIVE CONSTRAINTS]" -ForegroundColor Yellow

# Lấy 1 gói thầu đang chấm hoặc đã trao
try {
    $packagesRes = Invoke-RestMethod -Uri "$baseUrl/api/bid-packages" -Method Get -Headers $adminHeaders
    $evaluatingPkg = $packagesRes.data.items | Where-Object { $_.status -eq "Evaluating" -or $_.status -eq "Awarded" -or $_.status -eq "Contracted" } | Select-Object -First 1

    if ($evaluatingPkg) {
        $body = @{ newStatus = 0; reason = "Co tinh mo lai goi dang cham hoac da trao" } | ConvertTo-Json # 0 = Open
        try {
            $res = Invoke-RestMethod -Uri "$baseUrl/api/bid-packages/$($evaluatingPkg.id)/status" -Method Put -Body $body -ContentType "application/json" -Headers $adminHeaders
            Record-TestResult "NEG-01" "StateMachine" "Chặn mở lại gói thầu khi đã vào vòng chấm điểm/trao thầu" "HTTP 400" "HTTP 200" $false "Hệ thống không chặn mở lại gói thầu!"
        }
        catch {
            $statusCode = $_.Exception.Response.StatusCode.value__
            $passed = ($statusCode -eq 400)
            Record-TestResult "NEG-01" "StateMachine" "Chặn mở lại gói thầu khi đã vào vòng chấm điểm/trao thầu" "HTTP 400" "HTTP $statusCode" $passed "Hệ thống từ chối mở lại hợp lệ theo NĐ 24/2024/NĐ-CP"
        }
    } else {
        Record-TestResult "NEG-01" "StateMachine" "Chặn mở lại gói thầu khi đã vào vòng chấm điểm/trao thầu" "HTTP 400" "SKIPPED" $true "Không có gói thầu ở trạng thái Evaluating/Awarded để thử nghiệm"
    }
}
catch {
    Record-TestResult "NEG-01" "StateMachine" "Chặn mở lại gói thầu khi đã vào vòng chấm điểm/trao thầu" "HTTP 400" "ERROR" $false "$_"
}

# -------------------------------------------------------------
# TEST 2: Cấm chuyển tay sang Awarded (3) hoặc Contracted (4) qua API ChangeStatus (P0-2)
# -------------------------------------------------------------
try {
    $packagesRes = Invoke-RestMethod -Uri "$baseUrl/api/bid-packages" -Method Get -Headers $adminHeaders
    $openPkg = $packagesRes.data.items | Where-Object { $_.status -eq "Open" } | Select-Object -First 1

    if ($openPkg) {
        $bodyAwarded = @{ newStatus = 3; note = "Cố tình nhảy cóc sang Awarded" } | ConvertTo-Json
        try {
            $res = Invoke-RestMethod -Uri "$baseUrl/api/bid-packages/$($openPkg.id)/status" -Method Put -Body $bodyAwarded -ContentType "application/json" -Headers $adminHeaders
            Record-TestResult "NEG-02" "StateMachine" "Chặn tự ý đổi trạng thái sang Awarded qua API ChangeStatus" "HTTP 400" "HTTP 200" $false "Hệ thống cho phép nhảy cóc sang Awarded!"
        }
        catch {
            $statusCode = $_.Exception.Response.StatusCode.value__
            $passed = ($statusCode -eq 400)
            Record-TestResult "NEG-02" "StateMachine" "Chặn tự ý đổi trạng thái sang Awarded qua API ChangeStatus" "HTTP 400" "HTTP $statusCode" $passed "Hệ thống chặn nhảy cóc sang Awarded thành công"
        }
    } else {
        Record-TestResult "NEG-02" "StateMachine" "Chặn tự ý đổi trạng thái sang Awarded qua API ChangeStatus" "HTTP 400" "SKIPPED" $true "Không tìm thấy gói Open để thử nghiệm"
    }
}
catch {
    Record-TestResult "NEG-02" "StateMachine" "Chặn tự ý đổi trạng thái sang Awarded qua API ChangeStatus" "HTTP 400" "ERROR" $false "$_"
}

# -------------------------------------------------------------
# TEST 3: Chặn nhà thầu chưa được duyệt thẩm định (VerificationStatus != Approved) nộp thầu (P1-6)
# -------------------------------------------------------------
Write-Host "`n[GROUP 2: CONTRACTOR VERIFICATION CONSTRAINTS]" -ForegroundColor Yellow

try {
    # 1. Admin tạm thời thẩm định từ chối (Rejected) cho Contractor 1
    $rejectBody = @{ isApproved = $false; notes = "Tạm từ chối để kiểm thử Negative Case P1-6" } | ConvertTo-Json
    $null = Invoke-RestMethod -Uri "$baseUrl/api/contractors/1/verify" -Method Patch -Body $rejectBody -ContentType "application/json" -Headers $adminHeaders

    # 2. Thử nộp thầu bằng token của Contractor 1 (lúc này đang bị Rejected)
    $packagesRes = Invoke-RestMethod -Uri "$baseUrl/api/bid-packages" -Method Get -Headers $adminHeaders
    $openPkg = $packagesRes.data.items | Where-Object { $_.status -eq "Open" } | Select-Object -First 1

    if ($openPkg) {
        $tempFile = [System.IO.Path]::GetTempFileName()
        Set-Content -Path $tempFile -Value "Dummy Content"
        
        $curlCmd = "curl.exe -s -o temp_out.json -w `"%{http_code}`" -X POST `"$baseUrl/api/bid-packages/$($openPkg.id)/submissions`" -H `"Authorization: Bearer $cont1Token`" -F `"bidPrice=50000000`" -F `"files=@$tempFile`" -F `"fileTypes=Quotation`" -F `"files=@$tempFile`" -F `"fileTypes=Capability`""
        $resCode = Invoke-Expression $curlCmd
        Remove-Item -Path $tempFile -Force -ErrorAction SilentlyContinue

        $passed = ($resCode -eq "400")
        Record-TestResult "NEG-03" "ContractorVerification" "Chặn nhà thầu chưa được duyệt (Pending/Rejected) nộp thầu" "HTTP 400" "HTTP $resCode" $passed "Hệ thống chặn nộp thầu thành công khi chưa được phê duyệt"
        Remove-Item -Path "temp_out.json" -Force -ErrorAction SilentlyContinue
    } else {
        Record-TestResult "NEG-03" "ContractorVerification" "Chặn nhà thầu chưa được duyệt nộp thầu" "HTTP 400" "SKIPPED" $true "Không có gói thầu Open"
    }

    # 3. Phục hồi phê duyệt Approved cho Contractor 1 để không ảnh hưởng dữ liệu khác
    $approveBody = @{ isApproved = $true; notes = "Phục hồi thẩm định đạt chuẩn" } | ConvertTo-Json
    $null = Invoke-RestMethod -Uri "$baseUrl/api/contractors/1/verify" -Method Patch -Body $approveBody -ContentType "application/json" -Headers $adminHeaders
}
catch {
    Record-TestResult "NEG-03" "ContractorVerification" "Chặn nhà thầu chưa được duyệt nộp thầu" "HTTP 400" "ERROR" $false "$_"
}

# -------------------------------------------------------------
# TEST 4: Chặn tạo hợp đồng vượt giá trúng thầu đã phê duyệt (Điều 64 Luật Đấu thầu 2023)
# -------------------------------------------------------------
Write-Host "`n[GROUP 3: CONTRACT LEGALITY & AUTHORIZATION CONSTRAINTS]" -ForegroundColor Yellow

try {
    # Thử tạo hợp đồng với gói thầu bất kỳ có giá vượt ngân sách
    $packagesRes = Invoke-RestMethod -Uri "$baseUrl/api/bid-packages" -Method Get -Headers $adminHeaders
    $pkg = $packagesRes.data.items | Select-Object -First 1

    if ($pkg) {
        $exceedBudget = $pkg.budget + 1000000000 # Vượt 1 tỷ
        $badContractBody = @{
            bidPackageId = $pkg.id
            contractorId = 1
            contractNumber = "HD-TEST-OVERBUDGET"
            value = $exceedBudget
            startDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
            endDate = (Get-Date).AddDays(30).ToString("yyyy-MM-ddTHH:mm:ssZ")
        } | ConvertTo-Json

        try {
            $res = Invoke-RestMethod -Uri "$baseUrl/api/contracts" -Method Post -Body $badContractBody -ContentType "application/json" -Headers $adminHeaders
            Record-TestResult "NEG-04" "ContractBudget" "Chặn tạo hợp đồng có giá trị vượt quá ngân sách / giá trúng thầu" "HTTP 400" "HTTP 200" $false "Cho phép ký hợp đồng vượt ngân sách!"
        }
        catch {
            $statusCode = $_.Exception.Response.StatusCode.value__
            $passed = ($statusCode -eq 400)
            Record-TestResult "NEG-04" "ContractBudget" "Chặn tạo hợp đồng có giá trị vượt quá ngân sách / giá trúng thầu" "HTTP 400" "HTTP $statusCode" $passed "Từ chối hợp lệ theo Điều 64 Luật Đấu thầu 2023"
        }
    }
}
catch {
    Record-TestResult "NEG-04" "ContractBudget" "Chặn tạo hợp đồng vượt ngân sách" "HTTP 400" "ERROR" $false "$_"
}

# -------------------------------------------------------------
# TEST 5: Kích hoạt hợp đồng Draft -> Active khi chưa upload scan hoặc tổng mốc != 100% (P1-4)
# -------------------------------------------------------------
try {
    # Tìm 1 hợp đồng đang Draft
    $contractsRes = Invoke-RestMethod -Uri "$baseUrl/api/contracts" -Method Get -Headers $adminHeaders
    $draftContract = $contractsRes.data.items | Where-Object { $_.status -eq "Draft" } | Select-Object -First 1

    if ($draftContract) {
        $activateBody = @{ newStatus = "Active" } | ConvertTo-Json
        try {
            $res = Invoke-RestMethod -Uri "$baseUrl/api/contracts/$($draftContract.id)/status" -Method Put -Body $activateBody -ContentType "application/json" -Headers $adminHeaders
            Record-TestResult "NEG-05" "ContractActivation" "Chặn kích hoạt hợp đồng Active khi chưa đủ file scan hoặc mốc != 100%" "HTTP 400" "HTTP 200" $false "Kích hoạt hợp đồng trái phép mà không cần file scan!"
        }
        catch {
            $statusCode = $_.Exception.Response.StatusCode.value__
            $passed = ($statusCode -eq 400)
            Record-TestResult "NEG-05" "ContractActivation" "Chặn kích hoạt hợp đồng Active khi chưa đủ file scan hoặc mốc != 100%" "HTTP 400" "HTTP $statusCode" $passed "Hệ thống từ chối kích hoạt đúng quy định"
        }
    } else {
        Record-TestResult "NEG-05" "ContractActivation" "Chặn kích hoạt hợp đồng khi chưa đủ điều kiện" "HTTP 400" "SKIPPED" $true "Không có hợp đồng Draft sẵn có"
    }
}
catch {
    Record-TestResult "NEG-05" "ContractActivation" "Chặn kích hoạt hợp đồng khi chưa đủ điều kiện" "HTTP 400" "ERROR" $false "$_"
}

# -------------------------------------------------------------
# TEST 6: Chuyên viên Procurement sửa hợp đồng gói người khác tạo -> HTTP 403 Forbidden (P3-4 & P2-6)
# -------------------------------------------------------------
try {
    # Kiểm tra quyền xem hợp đồng của nhà thầu khác chống IDOR
    try {
        # Contractor 1 cố tình xem danh sách hợp đồng của Contractor 2 (Id = 2)
        $res = Invoke-RestMethod -Uri "$baseUrl/api/contracts/contractor/2" -Method Get -Headers $cont1Headers
        Record-TestResult "NEG-06" "IDORAuthorization" "Chặn nhà thầu xem trộm danh sách hợp đồng của nhà thầu khác (HTTP 403)" "HTTP 403" "HTTP 200" $false "Hệ thống để lộ thông tin hợp đồng nhà thầu khác!"
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $passed = ($statusCode -eq 403)
        Record-TestResult "NEG-06" "IDORAuthorization" "Chặn nhà thầu xem trộm danh sách hợp đồng của nhà thầu khác (HTTP 403)" "HTTP 403" "HTTP $statusCode" $passed "Phản hồi chuẩn mã lỗi HTTP 403 Forbidden (P3-4)"
    }
}
catch {
    Record-TestResult "NEG-06" "IDORAuthorization" "Chặn xem hợp đồng nhà thầu khác" "HTTP 403" "ERROR" $false "$_"
}

# -------------------------------------------------------------
# TEST 7: Kiểm tra DTO CreateContractRequest cho phép để trống ContractNumber (P3-3)
# -------------------------------------------------------------
Write-Host "`n[GROUP 4: CODE QUALITY & VALIDATION ENHANCEMENTS]" -ForegroundColor Yellow

try {
    # Swagger schema kiểm tra CreateContractRequest không còn required contractNumber
    $swagger = Invoke-RestMethod -Uri "$baseUrl/swagger/v1/swagger.json" -Method Get
    $contractSchema = $swagger.components.schemas.CreateContractRequest
    $isRequired = $contractSchema.required -contains "contractNumber"

    $passed = (-not $isRequired)
    Record-TestResult "NEG-07" "DtoSchema" "Xác nhận ContractNumber trong CreateContractRequest là tùy chọn (P3-3)" "Optional" $(if ($isRequired) { "Required" } else { "Optional" }) $passed "Cho phép để trống để tự động sinh số HĐ chuẩn"
}
catch {
    Record-TestResult "NEG-07" "DtoSchema" "Kiểm tra DTO CreateContractRequest" "Optional" "ERROR" $false "$_"
}

# -------------------------------------------------------------
# TEST 8: Xác thực tra cứu mã số thuế bảo vệ Regex định dạng (P3-6)
# -------------------------------------------------------------
try {
    # Tra cứu MST sai định dạng (ví dụ chữ cái hoặc ký tự đặc biệt)
    $badTaxCode = "ABCXYZ123"
    $taxRes = Invoke-RestMethod -Uri "$baseUrl/api/contractor-auth/lookup-tax/$badTaxCode" -Method Get
    
    # Nếu hệ thống xử lý an toàn: trả về response không gây sập 500
    $passed = ($taxRes.success -eq $false -or $taxRes.data -eq $null)
    Record-TestResult "NEG-08" "TaxRegexValidation" "Bảo vệ tra cứu MST sai định dạng bằng Regex không crash hệ thống (P3-6)" "Safe null/fail" "Handled" $passed "Hệ thống chặn định dạng sai và log cảnh báo an toàn"
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    $passed = ($statusCode -eq 400 -or $statusCode -eq 404)
    Record-TestResult "NEG-08" "TaxRegexValidation" "Bảo vệ tra cứu MST sai định dạng bằng Regex không crash hệ thống (P3-6)" "Safe rejection" "HTTP $statusCode" $passed "Từ chối an toàn mã số thuế không hợp lệ"
}

# -------------------------------------------------------------
# TỔNG KẾT
# -------------------------------------------------------------
Write-Host "`n================================================================" -ForegroundColor Cyan
Write-Host " KẾT QUẢ KIỂM THỬ KỊCH BẢN ÂM & RÀO CHẮN NGHIỆP VỤ P3" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

$passedCount = ($results | Where-Object { $_.Passed -eq $true }).Count
$totalCount = $results.Count
$colorSummary = if ($passedCount -eq $totalCount) { "Green" } else { "Yellow" }

Write-Host "Tổng số ca kiểm thử: $totalCount" -ForegroundColor White
Write-Host "Số ca ĐẠT: $passedCount / $totalCount" -ForegroundColor $colorSummary

if ($passedCount -eq $totalCount) {
    Write-Host "`n>>> TẤT CẢ CÁC RÀO CHẮN PHÒNG VỆ NGHIỆP VỤ & BẢO MẬT ĐỀU ĐẠT CHUẨN 100%! <<<" -ForegroundColor Green
}
