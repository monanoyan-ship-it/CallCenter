namespace CallCenter.Shared.Entities;

/// <summary>
/// Salon yorum/degerlendirme (dahili + Google/sosyal medya linkleri)
/// </summary>
public class SlnReview
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    public string? ClientName { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }

    /// <summary>1=Dahili, 2=Google, 3=Instagram, 4=Facebook</summary>
    public int SourceId { get; set; } = 1;
    public string? ExternalUrl { get; set; }

    /// <summary>1=Bekliyor, 2=Onaylandi, 3=Reddedildi</summary>
    public int StatusId { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Yorum talep kaydi (adisyon sonrasi otomatik gonderim)
/// </summary>
public class SlnReviewRequest
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    public int? InvoiceId { get; set; }
    public SlnInvoice? Invoice { get; set; }

    /// <summary>1=Gonderildi, 2=Acildi, 3=Yorum Yapildi</summary>
    public int StatusId { get; set; } = 1;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
