namespace CallCenter.Shared.Entities;

/// <summary>
/// Musteri cari hesap hareketi (borc/alacak).
/// Veresiye satislar, odemeler ve iadeler takip edilir.
/// </summary>
public class SlnClientLedger
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    /// <summary>1=Borc (satis/veresiye), 2=Alacak (odeme), 3=Iade</summary>
    public int TransactionTypeId { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Islem sonrasi bakiye</summary>
    public decimal RunningBalance { get; set; }

    /// <summary>Ilgili adisyon</summary>
    public int? InvoiceId { get; set; }
    public SlnInvoice? Invoice { get; set; }

    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
