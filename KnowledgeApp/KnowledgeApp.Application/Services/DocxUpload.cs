using System.IO.Compression;
using KnowledgeApp.Application.Exceptions;

namespace KnowledgeApp.Application.Services;

public sealed class DocxUpload : IDisposable
{
    private static long MaxBytes => PdfUpload.MaxSizeMb * 1024L * 1024L;

    public MemoryStream Content { get; }
    public string FileName { get; }

    private DocxUpload(MemoryStream content, string fileName)
    {
        Content = content;
        FileName = fileName;
    }

    public static void EnsureDeclaredSize(long? declaredSize)
    {
        if (declaredSize > MaxBytes)
            throw new DocumentValidationException($"Файл больше {PdfUpload.MaxSizeMb} МБ");
    }

    public static async Task<DocxUpload> ReadAsync(
        Stream source,
        string? fileName,
        long? declaredSize,
        CancellationToken cancellationToken = default)
    {
        var name = PdfUpload.SanitizeFileName(fileName);
        if (string.IsNullOrEmpty(name))
            throw new DocumentValidationException("Файл не передан");

        if (!name.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            throw new DocumentValidationException("Можно загрузить только DOCX-файл");

        EnsureDeclaredSize(declaredSize);

        var capacity = declaredSize is > 0 ? (int)declaredSize.Value : 0;
        var buffer = new MemoryStream(capacity);
        try
        {
            var chunk = new byte[81920];
            int read;
            while ((read = await source.ReadAsync(chunk, cancellationToken)) > 0)
            {
                if (buffer.Length + read > MaxBytes)
                    throw new DocumentValidationException($"Файл больше {PdfUpload.MaxSizeMb} МБ");
                buffer.Write(chunk, 0, read);
            }

            if (buffer.Length == 0)
                throw new DocumentValidationException("Файл пустой");

            buffer.Position = 0;
            using var archive = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: true);
            if (archive.GetEntry("[Content_Types].xml") is null ||
                archive.GetEntry("word/document.xml") is null)
                throw new DocumentValidationException("Файл не является документом DOCX");
        }
        catch (InvalidDataException)
        {
            buffer.Dispose();
            throw new DocumentValidationException("Файл не является корректным документом DOCX");
        }
        catch
        {
            buffer.Dispose();
            throw;
        }

        buffer.Position = 0;
        return new DocxUpload(buffer, name);
    }

    public void Dispose() => Content.Dispose();
}
