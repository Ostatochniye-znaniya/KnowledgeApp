using System.ComponentModel.DataAnnotations;

namespace KnowledgeApp.API.Contracts;

public sealed record TestingDeliveryRequest
{
    [Required]
    public required bool IsInPerson { get; init; }

    [MaxLength(255)]
    public string? Room { get; init; }

    [MaxLength(2048)]
    public string? LmsUrl { get; init; }
}
