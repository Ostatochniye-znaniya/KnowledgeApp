using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using KnowledgeApp.DataAccess.Context;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.DocumentGenerator;

public sealed class OrderGenerator
{
    private readonly KnowledgeTestDbContext _ctx;
    public OrderGenerator(KnowledgeTestDbContext ctx) => _ctx = ctx;

    public async Task<byte[]> GenerateAsync(DateOnly? date = null)
    {
        date ??= DateOnly.FromDateTime(DateTime.Today);

        string[] active = { "Запланировано", "Назначено", "Проведено" };

        // — тащим свежее Testing + навигацию до факультетов —
        var tests = await _ctx.Testings
            .Where(t => t.ScheduledDate != null && active.Contains(t.Status))
            .Include(t => t.Group)               
                .ThenInclude(g => g!.StudyProgram) 
                    .ThenInclude(p => p.Department)
                        .ThenInclude(d => d.Faculty)
            .ToListAsync();

        if (!tests.Any())
            throw new InvalidOperationException("Нет активных записей Testing → приказ не о чем.");

        // диапазон дат проверки
        var minDate = tests.Min(t => t.ScheduledDate)!.Value;
        var maxDate = tests.Max(t => t.ScheduledDate)!.Value;

        // сгруппируем по факультету
        var byFaculty = tests
            .GroupBy(t => t.Group!.StudyProgram!.Department!.Faculty!.FacultyName)
            .Select(g => new
            {
                Faculty = g.Key,
                Groups = g.Select(t => t.Group!.GroupNumber).Distinct().OrderBy(s => s)
            })
            .OrderBy(f => f.Faculty)
            .ToList();

        // — создаём DOCX —
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body());
            var body = main.Document.Body;

            // 0. шапка министерства
            AppendCentered(body,
                "МИНИСТЕРСТВО НАУКИ И ВЫСШЕГО ОБРАЗОВАНИЯ РОССИЙСКОЙ ФЕДЕРАЦИИ", 10);
            AppendCentered(body,
                "ФЕДЕРАЛЬНОЕ ГОСУДАРСТВЕННОЕ АВТОНОМНОЕ ОБРАЗОВАТЕЛЬНОЕ УЧРЕЖДЕНИЕ ВЫСШЕГО ОБРАЗОВАНИЯ", 10);
            AppendCentered(body,
                "«МОСКОВСКИЙ ПОЛИТЕХНИЧЕСКИЙ УНИВЕРСИТЕТ»", 11, true);
            AppendCentered(body,
                "(МОСКОВСКИЙ ПОЛИТЕХ)", 10, true);

            // 1. ПРИКАЗ
            body.Append(new Paragraph(
                new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                new Run(new RunProperties(new Bold(), new FontSize { Val = "36" }),
                        new Text("ПРИКАЗ"))));

            // 2. дата / номер
            var tblInfo = new Table(
                new TableProperties(new TableBorders(
                    new InsideHorizontalBorder { Val = BorderValues.None },
                    new InsideVerticalBorder { Val = BorderValues.None },
                    new TopBorder { Val = BorderValues.None },
                    new BottomBorder { Val = BorderValues.None },
                    new LeftBorder { Val = BorderValues.None },
                    new RightBorder { Val = BorderValues.None })),
                new TableRow(
                    new TableCell(new Paragraph(new Run(
                        new Text(date.Value.ToString("dd.MM.yyyy"))))),
                    new TableCell(new Paragraph(new Run(new Text("№ ________"))))));
            body.Append(tblInfo);

            // 3. тема
            body.Append(new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines { After = "200" }),
                new Run(new Text("О проведении анкетирования") { Break = BreakValues.TextWrapping }),
                new Run(new Text("и проверки остаточных знаний обучающихся"))));

            // 4. вводная часть
            body.Append(new Paragraph(new Run(new Text(
                "В целях оценки качества подготовки обучающихся за период 2023/2024 учебного года … (сокращено)"
            ))));

            // 5. ПРИКАЗЫВАЮ
            body.Append(new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines { After = "200" }),
                new Run(new RunProperties(new Bold()), new Text("ПРИКАЗЫВАЮ:"))));

            // 5.1 пункт 1
            body.Append(MakeNumberedParagraph("1.", $"Провести анкетирование обучающихся в сроки c 30.05.2024 по 04.06.2024."));

            // 5.2 пункт 2
            body.Append(MakeNumberedParagraph("2.", $"Провести проверку остаточных знаний обучающихся по дисциплинам, " +
                                                   $"освоенным в осеннем семестре, в сроки c {minDate:dd.MM.yyyy} по {maxDate:dd.MM.yyyy}."));

            int subNo = 1;
            foreach (var fac in byFaculty)
            {
                body.Append(MakeNumberedParagraph($"2.{subNo}.", $"Директору факультета {fac.Faculty}:"));
                body.Append(MakeDashParagraph("- организовать и провести Проверку;"));
                body.Append(MakeDashParagraph("- назначить ответственных лиц;"));
                body.Append(MakeDashParagraph("- подготовить отчёты по результатам Проверки;"));
                subNo++;
            }

            // 6. исполнитель
            body.Append(new Paragraph(
                new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                new Run(new Text("Исп.: А.Б. Максимов  ИД 2671645"))));

            main.Document.Save();
        }

        return ms.ToArray();
    }

    /*──────────────── helpers ───────────────*/
    private static void AppendCentered(Body body, string text, int fsPt, bool bold = false)
    {
        var runProps = new RunProperties(new FontSize { Val = (fsPt * 2).ToString() });
        if (bold) runProps.Bold = new Bold();

        body.Append(new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
            new Run(runProps, new Text(text))));
    }

    private static Paragraph MakeNumberedParagraph(string num, string text) =>
        new(new Run(new RunProperties(new Bold()), new Text(num)),
            new Run(new Text(" " + text)));

    private static Paragraph MakeDashParagraph(string text) =>
        new(new Run(new Text("   - " + text)));

    private static string SemesterName(DateOnly d) =>
        d.Month <= 6 ? $"Весна {d.Year}" : $"Осень {d.Year}";
}
