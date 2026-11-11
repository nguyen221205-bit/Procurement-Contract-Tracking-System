# BÁO CÁO CÔNG VIỆC NGÀY [11/11/2026]

**Dự án**: Hệ thống Quản lý Đấu thầu và Theo dõi Hợp đồng (*Procurement & Contract Tracking System*)  
**Nhóm thực hiện**: 2 thành viên  
**Họ tên**: Phạm Minh Tài  

---

### TÓM TẮT TIẾN ĐỘ THỰC HIỆN
- **Hôm trước**:
  - Hợp nhất mã nguồn bản vá sáu lỗi sản phẩm lên nhánh chung của dự án; giải quyết xung đột mã nguồn liên quan đến chữ ký tải tệp đa hình và bộ sinh tài liệu hành chính mẫu; thiết kế và triển khai trọn vẹn phân hệ Sao lưu và Phục hồi (Backup & Restore) cho cả Cơ sở dữ liệu SQL Server và Kho tệp tải lên; hoàn thành kịch bản tự động hóa sao lưu nền bằng PowerShell và giao diện quản trị; đạt 94 trên 94 bài kiểm thử Backend và 6 trên 6 bài kiểm thử Frontend.
- **Hôm nay**:
  - **Nhiệm vụ 1**: Triệt tiêu hoàn toàn lỗ hổng Tham chiếu đối tượng trực tiếp không an toàn (IDOR) và Tải tệp tùy ý trong phân hệ Làm rõ hồ sơ mời thầu và Hồ sơ dự thầu: thiết lập chốt chặn bắt buộc tệp tải về phải liên kết chặt chẽ với bản ghi làm rõ trong cơ sở dữ liệu; thắt chặt ma trận kiểm soát truy cập đối với tài liệu giải trình dự thầu mật của nhà thầu.
  - **Nhiệm vụ 2**: Tái cấu trúc và chuẩn hóa thẩm quyền điều hành gói thầu cấp Bên mời thầu: xóa bỏ cơ chế gắn chặt quyền hạn với tài khoản cá nhân người tạo gói ban đầu; mở rộng toàn quyền cho Lãnh đạo và Chuyên viên đang hoạt động của đơn vị; tự động phân phối thông báo câu hỏi làm rõ đến toàn bộ cán bộ phụ trách.
  - **Nhiệm vụ 3**: Nâng cấp kịch bản tự động hóa sao lưu cơ sở dữ liệu định kỳ bằng PowerShell: tự động nhận diện chuỗi kết nối từ cấu hình ứng dụng hoặc biến môi trường khi chạy độc lập dưới quyền lịch tác vụ hệ điều hành.
  - **Nhiệm vụ 4**: Đồng bộ giao diện người dùng phân hệ Làm rõ Hồ sơ và xây dựng bộ kiểm thử tự động xUnit chuyên sâu gồm 11 kịch bản kiểm thử bảo mật và phân quyền đơn vị; nâng tổng số bài kiểm thử Backend đạt tuyệt đối lên 105 trên 105 bài kiểm thử; duy trì 6 trên 6 bài kiểm thử Frontend đạt kết quả tốt.
- **Mục tiêu**:
  - Bảo vệ an toàn thông tin và tính bảo mật của hồ sơ dự thầu doanh nghiệp; hoàn thiện cơ chế cộng tác và phân quyền cấp đơn vị theo quy chuẩn chuyển đổi số; đảm bảo tính sẵn sàng và tính độc lập của các công cụ sao lưu vận hành.

---

## I. BẢNG PHÂN CÔNG KHỐI LƯỢNG CÔNG VIỆC TRONG NGÀY

**Bảng 1.1: Khối lượng công việc và kết quả thực hiện ngày 11/11/2026**

| Nhiệm vụ trọng tâm | Nội dung triển khai và kết quả thực tế |
| :--- | :--- |
| **1. Triệt tiêu Lỗ hổng IDOR & Tải tệp tùy ý trong Làm rõ Hồ sơ** | • Phát hiện và khắc phục nguy cơ bảo mật nghiêm trọng tại cổng tải tệp đính kèm khi nhận đường dẫn tệp trực tiếp từ người dùng mà không xác thực quyền sở hữu.<br>• Xây dựng phương thức kiểm tra độc lập tại tầng xử lý nghiệp vụ: bắt buộc đường dẫn tệp phải khớp chính xác với bản ghi làm rõ hồ sơ trong cơ sở dữ liệu, từ chối ngay lập tức mọi đường dẫn lạ với mã lỗi 404.<br>• Thiết lập ma trận phân quyền nghiêm ngặt: cho phép tải công khai đối với tài liệu làm rõ hồ sơ mời thầu; kiểm soát chặt chẽ tài liệu giải trình dự thầu mật, chỉ cho phép Nhà thầu nộp hồ sơ, Tổ chuyên gia chấm thầu, Cán bộ thuộc đơn vị mời thầu và Quản trị viên hệ thống được phép tiếp cận (từ chối người dùng khác với mã lỗi 403).<br>• Bổ sung cổng tải tệp chuẩn hóa theo mã định danh làm rõ để tối ưu hóa khả năng tích hợp. |
| **2. Chuẩn hóa Thẩm quyền Điều hành Gói thầu cấp Bên mời thầu** | • Khắc phục bất cập nghiệp vụ khi hệ thống khóa cứng quyền xem và trả lời giải trình theo định danh của cá nhân người tạo gói thầu, gây đình trệ công việc khi nhân sự luân chuyển hoặc vắng mặt.<br>• Chuyển đổi sang cơ chế phân quyền theo cấp Đơn vị mời thầu dựa trên bảng quản lý thành viên nội bộ: xác thực cán bộ có tư cách hoạt động hợp lệ với vai trò Quản lý hoặc Chuyên viên để trao quyền xử lý tương đương.<br>• Cho phép Lãnh đạo và Chuyên viên cùng đơn vị xem danh sách giải trình dự thầu riêng tư, gửi văn bản yêu cầu giải trình và phản hồi câu hỏi làm rõ hồ sơ mời thầu cho nhà thầu.<br>• Mở rộng cơ chế gửi thông báo tự động đến toàn bộ cán bộ phụ trách đang hoạt động của đơn vị khi xuất hiện câu hỏi làm rõ mới. |
| **3. Nâng cấp Tự động hóa Kịch bản Sao lưu Cơ sở Dữ liệu Định kỳ** | • Cải tiến kịch bản tự động hóa sao lưu định kỳ chạy nền bằng PowerShell để giải quyết vấn đề phụ thuộc vào tham số mật khẩu cố định khi đưa vào môi trường vận hành thực tế.<br>• Bổ sung cơ chế tự động đọc và phân tích cú pháp chuỗi kết nối từ biến môi trường hệ thống hoặc các tệp cấu hình ứng dụng.<br>• Trích xuất chính xác địa chỉ máy chủ, tên cơ sở dữ liệu, tài khoản và mật khẩu quản trị để thực thi các lệnh sao lưu và nén dữ liệu một cách độc lập. |
| **4. Đồng bộ Giao diện & Xây dựng Bộ Kiểm thử Tự động xUnit Chuyên sâu** | • Cập nhật các hàm giao tiếp và giao diện người dùng phân hệ Làm rõ Hồ sơ trên ứng dụng Frontend, gắn mã định danh làm rõ vào liên kết xem tệp đính kèm.<br>• Xây dựng bộ kiểm thử tự động xUnit hoàn toàn mới gồm 11 kịch bản kiểm thử bao phủ toàn diện các tình huống: chặn tải tệp tùy ý, chặn tải trộm giải trình mật, cấp quyền cho nhà thầu sở hữu và cán bộ cùng đơn vị, kiểm tra phân phối thông báo đa cán bộ.<br>• Nâng tổng số bài kiểm thử Backend đạt tuyệt đối lên 105 trên 105 bài kiểm thử; toàn bộ 6 trên 6 bài kiểm thử Frontend đạt kết quả tốt; đóng gói phát hành thành công. |

---

## II. CHI TIẾT TIẾN ĐỘ THỰC HIỆN CÔNG VIỆC

### 1. Triệt tiêu Lỗ hổng IDOR và Tải tệp Tùy ý trong Phân hệ Làm rõ Hồ sơ
- **Bối cảnh kỹ thuật**: Trong quá trình đánh giá an toàn thông tin chuyên sâu của hệ thống, một lỗ hổng bảo mật dạng Tham chiếu đối tượng trực tiếp không an toàn (Insecure Direct Object Reference - IDOR) kết hợp Tải tệp tùy ý (Arbitrary File Download) đã được phát hiện tại cổng tải tài liệu đính kèm của phân hệ Làm rõ Hồ sơ.
- **Nguy cơ rủi ro và lỗ hổng**: Trước khi điều chỉnh, cổng tải tệp đính kèm làm rõ tiếp nhận trực tiếp chuỗi đường dẫn tệp tin từ phía người dùng qua tham số truy vấn trên thanh địa chỉ. Sau khi tiếp nhận, hệ thống chuyển thẳng chuỗi đường dẫn này tới dịch vụ lưu trữ tệp để phân giải và trả về dữ liệu mà không hề thực hiện truy vấn đối soát với bảng dữ liệu Làm rõ trong cơ sở dữ liệu. Điều này dẫn tới nguy cơ đặc biệt nghiêm trọng: một người dùng bất kỳ sau khi đăng nhập tài khoản có thể thay đổi tham số đường dẫn thành các tài liệu nội bộ khác như hợp đồng kinh tế đã ký, tệp quét bảo lãnh ngân hàng, hồ sơ tài chính dự thầu của các nhà thầu đối thủ hoặc thậm chí các tệp sao lưu dữ liệu máy chủ để tải về trái phép.
- **Giải pháp triển khai**: Hệ thống đã triển khai phương thức kiểm soát truy cập và đối soát dữ liệu toàn diện với ba lớp bảo vệ độc lập:
  - *Đối soát liên kết dữ liệu bắt buộc*: Bắt buộc đường dẫn tệp tải về phải khớp chính xác với đường dẫn tệp đính kèm trong câu hỏi hoặc văn bản giải trình của một bản ghi làm rõ cụ thể đang lưu trữ trong cơ sở dữ liệu. Mọi đường dẫn không thuộc danh mục làm rõ đều bị từ chối ngay lập tức với mã lỗi 404 Không tìm thấy, triệt tiêu hoàn toàn khả năng can thiệp vào các thư mục tài liệu khác của hệ thống.
  - *Ma trận phân quyền theo tính chất tài liệu*: Phân loại rõ ràng tính chất của hai nhóm tài liệu làm rõ để áp dụng chính sách truy cập tương ứng:
    + **Tài liệu làm rõ hồ sơ mời thầu (HSMT)**: Mang tính chất công khai, minh bạch nhằm giải đáp các yêu cầu kỹ thuật của gói thầu cho tất cả các đơn vị quan tâm; mọi người dùng đã xác thực danh tính hợp lệ trên hệ thống đều có quyền tải về nghiên cứu.
    + **Tài liệu giải trình hồ sơ dự thầu (HSDT)**: Chứa đựng bí mật kinh doanh, giải pháp kỹ thuật, biện pháp thi công và năng lực tài chính riêng biệt của từng nhà thầu. Do đó, hệ thống thắt chặt quyền tiếp cận nghiêm ngặt: chỉ Nhà thầu nộp hồ sơ, các chuyên gia thuộc Tổ chuyên gia được phân công chấm thầu gói thầu đó, các cán bộ quản lý đang hoạt động thuộc Bên mời thầu sở hữu gói thầu và Quản trị viên hệ thống mới có quyền tải về. Bất kỳ tài khoản nào khác cố tình truy cập đều bị từ chối với mã lỗi 403 Cấm truy cập.
  - *Chuẩn hóa cổng giao tiếp*: Hỗ trợ thêm cổng tải tài liệu trực tiếp theo mã định danh của bản ghi làm rõ bên cạnh cổng truyền thống, giúp việc tích hợp trên giao diện người dùng trở nên mạch lạc và an toàn hơn.

---

### 2. Tái cấu trúc và Chuẩn hóa Thẩm quyền Điều hành Gói thầu cấp Bên mời thầu
- **Bất cập nghiệp vụ**: Trong các phiên bản trước đây, logic kiểm tra quyền hạn đối với việc xem danh sách giải trình dự thầu và trả lời câu hỏi làm rõ hồ sơ mời thầu được lập trình gắn chặt với tài khoản cá nhân của người tạo gói thầu ban đầu. Cơ chế này bộc lộ những bất cập lớn trong môi trường doanh nghiệp thực tế:
  - *Tắc nghẽn quy trình khi biến động nhân sự*: Khi chuyên viên trực tiếp tạo gói thầu chuyển công tác, nghỉ phép hoặc nghỉ việc, các cán bộ lãnh đạo hoặc chuyên viên tiếp quản khác trong cùng cơ quan mời thầu không thể xem được các văn bản giải trình của nhà thầu và không có quyền phản hồi câu hỏi làm rõ, gây tắc nghẽn toàn bộ tiến trình đấu thầu.
  - *Thiếu hụt thông tin cảnh báo quản lý*: Việc gửi thông báo khi nhà thầu đặt câu hỏi làm rõ chỉ được chuyển đến duy nhất người tạo gói ban đầu, khiến công tác giám sát của Ban lãnh đạo đơn vị bị phân mảnh và thiếu tính đồng bộ.
- **Giải pháp tái cấu trúc**: Để giải quyết dứt điểm các vướng mắc trên, kiến trúc phân quyền của phân hệ Làm rõ đã được tái cấu trúc theo mô hình tổ chức doanh nghiệp:
  - *Phân quyền theo tư cách thành viên đơn vị*: Xóa bỏ điều kiện kiểm tra người tạo gói cá nhân. Xây dựng hàm kiểm tra nghiệp vụ tập trung đối soát qua bảng quản lý thành viên nội bộ của Bên mời thầu, xác định người dùng có đang giữ trạng thái hoạt động với vai trò Quản lý hoặc Chuyên viên của đơn vị sở hữu gói thầu hay không.
  - *Trao quyền điều hành kế thừa*: Tất cả các cán bộ thuộc đơn vị mời thầu (kể cả không phải người trực tiếp tạo gói thầu) đều được trao đầy đủ quyền hạn điều hành: xem các văn bản giải trình dự thầu của nhà thầu, gửi yêu cầu giải trình bổ sung hồ sơ và thay mặt đơn vị phản hồi câu hỏi làm rõ hồ sơ mời thầu.
  - *Phân phối thông báo đa cán bộ*: Nâng cấp cơ chế phát thông báo tự động: khi nhà thầu gửi câu hỏi làm rõ mới, thông báo hệ thống được phân phối tự động đến toàn bộ danh sách cán bộ quản lý và chuyên viên đang hoạt động của đơn vị phụ trách gói thầu, bảo đảm thông tin thông suốt và xử lý kịp thời.

---

### 3. Nâng cấp Tự động hóa Nhận diện Chuỗi kết nối trong Kịch bản Sao lưu CSDL
- **Vấn đề kỹ thuật**: Kịch bản tự động hóa sao lưu định kỳ bằng PowerShell phục vụ công tác vận hành hệ thống chạy ngầm qua bộ lập lịch tác vụ của hệ điều hành. Trước đây, kịch bản sử dụng các tham số mặc định với tài khoản và mật khẩu cố định, dẫn tới nguy cơ gián đoạn sao lưu khi triển khai trên các môi trường máy chủ sản xuất với mật khẩu bảo mật riêng.
- **Giải pháp nâng cấp**: Kịch bản đã được bổ sung khối xử lý thông minh để tự động nhận diện thông tin kết nối mà không cần can thiệp thủ công:
  - *Ưu tiên biến môi trường*: Ưu tiên kiểm tra và nạp chuỗi kết nối từ biến môi trường của hệ thống.
  - *Tự động đọc cấu hình ứng dụng*: Trường hợp không có biến môi trường, kịch bản tự động định vị và đọc các tệp cấu hình của ứng dụng máy chủ; phân tích cấu trúc dữ liệu để trích xuất chuỗi kết nối mặc định.
  - *Bóc tách thông số chuẩn xác*: Áp dụng biểu thức chính quy để bóc tách chính xác các thành phần: địa chỉ máy chủ, tên cơ sở dữ liệu, tài khoản và mật khẩu quản trị để thực thi tiến trình sao lưu, nén dữ liệu và ghi nhật ký kiểm toán độc lập.

---

### 4. Đồng bộ Giao diện Người dùng và Xây dựng Bộ Kiểm thử Tự động xUnit Chuyên sâu
- **Đồng bộ giao diện người dùng**: Cập nhật các mô-đun gọi API và thành phần giao diện xem yêu cầu làm rõ trên ứng dụng Frontend, gắn kèm cả mã định danh bản ghi làm rõ và đường dẫn tệp tin vào liên kết tải tài liệu, giúp các thao tác xem trước và tải tệp của người dùng diễn ra thông suốt và an toàn.
- **Xây dựng bộ kiểm thử xUnit độc lập**: Xây dựng mới một bộ kiểm thử tự động chuyên sâu gồm 11 bài kiểm thử xUnit độc lập, thiết lập trên cơ sở dữ liệu SQLite trong bộ nhớ với các dữ liệu giả lập chuẩn xác, kiểm thử toàn diện các tình huống:
  - Xác minh hệ thống từ chối ngay lập tức và trả về mã lỗi 404 khi người dùng truyền đường dẫn tệp tin tùy ý không thuộc dữ liệu làm rõ.
  - Xác minh hệ thống ném ngoại lệ cấm truy cập 403 khi người dùng không có thẩm quyền cố tình tải tài liệu giải trình dự thầu mật của nhà thầu khác.
  - Xác minh Nhà thầu nộp hồ sơ, Tổ chuyên gia chấm thầu và Cán bộ cùng đơn vị mời thầu tải tệp giải trình thành công.
  - Xác minh người dùng đăng nhập hợp lệ tải thành công tài liệu làm rõ hồ sơ mời thầu công khai.
  - Xác minh cán bộ cùng đơn vị (không phải người tạo gói ban đầu) xem được danh sách giải trình dự thầu riêng tư và trả lời thành công câu hỏi làm rõ.
  - Xác minh cán bộ thuộc đơn vị mời thầu khác bị chặn xem giải trình mật và bị từ chối khi cố tình trả lời câu hỏi làm rõ.
  - Xác minh thông báo câu hỏi làm rõ mới được phân phối đầy đủ đến tất cả các cán bộ quản lý đang hoạt động của đơn vị.
  - Xác minh cổng giao tiếp API cấp Bộ điều khiển xử lý và chuyển đổi chính xác các mã trạng thái HTTP chuẩn RESTful.

---

## III. ĐÁNH GIÁ KẾT QUẢ VÀ KẾ HOẠCH TIẾP THEO

### 1. Đánh giá kết quả đạt được
- **Về mặt an toàn thông tin**: Lỗ hổng IDOR tải tệp tùy ý đã được khắc phục triệt để bằng cơ chế đối soát dữ liệu và phân quyền đa lớp, bảo vệ tuyệt đối tính bí mật của hồ sơ dự thầu doanh nghiệp.
- **Về mặt quy trình nghiệp vụ**: Chuyển đổi thành công cơ chế quản lý gói thầu từ mô hình cá nhân hóa sang mô hình tổ chức doanh nghiệp, nâng cao tính linh hoạt và khả năng cộng tác trong nội bộ Bên mời thầu.
- **Về mặt chất lượng phần mềm**: Hệ thống kiểm thử tự động đạt độ bao phủ toàn diện với 105 trên 105 bài kiểm thử Backend vượt qua thành công, 6 trên 6 bài kiểm thử Frontend đạt kết quả tốt, bảo đảm hệ thống vận hành ổn định và không phát sinh lỗi tiềm ẩn.

### 2. Kế hoạch tiếp theo
- Phối hợp triển khai phân hệ Báo cáo và Thống kê Quản trị: bổ sung cơ chế cách ly số liệu theo cấp Bên mời thầu để cán bộ đơn vị chỉ xem được các báo cáo và hợp đồng thuộc đơn vị mình, ngăn ngừa rò rỉ dữ liệu tài chính đa đơn vị.
- Xây dựng màn hình giao diện tập trung cho Quản trị viên hệ thống để thẩm định và phê duyệt các hồ sơ đề nghị cấp thẩm quyền ký phê duyệt giải ngân từ các Bên mời thầu.
- Đồng bộ giao diện Bảng điều khiển (Dashboard) trên ứng dụng Frontend cho người dùng Bên mời thầu sau khi hoàn tất phân quyền dữ liệu.
