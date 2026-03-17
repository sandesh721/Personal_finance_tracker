namespace FinanceTracker.Infrastructure.Auth;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; init; }
    public string? FromAddress { get; init; }
    public string FromName { get; init; } = "Ledger Nest";
    public string? SmtpHost { get; init; }
    public int Port { get; init; } = 587;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public bool UseSsl { get; init; } = true;
}
