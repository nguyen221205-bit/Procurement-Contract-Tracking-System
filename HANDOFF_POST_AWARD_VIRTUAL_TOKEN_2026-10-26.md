# TÀI LIỆU BÀN GIAO TIẾN TRÌNH THẢO LUẬN & PHÂN TÍCH NGHIỆP VỤ (WIP - CHƯA CHỐT TRIỂN KHAI)
## CHỦ ĐỀ: KIỂM SOÁT HẬU TRAO GIẢI, CHỐNG THÔNG ĐỒNG NGHIỆM THU & PHƯƠNG ÁN THAY THẾ USB TOKEN
**Dự án:** Procurement & Contract Tracking System  
**Thời gian cập nhật:** 26/10/2026 - 17:55 (GMT+7)  
**Trạng thái hiện tại:** 🟡 **ĐANG TRONG GIAI ĐOẠN PHÂN TÍCH & BÀN LUẬN (OPEN BRAINSTORMING) — CHƯA CHỐT PHƯƠNG ÁN TRIỂN KHAI — TUYỆT ĐỐI CHƯA LẬP TRÌNH HAY SỬA CODE**  
**Người bàn giao:** Antigravity AI Pair Programmer & Người dùng (Phạm Minh Tài)  
**Người nhận bàn giao:** AI Agent tiếp theo (Tiếp tục trao đổi, phản biện và cùng người dùng tìm giải pháp tối ưu)  

---

## 1. MỤC ĐÍCH CỦA TÀI LIỆU NÀY
Tài liệu này được lập ra nhằm giúp Agent tiếp theo nắm bắt **toàn bộ dòng tư duy, các câu hỏi hóc búa và các phản biện thực tế của người dùng** từ đầu đến thời điểm hiện tại.  
> [!IMPORTANT]
> **LƯU Ý ĐẶC BIỆT CHO AGENT TIẾP THEO:**  
> Người dùng **CHƯA ĐỒNG Ý CHỐT** bất kỳ phương án nào để đem vào lập trình. Nhiệm vụ của bạn khi tiếp nhận phiên làm việc là: **Đọc kỹ bối cảnh tranh luận dưới đây, hiểu rõ từng nút thắt và tiếp tục đàm luận, phân tích các khía cạnh cùng người dùng để đi đến giải pháp triệt để nhất.**

---

## 2. BỐI CẢNH VẤN ĐỀ TỪ MENTOR

1. **Lỗ hổng mentor nêu ra:**
   * Trong giai đoạn **Hậu trao giải (Post-award)**, đặc biệt là khâu **Nghiệm thu mốc thanh toán và giải ngân hợp đồng**, hệ thống hiện tại đang trao toàn quyền cho Bên mời thầu (`Procurement`).
   * Cụ thể trong [`ContractService.cs`](file:///d:/Thuc_Tap_backend/Procurement%20&%20Contract%20Tracking%20System/ProcurementSystem.Infrastructure/Services/ContractService.cs#L1189-L1192), người tạo gói thầu (`CreatedBy == userId`) có thể tự bấm nghiệm thu và giải ngân hàng trăm triệu/hàng tỷ đồng.
   * Nguy cơ: **Cán bộ mời thầu có thể cấu kết, bắt tay với Nhà thầu** để nghiệm thu khống, ăn chia giải ngân mà không ai kiểm soát được.

2. **Làm rõ ranh giới kiến trúc (User định hình):**
   * Tài khoản `Admin` hệ thống chỉ là **Quản trị viên nền tảng kỹ thuật (Platform Operator)**, không phải là Chủ doanh nghiệp của từng công ty mời thầu để đi duyệt chi cho các bên.
   * Doanh nghiệp thực tế có cơ cấu phân cấp: Chủ doanh nghiệp (Giám đốc) ra chính sách/nhiệm vụ rồi giao cho cấp dưới thực hiện, nhưng quyền tiền bạc/giải ngân pháp lý phải do người có thẩm quyền nắm giữ.
   * Giải pháp phải nằm ở **cơ chế kiểm soát nội bộ của Bên mời thầu**, không thể dựa vào Admin hệ thống.

---

## 3. TOÀN BỘ DIỄN BIẾN THẢO LUẬN & CÁC VÒNG PHẢN BIỆN CỦA NGƯỜI DÙNG

### Vòng 1: Ý tưởng dùng chung 1 tài khoản Procurement và chuyển quyền bằng Key
* **Người dùng đề xuất:**
  * Giữ nguyên 1 vai trò `Procurement`, khi đăng nhập thì các thông tin chung public cho mọi người trong doanh nghiệp đều xem được.
  * Khi cần nghiệm thu thì có một nút/cơ chế chuyển role (dùng 1 key hoặc password gì đó) cho phép người đảm nhận thực hiện chức năng.
* **Người dùng đặt câu hỏi kỹ thuật:**
  1. Cái key này lưu ở đâu?
  2. Tạo thế nào và cấp ra sao?
  3. Lưu Audit Log như thế nào?
  4. Thực tế người dùng **không có USB Token phần cứng** thì làm sao? Có thể thay thế bằng gì?
* **Phía AI đưa ra giải đáp ban đầu:**
  * Đề xuất lưu mã PIN băm BCrypt trong DB (`ProcuringEntities.ApprovalPinHash`).
  * Đề xuất thay USB Token bằng: Mã PIN Lãnh đạo (như ngân hàng) hoặc Email OTP.

---

### Vòng 2: Phản biện mang tính bước ngoặt của Người dùng về "Chia sẻ mã PIN"
* **Người dùng lập tức chỉ ra lỗ hổng tư duy:**
  > *"Giả sử chủ doanh nghiệp nhận được mã PIN của các cấp dưới thì chủ doanh nghiệp đưa mã này cho cấp dưới như nào? Gửi mail/message thì có nguy cơ lộ, truyền miệng thì nghe rất ngố. Cấp dưới có mã rồi thì tự ý duyệt thông đồng được. Có cách nào mô phỏng cái USB Token không?"*
* **Phân tích bản chất sau phản biện của người dùng:**
  * Người dùng nhận định hoàn toàn đúng: Nếu Chủ đưa mã PIN cho cấp dưới qua bất kỳ kênh nào (mail, chat, nói miệng), thì **cấp dưới nắm mã sẽ toàn quyền tự tung tự tác thông đồng với nhà thầu**. Việc phân quyền bị vô hiệu hóa hoàn toàn!
  * **Nguyên tắc cốt tử được rút ra:** **CHỦ DOANH NGHIỆP KHÔNG BAO GIỜ ĐƯỢC ĐƯA CHÌA KHÓA CHO CẤP DƯỚI!**
  * **Mô hình Maker - Checker bắt buộc:**
    * Cán bộ kỹ thuật hiện trường (Maker): Chỉ lập hồ sơ, kiểm tra hiện trường, bấm *"Xác nhận kỹ thuật ĐẠT & Trình duyệt"*. Cán bộ không có khóa và không thể giải ngân.
    * Chủ doanh nghiệp / Lãnh đạo (Checker): Người duy nhất cầm chìa khóa/token để ký duyệt giải ngân.
* **Ý tưởng mô phỏng USB Token được gợi mở:**
  * Mô phỏng USB Token bằng **USB Flash thông thường + File khóa số mã hóa (`.key` / `.pfx` RSA-2048)**. Lãnh đạo copy file vào USB cá nhân cất ví. Khi duyệt thì cắm USB, chọn file từ USB và nhập PIN để ký.

---

### Vòng 3: Phản biện sâu sắc về Nhận diện Người dùng (IAM Dilemma)
* **Người dùng tiếp tục bắt bài và phản biện:**
  > *"Tất cả các cấp quyền đều sài 1 tài khoản procurement thì làm sao biết ai có quyền cao nhất đang sử dụng mà hiện nút cấp mới chữ ký số lãnh đạo?"*
* **Bản chất nghịch lý mà người dùng chỉ ra:**
  * Nếu cả Giám đốc và nhân viên cấp dưới đều dùng chung 1 username/password `Procurement`, hệ thống chỉ nhận được một JWT Token duy nhất.
  * Hệ thống hoàn toàn **không biết ai đang ngồi trước màn hình**.
  * Nếu ai đăng nhập vào cũng thấy nút "Tạo USB Token" -> Nhân viên cấp dưới cũng bấm vào tự tạo Token riêng rồi tự ký duyệt luôn!
  * Nếu giấu nút đi -> Giám đốc vào cũng không thấy!

---

## 4. CÁC HƯỚNG XỬ LÝ ĐÃ ĐƯỢC ĐƯA RA SAU PHẢN BIỆN (ĐANG MỞ ĐỂ BÀN TIẾP)

Sau khi người dùng đặt vấn đề ở Vòng 3, phía AI đã đề xuất 2 hướng tiếp cận để tháo gỡ nghịch lý, nhưng **cả 2 hướng đều có ưu / nhược điểm và cần tiếp tục thảo luận với người dùng**:

### Hướng A: Giữ 1 tài khoản Procurement chung, nhưng "Bảo vệ nút Lãnh đạo bằng OTP Email Pháp nhân"
* **Cơ chế:**
  * Nút "Cấp mới Token / Quản trị Chữ ký số" luôn hiển thị nhưng ở trạng thái **Khóa (Locked)**.
  * Muốn mở nút này, hệ thống bắt buộc gửi **Mã xác thực OTP 6 số về Hộp thư Email chính thức của Người đại diện pháp luật (`RepresentativeName` / Email công ty đã duyệt hồ sơ)**.
  * Cán bộ cấp dưới biết pass tài khoản web nhưng **không thể truy cập hòm thư email cá nhân của Giám đốc**.
  * Chỉ Giám đốc mở email lấy mã OTP nhập vào -> Hệ thống mới mở khóa giao diện sinh file Token tải về USB của Giám đốc.
* **Các băn khoăn còn bỏ ngỏ (Open Questions / Nhược điểm):**
  * *Băn khoăn 1:* Nếu email đăng ký tài khoản là email chung của công ty (ví dụ `contact@congty.com` mà thư ký hoặc nhân viên cũng vào đọc được) thì cấp dưới vẫn lấy được OTP! Cần làm rõ email này phải là email định danh riêng của Lãnh đạo (`giamdoc.nguyen@congty.com`).
  * *Băn khoăn 2:* Mỗi lần ký duyệt mốc nghiệm thu, việc phải cắm USB và chọn file trên trình duyệt web có bị rườm rà, có gặp cản trở về UX/bảo mật trình duyệt không?

### Hướng B: Chuyển sang mô hình Doanh nghiệp Đa người dùng chuẩn (Multi-user Tenant: Master & Sub-accounts)
* **Cơ chế:**
  * Doanh nghiệp có 1 tài khoản Trưởng (Master - Giám đốc).
  * Giám đốc tạo các tài khoản con (Sub-accounts) cho nhân viên (Cán bộ kỹ thuật A, Kế toán B).
  * Cán bộ A đăng nhập tài khoản của mình -> Chỉ thấy nút "Trình duyệt kỹ thuật".
  * Giám đốc đăng nhập tài khoản của mình -> Thấy nút "Ký số phê duyệt & Cấp Token".
* **Các băn khoăn còn bỏ ngỏ (Open Questions / Nhược điểm):**
  * *Băn khoăn 1:* Cách này đi ngược lại mong muốn ban đầu của người dùng là *"tôi dự là 1 role procurement rồi chia ra các role thấp hơn từ role này... tất cả sài 1 tài khoản"*.
  * *Băn khoăn 2:* Phải đập đi sửa lại bảng `Users`, thêm khái niệm `ProcuringEntityId` vào bảng `User`, viết thêm trang quản lý nhân viên nội bộ, tăng độ phức tạp của đồ án.

### Hướng C: Cơ chế "Phiên làm việc tạm thời theo Thẩm quyền" (Step-Up Session / PIN Lãnh đạo độc lập)
* **Cơ chế:**
  * Vẫn 1 tài khoản `Procurement`. Khi cần thực hiện nghiệp vụ tối cao (Giải ngân), hệ thống yêu cầu một **Mã thẩm quyền tối cao (Master Key / PIN Lãnh đạo)** hoặc **Smart OTP qua điện thoại Lãnh đạo**.
  * Giám đốc là người duy nhất nắm mã này và chỉ ngồi vào máy nhập mã khi có biên bản nghiệm thu trước mặt.
* **Các băn khoăn còn bỏ ngỏ:**
  * Quay lại phản biện Vòng 2 của người dùng: Làm sao ngăn Giám đốc không đưa mã này cho cấp dưới? Làm sao mô phỏng được hành vi vật lý giống USB Token để thể hiện tính chuyên nghiệp trước hội đồng chấm thi?

---

## 5. NHỮNG ĐIỂM CẦN TIẾP TỤC BÀN LUẬN VỚI NGƯỜI DÙNG (TALKING POINTS FOR NEXT AGENT)

Khi Agent tiếp theo nhận phiên làm việc, **hãy bắt đầu bằng việc thảo luận với người dùng về các điểm nghẽn sau, TUYỆT ĐỐI KHÔNG TỰ Ý CODE**:

1. **Về mặt trải nghiệm người dùng (UX):**
   * Người dùng muốn việc "mô phỏng cắm USB Token" diễn ra cụ thể thế nào trên giao diện web?
   * Người dùng cắm 1 chiếc USB thật vào máy tính, bấm chọn file `.key` từ USB đó và nhập PIN? Hay muốn theo mô hình Cloud Token / Smart OTP trên web?
2. **Về việc phân biệt quyền hạn khi dùng chung tài khoản:**
   * Giữa **Hướng A (Bảo vệ bằng OTP Email Lãnh đạo)** và **Hướng B (Tạo tài khoản con riêng)**, người dùng cảm thấy hướng nào phù hợp với phạm vi đồ án và thực tế hơn?
   * Liệu có hướng D nào đơn giản và thông minh hơn nữa mà vẫn thỏa mãn được yêu cầu của Mentor?
3. **Mức độ phức tạp mong muốn:**
   * Dự án chỉ còn vài ngày thực tập/bảo vệ, người dùng muốn làm sâu đến mức độ nào (mô phỏng mật mã học RSA thật hay chỉ cần mô phỏng quy trình kiểm duyệt Maker-Checker có thẩm quyền)?

---

> [!NOTE]
> **TÓM LẠI CHO AGENT TIẾP THEO:**  
> Đọc kỹ file này để hiểu người dùng rất sắc sảo và luôn phản biện để tìm ra gốc rễ thực tế của vấn đề. Hãy chào người dùng, tóm tắt trạng thái ta đang ở bước thảo luận, và hỏi người dùng muốn đi sâu phân tích tiếp khía cạnh nào trong các phương án trên!
