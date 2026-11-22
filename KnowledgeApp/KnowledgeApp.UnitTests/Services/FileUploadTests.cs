using System.IO.Compression;
using System.Text;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Application.Services;

namespace KnowledgeApp.UnitTests.Services;

public class PdfUploadTests
{
    private static readonly byte[] MinimalPdf = Encoding.ASCII.GetBytes("%PDF-1.7\n%test\n%%EOF");

    private static long MaxBytes => PdfUpload.MaxSizeMb * 1024L * 1024L;

    [Fact]
    public async Task ReadAsync_ValidPdf_CopiesContent()
    {
        using var upload = await PdfUpload.ReadAsync(new MemoryStream(MinimalPdf), "report.pdf", MinimalPdf.Length);

        Assert.Equal("report.pdf", upload.FileName);
        Assert.Equal(MinimalPdf.Length, upload.Size);
        Assert.Equal(0, upload.Content.Position);
        Assert.Equal(MinimalPdf, upload.Content.ToArray());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReadAsync_WithoutFileName_Throws(string? fileName)
    {
        var error = await Assert.ThrowsAsync<DocumentValidationException>(
            () => PdfUpload.ReadAsync(new MemoryStream(MinimalPdf), fileName, null));
        Assert.Equal("Файл не передан", error.Message);
    }

    [Theory]
    [InlineData("report.docx")]
    [InlineData("report.pdf.exe")]
    [InlineData("report")]
    public async Task ReadAsync_WrongExtension_Throws(string fileName)
    {
        var error = await Assert.ThrowsAsync<DocumentValidationException>(
            () => PdfUpload.ReadAsync(new MemoryStream(MinimalPdf), fileName, null));
        Assert.Equal("Можно загрузить только PDF-файл", error.Message);
    }

    [Fact]
    public async Task ReadAsync_EmptyFile_Throws()
    {
        var error = await Assert.ThrowsAsync<DocumentValidationException>(
            () => PdfUpload.ReadAsync(new MemoryStream(), "empty.pdf", 0));
        Assert.Equal("Файл пустой", error.Message);
    }

    [Fact]
    public async Task ReadAsync_ContentWithoutPdfSignature_Throws()
    {
        var fake = Encoding.UTF8.GetBytes("<html>not a pdf</html>");

        var error = await Assert.ThrowsAsync<DocumentValidationException>(
            () => PdfUpload.ReadAsync(new MemoryStream(fake), "fake.pdf", fake.Length));
        Assert.Equal("Файл не похож на PDF", error.Message);
    }

    [Fact]
    public async Task ReadAsync_DeclaredSizeOverLimit_ThrowsBeforeReading()
    {
        await Assert.ThrowsAsync<DocumentValidationException>(
            () => PdfUpload.ReadAsync(new ThrowingStream(), "big.pdf", MaxBytes + 1));
    }

    [Fact]
    public async Task ReadAsync_ActualSizeOverLimit_Throws()
    {
        // Заявленный размер может быть занижен клиентом, поэтому лимит проверяется и при чтении
        var content = new byte[MaxBytes + 1];
        MinimalPdf.CopyTo(content, 0);

        await Assert.ThrowsAsync<DocumentValidationException>(
            () => PdfUpload.ReadAsync(new MemoryStream(content), "big.pdf", 10));
    }

    [Theory]
    [InlineData("../../etc/report.pdf", "report.pdf")]
    [InlineData("C:\\Users\\student\\report.pdf", "report.pdf")]
    [InlineData("re<po>rt?.pdf", "report.pdf")]
    [InlineData("..hidden.pdf", "hidden.pdf")]
    [InlineData("отчёт\u202Efdp.pdf", "отчётfdp.pdf")] // управляющий символ направления текста
    public async Task ReadAsync_SanitizesFileName(string fileName, string expected)
    {
        using var upload = await PdfUpload.ReadAsync(new MemoryStream(MinimalPdf), fileName, null);

        Assert.Equal(expected, upload.FileName);
    }

    [Fact]
    public async Task ReadAsync_TruncatesLongFileNameKeepingExtension()
    {
        var fileName = new string('а', 300) + ".pdf";

        using var upload = await PdfUpload.ReadAsync(new MemoryStream(MinimalPdf), fileName, null);

        Assert.Equal(200, upload.FileName.Length);
        Assert.EndsWith(".pdf", upload.FileName);
    }

    [Fact]
    public async Task Dispose_ReleasesContent()
    {
        var upload = await PdfUpload.ReadAsync(new MemoryStream(MinimalPdf), "report.pdf", null);

        upload.Dispose();

        Assert.Throws<ObjectDisposedException>(() => upload.Content.ReadByte());
    }

    [Fact]
    public void EnsureDeclaredSize_AllowsLimitAndUnknownSize()
    {
        PdfUpload.EnsureDeclaredSize(null);
        PdfUpload.EnsureDeclaredSize(MaxBytes);
        Assert.Throws<DocumentValidationException>(() => PdfUpload.EnsureDeclaredSize(MaxBytes + 1));
    }

    /// <summary>Поток, из которого нельзя читать: проверка размера должна сработать раньше.</summary>
    private sealed class ThrowingStream : MemoryStream
    {
        public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException();
    }
}

public class DocxUploadTests
{
    [Fact]
    public async Task ReadAsync_ValidDocx_CopiesContent()
    {
        var docx = CreateZip("[Content_Types].xml", "word/document.xml");

        using var upload = await DocxUpload.ReadAsync(new MemoryStream(docx), "schedule.docx", docx.Length);

        Assert.Equal("schedule.docx", upload.FileName);
        Assert.Equal(0, upload.Content.Position);
        Assert.Equal(docx, upload.Content.ToArray());
    }

    [Fact]
    public async Task ReadAsync_WrongExtension_Throws()
    {
        var docx = CreateZip("[Content_Types].xml", "word/document.xml");

        var error = await Assert.ThrowsAsync<DocumentValidationException>(
            () => DocxUpload.ReadAsync(new MemoryStream(docx), "schedule.pdf", null));
        Assert.Equal("Можно загрузить только DOCX-файл", error.Message);
    }

    [Fact]
    public async Task ReadAsync_EmptyFile_Throws()
    {
        var error = await Assert.ThrowsAsync<DocumentValidationException>(
            () => DocxUpload.ReadAsync(new MemoryStream(), "schedule.docx", null));
        Assert.Equal("Файл пустой", error.Message);
    }

    [Fact]
    public async Task ReadAsync_NotZip_Throws()
    {
        var content = Encoding.UTF8.GetBytes("plain text renamed to docx");

        var error = await Assert.ThrowsAsync<DocumentValidationException>(
            () => DocxUpload.ReadAsync(new MemoryStream(content), "schedule.docx", null));
        Assert.Equal("Файл не является корректным документом DOCX", error.Message);
    }

    [Theory]
    [InlineData("[Content_Types].xml")] // нет word/document.xml, например переименованный xlsx
    [InlineData("word/document.xml")]
    public async Task ReadAsync_ZipWithoutWordParts_Throws(string onlyEntry)
    {
        var zip = CreateZip(onlyEntry);

        var error = await Assert.ThrowsAsync<DocumentValidationException>(
            () => DocxUpload.ReadAsync(new MemoryStream(zip), "schedule.docx", null));
        Assert.Equal("Файл не является документом DOCX", error.Message);
    }

    [Fact]
    public async Task ReadAsync_DeclaredSizeOverLimit_Throws()
    {
        var docx = CreateZip("[Content_Types].xml", "word/document.xml");

        await Assert.ThrowsAsync<DocumentValidationException>(
            () => DocxUpload.ReadAsync(new MemoryStream(docx), "schedule.docx", PdfUpload.MaxSizeMb * 1024L * 1024L + 1));
    }

    [Fact]
    public async Task Dispose_ReleasesContent()
    {
        var docx = CreateZip("[Content_Types].xml", "word/document.xml");
        var upload = await DocxUpload.ReadAsync(new MemoryStream(docx), "schedule.docx", null);

        upload.Dispose();

        Assert.Throws<ObjectDisposedException>(() => upload.Content.ReadByte());
    }

    private static byte[] CreateZip(params string[] entries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in entries)
            {
                using var writer = new StreamWriter(archive.CreateEntry(entry).Open());
                writer.Write("<xml/>");
            }
        }
        return stream.ToArray();
    }
}
