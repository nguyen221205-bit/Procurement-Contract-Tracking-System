# HƯỚNG DẪN CẤU HÌNH & KÍCH HOẠT SUPABASE CLOUD STORAGE (S3-COMPATIBLE)

Tài liệu này hướng dẫn cách kết nối hệ sinh thái lưu trữ đám mây miễn phí **Supabase Storage** cho **Hệ thống Quản lý Đấu thầu & Theo dõi Hợp đồng**. 

Được thiết kế theo chuẩn **Zero-Risk**: Bạn có thể kích hoạt, trải nghiệm và hoàn tác (Rollback) về máy cục bộ bất cứ lúc nào chỉ bằng 1 thao tác mà không sợ mất mát dữ liệu.

---

## BƯỚC 1: TẠO TÀI KHOẢN VÀ PROJECT SUPABASE (MIỄN PHÍ)
1. Truy cập [https://supabase.com](https://supabase.com) và chọn **Start your project**.
2. Đăng nhập trực tiếp bằng tài khoản **GitHub** hoặc **Google** *(hoàn toàn KHÔNG cần nhập thẻ tín dụng hay thông tin ngân hàng)*.
3. Bấm **New Project**:
   - **Name**: `procurement-system` (hoặc tên bất kỳ bạn thích).
   - **Database Password**: Đặt mật khẩu tùy ý (ví dụ: `Procurement2026!`).
   - **Region**: Chọn `Southeast Asia (Singapore)` để có tốc độ truyền tải tối ưu nhất tại Việt Nam.
   - Bấm **Create new project** và đợi khoảng 1-2 phút để Supabase khởi tạo.

---

## BƯỚC 2: TẠO PRIVATE STORAGE BUCKET
1. Trên thanh menu bên trái của Dashboard Supabase, bấm vào biểu tượng **Storage** (hình chiếc xô / bucket).
2. Bấm nút **New bucket**:
   - **Bucket name**: Nhập chính xác `procurement-documents`.
   - **Public bucket**: **TẮT** (Để chế độ Riêng tư / Private để bảo vệ tính bảo mật hồ sơ thầu Sealed-Bid).
3. Bấm **Save**.

---

## BƯỚC 3: LẤY THÔNG TIN S3 ACCESS KEYS
Supabase Storage hỗ trợ giao thức S3 Protocol tiêu chuẩn quốc tế:
1. Ở góc dưới cùng bên trái màn hình, bấm vào biểu tượng bánh răng **Project Settings**.
2. Chọn mục **Storage** trong danh mục bên trái.
3. Cuộn xuống phần **S3 Access Keys**:
   - Bấm **New Access Key**.
   - Nhập mô tả (Description), ví dụ: `Procurement Backend Key`.
   - Bấm **Create Access Key**.
4. Màn hình sẽ hiển thị 2 thông số:
   - **Access Key ID**: Sao chép chuỗi này.
   - **Secret Access Key**: Sao chép chuỗi bí mật này (lưu ý chỉ hiển thị 1 lần duy nhất).
5. Lấy **Project Reference ID**:
   - Nằm ngay trong URL trang Dashboard của bạn: `https://supabase.com/dashboard/project/<PROJECT_REF>`.
   - Hoặc xem tại mục *Project Settings -> General -> Reference ID*.
   - Endpoint S3 sẽ có dạng: `https://<PROJECT_REF>.storage.supabase.co/storage/v1/s3`.

---

## BƯỚC 4: SINH KHÓA MÃ HÓA MASTERKEY (AES-256) VÀ ĐIỀN CẤU HÌNH

### 1. Sinh khóa mã hóa 256-bit (32 bytes Base64) ngẫu nhiên an toàn:
Khóa mã hóa bắt buộc phải là chuỗi Base64 giải mã ra đúng 32 bytes (256 bits). Bạn có thể sinh khóa bằng một trong các lệnh sau:

- **PowerShell (Windows)**:
  ```powershell
  $bytes = New-Object byte[] 32
  [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
  [Convert]::ToBase64String($bytes)
  ```
- **Linux / macOS Terminal (OpenSSL)**:
  ```bash
  openssl rand -base64 32
  ```

### 2. Điền cấu hình vào `appsettings.json` hoặc User Secrets:
Mở tệp `ProcurementSystem.API/appsettings.json` và cập nhật block `StorageSettings`:

```json
  "StorageSettings": {
    "Provider": "Supabase", // "Local" hoặc "Supabase"
    "Supabase": {
      "Endpoint": "https://<PROJECT_REF>.storage.supabase.co/storage/v1/s3",
      "BucketName": "procurement-documents",
      "AccessKeyId": "<ACCESS_KEY_ID_VỪA_LẤY>",
      "SecretAccessKey": "<SECRET_ACCESS_KEY_VỪA_LẤY>",
      "Region": "ap-southeast-1"
    },
    "Encryption": {
      "Enabled": true,
      "MasterKey": "<CHUỖI_BASE64_32_BYTES_VỪA_SINH>"
    }
  }
```

> 🔒 **Khuyến nghị bảo mật môi trường Production**:
> - Không commit `MasterKey`, `AccessKeyId`, `SecretAccessKey` vào kho mã nguồn Git.
> - Thiết lập thông qua **Environment Variables**:
>   + `StorageSettings__Encryption__MasterKey`
>   + `StorageSettings__Supabase__AccessKeyId`
>   + `StorageSettings__Supabase__SecretAccessKey`
> - Hoặc trong môi trường phát triển cục bộ, sử dụng **.NET User Secrets**:
>   ```powershell
>   dotnet user-secrets set "StorageSettings:Encryption:MasterKey" "<YOUR_BASE64_KEY>" --project ProcurementSystem.API
>   ```

> ⚠️ **Cảnh báo quan trọng về thay đổi khóa MasterKey**:
> Việc thay đổi hoặc mất `MasterKey` mà chưa chuyển đổi dữ liệu sẽ khiến toàn bộ các tệp đã mã hóa trước đó trên Cloud **không thể giải mã được** (hệ thống sẽ ném `CryptographicException` để bảo vệ tính toàn vẹn dữ liệu). Bắt buộc phải sao lưu khóa bí mật an toàn.

---

## BƯỚC 5: SỬ DỤNG TIỆN ÍCH 1-CLICK SYNC & ROLLBACK
Hệ thống đã tích hợp sẵn Controller quản trị lưu trữ dành riêng cho Admin (`StorageAdminController`):

### 1. Kiểm tra trạng thái và kết nối S3:
- **API**: `GET /api/admin/storage/status`: Xem dung lượng tệp uploads trên máy local và trạng thái nhà cung cấp.
- **API**: `GET /api/admin/storage/test-connection`: Kiểm tra kết nối S3, danh sách Bucket trên Supabase và tự động tạo Bucket nếu chưa có.

### 2. 1-Click Sync lên Cloud (Không lo mất dữ liệu):
- **API**: `POST /api/admin/storage/sync-to-cloud`
- **Cơ chế an toàn tự động**:
  1. Tự động tạo bảng sao lưu CSDL `_Backup_FilePaths_YYYYMMDD_HHMMSS` lưu toàn bộ đường dẫn cũ.
  2. Nén GZip và mã hóa AES-256 từng tệp trong thư mục `uploads/` trước khi đẩy lên Supabase Private Bucket.
  3. Cập nhật các đường dẫn trong CSDL sang tiền tố `supabase://...` (bao gồm các bảng BidSubmissions, Contracts, Acceptances, EvaluatorProposals, ProcuringEntities, BidClarifications).
  4. Trả về báo cáo chi tiết từng tệp thành công/thất bại.

### 3. 1-Click Rollback về Local (Khôi phục nếu muốn):
- **API**: `POST /api/admin/storage/rollback`
- **Mô tả**: Nếu có bất kỳ sự cố gì, Admin chỉ cần gọi API này. Hệ thống sẽ đọc bảng sao lưu gần nhất và khôi phục lại 100% đường dẫn tệp trong CSDL về `/uploads/...` trên máy cục bộ chỉ trong 1-2 giây.

---

## QUY TRÌNH KIỂM KÊ VÀ CHUYỂN ĐỔI FILE CŨ (LEGACY CONVERSION GUIDE)

Nếu trong quá khứ hệ thống từng có các tệp mã hóa bằng khóa fallback mặc định (trước khi ban hành quy định bảo mật bắt buộc MasterKey 05/11/2026), quản trị viên cần tuân thủ quy trình kiểm kê và chuyển đổi sau:

1. **Kiểm kê tệp**:
   - Quét tất cả các tệp có tiền tố `supabase://` trong cơ sở dữ liệu.
   - Thử nghiệm giải mã với MasterKey chính thức trên môi trường staging / replica.
   - Tệp nào ném `CryptographicException` tức là từng được mã hóa bằng khóa cũ hoặc đã bị biến đổi.
2. **Chuyển đổi an toàn (Re-encryption Tool)**:
   - Sử dụng script chuyển đổi chuyên biệt được cấp quyền tường minh:
     + Đọc tệp cũ với khóa cũ được cung cấp qua secret tạm thời.
     + Giải mã và giải nén.
     + Nén và mã hóa lại với MasterKey mới chính thức 32-byte Base64.
     + Cập nhật lại blob trên Supabase Bucket và đối soát mã băm SHA-256.
   - **Tuyệt đối không nhúng khóa cũ vào mã nguồn ứng dụng như một cơ chế fallback tự động**.
