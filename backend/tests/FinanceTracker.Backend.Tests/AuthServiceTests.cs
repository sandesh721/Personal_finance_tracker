using FinanceTracker.Application.Auth.DTOs;
using FinanceTracker.Application.Auth.Exceptions;
using FinanceTracker.Application.Settings.DTOs;
using FinanceTracker.Infrastructure.Auth;
using FinanceTracker.Infrastructure.Financial;
using FinanceTracker.Infrastructure.Settings;
using FinanceTracker.Backend.Tests.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FinanceTracker.Backend.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterLoginAndRefresh_SeedDefaultsAndRotateRefreshToken()
    {
        await using var database = new SqliteTestDatabase();
        await using var dbContext = database.CreateContext();
        var tokenGenerator = CreateTokenGenerator();
        var service = new AuthService(dbContext, new PasswordHasher<FinanceTracker.Domain.Entities.User>(), tokenGenerator, new CategorySeeder(dbContext));

        var register = await service.RegisterAsync(new RegisterRequest
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            Password = "Abcd1234567@#"
        }, "127.0.0.1", "tests", CancellationToken.None);

        var login = await service.LoginAsync(new LoginRequest
        {
            Email = "ada@example.com",
            Password = "Abcd1234567@#"
        }, "127.0.0.1", "tests", CancellationToken.None);

        var refresh = await service.RefreshAsync(register.RefreshToken, "127.0.0.1", "tests", CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(register.Response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(login.Response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refresh.Response.AccessToken));
        Assert.NotEqual(register.RefreshToken, refresh.RefreshToken);
        Assert.True(await dbContext.Categories.CountAsync(x => x.UserId == register.Response.User.Id) >= 18);
        Assert.True(await dbContext.RefreshTokens.CountAsync(x => x.UserId == register.Response.User.Id) >= 3);
        Assert.True(await dbContext.RefreshTokens.AnyAsync(x => x.UserId == register.Response.User.Id && x.RevokedUtc != null));
    }

    [Fact]
    public async Task RequestPasswordReset_ForUnknownEmail_DoesNotCreateToken()
    {
        await using var database = new SqliteTestDatabase();
        await using var dbContext = database.CreateContext();
        var service = new AuthService(dbContext, new PasswordHasher<FinanceTracker.Domain.Entities.User>(), CreateTokenGenerator(), new CategorySeeder(dbContext));

        var token = await service.RequestPasswordResetAsync(new ForgotPasswordRequest
        {
            Email = "missing@example.com"
        }, "127.0.0.1", "tests", CancellationToken.None);

        Assert.Null(token);
        Assert.Equal(0, await dbContext.PasswordResetTokens.CountAsync());
    }

    [Fact]
    public async Task PasswordReset_UsesOneTimeToken_UpdatesPassword_AndRevokesSessions()
    {
        await using var database = new SqliteTestDatabase();
        await using var dbContext = database.CreateContext();
        var passwordHasher = new PasswordHasher<FinanceTracker.Domain.Entities.User>();
        var tokenGenerator = CreateTokenGenerator();
        var service = new AuthService(dbContext, passwordHasher, tokenGenerator, new CategorySeeder(dbContext));

        var user = TestData.AddUser(dbContext, "reset@example.com");
        user.PasswordHash = passwordHasher.HashPassword(user, "OldPassword123!");
        await dbContext.SaveChangesAsync();

        var login = await service.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "OldPassword123!"
        }, "127.0.0.1", "tests", CancellationToken.None);

        var firstToken = await service.RequestPasswordResetAsync(new ForgotPasswordRequest
        {
            Email = user.Email
        }, "127.0.0.1", "tests", CancellationToken.None);
        var secondToken = await service.RequestPasswordResetAsync(new ForgotPasswordRequest
        {
            Email = user.Email
        }, "127.0.0.1", "tests", CancellationToken.None);

        Assert.NotNull(firstToken);
        Assert.NotNull(secondToken);
        Assert.NotEqual(firstToken, secondToken);
        Assert.Equal(2, await dbContext.PasswordResetTokens.CountAsync(x => x.UserId == user.Id));
        Assert.Equal(1, await dbContext.PasswordResetTokens.CountAsync(x => x.UserId == user.Id && x.UsedUtc == null));

        await service.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = user.Email,
            Token = secondToken!,
            NewPassword = "NewPassword123!"
        }, CancellationToken.None);

        var refreshedUser = await dbContext.Users.SingleAsync(x => x.Id == user.Id);
        var refreshedTokens = await dbContext.PasswordResetTokens.Where(x => x.UserId == user.Id).ToListAsync();
        var refreshSessions = await dbContext.RefreshTokens.Where(x => x.UserId == user.Id).ToListAsync();

        Assert.Equal(PasswordVerificationResult.Success, passwordHasher.VerifyHashedPassword(refreshedUser, refreshedUser.PasswordHash, "NewPassword123!"));
        Assert.All(refreshedTokens, token => Assert.NotNull(token.UsedUtc));
        Assert.All(refreshSessions, session => Assert.NotNull(session.RevokedUtc));

        await Assert.ThrowsAsync<AuthException>(() => service.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "OldPassword123!"
        }, "127.0.0.1", "tests", CancellationToken.None));

        var newLogin = await service.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "NewPassword123!"
        }, "127.0.0.1", "tests", CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(newLogin.Response.AccessToken));
        Assert.NotEqual(login.RefreshToken, newLogin.RefreshToken);

        await Assert.ThrowsAsync<FinanceTracker.Application.Common.ValidationException>(() => service.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = user.Email,
            Token = secondToken,
            NewPassword = "AnotherPassword123!"
        }, CancellationToken.None));
    }

    [Fact]
    public async Task SettingsPreferences_PersistThemeAndDefaultValues()
    {
        await using var database = new SqliteTestDatabase();
        await using var dbContext = database.CreateContext();
        var passwordHasher = new PasswordHasher<FinanceTracker.Domain.Entities.User>();
        var user = TestData.AddUser(dbContext, "settings@example.com");
        user.PasswordHash = passwordHasher.HashPassword(user, "Password123!");
        var account = TestData.AddAccount(dbContext, user.Id, "Primary", 500m);
        await dbContext.SaveChangesAsync();

        var service = new SettingsService(dbContext, passwordHasher);

        var initial = await service.GetAsync(user.Id, CancellationToken.None);
        Assert.Equal("slate", initial.Preferences.Theme);

        var updatedPreferences = await service.UpdatePreferencesAsync(user.Id, new UpdatePreferencesRequest
        {
            PreferredCurrencyCode = "usd",
            DateFormat = "yyyy-MM-dd",
            LandingPage = "/reports",
            Theme = "dark"
        }, CancellationToken.None);

        var updatedFinancialDefaults = await service.UpdateFinancialDefaultsAsync(user.Id, new UpdateFinancialDefaultsRequest
        {
            DefaultAccountId = account.Id,
            DefaultPaymentMethod = "UPI",
            DefaultBudgetAlertThresholdPercent = 85
        }, CancellationToken.None);

        var reloaded = await service.GetAsync(user.Id, CancellationToken.None);

        Assert.Equal("USD", updatedPreferences.PreferredCurrencyCode);
        Assert.Equal("dark", updatedPreferences.Theme);
        Assert.Equal("/reports", reloaded.Preferences.LandingPage);
        Assert.Equal("dark", reloaded.Preferences.Theme);
        Assert.Equal(account.Id, updatedFinancialDefaults.DefaultAccountId);
        Assert.Equal("Primary", reloaded.FinancialDefaults.DefaultAccountName);
        Assert.Equal("UPI", reloaded.FinancialDefaults.DefaultPaymentMethod);
        Assert.Equal(85, reloaded.FinancialDefaults.DefaultBudgetAlertThresholdPercent);
    }

    private static TokenGenerator CreateTokenGenerator()
        => new(Options.Create(new JwtOptions
        {
            Issuer = "tests",
            Audience = "tests",
            SigningKey = "12345678901234567890123456789012",
            AccessTokenLifetimeMinutes = 15,
            RefreshTokenLifetimeDays = 7,
            RefreshCookieName = "ft_refresh",
            RequireHttpsMetadata = false
        }));
}
