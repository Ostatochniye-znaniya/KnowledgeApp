using KnowledgeApp.Application.DTOs;

namespace KnowledgeApp.Application.Interfaces;

public interface IDocxGeneratorService
{
    byte[] GenerateGroupReportDocx(GroupReportDto report);
}
