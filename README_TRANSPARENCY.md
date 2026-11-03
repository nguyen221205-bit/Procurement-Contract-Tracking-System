# Procurement Transparency Controls

Tài liệu này mô tả cách hệ thống giới hạn quyền theo danh tính và đơn vị mời thầu, cùng quy trình kiểm tra từ mở thầu đến hoàn tất nghiệm thu.

## 1. Mô hình minh bạch

### Danh tính và thành viên đơn vị

- Mỗi người dùng có tài khoản riêng; không dùng chung mật khẩu, OTP hoặc token.
- Người tạo đơn vị là Manager của đơn vị đó. Manager có thể mời người khác bằng email riêng; người được mời đăng nhập hoặc đăng ký bằng đúng email rồi chấp nhận lời mời.
- Các vai trò thành viên gồm Manager, Officer, Finance và Signer. Quyền nghiệp vụ yêu cầu membership còn Active tại đúng Procuring Entity.
- Thẩm quyền ký được quản lý riêng với membership. Vai trò Signer không tự chứng minh người đó có quyền ký thay pháp nhân.
- Admin quản trị nền tảng và xử lý dữ liệu ownership cần đối soát; Admin không thay người có thẩm quyền của đơn vị để phê duyệt chi.

### Quyền sở hữu gói thầu

- Gói thầu được gắn với ProcuringEntityId khi tạo. Quyền sở hữu không đổi theo membership hiện tại của người tạo.
- Giao diện lấy danh sách đơn vị được phép tạo từ GET /api/bid-packages/creatable-entities. API kiểm tra lại entity được gửi lên; không có entity phù hợp thì từ chối.
- Dữ liệu cũ thiếu căn cứ xác định đơn vị được đánh dấu OwnershipNeedsReview. Giá trị ứng viên được giữ để điều tra, không được xem là kết luận.
- Admin có thể xem danh sách qua GET /api/bid-packages/ownership-review và ghi nhận quyết định qua POST /api/bid-packages/{id}/ownership-review. Request yêu cầu mã tham chiếu chứng cứ và ghi chú; API lưu audit nhưng không tự xác minh nội dung hay tính pháp lý của chứng cứ.

### Maker–Checker và nghiệm thu

- Officer/Maker thực hiện kiểm tra kỹ thuật milestone.
- Một Checker khác, có membership đang hoạt động và signing authority hợp lệ, mới được phê duyệt nghiệm thu. Maker không tự phê duyệt kết quả của chính mình.
- Quyết định duyệt gắn với AcceptanceId, ExpectedRevision và ExpectedVersion. Khi nội dung thay đổi, phiên bản cũ không còn dùng để phê duyệt.
- Hash SHA-256 được tạo từ bytes thực tế của tệp. Tải tài liệu nghiệm thu đi qua endpoint có xác thực và kiểm tra quyền.
- Các thao tác nhạy cảm dùng transaction/concurrency checks để phát hiện cập nhật đồng thời, bao gồm tình huống membership bị thu hồi.

## 2. Kiểm thử tự động

Chạy từ thư mục gốc của repository.

### Backend

~~~powershell
dotnet test tests/ProcurementSystem.Tests/ProcurementSystem.Tests.csproj
~~~

Bộ test gồm kiểm thử nghiệp vụ và authorization, test migration/concurrency trên SQL Server LocalDB, cùng một HTTP integration test qua Kestrel, JWT và HttpClient. Nhóm SQL Server yêu cầu LocalDB; nếu LocalDB không khả dụng, test báo lỗi thay vì tự bỏ qua hoặc báo PASS.

### Frontend

~~~powershell
npm install --prefix frontend
npm test --prefix frontend
npm run build --prefix frontend
~~~

Các test frontend dùng Node test runner và kiểm tra một số logic enum/điểm xem trước. Chúng không thay thế kiểm thử giao diện trên trình duyệt.

### Giới hạn của script E2E cũ

Script scripts/testing/test_e2e_full_flow.ps1 được viết cho dữ liệu mẫu và luồng Admin cũ. Script không gửi ProcuringEntityId, không tạo membership/invitation và không kiểm tra Maker–Checker; kịch bản cũng dừng sau khi tạo milestone, chưa chạy nghiệm thu. Vì vậy không dùng script này làm bằng chứng cho transparency hiện tại hoặc chạy nó trên database gốc.

## 3. Hướng dẫn E2E: mở thầu đến nghiệm thu

Đây là checklist chạy thủ công qua giao diện và Swagger. Các bước tạo dữ liệu thật trong database đang cấu hình. Hãy dùng database thử nghiệm riêng.

### Chuẩn bị môi trường an toàn

1. Tạo database mới trên SQL Server/LocalDB dành riêng cho E2E.
2. Cấu hình ứng dụng trỏ đến database thử nghiệm trước khi khởi động API. API có thể chạy migration lúc khởi động; không khởi động với connection string trỏ vào database gốc.
3. Chạy API:

~~~powershell
dotnet run --project ProcurementSystem.API
~~~

4. Mở Swagger tại http://localhost:5225/swagger. Chạy frontend trong terminal khác:

~~~powershell
npm install --prefix frontend
npm run dev --prefix frontend
~~~

5. Chuẩn bị một entity đã được duyệt, tài khoản Manager, Officer/Maker, Checker riêng có signing authority đã xác minh, tài khoản Evaluator theo cấu hình Tổ chuyên gia và ít nhất hai nhà thầu. Mỗi người dùng một email/tài khoản riêng.

### A. Đăng ký thành viên và tạo gói thầu

1. Manager mời Officer bằng email riêng. Officer chấp nhận lời mời bằng chính tài khoản/email đó.
2. Đăng nhập bằng tài khoản tạo gói. Gọi GET /api/bid-packages/creatable-entities và chọn đúng entity.
3. Tạo gói qua giao diện hoặc POST /api/bid-packages. Kiểm tra các ca âm:
   - Không gửi ProcuringEntityId: request bị từ chối.
   - Gửi ID của entity người tạo không là thành viên: request bị từ chối.
   - Chọn entity được trả về và membership còn Active: gói được gắn với đúng ProcuringEntityId.
4. Thiết lập tiêu chí và thời hạn. Xác nhận trạng thái gói bắt đầu ở Open.

### B. Nhận hồ sơ, đánh giá và trao thầu

1. Mỗi nhà thầu đăng nhập bằng tài khoản riêng và nộp hồ sơ vào gói đang mở.
2. Đóng thầu, chuyển gói sang Evaluating và phân công hội đồng theo cấu hình.
3. Mỗi evaluator chấm điểm bằng danh tính riêng. Hoàn tất đánh giá, xem thứ hạng và ghi nhận quyết định trao thầu.
4. Xác nhận hồ sơ trúng thầu và entity sở hữu gói không thay đổi trong suốt quá trình.

### C. Hợp đồng và nghiệm thu

1. Tạo hợp đồng từ kết quả trúng thầu; tạo milestone theo điều khoản hợp đồng, tải bản scan và hoàn tất điều kiện để hợp đồng có hiệu lực.
2. Officer/Maker gửi kiểm tra kỹ thuật:

~~~http
PUT /api/contracts/milestones/{milestoneId}/technical-review
Content-Type: application/json

{"isPassed": true, "note": "Đã kiểm tra kết quả tại hiện trường"}
~~~

3. Lấy AcceptanceId, ExpectedRevision và ExpectedVersion từ kết quả kiểm tra kỹ thuật. Đăng nhập bằng Checker khác và gửi phê duyệt:

~~~http
PUT /api/contracts/milestones/{milestoneId}/acceptance
Content-Type: application/json

{
  "isApproved": true,
  "note": "Đã đối chiếu hồ sơ nghiệm thu",
  "acceptanceId": 12,
  "expectedRevision": 1,
  "expectedVersion": 3
}
~~~

Thay các số minh họa bằng giá trị thực của lần chạy. Lặp lại cho từng milestone theo quy trình, rồi xác nhận hợp đồng chuyển trạng thái hoàn tất theo điều kiện nghiệp vụ.

### D. Kiểm tra phân quyền và dấu vết

- Cho Maker thử phê duyệt nghiệm thu do chính mình lập: hệ thống phải từ chối.
- Thu hồi membership của một thành viên rồi thử thao tác ghi/duyệt: hệ thống phải từ chối.
- Gửi ExpectedRevision hoặc ExpectedVersion đã cũ sau khi cập nhật milestone: hệ thống phải từ chối vì xung đột phiên bản.
- Thử tải tài liệu nghiệm thu khi chưa đăng nhập, bằng người ngoài entity và bằng người có quyền. Chỉ người đủ quyền mới tải được tài liệu.
- Kiểm tra audit log có actor và sự kiện tương ứng với thay đổi membership, nghiệm thu và ownership review.

Ghi lại commit SHA, database thử nghiệm, actor từng bước, request/response và kết quả thực tế. Checklist này là hướng dẫn manual; chỉ kết luận browser E2E PASS sau khi đã thực sự chạy toàn bộ luồng trên trình duyệt.

## 4. Kỹ thuật chính

| Kỹ thuật | Mục đích |
|---|---|
| ASP.NET Core Authorization và JWT | Xác định actor ở mỗi request; dịch vụ kiểm tra lại quyền ở phía máy chủ. |
| Membership theo Procuring Entity | Phân quyền theo đơn vị cụ thể và trạng thái membership hiện hành. |
| Entity ownership cố định | Lưu entity trên gói thầu, không suy quyền từ người tạo đang thuộc đơn vị nào. |
| Maker–Checker và signing authority riêng | Tách người lập, người kiểm tra và thẩm quyền ký; Admin nền tảng không thay Checker. |
| EF Core transaction và concurrency token | Bảo vệ cập nhật nhạy cảm khi có thay đổi đồng thời hoặc thu hồi quyền. |
| Revision-bound acceptance | Bắt buộc duyệt đúng acceptance, revision và phiên bản milestone đã kiểm tra. |
| SHA-256 trên bytes của tệp | Đối chiếu tính toàn vẹn tài liệu nghiệm thu được lưu. |
| Migration quarantine và audit trail | Đưa ownership lịch sử thiếu căn cứ vào hàng đợi đối soát và ghi lại quyết định xử lý. |
| SQL Server LocalDB, Kestrel/JWT/HttpClient | Kiểm tra migration, concurrency và biên xác thực HTTP bằng runtime thật. |
