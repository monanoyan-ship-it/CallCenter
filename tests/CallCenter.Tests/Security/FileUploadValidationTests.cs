using CallCenter.Api.Security;
using Microsoft.AspNetCore.Http;

namespace CallCenter.Tests.Security;

public class FileUploadValidationTests
{
    [Fact]
    public async Task ValidateImageAsync_RejectsSpoofedContentType()
    {
        var file = CreateFormFile("fake.jpg", "image/jpeg", "not really an image"u8.ToArray());

        var result = await FileUploadValidation.ValidateImageAsync(file);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateImageAsync_DetectsPngByMagicBytes()
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };
        var file = CreateFormFile("photo.bin", "application/octet-stream", bytes);

        var result = await FileUploadValidation.ValidateImageAsync(file);

        result.Success.Should().BeTrue();
        result.ContentType.Should().Be("image/png");
        result.Extension.Should().Be(".png");
    }

    [Fact]
    public async Task ValidateExcelWorkbookAsync_RejectsMacroExtension()
    {
        var file = CreateFormFile("clients.xlsm", "application/vnd.ms-excel.sheet.macroEnabled.12", new byte[] { 0x50, 0x4B, 0x03, 0x04 });

        var result = await FileUploadValidation.ValidateExcelWorkbookAsync(file);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("XLSX");
    }

    [Fact]
    public void ValidateReceiptBytes_RejectsExtensionMagicMismatch()
    {
        var result = FileUploadValidation.ValidateReceiptBytes("%PDF-"u8.ToArray(), "dekont.jpg", "image/jpeg");

        result.Success.Should().BeFalse();
    }

    private static FormFile CreateFormFile(string fileName, string contentType, byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
