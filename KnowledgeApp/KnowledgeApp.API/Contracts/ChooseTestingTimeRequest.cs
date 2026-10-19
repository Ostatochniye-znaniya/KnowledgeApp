namespace KnowledgeApp.API.Contracts;

public sealed record ChooseTestingTimeRequest(DateOnly Date, int PairNumber);
