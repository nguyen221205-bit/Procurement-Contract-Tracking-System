namespace ProcurementSystem.Core.Enums
{
    /// <summary>
    /// Các trạng thái vòng đời chuẩn của Hồ sơ dự thầu (E-HSDT)
    /// </summary>
    public static class BidSubmissionStatus
    {
        /// <summary>
        /// Đã nộp hồ sơ, niêm phong chờ mở thầu
        /// </summary>
        public const string Submitted = "Submitted";

        /// <summary>
        /// Đã hoàn tất chấm điểm đủ tiêu chí
        /// </summary>
        public const string Evaluated = "Evaluated";

        /// <summary>
        /// Đã được phê duyệt trúng thầu
        /// </summary>
        public const string Selected = "Selected";

        /// <summary>
        /// Không trúng thầu hoặc bị loại từ vòng đánh giá
        /// </summary>
        public const string Rejected = "Rejected";

        /// <summary>
        /// Nhà thầu đã rút hồ sơ dự thầu hợp lệ trước hạn đóng thầu
        /// </summary>
        public const string Withdrawn = "Withdrawn";
    }
}
