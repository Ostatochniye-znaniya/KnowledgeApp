namespace KnowledgeApp.Application.DTOs.Orders;

public class OrderEssentialsDto
{
    public int Id { get; set; }
    public string? Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly OrderDate { get; set; }
    public DateOnly TestingStartDate { get; set; }
    public DateOnly TestingEndDate { get; set; }
    public OrderSemesterDto Semester { get; set; } = new();
}

public class OrderSemesterDto
{
    public int Id { get; set; }
    public int SemesterYear { get; set; }
    public int SemesterPart { get; set; }
}
