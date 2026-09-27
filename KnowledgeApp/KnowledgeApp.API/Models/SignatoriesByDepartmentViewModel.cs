namespace KnowledgeApp.API.Models;

public class SignatoriesByDepartmentViewModel
{
    public List<SignatoryDepartmentViewModel> Departments { get; set; } = new();

    public int AssignmentCount => Departments.Sum(department => department.Signatories.Count);
}

public class SignatoryDepartmentViewModel
{
    public string Name { get; set; } = string.Empty;

    public List<SignatoryViewModel> Signatories { get; set; } = new();
}

public class SignatoryViewModel
{
    public string FullName { get; set; } = string.Empty;

    public string? JobName { get; set; }
}
