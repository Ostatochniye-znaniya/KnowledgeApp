namespace KnowledgeApp.API.Contracts.Admin;

public sealed record UpdateSystemSettingsRequest(
    string? SupportEmail,
    string? SupportPhone,
    string? WelcomeText,
    string? AccessRequestInstructions,
    string? NotificationTemplate);
