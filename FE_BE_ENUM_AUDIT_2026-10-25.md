# Rà soát phân chia FE/BE và enum

Ngày: 25/10/2026. Repository: Procurement & Contract Tracking System.
Baseline: nhánh `dev`, SHA `0446e500f1188b8fc27311450672fde6a3be68a1`.

## Phạm vi và kết luận

Đọc các API client, component/page FE, đối chiếu DTO, enum, controller và service BE có liên quan đến gói thầu, tiêu chí, hồ sơ, đánh giá, hợp đồng, phụ lục, bảo lãnh, phân quyền và thông báo. Không sửa implementation, không đổi DB, không gọi API ghi dữ liệu.

Có nghiệp vụ đang được FE tự tổng hợp/quyết định, một số chỗ đã lệch BE. Tuy nhiên, không phải mọi phép tính/validation ở FE đều cần loại bỏ: định dạng, tương tác form, kiểm tra đầu vào sớm và xem trước được giữ lại; BE phải quyết định kết quả được lưu/công bố và tính hợp lệ của thao tác.

Việc gửi `Goods`, `Closed`, `Active` hiện phù hợp cấu hình JSON của BE, chưa phải lỗi gọi API: `ProcurementSystem.API/Program.cs:180` bật `JsonStringEnumConverter`. Nếu yêu cầu thiết kế là truyền giá trị enum số, phải chuyển đồng bộ hợp đồng API, DTO và toàn bộ FE. Không chỉ sửa FE hoặc xóa converter toàn cục.

## Các finding cần sửa

### F01 — P1: FE tự điều phối thay thế tiêu chí, không bảo đảm nguyên tử

- FE: `frontend/src/components/packages/CriteriaTemplateModal.jsx:37` xóa từng tiêu chí cũ, bắt lỗi xóa rồi tiếp tục; dòng 49 tạo từng tiêu chí mới.
- BE: `ProcurementSystem.Infrastructure/Services/EvaluationService.cs:47,118,227,257`; tạo/xóa là các thao tác lưu độc lập. Interface hiện chưa có thao tác áp dụng template trong một transaction.
- Hậu quả: một request lỗi giữa chừng có thể để lại bộ tiêu chí trộn cũ/mới hoặc thiếu tiêu chí. Đây là nghiệp vụ cần transaction phía BE, không thể giải quyết bằng rollback tự chế ở trình duyệt.
- Fixture chạy chính hàm FE với API stub: xóa tiêu chí 1 thành công, xóa 2 lỗi, tạo A thành công, tạo B lỗi -> còn tiêu chí cũ 2 và tiêu chí mới A. Không có API/DB thật bị thay đổi trong fixture.
- Hướng sửa: bổ sung một command BE áp dụng/thay thế bộ tiêu chí. BE kiểm tra quyền, trạng thái, điểm đã có, tổng trọng số và quy tắc tiêu chí giá; thực hiện toàn bộ trong một transaction. FE gửi template ID + phiên bản hoặc danh sách đầu vào, rồi reload kết quả. Không báo thành công khi thao tác chưa hoàn tất.

### F02 — P1: API báo cáo lỗi nhưng FE tự dựng báo cáo nghiệp vụ

- FE: `frontend/src/components/evaluation/BidEvaluationReportModal.jsx:48,79,109,124,143,174,193`.
- Nhánh lỗi API vẫn gọi `buildFallbackReport`; tự điền MST `0101234567`, địa điểm Hà Nội, gán tổ trưởng theo thứ tự danh sách và suy diễn kết luận/kết quả xuất báo cáo.
- Hàm còn đọc `e.scores` trong khi `PackageEvaluatorDto` không có trường này; DTO có `HasSubmittedScores` nhưng đó cũng không phải bằng chứng đã chấm đủ mọi cặp hồ sơ/tiêu chí.
- BE đã có `GetEvaluationReportAsync` tại `EvaluationService.cs:1386`, DTO có `CanFinalizeExport`.
- Hậu quả: báo cáo hiển thị/xuất ra có thể chứa dữ liệu do FE đặt mặc định khi nguồn có thẩm quyền đang lỗi. Không kết luận DB bị thay đổi hay quyền BE bị vượt qua.
- Hướng sửa: khi API báo cáo lỗi, hiển thị lỗi và cho thử lại; không tự dựng báo cáo chính thức. Nếu cần bản xem trước, đánh dấu rõ dữ liệu chưa xác nhận, không tự điền thông tin tổ chức và không mở xuất bản chính thức. Dữ liệu nghiệp vụ và quyền xuất lấy từ BE. Vai trò tổ chuyên gia cần dữ liệu được lưu rõ nếu sản phẩm yêu cầu hiển thị.

### F03 — P2: công thức điểm FE không thống nhất và lệch BE

- `ScorecardForm.jsx:239,358,573`: tính `score * weight / 100`, không chuẩn hóa theo `maxScore`; tổng ở dòng 673 gắn nhãn `/100`.
- `LeaderboardView.jsx:431,450` lặp phép tính tương tự cho điểm theo tiêu chí.
- `BidComparisonView.jsx:117` lại dùng `(avgScore / maxScore) * weight`.
- BE `EvaluationService.cs:1303-1334`: chuẩn hóa theo MaxScore, tính tiêu chí giá từ dữ liệu thầu, lấy trung bình giám khảo hợp lệ và chia tổng trọng số.
- Fixture: Score=40, MaxScore=50, Weight=100 -> hàm phiếu chấm trả 40; hàm so sánh trả 80; công thức BE tương ứng 80. Đây là lỗi hiển thị đã tái hiện, không phải bằng chứng điểm DB bị tính sai.
- Hướng sửa: BE trả breakdown gồm điểm gốc/trung bình, điểm chuẩn hóa, đóng góp theo trọng số, điểm giá, tổng và trạng thái tạm tính. FE hiển thị các giá trị này. Phần nhập chưa lưu nếu cần xem trước phải ghi rõ phạm vi, dùng công thức chung được kiểm thử; không gọi tổng điểm cá nhân thiếu tiêu chí giá là điểm cuối cùng của hồ sơ.

### F04 — P2: KPI hợp đồng tổng hợp từ dữ liệu đã phân trang

- `frontend/src/pages/ContractsPage.jsx:89,133-141,236,258,269`: tải một trang rồi tự cộng giá trị, giải ngân và đếm trạng thái.
- Thẻ tổng hợp đồng dùng `totalCount` của query, còn các thẻ khác dùng `items` của trang; phạm vi số liệu không đồng nhất. Đổi trang có thể đổi KPI dù cùng bộ lọc.
- BE đã có dashboard/report aggregate; cần đối chiếu quyền và phạm vi bộ lọc trước khi tái sử dụng, không mặc định dashboard toàn hệ thống đúng cho mọi tài khoản/query.
- Hướng sửa: bổ sung aggregate cùng query filter và cùng phạm vi quyền, tính trước pagination. Trả `items`, `pagination`, `summary`, hoặc dùng endpoint summary riêng. Nếu chủ ý chỉ cộng trang hiện tại, phải đổi nhãn cho rõ.

### F05 — P2: thông báo xử lý enum số ở FE nhưng REST cấu hình enum chuỗi

- `NotificationsPage.jsx:120` và `NotificationBell.jsx:220` chỉ switch 0..4, không xử lý enum `Success=5`.
- `NotificationDto.Type` là `NotificationType`; controller JSON bật converter chuỗi. Hàm FE nhận `Warning` sẽ rơi vào nhánh thông báo chung.
- `Program.cs:88` đăng ký SignalR riêng; không thấy cấu hình `AddJsonProtocol` đồng bộ converter với controller. Do đó cần kiểm tra và thống nhất cả REST và realtime, không chỉ một transport.
- Fixture từ helper FE, bỏ riêng JSX icon để chạy Node: `1` cho nhãn Cảnh báo, `Warning` cho nhãn Thông báo.
- Hướng sửa: thống nhất biểu diễn enum tại ranh giới API/realtime, adapter normalize một lần và mapping có đủ Success cùng giá trị chưa biết. Không switch số rải rác trong component.

### F06 — P2: tab phụ lục đọc sai cấu trúc quyền Admin

- `ContractAddendumTab.jsx:37-38` kiểm tra `user.role === 'Admin'`; nút duyệt ở dòng 236 phụ thuộc biến đó.
- `AuthContext.jsx:60,71,79` dùng `user.roles`; `role` là trường của context, không phải trường của `user`. `UserInfo` trong `LoginResponse.cs` chỉ có `Roles`.
- Fixture theo DTO: `{ roles: ['Admin'] }` làm điều kiện hiện tại trả false.
- Hướng sửa: dùng `hasRole(ROLES.ADMIN)` thống nhất ngay; về lâu dài BE có thể trả quyền thao tác theo tài nguyên. BE vẫn phải kiểm tra Admin và trạng thái phụ lục khi duyệt.

### F07 — P2: FE thiếu trạng thái gói thầu Cancelled

- BE `BidPackageStatus.cs` có `Cancelled=5` và `BidPackageService.cs:398` xử lý hủy thầu.
- `frontend/src/utils/constants.js:34,42` thiếu cả constant và mapping `Cancelled/5`; `PackagesPage.jsx:29` không có tab hủy thầu.
- Fixture import constants thật xác nhận cả `PACKAGE_STATUS_CONFIG.Cancelled` và `[5]` đều undefined.
- Hướng sửa: sinh danh mục đầy đủ từ enum BE, xử lý trạng thái chưa biết rõ ràng và bổ sung bộ lọc nếu nghiệp vụ cần. Đây là ví dụ mapping tay đã lệch nguồn enum.

### F08 — P2: quy tắc phân bổ tiền ở FE khác BE

- `ContractDetailPage.jsx:156-158` coi chênh dưới 1 đồng là đã phân bổ đủ; dòng 662 khóa thêm mốc, dòng 678 hiển thị đủ 100%.
- `ContractService.cs:483-487` chỉ cho kích hoạt khi tổng decimal bằng đúng giá trị hợp đồng. Request/DB hỗ trợ số thập phân.
- Chênh 0.50 đồng được FE coi là đủ nhưng BE từ chối kích hoạt. Đây là tình huống biên số học, chưa tái hiện trên hợp đồng thật.
- Hướng sửa: thống nhất đơn vị tiền, độ chính xác và quy tắc làm tròn; BE trả `allocatedAmount`, `unallocatedAmount`, `isFullyAllocated`, `canAddMilestone` và lý do chặn. Không nhầm tiền chưa phân bổ với `TotalRemainingAmount` là tiền chưa giải ngân.

## Các chỗ nên tách trách nhiệm tiếp theo

1. `PackageDetailPage.jsx:139,225-264` tự kiểm số giám khảo lẻ/tối thiểu, tổng trọng số, deadline và trạng thái kế tiếp; BE đã kiểm tại `BidPackageService.cs:415-448`. Validation sớm có thể giữ, nhưng điều kiện/quyền thao tác nên lấy từ BE (`allowedTransitions`, `canStartEvaluation`, `blockingReasons`, thời gian server). Đây là duplication cần refactor, không phải bằng chứng BE thiếu bảo vệ.
2. `EvaluationPage.jsx:494` suy ra sẵn sàng trao thầu từ trạng thái hồ sơ; `LeaderboardView.jsx:36-65` tự chọn ứng viên, suy ra hòa điểm và fallback người đứng đầu. BE kiểm hồ sơ hợp lệ và ràng buộc khác ở `EvaluationService.cs:638-656`. FE có thể cung cấp lựa chọn người dùng, nhưng BE nên trả ứng viên đủ điều kiện, readiness và lý do. Không coi highestScore đơn thuần là quyết định trúng thầu.
3. `ContractorEvaluationModal.jsx:31-74,104` và `ContractorsPage.jsx:35` lặp quy tắc xếp hạng A/B/C/D; BE đã tính tại `ContractService.cs:2046`. Sau lưu nên dùng DTO response để báo kết quả. Preview chưa lưu được phép nhưng phải rõ là dự kiến.
4. `AddGuaranteeModal.jsx:61-76,107` tính cả Amount và Percentage rồi gửi cả hai. BE `ContractService.cs:1400` tin Percentage dương, chỉ tự tính khi không có. Cần chọn dữ liệu gốc hoặc kiểm tra nhất quán phía BE; preview tỷ lệ ở FE được giữ. `ContractGuaranteeTab.jsx:41` cũng có thể dùng tổng bảo lãnh BE đã trả thay vì lặp cách tổng hợp.
5. `CriteriaModal.jsx:52` suy ra tiêu chí giá từ tên gợi ý. Hiện request đã có `isPriceCriteria`, không phải enum ID bị dùng sai. Nên gợi ý dưới dạng object chứa flag rõ ràng/template ID, tránh quyết định nghiệp vụ bằng nhãn tiếng Việt. FE gửi tên khi tạo một tiêu chí mới là phù hợp; khi chấm điểm đã gửi `criteriaId`.

## Kiểm kê enum và chuỗi tên

| Nhóm | Hiện tại | Hướng chuẩn hóa |
|---|---|---|
| BidPackageType | FE tạo gói gửi Goods/Construction/Service; BE là enum 0/1/2 | Theo yêu cầu ID số: giá trị số được sinh từ BE; label hiển thị tách riêng |
| BidPackageStatus | FE gửi Closed/Evaluating; mở lại gửi 0; đọc cả chuỗi/số | Thống nhất 0..5, bổ sung Cancelled, một adapter/mapping |
| ContractStatus | FE gửi Active/Completed/Terminated; nhiều chỗ đọc cả chuỗi/số | Thống nhất 0..3 |
| MilestoneStatus | BE enum 0..3, FE constants chuỗi và mapping kép | Thống nhất numeric response; trạng thái do BE quyết định |
| SubmissionFileType | FE đã gửi giá trị số 0..3 trong multipart; BE nhận List<SubmissionFileType> | Giữ; thay số rải rác bằng constant sinh từ BE; text trong multipart là biểu diễn truyền tải bình thường |
| AddendumType | FE đã gửi Number(type), BE enum 0..3 | Giữ input số; đồng bộ response, mapping |
| AddendumStatus | BE enum, FE có điều kiện numeric hoặc so nhãn tiếng Việt `statusName` | Điều kiện chỉ dùng giá trị enum; `statusName` dùng hiển thị |
| GuaranteeStatus | FE gửi số 1/2/3 khi đổi trạng thái; đọc số và chuỗi | Gom constant, bổ sung mọi giá trị và kiểm tra miền giá trị phía BE |
| ApprovalStatus | BE enum; FE đọc Pending/Approved/Rejected hoặc 0/1/2 | Chuẩn hóa số, giữ nhãn riêng |
| ClarificationType/Status | BE enum; FE đọc đồng thời chuỗi/số | Chuẩn hóa cùng các enum khác |
| NotificationType | BE enum 0..5; FE helper chỉ hiểu số 0..4 | Sửa F05, đồng bộ REST/SignalR |
| BidSubmissionStatus | File nằm trong thư mục Enums nhưng là static class const string | Chưa có ID enum số. Muốn đổi số cần thiết kế enum, migration DB, DTO và tương thích dữ liệu cũ |
| VerificationStatus, trạng thái đề xuất giám khảo | Entity/filter BE hiện dùng string | Không tự thay bằng ApprovalStatus chỉ vì tên trạng thái giống nhau; cần quyết định domain và migration |
| Roles | Bảng Role có Id; AssignRolesRequest hiện nhận List<string>, JWT/Authorize dùng role code | Gửi tên role hiện đúng DTO. Có thể đổi riêng command sang roleIds qua BE + FE; giữ claims/Authorize theo role code, không đổi toàn bộ auth sang số |
| TerminationReasonCategory, BankName | Request hiện là string, không có enum/lookup ID tương ứng trong Core enums | Muốn dùng ID phải tạo danh mục/enum trước; không tự bịa ID phía FE |

Entity IDs như `packageId`, `submissionId`, `contractorId`, `evaluatorId`, `criteriaId` khác với giá trị enum. Trong các luồng kiểm tra, phân công giám khảo và chấm điểm đã gửi ID entity; không có căn cứ kết luận FE đang gửi tên nhà thầu thay ContractorId ở các command đó.

## Kế hoạch xử lý đề xuất

1. Sửa F01/F02 trước: transaction áp dụng template và báo cáo lấy dữ liệu có thẩm quyền. Không phụ thuộc việc đổi enum.
2. Sửa điểm/KPI/quyền/notification/Cancelled và precision; xác định rõ số liệu nào chính thức, số nào dự kiến.
3. Chốt contract enum số theo yêu cầu: ví dụ `type: 0`, `newStatus: 1`; response `status: 2`, `statusName: "Đang chấm điểm"`. Nhãn không được dùng làm điều kiện hay payload. Giá trị enum số khác ID bản ghi DB.
4. Dùng Core enums làm nguồn danh mục; sinh constants/types/client hoặc bổ sung endpoint metadata có version (đây là đề xuất, chưa có endpoint này). Không duy trì mapping nghiệp vụ bằng tay ở cả hai phía.
5. Rà soát DTO có string tường minh: `EvaluationSummaryDto.PackageStatus`, `BidEvaluationReportDto.PackageStatus`, `RecentContractItemDto.Status`... Converter không tự chuyển chúng thành số. Cần thay DTO/field có kế hoạch.
6. Không xóa converter global ngay. Chuyển theo từng domain/phiên bản, normalize một lần tại API adapter, cập nhật cả query string, JSON body, response và SignalR; hỗ trợ client cũ trong khoảng chuyển tiếp nếu cần.
7. Sửa trường hợp enum 0 trước migration: `PackagesPage.jsx:85,87` dùng if(value) sẽ bỏ lọc Open/Goods nếu đổi state thành số; `PackageDetailPage.jsx:228` dùng `pkg.status || ''` làm mất Open=0. Dùng kiểm tra null/undefined/chuỗi rỗng và `??` đúng chỗ.
8. Validation BE phải kiểm enum thuộc miền hợp lệ, không chỉ `[Required]`. Riêng UpdateGuaranteeStatusAsync hiện gán trực tiếp request.Status; cần kiểm giá trị và chuyển trạng thái theo quy tắc đã thống nhất. Không coi serialize được một số là số đó hợp lệ.

## Acceptance criteria và kiểm thử khi triển khai

- Áp dụng template thất bại ở bất kỳ bước nào phải giữ nguyên toàn bộ bộ tiêu chí cũ; kiểm tra quyền, trạng thái, có điểm, và concurrent update.
- Báo cáo API lỗi/403/timeout không tạo thông tin tổ chức/kết luận mới và không mở xuất chính thức. Completion lấy từ BE.
- Điểm nhất quán với MaxScore 50/100/1000, tổng weight hợp lệ, tiêu chí giá, nhiều giám khảo, thay giám khảo và làm tròn. Preview cá nhân không bị nhầm với điểm cuối hồ sơ.
- KPI không đổi do chuyển trang khi bộ lọc/phạm vi quyền giữ nguyên.
- Contract tests mọi enum ở query/body/response/multipart/realtime; giá trị 0, Cancelled=5, Notification Success=5, giá trị lạ 999 và trường bắt buộc bị thiếu.
- Admin thấy đúng thao tác duyệt phụ lục, tài khoản khác không được BE cho duyệt; không dựa vào UI để bảo vệ quyền.
- Tiền phân bổ, tiền giải ngân, Amount/Percentage và quy tắc làm tròn nhất quán ở BE/FE.
- Bảo toàn hành vi sealed bid, ownership, phân công giám khảo và maker-checker khi refactor.

## Bằng chứng thực thi và giới hạn

- Đã đọc source trên baseline nêu trên; chạy Node fixtures gọi hàm lấy từ source cho công thức phiếu chấm, công thức so sánh, fallback báo cáo và áp dụng template. Template sử dụng stub trong bộ nhớ, không gọi API thật.
- Import constants thật xác nhận thiếu Cancelled. Fixture theo DTO xác nhận điều kiện user.role; helper thông báo chạy sau khi bỏ JSX icon; số học precision/KPI là minh họa, không phải E2E dữ liệu thật.
- Chạy serializer fixture bằng PowerShell host .NET 10.0.11 với enum BidPackageType lấy từ source và cùng loại JsonStringEnumConverter: input 0 hoặc Goods đều output Goods; input 999 được deserialize nhưng IsDefined=false. Đây là kiểm tra converter độc lập, không phải test runtime net8.0 đang phục vụ API.
- Đã thử GET `/swagger/v1/swagger.json` trên API localhost:5225: server trả lỗi hệ thống với payload statusCode 400; không lấy được schema live. Không coi bảng schema rỗng của lệnh thử là schema thực tế. Contract trong báo cáo được đối chiếu từ source, không tuyên bố đã xác nhận mọi payload live.
- Không chạy build/lint/full suite hoặc browser E2E cho đợt rà soát này. Không gửi mutation request, không sửa source implementation, không đổi DB. Chỉ bổ sung file báo cáo này.

Kết luận: cần sửa trước các luồng ghi nhiều bước/báo cáo và đồng bộ kết quả nghiệp vụ. Numeric enum là thay đổi contract có chủ đích; không nên thực hiện bằng thay chuỗi hàng loạt tại FE.
