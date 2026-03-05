using System.Collections.Concurrent;

namespace CallCenter.PbxService.Services;

/// <summary>
/// Kuyruk Bekleme Sistemi.
/// Arayanlari kuyrukta tutar, bekleme muzigi calar, sira takibi yapar.
/// ACD agent bulunca TransferReady eventi firlatir.
/// </summary>
public class QueueManager
{
    private readonly ILogger<QueueManager> _logger;
    private readonly IAudioPlayer _audioPlayer;
    private readonly AudioFileCache _audioCache;
    private readonly IApiClient _apiClient;

    // QueueId -> siradaki arayanlar
    private readonly ConcurrentDictionary<int, ConcurrentQueue<QueuedCall>> _queues = new();

    // CallId -> QueuedCall (hizli erisim)
    private readonly ConcurrentDictionary<string, QueuedCall> _callIndex = new();

    public QueueManager(
        ILogger<QueueManager> logger,
        IAudioPlayer audioPlayer,
        AudioFileCache audioCache,
        IApiClient apiClient)
    {
        _logger = logger;
        _audioPlayer = audioPlayer;
        _audioCache = audioCache;
        _apiClient = apiClient;
    }

    /// <summary>Arayani kuyruga ekle ve bekleme muzigi basla</summary>
    public async Task<QueuedCall> EnqueueAsync(
        string callId,
        int queueId,
        IRtpSender rtpSender,
        CancellationToken cancellationToken)
    {
        var queue = _queues.GetOrAdd(queueId, _ => new ConcurrentQueue<QueuedCall>());

        var queuedCall = new QueuedCall
        {
            CallId = callId,
            QueueId = queueId,
            Position = queue.Count + 1,
            EnqueuedAt = DateTime.UtcNow,
            RtpSender = rtpSender,
            CancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
        };

        queue.Enqueue(queuedCall);
        _callIndex[callId] = queuedCall;

        _logger.LogInformation("Kuyruga eklendi: CallId={CallId}, Queue={QueueId}, Sira={Position}",
            callId, queueId, queuedCall.Position);

        // Arka planda bekleme muzigi cal
        _ = PlayHoldMusicAsync(queuedCall);

        return queuedCall;
    }

    /// <summary>Arayani kuyruktan cikar (agent bulundu veya cagri kapandi)</summary>
    public QueuedCall? Dequeue(int queueId)
    {
        if (!_queues.TryGetValue(queueId, out var queue))
            return null;

        if (!queue.TryDequeue(out var call))
            return null;

        _callIndex.TryRemove(call.CallId, out _);
        call.CancellationSource.Cancel(); // Bekleme muzigini durdur

        // Kalan arayanların siralarini guncelle
        UpdatePositions(queueId);

        _logger.LogInformation("Kuyruktan cikarildi: CallId={CallId}, Queue={QueueId}",
            call.CallId, queueId);

        return call;
    }

    /// <summary>CallId ile kuyruktan cikar (cagri kapandiginda)</summary>
    public void RemoveByCallId(string callId)
    {
        if (!_callIndex.TryRemove(callId, out var call))
            return;

        call.CancellationSource.Cancel();

        // ConcurrentQueue'dan dogrudan silme yok, rebuild gerekli
        if (_queues.TryGetValue(call.QueueId, out var queue))
        {
            var remaining = new ConcurrentQueue<QueuedCall>();
            while (queue.TryDequeue(out var item))
            {
                if (item.CallId != callId)
                    remaining.Enqueue(item);
            }
            _queues[call.QueueId] = remaining;
            UpdatePositions(call.QueueId);
        }

        _logger.LogInformation("Kuyruktan kaldirildi (cagri kapandi): CallId={CallId}", callId);
    }

    /// <summary>Kuyrukta bekleyen cagri sayisi</summary>
    public int GetQueueLength(int queueId)
    {
        return _queues.TryGetValue(queueId, out var queue) ? queue.Count : 0;
    }

    /// <summary>Kuyrukta bekleyen ilk cagriyi getir (peek)</summary>
    public QueuedCall? PeekNext(int queueId)
    {
        if (_queues.TryGetValue(queueId, out var queue) && queue.TryPeek(out var call))
            return call;
        return null;
    }

    /// <summary>Belirli bir cagriyi getir</summary>
    public QueuedCall? GetCall(string callId)
    {
        return _callIndex.TryGetValue(callId, out var call) ? call : null;
    }

    /// <summary>Tum kuyruklarin durumu</summary>
    public Dictionary<int, int> GetAllQueueLengths()
    {
        var result = new Dictionary<int, int>();
        foreach (var kvp in _queues)
        {
            result[kvp.Key] = kvp.Value.Count;
        }
        return result;
    }

    private void UpdatePositions(int queueId)
    {
        if (!_queues.TryGetValue(queueId, out var queue))
            return;

        var pos = 1;
        foreach (var call in queue)
        {
            call.Position = pos++;
        }
    }

    private async Task PlayHoldMusicAsync(QueuedCall call)
    {
        var ct = call.CancellationSource.Token;

        try
        {
            // API'den kuyruk config al (bekleme muzigi ID)
            var queueConfig = await _apiClient.GetQueueConfigAsync(call.QueueId);

            string? holdMusicPath = null;
            if (queueConfig?.HoldMusicId != null)
            {
                holdMusicPath = await _audioCache.GetAudioFileAsync(queueConfig.HoldMusicId.Value);
            }

            if (holdMusicPath == null)
            {
                _logger.LogDebug("Bekleme muzigi bulunamadi, sessiz bekleme: Queue={QueueId}", call.QueueId);
                // Muzik yoksa sadece bekle
                try { await Task.Delay(Timeout.Infinite, ct); } catch (OperationCanceledException) { }
                return;
            }

            var maxWait = queueConfig?.MaxWaitTimeSeconds ?? 600; // Default 10 dk
            var waitDeadline = call.EnqueuedAt.AddSeconds(maxWait);

            _logger.LogInformation("Bekleme muzigi basliyor: Queue={QueueId}, MaxWait={MaxWait}s",
                call.QueueId, maxWait);

            // Bekleme muzigini loop olarak cal
            while (!ct.IsCancellationRequested && DateTime.UtcNow < waitDeadline)
            {
                await _audioPlayer.PlayFileAsync(holdMusicPath, call.RtpSender, ct);

                // Kisa ara (dosya bitti, tekrar baslayacak)
                if (!ct.IsCancellationRequested)
                    await Task.Delay(500, ct);
            }

            // Max bekleme suresi doldu
            if (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("Kuyruk max bekleme suresi doldu: CallId={CallId}, Queue={QueueId}",
                    call.CallId, call.QueueId);

                // Timeout mesaji cal
                if (queueConfig?.TimeoutMessageId != null)
                {
                    var timeoutPath = await _audioCache.GetAudioFileAsync(queueConfig.TimeoutMessageId.Value);
                    if (timeoutPath != null)
                        await _audioPlayer.PlayFileAsync(timeoutPath, call.RtpSender, ct);
                }

                call.TimedOut = true;
                RemoveByCallId(call.CallId);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal: agent bulundu veya cagri kapandi
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bekleme muzigi hatasi: CallId={CallId}", call.CallId);
        }
    }
}

/// <summary>Kuyrukta bekleyen cagri</summary>
public class QueuedCall
{
    public string CallId { get; set; } = string.Empty;
    public int QueueId { get; set; }
    public int Position { get; set; }
    public DateTime EnqueuedAt { get; set; }
    public IRtpSender RtpSender { get; set; } = null!;
    public CancellationTokenSource CancellationSource { get; set; } = null!;
    public bool TimedOut { get; set; }

    /// <summary>Bekleme suresi (saniye)</summary>
    public int WaitSeconds => (int)(DateTime.UtcNow - EnqueuedAt).TotalSeconds;
}

/// <summary>API'den gelen kuyruk yapilandirmasi</summary>
public class QueueConfigInfo
{
    public int QueueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? HoldMusicId { get; set; }
    public int? TimeoutMessageId { get; set; }
    public int MaxWaitTimeSeconds { get; set; } = 600;
    public int MaxQueueLength { get; set; } = 50;
}
