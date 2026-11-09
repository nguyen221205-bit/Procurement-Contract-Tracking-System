# Handoff nghiệm thu toàn bộ luồng nghiệp vụ và vai trò

Ngày lập: 06/11/2026.

Repository: `D:\Thuc_Tap_backend\Procurement & Contract Tracking System`.

## 1. Yêu cầu được giao

Xây dựng, thực thi và bàn giao bộ kiểm thử toàn diện cho tất cả phân hệ nghiệp vụ và tất cả vai trò đang tồn tại trong sản phẩm. Bao phủ luồng thành công, nhánh thay thế, bị từ chối, dữ liệu sai, chuyển trạng thái và truy cập chéo đơn vị/người dùng. Đây là yêu cầu làm và chạy kiểm thử, không chỉ viết kế hoạch hoặc checklist.

Mở rộng handoff `HANDOFF_AGENT_REPRODUCIBLE_LIFECYCLE_E2E_2026-11-05.md`; giữ yêu cầu cùng chuỗi dữ liệu xuyên suốt đến Contract Completed. Bổ sung các phân hệ/nhánh chưa nằm trong happy path đó, không thay toàn bộ nghiệm thu bằng một script chỉ tạo gói và hợp đồng.

Snapshot lúc lập: `dev`, HEAD `bf2f10c`. Nhánh B đã được review tại `43db1e3`, còn handoff sửa storage/OTP `HANDOFF_AGENT_B_REVIEW_FIXES_2026-11-06.md`. Không giả định B đã sửa hoặc được merge vào dev. Agent xác nhận HEAD/branch/status thực tế, các thay đổi chưa commit và trạng thái các Agent khác trước khi chạy. Những số liệu/ảnh trước đây không phải bằng chứng cho snapshot mới.

## 2. Quyền làm việc và phạm vi file

Được mở rộng `scripts/testing/lifecycle-e2e/`, `scripts/testing/run-lifecycle-e2e.ps1` hoặc tạo bộ `scripts/testing/full-system-e2e/` với entry point riêng. Tái sử dụng runner hiện có sau khi kiểm tra isolation, fixture và assertion; không mặc định runner đã đúng vì báo cáo ghi PASS.

Tạo các đầu ra được quản lý trong repo:

- `docs/E2E_ALL_WORKFLOWS_INVENTORY.md`: danh mục chức năng, UI, API và trạng thái.
- `docs/E2E_ROLE_PERMISSION_MATRIX.csv`: ma trận quyền và ranh giới truy cập.
- `docs/E2E_ALL_WORKFLOWS_CASES.csv`: danh sách ca kiểm thử có expected, actor, dữ liệu, dependency và test ID.
- `docs/E2E_ALL_WORKFLOWS_RUNBOOK.md`: hướng dẫn cài/chạy lại, cấu hình và cleanup.
- `docs/E2E_ALL_WORKFLOWS_RESULT_YYYY-MM-DD.md`: kết quả ngày thực thi thật, lỗi và hạn chế.
- Runner/test/fixture/dependency lock/config mẫu; output mỗi run có manifest và kết quả máy đọc được.

Được thêm test HTTP/SQL/service bổ trợ trong file test riêng nếu cần. Chỉ sửa README mục hướng dẫn kiểm thử để trỏ runbook mới. Phối hợp trước khi sửa file dependency, `.gitignore`, Program.cs hoặc file chung.

Không tự sửa nghiệp vụ để làm test xanh, không nới quyền, bỏ OTP, tắt kiểm tra version hoặc gán SQL trạng thái nghiệp vụ. Khi gặp lỗi sản phẩm, lưu repro và báo blocker cho Agent phụ trách. Tiếp tục các nhóm test độc lập có thể thực hiện; các ca phụ thuộc bị lỗi ghi BLOCKED rõ ràng.

Không commit/push/merge/deploy, restart dịch vụ gốc, áp migration/ghi CSDL gốc, thay khóa thật, sync/rollback bucket thật hoặc gửi email tới người thật. Không checkout/reset/stash/clean làm ảnh hưởng thay đổi của người khác.

## 3. Định nghĩa mức bao phủ và cách kết luận

Không suy quyền chỉ từ sidebar hoặc thuộc tính Authorize. Đọc cả controller, service, DTO, enum, entity, frontend route/component và quy tắc đã thống nhất. Code hiện tại không tự là expected đúng: nếu code cho thao tác trái nguyên tắc nghiệp vụ, ghi lỗi thay vì sửa expected cho phù hợp.

Trước khi chạy, lập inventory toàn bộ action có trong API, thao tác có trên UI và worker tự động. Mỗi dòng phải liên kết module → luồng/trạng thái → role/scope → test ID → bằng chứng. Bao gồm endpoint tải file, import/export, tài nguyên con và route truy cập trực tiếp; không chỉ CRUD danh sách.

Mỗi tổ hợp áp dụng cần xác định:

- Role toàn hệ thống; vai trò trong entity; userId/entityId; trạng thái user/member/authority và trạng thái tài nguyên.
- Kết quả được phép/từ chối, dữ liệu nào có thể đọc, trường nào phải ẩn, HTTP/body mong đợi và thay đổi DB/audit mong đợi.
- Kết quả thực tế PASS/FAIL/BLOCKED/NOT RUN. N/A chỉ khi không áp dụng có lý do được ghi rõ; không dùng N/A vì khó chạy, thiếu UI hoặc thiếu dữ liệu.

Luồng có UI: chạy trình duyệt thật, thao tác thật, kiểm tra dữ liệu sau reload. Gọi API trực tiếp để kiểm tra bypass/negative là bằng chứng HTTP riêng. Luồng chỉ có API/worker: kiểm thử tích hợp đúng lớp và ghi rõ API integration/worker integration; không đổi tên chúng thành browser E2E. Không mock response các API nghiệp vụ trong ca browser.

Tính coverage bằng số dòng áp dụng đã được thực thi trên tổng inventory đã đối chiếu, có bảng thiếu sót. Không ghi “tất cả role/100% E2E” nếu còn nhóm chức năng hoặc tổ hợp quyền bị BLOCKED/NOT RUN. Không yêu cầu mọi phép tích Descartes vô nghĩa: phân nhóm tương đương có giải thích, nhưng không gộp các ranh giới owner/member/ngoài entity thành một ca.

## 4. Actor và trạng thái bắt buộc

Liệt kê role thực tế từ nguồn/DB. Tối thiểu phải bao phủ:

| Actor | Biến thể cần có |
|---|---|
| Chưa đăng nhập | Giao diện công khai, route riêng tư, API không token/token sai/hết hạn |
| Admin | Quản trị nền tảng; thẩm định; không được thay Checker duyệt chi |
| Procurement Manager | Thành viên entity A; nhiều entity; quản lý thành viên, tiếp quản công việc theo policy |
| Procurement Officer | Maker; người tạo và không phải người tạo; Active/Revoked/Suspended |
| Procurement Finance | Quyền xem/thao tác đúng policy; không được mặc nhiên có quyền của Manager/Officer/Checker |
| Procurement Signer | Authority chưa có/Pending/Verified/hết hạn/Revoked; member Active và bị revoke; cùng người với Maker |
| Evaluator | Được phân công/không phân công/đã bị thay thế; chưa chấm/đã chấm; các giám khảo độc lập |
| Contractor | Chủ tài nguyên/nhà thầu khác; Pending/Approved/Rejected; đã/chưa nộp hồ sơ; trúng/trượt/bị loại/rút hồ sơ |
| User bị hạn chế | IsActive=false; MustChangePassword=true; role vừa bị gỡ/đổi; phiên đăng nhập cũ |

Không coi Manager/Officer/Finance/Signer là role JWT mới nếu mô hình hiện tại dùng membership. Signing authority là điều kiện riêng. Mỗi actor có danh tính/credential/context trình duyệt riêng; không đổi role trong cùng một token giả để thay cho tài khoản thật.

Có ít nhất hai entity A/B, hai nhà thầu, tổ chuyên gia đủ điều kiện hiện hành (tối thiểu ba và số lẻ), user thuộc nhiều entity, user không thuộc entity. Các actor nguy hiểm cho chuỗi chính như bị khóa/revoke dùng bản sao tài khoản test riêng.

## 5. Ma trận các nhóm nghiệp vụ bắt buộc

Inventory dưới đây là mức tối thiểu. Agent phải bổ sung nếu nguồn hiện tại có action/luồng chưa được liệt kê.

| Nhóm | Luồng và nhánh cần kiểm tra |
|---|---|
| AUTH | Đăng ký Contractor/ProcuringEntity; gửi OTP, cooldown, mã thiếu/sai/email hoặc purpose khác/hết hạn/tái sử dụng/quá số lần thử; đăng nhập, refresh, logout, đổi mật khẩu; MustChangePassword; tài khoản khóa; session cũ sau đổi role/khóa/logout theo contract thực tế |
| USER/ROLE | Tạo/xem/sửa/khóa/gỡ role, tìm kiếm/phân trang; giới hạn role chồng lặp/xung đột lợi ích; chặn ảnh hưởng hội đồng đang làm việc; kiểm tra audit và quyền Admin |
| ENTITY | Đăng ký/thẩm định Approved/Rejected; xem và cập nhật hồ sơ pháp lý; bổ sung hồ sơ sau bị từ chối; tải giấy tờ; reset trạng thái thẩm định khi đổi giấy tờ theo policy; người ngoài đơn vị |
| MEMBER | Gửi/xem/hủy/gửi lại invitation nếu có; accept user mới/cũ; email không khớp, token sai/hết hạn/đã dùng; đổi role/revoke; tự nâng quyền; quyền Manager/Officer/Finance; user đa entity; membership revoked với JWT còn hạn |
| SIGNING | Đề nghị, xem, thẩm định/từ chối/thu hồi authority; tài liệu chứng minh; thẩm quyền chỉ đúng entity/thời hạn; membership bị revoke dù authority còn hợp lệ; không cấp Signer bằng lối tắt trái policy |
| PACKAGE | Tạo/chọn entity/cập nhật/xóa theo điều kiện; loại gói/ngân sách/deadline; tài liệu mời thầu; filter/search/pagination; mọi chuyển trạng thái đang hỗ trợ; đóng sớm, duyệt đóng, mở lại/gia hạn, hủy nếu có; đơn vị sở hữu không đổi theo creator |
| OWNERSHIP | Danh sách gói cần đối soát; Admin giải quyết cùng evidence/note và audit; candidate/version thay đổi, lặp request, người thường gọi trực tiếp; quarantine chặn thao tác nhạy cảm; không tự gỡ cờ trên dữ liệu cũ |
| SUBMISSION | Chọn gói, nộp nhiều file báo giá/năng lực/tiến độ theo contract; cập nhật/nộp lại/rút nếu hỗ trợ; sau deadline/sai trạng thái/trùng hồ sơ/thiếu file/giá sai; hồ sơ Disqualified không xóa dấu vết bằng withdraw; tải tài liệu theo đúng owner/hội đồng/entity |
| CLARIFICATION | Hỏi làm rõ gói và yêu cầu giải trình HSDT; ai được hỏi/xem/trả lời; deadline, phản hồi lặp, file đính kèm; tính công khai/riêng tư theo loại; IDOR vào hồ sơ nhà thầu khác |
| EVALUATOR PROPOSAL | Procurement đề xuất; Admin duyệt/từ chối; xem/tải chứng chỉ; tài khoản được cấp và MustChangePassword; email test; người ngoài scope truy cập |
| COMMITTEE/CRITERIA | Tạo/sửa/thay tiêu chí/template nếu có; tổng trọng số, maxScore, tiêu chí giá; phân công/gỡ/thay giám khảo; đủ người/số lẻ/xung đột; thay người khi đã có điểm và tính lại kết quả |
| SCORING | Mỗi giám khảo chấm đúng hồ sơ, lưu/chốt/sửa đúng trạng thái; thiếu điểm/ngoài khoảng; chặn chấm tay tiêu chí giá; điểm của giám khảo bị thay không tính sai; sealed scoring ở mọi API/UI/export; không thấy điểm người khác trước thời điểm được phép |
| AWARD | Summary/rankings; hòa điểm, đủ/thiếu phiếu, gói không hồ sơ; chọn người thắng, lý do chọn khác hạng nếu hỗ trợ; chặn Evaluator/Contractor tự trao thầu; disqualify, revoke award, chọn lại; trường hợp đã có hợp đồng Active/Completed/Terminated |
| CONTRACT | Tạo từ đúng winning submission; điều khoản/giá/ngày, chống tạo hoặc liên kết sai; draft/active/completed/terminated và mọi transition hợp lệ/bị cấm; bản scan, xóa/thay file theo trạng thái; quyền Manager/Officer đúng entity và không phải creator |
| MILESTONE/PROGRESS | Tạo/sửa/xóa, tổng phân bổ không vượt/thiếu khi kích hoạt; cập nhật tuần/%/ghi chú; trùng tuần/giảm tiến độ/sai nhà thầu/sai trạng thái; kiểm tra tiền và tiến độ độc lập |
| ACCEPTANCE | Maker technical pass/fail; Checker duyệt/từ chối/làm lại; self-approval; thiếu/hết hạn/revoked authority/member; sai AcceptanceId/old revision/version; sửa milestone/thay scan làm mất hiệu lực thẩm tra; quyết định lặp không giải ngân hai lần |
| GUARANTEE/ADDENDUM | Thêm/xem/sửa/trạng thái/tài liệu theo action thực tế; ngày/giá trị sai; bảo lãnh hết hạn; phụ lục điều chỉnh có phản ánh đúng hợp đồng/milestone; người không đủ quyền; tải file |
| CONTRACTOR | Hồ sơ công ty/người liên hệ/lịch sử tham gia/đánh giá và rating; xem/sửa đúng owner; đánh giá sau hợp đồng theo policy; import/template/export Excel; dòng sai/trùng/CSV formula injection; không tạo tài khoản đã duyệt hoặc mật khẩu cố định trái policy |
| REPORT/DASHBOARD | Đấu thầu theo năm/loại; tỷ lệ trúng/đúng hạn; hợp đồng sắp hết hạn và chi theo tiến độ; top nhà thầu, export; filter/pagination; đối soát từ dữ liệu test biết trước; phạm vi entity/role và dữ liệu không được lộ |
| NOTIFICATION/WORKER | Hết hạn gói, hợp đồng, milestone, bảo lãnh, giải trình; entity A/B, member revoked/disabled, Admin, contractor; email/DB/SignalR đúng người, chống trùng; đọc/đánh dấu đã đọc không sửa thông báo người khác; reconnect/refresh |
| STORAGE | Upload/download mọi loại tài liệu; Local chuẩn, cloud và fallback khi có môi trường; tệp cũ/plaintext/PSEN, thiếu/sai khóa/file hỏng, đổi Enabled/provider; traversal, loại/size sai; authenticated snapshot/hash; Admin status/test-connection/sync/rollback chỉ trên storage test |
| AUDIT | Truy cập/list/filter/export nếu có; đúng actor/entity/action/thời điểm, không lộ credential; dữ liệu trước/sau đúng; user không đủ quyền không xem audit hệ thống |
| UI/SESSION | Sidebar và deep link, trang chi tiết/dialog/export; session đổi user không còn dữ liệu user cũ; logout/back/refresh; thông báo lỗi, loading, double click; smoke desktop/mobile cho màn hình chính |

Finance hoặc action không có UI vẫn cần đánh giá quyền ở API và ghi thiếu UI nếu ảnh hưởng sử dụng. Không bịa thêm endpoint/action không tồn tại: ghi đúng inventory nguồn, phân biệt chức năng chưa có với chức năng không áp dụng.

## 6. Các chuỗi dữ liệu và nhánh kết thúc

Tối thiểu phải có các scenario độc lập, sinh ID mới qua thao tác hợp lệ:

1. Chuỗi chính: onboarding → tạo gói A → hai nhà thầu nộp → đủ hội đồng chấm → trao thầu → hợp đồng → ít nhất hai milestone → tiến độ tuần → Maker/Checker → tất cả mốc Completed → Contract Completed. Kiểm tra từng bước cùng package/submission/winner/contract và các tổng tiền.
2. Chuỗi bị từ chối: hồ sơ pháp lý bị từ chối → sửa/nộp lại → duyệt; nghiệm thu bị từ chối → xử lý/lập thẩm tra mới → duyệt đúng revision.
3. Chuỗi thay đổi: thay giám khảo khi đang đánh giá; gia hạn/mở lại theo điều kiện; sửa milestone/tài liệu sau review → request cũ bị chặn, bản review mới được chấp nhận.
4. Chuỗi dừng/hủy: rút hồ sơ hợp lệ; truất quyền không được tự rút để xóa dấu vết; thu hồi trao thầu/Terminated và trường hợp được/không được trao lại theo policy.
5. Chuỗi thu hồi quyền: member/authority bị revoke hoặc user bị khóa trong khi JWT/route/dialog cũ còn mở; request tiếp theo phải theo policy hiện hành.
6. Chuỗi vận hành: worker tạo cảnh báo → đúng người nhận trên email test/notification UI → read/unread; báo cáo và audit đối soát cùng dữ liệu.

Nhánh kết thúc khác nhau có dữ liệu độc lập; không bắt một gói phải vừa Completed vừa Terminated để bao phủ. Không seed sẵn kết quả của chính bước cần nghiệm thu, không SQL UPDATE status hay thời gian hệ thống để vượt điều kiện. Điều khiển thời gian chỉ qua seam test có giới hạn và phải công bố rõ; không áp dụng vào production.

## 7. Môi trường và cô lập

- SQL Server/LocalDB, database riêng cho từng runId; storage/output riêng; API/FE cổng riêng do runner quản lý. Kiểm tra môi trường API thực tế trước khi ghi; không chỉ tin tên DB trong config runner.
- Startup có thể migration/worker; chỉ chạy sau khi xác nhận DB, storage, SMTP test. Không dùng API gốc đang mở để tiện test.
- Mail catcher/fake SMTP để thực sự đi qua logic OTP và invitation/email; không dùng mã mặc định/bypass. Ghi lớp nào fake; không mock quyết định nghiệp vụ.
- Cloud dùng bucket/prefix/credential test riêng. Endpoint test-connection có thể tạo bucket, sync/rollback có thể thay dữ liệu: bắt buộc guard tài nguyên test. Không có cloud test thì ghi BLOCKED cho nhóm đó, Local PASS không thay thế.
- Fixture bootstrap tối thiểu Admin/danh mục được phép trong DB mới; onboarding và nghiệp vụ cần nghiệm thu phải thực hiện qua UI/API phù hợp và ghi rõ lớp setup.
- Không dùng credential mẫu trong repo cho hệ thống thật. Auth state/token/OTP/connection string/key/S3 URL nhạy cảm giữ ngoài Git.
- Cleanup chỉ process/DB/file thuộc runId đã kiểm tra; không wildcard xóa tài nguyên chung. Giữ evidence khi fail. Chạy lần sau không phụ thuộc cleanup thủ công lần trước.
- Kiểm tra hai Agent bảo mật/cảnh báo đã tích hợp tới snapshot nào. Nếu chưa, có thể chạy chẩn đoán để phát hiện lỗi, nhưng kết quả release acceptance phải chạy lại trên bản tích hợp ổn định.

## 8. Phương pháp kiểm thử và assertion

Luồng thành công UI cần xác nhận network response cùng trạng thái hiển thị và đọc lại sau reload; kiểm tra DB read-only khi cần đối soát. Không dùng toast, nút disable, hoặc ảnh màn hình đơn lẻ làm bằng chứng bảo mật.

Với mỗi action nhạy cảm, có request trực tiếp từ actor không đủ quyền, actor khác entity và actor khác owner khi áp dụng. Kiểm tra cả status/body và dữ liệu không đổi; không chấp nhận response 200 nhưng success=false là PASS mặc định nếu contract yêu cầu HTTP khác. Public danh sách gói không tự bị coi là leak; phải phân biệt dữ liệu công khai với thông tin niêm phong/riêng tư.

Rà mọi endpoint trả cùng loại dữ liệu: điểm có thể lộ qua summary/rankings/export/detail; file qua endpoint chính/snapshot/tài nguyên con; quyền ghi qua thao tác thường và API thay thế. Không chỉ kiểm tra một route rồi suy toàn bộ an toàn.

Tiền đối soát theo decimal/quy tắc làm tròn server. Export Excel/báo cáo phải được đọc nội dung để xác nhận hàng/số liệu/phạm vi, không chỉ assert file tải được. File tải về cần so SHA-256 bytes khi có snapshot hash. “Giải ngân” của app không được mô tả thành chuyển tiền ngân hàng thật.

Concurrency quan trọng như duyệt trùng/revoke chen giữa/sửa sau review cần lớp SQL integration bằng kết nối độc lập khi phù hợp. Request tuần tự không chứng minh race condition. Dùng suite concurrency hiện có, bổ sung theo gap thực sự; báo riêng với browser E2E.

Không nuốt exception/assertion, không đổi expected vì app đang trả sai, không tự retry thao tác ghi làm phát sinh bản ghi trùng để che lỗi. Retry cần giới hạn và điều kiện rõ; lỗi chỉ xảy ra lần đầu vẫn phải được lưu và báo.

## 9. Khả năng chạy lại và evidence

Runner có entry point thực thi được và lock/version dependency; không chỉ dựa vào scratch hoặc thao tác nhớ của tác giả. Chia suite theo module/role để chạy độc lập và có một lệnh chạy toàn bộ. Có preflight và exit code khác 0 khi FAIL/BLOCKED chưa được giải quyết.

Chạy ít nhất hai lượt đầy đủ trên dữ liệu mới của cùng snapshot ổn định. RunId/email/code/ID tự sinh; không hardcode package/contract/member từ database cũ. Nếu còn blocker, không giả vờ đã hoàn tất hai lượt; báo phần đã chạy và blocker còn lại.

Mỗi run lưu:

- Manifest: UTC timestamp, SHA, branch, dirty hash/diff nếu có, version công cụ, môi trường đã sanitize, actor và sơ đồ FK.
- Case results machine-readable: testId/module/role/scope/layer/expected/actual/status/duration/error/evidence link. Nêu setup API hoặc bootstrap ở bước nào.
- Screenshot/trace/console/network phù hợp; failure evidence giữ ngay bước lỗi. Trace/auth state gốc có thể chứa bí mật phải giữ ngoài Git và sanitize trước khi công bố.
- Đối soát chain, số tiền, trạng thái, audit actor/entity; kết quả thông báo và email đúng người nhận; bộ export được kiểm tra nội dung.
- Inventory coverage: tổng applicable, đã chạy PASS/FAIL, BLOCKED/NOT RUN, N/A có lý do, nhóm thiếu môi trường.

Không copy ảnh hoặc test result cũ làm evidence mới. Không commit log/ảnh lớn hàng loạt; lưu có tổ chức và chọn bằng chứng đã sanitize. Mọi đường dẫn trong báo cáo phải tồn tại và khớp runId.

## 10. Thứ tự làm và xử lý lỗi

1. Xác nhận snapshot/môi trường; rà runner cũ; lập inventory và ma trận role/action/state trước.
2. Dựng fixture/guard/isolation và thực thi onboarding/auth trước để các suite sau có actor hợp lệ.
3. Chạy chuỗi chính đến Completed, sau đó các nhánh độc lập và toàn bộ module trong inventory.
4. Chạy negative API/scope/session, đọc file/export/report và worker/operational suites.
5. Tổng hợp lỗi theo severity: thao tác sai actor/chéo entity, lộ dữ liệu hoặc tiền sai là ưu tiên cao. Báo blocker với request/repro/file/hàm nghi ngờ, tác động và bằng chứng. Không tự sửa sản phẩm ngoài phạm vi.
6. Sau khi lỗi được Agent sản phẩm sửa trên snapshot mới, chạy lại ca lỗi và nhóm bị ảnh hưởng; rồi thực thi lại các lượt full acceptance cần thiết trên bản cuối.
7. Chạy các regression/build cần thiết, ghi thật kết quả; những test service/Node không thay thế kết quả browser.

Nếu một module fail, tiếp tục nhóm độc lập để inventory không dừng ở lỗi đầu. Không cho các ca sau chạy trên dữ liệu đã hỏng rồi ghi lỗi dây chuyền thiếu nguyên nhân; ghi dependency và BLOCKED đúng chỗ.

## 11. Điều kiện hoàn thành

- Inventory được đối chiếu với toàn bộ controller/service/UI/worker hiện hành, không bỏ qua vai trò Finance/Signer/Evaluator/Contractor hoặc endpoint ít dùng.
- Mỗi tổ hợp quyền áp dụng có expected rõ, test/evidence thật; bảng coverage chỉ ra phần nào chưa đạt.
- Chuỗi chính hoàn tất cùng dữ liệu, mọi nhánh phụ có kết quả và mỗi role có cả allowed/denied khi áp dụng.
- Hai lần full run tái lập trên snapshot cuối có ID/dữ liệu mới; không dùng SQL sửa trạng thái hoặc mock API nghiệp vụ.
- Không còn lỗi chặn nghiệm thu về truy cập, tính toàn vẹn tiền/dữ liệu hoặc chuỗi trạng thái. Nếu còn, kết luận chưa đạt, dù nhiều case PASS.
- Runbook, runner/dependency, fixture, case matrix và báo cáo đủ để người khác tự chạy lại; secrets không nằm trong repo/evidence công khai.
- Kết luận phân biệt browser E2E, HTTP integration, worker/SQL/storage tests và phần môi trường chưa chạy; không tuyên bố toàn hệ thống PASS khi còn bỏ trống.

## 12. Prompt giao Agent

> Đọc HANDOFF_AGENT_ALL_WORKFLOWS_ALL_ROLES_E2E_2026-11-06.md. Xây dựng và thực thi nghiệm thu tất cả luồng nghiệp vụ và mọi role/scope đang có trong sản phẩm. Lập inventory API/UI/worker và ma trận quyền/trạng thái, mở rộng runner chạy lại được trên môi trường cô lập. Thực hiện cùng chuỗi từ tạo gói đến Contract Completed và các nhánh thay thế/âm cho toàn bộ phân hệ; kiểm chứng server, dữ liệu, tiền, audit, export/file và session. Chạy hai full run trên dữ liệu mới của snapshot cuối, lưu evidence đã sanitize, báo coverage và blocker trung thực. Khi có lỗi sản phẩm, lưu repro và bàn giao Agent phụ trách thay vì nới bảo mật hoặc sửa expected. Không commit/push/merge/deploy, không restart API gốc, không ghi DB/storage thật hoặc gửi mail thật; phối hợp các Agent đang sửa OTP/storage/cảnh báo để không ghi đè và phải chạy lại sau tích hợp.
