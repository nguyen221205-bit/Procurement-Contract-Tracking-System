namespace ProcurementSystem.Core.Enums
{
    public enum BidPackageStatus
    {
        Open = 0,         // Đang mở thầu
        Closed = 1,       // Đã đóng thầu
        Evaluating = 2,   // Đang chấm điểm
        Awarded = 3,      // Đã trao thầu (Phê duyệt kết quả LCNT)
        Contracted = 4    // Đã ký hợp đồng
    }
}
