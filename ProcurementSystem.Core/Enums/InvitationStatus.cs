namespace ProcurementSystem.Core.Enums
{
    public enum InvitationStatus
    {
        Pending = 0,   // Đang chờ chấp nhận
        Accepted = 1,  // Đã chấp nhận
        Revoked = 2,   // Đã thu hồi bởi người mời
        Expired = 3    // Đã hết hạn
    }
}
