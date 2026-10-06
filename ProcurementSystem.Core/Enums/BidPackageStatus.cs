namespace ProcurementSystem.Core.Enums
{
    public enum BidPackageStatus
    {
        Open = 0,         // Đang mở thầu (Đang tiếp nhận E-HSDT)
        Closed = 1,       // Đã đóng thầu (Hết hạn nộp thầu, chờ mở thầu)
        Evaluating = 2,   // Đang chấm điểm (Đã mở thầu, Tổ chuyên gia đang đánh giá)
        Awarded = 3,      // Đã trao thầu (Phê duyệt kết quả LCNT)
        Contracted = 4    // Đã ký hợp đồng (Hợp đồng đã có hiệu lực thi hành)
    }
}

