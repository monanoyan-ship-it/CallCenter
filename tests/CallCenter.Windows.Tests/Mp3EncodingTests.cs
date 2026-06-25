using CallCenter.Windows.Services;
using NAudio.Wave;
using Xunit;

namespace CallCenter.Windows.Tests;

/// <summary>
/// WAV -> MP3 donusumu (PC'de oynatilabilir, sifresiz). Gercek MediaFoundation encode'u test eder.
/// 8kHz (PCMU/PCMA) ve 16kHz (G722) ses kayitlari icin calismali.
/// </summary>
public class Mp3EncodingTests
{
    private static string MakeSineWav(string dir, int sampleRate, int seconds = 1)
    {
        var path = Path.Combine(dir, $"in_{sampleRate}.wav");
        using var writer = new WaveFileWriter(path, new WaveFormat(sampleRate, 16, 1));
        var samples = sampleRate * seconds;
        for (int i = 0; i < samples; i++)
        {
            // 440 Hz sinus, kisik genlik
            var v = (short)(Math.Sin(2 * Math.PI * 440 * i / sampleRate) * 8000);
            writer.WriteSample(v / 32768f);
        }
        return path;
    }

    private static bool LooksLikeMp3(string path)
    {
        var b = File.ReadAllBytes(path);
        if (b.Length < 4) return false;
        // ID3 tag ("ID3") veya MPEG frame sync (0xFF 0xEx/0xFx)
        if (b[0] == (byte)'I' && b[1] == (byte)'D' && b[2] == (byte)'3') return true;
        return b[0] == 0xFF && (b[1] & 0xE0) == 0xE0;
    }

    [Theory]
    [InlineData(8000)]   // PCMU/PCMA — resample yolu
    [InlineData(16000)]  // G722 — dogrudan
    public void EncodeWavToMp3_ProducesPlayableMp3(int sampleRate)
    {
        var dir = Directory.CreateTempSubdirectory("mp3enc");
        try
        {
            var wav = MakeSineWav(dir.FullName, sampleRate);
            var mp3 = Path.Combine(dir.FullName, "out.mp3");

            NativeSipService.EncodeWavToMp3(wav, mp3);

            Assert.True(File.Exists(mp3), "MP3 dosyasi olusmadi");
            Assert.True(new FileInfo(mp3).Length > 0, "MP3 dosyasi bos");
            Assert.True(LooksLikeMp3(mp3), "Gecerli MP3 header bulunamadi");
        }
        finally { dir.Delete(true); }
    }
}
