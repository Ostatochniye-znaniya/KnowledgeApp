using KnowledgeApp.Application.DTOs;

namespace KnowledgeApp.Application.Interfaces;

public interface IDocxGeneratorService
{
    byte[] GenerateBlankReportDocx();

    byte[] GenerateGroupReportDocx(GroupReportDto report);
}
