namespace ProcurementSystem.Core.Interfaces
{
    public class TaxBusinessData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ShortName { get; set; }
        public string? Address { get; set; }
    }

    public class TaxApiResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public TaxBusinessData? Data { get; set; }
    }

    public interface ITaxLookupService
    {
        Task<TaxBusinessData?> VerifyTaxCodeAsync(string taxCode);
    }
}
