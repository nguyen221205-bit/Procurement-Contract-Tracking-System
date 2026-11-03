# BÁO CÁO CÔNG VIỆC NGÀY [03/11/2026]

**Dự án**: Hệ thống Quản lý Đấu thầu và Theo dõi Hợp đồng (*Procurement & Contract Tracking System*)  
**Nhóm thực hiện**: 2 thành viên  
**Họ tên**: Phạm Minh Tài  

---

### TÓM TẮT TIẾN ĐỘ THỰC HIỆN
- **Hôm trước**:
  - Thực hiện thành công migration trên cơ sở dữ liệu bản sao độc lập `procurement_db_replica`, đối soát toàn bộ các gói thầu cũ mang cờ `OwnershipNeedsReview`.
  - Kiểm thử E2E giao diện chọn Bên mời thầu khi tạo gói thầu (`CreatePackageModal.jsx`) và chu trình hợp đồng Maker - Checker (`ContractDetailPage.jsx`), xác nhận cơ chế chặn tự phê duyệt giải ngân hoạt động chuẩn xác.
  - Triển khai toàn diện kiểm thử tự động E2E phân hệ Cấp phát tài khoản và Phân quyền thành viên nội bộ qua Playwright, thu thập đầy đủ 9 ảnh chụp màn hình minh chứng.
  - Khắc phục triệt để các lỗi phân quyền endpoint bên mời thầu, bổ sung route alias cho đổi vai trò và sửa lỗi icon giao diện.
- **Hôm nay**:
  - Kiểm tra và xác nhận đồng bộ mã nguồn Git trên nhánh `dev` so với remote `origin/dev`, bảo đảm trạng thái làm việc sạch sẽ trước khi tiến hành kiểm thử.
  - Chạy kiểm thử hồi quy tự động toàn bộ 63 bài kiểm thử Backend C# (.NET 9) và 6 bài kiểm thử Frontend JavaScript (Vite), xác nhận toàn bộ hệ thống đạt chất lượng ổn định và không phát sinh lỗi hồi quy.
  - Thiết lập kế hoạch kiểm thử chi tiết và triển khai toàn diện kiểm thử E2E phân hệ "Đánh giá hồ sơ dự thầu & Phê duyệt kết quả lựa chọn nhà thầu" (*Bid Evaluation & Award Approval*) trên trình duyệt Chromium qua Playwright.
  - Kiểm chứng chuyên sâu nguyên tắc phân tách trách nhiệm Maker - Checker: Giám khảo chấm điểm chuyên môn độc lập (Maker) bị khóa quyền tự phê duyệt trao thầu; Bên mời thầu / Chủ đầu tư (Checker) truy cập trung tâm trao thầu, đối soát bục vinh danh và phê duyệt trúng thầu chính thức theo đúng quy định pháp lý.
  - Thu thập đầy đủ 9 ảnh chụp màn hình minh chứng thực tế trên giao diện, kiểm chứng tính năng khóa bảng điểm sau trao thầu và xuất Biên bản Đánh giá HSDT chính thức theo Nghị định số 24/2024/NĐ-CP.
- **Mục tiêu**:
  - Bảo đảm quy trình thẩm định, chấm điểm hồ sơ dự thầu và phê duyệt kết quả lựa chọn nhà thầu tuân thủ nghiêm ngặt Luật Đấu thầu số 22/2023/QH15 và Nghị định số 24/2024/NĐ-CP.
  - Bảo đảm tính toàn vẹn dữ liệu điểm số, cơ chế bảo mật niêm phong điểm chấm trước khi mở kết quả và tự động khóa bảng điểm lưu trữ an toàn ngay sau khi phê duyệt.

---

## I. BẢNG PHÂN CÔNG KHỐI LƯỢNG CÔNG VIỆC TRONG NGÀY

**Bảng 1.1: Khối lượng công việc và kết quả thực hiện ngày 03/11/2026**

| Nhiệm vụ trọng tâm | Nội dung triển khai và kết quả thực tế |
| :--- | :--- |
| **1. Đồng bộ Git & Kiểm thử hồi quy tự động** | • Thực hiện lệnh `git fetch` và `git status`, xác nhận nhánh `dev` cập nhật đồng bộ hoàn toàn với remote `origin/dev`.<br>• Chạy kiểm thử hồi quy `dotnet test` trên toàn bộ 63 bài test Backend C# tại dự án `ProcurementSystem.Tests`, đạt kết quả hoàn hảo 63/63 bài vượt qua.<br>• Chạy kiểm thử `npm test` trên Frontend, vượt qua toàn bộ 6/6 bài test giao diện. |
| **2. Chuẩn bị dữ liệu kiểm thử & Hội đồng thẩm định** | • Thiết lập gói thầu kiểm thử ID 2070 (Gói thầu Mua sắm Thiết bị Mạng & An toàn Thông tin 2026, dự toán 2.000.000.000 đồng) trên CSDL bản sao `procurement_db_replica`.<br>• Cấu hình Tổ chuyên gia gồm 2 giám khảo độc lập: Giám khảo ID 29 đã chấm điểm trước và Giám khảo ID 5 (`evaluator@procurement.com`).<br>• Gán 2 hồ sơ dự thầu hợp lệ từ Công ty Đầu tư và Phát triển Công nghệ (giá dự thầu 1.500.000.000 đồng) và Công ty Khang Nghị (giá dự thầu 1.600.000.000 đồng). |
| **3. Kiểm thử tự động E2E Đánh giá & Phê duyệt trúng thầu** | • Xây dựng đầy đủ Kế hoạch kiểm thử, Danh mục kịch bản, Bảng chi tiết 9 ca kiểm thử E2E (Test Cases Table).<br>• Chạy kịch bản Playwright tự động trên trình duyệt Chromium thực tế, kiểm chứng quy trình chấm điểm ma trận Scorecard và nguyên tắc phân tách Maker - Checker.<br>• Xác nhận Giám khảo (Maker) bị chặn hoàn toàn nút phê duyệt; Bên mời thầu (Checker) đăng nhập đối soát bục vinh danh và phê duyệt trúng thầu thành công. |
| **4. Kiểm chứng khóa bảng điểm & Xuất Báo cáo Đánh giá HSDT** | • Kiểm chứng trạng thái gói thầu tự động chuyển sang Đã trao thầu (*Awarded*), hiển thị banner khóa dữ liệu bảng điểm chống chỉnh sửa trái phép.<br>• Kích hoạt thành công hộp thoại Biên bản Đánh giá HSDT chính thức theo Nghị định số 24/2024/NĐ-CP với đầy đủ cơ sở pháp lý, chữ ký thẩm quyền và hỗ trợ in ấn/tải về tài liệu. |

---

## II. CHI TIẾT TIẾN ĐỘ THỰC HIỆN CÔNG VIỆC

### 1. Đồng bộ mã nguồn Git và Kiểm thử hồi quy tự động
Trước khi bước vào đợt kiểm thử phân hệ Đánh giá hồ sơ dự thầu, việc kiểm tra tính toàn vẹn mã nguồn được tiến hành qua Git CLI. Trạng thái kho chứa cục bộ hoàn toàn đồng nhất với nhánh `dev` trên máy chủ điều hành từ xa, không tồn tại xung đột hoặc bản vá tồn đọng. Kế tiếp, toàn bộ hệ thống kiểm thử hồi quy tự động được kích hoạt:
- **Kiểm thử Backend**: Lệnh `dotnet test` thực thi trên dự án `ProcurementSystem.Tests` đạt kết quả vượt qua hoàn hảo toàn bộ 63/63 ca kiểm thử, bao gồm các bài kiểm tra logic tính điểm bình quân trọng số, nguyên tắc niêm phong điểm số (Sealed Scoring) và kiểm tra ràng buộc toàn vẹn khi phê duyệt kết quả trao thầu.
- **Kiểm thử Frontend**: Bộ kiểm thử đơn vị giao diện thực thi qua Vitest/Jest hoàn thành đạt yêu cầu với 6/6 ca kiểm thử thành công, bảo đảm các thành phần hiển thị thẻ trạng thái và bảng tính toán không bị ảnh hưởng sau các đợt cập nhật.

### 2. Thiết lập Dữ liệu kiểm thử và Mô hình Hội đồng thẩm định độc lập
Để đảm bảo quy trình kiểm thử phản ánh chính xác nghiệp vụ đấu thầu thực tế theo quy định pháp lý, môi trường kiểm thử được cấu hình trên cơ sở dữ liệu bản sao `procurement_db_replica` với các đối tượng cụ thể như sau:
- **Gói thầu thẩm định**: ID 2070, mã gói thầu `PKG-2026-0009`, tên *Gói thầu Mua sắm Thiết bị Mạng & An toàn Thông tin 2026*, ngân sách dự toán 2.000.000.000 đồng, trạng thái ban đầu Đang đánh giá (*Evaluating*).
- **Hội đồng chuyên gia thẩm định**: Bao gồm 2 giám khảo độc lập: Giám khảo 1 (ID 29) đã hoàn tất chấm điểm trước đó, và Giám khảo 2 (ID 5 - tài khoản `evaluator@procurement.com`) đóng vai trò người lập đề xuất chấm điểm chuyên môn (Maker).
- **Các đơn vị tham gia dự thầu**: Hồ sơ số 1075 của Công ty Đầu tư và Phát triển Công nghệ (giá dự thầu 1.500.000.000 đồng) và Hồ sơ số 1076 của Công ty TNHH Thương mại và Dịch vụ Kỹ thuật Khang Nghị (giá dự thầu 1.600.000.000 đồng).
- **Bên mời thầu / Chủ đầu tư**: Tài khoản `procurement@procurement.com` quản lý pháp nhân Ban Quản lý Dự án Đầu tư Xây dựng Mua sắm Tập trung (ID 1), đóng vai trò người kiểm soát và phê duyệt kết quả lựa chọn nhà thầu (Checker).

### 3. Chi tiết Kiểm thử E2E: Đánh giá HSDT & Phê duyệt trúng thầu

#### 3.1. Kế hoạch kiểm thử (Test Plan)
**Bảng 2.1: Kế hoạch kiểm thử (Test Plan) phân hệ Đánh giá HSDT & Phê duyệt trúng thầu**

| Hạng mục kế hoạch | Nội dung chi tiết |
| :--- | :--- |
| **Mục tiêu kiểm thử** | • Xác minh quy trình Giám khảo thẩm định và nhập điểm ma trận Scorecard theo từng tiêu chí chuyên môn.<br>• Kiểm chứng nguyên tắc bảo mật niêm phong điểm chấm (Sealed Scoring): Giám khảo không được biết điểm của giám khảo khác và không thấy bục vinh danh trước khi hoàn tất.<br>• Kiểm chứng cơ chế phân tách trách nhiệm Maker - Checker (Anti-Self Approval): Giám khảo (Maker) bị chặn hoàn toàn quyền tự phê duyệt trao thầu.<br>• Kiểm chứng thẩm quyền của Bên mời thầu (Checker): Đối soát bục vinh danh, điểm bình quân trọng số và thực hiện phê duyệt trao thầu chính thức.<br>• Kiểm tra tính năng tự động khóa bảng điểm lưu trữ và xuất Báo cáo Đánh giá HSDT chính thức theo Nghị định số 24/2024/NĐ-CP. |
| **Phạm vi kiểm thử** | • Giao diện Web SPA: `/login`, `/evaluation`, `/evaluation/{id}`, Tab Ma trận Chấm điểm, Tab Bảng xếp hạng & Trao thầu, Modal Phê duyệt Trao thầu, Modal Báo cáo Đánh giá HSDT.<br>• Backend API: Endpoints `/api/evaluations/packages/{id}/scorecard`, `/api/evaluations/scores`, `/api/evaluations/packages/{id}/rankings`, `/api/evaluations/packages/{id}/finalize`, `/api/evaluations/packages/{id}/report`.<br>• Cơ sở dữ liệu: Các bảng `BidPackages`, `BidSubmissions`, `EvaluationScores`, `EvaluationCriteria`, `BidPackageEvaluators`. |
| **Môi trường kiểm thử** | • Trình duyệt: Chromium Headless (viewport 1280x850) thông qua công cụ Playwright Sync API.<br>• Máy chủ ứng dụng: Backend ASP.NET Core 9.0 (port 5225), Frontend Vite React (port 5173).<br>• Máy chủ CSDL: SQL Server 2022 Localhost (CSDL bản sao độc lập `procurement_db_replica`). |
| **Tài khoản kiểm thử** | • Giám khảo chấm điểm (Maker): `evaluator@procurement.com` (Vai trò Evaluator, ID 5).<br>• Bên mời thầu / Chủ đầu tư (Checker): `procurement@procurement.com` (Vai trò Procurement, ID 4). |
| **Tiêu chuẩn chấp nhận** | • Giám khảo nhập điểm và lưu phiếu chấm điểm thành công, hiển thị thông báo toast màu xanh.<br>• Giám khảo truy cập tab xếp hạng không nhìn thấy nút Phê duyệt Trao thầu (số lượng nút bằng 0).<br>• Bên mời thầu nhìn thấy đầy đủ khối Trung Tâm Phê Duyệt Trao Thầu, bục vinh danh quán quân và nút phê duyệt khả dụng.<br>• Phê duyệt trao thầu thành công chuyển trạng thái gói thầu sang Đã trao thầu, hiển thị banner Bảng điểm đã khóa.<br>• Hộp thoại Biên bản Đánh giá HSDT chính thức hiển thị đầy đủ văn bản hành chính theo đúng quy định pháp luật. |

#### 3.2. Danh mục kịch bản kiểm thử (Test Scenarios)
**Bảng 2.2: Danh mục kịch bản kiểm thử (Test Scenarios)**

| Mã kịch bản | Tên kịch bản kiểm thử | Mục đích nghiệp vụ |
| :---: | :--- | :--- |
| **TS-EVAL-01** | Truy cập Phân hệ & Mở Ma trận Chấm điểm (Scorecard) | Kiểm tra khả năng hiển thị danh sách gói thầu được phân công và nạp ma trận chấm điểm ban đầu của giám khảo. |
| **TS-EVAL-02** | Chấm điểm chuyên môn & Lưu phiếu đánh giá HSDT | Kiểm tra nhập điểm số tiêu chí kỹ thuật, ghi chú nhận xét chuyên môn và lưu phiếu điểm thành công. |
| **TS-EVAL-03** | Kiểm soát phân tách trách nhiệm Maker - Checker | Kiểm tra cơ chế chặn giám khảo tự phê duyệt trao thầu và bảo đảm tính bảo mật niêm phong điểm số. |
| **TS-EVAL-04** | Phê duyệt Trao thầu & Xuất Biên bản Đánh giá chính thức | Kiểm tra quy trình Bên mời thầu đối soát bục vinh danh, phê duyệt trúng thầu, khóa bảng điểm và mở báo cáo chính thức. |

#### 3.3. Bảng Chi tiết Ca kiểm thử (Test Cases Table)
**Bảng 2.3: Bảng chi tiết các ca kiểm thử E2E phân hệ Đánh giá & Phê duyệt trúng thầu**

| Mã TC | Tên ca kiểm thử & Các bước thực hiện | Dữ liệu kiểm thử | Kết quả mong muốn (Hiển thị màn hình) | Kết quả thực tế (Hiển thị màn hình) | Trạng thái |
| :---: | :--- | :--- | :--- | :--- | :---: |
| **TC-EVAL-01** | **Giám khảo đăng nhập và vào Trung tâm Thẩm định**<br>• Bước 1: Mở trang đăng nhập `/login`.<br>• Bước 2: Điền email, mật khẩu Giám khảo và bấm Đăng nhập.<br>• Bước 3: Điều hướng tới `/evaluation`. | • URL: `http://localhost:5173/login`<br>• Email: `evaluator@procurement.com`<br>• Mật khẩu: `Admin@123`<br>• Vai trò: `Evaluator` | Màn hình hiển thị tiêu đề *Trung Tâm Thẩm Định & Chấm Điểm Thầu*. Danh sách hiển thị thẻ gói thầu `PKG-2026-0009` với tên *Gói thầu Mua sắm Thiết bị Mạng & An toàn Thông tin 2026*, ngân sách 2.000.000.000 đồng, trạng thái Đang đánh giá và nút Vào chấm điểm. | Màn hình chuyển hướng chính xác đến `/evaluation`. Tiêu đề lớn hiển thị rõ ràng cùng thẻ gói thầu `PKG-2026-0009`, nhãn trạng thái màu vàng Đang đánh giá và nút Vào chấm điểm sẵn sàng thao tác. | **Đạt** |
| **TC-EVAL-02** | **Mở gói thầu và xem Ma trận Chấm điểm ban đầu**<br>• Bước 1: Bấm nút Vào chấm điểm hoặc truy cập `/evaluation/2070`.<br>• Bước 2: Kiểm tra các khối thẻ tiến độ và ma trận scorecard. | • Đường dẫn: `/evaluation/2070`<br>• Gói thầu ID: 2070<br>• Hồ sơ dự thầu: 2 nhà thầu | Màn hình hiển thị tên gói thầu, nhãn Đang đánh giá, 4 thẻ thống kê ở phía trên (Tổng số hồ sơ: 2 nhà thầu, Tiến độ chấm thầu, Điểm cao nhất, Điểm trung bình). Bảng ma trận Scorecard hiển thị danh sách các tiêu chí kỹ thuật kèm trọng số và các ô nhập điểm. | Màn hình hiển thị đầy đủ thông tin chi tiết gói thầu 2070, các tab điều hướng Ma trận Chấm điểm (Scorecard) và Bảng xếp hạng & Trao thầu, bảng ma trận hiển thị 4 tiêu chí đánh giá chuẩn. | **Đạt** |
| **TC-EVAL-03** | **Giám khảo chấm điểm Hồ sơ số 1 (Công ty Đầu tư và Phát triển Công nghệ)**<br>• Bước 1: Chọn tab hồ sơ Công ty Đầu tư và Phát triển Công nghệ.<br>• Bước 2: Nhập điểm các tiêu chí kỹ thuật.<br>• Bước 3: Nhập nhận xét chuyên môn.<br>• Bước 4: Bấm nút Lưu phiếu chấm điểm. | • Hồ sơ ID: 1075<br>• Tiêu chí 1113: 95 điểm<br>• Tiêu chí 1115: 90 điểm<br>• Tiêu chí 1116: 85 điểm<br>• Nhận xét: Thiết bị chính hãng, thông số kỹ thuật đáp ứng vượt chuẩn so với HSMT. | Các ô nhập số hiển thị điểm 95, 90, 85. Ô nhận xét hiển thị nội dung đánh giá. Sau khi bấm Lưu phiếu chấm điểm, màn hình xuất hiện thông báo toast *Lưu phiếu chấm điểm thành công* và các giá trị điểm được cập nhật ổn định trên bảng. | Màn hình ghi nhận đầy đủ các giá trị điểm số và nhận xét kỹ thuật. Thông báo toast màu xanh hiển thị thành công, giao diện tự động đồng bộ điểm số tức thời. | **Đạt** |
| **TC-EVAL-04** | **Giám khảo chấm điểm Hồ sơ số 2 (Công ty Khang Nghị)**<br>• Bước 1: Chọn tab hồ sơ Công ty Khang Nghị.<br>• Bước 2: Nhập điểm các tiêu chí kỹ thuật.<br>• Bước 3: Nhập nhận xét chuyên môn.<br>• Bước 4: Bấm nút Lưu phiếu chấm điểm. | • Hồ sơ ID: 1076<br>• Tiêu chí 1113: 75 điểm<br>• Tiêu chí 1115: 70 điểm<br>• Tiêu chí 1116: 80 điểm<br>• Nhận xét: Đáp ứng cơ bản yêu cầu kỹ thuật, tiến độ giao hàng và bảo hành chấp nhận được. | Các ô nhập hiển thị điểm 75, 70, 80 và chuỗi nhận xét. Sau khi bấm Lưu phiếu chấm điểm, màn hình xuất hiện thông báo toast thành công. Thẻ tiến độ phía trên cập nhật số lượng hồ sơ đã chấm. | Màn hình điền thành công điểm số cho nhà thầu thứ hai, thông báo toast xác nhận lưu điểm hiển thị trên góc màn hình và bảng điểm ghi nhận số liệu chuẩn xác. | **Đạt** |
| **TC-EVAL-05** | **Kiểm tra phân tách quyền hạn Maker - Checker trên giao diện Giám khảo**<br>• Bước 1: Giám khảo bấm vào tab Bảng xếp hạng & Trao thầu.<br>• Bước 2: Kiểm tra sự xuất hiện của nút Phê duyệt Trao thầu.<br>• Bước 3: Kiểm tra hiển thị tiến độ chấm điểm. | • Tài khoản: `evaluator@procurement.com`<br>• Tab: Bảng xếp hạng & Trao thầu<br>• Ràng buộc: Maker không được tự duyệt | Màn hình hiển thị khối Tiến Độ Đánh Giá Của Tổ Chuyên Gia với thông báo Đã hoàn thành toàn diện (2/2 hồ sơ). Nút Phê duyệt Trao thầu hoàn toàn KHÔNG xuất hiện trên giao diện màn hình (số lượng phần tử bằng 0). | Màn hình hiển thị thanh tiến độ đánh giá màu xanh lá hoàn thành toàn bộ. Giao diện tuyệt đối không hiển thị nút Phê duyệt Trao thầu hay cột quyết định trao thầu, xác nhận cơ chế Maker bị chặn tự phê duyệt. | **Đạt** |
| **TC-EVAL-06** | **Bên mời thầu (Checker) đăng nhập và xem Trung tâm Phê duyệt Trao thầu**<br>• Bước 1: Đăng nhập tài khoản Bên mời thầu.<br>• Bước 2: Truy cập `/evaluation/2070`.<br>• Bước 3: Bấm tab Bảng xếp hạng & Trao thầu. | • Email: `procurement@procurement.com`<br>• Mật khẩu: `Admin@123`<br>• Vai trò: `Procurement` (Chủ đơn vị) | Màn hình hiển thị khối Trung Tâm Phê Duyệt Trao Thầu với nền màu vàng viền nổi bật. Ô chọn đơn vị hiển thị Hạng #1: Công ty Đầu tư và Phát triển Công nghệ (94.12 điểm). Nút Phê duyệt Trao thầu màu cam vàng khả dụng. Phía dưới hiển thị Bục Vinh Danh Xếp Hạng Nhà Thầu với vị trí quán quân đứng ở bục số 1 cao nhất. | Màn hình hiển thị chuẩn xác khối Trung Tâm Phê Duyệt Trao Thầu, nút Phê duyệt Trao thầu sáng rõ ở trạng thái kích hoạt, bục vinh danh hiển thị quán quân Công ty Đầu tư và Phát triển Công nghệ (94.12 điểm) và á quân Công ty Khang Nghị (73.50 điểm). | **Đạt** |
| **TC-EVAL-07** | **Bên mời thầu mở hộp thoại Phê duyệt Trao thầu**<br>• Bước 1: Bấm vào nút Phê duyệt Trao thầu tại khối trung tâm. | • Thao tác: Bấm nút Phê duyệt Trao thầu | Màn hình hiển thị hộp thoại modal Phê duyệt Trao thầu - Xác nhận kết quả trúng thầu chính thức. Trong modal hiển thị thẻ đơn vị được phê duyệt: Công ty Đầu tư và Phát triển Công nghệ, Thứ hạng #1, Mã số thuế 0101248141, Điểm đánh giá 94.12 / 100, và ô nhập văn bản Căn cứ / Lý do lựa chọn trao thầu. | Màn hình bật hộp thoại modal trung tâm với đầy đủ tiêu đề, huy hiệu Thứ hạng #1, thông tin pháp nhân trúng thầu và ô nhập căn cứ pháp lý. | **Đạt** |
| **TC-EVAL-08** | **Điền căn cứ pháp lý và Xác nhận phê duyệt trao thầu thành công**<br>• Bước 1: Nhập căn cứ quyết định pháp lý vào ô textarea.<br>• Bước 2: Bấm nút Xác nhận Trao thầu.<br>• Bước 3: Quan sát giao diện cập nhật trạng thái. | • Căn cứ: Căn cứ Biên bản đánh giá E-HSDT của Tổ chuyên gia, Công ty Đầu tư và Phát triển Công nghệ đạt điểm đánh giá cao nhất (Hạng 1) theo Quyết định số 88/QĐ-BQL.<br>• Nút: Xác nhận Trao thầu | Hộp thoại đóng lại, màn hình xuất hiện thông báo toast *Phê duyệt kết quả trúng thầu thành công*. Đầu trang hiển thị banner màu xanh ngọc: *Gói thầu đã hoàn tất thẩm định & trao thầu* kèm nhãn *Bảng điểm đã khóa*. Hiển thị banner lớn *Đã trao thầu chính thức* với giá trúng thầu 1.500.000.000 đồng và nút liên kết *Xem phân hệ Hợp đồng*. | Màn hình chuyển trạng thái hoàn tất tức thì. Banner màu xanh ngọc khẳng định gói thầu đã trao thầu và bảng điểm đã khóa. Banner vinh danh hiển thị đầy đủ thông tin giá trúng thầu và đường dẫn tới hợp đồng. | **Đạt** |
| **TC-EVAL-09** | **Mở Biên bản Đánh giá HSDT chính thức theo Nghị định 24/2024/NĐ-CP**<br>• Bước 1: Bấm nút Xuất Biên bản Đánh giá (Chính thức) ở góc trên bên phải trang web. | • Thao tác: Bấm Xuất Biên bản Đánh giá (Chính thức) | Màn hình hiển thị hộp thoại modal toàn màn hình *Biên bản & Báo cáo Đánh giá E-HSDT (Chính Thức)* có nhãn màu xanh Chính thức. Thanh công cụ hiển thị các nút Thu nhỏ, Phóng to, Tỷ lệ mặc định, Tải về (.doc), In / Xuất Báo cáo chính thức. Bản in hiển thị đầy đủ tiêu đề *BÁO CÁO ĐÁNH GIÁ HỒ SƠ DỰ THẦU*, căn cứ pháp luật và bảng điểm xếp hạng. | Màn hình mở giao diện văn bản hành chính khổ A4 chuẩn mực, tiêu đề in hoa đậm rõ nét, thanh công cụ in ấn và tải file hoạt động tốt, chữ ký các bên hiển thị đầy đủ. | **Đạt** |

#### 3.4. Bộ ảnh chụp màn hình minh chứng kết quả kiểm thử thực tế
*(Ghi chú: Toàn bộ 9 ảnh minh chứng dưới đây được chụp tự động và nhúng nguyên vẹn vào tệp văn bản Word chính thức [Phạm Minh Tài_Báo cáo ngày 03-11-2026.docx](file:///D:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/docx/word/Tuan_11%20(02-06_11_2026)/Ph%E1%BA%A1m%20Minh%20T%C3%A0i_B%C3%A1o%20c%C3%A1o%20ng%C3%A0y%2003-11-2026.docx))*:

1. **Hình 2.1: Giao diện Trung tâm Thẩm định & Chấm điểm thầu của Giám khảo**  
   *Mô tả*: Giám khảo (`evaluator@procurement.com`) đăng nhập vào hệ thống và truy cập phân hệ `/evaluation`. Giao diện hiển thị danh sách các gói thầu được phân công đánh giá chuyên môn, trong đó thẻ gói thầu `PKG-2026-0009` hiển thị trạng thái Đang đánh giá cùng nút Vào chấm điểm.
2. **Hình 2.2: Giao diện Ma trận Chấm điểm (Scorecard) chi tiết gói thầu 2070**  
   *Mô tả*: Màn hình chi tiết gói thầu `/evaluation/2070` hiển thị các thẻ tổng hợp chỉ số tiến độ ở đầu trang, hệ thống tab chuyển đổi và bảng ma trận chấm điểm phân bổ theo 4 tiêu chí chuyên môn cùng trọng số tương ứng.
3. **Hình 2.3: Giám khảo lưu phiếu chấm điểm chuyên môn cho Hồ sơ dự thầu số 1**  
   *Mô tả*: Giám khảo thực hiện nhập điểm số cho 3 tiêu chí chấm tay của Công ty Đầu tư và Phát triển Công nghệ (95, 90, 85 điểm) kèm nhận xét chuyên môn và bấm Lưu phiếu chấm điểm. Thông báo toast màu xanh xác nhận lưu thành công xuất hiện ở góc màn hình.
4. **Hình 2.4: Giám khảo lưu phiếu chấm điểm chuyên môn cho Hồ sơ dự thầu số 2**  
   *Mô tả*: Giám khảo chuyển sang tab hồ sơ của Công ty Khang Nghị, nhập các mức điểm kỹ thuật (75, 70, 80 điểm) kèm nhận xét đánh giá và lưu phiếu điểm thành công vào hệ thống CSDL bản sao.
5. **Hình 2.5: Kiểm soát phân tách trách nhiệm Maker - Checker trên giao diện Giám khảo**  
   *Mô tả*: Giám khảo chuyển sang tab Bảng xếp hạng & Trao thầu. Màn hình ghi nhận tiến độ chấm điểm của Tổ chuyên gia đã hoàn thành toàn diện, nhưng nút Phê duyệt Trao thầu hoàn toàn không hiển thị, bảo đảm Giám khảo (Maker) không thể tự ý phê duyệt kết quả trao thầu.
6. **Hình 2.6: Bên mời thầu (Checker) truy cập Trung tâm Phê duyệt Trao thầu**  
   *Mô tả*: Bên mời thầu (`procurement@procurement.com`) đăng nhập vào kiểm soát chéo. Khối Trung Tâm Phê Duyệt Trao Thầu hiển thị nổi bật với nút Phê duyệt Trao thầu sẵn sàng hoạt động, phía dưới là Bục Vinh Danh Xếp Hạng Nhà Thầu hiển thị vị trí Quán quân thuộc về Công ty Đầu tư và Phát triển Công nghệ (94.12 điểm).
7. **Hình 2.7: Hộp thoại xác nhận Phê duyệt Trao thầu chính thức**  
   *Mô tả*: Hộp thoại modal mở ra hiển thị thông tin tóm tắt của nhà thầu xếp Hạng 1 được đề xuất trúng thầu cùng ô nhập căn cứ pháp lý phê duyệt quyết định trao thầu theo thẩm quyền của Bên mời thầu.
8. **Hình 2.8: Gói thầu hoàn tất phê duyệt trao thầu và tự động khóa bảng điểm**  
   *Mô tả*: Sau khi bấm Xác nhận Trao thầu, trạng thái gói thầu chuyển thành Đã trao thầu. Banner màu xanh ngọc xác nhận bảng điểm đã được khóa lưu trữ chống sửa đổi, kèm banner vinh danh nhà thầu trúng thầu và nút điều hướng sang phân hệ Hợp đồng.
9. **Hình 2.9: Biên bản Đánh giá HSDT chính thức theo Nghị định số 24/2024/NĐ-CP**  
   *Mô tả*: Hộp thoại Báo cáo Đánh giá HSDT chính thức hiển thị định dạng văn bản hành chính chuẩn khổ A4, thể hiện đầy đủ cơ sở pháp lý, điểm số chi tiết của từng giám khảo, xếp hạng nhà thầu và các nút chức năng hỗ trợ in ấn trực tiếp hoặc xuất tệp văn bản.

---

## III. ĐÁNH GIÁ CHẤT LƯỢNG VÀ KẾ HOẠCH TIẾP THEO

### 1. Đánh giá chất lượng thực hiện
Công việc trong ngày đã hoàn thành đầy đủ toàn bộ các mục tiêu và nhiệm vụ đề ra theo đúng kế hoạch. Cụ thể:
- Mã nguồn hệ thống đạt độ tin cậy và ổn định cao, vượt qua toàn bộ 63 bài kiểm thử tự động Backend và 6 bài kiểm thử Frontend cục bộ, không có xung đột mã nguồn trên Git.
- Quy trình kiểm thử E2E phân hệ Đánh giá hồ sơ dự thầu & Phê duyệt trúng thầu được tự động hóa hoàn chỉnh qua Playwright, xác nhận cơ chế phân tách quyền hạn Maker - Checker vận hành chặt chẽ và không thể bị vượt quyền.
- Dữ liệu bảng điểm chấm thầu được niêm phong an toàn và tự động khóa vĩnh viễn ngay sau khi phê duyệt kết quả, đáp ứng trọn vẹn yêu cầu lưu vết kiểm toán và pháp lý đấu thầu hiện hành.

### 2. Kế hoạch công việc tiếp theo
Dựa trên kết quả kiểm thử đạt chất lượng cao của phân hệ Đánh giá và Trao thầu, kế hoạch công tác cho ngày tiếp theo bao gồm:
- Nghiên cứu cấu trúc và chuẩn bị kịch bản kiểm thử E2E cho phân hệ Quản lý Hợp đồng sau trao thầu (*Contract Lifecycle Management*), bao gồm các mốc ký kết hợp đồng, bảo lãnh thực hiện hợp đồng và quy trình nghiệm thu thanh toán nhiều giai đoạn.
- Xây dựng các bộ dữ liệu kiểm thử biên đối với các trường hợp vi phạm tiến độ hợp đồng hoặc phạt vi phạm hợp đồng để đánh giá tính toàn vẹn của luồng cảnh báo tự động.
- Tiếp tục hoàn thiện các tài liệu kiểm thử kỹ thuật và báo cáo tiến độ theo chuẩn quy định thể thức văn bản.
