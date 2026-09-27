namespace KnowledgeApp.Application.DTOs.Documents;

public class DocumentFileDto
{
    public Stream Content { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
}
