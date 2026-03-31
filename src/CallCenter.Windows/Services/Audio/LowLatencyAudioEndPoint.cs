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

    public static readonly AudioSamplingRatesEnum DefaultAudioSourceSamplingRate = AudioSamplingRatesEnum.Rate8KHz;
    public static readonly AudioSamplingRatesEnum DefaultAudioPlaybackRate = AudioSamplingRatesEnum.Rate8KHz;

    private readonly ILogger _logger;
    private WaveFormat _waveSinkFormat;
    private WaveFormat _waveSourceFormat;
    private WaveOutEvent _waveOutEvent;
    private BufferedWaveProvider _waveProvider;
    private WaveInEvent _waveInEvent;
    private readonly IAudioEncoder _audioEncoder;
    private readonly MediaFormatManager<AudioFormat> _audioFormatManager;

    private readonly bool _disableSink;
    private readonly int _audioOutDeviceIndex;
    private readonly int _audioInDeviceIndex;
    private readonly bool _disableSource;

    protected bool _isAudioSourceStarted;
    protected bool _isAudioSinkStarted;
    protected bool _isAudioSourcePaused;
    protected bool _isAudioSinkPaused;
    protected bool _isAudioSourceClosed;
    protected bool _isAudioSinkClosed;

    public event EncodedSampleDelegate OnAudioSourceEncodedSample;
    public event Action<EncodedAudioFrame> OnAudioSourceEncodedFrameReady;

    [Obsolete("The audio source only generates encoded samples.")]
    public event RawAudioSampleDelegate OnAudioSourceRawSample { add { } remove { } }

    public event SourceErrorDelegate OnAudioSourceError;
    public event SourceErrorDelegate OnAudioSinkError;

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
                DiscardOnBufferOverflow = true
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

    private void LocalAudioSampleAvailable(object sender, WaveInEventArgs args)
    {
        byte[] buffer = args.Buffer.Take(args.BytesRecorded).ToArray();
        short[] pcm = buffer.Where((x, i) => i % 2 == 0).Select((y, i) => BitConverter.ToInt16(buffer, i * 2)).ToArray();
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
        _waveProvider?.AddSamples(pcmSample, 0, pcmSample.Length);
    }

    [Obsolete("Use GotEncodedMediaFrame instead.")]
    public void GotAudioRtp(IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID, bool marker, byte[] payload)
    {
        if (_waveProvider != null && _audioEncoder != null)
        {
            var pcmSample = _audioEncoder.DecodeAudio(payload, _audioFormatManager.SelectedFormat);
            byte[] pcmBytes = pcmSample.SelectMany(BitConverter.GetBytes).ToArray();
            _waveProvider?.AddSamples(pcmBytes, 0, pcmBytes.Length);
        }
    }

    public void GotEncodedMediaFrame(EncodedAudioFrame encodedMediaFrame)
    {
        var audioFormat = encodedMediaFrame.AudioFormat;

        if (_waveProvider != null && _audioEncoder != null && !audioFormat.IsEmpty())
        {
            var pcmSample = _audioEncoder.DecodeAudio(encodedMediaFrame.EncodedAudio, audioFormat);
            byte[] pcmBytes = pcmSample.SelectMany(BitConverter.GetBytes).ToArray();
            _waveProvider?.AddSamples(pcmBytes, 0, pcmBytes.Length);
        }
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
