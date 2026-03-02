using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeApp.Infrastructure.Entities;

[Table("employee_rigths_requests")]
public class EmployeeRightsRequest
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string? StructuralDivision { get; set; }
    public string? JobName { get; set; }
    public DateOnly JobStart { get; set; }
    public DateOnly JobEnd { get; set; }
    public bool IsActive { get; set; }
    public string? CategoryName { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }
}