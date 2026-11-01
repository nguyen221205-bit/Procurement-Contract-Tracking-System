namespace ProcurementSystem.Core.Enums
{
    public enum MemberStatus
    {
        Active = 0,    // Đang hoạt động
        Suspended = 1, // Tạm khóa quyền
        Revoked = 2    // Đã thu hồi tư cách thành viên (nghỉ việc / chuyển công tác)
    }
}
