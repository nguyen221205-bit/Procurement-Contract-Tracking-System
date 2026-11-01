namespace ProcurementSystem.Core.Enums
{
    public enum ProcuringEntityMemberRole
    {
        Manager = 0,   // Quản trị viên đơn vị / Đầu mối pháp nhân (Quản lý thành viên, mời nhân sự)
        Officer = 1,   // Cán bộ phụ trách dự án / Kỹ thuật hiện trường (Maker)
        Finance = 2,   // Kế toán / Kiểm soát tài chính
        Signer = 3     // Người có thẩm quyền ký / Lãnh đạo phê duyệt giải ngân (Checker)
    }
}
