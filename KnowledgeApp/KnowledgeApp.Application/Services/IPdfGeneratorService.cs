using KnowledgeApp.Application.DTOs;

namespace KnowledgeApp.Application.Interfaces
{
    public interface IPdfGeneratorService
    {
        byte[] GenerateTestingSchedulePdf(List<TestingScheduleDto> schedule, string? facultyName = null, int facultyId = 0, string? semesterPeriod = null, int semesterId = 0);
        byte[] GenerateGroupReportPdf(
            IReadOnlyList<GroupReportPageDto> pages,
            string? orderNumber = null,
            DateOnly? orderDate = null,
            int appendixNumber = 2,
            int firstPageNumber = 3);
        byte[] GenerateGroupReportPagesPdf(
            IReadOnlyList<GroupReportPageDto> pages,
            string orderNumber,
            DateOnly orderDate,
            int appendixNumber,
            int firstPageNumber);
    }
}