namespace CallCenter.PbxService.Services;

/// <summary>
/// RTP uzerinden ses dosyasi oynatma.
/// WAV dosyasini okuyup 20ms frame'ler halinde RTP stream olarak gonderir.
/// </summary>
public interface IAudioPlayer
{
    /// <summary>Tek bir ses dosyasi cal. Bitene kadar bekler.</summary>
    Task PlayFileAsync(string filePath, IRtpSender rtpSender, CancellationToken cancellationToken);

    /// <summary>Birden fazla dosyayi sirayla cal.</summary>
    Task PlayFilesAsync(IEnumerable<string> filePaths, IRtpSender rtpSender, CancellationToken cancellationToken);

    /// <summary>Dosyayi loop olarak cal (bekleme muzigi icin). Cancel edilene kadar devam eder.</summary>
    Task PlayLoopAsync(string filePath, IRtpSender rtpSender, CancellationToken cancellationToken);
}

/// <summary>RTP paket gonderme soyutlamasi</summary>
public interface IRtpSender
{
    /// <summary>20ms PCM frame gonder (PCMU/PCMA encoded)</summary>
    Task SendAudioFrameAsync(byte[] encodedFrame, int durationMs);
}
