namespace FinanceTracker.Application.Settings.DTOs;

public sealed class UpdateFinancialDefaultsRequest
{
    public Guid? DefaultAccountId { get; init; }
    public string? DefaultPaymentMethod { get; init; }
    public int DefaultBudgetAlertThresholdPercent { get; init; } = 80;
}