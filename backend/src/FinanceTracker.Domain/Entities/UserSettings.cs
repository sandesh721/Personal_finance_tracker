using FinanceTracker.Domain.Common;

namespace FinanceTracker.Domain.Entities;

public sealed class UserSettings : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string PreferredCurrencyCode { get; set; } = "INR";
    public string DateFormat { get; set; } = "dd MMM yyyy";
    public string LandingPage { get; set; } = "/dashboard";
    public string Theme { get; set; } = "slate";
    public bool BudgetWarningsEnabled { get; set; } = true;
    public bool GoalRemindersEnabled { get; set; } = true;
    public bool RecurringRemindersEnabled { get; set; } = true;
    public Guid? DefaultAccountId { get; set; }
    public string? DefaultPaymentMethod { get; set; }
    public int DefaultBudgetAlertThresholdPercent { get; set; } = 80;

    public User User { get; set; } = null!;
    public Account? DefaultAccount { get; set; }
}