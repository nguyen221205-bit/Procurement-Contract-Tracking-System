# Handoff xây dựng bộ nghiệm thu toàn vòng đời có thể chạy lại

Ngày lập: 05/11/2026.

Repo: `D:\Thuc_Tap_backend\Procurement & Contract Tracking System`.

## 1. Mục tiêu và giới hạn

Triển khai và thực thi bộ nghiệm thu có thể chạy lại trên môi trường cô lập, dùng cùng một chuỗi dữ liệu từ mở thầu đến hoàn tất hợp đồng:

`Entity → BidPackage → BidSubmissions → kết quả trao thầu → Contract → Milestones → Technical Acceptances → phê duyệt → Contract Completed`.

Không dùng gói A để chứng minh mở thầu, hợp đồng B để chứng minh nghiệm thu rồi ghép thành một kết quả E2E. Không coi ảnh cũ hoặc báo cáo cũ là kết quả của lần chạy mới.

Chỉ thực hiện phần 3 trong đánh giá dự án: bộ nghiệm thu tái lập và bằng chứng cùng chuỗi dữ liệu. Không nhận thêm phần sửa cảnh báo, OTP, khóa storage, ERD hay sửa hàng loạt báo cáo lịch sử.

Baseline HEAD lúc lập: `dev`, `eb9d90a`. Working tree đang có thay đổi của Agent A ở worker, `NotificationRecipientResolver.cs`, `NotificationRecipientScopeTests.cs` và tài liệu kết quả; chưa coi các thay đổi đó là đã tích hợp/đạt nghiệm thu. Kiểm tra lại HEAD/status và trao đổi với các Agent đang sửa trước khi bắt đầu.

## 2. Phạm vi file và phối hợp

Được tạo/sửa:

- `scripts/testing/lifecycle-e2e/`: runner, cấu hình mẫu, fixture, các test trình duyệt và hướng dẫn phụ thuộc.
- `scripts/testing/run-lifecycle-e2e.ps1`: một entry point chạy toàn bộ quy trình.
- File dependency/lock riêng cho runner, ưu tiên trong thư mục trên; không thêm phụ thuộc vào frontend/package.json nếu không cần.
- `docs/E2E_LIFECYCLE_RUNBOOK.md`: hướng dẫn người mới chạy lại.
- `docs/E2E_LIFECYCLE_RESULT_2026-11-05.md`: kết quả thực tế, cập nhật ngày nếu thực thi ngày khác.
- `.gitignore`: chỉ thêm mục bỏ qua secrets, browser auth state, log nhạy cảm và output lớn khi cần; phối hợp trước khi sửa file chung.
- README mục kiểm thử: chỉ thêm link tới runbook mới sau khi bộ kiểm thử thực sự chạy được; tránh đụng phần các Agent khác đang sửa.

Mặc định dùng Playwright và stack thuận tiện trong môi trường hiện có; dependency và browser version phải được khai báo để người khác cài lại được. Không để script duy nhất trong thư mục scratch bị bỏ qua bởi Git.

Không sửa file nghiệp vụ của Agent A/B, không nới authorization, bỏ OTP, gán SQL trực tiếp trạng thái Completed hay cấp quyền để làm test xanh. Nếu phát hiện lỗi sản phẩm, giữ failure evidence và bàn giao blocker cùng file/hàm/repro; chỉ sửa sản phẩm sau khi được giao rõ phạm vi.

Không tự commit/push/merge/deploy hoặc restart API gốc. Nếu dùng chung working tree, không switch/reset/stash/clean và không chạy build/test .NET trùng thời điểm với Agent khác. Lần nghiệm thu cuối phải thực hiện sau khi các sửa cần thiết ổn định. Ghi HEAD và hash/diff của phần chưa commit; nếu code đổi giữa chừng, hủy kết quả đó và chạy lại trên snapshot ổn định.

## 3. Môi trường chạy lại

Runner phải tự kiểm tra trước khi ghi dữ liệu:

1. Database SQL Server/LocalDB có tên riêng cho E2E, tốt nhất sinh theo runId; storage/output tách riêng. Không sử dụng database gốc hoặc bản sao dùng chung của người khác.
2. API/FE dùng cổng riêng, base URL qua cấu hình; từ chối kết nối nhầm API gốc. Không chỉ kiểm tra tên database trong chuỗi kết nối: xác nhận môi trường API thực tế bằng cơ chế kiểm tra phù hợp trước khi bắt đầu. Không thêm endpoint công khai làm lộ connection string.
3. Startup có thể chạy migration/worker: chỉ khởi động process riêng sau khi cấu hình DB, storage và email test đã được xác nhận. Không dựa vào API đang chạy ở 5225 là môi trường test.
4. Email dùng fake SMTP/mail catcher cô lập; OTP lấy từ mailbox test hoặc cơ chế test có kiểm soát, không bypass logic xác minh. Không dùng inbox thật hay in OTP/token/password ra báo cáo.
5. Storage Local cô lập là cấu hình chuẩn tối thiểu cho bộ vòng đời. Nếu chạy thêm Supabase, dùng bucket/prefix test riêng, cấu hình khóa hợp lệ qua secret và báo kết quả cloud riêng. PASS Local không chứng minh cloud fallback.
6. Quy trình tạo fixture ưu tiên API hợp lệ. Nếu cần bootstrap Admin hoặc dữ liệu danh mục, giới hạn seed đó trong DB test mới và mô tả chính xác; không seed sẵn package/award/contract/acceptance để thay các bước đang nghiệm thu.
7. Cleanup chỉ được dừng process do runner tạo và xử lý DB/file thuộc đúng runId đã xác nhận. Khi thất bại, giữ bằng chứng để điều tra; không xóa dữ liệu người khác hoặc chạy DELETE/DROP theo wildcard.

Runbook phải có: yêu cầu SDK/Node hoặc Python/browser/LocalDB; lệnh cài dependency; cấu hình mẫu không chứa secret; lệnh một lần chạy; nơi xem kết quả; cách chạy lại và cleanup. Thiếu môi trường phải báo BLOCKED/FAIL, không return sớm rồi exit 0.

## 4. Dữ liệu và actor

Mỗi lần chạy sinh runId, email/mã gói riêng. Không hardcode ID 2070, hợp đồng 23, entity 1 hoặc trạng thái từ database cũ.

Chuẩn bị tối thiểu:

- Một Admin để thẩm định đơn vị/nhà thầu/thẩm quyền theo workflow; không dùng Admin thay Checker duyệt chi.
- Entity A đã được duyệt; Manager A, Officer A/Maker và Checker A là ba người riêng. Checker có membership Active và signing authority được xác minh qua quy trình phù hợp.
- Entity B và user Procurement B làm đối chứng truy cập chéo.
- Hai nhà thầu độc lập đã hoàn tất điều kiện đăng ký/thẩm định.
- Tổ chuyên gia đáp ứng quy tắc hiện hành, tối thiểu ba người và số lẻ; mỗi người đăng nhập riêng.
- Tối thiểu hai milestone để chứng minh giải ngân từng phần và hoàn tất toàn bộ. Tổng phân bổ phải đúng giá trị hợp đồng.

Ưu tiên thực hiện lời mời/chấp nhận thành viên qua UI, dùng email test. Không mời trực tiếp Signer để né quy trình thẩm quyền. Manifest lưu actor userId/role/entity/memberId/authorityId và các ID nghiệp vụ, không lưu credential hay bearer token.

## 5. Kịch bản chính bắt buộc trên trình duyệt

Fixture setup có thể dùng API để chuẩn bị các actor đã được duyệt; ghi rõ bước nào setup/API. Các thao tác nghiệp vụ chính dưới đây phải thật sự đi qua UI trình duyệt. Không gọi API tạo hộ cả vòng đời rồi chỉ mở trang chụp ảnh.

| Bước | Actor và thao tác | Điều phải xác nhận |
|---|---|---|
| 1 | Manager/Officer chọn Entity A, tạo gói, nhập tiêu chí và tài liệu mời thầu | Package mới Open, đúng ProcuringEntityId; ghi packageId/code vào manifest |
| 2 | Hai Contractor đăng nhập riêng, nộp nhiều tài liệu đúng loại và giá dự thầu | Hai submission cùng packageId, contractorId khác nhau, thời gian nộp hợp lệ; file tải lại đúng nội dung |
| 3 | Bên mời thầu phân công tổ chuyên gia, đóng thầu và bắt đầu đánh giá | Chuyển trạng thái hợp lệ, không sửa giờ hệ thống/DB để lách điều kiện |
| 4 | Từng Evaluator chấm đủ phần tiêu chí được phép | Điểm được lưu; trong giai đoạn cần niêm phong không lộ điểm người khác; điểm giá do server tính |
| 5 | Bên mời thầu xem xếp hạng, ghi căn cứ và trao thầu | Selected submission thuộc chính package; winner contractorId và giá trúng thầu được ghi nhận |
| 6 | Tạo hợp đồng từ kết quả đó | contract.bidPackageId = packageId; contractorId đúng winner; giá trị hợp lệ |
| 7 | Tạo ít nhất hai milestone, upload scan và kích hoạt hợp đồng | Tổng phân bổ đúng; Contract Active; package trạng thái tương ứng; tải scan đúng bytes |
| 8 | Contractor cập nhật tiến độ tuần, % hoàn thành, ghi chú | Dữ liệu đúng contractId, lưu và đọc lại sau refresh; thử sai tuần/giảm tiến độ theo quy tắc hiện hành |
| 9 | Officer kiểm tra kỹ thuật mốc thứ nhất, Checker khác phê duyệt đúng revision/version | Đúng Maker/Checker; milestone hoàn tất và số đã giải ngân đúng; hợp đồng chưa Completed khi còn mốc chưa hoàn tất |
| 10 | Thực hiện tiến độ/thẩm tra/duyệt các mốc còn lại | Tất cả mốc Completed, Contract Completed; số giải ngân và số còn lại khớp giá trị hợp đồng |
| 11 | Refresh/đăng nhập lại, kiểm tra chi tiết, báo cáo và audit | Trạng thái được lưu bền vững; báo cáo phù hợp dữ liệu test; có actor và sự kiện đúng các ID của cùng chuỗi |

Lấy AcceptanceId/revision/version từ dữ liệu thực của lần chạy. Không thay tất cả request bằng ID mẫu. Đồng bộ UI bằng trạng thái/response thực, không dùng sleep cố định làm tiêu chí thành công.

## 6. Ca âm phải kiểm tra ở server

Thực hiện tại thời điểm phù hợp trước khi hợp đồng kết thúc. Không để ca âm làm hỏng chuỗi chính; khi có thay đổi hợp lệ phục vụ kiểm tra, phải thực hiện lại thẩm tra theo đúng workflow trước khi tiếp tục.

1. Procurement B thử đọc dữ liệu nhạy cảm/ghi gói hoặc hợp đồng của A và tải snapshot: server từ chối. Chọn endpoint có ranh giới riêng tư rõ; không yêu cầu danh mục gói công khai phải bị chặn.
2. Maker gửi trực tiếp request duyệt acceptance do mình thẩm tra: server từ chối; không chỉ chụp nút bị disable.
3. Một user vẫn có JWT nhưng membership A vừa bị revoke thử ghi/duyệt: server từ chối. Dùng actor phụ hoặc khôi phục qua workflow hợp lệ để không phá chuỗi chính; không dùng SQL bật lại quyền.
4. Checker mở revision/version cũ, sau đó milestone/tài liệu thay đổi: request cũ bị chặn với conflict theo contract API; tạo bản thẩm tra mới rồi mới duyệt thành công.
5. Tải snapshot không đăng nhập → 401; user ngoài scope → 403 theo policy; user đủ quyền → tải được, hash bytes khớp snapshot. Không coi URL public là authenticated download.
6. Admin nền tảng thử duyệt chi thay Checker → bị chặn theo policy.

Mỗi ca phải khẳng định cả HTTP status/body và dữ liệu nghiệp vụ không bị đổi khi bị từ chối. Nếu quy tắc API thực tế khác tài liệu, ghi sai khác để review thay vì sửa expected cho test xanh. Không tuyên bố kiểm thử tranh chấp đồng thời chỉ từ các request tuần tự; suite SQL concurrency hiện có là lớp bằng chứng riêng.

## 7. Đóng gói bằng chứng

Mỗi run có thư mục output theo runId, gồm:

- `manifest.json`: thời điểm UTC, commit, trạng thái working tree/hash nguồn nếu có sửa chưa commit, version công cụ, môi trường đã làm sạch secret, actor và FK của chuỗi chính.
- Kết quả từng case có expected/actual, actor, entity, các ID liên quan, step setup/API/browser, HTTP status khi cần.
- Ảnh theo bước, trace/video khi phù hợp, browser console/network errors; giữ failure screenshot ngay bước lỗi.
- Bảng đối chiếu cuối: package → winning submission → contractor → contract → từng milestone → acceptance/revision → Maker/Checker. Kiểm tra quan hệ bằng dữ liệu trả về/API hoặc truy vấn read-only trên chính DB test.
- Số liệu đầu/cuối: contract value, từng amount, đã duyệt/đã giải ngân, còn lại. Chỉ ghi các khái niệm đúng mô hình hiện có; không mô tả giải ngân ghi nhận trong app là chuyển tiền ngân hàng thật.
- Kiểm tra audit bằng actor và EntityId, không chỉ tìm một dòng có từ APPROVE hoặc chụp trang nhật ký bất kỳ.

Sanitize trước khi đưa vào repo: token, password, OTP, connection string, khóa storage, signed URL và auth state không được nằm trong log/trace/ảnh công khai. Trace gốc có thể chứa bí mật phải giữ ngoài Git; xuất bản bản đã xử lý hoặc bằng chứng tối thiểu an toàn.

Không commit output lớn hàng loạt. Giữ runner/fixture/assertion/runbook trong repo; báo cáo dẫn tới bộ evidence được lưu giữ phù hợp và một số ảnh chọn lọc. Các file được tham chiếu phải thực sự tồn tại.

## 8. Tiêu chí hoàn thành

1. Người khác có thể cài dependency và chạy từ runbook bằng entry point đã bàn giao, không cần biết ID/mật khẩu trong máy tác giả.
2. Thực thi thành công ít nhất hai lần liên tiếp với runId và dữ liệu mới, không cần sửa script thủ công giữa hai lần.
3. Mỗi lần hoàn tất cùng một chuỗi từ package mới đến Contract Completed; mọi FK và tổng tiền được assert, không chỉ kiểm tra toast.
4. Các ca âm có request thật và chứng minh không thay đổi nghiệp vụ trái phép.
5. Không có assertion lỗi bị catch rồi bỏ qua; runner exit khác 0 khi FAIL/BLOCKED; kết quả phân biệt PASS/FAIL/NOT RUN/BLOCKED.
6. Không mock API nghiệp vụ trong các bước browser E2E. Nếu fake mail/clock hoặc fixture setup được dùng, nêu rõ phạm vi; không giả lập response trao thầu/nghiệm thu.
7. Không thay code/config sau lần chạy cuối rồi dùng lại kết quả cũ. Nếu hai Agent sửa bảo mật/cảnh báo sau đó, phải chạy lại bộ nghiệm thu trên snapshot đã tích hợp trước khi kết luận cho snapshot đó.

Kết quả backend 80/80 và frontend 6/6 đã ghi ở lượt đánh giá trước chỉ là baseline, không phải kết quả bộ E2E này. Không lấy số liệu cũ hoặc ảnh mới thêm vào Git để tự kết luận PASS.

## 9. Bàn giao cuối và prompt cho Agent

Báo cáo cuối cần có file đã tạo, cách chạy chính xác, snapshot đã chạy, hai runId, ma trận kết quả, bằng chứng Contract Completed và các blocker còn lại. Nếu thất bại, bàn giao bộ runner cùng repro/failure evidence và phần đã chạy; không đặt trạng thái hoàn thành E2E.

Prompt giao Agent:

> Đọc handoff này và xây dựng, thực thi bộ nghiệm thu browser E2E có thể chạy lại từ tạo gói thầu đến Contract Completed trên cùng một chuỗi dữ liệu. Chuẩn bị môi trường/actor cô lập, lưu manifest và kiểm tra FK, tiền, audit, các ca âm phân quyền. Chạy ít nhất hai lần với dữ liệu mới, ghi kết quả thật và bàn giao entry point cùng runbook. Phối hợp với hai Agent đang sửa cảnh báo/OTP/storage; không sửa đè file họ sở hữu, không bỏ kiểm soát bảo mật để làm test xanh. Không commit/push/deploy, không restart API gốc hoặc ghi vào database/storage thật. Khi phát hiện lỗi sản phẩm, ghi blocker và repro để được giao sửa riêng.
