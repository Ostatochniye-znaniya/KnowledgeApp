namespace KnowledgeApp.Domain.Enums;

public static class OrderStatuses
{
    public const string Draft = "draft";
    public const string Issued = "issued";

    public static bool IsValid(string status) => status is Draft or Issued;
}
