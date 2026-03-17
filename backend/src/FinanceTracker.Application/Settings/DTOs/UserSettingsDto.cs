namespace FinanceTracker.Application.Settings.DTOs;

public sealed record UserSettingsDto(
    ProfileSettingsDto Profile,
    PreferenceSettingsDto Preferences,
    NotificationSettingsDto Notifications,
    FinancialDefaultsSettingsDto FinancialDefaults);