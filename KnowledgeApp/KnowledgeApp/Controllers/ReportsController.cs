using KnowledgeApp.DocumentGenerator;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly GroupsReportGenerator _gen;
    public ReportsController(GroupsReportGenerator gen) => _gen = gen;

    [HttpGet("Download")]
    public async Task<IActionResult> Download()
    {
        var bytes = await _gen.GenerateAsync();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "GroupsReport.docx");
    }
}
