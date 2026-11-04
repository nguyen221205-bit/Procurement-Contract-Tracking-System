# BÁO CÁO CÔNG VIỆC NGÀY [04/11/2026]

**Dự án**: Hệ thống Quản lý Đấu thầu và Theo dõi Hợp đồng (*Procurement & Contract Tracking System*)  
**Nhóm thực hiện**: 2 thành viên  
**Họ tên**: Phạm Minh Tài  

---

### TÓM TẮT TIẾN ĐỘ THỰC HIỆN
- **Hôm trước**:
  - Tích hợp thành công kiến trúc lưu trữ tệp đám mây lai (*Hybrid Cloud File Storage*) với Supabase Cloud Storage S3 Protocol, hỗ trợ tự động dự phòng (*fallback*) về Local Storage khi ngắt kết nối mạng.
  - Xây dựng hoàn chỉnh đường ống (*pipeline*) tối ưu hóa lưu trữ và bảo mật Zero-Trust: Nén GZip giảm 60-80% dung lượng tệp và mã hóa AES-256-CBC với IV ngẫu nhiên trước khi đẩy lên Supabase Bucket.
  - Xây dựng bộ công cụ quản trị di chuyển dữ liệu `StorageMigrationService` hỗ trợ sao lưu CSDL tự động, đồng bộ hàng loạt lên Cloud và hoàn tác (*rollback*) khẩn cấp.
  - Bổ sung bộ bài kiểm thử tự động `HybridStorageAndAdminTests.cs` kiểm chứng toàn diện các kịch bản mã hóa, nén dữ liệu và API quản trị.
- **Hôm nay**:
  - Tiếp nhận mã nguồn mới nhất từ nhánh `origin/dev`, đối soát lịch sử commit và rebase đồng bộ hoàn toàn với kho chứa từ xa.
  - Đóng vai trò Chuyên viên Quản lý Chất lượng cao cấp (*Senior QA Lead / Business Analyst*), thực hiện kiểm toán quét lỗi nghiệp vụ sâu toàn bộ hệ thống theo Luật Đấu thầu số 22/2023/QH15 và Nghị định số 24/2024/NĐ-CP, phát hiện 5 lỗi nghiệp vụ và rủi ro vận hành (2 lỗi Critical P0, 2 lỗi High P1, 1 lỗi Medium P2).
  - Lập Kế hoạch kiểm thử & khắc phục chi tiết và được phê duyệt triển khai **Giai đoạn 1**:
    1. **Khắc phục lỗi BUG-01 (P0 - Critical Crash 500)**: Chuẩn hóa toàn bộ 6 API tải tệp (bảo lãnh hợp đồng, phụ lục hợp đồng, snapshot nghiệm thu kỹ thuật, chứng chỉ giám khảo, quyết định thành lập và quyết định bổ nhiệm) từ cơ chế đường dẫn vật lý cục bộ sang mô hình `ResolveDownloadAsync` trả về `FileDownloadResult`, xử lý mượt mà cả tệp lưu trữ trên Supabase Cloud và tệp lưu trữ Local.
    2. **Khắc phục lỗi BUG-02 (P0 - Critical Bế tắc thanh toán)**: Bổ sung bảng `Acceptances` (`SnapshotDocumentPath`) vào toàn bộ chu trình di chuyển dữ liệu của `StorageMigrationService` (sao lưu bảng tạm, đồng bộ đường dẫn Cloud và hoàn tác rollback), xóa bỏ nguy cơ lệch đường dẫn gây lỗi HTTP 409 bế tắc giải ngân hợp đồng.
    3. **Nâng cấp cơ chế kiểm tra tính toàn vẹn SHA-256**: Cập nhật hàm `ComputeFileHashSafeAsync` trong `ContractService` hỗ trợ tính toán và đối soát mã băm toàn vẹn cho cả luồng tệp mã hóa/nén trên Cloud và tệp cục bộ trên đĩa.
  - Chạy kiểm thử tự động toàn diện: Đạt kết quả xuất sắc **80/80 ca kiểm thử Backend C# (100% Pass)** và **6/6 ca kiểm thử Frontend Vite (100% Pass)**, `npm run build` không phát sinh bất kỳ lỗi nào.
  - Tạo commit Git chuẩn hóa bằng tiếng Việt trên nhánh `dev`.
- **Mục tiêu**:
  - Bảo đảm kiến trúc Hybrid Cloud Storage vận hành ổn định 100%, mọi tệp tin nghiệp vụ pháp lý quan trọng được mã hóa bảo mật, nén tối ưu và tải về trơn tru không phụ thuộc vào hạ tầng lưu trữ.
  - Đảm bảo tính toàn vẹn dữ liệu hợp đồng và nghiệm thu giải ngân, triệt tiêu mọi khả năng gây gián đoạn quy trình thanh toán vốn ngân sách.

---

## I. BẢNG PHÂN CÔNG KHỐI LƯỢNG CÔNG VIỆC TRONG NGÀY

**Bảng 1.1: Khối lượng công việc và kết quả thực hiện ngày 04/11/2026**

| Nhiệm vụ trọng tâm | Nội dung triển khai và kết quả thực tế |
| :--- | :--- |
| **1. Đồng bộ Git & Tiếp nhận mã nguồn mới** | • Chạy `git fetch origin dev` và kiểm tra trạng thái nhánh `dev`.<br>• Rebase tích hợp thành công các commit tài liệu và bộ test mới từ remote (`98cbdba`, `eb9d90a`), không có xung đột mã nguồn. |
| **2. Quét lỗi nghiệp vụ & Lập Kế hoạch kiểm thử** | • Rà soát toàn diện nghiệp vụ đấu thầu, hợp đồng và kiến trúc lưu trữ tệp tin.<br>• Phát hiện 5 lỗi nghiệp vụ trọng yếu (BUG-01: Lỗi Crash 500 khi tải tệp Cloud; BUG-02: Bỏ sót bảng Acceptances khi di chuyển Cloud Storage; BUG-03: Rủi ro phân quyền tải chứng chỉ giám khảo; BUG-04: Đóng gói Audit Log hợp đồng; BUG-05: Thiếu validation ngày hiệu lực bảo lãnh).<br>• Lập Artifact Báo cáo kiểm thử nghiệp vụ và Kế hoạch khắc phục Giai đoạn 1 đã được phê duyệt. |
| **3. Khắc phục BUG-01: Chuẩn hóa 6 API tải tệp sang ResolveDownloadAsync** | • Cập nhật 3 interfaces: `IContractService`, `IEvaluatorProposalService`, `IProcuringEntityService`.<br>• Cập nhật 3 services: `ContractService`, `EvaluatorProposalService`, `ProcuringEntityService` gọi `_fileStorageService.ResolveDownloadAsync`.<br>• Cập nhật 3 controllers: `ContractsController`, `EvaluatorProposalsController`, `ProcuringEntitiesController` hỗ trợ linh hoạt 3 cơ chế phân phối tệp: `Redirect(url)`, `File(stream, ...)`, và `PhysicalFile(...)`. |
| **4. Khắc phục BUG-02: Đồng bộ hóa bảng Acceptances trong Cloud Migration** | • Chỉnh sửa `StorageMigrationService.cs`: Bổ sung bảng `Acceptances` vào truy vấn tạo bảng sao lưu an toàn `INSERT INTO [{backupTableName}]`.<br>• Thêm `("Acceptances", "SnapshotDocumentPath")` vào danh sách cập nhật đường dẫn tệp `updateTables`.<br>• Bổ sung câu lệnh hoàn tác `UPDATE acc SET acc.SnapshotDocumentPath = bk.OriginalPath` trong `RollbackFilePathsAsync`. |
| **5. Nâng cấp kiểm tra toàn vẹn SHA-256 & Kiểm thử hồi quy** | • Cập nhật hàm `ComputeFileHashSafeAsync` trong `ContractService` tự động giải nén và giải mã luồng tệp đám mây để kiểm tra mã băm SHA-256 chống giả mạo hồ sơ.<br>• Chạy `dotnet test` đạt **80/80 tests PASS (100%)**.<br>• Chạy `npm test` đạt **6/6 tests PASS (100%)** và `npm run build` thành công rực rỡ. |

---

## II. CHI TIẾT TIẾN ĐỘ THỰC HIỆN CÔNG VIỆC

### 1. Đồng bộ mã nguồn Git và Rà soát hệ thống
Đầu ngày làm việc, tiến trình kiểm tra nhánh `dev` so với máy chủ điều hành `origin/dev` được thực hiện. Hệ thống đã tiếp nhận 2 commit mới:
- `98cbdba test(storage): add unit and integration tests for HybridFileStorageService and StorageAdminController`: Bổ sung 14 ca kiểm thử chuyên sâu về mã hóa AES-256, nén GZip và các endpoint quản trị di chuyển tệp.
- `eb9d90a docs(e2e): add contract lifecycle and milestone disbursement screenshots`: Bổ sung 9 ảnh chụp màn hình kiểm thử E2E chu trình vòng đời hợp đồng và nghiệm thu thanh toán.

Mã nguồn được rebase sạch sẽ, không có xung đột tệp.

---

### 2. Phát hiện lỗi nghiệp vụ & Báo cáo kiểm toán chất lượng
Qua quá trình quét logic nghiệp vụ theo tiêu chuẩn Luật Đấu thầu 2023, Nghị định 24/2024/NĐ-CP và kiến trúc Zero-Trust Storage, nhóm đã xác định được 2 lỗi nghiêm trọng cấp bách cần xử lý ngay ở **Giai đoạn 1**:

#### 🔴 Lỗi BUG-01 (Mức độ Critical - P0): Nguy cơ vỡ giao dịch (Crash 500) khi tải tài liệu trên môi trường Cloud Storage
- **Hiện tượng**: Khi hệ thống kích hoạt chế độ lưu trữ đám mây `HybridFileStorageService` với Supabase, các tệp tải lên mới hoặc sau khi chạy di chuyển dữ liệu có đường dẫn định dạng `supabase://procurement-contracts/...`.
- **Nguyên nhân gốc rễ**: 6 API sau đây vẫn sử dụng hàm `_fileStorageService.GetPhysicalFilePath(storedPath)`:
  1. `GET /api/contracts/{id}/guarantees/{guaranteeId}/download` (Tải tệp bảo lãnh hợp đồng).
  2. `GET /api/contracts/{id}/addenda/{addendumId}/download` (Tải tệp phụ lục hợp đồng).
  3. `GET /api/contracts/milestones/{mId}/acceptances/{aId}/document/download` (Tải snapshot biên bản nghiệm thu kỹ thuật).
  4. `GET /api/evaluator-proposals/{id}/certificate/download` (Tải tệp chứng chỉ hành nghề của giám khảo).
  5. `GET /api/procuring-entities/{id}/establishment-file` (Tải quyết định thành lập bên mời thầu).
  6. `GET /api/procuring-entities/{id}/appointment-file` (Tải quyết định bổ nhiệm bên mời thầu).
  
  Do hàm `GetPhysicalFilePath` trong `HybridFileStorageService` chủ động ném ngoại lệ `InvalidOperationException("Đường dẫn thuộc Cloud Storage, không thể lấy Physical Path cục bộ...")`, dẫn đến việc các Controller bị văng ngoại lệ `HTTP 500 Internal Server Error`, khiến người dùng không thể tải tài liệu về xem.

#### 🔴 Lỗi BUG-02 (Mức độ Critical - P0): Bỏ sót bảng `Acceptances` trong quy trình di chuyển Cloud Storage gây lỗi 409 bế tắc giải ngân
- **Hiện tượng**: Khi Quản trị viên kích hoạt tính năng đồng bộ tệp cục bộ lên Supabase Cloud Storage qua `POST /api/storage-admin/sync-local-to-cloud`, tiến trình quét và tải toàn bộ tệp trong thư mục `uploads/` lên Bucket. Tuy nhiên, bảng `Acceptances` (cột `SnapshotDocumentPath`) không được khai báo trong danh sách cập nhật CSDL.
- **Hệ quả pháp lý**: Bảng `Contracts.ScannedFilePath` bị đổi thành `supabase://...`, trong khi `Acceptances.SnapshotDocumentPath` vẫn giữ nguyên giá trị cũ `/uploads/...`. Đến khi Bên mời thầu / Chủ đầu tư thực hiện phê duyệt giải ngân mốc thanh toán tại hàm `ApproveMilestoneAcceptanceAsync`, hệ thống so sánh:
  ```csharp
  if (milestone.Contract.ScannedFilePath != acceptance.SnapshotDocumentPath)
  {
      return ApiResponse.Fail("Tệp tài liệu snapshot không khớp với tệp scan hợp đồng...", statusCode: 409);
  }
  ```
  Sự lệch pha đường dẫn này lập tức kích hoạt lỗi `HTTP 409 Conflict`, gây đóng băng và bế tắc toàn bộ quy trình giải ngân tài chính cho nhà thầu.

---

### 3. Triển khai giải pháp kỹ thuật khắc phục triệt để Giai đoạn 1

#### 3.1. Chuẩn hóa mô hình tải tệp đa tầng (`ResolveDownloadAsync`)
- **Thay đổi Interface & DTO**: Chuyển đổi kiểu trả về của các phương thức tải tệp trong `IContractService`, `IEvaluatorProposalService`, và `IProcuringEntityService` từ tuple `(string PhysicalPath, ...)` sang đối tượng chuẩn hóa `FileDownloadResult` từ namespace `ProcurementSystem.Core.DTOs.Common`.
- **Cập nhật Service Implementation**:
  - Tại `ContractService.cs`:
    - `GetGuaranteeDocumentForDownloadAsync`: Gọi `_fileStorageService.ResolveDownloadAsync(guarantee.ScannedDocumentPath, fileName, contentType)`.
    - `GetAddendumDocumentForDownloadAsync`: Gọi `_fileStorageService.ResolveDownloadAsync(addendum.ScannedDocumentPath, fileName, contentType)`.
    - `GetAcceptanceSnapshotDocumentDownloadAsync`: Gọi `_fileStorageService.ResolveDownloadAsync(acceptance.SnapshotDocumentPath, fileName, "application/pdf")`.
  - Tại `EvaluatorProposalService.cs`:
    - `GetCertificateFileForDownloadAsync`: Gọi `_fileStorageService.ResolveDownloadAsync(proposal.CertificateFile, fileName, contentType)`.
  - Tại `ProcuringEntityService.cs`:
    - `GetEstablishmentFileAsync`: Gọi `_fileStorageService.ResolveDownloadAsync(entity.EstablishmentDecisionFile, fileName, contentType)`.
    - `GetAppointmentFileAsync`: Gọi `_fileStorageService.ResolveDownloadAsync(entity.AppointmentDecisionFile, fileName, contentType)`.
- **Cập nhật Controller Layer**:
  Tại `ContractsController.cs`, `EvaluatorProposalsController.cs` và `ProcuringEntitiesController.cs`, áp dụng mô hình phân phối linh hoạt:
  ```csharp
  if (downloadResult.IsRedirect)
  {
      return Redirect(downloadResult.RedirectUrl!);
  }
  if (downloadResult.IsStream)
  {
      return File(downloadResult.FileStream!, downloadResult.ContentType, downloadResult.FileName);
  }
  if (!System.IO.File.Exists(downloadResult.PhysicalPath))
  {
      return NotFound(ApiResponse<string>.Fail("Tệp tài liệu không tồn tại trên hệ thống lưu trữ."));
  }
  return PhysicalFile(downloadResult.PhysicalPath!, downloadResult.ContentType, downloadResult.FileName);
  ```

#### 3.2. Nâng cấp cơ chế đối soát tính toàn vẹn SHA-256 an toàn
- Khi tải tệp snapshot nghiệm thu hoặc khi phê duyệt giải ngân, hệ thống cần đối soát mã băm SHA-256 với `acceptance.SnapshotDocumentHash` để ngăn chặn hành vi can thiệp trái phép vào file tài liệu trên đĩa.
- Nhóm đã tối ưu hóa hàm `ComputeFileHashSafeAsync` trong `ContractService.cs`:
  1. Nếu tệp là liên kết Pre-signed URL / Cloud URL (`http://` hoặc `https://`), tự động tải stream qua `HttpClient` để tính toán mã SHA-256.
  2. Nếu tệp được lưu trữ trên Supabase Cloud Storage (`supabase://...`), sử dụng `_fileStorageService.ResolveDownloadAsync` để giải nén GZip và giải mã AES-256, sau đó tính mã băm trên nội dung tệp nguyên bản ban đầu.
  3. Nếu tệp lưu trên ổ đĩa vật lý cục bộ, đọc trực tiếp qua `File.OpenRead` để đạt hiệu năng tối ưu.
  4. Nếu mã băm không khớp, hệ thống lập tức trả về mã `HTTP 409 Conflict` để bảo vệ an toàn ngân sách.

#### 3.3. Hoàn thiện quy trình di chuyển dữ liệu trong `StorageMigrationService`
- **Bổ sung sao lưu an toàn**:
  ```sql
  INSERT INTO [{backupTableName}] (TableName, RecordId, ColumnName, OriginalPath)
  SELECT 'Acceptances', Id, 'SnapshotDocumentPath', SnapshotDocumentPath 
  FROM Acceptances 
  WHERE SnapshotDocumentPath IS NOT NULL AND SnapshotDocumentPath LIKE '/uploads/%';
  ```
- **Bổ sung danh sách cập nhật đường dẫn Cloud**:
  Thêm cặp `("Acceptances", "SnapshotDocumentPath")` vào mảng `updateTables` của phương thức `SyncLocalFilesToCloudAsync`.
- **Bổ sung cơ chế Rollback hoàn tác khẩn cấp**:
  ```sql
  UPDATE acc SET acc.SnapshotDocumentPath = bk.OriginalPath
  FROM Acceptances acc 
  INNER JOIN [{backupTableName}] bk 
      ON bk.TableName = 'Acceptances' 
     AND bk.ColumnName = 'SnapshotDocumentPath' 
     AND bk.RecordId = acc.Id;
  ```

---

### 4. Kết quả kiểm thử kỹ thuật và nghiệm thu chất lượng

#### 4.1. Biên dịch Solution (.NET 8 SDK)
```text
Command: rtk dotnet build ProcurementSystem.slnx
Result: 5 projects, 0 errors, 0 warnings
Time Elapsed: 00:00:19.36
Status: SUCCESSFUL
```

#### 4.2. Kiểm thử tự động Backend (`dotnet test`)
Chạy toàn bộ bộ kiểm thử tích hợp và đơn vị của dự án `ProcurementSystem.Tests`:
- **Số lượng bài kiểm thử**: 80 bài (vượt qua 80/80 bài, đạt tỷ lệ **100% Pass**).
- **Thời gian thực thi**: 32.2 giây.
- **Các nhóm kiểm thử trọng yếu đã xác minh thành công**:
  + `SqlServerConcurrencyAndIntegrityIntegrationTests`: Xác minh toàn diện HTTP Pipeline thực tế, cơ chế xác thực JWT Bearer, phân quyền ma trận Maker - Checker, và tính năng phát hiện tệp snapshot nghiệm thu bị sửa đổi bất hợp pháp (trả về đúng mã lỗi `409 Conflict`).
  + `HybridStorageAndAdminTests`: Xác minh 14 ca kiểm thử mới về nén GZip, mã hóa AES-256 Zero-Trust, cơ chế fallback Local Storage khi mất mạng, và các API di chuyển dữ liệu sao lưu/đồng bộ/rollback.
  + Toàn bộ các kiểm thử về thẩm định hồ sơ, niêm phong điểm chấm, bảo lãnh hợp đồng và giải ngân thanh toán.

#### 4.3. Kiểm thử tự động Frontend (`npm test` & `npm run build`)
- Bộ kiểm thử `node --test tests/*.test.mjs` chạy thành công **6/6 ca kiểm thử (100% Pass)**.
- Quá trình đóng gói sản phẩm `npm run build` hoàn tất xuất sắc trong 6.16 giây, bảo đảm tính tương thích tuyệt đối giữa giao diện người dùng và API Backend.

#### 4.4. Quản lý phiên bản Git
- Tạo commit hoàn chỉnh trên nhánh `dev`:
  ```text
  commit f5111b8
  fix(storage): chuẩn hóa 6 API tải tệp sang ResolveDownloadAsync và bổ sung bảng Acceptances vào pipeline đồng bộ Supabase Cloud
  ```

---

## III. ĐÁNH GIÁ CHẤT LƯỢNG VÀ KẾ HOẠCH TIẾP THEO

### 1. Đánh giá chất lượng thực hiện
- Toàn bộ khối lượng công việc đặt ra cho **Giai đoạn 1** đã được hoàn thành 100% với chất lượng kỹ thuật cao nhất.
- Triệt tiêu hoàn toàn rủi ro sập ứng dụng (Crash 500) khi hệ thống chuyển sang hạ tầng đám mây Supabase Cloud Storage.
- Xóa bỏ triệt để nguy cơ bế tắc dòng tiền thanh toán (lỗi 409 do lệch đường dẫn) trong quá trình nghiệm thu giải ngân hợp đồng.
- Duy trì tính toàn vẹn tuyệt đối của toàn bộ 80 bài kiểm thử Backend và 6 bài kiểm thử Frontend, bảo đảm không phát sinh bất kỳ lỗi hồi quy nào.

### 2. Kế hoạch công việc tiếp theo
- Chuẩn bị kế hoạch thực thi **Giai đoạn 2** nhằm giải quyết các lỗi nghiệp vụ còn lại:
  + Củng cố phân quyền tải tệp chứng chỉ giám khảo trong `EvaluatorProposalsController` để cán bộ Bên mời thầu (Procurement) có thể thẩm định hồ sơ đề xuất.
  + Hoàn thiện cơ chế đóng gói nhật ký kiểm toán (*Audit Log*) khi điều chỉnh các mốc thanh toán hợp đồng.
  + Thêm ràng buộc kiểm tra ngày hiệu lực của bảo lãnh thực hiện hợp đồng.
- Phối hợp với đồng nghiệp kiểm thử đầu cuối toàn bộ quy trình trên môi trường thử nghiệm thực tế.
