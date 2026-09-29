using System.Net.Http.Json;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.Infrastructure.Services
{
    public class TaxLookupService : ITaxLookupService
    {
        private readonly HttpClient _httpClient;

        public TaxLookupService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://api.vietqr.io/v2/");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<TaxBusinessData?> VerifyTaxCodeAsync(string taxCode)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<TaxApiResponse>($"business/{taxCode.Trim()}");
                if (response != null && response.Code == "00" && response.Data != null)
                {
                    return response.Data;
                }
            }
            catch
            {
                // Xử lý timeout hoặc lỗi mạng
            }
            return null;
        }
    }
}
