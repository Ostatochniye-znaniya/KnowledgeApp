using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Application.Services;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.UnitTests.Services;

public class DocumentStatusInfoTests
{
    [Theory]
    [InlineData(DocumentStatus.Pending, "pending")]
    [InlineData(DocumentStatus.Signed, "signed")]
    [InlineData(DocumentStatus.Rejected, "rejected")]
    public void Code_IsLowercaseName(DocumentStatus status, string expected)
    {
        Assert.Equal(expected, DocumentStatusInfo.Code(status));
    }

    [Theory]
    [InlineData("signed", DocumentStatus.Signed)]
    [InlineData(" SIGNED ", DocumentStatus.Signed)]
    [InlineData("Rejected", DocumentStatus.Rejected)]
    [InlineData("pending", DocumentStatus.Pending)]
    public void Parse_AcceptsCodeInAnyCase(string value, DocumentStatus expected)
    {
        Assert.Equal(expected, DocumentStatusInfo.Parse(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyValue_ReturnsNull(string? value)
    {
        Assert.Null(DocumentStatusInfo.Parse(value));
    }

    [Theory]
    [InlineData("approved")]
    [InlineData("1")]
    public void Parse_UnknownValue_Throws(string value)
    {
        Assert.Throws<DocumentValidationException>(() => DocumentStatusInfo.Parse(value));
    }

    [Theory]
    [InlineData(DocumentStatus.Pending, false, "Ожидает подписи")]
    [InlineData(DocumentStatus.Signed, false, "Подписан")]
    [InlineData(DocumentStatus.Rejected, false, "Отклонён")]
    [InlineData(DocumentStatus.Pending, true, "Отправлен на проверку")]
    [InlineData(DocumentStatus.Signed, true, "Сделан")]
    [InlineData(DocumentStatus.Rejected, true, "Отправлен на доработку")]
    public void Label_DependsOnWorkflow(DocumentStatus status, bool reportWorkflow, string expected)
    {
        Assert.Equal(expected, DocumentStatusInfo.Label(status, reportWorkflow));
    }

    [Theory]
    [InlineData(null, null, "not_required", "Пока не требуется")]
    [InlineData(DocumentStatus.Pending, true, "not_required", "Пока не требуется")]
    [InlineData(DocumentStatus.Rejected, false, "not_required", "Пока не требуется")]
    [InlineData(DocumentStatus.Signed, null, "pending", "Ожидает сдачи")]
    [InlineData(DocumentStatus.Signed, false, "pending", "Ожидает сдачи")]
    [InlineData(DocumentStatus.Signed, true, "submitted", "Сдан")]
    public void PaperStatus_RequiredOnlyAfterSigning(
        DocumentStatus? electronic, bool? submitted, string expectedCode, string expectedLabel)
    {
        Assert.Equal(expectedCode, DocumentStatusInfo.PaperCode(electronic, submitted));
        Assert.Equal(expectedLabel, DocumentStatusInfo.PaperLabel(electronic, submitted));
    }

    [Fact]
    public void AsUtc_MarksValueAsUtcWithoutShifting()
    {
        var unspecified = new DateTime(2026, 11, 20, 10, 30, 0, DateTimeKind.Unspecified);

        var utc = DocumentStatusInfo.AsUtc(unspecified);

        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        Assert.Equal(unspecified.Ticks, utc.Ticks);
        Assert.Null(DocumentStatusInfo.AsUtc((DateTime?)null));
    }

    [Fact]
    public void Options_ListsAllStatusesWithLabels()
    {
        var options = DocumentStatusInfo.Options(reportWorkflow: true);

        Assert.Equal(new[] { "pending", "signed", "rejected" }, options.Select(o => o.Value));
        Assert.Equal("Сделан", options.Single(o => o.Value == "signed").Label);
    }
}
