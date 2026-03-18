namespace FinanceTracker.Application.Automation.DTOs;

public sealed record AutomationStatusDto(
    bool BackgroundProcessingEnabled,
    int PollingIntervalSeconds,
    DateTime? LastStartedUtc,
    DateTime? LastCompletedUtc,
    DateTime? LastSuccessfulCompletedUtc,
    bool? LastRunSucceeded,
    bool IsCycleRunning,
    int ConsecutiveFailureCount,
    int TotalFailureCount,
    DateTime? NextAttemptUtc,
    string? LastError,
    AutomationRunSummaryDto? LastSummary);
