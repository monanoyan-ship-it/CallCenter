namespace CallCenter.PbxService.Services;

/// <summary>
/// IVR State Machine.
/// Karsilama mesaji cal -> DTMF bekle -> Eylem uygula (kuyruk/extension/alt menu/tekrar)
/// </summary>
public class IvrEngine
{
    private readonly ILogger<IvrEngine> _logger;
    private readonly IApiClient _apiClient;
    private readonly IAudioPlayer _audioPlayer;
    private readonly AudioFileCache _audioCache;

    public IvrEngine(
        ILogger<IvrEngine> logger,
        IApiClient apiClient,
        IAudioPlayer audioPlayer,
        AudioFileCache audioCache)
    {
        _logger = logger;
        _apiClient = apiClient;
        _audioPlayer = audioPlayer;
        _audioCache = audioCache;
    }

    /// <summary>IVR oturumu baslat. Sonuc olarak yonlendirme karari doner.</summary>
    public async Task<IvrResult> RunAsync(
        int ivrMenuId,
        string customerUid,
        IRtpSender rtpSender,
        Func<Task<char?>> dtmfReader,
        CancellationToken cancellationToken)
    {
        var menu = await _apiClient.GetIvrMenuAsync(customerUid, ivrMenuId);
        if (menu == null)
        {
            _logger.LogError("IVR menu bulunamadi: {MenuId}", ivrMenuId);
            return IvrResult.Hangup("Menu bulunamadi");
        }

        _logger.LogInformation("IVR baslatildi: {MenuName} ({OptionCount} secenek)",
            menu.Name, menu.Options.Count);

        var retryCount = 0;

        while (retryCount < menu.MaxRetries && !cancellationToken.IsCancellationRequested)
        {
            // 1. Karsilama mesaji cal
            if (menu.GreetingMessageId.HasValue)
            {
                var audioPath = await _audioCache.GetAudioFileAsync(menu.GreetingMessageId.Value);
                if (audioPath != null)
                {
                    await _audioPlayer.PlayFileAsync(audioPath, rtpSender, cancellationToken);
                }
            }

            // 2. DTMF bekle
            var digit = await WaitForDtmfAsync(dtmfReader, menu.TimeoutSeconds, cancellationToken);

            if (digit == null)
            {
                // Timeout - tekrar dene
                retryCount++;
                _logger.LogDebug("IVR timeout ({Retry}/{Max})", retryCount, menu.MaxRetries);
                continue;
            }

            _logger.LogInformation("IVR DTMF alindi: {Digit}", digit);

            // 3. Tusa karsilik gelen eylemi bul
            var option = menu.Options.FirstOrDefault(o => o.Digit == digit.ToString());
            if (option == null)
            {
                // Gecersiz tus
                retryCount++;
                _logger.LogDebug("IVR gecersiz tus: {Digit} ({Retry}/{Max})", digit, retryCount, menu.MaxRetries);
                continue;
            }

            // 4. Eylemi uygula
            var result = await ExecuteActionAsync(option, customerUid, rtpSender, dtmfReader, cancellationToken);
            if (result != null) return result;
        }

        // Max retry asildi
        _logger.LogWarning("IVR max retry asildi, varsayilan eylem uygulanacak");

        // Varsayilan kuyruga yonlendir veya kapat
        var defaultQueue = menu.Options.FirstOrDefault(o => o.ActionType == "transfer_queue");
        if (defaultQueue?.TargetQueueId != null)
        {
            return IvrResult.TransferToQueue(defaultQueue.TargetQueueId.Value);
        }

        return IvrResult.Hangup("Max retry asildi");
    }

    private async Task<IvrResult?> ExecuteActionAsync(
        IvrMenuOptionInfo option,
        string customerUid,
        IRtpSender rtpSender,
        Func<Task<char?>> dtmfReader,
        CancellationToken cancellationToken)
    {
        switch (option.ActionType)
        {
            case "transfer_queue":
                if (option.TargetQueueId.HasValue)
                {
                    _logger.LogInformation("IVR -> Kuyruk: {QueueId}", option.TargetQueueId.Value);
                    return IvrResult.TransferToQueue(option.TargetQueueId.Value);
                }
                break;

            case "transfer_extension":
                if (!string.IsNullOrEmpty(option.TargetExtension))
                {
                    _logger.LogInformation("IVR -> Extension: {Ext}", option.TargetExtension);
                    return IvrResult.TransferToExtension(option.TargetExtension);
                }
                break;

            case "play_message":
                if (option.TargetGreetingMessageId.HasValue)
                {
                    var audioPath = await _audioCache.GetAudioFileAsync(option.TargetGreetingMessageId.Value);
                    if (audioPath != null)
                        await _audioPlayer.PlayFileAsync(audioPath, rtpSender, cancellationToken);
                }
                return null; // Mesajdan sonra menuye don

            case "sub_menu":
                if (option.TargetIvrMenuId.HasValue)
                {
                    _logger.LogInformation("IVR -> Alt menu: {MenuId}", option.TargetIvrMenuId.Value);
                    return await RunAsync(option.TargetIvrMenuId.Value, customerUid,
                        rtpSender, dtmfReader, cancellationToken);
                }
                break;

            case "repeat":
                return null; // Menuyu tekrarla

            case "hangup":
                return IvrResult.Hangup("Kullanici kapatti");

            default:
                _logger.LogWarning("Bilinmeyen IVR action: {Action}", option.ActionType);
                break;
        }

        return null;
    }

    private static async Task<char?> WaitForDtmfAsync(
        Func<Task<char?>> dtmfReader, int timeoutSeconds, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            return await dtmfReader();
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
}

/// <summary>IVR sonucu - nereye yonlendirilecek</summary>
public class IvrResult
{
    public IvrAction Action { get; init; }
    public int? QueueId { get; init; }
    public string? Extension { get; init; }
    public string? Reason { get; init; }

    public static IvrResult TransferToQueue(int queueId) =>
        new() { Action = IvrAction.TransferToQueue, QueueId = queueId };

    public static IvrResult TransferToExtension(string extension) =>
        new() { Action = IvrAction.TransferToExtension, Extension = extension };

    public static IvrResult Hangup(string reason) =>
        new() { Action = IvrAction.Hangup, Reason = reason };
}

public enum IvrAction
{
    TransferToQueue,
    TransferToExtension,
    Hangup
}

/// <summary>API'den gelen IVR menu bilgisi (PbxService icin)</summary>
public class IvrMenuInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? GreetingMessageId { get; set; }
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 5;
    public List<IvrMenuOptionInfo> Options { get; set; } = [];
}

public class IvrMenuOptionInfo
{
    public string Digit { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public int? TargetQueueId { get; set; }
    public string? TargetExtension { get; set; }
    public int? TargetIvrMenuId { get; set; }
    public int? TargetGreetingMessageId { get; set; }
}
