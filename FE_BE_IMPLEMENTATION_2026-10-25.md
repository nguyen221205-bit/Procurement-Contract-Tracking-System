# Bàn giao sửa phân chia FE/BE và enum

Ngày: 25/10/2026. Nhánh: `dev`. Baseline SHA: `0446e500f1188b8fc27311450672fde6a3be68a1`.
Thay đổi hiện nằm trong working tree; chưa commit/push/deploy. Báo cáo phát hiện ban đầu: `FE_BE_ENUM_AUDIT_2026-10-25.md`.

## Thay đổi đã thực hiện

| Finding | Thực hiện |
|---|---|
| F01: thay tiêu chí nhiều request | Thêm `PUT /api/evaluations/packages/{packageId}/criteria`. BE validate cả bộ, kiểm quyền/trạng thái/điểm đã có, so snapshot toàn bộ tiêu chí, transaction Serializable và rollback nếu có lỗi. FE gửi một command duy nhất. Snapshot khác dữ liệu hiện tại trả 409. |
| F02: FE tự dựng báo cáo | Xóa toàn bộ fallback báo cáo, MST mẫu, suy diễn tổ trưởng/kết luận. Chỉ dùng response báo cáo BE; API lỗi thì hiển thị lỗi và thử lại. Chặn response cũ ghi đè dữ liệu khi đổi gói hoặc đóng modal. |
| F03: điểm sai MaxScore | BE trả `criteriaBreakdown`, `evaluatorSummaries`, `normalizedScore`, `weightedScore`; chỉ tính điểm của giám khảo hiện thuộc tổ. Leaderboard và so sánh dùng breakdown BE. Phiếu chưa lưu dùng helper preview chuẩn hóa MaxScore và ghi rõ điểm chấm tay dự kiến, không cộng giả điểm giá. |
| F04: KPI tính từ trang hiện tại | Response danh sách hợp đồng thêm `summary`, aggregate trên cùng query/bộ lọc/phạm vi nhà thầu trước pagination. FE không cộng items thành KPI. Dữ liệu chưa có hiển thị dấu gạch; tránh response cũ ghi đè bộ lọc mới. |
| F05: notification enum | Normalize tên/số qua một helper dùng enum sinh từ BE; bổ sung Success. SignalR dùng cùng JsonStringEnumConverter với REST để thống nhất biểu diễn. |
| F06: quyền Admin phụ lục | Dùng `hasRole('Admin')`, bỏ kiểm tra `user.role` sai cấu trúc. Điều kiện Pending dùng mã trạng thái, không so nhãn tiếng Việt. |
| F07: Cancelled bị thiếu | Bổ sung constant, badge số/tên và tab lọc hủy thầu. Kiểm thử đảm bảo mọi trạng thái gói đều có mapping. |
| F08: phân bổ tiền lệch BE | DTO hợp đồng trả `allocatedAmount`, `unallocatedAmount`, `isFullyAllocated`, `canAddMilestone`; FE dùng kết quả BE thay ngưỡng lệch dưới 1 đồng. Số dư phân bổ hiển thị được phần thập phân. |

Các điểm tách trách nhiệm bổ sung:

- Chi tiết gói lấy `isCommitteeValid`, `canStartEvaluation`, `evaluationBlockingReasons`, `canEditCommittee`, `requiresEarlyCloseReason` từ BE. Quyền hiển thị quản lý còn kiểm Admin hoặc Procurement sở hữu gói; BE vẫn kiểm lại ở command.
- Summary đánh giá trả `canFinalize`, `eligibleSubmissionIds`; FE không tự kết luận mọi hồ sơ đã đủ điều kiện trao thầu. Nút chọn ứng viên tuân theo eligibility của BE, bao gồm ràng buộc hợp đồng bị chấm dứt.
- Xếp hạng tín nhiệm dùng `ContractorRatingPolicy` ở Core; danh sách nhà thầu dùng grade BE. Form chưa lưu chỉ thể hiện điểm dự kiến; thông báo sau lưu dùng grade từ response.
- Số tiền là đầu vào bảo lãnh; BE tính tỷ lệ từ giá trị hợp đồng hiện tại, kiểm dữ liệu mâu thuẫn nếu client cũ còn gửi tỷ lệ. FE không gửi hai nguồn tiền/tỷ lệ cùng quyết định kết quả.
- Gợi ý tiêu chí giá mang flag `isPriceCriteria` rõ ràng, không suy ra nghiệp vụ bằng substring tên tiếng Việt.

## Phạm vi chuẩn hóa enum

- `frontend/scripts/generate-enums.mjs` sinh `src/api/enums.generated.js` trực tiếp từ các enum số trong Core. `npm run generate:enums` cập nhật; prebuild kiểm danh mục còn đồng bộ, chấp nhận cả LF/CRLF.
- `enumContract.js` là ranh giới chuyển tên legacy sang ID số trong JSON body/query của gói thầu, hợp đồng, phụ lục và bảo lãnh. Bảo toàn giá trị 0; từ chối mã không thuộc enum.
- BE bổ sung EnumDataType cho request/filter và JsonRequired cho các field JSON bắt buộc. Loại file multipart được kiểm miền enum ngay trong service trước khi ghi file.
- Response giữ nguyên cấu trúc/tên enum hiện tại trong đợt này để tương thích các màn hình và client đang có. Đây là giai đoạn chuyển request sang ID; không tuyên bố toàn bộ response đã thành enum số.
- `BidSubmissionStatus`, verification status, trạng thái đề xuất giám khảo và role code vốn là chuỗi theo entity/DTO hiện tại được giữ nguyên. Không bịa ID hoặc đổi kiểu DB của các domain này. Không có migration DB mới.

## Kiểm tra đã chạy

| Lệnh/kiểm tra | Kết quả |
|---|---|
| `dotnet build ProcurementSystem.slnx --no-restore -p:OutDir=<TEMP>/procurement-fe-be-build-20261025/ -v:q` | Thành công, 0 errors, 0 warnings |
| `dotnet test tests/ProcurementSystem.Tests/ProcurementSystem.Tests.csproj --no-restore -v:q` | 21 passed, 0 failed, 0 skipped; target net8.0 |
| `npm run build` trong frontend | Thành công; còn cảnh báo chunk bundle lớn hơn 500 kB và thông tin plugin timing |
| `npm test` trong frontend | 6 passed, 0 failed |
| Oxlint qua script `npm run lint`; lần kiểm kê cuối bằng JSON formatter | Exit 0, 0 errors, 248 warnings; chưa dọn toàn bộ warning ngoài phạm vi |
| `git diff --check` | Không có lỗi whitespace |

Kiểm thử BE dùng SQLite trong bộ nhớ, service/repository thật và transaction thật; không kết nối/ghi DB procurement hiện tại. SQLite có conversion decimal sang double riêng trong context test để hỗ trợ order/aggregate, production vẫn dùng SQL Server decimal. Các kết quả này không thay thế kiểm thử SQL Server/concurrency/live E2E.

Nội dung kiểm thử: rollback sau khi xóa bộ cũ rồi insert lỗi; thay bộ thành công; snapshot cũ; sai chủ sở hữu; trạng thái khóa; trọng số sai; tiêu chí đã có điểm; chặn loại file không xác định; chuẩn hóa thang điểm 50/100/1000; điểm giá tính ở BE; giám khảo chỉ xem dữ liệu của mình; loại điểm giám khảo cũ; niêm phong bảng điểm gói Open; readiness từ dữ liệu BE; KPI không đổi khi chuyển trang, đúng filter và đúng contractor scope; precision phân bổ; enum 0/tên legacy/mã lạ/field JSON bị thiếu; danh mục enum sinh đúng nguồn; preview chấm tay không tạo điểm giá.

Trong quá trình làm, kiểm thử bổ sung phát hiện truy vấn chi tiết chưa nạp dữ liệu readiness; đã sửa bằng query count/trọng số trực tiếp và giữ test regression. Một fixture upload ban đầu đi vào nhánh hồ sơ nộp trùng trước bước kiểm enum; đã sửa setup để kiểm đúng nhánh. Các lần chạy cuối ở bảng trên đều qua.

## Chạy bản mới và giới hạn còn lại

- Chưa restart BE đang phục vụ ở localhost:5225; chưa gửi mutation API lên dữ liệu người dùng, chưa chạy browser E2E. BE đang chạy chưa tự nhận các endpoint/DTO mới chỉ nhờ FE hot reload.
- Cần dừng và khởi động lại BE theo cách chạy dự án thông thường, đồng thời refresh FE, để dùng hai phía đồng bộ. Không dùng FE mới với BE cũ để kết luận các thao tác readiness/template đã hoạt động live.
- Không chạy migration, không thay schema, không thay role claim/JWT hoặc tự chuyển trạng thái vốn lưu bằng chuỗi sang ID.
- Nên kiểm thử live sau restart bằng dữ liệu test: áp dụng template ở Open/Closed; báo cáo API lỗi/thành công; chấm bằng hai tài khoản; đổi trang/bộ lọc hợp đồng; duyệt phụ lục bằng Admin. Không dùng hồ sơ/hợp đồng đã hoàn tất để chạy lại thao tác ghi khi chưa xác định dữ liệu test phù hợp.
- `PackageDetailPage.jsx` được chuẩn hóa xuống dòng LF khi chỉnh sửa; phần diff có thêm thay đổi line ending của file legacy.
