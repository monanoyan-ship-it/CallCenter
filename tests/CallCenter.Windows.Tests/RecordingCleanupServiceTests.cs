using CallCenter.Windows.LocalData;
using CallCenter.Windows.LocalData.Entities;
using CallCenter.Windows.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CallCenter.Windows.Tests;

/// <summary>
/// "Asla kaybolmasin" veri-kaybi korumasi: RecordingCleanupService, retention suresi dolmus
/// olsa bile HENUZ YUKLENMEMIS yerel kaydi silmemeli; yuklenmis veya zaten yok olan kaydin
/// metadata'sini temizleyebilmeli.
/// </summary>
public class RecordingCleanupServiceTests
{
    private static LocalRecording Expired(string path, bool uploadedToCloud, bool uploadedToPlatform)
        => new()
        {
            Uid = Guid.NewGuid(),
            FilePath = path,
            IsUploadedToCloud = uploadedToCloud,
            IsUploadedToPlatform = uploadedToPlatform,
            RetentionDate = DateTime.UtcNow.AddDays(-1) // suresi dolmus
        };

    private static RecordingCleanupService BuildService(ILocalRepository repo)
        => new(repo, Substitute.For<ILogger<RecordingCleanupService>>());

    [Fact]
    public async Task Expired_Unuploaded_File_IsPreserved()
    {
        var dir = Directory.CreateTempSubdirectory("rec-keep");
        try
        {
            var file = Path.Combine(dir.FullName, "call.enc");
            await File.WriteAllTextAsync(file, "ses verisi");
            var rec = Expired(file, uploadedToCloud: false, uploadedToPlatform: false);

            var repo = Substitute.For<ILocalRepository>();
            repo.IsConfigured.Returns(true);
            repo.GetExpiredRecordingsAsync().Returns(new List<LocalRecording> { rec });

            await BuildService(repo).CleanupExpiredRecordingsAsync();

            Assert.True(File.Exists(file)); // dosya KORUNDU
            await repo.DidNotReceive().DeleteRecordingAsync(rec.Uid); // metadata da korundu
        }
        finally { dir.Delete(true); }
    }

    [Fact]
    public async Task Expired_UploadedToCloud_File_IsDeleted()
    {
        var dir = Directory.CreateTempSubdirectory("rec-del-cloud");
        try
        {
            var file = Path.Combine(dir.FullName, "call.enc");
            await File.WriteAllTextAsync(file, "ses verisi");
            var rec = Expired(file, uploadedToCloud: true, uploadedToPlatform: false);

            var repo = Substitute.For<ILocalRepository>();
            repo.IsConfigured.Returns(true);
            repo.GetExpiredRecordingsAsync().Returns(new List<LocalRecording> { rec });

            await BuildService(repo).CleanupExpiredRecordingsAsync();

            Assert.False(File.Exists(file)); // yuklenmis → silindi
            await repo.Received(1).DeleteRecordingAsync(rec.Uid);
        }
        finally { dir.Delete(true); }
    }

    [Fact]
    public async Task Expired_UploadedToPlatform_File_IsDeleted()
    {
        var dir = Directory.CreateTempSubdirectory("rec-del-plat");
        try
        {
            var file = Path.Combine(dir.FullName, "call.enc");
            await File.WriteAllTextAsync(file, "ses verisi");
            var rec = Expired(file, uploadedToCloud: false, uploadedToPlatform: true);

            var repo = Substitute.For<ILocalRepository>();
            repo.IsConfigured.Returns(true);
            repo.GetExpiredRecordingsAsync().Returns(new List<LocalRecording> { rec });

            await BuildService(repo).CleanupExpiredRecordingsAsync();

            Assert.False(File.Exists(file));
            await repo.Received(1).DeleteRecordingAsync(rec.Uid);
        }
        finally { dir.Delete(true); }
    }

    [Fact]
    public async Task Expired_MissingFile_MetadataCleaned()
    {
        var file = Path.Combine(Path.GetTempPath(), "yok_" + Guid.NewGuid().ToString("N") + ".enc");
        var rec = Expired(file, uploadedToCloud: false, uploadedToPlatform: false);

        var repo = Substitute.For<ILocalRepository>();
        repo.IsConfigured.Returns(true);
        repo.GetExpiredRecordingsAsync().Returns(new List<LocalRecording> { rec });

        await BuildService(repo).CleanupExpiredRecordingsAsync();

        // Dosya zaten yok → kaybolacak veri yok → artakalan metadata temizlenebilir
        await repo.Received(1).DeleteRecordingAsync(rec.Uid);
    }

    [Fact]
    public async Task NotConfigured_Repo_NoOp()
    {
        var repo = Substitute.For<ILocalRepository>();
        repo.IsConfigured.Returns(false);

        await BuildService(repo).CleanupExpiredRecordingsAsync();

        await repo.DidNotReceive().GetExpiredRecordingsAsync();
    }
}
