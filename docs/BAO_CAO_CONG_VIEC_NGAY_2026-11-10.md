# BÁO CÁO CÔNG VIỆC NGÀY [10/11/2026]

**Dự án**: Hệ thống Quản lý Đấu thầu và Theo dõi Hợp đồng (*Procurement & Contract Tracking System*)  
**Nhóm thực hiện**: 2 thành viên  
**Họ tên**: Phạm Minh Tài  

---

### TÓM TẮT TIẾN ĐỘ THỰC HIỆN
- **Hôm trước**:
  - Tinh gọn và tái cấu trúc giao diện phân hệ Bên mời thầu; phân định rạch ròi phạm vi hiển thị giữa Quản trị viên và người dùng Bên mời thầu; tích hợp trực tiếp bảng quản lý thành viên nội bộ vào trang chi tiết đơn vị; chuẩn hóa thuật ngữ giao diện phù hợp nghiệp vụ.
  - Khắc phục triệt để toàn bộ sáu lỗi sản phẩm còn tồn đọng phát hiện từ đợt nghiệm thu toàn diện: bắt buộc xác thực mã OTP email khi đăng ký tài khoản; nạp dữ liệu gói thầu liên kết và kiểm soát truy cập theo đơn vị đối với danh sách bảo lãnh và phụ lục hợp đồng; khoanh vùng phạm vi thông báo nộp thầu tránh rò rỉ dữ liệu giữa các đơn vị; hỗ trợ cán bộ được mời tra cứu thông tin đơn vị và chặn truy cập trái phép chéo đơn vị; cấp quyền quản lý cho Lãnh đạo đơn vị trong việc điều phối Tổ chuyên gia và cập nhật bộ tiêu chí đánh giá; thiết lập cờ bắt buộc đổi mật khẩu tạm trong lần đăng nhập đầu tiên khi được cấp lại mật khẩu hoặc phê duyệt đề xuất chuyên gia.
  - Thực thi toàn diện các bộ kiểm thử tự động trên toàn hệ thống đạt kết quả tuyệt đối 88 trên 88 bài kiểm thử Backend và 93 trên 93 kịch bản kiểm thử E2E độc lập trên cơ sở dữ liệu cô lập.
- **Hôm nay**:
  - **Nhiệm vụ 1**: Hoàn tất hợp nhất mã nguồn bản vá sáu lỗi sản phẩm lên nhánh chung của dự án; giải quyết xung đột mã nguồn phát sinh với nhánh từ xa liên quan đến kiểu kết quả tải tệp đa hình và bộ sinh tài liệu hành chính mẫu; tinh chỉnh cơ chế tự phục hồi lưu trữ bảo vệ tính toàn vẹn của dữ liệu kiểm thử và mã băm SHA-256; đẩy thành công mã nguồn lên kho lưu trữ từ xa.
  - **Nhiệm vụ 2**: Thiết kế kiến trúc và triển khai toàn diện phân hệ Sao lưu và Phục hồi (Backup & Restore) cho cả Cơ sở dữ liệu SQL Server và Kho tệp tải lên: xây dựng cấu trúc dữ liệu truyền tải; thực thi sao lưu cơ sở dữ liệu toàn vẹn có nén dữ liệu và kiểm tra khối dữ liệu; kiểm định cấu trúc bản sao lưu ngay sau khi ghi đĩa; nén toàn bộ thư mục tệp đính kèm thành tệp lưu trữ chuẩn; tự động băm SHA-256 đối soát chống giả mạo; thiết lập quy trình phục hồi cơ sở dữ liệu độc lập có bảo vệ đa tầng với cụm từ xác nhận an toàn; ngăn chặn triệt để tấn công điều hướng thư mục; ghi vết toàn bộ hành vi vào nhật ký kiểm toán.
  - **Nhiệm vụ 3**: Phát triển kịch bản tự động hóa định kỳ chạy nền bằng PowerShell hỗ trợ dọn dẹp các bản sao lưu quá hạn; xây dựng giao diện người dùng Quản trị phân hệ Sao lưu & Phục hồi trên Frontend React với bảng số liệu tổng quan, các bảng quản lý trực quan và hộp thoại cảnh báo phục hồi; mở rộng bộ kiểm thử tự động đạt 94 trên 94 bài kiểm thử Backend và 6 trên 6 bài kiểm thử Frontend vượt qua thành công.
- **Mục tiêu**:
  - Bảo đảm an toàn dữ liệu, tính sẵn sàng cao và khả năng phục hồi thảm họa toàn vẹn cho hệ thống đấu thầu trực tuyến.
  - Cung cấp công cụ quản trị trực quan, chuẩn hóa quy trình sao lưu định kỳ theo tiêu chuẩn an toàn thông tin doanh nghiệp.

---

## I. BẢNG PHÂN CÔNG KHỐI LƯỢNG CÔNG VIỆC TRONG NGÀY

**Bảng 1.1: Khối lượng công việc và kết quả thực hiện ngày 10/11/2026**

| Nhiệm vụ trọng tâm | Nội dung triển khai và kết quả thực tế |
| :--- | :--- |
| **1. Hợp nhất Mã nguồn & Đồng bộ Nhánh dev** | • Rà soát lịch sử commit của nhóm, phát hiện các cập nhật mới từ đồng đội bổ sung cơ chế lưu trữ xem trước và bộ sinh tài liệu hành chính.<br>• Giải quyết xung đột giao diện dịch vụ Bên mời thầu, tích hợp hài hòa chữ ký phương thức tải tệp đa hình cùng phương thức kiểm tra quyền truy cập đơn vị.<br>• Tinh chỉnh cơ chế tự phục hồi tệp tải lên để không ghi đè nhầm các tệp kiểm thử có dung lượng nhỏ, bảo vệ nguyên vẹn mã băm dữ liệu.<br>• Biên dịch và kiểm thử thành công, hợp nhất vào nhánh phát triển chính và đẩy lên máy chủ từ xa an toàn. |
| **2. Thiết kế & Xây dựng Phân hệ Sao lưu & Phục hồi** | • Thiết kế các mô hình truyền tải dữ liệu bao gồm thông tin chi tiết bản sao lưu, tổng quan dung lượng, yêu cầu khởi tạo và yêu cầu phục hồi có bảo vệ.<br>• Xây dựng dịch vụ sao lưu hoàn chỉnh tại tầng hạ tầng:<br>  - Thực thi lệnh sao lưu toàn vẹn cơ sở dữ liệu có nén dung lượng và kiểm tra mã băm từng khối dữ liệu.<br>  - Kiểm định tính toàn vẹn của tệp sao lưu ngay sau khi tạo bằng lệnh kiểm tra cấu trúc.<br>  - Nén toàn bộ thư mục tệp đính kèm thành tệp lưu trữ chuẩn.<br>  - Tự động tính toán mã băm an toàn SHA-256 cho mọi bản sao lưu phục vụ đối soát và chống giả mạo.<br>  - Thiết lập quy trình phục hồi CSDL an toàn đa tầng: kiểm tra tệp tồn tại, kiểm tra tính toàn vẹn, chuyển phiên làm việc sang CSDL master, ngắt kết nối hiện hành, chuyển sang chế độ đơn người dùng, phục hồi đè và mở lại chế độ đa người dùng.<br>  - Bắt buộc kiểm tra cụm từ xác nhận bảo vệ trước khi cho phép khôi phục cơ sở dữ liệu.<br>  - Ngăn chặn triệt để lỗ hổng tấn công điều hướng thư mục khi tải tệp sao lưu.<br>• Cung cấp đầy đủ 6 API RESTful tại bộ điều khiển sao lưu với phân quyền nghiêm ngặt chỉ dành riêng cho Quản trị viên.<br>• Ghi vết đầy đủ mọi hành động sao lưu, phục hồi và xóa dữ liệu vào bảng nhật ký kiểm toán hệ thống. |
| **3. Tự động hóa Định kỳ, Kiểm thử Toàn diện & Nghiệm thu Hệ thống** | • Phát triển kịch bản tự động hóa bằng PowerShell có cấu trúc tham số linh hoạt; hỗ trợ sao lưu cơ sở dữ liệu, nén thư mục tài liệu và tự động ghi nhật ký vào tệp theo dõi; tích hợp tính năng tự động dọn dẹp các bản sao lưu cũ vượt quá số ngày lưu trữ quy định.<br>• Xây dựng màn hình Quản lý Sao lưu và Phục hồi trên giao diện Quản trị viên với bốn thẻ chỉ số tổng quan, các bảng quản lý trực quan, hộp thoại xác nhận phục hồi cảnh báo cao và bảng hướng dẫn vận hành tự động.<br>• Bổ sung sáu bài kiểm thử tự động chuyên sâu kiểm tra toàn diện các quy tắc nghiệp vụ sao lưu và bảo mật, nâng tổng số bài kiểm thử Backend vượt qua lên 94 trên 94 bài kiểm thử.<br>• Thực thi kiểm thử đơn vị Frontend đạt 6 trên 6 bài test vượt qua; đóng gói bản phát hành thành công.<br>• Thực thi kịch bản sao lưu thực tế, tạo thành công bản sao lưu cơ sở dữ liệu hoàn chỉnh dung lượng 4.19 MB. |

---

## II. CHI TIẾT TIẾN ĐỘ THỰC HIỆN CÔNG VIỆC

### 1. Hợp nhất Mã nguồn và Khắc phục Xung đột Tích hợp Kho lưu trữ
Trong quá trình chuẩn bị hợp nhất bản vá sáu lỗi sản phẩm lên nhánh chung của dự án, nhánh từ xa xuất hiện cập nhật mới từ đồng đội liên quan đến việc nâng cấp chữ ký phương thức tải tệp sang cấu trúc đa hình và cơ chế tự tái tạo tệp hành chính mẫu. Quá trình tích hợp đã giải quyết triệt để các vấn đề kỹ thuật:
- **Tích hợp giao diện hợp nhất**: Bảo toàn phương thức kiểm tra quyền truy cập đơn vị đã được chuẩn hóa trong bản vá sáu lỗi, đồng thời tiếp nhận kiểu kết quả tải tệp đa hình của đồng đội tại giao diện dịch vụ Bên mời thầu.
- **Khắc phục lỗi ghi đè tệp kiểm thử**: Cơ chế tự phục hồi tệp ban đầu tự động ghi đè mọi tệp có dung lượng dưới 500 byte thành tệp mẫu. Điều này làm thay đổi nội dung các tệp giả lập của bài kiểm thử tích hợp, dẫn đến sai lệch mã băm SHA-256. Giải pháp tinh chỉnh đã bổ sung kiểm tra nội dung chuỗi đánh dấu đặc thù, chỉ tái tạo khi tệp thực sự bị gián đoạn kết nối đám mây, bảo vệ nguyên vẹn các tệp dữ liệu kiểm thử và tệp thực tế.

---

### 2. Chi tiết Giải pháp Kỹ thuật Xây dựng Phân hệ Sao lưu & Phục hồi

#### 2.1. Sao lưu Cơ sở dữ liệu Toàn vẹn có Nén và Kiểm định Khối
- **Vấn đề**: Trước đây hệ thống chưa có tính năng cho phép Quản trị viên chủ động khởi tạo bản sao lưu cơ sở dữ liệu ngay trên ứng dụng, gây khó khăn cho công tác vận hành và bảo trì đột xuất.
- **Giải pháp**: Xây dựng chức năng sao lưu tận dụng trực tiếp sức mạnh của hệ quản trị cơ sở dữ liệu SQL Server với các tham số định dạng mới, khởi tạo bộ phương tiện, tính toán mã băm khối và nén dữ liệu trực tiếp trong quá trình ghi đĩa. Ngay sau khi hoàn tất ghi tệp, hệ thống tự động thực thi lệnh kiểm tra cấu trúc để bảo đảm tệp sao lưu không bị lỗi vật lý hay thiếu hụt trang dữ liệu.

#### 2.2. Sao lưu Nén Kho Tệp Đính kèm và Đối soát Mã băm An toàn
- **Vấn đề**: Tài liệu dự thầu, hồ sơ năng lực, bảo lãnh ngân hàng và hợp đồng được lưu trữ dạng tệp tin rời rạc trong thư mục tải lên, tiềm ẩn nguy cơ thất thoát nếu không có cơ chế đóng gói định kỳ.
- **Giải pháp**: Tích hợp chức năng nén toàn bộ thư mục tệp tải lên thành một tệp lưu trữ chuẩn duy nhất với mức nén tối ưu. Mỗi bản sao lưu sinh ra đều được tính toán chuỗi băm an toàn SHA-256 ghi nhận vào cấu trúc dữ liệu và nhật ký kiểm toán, phục vụ việc đối soát và kiểm tra tính toàn vẹn.

#### 2.3. Quy trình Phục hồi Cơ sở dữ liệu với Cơ chế Bảo vệ Đa tầng
- **Vấn đề**: Thao tác phục hồi cơ sở dữ liệu có tính chất hủy bỏ và thay thế toàn bộ dữ liệu hiện tại, nếu người dùng vô tình bấm nhầm hoặc kết nối đang bận sẽ dẫn đến xung đột khóa tiến trình hoặc mất mát dữ liệu nghiêm trọng.
- **Giải pháp**: Thiết lập quy trình phục hồi nghiêm ngặt với bốn tầng bảo vệ:
  - Bắt buộc Quản trị viên nhập chính xác chuỗi ký tự quy định `RESTORE_DATABASE_CONFIRM`, từ chối ngay lập tức mọi trường hợp nhập sai với mã lỗi 400.
  - Tự động thực thi kiểm tra tính toàn vẹn của bản sao lưu trước khi tiến hành ngắt kết nối.
  - Tự động chuyển đổi chuỗi kết nối sang cơ sở dữ liệu quản trị master của máy chủ để không bị xung đột khóa tiến trình với cơ sở dữ liệu đích.
  - Chuyển trạng thái cơ sở dữ liệu đích sang chế độ đơn người dùng, ngắt các kết nối đang hoạt động, tiến hành phục hồi đè dữ liệu và mở lại chế độ đa người dùng ngay sau khi hoàn tất. Nếu phát sinh sự cố, hệ thống luôn chủ động mở lại trạng thái đa kết nối.

#### 2.4. Kịch bản Tự động hóa Định kỳ và Quản trị Vòng đời Bản sao lưu
- **Vấn đề**: Việc chỉ dựa vào thao tác thủ công có thể dẫn đến việc quên sao lưu định kỳ hoặc gây tràn dung lượng đĩa cứng khi số lượng bản lưu tích tụ theo thời gian.
- **Giải pháp**: Phát triển kịch bản tự động hóa bằng PowerShell hỗ trợ tạo bản sao lưu đồng bộ cả CSDL và tệp tải lên, tự động quét và xóa các bản sao lưu cũ vượt quá số ngày quy định (mặc định 14 ngày), đồng thời ghi vết lịch sử thực thi đầy đủ vào tệp nhật ký riêng biệt.

#### 2.5. Giao diện Quản trị Trực quan và Phân quyền Vận hành Nghiêm ngặt
- **Vấn đề**: Quản trị viên thiếu công cụ trực quan để theo dõi tình trạng sao lưu, dung lượng chiếm dụng và thao tác phục hồi dữ liệu khi cần.
- **Giải pháp**: Xây dựng trang Quản lý Sao lưu và Phục hồi trên giao diện Quản trị viên với bốn thẻ chỉ số KPI tổng quan, ba phân khu quản lý chuyên biệt (CSDL, tệp đính kèm và tài liệu tự động hóa), hộp thoại phục hồi cảnh báo cao và cơ chế kiểm soát phân quyền chặt chẽ chỉ dành cho Quản trị viên.

---

### 3. Kết quả Kiểm thử Nghiệm thu Toàn diện Hệ thống

Sau khi áp dụng đầy đủ các giải pháp kỹ thuật, quy trình kiểm thử tự động đa tầng được thực thi trên môi trường độc lập:

**Bảng 2.1: Tổng hợp kết quả kiểm thử toàn diện ngày 10/11/2026**

| Phân hệ kiểm thử | Công cụ & Lệnh thực thi | Chỉ tiêu đánh giá | Kết quả thực tế | Đánh giá |
| :--- | :--- | :--- | :--- | :---: |
| **Backend Backup & Restore Suite** | `dotnet test --filter BackupAndRestoreTests` | Kiểm tra phân quyền Admin, chống Path Traversal, cụm từ xác nhận, nén tệp, băm SHA-256, xóa và AuditLog | 6 trên 6 bài test chuyên biệt vượt qua thành công | **Đạt** |
| **Toàn bộ Backend Unit & Integration Tests** | `dotnet test ProcurementSystem.slnx --nologo` | Kiểm tra hồi quy toàn bộ hệ thống sau khi bổ sung phân hệ mới | 94 trên 94 bài test vượt qua tuyệt đối (thời gian chạy 23 giây) | **Đạt** |
| **Biên dịch Mã nguồn Backend** | `dotnet build ProcurementSystem.slnx --nologo` | Biên dịch toàn bộ các dự án trong giải pháp | 0 cảnh báo, 0 lỗi biên dịch | **Đạt** |
| **Frontend Unit Tests** | `npm test --prefix frontend -- --watchAll=false` | Kiểm tra đồng bộ mã danh mục và xử lý dữ liệu giao diện | 6 trên 6 bài test vượt qua | **Đạt** |
| **Đóng gói Bản phát hành Frontend** | `npm run build --prefix frontend` | Biên dịch gói phân phối sản phẩm chính thức | 2077 mô-đun được đóng gói hoàn tất | **Đạt** |
| **Thực thi Kịch bản Tự động hóa Thực tế** | `database-backup-cron.ps1 -RetentionDays 14` | Chạy thử nghiệm quy trình sao lưu CSDL và dọn dẹp thực tế | Tạo thành công tệp sao lưu CSDL dung lượng 4.19 MB có mã băm toàn vẹn | **Đạt** |

Toàn bộ sáu ca kiểm thử chuyên sâu về phân hệ Sao lưu & Phục hồi đều đã được xác nhận đạt kết quả mong muốn:
- `BK-OV-01`: Truy vấn tổng quan và danh sách bản sao lưu cơ sở dữ liệu và tệp tải lên phản hồi thành công mã 200.
- `BK-ZIP-01`: Tạo bản sao lưu nén toàn bộ thư mục uploads thành tệp ZIP, tự động tính toán mã băm SHA-256 và ghi vết nhật ký kiểm toán.
- `BK-PATH-01`: Chặn triệt để lỗ hổng điều hướng thư mục khi người dùng cố tình tải tệp qua đường dẫn tương đối độc hại.
- `BK-REST-01`: Cơ chế phục hồi CSDL từ chối yêu cầu thiếu hoặc sai cụm từ xác nhận (mã 400), từ chối tệp không phải đuôi .bak (mã 400) và từ chối tệp không tồn tại (mã 404).
- `BK-AUTH-01`: Bộ điều khiển sao lưu thực thi phân quyền nghiêm ngặt, cho phép Quản trị viên truy cập hợp lệ (mã 200) và chặn mọi vai trò khác.
- `BK-DEL-01`: Xóa an toàn tệp sao lưu khỏi đĩa cứng và ghi nhận đầy đủ vào nhật ký kiểm toán hệ thống.

---

## III. KẾ HOẠCH CÔNG VIỆC TIẾP THEO
- Tiếp tục theo dõi tính ổn định của phân hệ sao lưu khi vận hành cùng các phân hệ đấu thầu và hợp đồng.
- Chuẩn bị tài liệu hướng dẫn vận hành quản trị phân hệ Sao lưu & Phục hồi cho đội ngũ kỹ thuật của đơn vị thực tập.
- Sẵn sàng phối hợp thực hiện các đợt kiểm thử bổ sung hoặc tiếp nhận phản hồi từ người hướng dẫn doanh nghiệp.
