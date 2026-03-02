namespace CallCenter.Shared.Entities;

/// <summary>
/// IVR (Interactive Voice Response) menu tanimlari.
/// "1'e basin satis, 2'ye basin destek" gibi tusla yonlendirme menusu.
/// </summary>
public class IvrMenu
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();
    public int CustomerId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Menu anonsu — bu ses calar, ardindan tus beklenir</summary>
    public int? GreetingMessageId { get; set; }

    /// <summary>Gecersiz tus veya zaman asiminda tekrar denenme sayisi</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Tus bekleme suresi (saniye)</summary>
    public int TimeoutSeconds { get; set; } = 5;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Customer Customer { get; set; } = null!;
    public GreetingMessage? GreetingMessage { get; set; }
    public List<IvrMenuOption> Options { get; set; } = new();
}
