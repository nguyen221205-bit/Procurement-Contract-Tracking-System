namespace ProcurementSystem.Core.DTOs.EvaluatorProposal
{
    /// <summary>
    /// Tham số lọc và phân trang danh sách đề xuất Giám khảo
    /// </summary>
    public class EvaluatorProposalFilterParams
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 10;

        public int PageIndex { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 10 : value);
        }

        /// <summary>
        /// Từ khóa tìm kiếm theo Họ tên, Email, Đơn vị công tác hoặc Chuyên môn
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Lọc theo trạng thái đề xuất: Pending, Approved, Rejected
        /// </summary>
        public string? Status { get; set; }
    }
}
