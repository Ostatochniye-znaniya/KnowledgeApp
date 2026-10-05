using System.Text;
using KnowledgeApp.Application.Exceptions;

namespace KnowledgeApp.Application.Services;

public sealed class PdfUpload : IDisposable
{
    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();

    public static readonly int MaxSizeMb =
        int.TryParse(Environment.GetEnvironmentVariable("MAX_UPLOAD_SIZE_MB"), out var mb) && mb > 0 ? mb : 20;

    private static long MaxBytes => MaxSizeMb * 1024L * 1024L;

    public MemoryStream Content { get; }
    public string FileName { get; }
    public long Size => Content.Length;

    private PdfUpload(MemoryStream content, string fileName)
    {
        Content = content;
        FileName = fileName;
    }

    public static void EnsureDeclaredSize(long? declaredSize)
    {
        if (declaredSize > MaxBytes)
            throw new DocumentValidationException($"Файл больше {MaxSizeMb} МБ");
    }

    public static async Task<PdfUpload> ReadAsync(Stream source, string? fileName, long? declaredSize,
        CancellationToken cancellationToken = default)
    {
        var name = SanitizeFileName(fileName);
        if (string.IsNullOrEmpty(name))
            throw new DocumentValidationException("Файл не передан");

        if (!name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new DocumentValidationException("Можно загрузить только PDF-файл");

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
                    throw new DocumentValidationException($"Файл больше {MaxSizeMb} МБ");
                buffer.Write(chunk, 0, read);
            }

            if (buffer.Length == 0)
                throw new DocumentValidationException("Файл пустой");

            if (!StartsWithPdfSignature(buffer))
                throw new DocumentValidationException("Файл не похож на PDF");
        }
        catch
        {
            buffer.Dispose();
            throw;
        }

        buffer.Position = 0;
        return new PdfUpload(buffer, name);
    }

    private static bool StartsWithPdfSignature(MemoryStream buffer) =>
        buffer.Length >= PdfSignature.Length
        && buffer.GetBuffer().AsSpan(0, PdfSignature.Length).SequenceEqual(PdfSignature);

    internal static string SanitizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return string.Empty;

        var name = fileName.Replace('\\', '/');
        name = name[(name.LastIndexOf('/') + 1)..];

        var cleaned = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (char.IsControl(ch) || IsBidiControl(ch) || "<>:\"|?*".Contains(ch)) continue;
            cleaned.Append(ch);
        }
        name = cleaned.ToString().Trim().TrimStart('.');

        const int maxLength = 200;
        if (name.Length > maxLength)
        {
            var extension = Path.GetExtension(name);
            var baseName = name[..^extension.Length];
            var info = new System.Globalization.StringInfo(baseName);
            var keep = Math.Min(info.LengthInTextElements, maxLength - extension.Length);
            name = info.SubstringByTextElements(0, keep) + extension;
            while (name.Length > maxLength)
            {
                info = new System.Globalization.StringInfo(name[..^extension.Length]);
                name = info.SubstringByTextElements(0, info.LengthInTextElements - 1) + extension;
            }
        }

        return name;
    }

    private static bool IsBidiControl(char ch) =>
        ch is '‎' or '‏' or >= '‪' and <= '‮' or >= '⁦' and <= '⁩';

    public void Dispose() => Content.Dispose();
}
