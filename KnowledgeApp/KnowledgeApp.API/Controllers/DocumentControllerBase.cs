using KnowledgeApp.Application.DTOs.Documents;
using KnowledgeApp.Application.Exceptions;

namespace KnowledgeApp.API.Controllers;

public abstract class DocumentControllerBase : BaseController
{
    protected const long UploadRequestLimit = 25L * 1024 * 1024;

    protected async Task<IResult> Execute(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (DocumentValidationException e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
        catch (DocumentAccessException e)
        {
            return Results.Json(new { error = e.Message }, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (DocumentNotFoundException e)
        {
            return Results.NotFound(new { error = e.Message });
        }
        catch (StoredFileMissingException e)
        {
            return Results.NotFound(new { error = e.Message });
        }
        catch (DocumentConflictException e)
        {
            return Results.Conflict(new { error = e.Message });
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<DocumentControllerBase>>()
                .LogError(e, "Ошибка в ручке документов {Path}", HttpContext.Request.Path);
            return Results.Problem("Внутренняя ошибка сервера");
        }
    }

    protected IResult PdfFile(DocumentFileDto file)
    {
        HttpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
        return Results.File(file.Content, file.ContentType, file.FileName);
    }
}
