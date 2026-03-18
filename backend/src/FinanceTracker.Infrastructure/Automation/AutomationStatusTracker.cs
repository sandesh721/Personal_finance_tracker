using FinanceTracker.Application.Automation.DTOs;
using FinanceTracker.Application.Automation.Interfaces;

namespace FinanceTracker.Infrastructure.Automation;

public sealed class AutomationStatusTracker : IAutomationStatusTracker
{
    private readonly object gate = new();
    private DateTime? lastStartedUtc;
    private DateTime? lastCompletedUtc;
    private DateTime? lastSuccessfulCompletedUtc;
    private bool? lastRunSucceeded;
    private bool isCycleRunning;
    private int consecutiveFailureCount;
    private int totalFailureCount;
    private DateTime? nextAttemptUtc;
    private string? lastError;
    private AutomationRunSummaryDto? lastSummary;

    public void RecordStarted(DateTime startedUtc)
    {
        lock (gate)
        {
            lastStartedUtc = startedUtc;
            isCycleRunning = true;
            lastRunSucceeded = null;
            lastError = null;
        }
    }

    public void RecordSucceeded(AutomationRunSummaryDto summary, DateTime completedUtc, DateTime nextAttemptUtc)
    {
        lock (gate)
        {
            lastCompletedUtc = completedUtc;
            lastSuccessfulCompletedUtc = completedUtc;
            lastRunSucceeded = true;
            isCycleRunning = false;
            consecutiveFailureCount = 0;
            this.nextAttemptUtc = nextAttemptUtc;
            lastError = null;
            lastSummary = summary;
        }
    }

    public int RecordFailed(DateTime completedUtc, string errorMessage, DateTime nextAttemptUtc)
    {
        lock (gate)
        {
            lastCompletedUtc = completedUtc;
            lastRunSucceeded = false;
            isCycleRunning = false;
            consecutiveFailureCount++;
            totalFailureCount++;
            this.nextAttemptUtc = nextAttemptUtc;
            lastError = errorMessage;
            return consecutiveFailureCount;
        }
    }

    public AutomationStatusDto GetSnapshot(bool backgroundProcessingEnabled, int pollingIntervalSeconds)
    {
        lock (gate)
        {
            return new AutomationStatusDto(
                backgroundProcessingEnabled,
                pollingIntervalSeconds,
                lastStartedUtc,
                lastCompletedUtc,
                lastSuccessfulCompletedUtc,
                lastRunSucceeded,
                isCycleRunning,
                consecutiveFailureCount,
                totalFailureCount,
                nextAttemptUtc,
                lastError,
                lastSummary);
        }
    }
}
