# KẾ HOẠCH PHÂN CHIA CÔNG VIỆC KỸ THUẬT HÔM NAY
**Dự án:** Hệ thống Quản lý Đấu Thầu & Theo Dõi Hợp Đồng (*Procurement & Contract Tracking System*)  
**Ngày thực hiện:** 22/09/2026 (Thứ Ba - Tuần 5)  
**Nhóm thực hiện:** 2 thành viên (Dev 1 & Dev 2)  
**Mục tiêu tổng quát:** Kiểm thử Bảo mật Nâng cao (Security & RBAC Audit), Kiểm thử Tải Hiệu năng Đồng thời (Concurrency & Load Testing), Hoàn thiện Luồng Nghiệp vụ Nghiệm thu - Giải ngân Hợp đồng và Chuẩn hóa Tài liệu Đặc tả API.

---

## 📌 PHẦN 1: DEV 1 (Track 1 - Phạm Minh Tài)
> **Phạm vi trọng tâm:** Kiểm thử Bảo mật An ninh Toàn diện, Kiểm thử Hiệu năng & Khả năng Chịu tải Đồng thời, Thẩm tra Chỉ mục Cơ sở Dữ liệu.

### 1.1. Kiểm toán An ninh Phân quyền (RBAC) & Chống Leo thang Đặc quyền (Privilege Escalation)
- **Kiểm thử ranh giới phân quyền giữa 4 vai trò:** `Admin`, `Procurement`, `Evaluator`, `Contractor`.
- **Kiểm thử các ca vi phạm phân quyền (Negative Test Cases):**
  - Tài khoản Nhà thầu (`Contractor`) cố tình gọi các API quản trị (Tạo/sửa gói thầu, chấm điểm, phê duyệt trao thầu, xem hồ sơ của nhà thầu đối thủ) $\rightarrow$ Hệ thống bắt buộc từ chối và trả về mã `HTTP 403 Forbidden`.
  - Gọi API mà không đính kèm Token hoặc Token giả mạo/hết hạn $\rightarrow$ Bắt buộc trả về `HTTP 401 Unauthorized`.
- **Kiểm toán an ninh chống lỗ hổng IDOR (Insecure Direct Object References):**
  - Thẩm tra API tải file đề xuất kỹ thuật (`GET /api/bid-submissions/{submissionId}/files/{fileId}/download`), đảm bảo nhà thầu khác không thể truy cập trái phép vào tệp đính kèm của đối thủ.

### 1.2. Kiểm thử Tải Đồng thời & Đo lường Hiệu năng (Concurrency & Load Testing)
- **Xây dựng kịch bản kiểm thử tải đa luồng** (giả lập 50 – 100 requests đồng thời dồn dập):
  - Tra cứu và lọc gói thầu theo trạng thái/hạn nộp (`GET /api/bid-packages`).
  - Truy vấn thống kê Dashboard quản trị (`GET /api/reports/dashboard`).
  - Tra cứu lịch sử hợp đồng nhà thầu (`GET /api/contracts/contractor/{id}`).
- **Đo lường & thu thập chỉ số hiệu năng thực tế:**
  - Thời gian phản hồi trung bình (*Average Latency*).
  - Thông lượng xử lý (*Throughput - Requests per second*).
  - Tỷ lệ phản hồi thành công (*Success Rate* đạt yêu cầu tuyệt đối, 0% lỗi xung đột kết nối cơ sở dữ liệu).
  - Đối soát và chứng minh hiệu quả tăng tốc độ truy vấn của các **Non-Clustered Indexes** đã cấu hình vào cơ sở dữ liệu SQL Server.

### 1.3. Tổng hợp Báo cáo Kỹ thuật Ngày 15
- Lưu vết toàn bộ log kiểm thử bảo mật và bảng số liệu benchmark hiệu năng để xuất bản báo cáo thực tập ngày 15.

---

## 📌 PHẦN 2: DEV 2 (Track 2 - Teammate)
> **Phạm vi trọng tâm:** Hoàn thiện Logic Nghiệp vụ Nghiệm thu - Giải ngân Hợp đồng, Ràng buộc Dòng tiền và Chuẩn hóa Tài liệu Đặc tả API.

### 2.1. Hoàn thiện Logic Nghiệp vụ Nghiệm thu & Chuyển đổi Trạng thái Hợp đồng
- **Tự động hóa trạng thái hoàn thành hợp đồng:**
  - Xây dựng logic tự động kiểm tra: Khi tất cả các mốc thanh toán (`ContractMilestones`) của một hợp đồng đều đã được phê duyệt nghiệm thu (`Status == Completed`), hệ thống tự động chuyển trạng thái Hợp đồng (`Contract`) từ `Active` sang `Completed`.
- **Bổ sung ràng buộc kiểm tra ngân sách mốc thanh toán (Validation Rule):**
  - Khi tạo hoặc cập nhật mốc nghiệm thu: Kiểm tra tổng giá trị của tất cả các mốc thanh toán (`Sum(Milestone.Amount)`) không được phép vượt quá tổng giá trị của Hợp đồng (`Contract.Value`). Nếu vượt quá, trả về lỗi `HTTP 400 Bad Request` với thông báo rõ ràng.
- **Đồng bộ số tiền giải ngân thực tế:**
  - Cập nhật số tiền đã giải ngân (`TotalDisbursedAmount`) theo từng đợt nghiệm thu thành công phục vụ việc hiển thị dòng tiền chính xác trên Dashboard báo cáo.

### 2.2. Chuẩn hóa Tài liệu Đặc tả API (Swagger XML Comments & Error Schema)
- **Bổ sung chú thích XML Documentation cho Controllers & DTOs:**
  - Viết đầy đủ thẻ `<summary>`, `<param>`, `<remarks>`, `<response code="...">` cho các Endpoints trong phân hệ Hợp đồng (`ContractsController`) và Hồ sơ thầu (`BidSubmissionsController`).
- **Chuẩn hóa schema phản hồi lỗi trên Swagger UI:**
  - Khai báo rõ ràng các mã trạng thái phản hồi: `HTTP 200 OK`, `HTTP 201 Created`, `HTTP 400 Bad Request`, `HTTP 401 Unauthorized`, `HTTP 403 Forbidden`, `HTTP 404 Not Found`, `HTTP 409 Conflict`.
  - Giúp giao diện Swagger UI chuyên nghiệp, dễ tra cứu và dễ dàng bàn giao cho đội ngũ Frontend / Tester.

### 2.3. Đóng gói Bộ Sưu Tập Kiểm thử (Postman Collection v2)
- Cập nhật và xuất bản tệp `Procurement_Contracts_Collection.json` bao gồm đầy đủ các API mới phát triển (Báo cáo tiến độ, Tải file kiểm toán IDOR, Phê duyệt nghiệm thu mốc).
- Cấu hình sẵn Environment Variables (`baseUrl`, `adminToken`, `contractorToken`) để tiện kiểm thử tích hợp chéo giữa 2 thành viên.

---

## 🤝 QUY TRÌNH PHỐI HỢP & QUẢN LÝ MÃ NGUỒN (GIT WORKFLOW)

1. **Nhánh làm việc:**
   - Cả hai thành viên tạo nhánh tính năng riêng biệt từ `dev`:
     - Dev 1: `feature/security-load-testing`
     - Dev 2: `feature/contract-acceptance-docs`
2. **Quy tắc Commit:**
   - Sử dụng Conventional Commits: `feat(...)`, `test(...)`, `docs(...)`, `fix(...)`.
3. **Quy trình Hợp nhất (Merge):**
   - Trước khi tạo Pull Request vào nhánh `dev`, thực hiện kéo cập nhật mới nhất bằng `git pull origin dev` và chạy `dotnet build` để bảo đảm **0 Warning, 0 Error**.
4. **Mốc thời gian dự kiến (Deadline):**
   - **16:30:** Hoàn thành các tính năng và kịch bản kiểm thử riêng.
   - **17:00:** Hợp nhất mã nguồn vào nhánh `dev` chung và đối soát kết quả toàn hệ thống.
