using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace PCShop.Services;

public class ExchangeRateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExchangeRateService> _logger;

    public ExchangeRateService(
        HttpClient httpClient,
        ILogger<ExchangeRateService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ExchangeRateResult?> GetRateAsync(
        string baseCurrency,
        string targetCurrency,
        CancellationToken cancellationToken = default)
    {
        string safeBase =
            Uri.EscapeDataString(baseCurrency.ToUpperInvariant());

        string safeTarget =
            Uri.EscapeDataString(targetCurrency.ToUpperInvariant());

        try
        {
            using HttpResponseMessage response =
                await _httpClient.GetAsync(
                    $"v2/rate/{safeBase}/{safeTarget}",
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            ExchangeRateApiResponse? data =
                await response.Content
                    .ReadFromJsonAsync<ExchangeRateApiResponse>(
                        cancellationToken);

            if (data == null || data.Rate <= 0)
            {
                return null;
            }

            return new ExchangeRateResult(
                data.Base,
                data.Quote,
                data.Rate,
                data.Date);
        }
        catch (Exception exception)
            when (exception is HttpRequestException
                  or TaskCanceledException)
        {
            _logger.LogWarning(
                exception,
                "Unable to retrieve the exchange rate.");

            return null;
        }
    }

    private sealed class ExchangeRateApiResponse
    {
        [JsonPropertyName("base")]
        public string Base { get; set; } = "";

        [JsonPropertyName("quote")]
        public string Quote { get; set; } = "";

        [JsonPropertyName("rate")]
        public decimal Rate { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; } = "";
    }
}

public record ExchangeRateResult(
    string BaseCurrency,
    string TargetCurrency,
    decimal Rate,
    string RateDate);