// SIPSorceryMedia.Windows WindowsAudioEndPoint fork'u - düşük gecikme için
// Orijinal: https://github.com/sipsorcery-org/SIPSorceryMedia.Windows
// Değişiklik: WaveOutEvent.DesiredLatency = 50ms (varsayılan 300ms)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using SIPSorceryMedia.Abstractions;

namespace CallCenter.Windows.Services.Audio;

public class LowLatencyAudioEndPoint : IAudioEndPoint
{
    private const int DEVICE_BITS_PER_SAMPLE = 16;
    private const int DEFAULT_DEVICE_CHANNELS = 1;
    private const int INPUT_BUFFERS = 2;
    private const int CAPTURE_BUFFER_MILLISECONDS = 20;
    private const int AUDIO_INPUTDEVICE_INDEX = -1;
    private const int AUDIO_OUTPUTDEVICE_INDEX = -1;

    // Düşük gecikme ayarları (50ms+2buf ses kırılmasına neden oldu, 300ms varsayılan çok geç)
    private const int PLAYBACK_DESIRED_LATENCY_MS = 100;
    private const int PLAYBACK_NUMBER_OF_BUFFERS = 3;

    // Playback jitter buffer'ı (BufferedWaveProvider) için gecikme sınırı.
    // Varsayılan NAudio buffer'ı 5 sn'dir ve birikmiş gecikmeyi boşaltmaz; bu yüzden
    // kapasiteyi düşürüp gecikme eşiği aşılınca canlıya atlanır (drift resync).
    private const int PLAYBACK_BUFFER_CAPACITY_MS = 1000;
    // 200ms: endustri onerisi 70-150ms ile eski "50ms kirilma yapiyor" gozlemi arasinda denge.
    private const int PLAYBACK_MAX_LATENCY_MS = 200;
    // Resync esigi asilinca buffer TAMAMEN bosaltilmaz (tam sessizlik = kesilme);
    // sadece bu hedefin uzerindeki fazla (en eski) ornekler atilir. Boylece kesinti minimuma iner.
    private const int PLAYBACK_RESYNC_TARGET_MS = 100;

    // Bounded gecikme ile yankı referansı ~360ms pencereyi kapsamalı (20ms frame * 18).
    private const int ECHO_REFERENCE_FRAME_LIMIT = 18;
    private const double ECHO_REMOTE_ACTIVE_RMS = 0.015;
    private const double ECHO_CORRELATION_THRESHOLD = 0.55;
    private const float ECHO_STRONG_ATTENUATION = 0.18f;
    private const float ECHO_SOFT_ATTENUATION = 0.45f;

    public static readonly AudioSamplingRatesEnum DefaultAudioSourceSamplingRate = AudioSamplingRatesEnum.Rate8KHz;
    public static readonly AudioSamplingRatesEnum DefaultAudioPlaybackRate = AudioSamplingRatesEnum.Rate8KHz;

    private readonly ILogger _logger;
    private WaveFormat _waveSinkFormat = null!;
    private WaveFormat _waveSourceFormat = null!;
    private WaveOutEvent _waveOutEvent = null!;
    private BufferedWaveProvider _waveProvider = null!;
    private WaveInEvent _waveInEvent = null!;
    private readonly IAudioEncoder _audioEncoder;
    private readonly MediaFormatManager<AudioFormat> _audioFormatManager;

    private readonly bool _disableSink;
    private readonly int _audioOutDeviceIndex;
    private readonly int _audioInDeviceIndex;
    private readonly bool _disableSource;
    private readonly object _echoReferenceLock = new();
    private readonly Queue<short[]> _echoReferenceFrames = new();
    private double _remoteRms;
    private DateTime _lastRemoteFrameUtc = DateTime.MinValue;

    protected bool _isAudioSourceStarted;
    protected bool _isAudioSinkStarted;
    protected bool _isAudioSourcePaused;
    protected bool _isAudioSinkPaused;
    protected bool _isAudioSourceClosed;
    protected bool _isAudioSinkClosed;

    public event EncodedSampleDelegate OnAudioSourceEncodedSample = null!;
    public event Action<EncodedAudioFrame> OnAudioSourceEncodedFrameReady = null!;

    [Obsolete("The audio source only generates encoded samples.")]
    public event RawAudioSampleDelegate OnAudioSourceRawSample { add { } remove { } }

    public event SourceErrorDelegate OnAudioSourceError = null!;
    public event SourceErrorDelegate OnAudioSinkError = null!;

    public float MicrophoneVolume { get; set; } = 1.0f;
    public float SpeakerVolume { get; set; } = 1.0f;
    public bool EchoGuardEnabled { get; set; } = true;

    public LowLatencyAudioEndPoint(
        IAudioEncoder audioEncoder,
        int audioOutDeviceIndex = AUDIO_OUTPUTDEVICE_INDEX,
        int audioInDeviceIndex = AUDIO_INPUTDEVICE_INDEX,
        bool disableSource = false,
        bool disableSink = false)
    {
        _logger = SIPSorcery.LogFactory.CreateLogger<LowLatencyAudioEndPoint>();
        _audioFormatManager = new MediaFormatManager<AudioFormat>(audioEncoder.SupportedFormats);
        _audioEncoder = audioEncoder;
        _audioOutDeviceIndex = audioOutDeviceIndex;
        _audioInDeviceIndex = audioInDeviceIndex;
        _disableSource = disableSource;
        _disableSink = disableSink;

        if (!_disableSink)
        {
            InitPlaybackDevice(_audioOutDeviceIndex, DefaultAudioPlaybackRate.GetHashCode(), DEFAULT_DEVICE_CHANNELS);

            if (audioEncoder.SupportedFormats?.Count == 1)
                SetAudioSinkFormat(audioEncoder.SupportedFormats[0]);
        }

        if (!_disableSource)
        {
            InitCaptureDevice(_audioInDeviceIndex, (int)DefaultAudioSourceSamplingRate, DEFAULT_DEVICE_CHANNELS);

            if (audioEncoder.SupportedFormats?.Count == 1)
                SetAudioSourceFormat(audioEncoder.SupportedFormats[0]);
        }
    }

    public void RestrictFormats(Func<AudioFormat, bool> filter) => _audioFormatManager.RestrictFormats(filter);
    public List<AudioFormat> GetAudioSourceFormats() => _audioFormatManager.GetSourceFormats();
    public List<AudioFormat> GetAudioSinkFormats() => _audioFormatManager.GetSourceFormats();

    public bool HasEncodedAudioSubscribers() => OnAudioSourceEncodedSample != null;
    public bool IsAudioSourcePaused() => _isAudioSourcePaused;
    public bool IsAudioSinkPaused() => _isAudioSinkPaused;

    public void ExternalAudioSourceRawSample(AudioSamplingRatesEnum samplingRate, uint durationMilliseconds, short[] sample) =>
        throw new NotImplementedException();

    public void SetAudioSourceFormat(AudioFormat audioFormat)
    {
        _audioFormatManager.SetSelectedFormat(audioFormat);

        if (!_disableSource && _waveSourceFormat.SampleRate != _audioFormatManager.SelectedFormat.ClockRate)
        {
            _logger.LogDebug("LowLatencyAudioEndPoint capture rate {Old} -> {New}",
                _waveSourceFormat.SampleRate, _audioFormatManager.SelectedFormat.ClockRate);
            InitCaptureDevice(_audioInDeviceIndex, _audioFormatManager.SelectedFormat.ClockRate, _audioFormatManager.SelectedFormat.ChannelCount);
        }
    }

    public void SetAudioSinkFormat(AudioFormat audioFormat)
    {
        _audioFormatManager.SetSelectedFormat(audioFormat);

        if (!_disableSink && _waveSinkFormat.SampleRate != _audioFormatManager.SelectedFormat.ClockRate)
        {
            _logger.LogDebug("LowLatencyAudioEndPoint playback rate {Old} -> {New}",
                _waveSinkFormat.SampleRate, _audioFormatManager.SelectedFormat.ClockRate);
            InitPlaybackDevice(_audioOutDeviceIndex, _audioFormatManager.SelectedFormat.ClockRate, _audioFormatManager.SelectedFormat.ChannelCount);
        }
    }

    public MediaEndPoints ToMediaEndPoints()
    {
        return new MediaEndPoints
        {
            AudioSource = _disableSource ? null : this,
            AudioSink = _disableSink ? null : this,
        };
    }

    public Task Start()
    {
        if (!_isAudioSourceStarted && _waveInEvent != null)
            StartAudio();

        if (!_isAudioSinkStarted && _waveOutEvent != null)
            StartAudioSink();

        return Task.CompletedTask;
    }

    public Task Close()
    {
        if (!_isAudioSourceClosed && _waveInEvent != null)
            CloseAudio();

        if (!_isAudioSinkClosed && _waveOutEvent != null)
            CloseAudioSink();

        return Task.CompletedTask;
    }

    public Task Pause()
    {
        if (!_isAudioSourcePaused && _waveInEvent != null)
            PauseAudio();

        if (!_isAudioSinkPaused && _waveOutEvent != null)
            PauseAudioSink();

        return Task.CompletedTask;
    }

    public Task Resume()
    {
        if (_isAudioSourcePaused && _waveInEvent != null)
            ResumeAudio();

        if (_isAudioSinkPaused && _waveOutEvent != null)
            ResumeAudioSink();

        return Task.CompletedTask;
    }

    private void InitPlaybackDevice(int audioOutDeviceIndex, int audioSinkSampleRate, int channels)
    {
        try
        {
            _waveOutEvent?.Stop();

            _waveSinkFormat = new WaveFormat(audioSinkSampleRate, DEVICE_BITS_PER_SAMPLE, channels);

            _waveOutEvent = new WaveOutEvent
            {
                DeviceNumber = audioOutDeviceIndex,
                DesiredLatency = PLAYBACK_DESIRED_LATENCY_MS,
                NumberOfBuffers = PLAYBACK_NUMBER_OF_BUFFERS
            };

            _waveProvider = new BufferedWaveProvider(_waveSinkFormat)
            {
                DiscardOnBufferOverflow = true,
                BufferDuration = TimeSpan.FromMilliseconds(PLAYBACK_BUFFER_CAPACITY_MS)
            };

            _waveOutEvent.Init(_waveProvider);
        }
        catch (Exception excp)
        {
            _logger.LogWarning(excp, "LowLatencyAudioEndPoint playback init failed.");
            OnAudioSinkError?.Invoke($"Playback init failed: {excp.Message}");
        }
    }

    private void InitCaptureDevice(int audioInDeviceIndex, int audioSourceSampleRate, int audioSourceChannels)
    {
        if (WaveInEvent.DeviceCount <= 0)
        {
            _logger.LogWarning("No audio capture devices available.");
            OnAudioSourceError?.Invoke("No audio capture devices available.");
            return;
        }

        if (WaveInEvent.DeviceCount <= audioInDeviceIndex && audioInDeviceIndex != -1)
        {
            _logger.LogWarning("Audio input device index {Index} exceeds max {Max}.",
                audioInDeviceIndex, WaveInEvent.DeviceCount - 1);
            OnAudioSourceError?.Invoke($"Audio input device index {audioInDeviceIndex} exceeds max {WaveInEvent.DeviceCount - 1}.");
            return;
        }

        if (_waveInEvent != null)
        {
            _waveInEvent.DataAvailable -= LocalAudioSampleAvailable;
            _waveInEvent.StopRecording();
        }

        _waveSourceFormat = new WaveFormat(audioSourceSampleRate, DEVICE_BITS_PER_SAMPLE, audioSourceChannels);

        _waveInEvent = new WaveInEvent
        {
            BufferMilliseconds = CAPTURE_BUFFER_MILLISECONDS,
            NumberOfBuffers = INPUT_BUFFERS,
            DeviceNumber = audioInDeviceIndex,
            WaveFormat = _waveSourceFormat
        };
        _waveInEvent.DataAvailable += LocalAudioSampleAvailable;
    }

    private void LocalAudioSampleAvailable(object? sender, WaveInEventArgs args)
    {
        byte[] buffer = args.Buffer.Take(args.BytesRecorded).ToArray();
        short[] pcm = BytesToShorts(buffer);
        ApplyMicrophoneProcessing(pcm);
        byte[] encodedSample = _audioEncoder.EncodeAudio(pcm, _audioFormatManager.SelectedFormat);

        OnAudioSourceEncodedSample?.Invoke((uint)encodedSample.Length, encodedSample);

        if (OnAudioSourceEncodedFrameReady != null)
        {
            var encodedAudioFrame = new EncodedAudioFrame(0,
                _audioFormatManager.SelectedFormat,
                GetEncodSampleDurationMs(pcm.Length, _audioFormatManager.SelectedFormat),
                encodedSample);
            OnAudioSourceEncodedFrameReady(encodedAudioFrame);
        }
    }

    private static uint GetEncodSampleDurationMs(int totalPcmSamples, AudioFormat audioFormat)
    {
        int frames = totalPcmSamples / audioFormat.ChannelCount;
        double durationMs = audioFormat.ClockRate > 0 ? (frames / (double)audioFormat.ClockRate) * 1000.0 : 0;
        return (uint)Math.Round(durationMs);
    }

    public void GotAudioSample(byte[] pcmSample)
    {
        ApplySpeakerProcessing(pcmSample);
        EnqueuePlayback(pcmSample, 0, pcmSample.Length);
    }

    /// <summary>
    /// Çözülmüş PCM'i oynatma kuyruğuna ekler. Birikmiş oynatma gecikmesi eşiği aşarsa
    /// (jitter/burst sonrası) buffer temizlenip "canlıya" atlanır; böylece gecikme sabit-düşük kalır.
    /// </summary>
    private void EnqueuePlayback(byte[] pcmBytes, int offset, int count)
    {
        if (_waveProvider == null || count <= 0)
        {
            return;
        }

        if (_waveProvider.BufferedDuration > TimeSpan.FromMilliseconds(PLAYBACK_MAX_LATENCY_MS))
        {
            // Tam ClearBuffer() yerine yumusak resync: sadece hedef gecikmenin uzerindeki
            // (en eski) fazla ornekleri at. Tam sessizlik/kesilme yerine kucuk bir atlama olur.
            var targetBytes = _waveProvider.WaveFormat.AverageBytesPerSecond * PLAYBACK_RESYNC_TARGET_MS / 1000;
            var excess = _waveProvider.BufferedBytes - targetBytes;
            if (excess > 0)
            {
                var discard = new byte[excess];
                _waveProvider.Read(discard, 0, discard.Length); // en eski ornekleri tuket/at
            }
        }

        _waveProvider.AddSamples(pcmBytes, offset, count);
    }

    [Obsolete("Use GotEncodedMediaFrame instead.")]
    public void GotAudioRtp(IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID, bool marker, byte[] payload)
    {
        if (_waveProvider != null && _audioEncoder != null)
        {
            var pcmSample = _audioEncoder.DecodeAudio(payload, _audioFormatManager.SelectedFormat);
            ApplySpeakerProcessing(pcmSample);
            byte[] pcmBytes = ShortsToBytes(pcmSample);
            EnqueuePlayback(pcmBytes, 0, pcmBytes.Length);
        }
    }

    public void GotEncodedMediaFrame(EncodedAudioFrame encodedMediaFrame)
    {
        var audioFormat = encodedMediaFrame.AudioFormat;

        if (_waveProvider != null && _audioEncoder != null && !audioFormat.IsEmpty())
        {
            var pcmSample = _audioEncoder.DecodeAudio(encodedMediaFrame.EncodedAudio, audioFormat);
            ApplySpeakerProcessing(pcmSample);
            byte[] pcmBytes = ShortsToBytes(pcmSample);
            EnqueuePlayback(pcmBytes, 0, pcmBytes.Length);
        }
    }

    private void ApplyMicrophoneProcessing(short[] pcm)
    {
        var gain = Math.Clamp(MicrophoneVolume, 0f, 1f);

        if (EchoGuardEnabled && HasRecentRemoteAudio())
        {
            gain *= GetEchoGuardGain(pcm);
        }

        ApplyGain(pcm, gain);
    }

    private void ApplySpeakerProcessing(short[] pcm)
    {
        ApplyGain(pcm, Math.Clamp(SpeakerVolume, 0f, 1f));
        RememberEchoReference(pcm);
        _remoteRms = CalculateRms(pcm);
        _lastRemoteFrameUtc = DateTime.UtcNow;
    }

    private void ApplySpeakerProcessing(byte[] pcmBytes)
    {
        if (pcmBytes.Length < 2)
        {
            _remoteRms = 0;
            return;
        }

        var pcm = BytesToShorts(pcmBytes);
        ApplySpeakerProcessing(pcm);
        Buffer.BlockCopy(pcm, 0, pcmBytes, 0, pcm.Length * 2);
    }

    private float GetEchoGuardGain(short[] micPcm)
    {
        var micRms = CalculateRms(micPcm);
        if (micRms < 0.004)
        {
            return ECHO_STRONG_ATTENUATION;
        }

        var similarity = GetMaxEchoReferenceCorrelation(micPcm);
        if (similarity >= ECHO_CORRELATION_THRESHOLD && micRms <= _remoteRms * 1.7)
        {
            return ECHO_STRONG_ATTENUATION;
        }

        if (micRms <= _remoteRms * 0.75)
        {
            return ECHO_SOFT_ATTENUATION;
        }

        return 1.0f;
    }

    private bool HasRecentRemoteAudio()
    {
        if (_remoteRms <= ECHO_REMOTE_ACTIVE_RMS)
        {
            return false;
        }

        var ageMs = (DateTime.UtcNow - _lastRemoteFrameUtc).TotalMilliseconds;
        if (ageMs <= 300)
        {
            return true;
        }

        if (ageMs > 1000)
        {
            _remoteRms = 0;
            lock (_echoReferenceLock)
            {
                _echoReferenceFrames.Clear();
            }
        }

        return false;
    }

    private void RememberEchoReference(short[] pcm)
    {
        if (!EchoGuardEnabled || pcm.Length == 0)
        {
            return;
        }

        var copy = new short[pcm.Length];
        Array.Copy(pcm, copy, pcm.Length);

        lock (_echoReferenceLock)
        {
            _echoReferenceFrames.Enqueue(copy);
            while (_echoReferenceFrames.Count > ECHO_REFERENCE_FRAME_LIMIT)
            {
                _echoReferenceFrames.Dequeue();
            }
        }
    }

    private double GetMaxEchoReferenceCorrelation(short[] micPcm)
    {
        short[][] references;
        lock (_echoReferenceLock)
        {
            references = _echoReferenceFrames.ToArray();
        }

        double max = 0;
        foreach (var reference in references)
        {
            max = Math.Max(max, CalculateAbsCorrelation(micPcm, reference));
        }

        return max;
    }

    private static double CalculateAbsCorrelation(short[] left, short[] right)
    {
        var count = Math.Min(left.Length, right.Length);
        if (count < 16)
        {
            return 0;
        }

        double sumLeftRight = 0;
        double sumLeftSquared = 0;
        double sumRightSquared = 0;

        for (var i = 0; i < count; i++)
        {
            var l = left[i] / 32768.0;
            var r = right[i] / 32768.0;
            sumLeftRight += l * r;
            sumLeftSquared += l * l;
            sumRightSquared += r * r;
        }

        var denominator = Math.Sqrt(sumLeftSquared * sumRightSquared);
        return denominator <= 0.000001 ? 0 : Math.Abs(sumLeftRight / denominator);
    }

    private static double CalculateRms(short[] pcm)
    {
        if (pcm.Length == 0)
        {
            return 0;
        }

        double sum = 0;
        for (var i = 0; i < pcm.Length; i++)
        {
            var normalized = pcm[i] / 32768.0;
            sum += normalized * normalized;
        }

        return Math.Sqrt(sum / pcm.Length);
    }

    private static void ApplyGain(short[] pcm, float gain)
    {
        if (gain >= 0.999f)
        {
            return;
        }

        if (gain <= 0.001f)
        {
            Array.Clear(pcm, 0, pcm.Length);
            return;
        }

        for (var i = 0; i < pcm.Length; i++)
        {
            pcm[i] = (short)Math.Clamp((int)Math.Round(pcm[i] * gain), short.MinValue, short.MaxValue);
        }
    }

    private static short[] BytesToShorts(byte[] pcmBytes)
    {
        var samples = new short[pcmBytes.Length / 2];
        Buffer.BlockCopy(pcmBytes, 0, samples, 0, samples.Length * 2);
        return samples;
    }

    private static byte[] ShortsToBytes(short[] pcm)
    {
        var bytes = new byte[pcm.Length * 2];
        Buffer.BlockCopy(pcm, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public Task PauseAudioSink() { _isAudioSinkPaused = true; _waveOutEvent?.Pause(); return Task.CompletedTask; }
    public Task ResumeAudioSink() { _isAudioSinkPaused = false; _waveOutEvent?.Play(); return Task.CompletedTask; }
    public Task StartAudioSink() { if (!_isAudioSinkStarted) { _isAudioSinkStarted = true; _waveOutEvent?.Play(); } return Task.CompletedTask; }
    public Task CloseAudioSink() { if (!_isAudioSinkClosed) { _isAudioSinkClosed = true; _waveOutEvent?.Stop(); } return Task.CompletedTask; }
    public Task PauseAudio() { _isAudioSourcePaused = true; _waveInEvent?.StopRecording(); return Task.CompletedTask; }
    public Task ResumeAudio() { _isAudioSourcePaused = false; _waveInEvent?.StartRecording(); return Task.CompletedTask; }
    public Task StartAudio() { if (!_isAudioSourceStarted) { _isAudioSourceStarted = true; _waveInEvent?.StartRecording(); } return Task.CompletedTask; }
    public Task CloseAudio()
    {
        if (!_isAudioSourceClosed)
        {
            _isAudioSourceClosed = true;
            if (_waveInEvent != null)
            {
                _waveInEvent.DataAvailable -= LocalAudioSampleAvailable;
                _waveInEvent.StopRecording();
            }
        }
        return Task.CompletedTask;
    }
}
