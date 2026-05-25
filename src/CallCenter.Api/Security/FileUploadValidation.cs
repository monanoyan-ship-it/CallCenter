using System.IO.Compression;
using Microsoft.AspNetCore.Http;

namespace CallCenter.Api.Security;

public static class FileUploadValidation
{
    public const long MaxImageBytes = 5_242_880;
    public const long MaxPersonnelPhotoBytes = 3_145_728;
    public const long MaxAudioBytes = 10_485_760;
    public const long MaxExcelBytes = 20_971_520;

    public static async Task<FileValidationResult> ValidateImageAsync(IFormFile? file, long maxBytes = MaxImageBytes)
    {
        if (file == null || file.Length == 0)
            return FileValidationResult.Fail("Dosya secilmedi.");
        if (file.Length > maxBytes)
            return FileValidationResult.Fail($"Dosya {maxBytes / 1024 / 1024} MB'dan buyuk olamaz.");

        var header = await ReadHeaderAsync(file, 16);
        var detected = DetectImage(header);
        if (detected == null)
            return FileValidationResult.Fail("Sadece JPEG, PNG ve WebP desteklenir.");

        return FileValidationResult.Ok(detected.Value.ContentType, detected.Value.Extension);
    }

    public static async Task<FileValidationResult> ValidateExcelWorkbookAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return FileValidationResult.Fail("Dosya secilmedi.");
        if (file.Length > MaxExcelBytes)
            return FileValidationResult.Fail("Dosya 20 MB'dan buyuk olamaz.");

        if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            return FileValidationResult.Fail("Sadece makrosuz XLSX dosyalari desteklenir.");

        var header = await ReadHeaderAsync(file, 8);
        if (header.Length < 4 || header[0] != 0x50 || header[1] != 0x4B || header[2] != 0x03 || header[3] != 0x04)
            return FileValidationResult.Fail("XLSX dosyasi gecersiz veya bozuk.");

        await using var stream = file.OpenReadStream();
        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            if (archive.Entries.Any(e => e.FullName.Contains("vbaProject.bin", StringComparison.OrdinalIgnoreCase)))
                return FileValidationResult.Fail("Makrolu Excel dosyalari desteklenmez.");
        }
        catch (InvalidDataException)
        {
            return FileValidationResult.Fail("XLSX dosyasi gecersiz veya bozuk.");
        }

        return FileValidationResult.Ok(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xlsx");
    }

    public static async Task<FileValidationResult> ValidateAudioAsync(IFormFile? file, long maxBytes = MaxAudioBytes)
    {
        if (file == null || file.Length == 0)
            return FileValidationResult.Fail("Ses dosyasi secilmedi.");
        if (file.Length > maxBytes)
            return FileValidationResult.Fail($"Ses dosyasi {maxBytes / 1024 / 1024} MB'dan buyuk olamaz.");

        var header = await ReadHeaderAsync(file, 16);
        var detected = DetectAudio(header);
        return detected == null
            ? FileValidationResult.Fail("Sadece WAV, MP3 veya OGG ses dosyalari desteklenir.")
            : FileValidationResult.Ok(detected.Value.ContentType, detected.Value.Extension);
    }

    public static FileValidationResult ValidateReceiptBytes(byte[] bytes, string? fileName, string? contentType, long maxBytes = MaxImageBytes)
    {
        if (bytes.Length == 0)
            return FileValidationResult.Fail("Dekont dosyasi bos.");
        if (bytes.Length > maxBytes)
            return FileValidationResult.Fail($"Dekont dosyasi en fazla {maxBytes / 1024 / 1024} MB olabilir.");

        var ext = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        var lowerContentType = (contentType ?? string.Empty).ToLowerInvariant();
        if (LooksLikePdf(bytes) && (ext is "" or ".pdf") && (lowerContentType is "" or "application/pdf"))
            return FileValidationResult.Ok("application/pdf", ".pdf");

        var image = DetectImage(bytes.Take(16).ToArray());
        if (image != null && (ext == "" || ext == image.Value.Extension || (ext == ".jpeg" && image.Value.Extension == ".jpg")))
            return FileValidationResult.Ok(image.Value.ContentType, image.Value.Extension);

        return FileValidationResult.Fail("Dekont dosyasi PDF, JPG, PNG veya WEBP olmalidir.");
    }

    private static async Task<byte[]> ReadHeaderAsync(IFormFile file, int byteCount)
    {
        var buffer = new byte[byteCount];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(buffer.AsMemory(0, byteCount));
        return buffer[..read];
    }

    private static (string ContentType, string Extension)? DetectImage(IReadOnlyList<byte> header)
    {
        if (header.Count >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return ("image/jpeg", ".jpg");
        if (header.Count >= 8
            && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
            && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
            return ("image/png", ".png");
        if (header.Count >= 12
            && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
            && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
            return ("image/webp", ".webp");
        return null;
    }

    private static (string ContentType, string Extension)? DetectAudio(IReadOnlyList<byte> header)
    {
        if (header.Count >= 12
            && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
            && header[8] == 0x57 && header[9] == 0x41 && header[10] == 0x56 && header[11] == 0x45)
            return ("audio/wav", ".wav");
        if (header.Count >= 3 && header[0] == 0x49 && header[1] == 0x44 && header[2] == 0x33)
            return ("audio/mpeg", ".mp3");
        if (header.Count >= 2 && header[0] == 0xFF && (header[1] & 0xE0) == 0xE0)
            return ("audio/mpeg", ".mp3");
        if (header.Count >= 4 && header[0] == 0x4F && header[1] == 0x67 && header[2] == 0x67 && header[3] == 0x53)
            return ("audio/ogg", ".ogg");
        return null;
    }

    private static bool LooksLikePdf(IReadOnlyList<byte> bytes)
        => bytes.Count >= 5
           && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46 && bytes[4] == 0x2D;
}

public sealed class FileValidationResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public string Extension { get; init; } = string.Empty;

    public static FileValidationResult Ok(string contentType, string extension)
        => new() { Success = true, ContentType = contentType, Extension = extension };

    public static FileValidationResult Fail(string error)
        => new() { Success = false, Error = error };
}
