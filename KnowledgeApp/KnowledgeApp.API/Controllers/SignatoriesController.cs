using KnowledgeApp.API.Models;
using KnowledgeApp.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.API.Controllers;

public class SignatoriesController : Controller
{
    private readonly EmployeeRightsRequestService _employeeRightsRequestService;

    public SignatoriesController(EmployeeRightsRequestService employeeRightsRequestService)
    {
        _employeeRightsRequestService = employeeRightsRequestService;
    }

    [HttpGet]
    public async Task<IActionResult> ByDepartment()
    {
        var assignments = await _employeeRightsRequestService.GetActiveSignatoryAssignments();
        var departments = assignments
            .Select(assignment => new
            {
                Assignment = assignment,
                DepartmentName = string.IsNullOrWhiteSpace(assignment.StructuralDivision)
                    ? "Подразделение не указано"
                    : assignment.StructuralDivision.Trim()
            })
            .GroupBy(item => item.DepartmentName, StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase)
            .Select(group => new SignatoryDepartmentViewModel
            {
                Name = group.Key,
                Signatories = group.Select(item => new SignatoryViewModel
                {
                    FullName = string.IsNullOrWhiteSpace(item.Assignment.FullName)
                        ? "ФИО не указано"
                        : item.Assignment.FullName,
                    JobName = item.Assignment.JobName
                }).ToList()
            })
            .ToList();

        return View(new SignatoriesByDepartmentViewModel
        {
            Departments = departments
        });
    }
}
