using KnowledgeApp.Application.DTOs;

namespace KnowledgeApp.Application.Interfaces
{
    public interface IPdfGeneratorService
    {
        byte[] GenerateTestingSchedulePdf(List<TestingScheduleDto> schedule, string facultyName = null, int facultyId = 0, string semesterPeriod = null, int semesterId = 0);
    }
}