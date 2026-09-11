using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace PCShop.Services;

public class PayPalService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public PayPalService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["PayPal:ClientId"]) &&
        !string.IsNullOrWhiteSpace(_configuration["PayPal:Secret"]);

    public async Task<(string OrderId, string ApprovalUrl)> CreateOrderAsync(
        decimal amount, string returnUrl, string cancelUrl)
    {
        string token = await GetAccessTokenAsync();
        using HttpRequestMessage request = new(HttpMethod.Post, GetBaseUrl() + "/v2/checkout/orders");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new
        {
            intent = "CAPTURE",
            purchase_units = new[] { new { amount = new { currency_code = "MYR", value = amount.ToString("0.00") } } },
            application_context = new { return_url = returnUrl, cancel_url = cancelUrl, user_action = "PAY_NOW" }
        });

        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string orderId = json.RootElement.GetProperty("id").GetString()!;
        string approvalUrl = json.RootElement.GetProperty("links").EnumerateArray()
            .First(link => link.GetProperty("rel").GetString() == "approve")
            .GetProperty("href").GetString()!;
        return (orderId, approvalUrl);
    }

    public async Task<string> CaptureOrderAsync(string orderId)
    {
        string token = await GetAccessTokenAsync();
        using HttpRequestMessage request = new(HttpMethod.Post,
            $"{GetBaseUrl()}/v2/checkout/orders/{Uri.EscapeDataString(orderId)}/capture");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new { });
        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string status = json.RootElement.GetProperty("status").GetString() ?? string.Empty;
        if (status != "COMPLETED") throw new InvalidOperationException("PayPal payment was not completed.");
        return json.RootElement.GetProperty("purchase_units")[0]
            .GetProperty("payments").GetProperty("captures")[0]
            .GetProperty("id").GetString()!;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        string clientId = _configuration["PayPal:ClientId"]
            ?? throw new InvalidOperationException("PayPal ClientId is missing.");
        string secret = _configuration["PayPal:Secret"]
            ?? throw new InvalidOperationException("PayPal Secret is missing.");
        using HttpRequestMessage request = new(HttpMethod.Post, GetBaseUrl() + "/v1/oauth2/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secret}")));
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials" });
        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("access_token").GetString()!;
    }

    private string GetBaseUrl() =>
        _configuration["PayPal:BaseUrl"] ?? "https://api-m.sandbox.paypal.com";
}
