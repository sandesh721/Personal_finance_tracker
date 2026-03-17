namespace FinanceTracker.Application.Auth.DTOs;

public sealed class ForgotPasswordRequest
{
    public string Email { get; init; } = string.Empty;
}
