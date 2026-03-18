using FinanceTracker.Application.Automation.DTOs;

namespace FinanceTracker.Application.Automation.Interfaces;

public interface IAutomationStatusTracker
{
    void RecordStarted(DateTime startedUtc);
    void RecordSucceeded(AutomationRunSummaryDto summary, DateTime completedUtc, DateTime nextAttemptUtc);
    int RecordFailed(DateTime completedUtc, string errorMessage, DateTime nextAttemptUtc);
    AutomationStatusDto GetSnapshot(bool backgroundProcessingEnabled, int pollingIntervalSeconds);
}
