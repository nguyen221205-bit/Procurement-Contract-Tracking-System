namespace ProcurementSystem.Core.Enums
{
    public enum AddendumStatus
    {
        Draft = 0,    // Dự thảo phụ lục
        Approved = 1, // Đã phê duyệt và có hiệu lực thi hành
        Rejected = 2, // Bị từ chối
        Pending = 3   // Chờ phê duyệt (Maker-Checker F5-5)
    }
}
