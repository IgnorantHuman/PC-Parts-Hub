using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PCShop.Services;

namespace PCShop.Pages.Tools;

public class CurrencyConverterModel : PageModel
{
    private static readonly HashSet<string> AllowedCurrencies =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "USD",
            "SGD",
            "EUR",
            "GBP",
            "JPY",
            "AUD"
        };

    private readonly ExchangeRateService _exchangeRateService;

    public CurrencyConverterModel(
        ExchangeRateService exchangeRateService)
    {
        _exchangeRateService = exchangeRateService;
    }

    [BindProperty]
    [Range(
        typeof(decimal),
        "0.01",
        "1000000",
        ErrorMessage =
            "Enter an amount between RM0.01 and RM1,000,000.")]
    public decimal Amount { get; set; } = 100;

    [BindProperty]
    [Required]
    public string TargetCurrency { get; set; } = "USD";

    public decimal? ConvertedAmount { get; private set; }

    public decimal? ExchangeRate { get; private set; }

    public string? RateDate { get; private set; }

    public string? ApiError { get; private set; }

    public IReadOnlyList<(string Code, string Name)> Currencies
    { get; } = new List<(string, string)>
        {
            ("USD", "US Dollar"),
            ("SGD", "Singapore Dollar"),
            ("EUR", "Euro"),
            ("GBP", "British Pound"),
            ("JPY", "Japanese Yen"),
            ("AUD", "Australian Dollar")
        };

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(
        CancellationToken cancellationToken)
    {
        if (!AllowedCurrencies.Contains(TargetCurrency))
        {
            ModelState.AddModelError(
                nameof(TargetCurrency),
                "Please select a supported currency.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        ExchangeRateResult? result =
            await _exchangeRateService.GetRateAsync(
                "MYR",
                TargetCurrency,
                cancellationToken);

        if (result == null)
        {
            ApiError =
                "The exchange-rate service is unavailable. " +
                "Please try again later.";

            return Page();
        }

        ExchangeRate = result.Rate;
        RateDate = result.RateDate;

        ConvertedAmount =
            decimal.Round(Amount * result.Rate, 2);

        return Page();
    }
}