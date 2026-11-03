namespace KnowledgeApp.Infrastructure.Entities;

public sealed class SystemSettings
{
    public int Id { get; set; }

    public string? SupportEmail { get; set; }

    public string? SupportPhone { get; set; }

    public string? WelcomeText { get; set; }

    public string? AccessRequestInstructions { get; set; }

    public string? NotificationTemplate { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? UpdatedByUserId { get; set; }

    public string? UpdatedByName { get; set; }
}
