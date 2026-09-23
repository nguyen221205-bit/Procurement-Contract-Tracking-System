namespace ProcurementSystem.Core.DTOs.Contract
{
    /// <summary>
    /// Thông tin chi tiết một lượt báo cáo tiến độ hợp đồng của nhà thầu
    /// </summary>
    public class ProgressUpdateDto
    {
        /// <summary>
        /// Mã định danh duy nhất của lượt báo cáo tiến độ
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Mã định danh hợp đồng tương ứng
        /// </summary>
        public int ContractId { get; set; }

        /// <summary>
        /// Tuần báo cáo tiến độ (1, 2, 3,...)
        /// </summary>
        public int WeekNumber { get; set; }

        /// <summary>
        /// Tỷ lệ phần trăm khối lượng công việc đã hoàn thành (%)
        /// </summary>
        public decimal CompletionPercent { get; set; }

        /// <summary>
        /// Mô tả chi tiết nội dung công việc và kết quả đạt được trong tuần
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Thời điểm gửi báo cáo tiến độ (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
