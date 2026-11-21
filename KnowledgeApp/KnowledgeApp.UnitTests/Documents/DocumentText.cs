using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace KnowledgeApp.UnitTests.Documents;

/// <summary>Извлечение текста из сгенерированных файлов для проверок в тестах.</summary>
internal static class DocumentText
{
    public static string FromPdf(byte[] pdf, out int pageCount)
    {
        using var document = new PdfDocument(new PdfReader(new MemoryStream(pdf)));
        pageCount = document.GetNumberOfPages();
        var text = new StringBuilder();
        for (var page = 1; page <= pageCount; page++)
            text.AppendLine(PdfTextExtractor.GetTextFromPage(document.GetPage(page)));
        return Normalize(text.ToString());
    }

    public static string FromPdf(byte[] pdf) => FromPdf(pdf, out _);

    public static string FromDocx(byte[] docx)
    {
        using var document = WordprocessingDocument.Open(new MemoryStream(docx), false);
        var body = document.MainDocumentPart?.Document.Body
                   ?? throw new InvalidDataException("В DOCX нет основного содержимого");
        return Normalize(string.Join(" ", body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text)));
    }

    // Переносы строк и неразрывные пробелы зависят от вёрстки, для проверок содержимого они не важны
    private static string Normalize(string text) =>
        Regex.Replace(text.Replace(' ', ' '), @"\s+", " ");
}
