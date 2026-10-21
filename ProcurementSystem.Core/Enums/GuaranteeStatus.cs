namespace ProcurementSystem.Core.Enums
{
    public enum GuaranteeStatus
    {
        PendingApproval = 0, // Chờ duyệt / tiếp nhận
        Valid = 1,           // Đang có hiệu lực
        Released = 2,        // Đã giải tỏa / hoàn trả
        Claimed = 3,         // Bị tịch thu (do vi phạm nghĩa vụ)
        Expired = 4          // Đã hết hiệu lực
    }
}
