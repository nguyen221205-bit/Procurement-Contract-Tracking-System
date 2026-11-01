namespace ProcurementSystem.Core.Enums
{
    public enum SigningAuthorityType
    {
        DisbursementApproval = 0,    // Ký duyệt biên bản nghiệm thu & giải ngân mốc thanh toán
        ContractSigning = 1,         // Ký kết hợp đồng kinh tế và phụ lục
        FullLegalRepresentative = 2  // Toàn quyền đại diện pháp nhân Bên mời thầu
    }
}
