# Bàn giao phương án đã chốt: thành viên doanh nghiệp và lời mời tham gia

Ngày bàn giao: **26/10/2026**, múi giờ **Asia/Saigon**.

## 1. Đọc mục này trước

**Người dùng đã chốt phương án nghiệp vụ:** mỗi người dùng tài khoản cá nhân độc lập, tham gia doanh nghiệp qua lời mời, được giao quyền theo nhiệm vụ và phạm vi công việc. Nhân viên tự thiết lập mật khẩu hoặc dùng tài khoản đã có; không đăng ký lại doanh nghiệp.

Câu xác nhận trực tiếp của người dùng:

> oke chốt phương án này, làm file handoff bàn giao lại phương án này cho Agent nhé

**Yêu cầu của lượt bàn giao này chỉ là tạo tài liệu.** Chưa triển khai tính năng, chưa thay đổi schema/DB, chưa chạy build/test/live E2E cho phương án này. Việc chốt thiết kế không phải là ủy quyền merge, push, deploy hay áp dụng production migration. Agent triển khai tiếp theo cần nhận nhiệm vụ thực thi tương ứng từ người dùng.

**Không tiếp tục dùng tài khoản chung hoặc tài khoản cha nắm mật khẩu tài khoản con làm thiết kế mục tiêu.** Không tự triển khai RSA, USB Token, passkey hoặc OTP ký duyệt từ những ý tưởng cũ: công nghệ xác thực khi ký chưa được chốt trong quyết định này.

## 2. Workspace, baseline và tài liệu liên quan

- Repo đã xác minh: `D:\Thuc_Tap_backend\Procurement & Contract Tracking System`.
- Nhánh hiện tại: `dev`.
- HEAD lúc soạn: `0446e500f1188b8fc27311450672fde6a3be68a1`.
- Working tree có nhiều thay đổi modified/untracked từ trước, gồm F01–F08. HEAD không đại diện cho toàn bộ source đang làm việc. Giữ nguyên những thay đổi này.
- Quy tắc: `.agents/rules/procurement-project.md`, `REVIEW_AND_AGENT_RULES.md`.
- Bàn giao FE/BE: `HANDOFF_AGENT_FE_BE_ENUM_2026-10-26.md`; các tài liệu được dẫn chiếu trong đó.
- Bối cảnh dự án: `HANDOFF_2026-10-26.md`.
- Lịch sử tranh luận: `HANDOFF_POST_AWARD_VIRTUAL_TOKEN_2026-10-26.md` trong repo và file ngoài repo: `C:\Users\HP\.gemini\antigravity\brain\46b1c7d8-1233-4c14-a1dc-d59e92e8bec9\handoff_post_award_virtual_token_2026-10-26.md`.

Hai handoff token bản WIP mô tả thời điểm chưa chốt. **Quyết định tài khoản/thành viên/lời mời tại tài liệu này là cập nhật mới hơn trong cuộc trao đổi với người dùng.** Giữ tài liệu cũ làm lịch sử, không coi phương án tài khoản chung trong đó là yêu cầu còn hiệu lực. Tài liệu này không thay thế các quy tắc FE/BE, enum và bảo mật đã có.

## 3. Bài toán phải giải quyết

Mentor chỉ ra nguy cơ cán bộ bên mời thầu cấu kết với nhà thầu để nghiệm thu khống: người tạo gói có thể tự phê duyệt mốc, thiếu kiểm soát chéo và truy vết cá nhân.

Người dùng đã cân nhắc tài khoản Procurement chung, PIN lãnh đạo, USB chứa file khóa và tài khoản cha/con. Nút thắt là không xác định được người thật khi dùng chung tài khoản, cấp dưới có thể lấy PIN hoặc tự cấp khóa, và chưa có cách xác lập lãnh đạo đầu tiên đáng tin.

Phương án được chọn chuyển sang **tài khoản cá nhân + tư cách thành viên doanh nghiệp + quyền/phân công + quy trình trình/duyệt có lịch sử**.

Phạm vi Admin hệ thống: thẩm định pháp nhân và xác minh việc gắn người có thẩm quyền với doanh nghiệp. Admin không trở thành lãnh đạo doanh nghiệp và không duyệt chi thay doanh nghiệp.

## 4. Các quyết định nghiệp vụ đã được chấp thuận

| Nội dung | Quyết định |
|---|---|
| Danh tính | Mỗi cá nhân dùng tài khoản riêng; không chia sẻ mật khẩu. |
| Doanh nghiệp | Một hồ sơ doanh nghiệp có các thành viên, gói thầu, hợp đồng và dữ liệu chung theo phạm vi quyền. |
| Quan hệ | Thành viên thuộc doanh nghiệp; không có quyền sở hữu mật khẩu hoặc đăng nhập thay người khác. |
| Role hệ thống | Có thể giữ `Procurement` cho thành viên nghiệp vụ; tách quyền nội bộ khỏi role này. Không suy ra quyền ký chỉ vì có role Procurement. |
| Gia nhập | Người có quyền gửi lời mời đến email; người nhận tự đăng ký hoặc đăng nhập và chấp nhận. |
| Hồ sơ pháp nhân | Thành viên được mời không đăng ký doanh nghiệp mới, không nhập lại MST hoặc nộp lại hồ sơ thành lập. |
| Người đầu tiên | Người nộp đăng ký chưa tự động là lãnh đạo/người ký; phải xác minh căn cứ đại diện/ủy quyền. |
| Quản lý thành viên | Được mời và phân công trong phạm vi cho phép; không mặc nhiên có quyền duyệt chi hoặc tự phong người ký. |
| Kiểm soát chéo | Người xác nhận kỹ thuật/trình nghiệm thu không được tự phê duyệt chính hồ sơ đó. |
| Quyền ký | Có căn cứ đại diện/ủy quyền, phạm vi và thời hạn khi áp dụng; việc giao/thay quyền ký có kiểm soát riêng. |
| Thu hồi | Khi nghỉ việc, thu hồi quyền/tư cách thành viên; giữ lịch sử hành động và tài khoản cá nhân. |
| Nhật ký | Lưu người thao tác, phiên bản hồ sơ, thời điểm, thay đổi, quyết định và lý do; bảo vệ lịch sử khỏi sửa/xóa thông thường. |

Phân biệt rõ: quyền xem dữ liệu chung không có nghĩa mọi thành viên được xem mọi tài liệu. Vẫn phải giữ sealed bid, phân công giám khảo, quyền tải file và các ranh giới nghiệp vụ hiện có.

## 5. Luồng đăng ký và gia nhập cần triển khai

### 5.1. Doanh nghiệp đăng ký lần đầu

1. Người đăng ký tạo tài khoản cá nhân và nộp hồ sơ doanh nghiệp.
2. Hồ sơ chờ thẩm định. Hệ thống không tự cấp quyền ký hoặc quản lý thẩm quyền chỉ vì người này gửi đăng ký.
3. Thẩm định căn cứ đại diện/ủy quyền, đối chiếu danh tính và thông tin liên hệ qua kênh độc lập phù hợp. Sở hữu hộp thư chỉ chứng minh khả năng truy cập email, chưa chứng minh chức danh/thẩm quyền.
4. Liên kết người được xác minh với tài khoản cá nhân có email đã xác thực. Nếu người đại diện khác người đăng ký, gửi lời mời kích hoạt riêng cho người đại diện.
5. Kích hoạt quyền quản lý thành viên cho người đã được xác minh. Quyền ký được ghi nhận riêng theo căn cứ thẩm quyền; không suy ra từ quyền quản lý thành viên.

Admin thực hiện bước thẩm định, không được lấy chức năng này làm lối duyệt nghiệm thu hoặc chi tiền. Chứng cứ và cách đối chiếu vận hành cụ thể cần được mô tả trước khi nghiệm thu chức năng xác minh.

### 5.2. Mời thành viên

Trang dự kiến: **Doanh nghiệp → Thành viên → Mời thành viên**.

Người mời nhập email người nhận, nhiệm vụ/quyền nghiệp vụ và phạm vi được phân công. Backend kiểm tra người mời đang là thành viên hoạt động, có quyền mời, và được phép giao đúng tập quyền/phạm vi này.

Hệ thống tạo lời mời và gửi email. Người mời không nhập, nhận hoặc gửi mật khẩu thay nhân viên. Không tạo doanh nghiệp mới cho người được mời.

Lời mời cần thể hiện doanh nghiệp mời, người mời, quyền/phạm vi dự kiến và hạn nhận. Không đưa quyền ký tối cao vào lựa chọn tự do của form mời thành viên thông thường.

### 5.3. Người nhận chưa có tài khoản

1. Mở liên kết mời; xem thông tin doanh nghiệp và quyền dự kiến.
2. Xác thực quyền kiểm soát đúng email được mời theo cơ chế đã thiết kế; không cho đổi sang email khác trong bước nhận lời.
3. Nhập họ tên và tự đặt mật khẩu theo chính sách tài khoản hiện có.
4. Chấp nhận tham gia. Backend tạo tài khoản và tư cách thành viên theo lời mời hợp lệ bằng thao tác nguyên tử phù hợp.
5. Sau khi hoàn tất, đăng nhập theo luồng xác thực được thiết kế. Không coi chỉ mở liên kết/GET là đã gia nhập.

### 5.4. Người nhận đã có tài khoản

1. Mở lời mời, đăng nhập bằng tài khoản có email đã xác thực khớp email được mời.
2. Xem doanh nghiệp, nhiệm vụ và phạm vi quyền; chủ động chấp nhận.
3. Backend gắn tư cách thành viên vào đúng tài khoản đó. Không tạo tài khoản trùng, không đặt lại mật khẩu hay đổi email của tài khoản đã tồn tại.

Người đang đăng nhập bằng email khác phải đổi tài khoản; không nhận lời mời bằng cách tự gửi `userId`, `procuringEntityId` hoặc quyền mong muốn trong request.

### 5.5. Quản lý vòng đời thành viên

- Xem lời mời đang chờ, gửi lại hoặc thu hồi trong phạm vi quyền. Gửi lại phải vô hiệu hóa liên kết cũ nếu phát hành token mới.
- Chỉ thành viên hoạt động có quyền/phân công hợp lệ được truy cập dữ liệu được bảo vệ.
- Khi thay quyền/thu hồi, command sau đó phải dùng quyền hiện hành, không tiếp tục tin quyền cũ trong JWT/cache.
- Thu hồi quyền tại doanh nghiệp không mặc nhiên khóa toàn bộ tài khoản cá nhân hoặc xóa lịch sử.
- Không để thao tác thông thường loại bỏ người quản trị hợp lệ cuối cùng mà không có quy trình bàn giao/khôi phục.

## 6. Kiểm soát nghiệp vụ hậu trao thầu

Luồng mục tiêu: **nhà thầu nộp hồ sơ → cán bộ được phân công xác nhận kỹ thuật và trình → người có thẩm quyền duyệt/từ chối → ghi nhận kết quả và nhật ký**.

Các bất biến cần giữ:

- Người xác nhận kỹ thuật và người phê duyệt phải có `UserId` khác nhau; việc một người có cả hai tập quyền không cho phép tự duyệt hồ sơ mình đã xác nhận.
- Quyền phải thuộc đúng doanh nghiệp/hợp đồng, còn hiệu lực và hợp lệ tại thời điểm thực thi.
- Lưu snapshot/phiên bản nội dung trình: mốc, số tiền, nhà thầu, tài liệu và nhận xét kỹ thuật. Thay đổi nội dung trọng yếu làm mất hiệu lực yêu cầu duyệt cũ và phải trình lại.
- Không cho caller bỏ qua bước trình bằng endpoint nghiệm thu cũ, quyền `CreatedBy` hoặc nhánh bypass Admin.
- Chặn duyệt lặp, hai yêu cầu đồng thời, hồ sơ cũ, hợp đồng sai trạng thái, tự đổi trạng thái qua endpoint khác. Kết quả nghiệp vụ và audit cần nhất quán trong transaction.
- Nếu triển khai xác thực tăng cường, mỗi lần xác thực gắn với đúng giao dịch/phiên bản và dùng một lần; không mở quyền lãnh đạo chung cho toàn phiên.

Đây là hướng nghiệp vụ đi cùng phương án đã chọn. Chi tiết state machine, DTO và công nghệ xác thực ký phải được đặc tả theo source trước khi lập trình. Không gọi việc cập nhật trạng thái hoặc KPI là bằng chứng chuyển tiền qua ngân hàng; tích hợp thanh toán thực tế chưa được xác minh trong lượt này.

## 7. Điểm nối với source đã đối chiếu

Các dòng dưới là mốc điều hướng tại working tree ngày 26/10, không bảo đảm giữ nguyên sau khi sửa:

| File/symbol | Quan sát và việc cần xem lại |
|---|---|
| `ProcurementSystem.Infrastructure/Entities/User.cs` | Tài khoản cá nhân, role và navigation hồ sơ. Bảo toàn identity, không chia sẻ thông tin đăng nhập. |
| `ProcurementSystem.Infrastructure/Entities/ProcuringEntity.cs` | Hiện có `UserId`, thông tin đại diện; chưa có mô hình thành viên trong entity đã đọc. Không lấy `RepresentativeName` làm bằng chứng danh tính người ký. |
| `ProcurementSystem.Infrastructure/Data/AppDbContext.cs:284` | Quan hệ `User`–`ProcuringEntity` đang one-to-one; dòng 298 có unique index `UserId`. Phải thiết kế bổ sung quan hệ thành viên thay vì giả định đã hỗ trợ. |
| `ProcurementSystem.Infrastructure/Services/ProcuringEntityAuthService.cs:RegisterProcuringEntityAsync` | Đang tạo User, gán Procurement, tạo hồ sơ doanh nghiệp với `UserId = user.Id`. OTP đăng ký chỉ được kiểm tra nếu caller cung cấp ở nhánh đã đọc; không giả định email đã xác thực. |
| `ProcurementSystem.Infrastructure/Services/TokenService.cs:31` | JWT chứa `NameIdentifier` theo user; role được thêm ở dòng 40. Quyền nội bộ/phân công cần cơ chế riêng và kiểm tra hiện hành. |
| `ProcurementSystem.Infrastructure/Services/ProcuringEntityService.cs` | Các hàm profile/verify cần chuyển sang kiểm tra membership và tách cập nhật hồ sơ thông thường khỏi thay đổi thẩm quyền. |
| `ProcurementSystem.Infrastructure/Services/ContractService.cs:1172` | `ApproveMilestoneAcceptanceAsync`: kiểm `!isAdmin && CreatedBy != userId` ở dòng 1189; ghi `ApprovedBy = userId` ở dòng 1216. Đây là đường duyệt cần thay thế/siết khi triển khai kiểm soát chéo. |
| `ProcurementSystem.API/Controllers/ProcuringEntitiesController.cs` | Có profile `me`, cập nhật `me`, verify và tải file. Không chỉ sửa màn hình mời rồi để endpoint cũ mở quyền. |
| `ProcurementSystem.API/Controllers/AuthController.Register.cs` | Có đăng ký doanh nghiệp và gửi OTP. Luồng nhận lời mời phải tách khỏi đăng ký pháp nhân. |
| `ProcurementSystem.Core/Interfaces/IEmailService.cs`, `ProcurementSystem.Infrastructure/Services/EmailService.cs` | Đọc contract thật trước khi tái sử dụng gửi thư mời. Chưa xác minh khả năng gửi email live. |
| `frontend/src/pages/RegisterPage.jsx`, `frontend/src/context/AuthContext.jsx` | Đọc route/form thật; bổ sung nhận lời mời và quyền theo membership, tránh buộc thành viên đăng ký lại doanh nghiệp. |
| `frontend/src/pages/UsersPage.jsx` | Trang quản trị user nền tảng không mặc nhiên là trang quản lý thành viên của từng doanh nghiệp. |

Agent phải rà thêm tất cả truy vấn `CreatedBy == userId`, `ProcuringEntity.UserId`, navigation `User.ProcuringEntity`, route tải file, báo cáo, notification và SignalR. Dữ liệu sở hữu theo doanh nghiệp cần được tách khỏi người tạo bản ghi; người tạo vẫn được giữ để audit. Không thay hàng loạt kiểm tra cá nhân thành quyền đọc/ghi toàn doanh nghiệp.

## 8. Gợi ý kỹ thuật để Agent thiết kế, chưa phải contract đã chốt

### 8.1. Dữ liệu

- Giữ `Users` làm danh tính cá nhân.
- Bổ sung quan hệ như `ProcuringEntityMembers`: doanh nghiệp, user, trạng thái, quyền nội bộ và thông tin gia nhập/thu hồi. Chống trùng cặp doanh nghiệp–user. Phân công hợp đồng/gói thầu lưu theo mô hình phù hợp source.
- Bổ sung lời mời như `ProcuringEntityInvitations`: doanh nghiệp, email chuẩn hóa, quyền/phạm vi dự kiến, người mời, hash token, hạn nhận, trạng thái/thời điểm chấp nhận hoặc thu hồi.
- Bổ sung thông tin thẩm quyền riêng khi cần: người được xác minh, căn cứ, phạm vi, thời hạn, người xác minh và lịch sử thu hồi/thay thế.
- Audit tham chiếu người thật và doanh nghiệp; không lưu mật khẩu, OTP, token lời mời nguyên bản hoặc private key vào log.

Tên bảng/field trên là gợi ý. Không bắt buộc rewrite toàn bộ bảng Users hoặc xóa `ProcuringEntity.UserId` ngay. Nếu giữ field cũ để tương thích, phải định nghĩa rõ nó là người đăng ký/liên hệ lịch sử, không phải cổng cấp toàn quyền.

### 8.2. API và UI

Các năng lực cần có: liệt kê thành viên, gửi/gửi lại/thu hồi lời mời, xem lời mời an toàn, nhận lời khi đăng ký hoặc đã đăng nhập, đổi phân công/quyền và thu hồi membership. Agent đặt endpoint theo convention hiện có; chưa có URL hoặc DTO mới nào được xác nhận là đã tồn tại.

BE là nơi quyết định quyền mời, quyền được giao, phạm vi dữ liệu, điều kiện ký và trạng thái. FE chỉ hiển thị capability do BE trả và gửi input. Nếu bổ sung enum số, theo cơ chế sinh danh mục từ Core trong handoff FE/BE; không bịa ID cho role hoặc domain chuỗi hiện hữu.

### 8.3. Bảo vệ lời mời

- Token ngẫu nhiên mật mã, đủ mạnh, có hạn, một lần dùng; chỉ lưu hash ở server. Thời hạn cụ thể là cấu hình cần quyết định, chưa chốt con số.
- Token/lời mời gắn với email, doanh nghiệp và quyền/phạm vi; không để FE sửa các giá trị đã được mời.
- Nhận lời phải nguyên tử; hai request đồng thời chỉ có một lần thành công. Không tạo hai user/membership, không nhận token đã hết hạn/thu hồi/được thay thế.
- Kiểm tra lại tư cách người mời, trạng thái doanh nghiệp và quyền được giao lúc chấp nhận; lời mời không được giữ khả năng cấp quyền đã bị thu hồi.
- Preview/GET không thay đổi trạng thái, không tạo user, không kích hoạt membership. Tránh thư viện quét email vô tình nhận lời thay người dùng.
- HTTPS, rate limit, hạn chế rò rỉ token qua log/referrer và chỉ tiết lộ tối thiểu thông tin trước xác thực. Không ghi raw link vào audit hoặc phát qua notification công khai.
- Gửi thư thất bại phải được phản ánh trung thực; không báo đã gửi/kích hoạt nếu chưa có bằng chứng. Không log bí mật để thay cho gửi email.

## 9. Dữ liệu cũ và chuyển đổi

1. Lập kế hoạch migration/backfill từ dữ liệu thật; không đoán người tạo doanh nghiệp hiện tại là lãnh đạo đã xác minh.
2. Có thể giữ người dùng hiện tại như thành viên/đầu mối đăng ký để bảo toàn dữ liệu, nhưng quyền quản lý thành viên và quyền ký cần căn cứ xác minh tương ứng.
3. Giữ nguyên `CreatedBy`, `ApprovedBy` và lịch sử cũ; không viết lại cho giống quy trình mới. Bản ghi cũ thiếu chứng cứ hai người phải được thể hiện đúng là dữ liệu lịch sử.
4. Xử lý rõ dữ liệu doanh nghiệp trùng/không xác định, user có role khác, người đại diện khác người đăng ký và membership đã bị thu hồi.
5. Không áp dụng migration/seed vào DB thật trong bước đọc hoặc soạn kế hoạch. Startup hiện gọi `DataSeeder.SeedAsync` tại `Program.cs:325`; seeder gọi `Database.MigrateAsync` tại `Seeders/DataSeeder.cs:16`. Restart ứng dụng có thể ghi DB.

## 10. Trình tự công việc cho Agent triển khai

1. Đọc tài liệu/quy tắc; xác minh lại root, branch, SHA, dirty tree, DTO, schema và các nhánh quyền thực tế. Không reset/clean làm mất F01–F08.
2. Đặc tả permission matrix, bootstrap thẩm quyền, membership/invitation lifecycle và kế hoạch chuyển dữ liệu cũ. Phân biệt thiết kế gợi ý với quyết định đã chốt.
3. Triển khai BE cho lời mời, nhận lời, quản lý membership và bootstrap có kiểm soát; giữ nguyên identity/password của tài khoản đã có.
4. Chuyển phạm vi dữ liệu và endpoint từ một user sở hữu hồ sơ sang membership/phân công theo từng nghiệp vụ; kiểm tra các đường bypass và rò rỉ chéo doanh nghiệp.
5. Triển khai UI quản lý thành viên/nhận lời mời và cập nhật luồng đăng ký. Không lấy trang Admin Users thay cho authorization theo doanh nghiệp.
6. Triển khai kiểm soát trình/duyệt theo đặc tả được xác lập, bảo đảm không còn endpoint cũ cho phép duyệt trực tiếp. Công nghệ ký cần quyết định riêng trước khi phụ thuộc vào nó.
7. Chạy kiểm thử phù hợp, ghi bằng chứng, kiểm migration ở DB test được xác định. Chỉ chạy live mutations/restart trong phạm vi được giao; không tự merge/push/deploy.

## 11. Tiêu chí nghiệm thu bắt buộc

| Nhóm | Tình huống cần có bằng chứng thực thi |
|---|---|
| Bootstrap | Người đăng ký thường không tự có quyền ký; người đại diện khác người đăng ký nhận đúng liên kết; không thể đổi thông tin profile để chiếm thẩm quyền. |
| Mời hợp lệ | Chỉ người có quyền được mời, chỉ được giao đúng phạm vi; người nhận không đăng ký lại MST/doanh nghiệp. |
| Người dùng mới | Tự đặt mật khẩu và xác thực email; nhận lời tạo đúng một user/membership; không cấp quyền trước khi nhận lời hợp lệ. |
| Tài khoản đã có | Đăng nhập đúng email để nhận; giữ nguyên mật khẩu/email/role không liên quan; email khác không nhận được. |
| Token và cạnh tranh | Hết hạn, thu hồi, dùng lại, resend, sửa payload, hai request đồng thời, người mời mất quyền và doanh nghiệp bị đình chỉ đều được xử lý đúng. |
| Phân quyền | Thành viên không tự nâng quyền; quản lý thành viên không tự có quyền ký; không nhận thêm role Admin hay quyền người ký qua payload lời mời. |
| Cô lập | Chặn truy cập chéo doanh nghiệp, kể cả sửa ID, tải file, report, notification/SignalR; giữ sealed bid và phạm vi giám khảo. |
| Thu hồi | JWT/cache cũ không tiếp tục được thực hiện command sau khi thu hồi quyền; lịch sử được giữ. |
| Maker–Checker | Người trình không tự duyệt; đúng người nhưng sai hợp đồng/thẩm quyền hết hạn bị chặn; endpoint cũ/Admin bypass không mở đường duyệt trực tiếp. |
| Toàn vẹn hồ sơ | Sửa số tiền/tài liệu sau khi trình làm yêu cầu cũ không còn hợp lệ; chống stale approval, duyệt lặp và concurrent approval. |
| Audit | Đúng actor, doanh nghiệp, đối tượng, phiên bản, thời điểm và lý do; lịch sử không bị sửa/xóa qua chức năng thông thường; không có bí mật trong log. |
| FE và hồi quy | Hiển thị đúng nhận lời/expired/revoked/forbidden; không mock kết quả; các chức năng FE/BE và enum F01–F08 không bị mất. |

Phân loại **PASS / FAIL / BLOCKED / NOT RUN** theo bằng chứng. Unit/SQLite/fixture không được gọi là browser E2E hoặc SQL Server live. Không chép kết quả test cũ thành kết quả của tính năng mới.

## 12. Các quyết định còn cần cụ thể hóa

- Một người được tham gia một hay nhiều doanh nghiệp, xử lý user hiện có mang role Contractor/Evaluator; chưa được phép mặc định mở rộng mô hình đa doanh nghiệp hoặc cấp thêm global role ngoài phạm vi.
- Bộ quyền tối thiểu, phạm vi phân công, quy trình giao/đổi người ký và cách xác minh độc lập cụ thể.
- TTL lời mời, chính sách resend/rate limit, phiên đăng nhập và email template.
- Cách xác thực tăng cường khi duyệt: chưa chốt PIN, OTP, passkey hoặc file khóa. Không đưa lựa chọn kỹ thuật vào phần “đã được duyệt”.
- Mức bảo vệ audit, lưu trữ chứng cứ và vận hành sao lưu. Bảng log trong cùng DB không tự tạo tính bất biến/chống chối bỏ tuyệt đối.

Các điểm này cần được Agent đề xuất cụ thể theo source và phạm vi công việc; không dùng chúng để diễn giải lại rằng người dùng chưa chốt mô hình thành viên qua lời mời.

## 13. Nguồn tham khảo và prompt chuyển giao

- [OWASP Authorization Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authorization_Cheat_Sheet.html): kiểm quyền phía server, quyền theo dữ liệu/nghiệp vụ.
- [OWASP Forgot Password Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Forgot_Password_Cheat_Sheet.html#url-tokens): nguyên tắc bảo vệ token qua email, được vận dụng cho lời mời; đây không phải đặc tả riêng cho chức năng invitation.
- [OWASP Transaction Authorization](https://cheatsheetseries.owasp.org/cheatsheets/Transaction_Authorization_Cheat_Sheet.html): gắn xác thực với giao dịch, nội dung và trạng thái.

Prompt có thể gửi cho Agent khi người dùng giao triển khai:

> Tiếp nhận HANDOFF_AGENT_MEMBERSHIP_INVITATIONS_2026-10-26.md. Người dùng đã chốt tài khoản cá nhân độc lập, tham gia doanh nghiệp qua lời mời và phân quyền theo nhiệm vụ/phạm vi; bỏ hướng tài khoản Procurement chung. Xác minh source và bảo toàn dirty tree F01–F08 trên dev trước khi sửa. Phân biệt tài khoản, membership và thẩm quyền ký; người đăng ký không tự thành lãnh đạo, người mời không biết mật khẩu thành viên, quản lý thành viên không tự cấp quyền ký. Đặc tả rồi triển khai theo phạm vi được giao; bảo vệ bootstrap, token lời mời, nhận lời bằng tài khoản mới/cũ, cô lập dữ liệu, thu hồi quyền, Maker–Checker và audit. Không tự chốt công nghệ USB Token/passkey/OTP, không gọi cập nhật trạng thái là chuyển tiền thật, không khởi động ứng dụng mù vì startup có migration/seed. Báo bằng chứng test thật và giới hạn; không tự merge/push/deploy/production migration.

## 14. Trạng thái của lần bàn giao này

- Đã tạo tài liệu quyết định và hướng triển khai.
- Đã đối chiếu Git và các điểm nối source nêu tại mục 7, cùng startup migration/seed.
- Chưa triển khai code, tạo migration, thay DB, restart server hoặc gửi lời mời/email thật.
- Kiểm thử tính năng mới, build, browser E2E, SQL Server và gửi email live: **NOT RUN**.
