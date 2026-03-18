using FinanceTracker.Application.Automation.Interfaces;
using FinanceTracker.Infrastructure.Automation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FinanceTracker.Api.HostedServices;

public sealed class FinanceAutomationHostedService(
    IServiceScopeFactory scopeFactory,
    IAutomationStatusTracker statusTracker,
    IOptionsMonitor<AutomationOptions> optionsMonitor,
    ILogger<FinanceAutomationHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = optionsMonitor.CurrentValue;
            var delay = TimeSpan.FromSeconds(Math.Max(options.PollingIntervalSeconds, 15));

            if (options.EnableBackgroundProcessing)
            {
                var startedUtc = DateTime.UtcNow;
                statusTracker.RecordStarted(startedUtc);

                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var automationService = scope.ServiceProvider.GetRequiredService<IAutomationService>();
                    var summary = await automationService.RunAsync(startedUtc, stoppingToken);
                    var completedUtc = DateTime.UtcNow;
                    statusTracker.RecordSucceeded(summary, completedUtc, completedUtc.Add(delay));
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    var completedUtc = DateTime.UtcNow;
                    var currentState = statusTracker.GetSnapshot(options.EnableBackgroundProcessing, options.PollingIntervalSeconds);
                    var nextFailureCount = currentState.ConsecutiveFailureCount + 1;
                    delay = ComputeFailureDelay(options, nextFailureCount);
                    statusTracker.RecordFailed(completedUtc, ex.Message, completedUtc.Add(delay));
                    logger.LogError(ex, "Automation cycle failed.");
                }
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private static TimeSpan ComputeFailureDelay(AutomationOptions options, int consecutiveFailureCount)
    {
        var baseDelaySeconds = Math.Max(options.InitialRetryDelaySeconds, Math.Max(options.PollingIntervalSeconds, 15));
        var maxDelaySeconds = Math.Max(options.MaxRetryDelaySeconds, baseDelaySeconds);
        var safeExponent = Math.Min(Math.Max(consecutiveFailureCount - 1, 0), 10);
        var scaledSeconds = baseDelaySeconds * Math.Pow(2, safeExponent);
        var boundedSeconds = Math.Min(maxDelaySeconds, scaledSeconds);
        return TimeSpan.FromSeconds(Math.Max(baseDelaySeconds, boundedSeconds));
    }
}
