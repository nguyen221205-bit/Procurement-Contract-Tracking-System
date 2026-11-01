namespace ProcurementSystem.Core.DTOs.Contract;

public class ContractListDto : PaginatedList<ContractSummaryDto>
{
    public ContractListSummaryDto Summary { get; set; }

    public ContractListDto(PaginatedList<ContractSummaryDto> page, ContractListSummaryDto summary)
        : base(page.Items, page.TotalCount, page.PageIndex, page.PageSize)
    {
        Summary = summary;
    }
}
