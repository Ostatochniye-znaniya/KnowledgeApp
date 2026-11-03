namespace KnowledgeApp.Application.DTOs.Admin;

public sealed record SystemSettingsDto(
    string? SupportEmail,
    string? SupportPhone,
    string? WelcomeText,
    string? AccessRequestInstructions,
    string? NotificationTemplate,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);

public sealed record PublicSettingsDto(
    string? SupportEmail,
    string? SupportPhone,
    string? WelcomeText,
    string? AccessRequestInstructions);

public sealed record UpdateSystemSettingsCommand(
    string? SupportEmail,
    string? SupportPhone,
    string? WelcomeText,
    string? AccessRequestInstructions,
    string? NotificationTemplate);
