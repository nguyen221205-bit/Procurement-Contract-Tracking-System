# Handoff Agent B sửa các phát hiện sau review OTP và storage

Ngày lập: 06/11/2026.

Repo: `D:\Thuc_Tap_backend\Procurement & Contract Tracking System`.

## 1. Snapshot và phạm vi

Snapshot đã review của Agent B: `origin/feat/agent-b-security-otp-storage`, commit `43db1e360c1ed0fad5d440cc6868a7c81b8c8e8a`.

Tại thời điểm viết handoff, checkout chính đã chuyển tới `dev` ở `bf2f10c` (phần notification và bộ E2E được commit sau lượt review). Vì vậy Agent phải kiểm tra lại branch/HEAD/status, không giả định `dev` đã chứa các sửa OTP/storage của B. Không checkout nhánh làm mất thay đổi người khác. Tiếp tục trên nhánh/worktree của B; việc tích hợp vào dev cần được giao riêng.

Mục tiêu lần này: sửa ba phát hiện storage R-B01–R-B03 và bổ sung bằng chứng OTP R-B04. Giữ các kiểm soát OTP bắt buộc và loại bỏ khóa mặc định đã thực hiện. Không nhận lại nhiệm vụ worker hoặc xây lại toàn bộ lifecycle E2E.

Đây là giao triển khai sửa lỗi và kiểm chứng. Không tự commit/push/merge/deploy, đổi khóa thật, chuyển đổi file cloud thật, chạy migration CSDL gốc hoặc restart API gốc. Không gửi SMTP thật trong kiểm thử. Nếu nhánh B đang có thay đổi ngoài handoff, phải giữ nguyên và ghi rõ trong báo cáo.

## 2. Bản đồ file được phép sửa

| Mã | Mức độ | File/hàm chính | Phạm vi liên quan được phép |
|---|---|---|---|
| R-B01 | P1 | `ProcurementSystem.Infrastructure/Services/FileCryptoCompressionService.cs`: constructor khoảng dòng 25–65 | `HybridFileStorageService.cs`, `SupabaseStorageService.cs` constructor, `ProcurementSystem.API/Program.cs`, `StorageSettings.cs`: chỉ khi cần sửa thời điểm validate và DI theo provider |
| R-B02 | P2 | `SupabaseStorageService.cs`: `ResolveDownloadAsync`, khoảng dòng 224–272 | Đường upload/metadata và `FileCryptoCompressionService.RestoreFromStorageAsync` khi cần nhận diện file; helper riêng để kiểm thử S3 |
| R-B03 | P2 | `FileCryptoCompressionService.cs`: `RestoreFromStorageAsync`, nhánh chiều dài dưới 20 bytes khoảng dòng 138–148 | Các test crypto/file hỏng và test đường download; không thay thuật toán/format mã hóa đại trà |
| R-B04 | Bằng chứng còn thiếu | `tests/ProcurementSystem.Tests/RegistrationOtpSecurityTests.cs`, test `SendOtpResponse_DevOtpCode_MustNotExposeCodeByDefault` khoảng dòng 355 | Test HTTP mới, test OTP thật với fake email, UI OTP trên môi trường test; DTO/auth service/RegisterPage chỉ sửa nếu phát hiện lỗi trong kiểm thử này |

Test được sửa/thêm: `FileCryptoCompressionTests.cs`, `HybridStorageAndAdminTests.cs`, `RegistrationOtpSecurityTests.cs`, file riêng cho HTTP/DI/storage boundary tests nếu cần. Tránh sửa `TestFakes.cs` chung; dùng fake riêng.

Tài liệu được cập nhật: `HUONG_DAN_CAU_HINH_SUPABASE_STORAGE.md` và kết quả mới `docs/RESULT_AGENT_B_REVIEW_FIXES_2026-11-06.md`. Nếu thực thi ngày khác, ghi ngày thực tế trong báo cáo. Chỉ đổi cấu hình mẫu không chứa secret khi cần.

Ngoài phạm vi: worker/NotificationRecipientResolver/test của A; bộ runner lifecycle E2E; luật chấm thầu, membership, ký duyệt; ERD và các báo cáo cũ. Không thay toàn bộ schema lưu trữ hoặc triển khai key rotation chỉ để xử lý các phát hiện này.

## 3. R-B01 Local Storage bị bắt buộc có khóa mã hóa

### Bằng chứng review

`StorageEncryptionConfig.Enabled` mặc định true, trong khi Provider mặc định Local. Constructor crypto kiểm tra Enabled nhưng không xét việc có dùng mã hóa hay không. `HybridFileStorageService` nhận trực tiếp `SupabaseStorageService`, service này nhận crypto qua DI, nên dù chỉ dùng Local vẫn có thể kích hoạt constructor ném lỗi.

Probe đã chạy trên snapshot B:

```csharp
new FileCryptoCompressionService(
    Options.Create(new StorageSettings { Provider = "Local" }),
    NullLogger<FileCryptoCompressionService>.Instance);
```

Kết quả: `InvalidOperationException` vì thiếu MasterKey. Đây là lỗi hồi quy về khả năng sử dụng Local, không phải yêu cầu cung cấp khóa cho cloud.

### Hành vi cần đạt

- Provider Local thuần, không có khóa và không có dữ liệu cần giải mã: resolve Hybrid Storage, upload/download Local bình thường.
- Provider Supabase bật mã hóa: khóa thiếu/sai Base64/sai độ dài bị từ chối trước khi ghi. Giữ startup validation phù hợp với cấu hình thực sự dùng.
- Đọc một file mã hóa cũ vẫn cần khóa hợp lệ, kể cả provider hiện tại Local. Không dùng Provider Local như một bypass cho việc giải mã file cloud lịch sử.
- Không tái tạo khóa mặc định, dẫn xuất seed tùy ý hoặc sinh khóa mới mỗi startup.
- Không sửa bằng cách tắt mã hóa mặc định ở mọi môi trường, hoặc yêu cầu tất cả người dùng Local cung cấp khóa giả.
- Chọn cách validate/lazy resolution nhất quán giữa constructor, DI, startup và thao tác I/O. Nếu khóa được cấu hình sai nhưng chưa cần dùng, mô tả policy rõ; khi thực sự cần mã hóa/giải mã phải từ chối, không fallback che lỗi.

### Kiểm thử bắt buộc

Test qua DI với chính các đăng ký storage của ứng dụng, không chỉ new crypto đơn lẻ: Local không có StorageSettings hoặc chỉ Provider Local; Local có cấu hình mẫu không dùng cloud; upload và tải lại bytes. Thêm ma trận Supabase bật/tắt mã hóa, khóa thiếu/sai/hợp lệ; khóa thiếu khi đọc file PSEN phải bị chặn.

Không chạy full API trỏ DB gốc để chứng minh DI. Dựng host/service provider test cô lập, fake S3 và không chạy worker thật.

## 4. R-B02 Cờ mã hóa file mới đang quyết định sai cách đọc file cũ

### Bằng chứng review

`ResolveDownloadAsync` chỉ fetch/giải mã khi `_settings.Encryption.Enabled == true`. Khi false, hàm luôn trả pre-signed URL, không biết object đang là plaintext hay ciphertext PSEN. Sau khi tắt mã hóa hoặc rollback provider, file đã mã hóa trước đó sẽ được tải ở dạng ciphertext, không phải tài liệu gốc.

Probe với Enabled=false, endpoint S3 fixture và `supabase://old-encrypted.pdf` đã trả `IsRedirect=true` mà không đọc/nhận diện nội dung. Probe này không truy cập cloud thật; nó chứng minh đường code bỏ qua nhận diện, không phải bằng chứng tải một object thật từ Supabase.

### Hành vi cần đạt

- `Encryption.Enabled` điều khiển việc ghi file mới, không tự kết luận định dạng của mọi file đã tồn tại.
- Nhận diện định dạng từng object bằng metadata tin cậy và/hoặc header. File cũ chưa có metadata vẫn phải được xử lý; không mặc định chúng là plaintext vì thiếu metadata.
- File PSEN + khóa đúng → trả plaintext gốc bất kể cờ ghi file mới hay provider hiện tại.
- File PSEN + thiếu/sai khóa hoặc nội dung hỏng → lỗi rõ; không redirect ciphertext, không trả bytes mã hóa giả thành công, không fallback Local để che lỗi.
- File legacy plaintext → tải đúng bytes với policy hiện có. Nếu dùng redirect, phải đủ căn cứ object đó là plaintext.
- Không dựa vào đuôi `.pdf`/ContentType hoặc tên đường dẫn để xác định mã hóa. Giữ các kiểm tra xác thực/phân quyền ở endpoint phía trên; không biến URL signed thành URL public.
- Không buộc chạy migration toàn bộ cloud để đọc các file cũ. Không đổi format PSEN hiện tại trong lần sửa này nếu không thật sự cần; tương thích phải được chứng minh bằng fixture tạo từ code cũ/khóa test hợp lệ.

### Kiểm thử bắt buộc

Test đường `SupabaseStorageService.ResolveDownloadAsync` với S3 giả có object thật trong bộ nhớ hoặc local S3 test cô lập:

1. Object đã mã hóa khi Enabled=true; chuyển Enabled=false, khóa giữ nguyên → tải về khớp SHA-256 plaintext.
2. Đổi provider sang Local nhưng đường dẫn vẫn supabase:// → đọc object mã hóa cũ đúng bytes khi có khóa.
3. Thiếu khóa/sai khóa/file hỏng → lỗi, không có redirect hoặc thành công ciphertext.
4. Object plaintext cũ, không metadata → tải đúng bytes.
5. Object mã hóa cũ, không metadata → nhận diện PSEN và xử lý đúng.

Fake phải trả object bytes qua đường đọc thực; không chỉ assert giá trị flag hoặc tự dựng FileDownloadResult mong muốn. Không log signed URL/secret trong kết quả công khai.

## 5. R-B03 PSEN bị cắt ngắn bị nhận nhầm là plaintext

### Bằng chứng review

Nhánh `memorySource.Length < EncryptedHeader.Length + 16` trả dữ liệu nguyên bản trước khi kiểm tra header. Probe đầu vào `{0x50,0x53,0x45,0x4E,1,2}` (PSEN + 2 bytes) trả stream 6 bytes thành công.

Đây là nhánh còn sót từ xử lý legacy, không nên ghi nhận là hồi quy mới do commit B. Tuy nhiên, nó chưa đạt yêu cầu từ chối file mã hóa hỏng của handoff ban đầu.

### Hành vi cần đạt

- Đọc/kiểm tra magic header trước khi quyết định passthrough. Đã có đầy đủ magic PSEN thì thiếu IV/payload hoặc cấu trúc cipher không hợp lệ phải bị từ chối.
- Giữ passthrough cho plaintext ngắn không có magic PSEN; không chặn tất cả file nhỏ để sửa lỗi này.
- Bao phủ file đủ header/IV nhưng thiếu ciphertext hoặc ciphertext bị cắt. Không chỉ kiểm tra tổng chiều dài tối thiểu mà bỏ qua lỗi giải mã/giải nén.
- Lỗi định dạng phải truyền tới download như lỗi, không rơi vào generic catch rồi redirect. Rà tương tác với R-B02; chọn exception contract nhất quán.
- Bảo toàn việc dispose stream và giới hạn dữ liệu hiện có; không thêm read bytes không giới hạn ngoài đường đang cần xử lý.

Test tối thiểu: plaintext ngắn/empty theo contract; PSEN chỉ 4 bytes; PSEN + IV thiếu; PSEN + IV đầy đủ nhưng không payload; file mã hóa thật bị truncate; sai khóa; round-trip hợp lệ; fixture PSEN cũ. Với mọi ca file PSEN lỗi, assert không trả stream/redirect thành công.

## 6. R-B04 Bổ sung kiểm chứng OTP thật ở boundary

Phần OTP bắt buộc ở service/DTO được giữ lại. Review chưa phát hiện bypass trong hai đường đăng ký đã xem; mục này khắc phục khoảng trống bằng chứng, không yêu cầu viết lại toàn bộ auth.

Test hiện tại `SendOtpResponse_DevOtpCode_MustNotExposeCodeByDefault` tự dựng DTO với `DevOtpCode=null` rồi assert null, nên không chứng minh được hành vi OtpService hoặc response endpoint.

Yêu cầu:

- Thay/bổ sung test gọi OtpService thật với MemoryCache thật và fake email ghi nhận thư. Kiểm tra response không trả OTP, nhưng thư test có mã có thể dùng cho đúng email/purpose. Không ghi mã ra báo cáo.
- Thêm HTTP integration multipart cho cả đăng ký Contractor và ProcuringEntity: thiếu/blank OTP, sai OTP và mã hợp lệ. Kiểm tra HTTP/body và không tạo User/entity/member/file thừa khi bị từ chối. Giữ phân biệt role/member/signing authority.
- Bao phủ email/purpose khác, expired/reused và giới hạn thử sai bằng service thật hoặc clock/cache có thể kiểm soát. Không chỉ dùng fake OTP luôn trả true/false rồi gọi đó là kiểm tra expired/replay.
- Có test endpoint gửi OTP chạy cấu hình Production cô lập: response không chứa mã. Không kết nối SMTP thật; không khởi động Program thật với cấu hình/secrets máy đang chạy.
- Thực hiện UI smoke cả hai form: gửi mã qua mailbox test → nhập mã → đăng ký; sửa email phải xóa trạng thái/mã cũ; nhập thiếu/sai mã hiển thị lỗi hợp lý. Kiểm tra endpoint trực tiếp thiếu OTP vẫn bị từ chối dù UI bị bỏ qua.
- Nếu UI/HTTP chưa chạy được, ghi NOT RUN/BLOCKED và nguyên nhân. Frontend 6 test enum/score và build xanh không được ghi thành test UI OTP PASS.
- Ghi rõ OTP được tiêu thụ ở bước nào và người dùng phải xin lại mã khi đăng ký thất bại sau xác minh. Không mở replay vô hạn để cải thiện trải nghiệm.

## 7. Kiểm chứng và cách báo cáo

Review đã thực thi trên các snapshot tách biệt: phần A trên working tree cũ `88/88`, phần B tại `43db1e3` `93/93`, frontend B `6/6`. Đây là baseline trước sửa; không phải kết quả đã tích hợp trên dev mới, cũng không phải browser E2E. Probe độc lập vẫn phát hiện ba vấn đề trên dù suite xanh.

Agent chạy các test tập trung trước, sau đó full suite/build trên snapshot sửa cuối:

```powershell
dotnet test tests/ProcurementSystem.Tests/ProcurementSystem.Tests.csproj --verbosity minimal
dotnet build ProcurementSystem.API/ProcurementSystem.API.csproj --no-restore --verbosity minimal
npm test --prefix frontend
npm run build --prefix frontend
git diff --check
```

Nhóm SQL integration cần LocalDB; thiếu môi trường phải báo rõ, không return sớm thành PASS. Không chạy đồng thời build/test .NET trên cùng bin/obj với Agent khác.

Báo cáo mới gồm:

1. Branch/HEAD thực tế, danh sách file/hàm sửa, khác biệt so với 43db1e3; nêu phần nào thuộc các commit nền khác.
2. Bảng R-B01–R-B04 với repro trước/sau, expected/actual, commands/log thật và lớp bằng chứng service/DI/HTTP/browser.
3. Ma trận provider × cấu hình mã hóa file mới × loại file đang lưu × tình trạng khóa; chỉ rõ kết quả đọc/ghi/lỗi ở từng tổ hợp.
4. Hướng dẫn vận hành và tương thích file cũ, xử lý trường hợp từng dùng khóa fallback, backup trước khi đổi khóa; không tự chuyển đổi dữ liệu thật.
5. Phần chưa chạy, blocker còn lại, liên hệ tích hợp với dev có Agent A/E2E; không tuyên bố PASS tổng thể chỉ dựa trên test nhánh B.

## 8. Tiêu chí hoàn thành và prompt giao Agent

Đạt khi ba repro storage không còn trả kết quả sai, Local thuần không phụ thuộc khóa cloud, file PSEN cũ vẫn đọc đúng khi tắt mã hóa file mới, file hỏng bị từ chối; có kiểm thử DI/download boundary/HTTP OTP và kết quả UI OTP trung thực. Giữ OTP bắt buộc, không lộ mã/secret, không đưa fallback khóa trở lại.

Prompt:

> Đọc HANDOFF_AGENT_B_REVIEW_FIXES_2026-11-06.md và sửa R-B01–R-B03 tại các file/hàm được chỉ định; bổ sung bằng chứng OTP R-B04. Tiếp tục trên snapshot/nhánh Agent B, kiểm tra dev mới để tránh ghi đè Agent A/E2E. Giữ mã hóa và OTP an toàn, bảo toàn khả năng đọc file cũ; không đổi khóa thật hoặc chạy migration dữ liệu thật. Chạy test phù hợp và bàn giao RESULT_AGENT_B_REVIEW_FIXES với repro/log thật; không commit/push/merge/deploy hoặc restart API gốc khi chưa được giao riêng.
