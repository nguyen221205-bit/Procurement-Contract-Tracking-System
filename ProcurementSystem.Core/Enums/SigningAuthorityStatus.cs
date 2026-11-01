namespace ProcurementSystem.Core.Enums
{
    public enum SigningAuthorityStatus
    {
        PendingVerification = 0, // Chờ Admin nền tảng thẩm tra và xác minh
        Active = 1,              // Đã xác minh hợp lệ, đang có hiệu lực ký duyệt
        Rejected = 2,            // Bị từ chối do hồ sơ/căn cứ ủy quyền không hợp lệ
        Revoked = 3,             // Đã bị thu hồi bởi Quản lý đơn vị hoặc Admin
        Expired = 4              // Đã hết thời hạn hiệu lực được cấp
    }
}
