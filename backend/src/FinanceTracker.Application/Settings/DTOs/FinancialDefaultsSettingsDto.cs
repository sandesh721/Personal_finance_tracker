namespace FinanceTracker.Application.Settings.DTOs;

public sealed record FinancialDefaultsSettingsDto(Guid? DefaultAccountId, string? DefaultAccountName, string? DefaultPaymentMethod, int DefaultBudgetAlertThresholdPercent);