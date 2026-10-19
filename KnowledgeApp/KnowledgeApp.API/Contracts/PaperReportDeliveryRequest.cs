namespace KnowledgeApp.API.Contracts;

/// <summary>Sets whether the signed paper copy of a report has been received.</summary>
public sealed record PaperReportDeliveryRequest(bool Submitted);
