namespace KnowledgeApp.API.Contracts;

public class ScheduleDocumentUploadRequest
{
    public int SemesterId { get; set; }

    public IFormFile? File { get; set; }
}
