using System.ComponentModel.DataAnnotations;

namespace KnowledgeApp.API.Contracts;

/// <summary>Format and delivery details selected by the responsible teacher.</summary>
public sealed record UpdateTestingDeliveryRequest
{
    public required bool IsInPerson { get; init; }

    [StringLength(255)]
    public string? Room { get; init; }

    [StringLength(2048)]
    public string? LmsUrl { get; init; }
}
