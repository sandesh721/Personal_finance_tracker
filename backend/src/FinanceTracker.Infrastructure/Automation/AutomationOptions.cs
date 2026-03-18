namespace FinanceTracker.Infrastructure.Automation;

public sealed class AutomationOptions
{
    public const string SectionName = "Automation";

    public bool EnableBackgroundProcessing { get; set; } = true;
    public int PollingIntervalSeconds { get; set; } = 60;
    public int GoalReminderLookaheadDays { get; set; } = 7;
    public int MaxRecurringRetryAttempts { get; set; } = 3;
    public int InitialRetryDelaySeconds { get; set; } = 60;
    public int MaxRetryDelaySeconds { get; set; } = 900;
}
