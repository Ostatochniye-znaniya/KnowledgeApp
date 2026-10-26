namespace KnowledgeApp.API.Contracts;

/// <summary>Schedule and delivery details to check before saving a testing appointment.</summary>
public sealed record ValidateTestingScheduleRequest(
    DateOnly Date,
    int PairNumber,
    bool IsInPerson,
    string? Room,
    string? LmsUrl);
