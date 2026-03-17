namespace FinanceTracker.Application.Settings.DTOs;

public sealed record ProfileSettingsDto(Guid Id, string Email, string FirstName, string LastName);