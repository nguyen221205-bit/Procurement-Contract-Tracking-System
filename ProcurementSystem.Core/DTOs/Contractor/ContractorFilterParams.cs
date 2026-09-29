namespace ProcurementSystem.Core.DTOs.Contractor
{
    public class ContractorFilterParams
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
        /// Tìm kiếm theo Tên công ty hoặc Mã số thuế
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Lọc điểm đánh giá tối thiểu (0.0 - 5.0)
        /// </summary>
        public decimal? MinRating { get; set; }

        /// <summary>
        /// Lọc điểm đánh giá tối đa (0.0 - 5.0)
        /// </summary>
        public decimal? MaxRating { get; set; }

        /// <summary>
        /// Tiêu chí sắp xếp: "rating", "name", "createdat"
        /// </summary>
        public string? SortBy { get; set; } = "rating";

        /// <summary>
        /// Sắp xếp giảm dần (mặc định true)
        /// </summary>
        public bool SortDescending { get; set; } = true;
    }
}
