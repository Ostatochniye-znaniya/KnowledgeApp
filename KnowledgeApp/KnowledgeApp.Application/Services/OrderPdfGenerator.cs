using System.Text.RegularExpressions;
using iText.IO.Font;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using KnowledgeApp.Application.DTOs;
using KnowledgeApp.Domain.Entities;
using PageSize = iText.Kernel.Geom.PageSize;

namespace KnowledgeApp.Application.Services;

public class OrderPdfGenerator
{
    public const string OrderName = "О проведении анкетирования и проверки остаточных знаний обучающихся";

    private const string Signer = "Г.Х. Шарипзянова";
    private const float FontSize = 14f;
    private const float LineHeight = 16.1f;
    private const float Indent = 35.4f;
    private const float MarginTop = 56.7f;
    private const float MarginRight = 24.1f;
    private const float MarginBottom = 56.7f;
    private const float MarginLeft = 85.05f;
    private const float AppendixHeaderOffset = 241f;

    private static readonly Regex ShortWord = new(@"(?<=^|[\s(«])(в|и|о|с|к|у|по|до|на|за|от|из|№) ", RegexOptions.IgnoreCase);

    public byte[] Generate(OrderModel order)
    {
        using var stream = new MemoryStream();
        var pdf = new PdfDocument(new PdfWriter(stream));
        var document = new Document(pdf, PageSize.A4, false);
        document.SetMargins(MarginTop, MarginRight, MarginBottom, MarginLeft);

        var regular = LoadFont("times.ttf");
        var bold = LoadFont("timesbd.ttf");
        document.SetFont(regular).SetFontSize(FontSize);

        var isSpring = order.SemesterPart == 1;
        var currentSemester = isSpring ? "весеннем" : "осеннем";
        var previousSemester = isSpring ? "осеннем" : "весеннем";
        var academicYear = AcademicPeriod.FormatAcademicYear(order.SemesterYear, order.SemesterPart, null);
        var previousAcademicYear = $"{order.SemesterYear - 1}/{order.SemesterYear}";
        var controlHead = PersonName.Dative(order.EducationControlDepartmentHead);
        var methodicalHead = PersonName.Accusative(order.EducationalMethodicalCenterHead);

        AddOrder(document, bold, order, academicYear, currentSemester, previousSemester, controlHead, methodicalHead);

        AddAppendixHeader(document, 1, order);
        AddSurveyInstruction(document, bold);

        AddAppendixHeader(document, 2, order);
        AddRecommendations(document, bold, order, isSpring, previousSemester, previousAcademicYear);

        AddAppendixHeader(document, 3, order);
        AddReportForm(document, regular, bold, academicYear);

        var pageWidth = pdf.GetDefaultPageSize().GetWidth();
        var center = MarginLeft + (pageWidth - MarginLeft - MarginRight) / 2;
        for (var page = 1; page <= pdf.GetNumberOfPages(); page++)
        {
            PdfExecutorFooter.AddToPage(pdf, page, regular, MarginLeft);
            if (page > 1)
            {
                document.ShowTextAligned(new Paragraph(page.ToString()).SetFont(regular).SetFontSize(11), center,
                    pdf.GetDefaultPageSize().GetHeight() - 35, page, TextAlignment.CENTER, VerticalAlignment.TOP, 0);
            }
        }

        document.Close();
        return stream.ToArray();
    }

    private static void AddOrder(Document document, PdfFont bold, OrderModel order, string? academicYear,
        string currentSemester, string previousSemester, string controlHead, string methodicalHead)
    {
        document.Add(Centered("МИНИСТЕРСТВО НАУКИ И ВЫСШЕГО ОБРАЗОВАНИЯ РОССИЙСКОЙ ФЕДЕРАЦИИ", 9));
        document.Add(Centered("ФЕДЕРАЛЬНОЕ ГОСУДАРСТВЕННОЕ АВТОНОМНОЕ ОБРАЗОВАТЕЛЬНОЕ УЧРЕЖДЕНИЕ ВЫСШЕГО ОБРАЗОВАНИЯ", 8)
            .SetMarginTop(4));
        document.Add(Centered("«МОСКОВСКИЙ ПОЛИТЕХНИЧЕСКИЙ УНИВЕРСИТЕТ»", 12).SetFont(bold).SetMarginTop(3.5f));
        document.Add(Centered("(МОСКОВСКИЙ ПОЛИТЕХ)", 14).SetFont(bold));
        document.Add(Centered("ПРИКАЗ", 21).SetFont(bold).SetMarginTop(7));

        var requisites = new Table(new float[] { 96, 268, 22, 96 }).SetFixedLayout();
        requisites.AddCell(RequisiteCell(order.OrderDate.ToString("dd.MM.yyyy"), underlined: true));
        requisites.AddCell(RequisiteCell(string.Empty, underlined: false));
        requisites.AddCell(RequisiteCell("№", underlined: false).SetTextAlignment(TextAlignment.RIGHT));
        requisites.AddCell(RequisiteCell(order.Number, underlined: true));
        document.Add(requisites);

        document.Add(Line("О проведении анкетирования\nи проверки остаточных знаний\nобучающихся")
            .SetFixedLeading(14.5f).SetMarginTop(13).SetMarginBottom(27));

        document.Add(Body($"В целях оценки качества подготовки обучающихся за период {academicYear} учебного года " +
            "и стимулирования повышения компетентности и уровня квалификации педагогических работников " +
            "на основании Положения о внутренней системе оценки качества образовательной деятельности " +
            "и подготовке обучающихся по основным образовательным программам высшего образования – программ " +
            "бакалавриата, программ специалитета, программ магистратуры в Московском политехническом " +
            "университете и его филиалах, утвержденного приказом от 15.10.2022 № 1058-ОД"));
        document.Add(Body("ПРИКАЗЫВАЮ:").SetFont(bold));

        document.Add(Body("1. Провести анкетирование обучающихся в системе дистанционного обучения Московского " +
            "Политеха (далее СДО Московского Политеха) по итогам организации образовательного процесса " +
            $"в {currentSemester} семестре (далее – Анкетирование) в сроки с " +
            $"{LongDate(order.SurveyStartDate)} по {LongDate(order.SurveyEndDate)}."));
        document.Add(Body("1.1 Директорам институтов и деканам факультетов:"));
        document.Add(Body("- организовать и провести Анкетирование обучающихся;"));
        document.Add(Body("- назначить ответственных лиц за организационное обеспечение Анкетирования;"));
        document.Add(Body("- поручить проведение Анкетирования академическим тьюторам и кураторам групп " +
            "в соответствии с инструкцией (Приложение № 1)."));
        document.Add(Body($"1.2 Начальнику отдела контроля образовательного процесса {controlHead}:"));
        document.Add(Body("- обеспечить консультирование работников институтов и факультетов по вопросам Анкетирования;"));
        document.Add(Body("- обеспечить контроль за организацией и проведением Анкетирования;"));
        document.Add(Body("- подготовить сводный отчет по результатам Анкетирования в срок " +
            $"до {ShortDate(order.SurveySummaryDeadline)}."));

        document.Add(Body("2. Провести проверку остаточных знаний обучающихся по дисциплинам, освоенным " +
            $"в {previousSemester} семестре (далее – Проверка), в сроки с {ShortDate(order.TestingStartDate)} " +
            $"по {ShortDate(order.TestingEndDate)}."));
        document.Add(Body("2.1 Директорам институтов и деканам факультетов:"));
        document.Add(Body("- организовать и провести Проверку;"));
        document.Add(Body("- назначить ответственных лиц за организационное обеспечение Проверки;"));
        document.Add(Body($"- подготовить отчеты по результатам Проверки (Приложение № 3) {ReportDeadlines(order)} " +
            "в соответствии с рекомендациями (Приложение № 2)."));
        document.Add(Body($"2.2 Начальнику отдела контроля образовательного процесса {controlHead}:"));
        document.Add(Body("- обеспечить консультирование работников институтов и факультетов по вопросам Проверки;"));
        document.Add(Body("- обеспечить контроль за организацией и проведением Проверки;"));
        document.Add(Body("- подготовить сводный отчет по результатам Проверки в срок " +
            $"до {ShortDate(order.InspectionSummaryDeadline)}."));
        document.Add(Body("3. Контроль за исполнением настоящего приказа возложить на начальника центра " +
            $"по учебно-методической работе {methodicalHead}."));

        var signature = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
            .UseAllAvailableWidth()
            .SetMarginTop(LineHeight * 2)
            .SetKeepTogether(true);
        signature.AddCell(PlainCell("Проректор по учебной работе"));
        signature.AddCell(PlainCell(Signer).SetTextAlignment(TextAlignment.RIGHT));
        document.Add(signature);
    }

    private static void AddAppendixHeader(Document document, int number, OrderModel order)
    {
        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
        document.Add(Line($"Приложение № {number}\nк приказу Московского Политеха\n" +
                $"от {ShortDate(order.OrderDate)} г. № {order.Number}")
            .SetMarginLeft(AppendixHeaderOffset)
            .SetMarginBottom(LineHeight * 2));
    }

    private static void AddSurveyInstruction(Document document, PdfFont bold)
    {
        document.Add(Centered("Инструкция для проведения Анкетирования обучающихся\nМосковского Политеха", FontSize)
            .SetFont(bold).SetFixedLeading(LineHeight).SetMarginBottom(10));

        document.Add(Step(1, "Обучающийся входит в систему ЛМС по своему логину и паролю."));
        document.Add(Step(2, "В верхнем правом углу экрана нажимает кнопку «Мои курсы»."));
        document.Add(Step(3, "В списке выбирает курс «Анкетирование <Аббревиатура факультета>»."));
        document.Add(Step(4, "Ищет на странице раздел своей группы со списком дисциплин."));
        document.Add(Step(5, "Открывает каждую из дисциплин и отвечает на предложенные вопросы."));
        document.Add(Step(6, "Если в первом вопросе «Преподаватель» перечислено несколько фамилий, " +
            "то обучающийся выбирает того, кто проводил у него занятия."));
        document.Add(Step(7, "После выбора всех ответов нажимает на кнопку «Отправить свои ответы»."));
        document.Add(Step(8, "Нажимает кнопку «Продолжить» и возвращается на главную страницу курса для выбора " +
            "следующей дисциплины или переходит сразу к следующей дисциплине."));
    }

    private static void AddRecommendations(Document document, PdfFont bold, OrderModel order, bool isSpring,
        string previousSemester, string previousAcademicYear)
    {
        var firstCourse = isSpring ? 1 : 2;

        document.Add(Centered("Рекомендации по проведению проверки\nостаточных знаний обучающихся", FontSize)
            .SetFont(bold).SetFixedLeading(LineHeight));

        document.Add(Body("1. Проверка остаточных знаний проводится в три этапа:"));
        document.Add(Body("– 1 этап – подготовительный (подготовка тестовых материалов; формирование списков " +
            $"студентов; формирование графиков тестирования) с {LongDate(order.PreparationStartDate)} " +
            $"по {LongDate(order.PreparationEndDate)};"));
        document.Add(Body($"– 2 этап – этап проведения тестирования с {LongDate(order.TestingStartDate)} " +
            $"по {LongDate(order.TestingEndDate)};"));
        document.Add(Body("– 3 этап – этап обработки и анализа результатов, формирования отчета " +
            $"с {LongDate(order.ResultsProcessingStartDate)} по {LongDate(order.InspectionSummaryDeadline)}"));
        document.Add(Body("2. Проверка остаточных знаний осуществляется в отношении обучающихся учебных групп " +
            $"{firstCourse}–3 курсов очной формы обучения уровня бакалавриата и {firstCourse}–4 курсов очной формы " +
            "обучения уровня специалитета, 1 курса магистратуры."));
        document.Add(Body("3. Учебные группы для тестирования отбираются по принципу представительства одной " +
            "группы от одного профиля/специализации."));
        document.Add(Body("4. Для каждой группы определяются две дисциплины, изученные " +
            $"в {previousSemester} семестре {previousAcademicYear} учебного года."));
        document.Add(Body("5. При отборе дисциплин необходимо учитывать следующие условия:"));
        document.Add(Body("– тестирование проводится по двум дисциплинам, формирующим универсальные " +
            "(для 1 курса), общепрофессиональные и профессиональные компетенции;"));
        document.Add(Body("– в перечень дисциплин для тестирования должны быть включены дисциплины, изучение " +
            "которых проходило под руководством штатных преподавателей, у которых заканчивается срок " +
            "трудового договора в августе;"));
        document.Add(Body("– в перечень дисциплин для тестирования должны быть включены дисциплины (не менее " +
            "одной) выпускающих кафедр, ответственных за группу и профиль/специализацию."));
        document.Add(Body("6. Расписание тестирования по группам и аудиториям, согласованное с расписанием " +
            "учебных занятий, готовят и утверждают факультеты/институты."));
        document.Add(Body("7. В тестировании должны принять участие не менее 50% студентов."));
        document.Add(Body("8. Тестирование, обработка и анализ результатов, а также формирование отчетов " +
            "проводятся ответственными лицами, назначенными деканами факультетов/директорами институтов."));
        document.Add(Body("9. Отчет о проверке остаточных знаний представляется в отдел контроля " +
            "образовательного процесса в бумажном и электронном виде (на электронную почту: " +
            "e.v.davydova@mospolytech.ru)."));
    }

    private static void AddReportForm(Document document, PdfFont regular, PdfFont bold, string? academicYear)
    {
        GroupReportPdfTemplate.AddToDocument(document, new GroupReportDto
        {
            AcademicYear = academicYear ?? string.Empty
        }, regular, bold, isBlankTemplate: true);
    }

    private static string ReportDeadlines(OrderModel order) =>
        order.ElectronicReportDeadline == order.PaperReportDeadline
            ? $"в срок до {ShortDate(order.PaperReportDeadline)}"
            : $"в электронном виде в срок до {ShortDate(order.ElectronicReportDeadline)}, " +
              $"в бумажном виде в срок до {ShortDate(order.PaperReportDeadline)}";

    private static string ShortDate(DateOnly date) => date.ToString("dd.MM.yyyy");

    private static string LongDate(DateOnly date) => ShortDate(date);

    private static Paragraph Line(string text) =>
        new Paragraph(text).SetFixedLeading(LineHeight).SetMargin(0);

    private static Paragraph Body(string text) =>
        Line(Bind(text)).SetFirstLineIndent(Indent).SetTextAlignment(TextAlignment.JUSTIFIED).SetSpacingRatio(1f);

    private static string Bind(string text) =>
        ShortWord.Replace(text, "$1\u00A0").Replace(" – ", "\u00A0– ");

    private static Paragraph Centered(string text, float fontSize) =>
        new Paragraph(text).SetFontSize(fontSize).SetMultipliedLeading(1.15f).SetMargin(0)
            .SetTextAlignment(TextAlignment.CENTER);

    private static Paragraph Step(int number, string text) =>
        Line($"{number}.").SetFontSize(12).SetFixedLeading(14.7f).SetFirstLineIndent(Indent)
            .AddTabStops(new TabStop(Indent))
            .Add(new Tab())
            .Add(Bind(text))
            .SetKeepWithNext(true);

    private static Cell RequisiteCell(string text, bool underlined)
    {
        var cell = new Cell().Add(Line(text)).SetBorder(Border.NO_BORDER).SetPadding(0)
            .SetTextAlignment(TextAlignment.CENTER);
        return underlined ? cell.SetBorderBottom(new SolidBorder(0.5f)) : cell;
    }

    private static Cell PlainCell(string text) =>
        new Cell().Add(Line(text)).SetBorder(Border.NO_BORDER).SetPadding(0);

    private static PdfFont LoadFont(string fileName) =>
        PdfFontFactory.CreateFont(Path.Combine(AppContext.BaseDirectory, "Fonts", fileName), PdfEncodings.IDENTITY_H,
            PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
}
