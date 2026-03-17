using FinanceTracker.Application.Common;
using FinanceTracker.Application.Settings.DTOs;
using FinanceTracker.Application.Settings.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Settings;

public sealed class SettingsService(ApplicationDbContext dbContext, IPasswordHasher<User> passwordHasher) : ISettingsService
{
    private static readonly HashSet<string> AllowedThemes = new(StringComparer.OrdinalIgnoreCase) { "slate", "warm", "dark" };
    private static readonly HashSet<string> AllowedDateFormats = new(StringComparer.Ordinal) { "dd MMM yyyy", "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd" };
    private static readonly HashSet<string> AllowedLandingPages = new(StringComparer.OrdinalIgnoreCase)
    {
        "/dashboard",
        "/transactions",
        "/accounts",
        "/budgets",
        "/goals",
        "/reports",
        "/recurring",
        "/settings"
    };

    public async Task<UserSettingsDto> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        var settings = await EnsureSettingsAsync(userId, cancellationToken);
        var accountName = settings.DefaultAccountId.HasValue
            ? await dbContext.Accounts.AsNoTracking().Where(x => x.UserId == userId && x.Id == settings.DefaultAccountId.Value).Select(x => x.Name).SingleOrDefaultAsync(cancellationToken)
            : null;

        return Map(user, settings, accountName);
    }

    public async Task<ProfileSettingsDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        var email = NormalizeEmail(request.Email);
        ValidateName(request.FirstName, "First name");
        ValidateName(request.LastName, "Last name");

        var emailExists = await dbContext.Users.AnyAsync(x => x.Id != userId && x.Email == email, cancellationToken);
        if (emailExists)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = email;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ProfileSettingsDto(user.Id, user.Email, user.FirstName, user.LastName);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            throw new ValidationException("Current password is required.");
        }

        ValidatePassword(request.NewPassword);

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new ValidationException("Current password is incorrect.");
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PreferenceSettingsDto> UpdatePreferencesAsync(Guid userId, UpdatePreferencesRequest request, CancellationToken cancellationToken)
    {
        var settings = await EnsureSettingsAsync(userId, cancellationToken);

        var currencyCode = NormalizeCurrency(request.PreferredCurrencyCode);
        if (!AllowedDateFormats.Contains(request.DateFormat))
        {
            throw new ValidationException("Selected date format is invalid.");
        }

        var landingPage = request.LandingPage.Trim();
        if (!AllowedLandingPages.Contains(landingPage))
        {
            throw new ValidationException("Selected landing page is invalid.");
        }

        var theme = request.Theme.Trim().ToLowerInvariant();
        if (!AllowedThemes.Contains(theme))
        {
            throw new ValidationException("Selected theme is invalid.");
        }

        settings.PreferredCurrencyCode = currencyCode;
        settings.DateFormat = request.DateFormat;
        settings.LandingPage = landingPage;
        settings.Theme = theme;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new PreferenceSettingsDto(settings.PreferredCurrencyCode, settings.DateFormat, settings.LandingPage, settings.Theme);
    }

    public async Task<NotificationSettingsDto> UpdateNotificationsAsync(Guid userId, UpdateNotificationSettingsRequest request, CancellationToken cancellationToken)
    {
        var settings = await EnsureSettingsAsync(userId, cancellationToken);
        settings.BudgetWarningsEnabled = request.BudgetWarningsEnabled;
        settings.GoalRemindersEnabled = request.GoalRemindersEnabled;
        settings.RecurringRemindersEnabled = request.RecurringRemindersEnabled;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new NotificationSettingsDto(settings.BudgetWarningsEnabled, settings.GoalRemindersEnabled, settings.RecurringRemindersEnabled);
    }

    public async Task<FinancialDefaultsSettingsDto> UpdateFinancialDefaultsAsync(Guid userId, UpdateFinancialDefaultsRequest request, CancellationToken cancellationToken)
    {
        var settings = await EnsureSettingsAsync(userId, cancellationToken);

        if (request.DefaultBudgetAlertThresholdPercent is < 1 or > 100)
        {
            throw new ValidationException("Default budget alert threshold must be between 1 and 100.");
        }

        Account? account = null;
        if (request.DefaultAccountId.HasValue)
        {
            account = await dbContext.Accounts.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == request.DefaultAccountId.Value && !x.IsArchived, cancellationToken)
                ?? throw new ValidationException("Selected default account is invalid or archived.");
        }

        settings.DefaultAccountId = account?.Id;
        settings.DefaultPaymentMethod = NormalizeNullable(request.DefaultPaymentMethod, 64);
        settings.DefaultBudgetAlertThresholdPercent = request.DefaultBudgetAlertThresholdPercent;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new FinancialDefaultsSettingsDto(settings.DefaultAccountId, account?.Name, settings.DefaultPaymentMethod, settings.DefaultBudgetAlertThresholdPercent);
    }

    public async Task LogoutAllSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedUtc == null)
            .ToListAsync(cancellationToken);

        if (activeTokens.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var token in activeTokens)
        {
            token.RevokedUtc = now;
            token.RevocationReason = "All sessions revoked by user";
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<UserSettings> EnsureSettingsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        var settings = await dbContext.Set<UserSettings>()
            .Include(x => x.DefaultAccount)
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        settings = new UserSettings
        {
            UserId = user.Id,
        };

        dbContext.Set<UserSettings>().Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static UserSettingsDto Map(User user, UserSettings settings, string? accountName)
        => new(
            new ProfileSettingsDto(user.Id, user.Email, user.FirstName, user.LastName),
            new PreferenceSettingsDto(settings.PreferredCurrencyCode, settings.DateFormat, settings.LandingPage, settings.Theme),
            new NotificationSettingsDto(settings.BudgetWarningsEnabled, settings.GoalRemindersEnabled, settings.RecurringRemindersEnabled),
            new FinancialDefaultsSettingsDto(settings.DefaultAccountId, accountName, settings.DefaultPaymentMethod, settings.DefaultBudgetAlertThresholdPercent));

    private static void ValidateName(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 100)
        {
            throw new ValidationException($"{fieldName} is required and must be 100 characters or fewer.");
        }
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email is required.");
        }

        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length > 256 || !normalized.Contains('@'))
        {
            throw new ValidationException("Email must be valid and 256 characters or fewer.");
        }

        return normalized;
    }

    private static string NormalizeCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length != 3)
        {
            throw new ValidationException("Preferred currency must be a 3-letter currency code.");
        }

        return value.Trim().ToUpperInvariant();
    }

    private static string? NormalizeNullable(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ValidationException($"Value must be {maxLength} characters or fewer.");
        }

        return trimmed;
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12 || password.Length > 128)
        {
            throw new ValidationException("New password must be between 12 and 128 characters.");
        }

        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            throw new ValidationException("New password must include uppercase, lowercase, number, and special characters.");
        }
    }
}