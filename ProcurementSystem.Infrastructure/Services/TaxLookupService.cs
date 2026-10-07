using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ProcurementSystem.Core.Interfaces;

namespace ProcurementSystem.Infrastructure.Services
{
    public class TaxLookupService : ITaxLookupService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TaxLookupService> _logger;
        private static readonly Regex TaxCodeRegex = new(@"^\d{10}(-\d{3})?$", RegexOptions.Compiled);

        public TaxLookupService(HttpClient httpClient, ILogger<TaxLookupService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.BaseAddress = new Uri("https://api.vietqr.io/v2/");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<TaxBusinessData?> VerifyTaxCodeAsync(string taxCode)
        {
            if (string.IsNullOrWhiteSpace(taxCode))
            {
                return null;
            }

            var trimmedTaxCode = taxCode.Trim();
            if (!TaxCodeRegex.IsMatch(trimmedTaxCode))
            {
                _logger.LogWarning("Mã số thuế '{TaxCode}' không đúng định dạng chuẩn Việt Nam (10 chữ số hoặc 13 chữ số có dấu gạch ngang).", trimmedTaxCode);
                return null;
            }

            try
            {
                var response = await _httpClient.GetFromJsonAsync<TaxApiResponse>($"business/{trimmedTaxCode}");
                if (response != null && response.Code == "00" && response.Data != null)
                {
                    return response.Data;
                }

                _logger.LogWarning("VietQR API phản hồi mã không thành công ({Code}) cho MST '{TaxCode}': {Desc}", response?.Code, trimmedTaxCode, response?.Desc);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Lỗi kết nối HTTP khi tra cứu MST '{TaxCode}' từ dịch vụ VietQR.", trimmedTaxCode);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Hết thời gian chờ (Timeout) khi tra cứu MST '{TaxCode}' từ dịch vụ VietQR.", trimmedTaxCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không xác định khi tra cứu thông tin doanh nghiệp qua MST '{TaxCode}'.", trimmedTaxCode);
            }

            return null;
        }
    }
}
