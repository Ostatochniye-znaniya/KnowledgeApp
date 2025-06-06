using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using KnowledgeApp.DataAccess.Context;
using KnowledgeApp.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.DocumentGenerator;

public sealed class GroupsReportGenerator
{
    private readonly KnowledgeTestDbContext _ctx;
    public GroupsReportGenerator(KnowledgeTestDbContext ctx) => _ctx = ctx;

    /*──────────────────────── PUBLIC ───────────────────────*/
    public async Task<byte[]> GenerateAsync()
    {
        await using var ms = new MemoryStream();

        /* 1. текущий семестр */
        DateTime today = DateTime.Today;
        string season = today.Month <= 6 ? "Весна" : "Осень";
        int year = today.Year;
        string curSem = $"{season} {year}";
        string[] active = { "Запланировано", "Назначено", "Проведено" };

        /* 2. id групп с активным тестом */
        var blueIds = new HashSet<int>(
            (await _ctx.Testings
                .Where(t => t.ScheduledDate != null && active.Contains(t.Status))
                .Select(t => new { t.GroupId, t.ScheduledDate })
                .ToListAsync())
            .Where(t => SemesterName(t.ScheduledDate!.Value) == curSem)
            .Select(t => t.GroupId!.Value));

        /* 3. иерархия */
        var faculties = await _ctx.Faculties
            .Include(f => f.Departments)
                .ThenInclude(d => d.StudyPrograms)
                    .ThenInclude(p => p.StudyGroups)
                        .ThenInclude(g => g.Testings)
            .ToListAsync();

        /* 4. DOCX */
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new Document();
            var body = main.Document.AppendChild(new Body());

            int fNo = 1;
            int totalGroups = 0;

            foreach (var fac in faculties)
            {
                /* ─ факультет-хедер ─ */
                body.Append(FacultyTable($"{fNo}. {fac.FacultyName}"));

                int pNo = 1;
                foreach (var prog in fac.Departments.SelectMany(d => d.StudyPrograms))
                {
                    body.Append(BuildProgramTable(fNo, pNo, prog, season, year, blueIds));
                    totalGroups += prog.StudyGroups.Count;
                    pNo++;
                }
                fNo++;
            }

            /* ─ итоговая строка ─ */
            body.Append(TotalTable(totalGroups, totalGroups * 2));

            body.Append(CreateSectionProps());
            main.Document.Save();
        }

        return ms.ToArray();
    }

    /*───────────────────── TABLES ─────────────────────*/
    private static Table FacultyTable(string text)
    {
        var tbl = CreateStyledTable();
        tbl.Append(MergedRow(text, "FFF2CC", true));
        return tbl;
    }

    private static Table TotalTable(int groups, int discs)
    {
        var tbl = CreateStyledTable();
        var run = new Run(new RunProperties(new Bold()),
                          new Text($"ВСЕГО: {groups} УЧЕБНЫХ ГРУПП ({discs} дисциплин)"));
        tbl.Append(new TableRow(new TableCell(
            new TableCellProperties(
                new GridSpan { Val = 5 },
                new Shading { Val = ShadingPatternValues.Clear, Fill = "FFF2CC" }),
            new Paragraph(run))));
        return tbl;
    }

    private static Table BuildProgramTable(int fNo, int pNo,
                                           StudyProgram prog,
                                           string season, int year,
                                           HashSet<int> blueIds)
    {
        var tbl = CreateStyledTable();

        tbl.Append(MergedRow(
            $"{fNo}.{pNo}. Образовательная программа «{prog.Name}»",
            "C5E0B3", true));

        tbl.Append(HeaderRow());

        int gNo = 1;
        foreach (var g in prog.StudyGroups.OrderBy(g => g.GroupNumber))
        {
            tbl.Append(GroupRow(fNo, pNo, gNo++, prog, g,
                                season, year,
                                blueIds.Contains(g.Id)));
        }
        return tbl;
    }

    /*───────────────────── ROW helpers ─────────────────────*/
    private static TableRow HeaderRow()
    {
        TableCell H(string t) => HeaderCell(t);
        return new TableRow(new TableRowProperties(new TableHeader()),
            H("№"),
            H("Группа"),
            H("Шифр и наименование направления (специальности)"),
            H("Период проверки"),
            H("Примечание"));
    }

    private static TableRow GroupRow(int fNo, int pNo, int gNo,
                                     StudyProgram prog, StudyGroup grp,
                                     string season, int year,
                                     bool blue)
    {
        string fill = blue ? "DDEBF7" : null;
        return new TableRow(
            Cell($"{fNo}.{pNo}.{gNo}", fill, JustificationValues.Center),
            Cell(grp.GroupNumber, fill, JustificationValues.Center),
            Cell($"{prog.CypherOfTheDirection} {prog.Name}", fill, JustificationValues.Left),
            Cell(PeriodText(grp, season, year), fill, JustificationValues.Left),
            Cell(string.Empty, fill, JustificationValues.Left));
    }

    /*───────────────────── CELL helpers ───────────────────*/
    private static TableRow MergedRow(string text, string fill, bool bold)
    {
        var props = new TableCellProperties(
            new GridSpan { Val = 5 },
            new Shading { Val = ShadingPatternValues.Clear, Fill = fill });

        var run = bold ? new Run(new RunProperties(new Bold()), new Text(text))
                       : new Run(new Text(text));

        var p = new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
            run);

        return new TableRow(new TableCell(props, p));
    }

    private static TableCell HeaderCell(string t) =>
        new(new TableCellProperties(new Shading
        { Val = ShadingPatternValues.Clear, Fill = "D9D9D9" }),
            new Paragraph(
                new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                new Run(new RunProperties(new Bold()), new Text(t))));

    private static TableCell Cell(string txt, string fill, JustificationValues align)
    {
        var props = new TableCellProperties();
        if (fill != null)
            props.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Fill = fill });

        return new TableCell(props,
            new Paragraph(
                new ParagraphProperties(new Justification { Val = align }),
                new Run(new Text(txt ?? string.Empty))));
    }

    /*───────────────────── Layout ─────────────────────*/
    private static Table CreateStyledTable() =>
        new(new TableProperties(
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "0" },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 8 },
                    new BottomBorder { Val = BorderValues.Single, Size = 8 },
                    new LeftBorder { Val = BorderValues.Single, Size = 8 },
                    new RightBorder { Val = BorderValues.Single, Size = 8 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })),
            new TableGrid(
                new GridColumn { Width = "1200" },  // № (шире)
                new GridColumn { Width = "1600" },  // Группа (шире)
                new GridColumn { Width = "3700" },
                new GridColumn { Width = "2300" },
                new GridColumn { Width = "1800" }));

    private static SectionProperties CreateSectionProps() =>
        new(new PageSize { Width = 11906, Height = 16838 },
            new PageMargin { Top = 1440, Right = 1440, Bottom = 1440, Left = 1440 });

    /*───────────────────── Business helpers ───────────*/
    private static string PeriodText(StudyGroup g, string curSeason, int curYear)
    {
        if (g.Testings == null) return string.Empty;

        static bool Active(string s) => s is "Запланировано" or "Назначено" or "Проведено";
        static (string s, int y) Sem(DateOnly? d) =>
            d == null ? (string.Empty, 0)
                      : d.Value.Month <= 6 ? ("Весна", d.Value.Year) : ("Осень", d.Value.Year);

        if (g.Testings.Any(t => Active(t.Status) && Sem(t.ScheduledDate) == (curSeason, curYear)))
            return $"Тестируется {Phrase($"{curSeason} {curYear}")}";

        static bool After((string s, int y) a, (string s, int y) b) =>
            a.y != b.y ? a.y > b.y : a.s == "Осень" && b.s == "Весна";

        var next = g.Testings
            .Where(t => Active(t.Status))
            .Select(t => Sem(t.ScheduledDate))
            .Where(s => After(s, (curSeason, curYear)))
            .OrderBy(s => s.y).ThenBy(s => s.s == "Весна" ? 0 : 1)
            .FirstOrDefault();

        return string.IsNullOrEmpty(next.s)
            ? string.Empty
            : $"Не тестируется {Phrase($"{curSeason} {curYear}")}; " +
              $"тестируется {Phrase($"{next.s} {next.y}")}";
    }

    private static string SemesterName(DateOnly d) =>
        d.Month <= 6 ? $"Весна {d.Year}" : $"Осень {d.Year}";

    private static string Phrase(string semName)
    {
        var parts = semName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return semName.ToLowerInvariant();
        string season = parts[0];
        string year = parts[1];
        return season switch
        {
            "Осень" => $"осенью {year}",
            "Весна" => $"весной {year}",
            _ => semName.ToLowerInvariant()
        };
    }
}
