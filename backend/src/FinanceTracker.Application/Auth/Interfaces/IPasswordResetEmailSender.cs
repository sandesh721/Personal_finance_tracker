namespace FinanceTracker.Application.Auth.Interfaces;

public interface IPasswordResetEmailSender
{
    Task SendResetLinkAsync(string email, string resetUrl, CancellationToken cancellationToken);
}
