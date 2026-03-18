namespace FinanceTracker.Application.Automation.DTOs;

public sealed record AutomationRunSummaryDto(
    int UsersProcessed,
    int TransactionsCreated,
    int AutoOccurrencesProcessed,
    int AutoOccurrencesDeferredForRetry,
    int AutoOccurrencesFailedPermanently,
    int ManualRemindersCreated,
    int GoalRemindersCreated,
    DateTime ProcessedAtUtc);
