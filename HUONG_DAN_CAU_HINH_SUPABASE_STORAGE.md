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
   - Endpoint S3 sẽ có dạng: `https://<PROJECT_REF>.storage.supabase.co/v1/s3`.
---

## BƯỚC 4: ĐIỀN CẤU HÌNH VÀO `appsettings.json`
Mở tệp `ProcurementSystem.API/appsettings.json` và cập nhật block `StorageSettings`:

```json
  "StorageSettings": {
    "Provider": "Supabase", // Chuyển từ "Local" sang "Supabase" để kích hoạt Cloud
    "Supabase": {
      "Endpoint": "https://<PROJECT_REF>.storage.supabase.co/storage/v1/s3",
      "BucketName": "procurement-documents",
      "AccessKeyId": "<ACCESS_KEY_ID_VỪA_LẤY>",
      "SecretAccessKey": "<SECRET_ACCESS_KEY_VỪA_LẤY>",
      "Region": "ap-southeast-1"
    }
  }
```

> **Lưu ý**: Nếu muốn quay trở lại dùng lưu trữ cục bộ trên ổ cứng như cũ, bạn chỉ cần đổi `"Provider": "Local"`.

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
  2. Đẩy toàn bộ tệp trong thư mục `uploads/` lên Supabase Private Bucket.
  3. Cập nhật các đường dẫn trong CSDL sang tiền tố `supabase://...`.
  4. Trả về báo cáo chi tiết từng tệp thành công/thất bại.

### 3. 1-Click Rollback về Local (Khôi phục nếu muốn):
- **API**: `POST /api/admin/storage/rollback`
- **Mô tả**: Nếu có bất kỳ sự cố gì, Admin chỉ cần gọi API này. Hệ thống sẽ đọc bảng sao lưu gần nhất và khôi phục lại 100% đường dẫn tệp trong CSDL về `/uploads/...` trên máy cục bộ chỉ trong 1-2 giây.

---

## KIỂM TRA BẢO MẬT & XỬ LÝ DỮ LIỆU ĐÁM MÂY (ZERO-TRUST PIPELINE)
Hệ thống triển khai chuẩn mực bảo mật và xử lý dữ liệu trước khi đẩy lên Cloud:

### 1. Luồng tải lên (Upload Pipeline):
1. **Kiểm tra Magic Bytes**: Kiểm tra header nhị phân thực tế của tệp (chặn giả mạo đuôi mở rộng).
2. **Nén dữ liệu (GZip Stream)**: Tối ưu dung lượng giảm 40% - 70%, tiết kiệm băng thông và bộ nhớ Cloud.
3. **Mã hóa đối xứng (AES-256-CBC)**: Sinh Dynamic IV 16 bytes ngẫu nhiên cho mỗi file và mã hóa dữ liệu. Tệp lưu trên Supabase Storage là chuỗi nhị phân vô nghĩa (Zero-Trust), bên thứ ba không thể xem được.

### 2. Luồng tải về (Download Pipeline):
1. Kiểm tra xác thực token JWT và phân quyền người dùng (RBAC).
2. Kiểm tra nghiệp vụ Đấu thầu (hồ sơ dự thầu chỉ được mở sau thời điểm đóng/mở thầu).
3. Backend kéo tệp mã hóa từ Supabase S3 $\rightarrow$ Đọc 16 bytes IV $\rightarrow$ Giải mã AES-256 $\rightarrow$ Giải nén GZip $\rightarrow$ Stream trả về tệp gốc cho trình duyệt (người dùng mở xem bình thường, hoàn toàn trong suốt).
