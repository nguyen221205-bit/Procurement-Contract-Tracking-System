namespace ProcurementSystem.Core.DTOs.BidSubmission
{
    public class BidSubmissionFilterParams
    {
        private const int MaxPageSize = 50;
        private int _pageSize = 10;

        public int PageNumber { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 10 : value);
        }

        /// <summary>
        /// Lọc theo trạng thái hồ sơ (Submitted, Evaluated, Selected, Rejected)
        /// </summary>
        public string? Status { get; set; }
    }
}
