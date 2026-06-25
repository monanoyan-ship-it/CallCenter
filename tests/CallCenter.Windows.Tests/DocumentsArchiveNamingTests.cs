using CallCenter.Windows.Services;
using Xunit;

namespace CallCenter.Windows.Tests;

/// <summary>
/// Belgeler kalici arsivi adlandirma kurali (kullanici spec'i):
/// Klasor adi = aranan numara; dosya adi = cikis_numarasi + aranan + tarih-saat + uzanti.
/// </summary>
public class DocumentsArchiveNamingTests
{
    private static readonly DateTime Started = new(2026, 6, 25, 14, 9, 3);
    private const string Root = @"C:\Docs";

    [Fact]
    public void Folder_IsCalledNumber_File_HasOutbound_Called_DateTime()
    {
        var (dir, fileName) = NativeSipService.BuildDocumentsArchiveTarget(
            remoteNumber: "05551234567", outboundNumber: "1001", startedAt: Started, documentsRoot: Root, extension: ".mp3");

        Assert.Equal(@"C:\Docs\CorpLynk Kayitlar\05551234567", dir);
        Assert.Equal("1001_05551234567_2026-06-25_14-09-03.mp3", fileName);
    }

    [Fact]
    public void NoOutbound_OmitsPrefix()
    {
        var (_, fileName) = NativeSipService.BuildDocumentsArchiveTarget(
            "05551234567", null, Started, Root, ".mp3");

        Assert.Equal("05551234567_2026-06-25_14-09-03.mp3", fileName);
    }

    [Fact]
    public void EmptyCalled_FallsBackToBilinmeyen()
    {
        var (dir, fileName) = NativeSipService.BuildDocumentsArchiveTarget(
            null, "1001", Started, Root, ".mp3");

        Assert.EndsWith(@"CorpLynk Kayitlar\Bilinmeyen", dir);
        Assert.Equal("1001_Bilinmeyen_2026-06-25_14-09-03.mp3", fileName);
    }

    [Fact]
    public void WavExtension_Honored()
    {
        var (_, fileName) = NativeSipService.BuildDocumentsArchiveTarget(
            "0555", "1001", Started, Root, ".wav");

        Assert.EndsWith(".wav", fileName);
    }

    [Fact]
    public void NumbersWithSeparators_AreSanitized()
    {
        var (dir, fileName) = NativeSipService.BuildDocumentsArchiveTarget(
            "+90 (555) 123-45-67", "100 1", Started, Root, ".mp3");

        Assert.Equal(@"C:\Docs\CorpLynk Kayitlar\+905551234567", dir);
        Assert.Equal("1001_+905551234567_2026-06-25_14-09-03.mp3", fileName);
    }
}
