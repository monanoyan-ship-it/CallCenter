using CallCenter.Windows.Services;
using Concentus.Enums;
using Concentus.Structs;
using Xunit;

namespace CallCenter.Windows.Tests;

/// <summary>
/// Opus decoder kayit yolunda iki ayri akistan (gelen=0, giden=1) ayni anda kullanilir.
/// Decoder thread-safe degil; akis basina ayri+kilitli decoder ile paralel decode
/// crash/bozulma yapmamali ve tutarli uzunlukta PCM uretmeli.
/// </summary>
public class OpusDecoderConcurrencyTests
{
    private static byte[] MakeOpusFrame()
    {
#pragma warning disable CS0618
        var enc = new OpusEncoder(48000, 1, OpusApplication.OPUS_APPLICATION_VOIP);
        var pcm = new short[960]; // 20ms @ 48kHz mono (sessizlik)
        var packet = new byte[4000];
        var len = enc.Encode(pcm, 0, 960, packet, 0, packet.Length);
#pragma warning restore CS0618
        return packet[..len];
    }

    [Fact]
    public void ConcurrentOpusDecode_TwoStreams_NoCorruptionNoCrash()
    {
        var frame = MakeOpusFrame();
        const int iterations = 400;
        var results = new System.Collections.Concurrent.ConcurrentBag<int>();

        Parallel.For(0, iterations, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
        {
            var stream = i % 2; // 0=gelen, 1=giden ayni anda
            var pcm = AudioCodecDecoder.Decode(frame, 111, opusStream: stream);
            results.Add(pcm.Length);
        });

        // 20ms @ 48kHz = 960 sample * 2 byte = 1920 byte; tum decode'lar tutarli olmali.
        Assert.Equal(iterations, results.Count);
        Assert.All(results, len => Assert.Equal(1920, len));
    }
}
