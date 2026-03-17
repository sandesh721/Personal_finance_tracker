namespace FinanceTracker.Application.Settings.DTOs;

public sealed class UpdatePreferencesRequest
{
    public string PreferredCurrencyCode { get; init; } = "INR";
    public string DateFormat { get; init; } = "dd MMM yyyy";
    public string LandingPage { get; init; } = "/dashboard";
    public string Theme { get; init; } = "slate";
}