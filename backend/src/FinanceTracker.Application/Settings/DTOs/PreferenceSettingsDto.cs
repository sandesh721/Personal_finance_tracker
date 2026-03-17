namespace FinanceTracker.Application.Settings.DTOs;

public sealed record PreferenceSettingsDto(string PreferredCurrencyCode, string DateFormat, string LandingPage, string Theme);