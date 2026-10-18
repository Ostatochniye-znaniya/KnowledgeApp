using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;

namespace KnowledgeApp.Application.Services;

internal static class PdfExecutorFooter
{
    private const float FontSize = 9;
    private const float LineSpacing = 11;

    private static readonly string[] Lines =
    {
        "О проведении проверки остаточных знаний обучающихся",
        "Исп.: Е.В. Давыдова",
        "ИД 3364791"
    };

    public static void AddToPage(PdfDocument pdf, int pageNumber, PdfFont font, float left)
    {
        var canvas = new PdfCanvas(pdf.GetPage(pageNumber));
        canvas.BeginText()
            .SetFontAndSize(font, FontSize)
            .MoveText(left, 45)
            .ShowText(Lines[0])
            .MoveText(0, -LineSpacing)
            .ShowText(Lines[1])
            .MoveText(0, -LineSpacing)
            .ShowText(Lines[2])
            .EndText();
    }
}
