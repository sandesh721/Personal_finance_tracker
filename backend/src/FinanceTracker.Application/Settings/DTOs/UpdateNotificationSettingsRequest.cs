namespace FinanceTracker.Application.Settings.DTOs;

public sealed class UpdateNotificationSettingsRequest
{
    public bool BudgetWarningsEnabled { get; init; } = true;
    public bool GoalRemindersEnabled { get; init; } = true;
    public bool RecurringRemindersEnabled { get; init; } = true;
}