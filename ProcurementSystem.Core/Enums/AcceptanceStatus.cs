namespace ProcurementSystem.Core.Enums
{
    public enum AcceptanceStatus
    {
        Pending = 0,            // Chờ xử lý / Chờ xác nhận kỹ thuật
        Approved = 1,           // Đã phê duyệt giải ngân (Checker hoàn tất)
        Rejected = 2,           // Từ chối nghiệm thu
        TechnicalApproved = 3,  // Đã xác nhận kỹ thuật đạt (Maker hoàn tất, chờ Checker phê duyệt)
        ReviewInvalidated = 4   // Biên bản kiểm tra kỹ thuật bị vô hiệu hóa do mốc thanh toán bị sửa đổi nội dung
    }
}
