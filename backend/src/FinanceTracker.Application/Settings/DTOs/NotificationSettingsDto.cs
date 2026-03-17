namespace FinanceTracker.Application.Settings.DTOs;

public sealed record NotificationSettingsDto(bool BudgetWarningsEnabled, bool GoalRemindersEnabled, bool RecurringRemindersEnabled);