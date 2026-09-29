using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.Contract
{
    public class ContractFilterParams
    {
        private const int MaxPageSize = 50;
        private int _pageSize = 10;

        public int PageNumber { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 10 : value);
        }
        public string? ContractNumber { get; set; }
        public ContractStatus? Status { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? EndDateTo { get; set; }
    }
}
