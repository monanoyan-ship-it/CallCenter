namespace CallCenter.Shared.Entities;

/// <summary>
/// Adisyon iade kaydi. Kismi veya tam iade destegi.
/// Iade yapildiginda stok geri yuklenir ve kasa iade hareketi olusturulur.
/// </summary>
public class SlnInvoiceRefund
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public SlnInvoice? Invoice { get; set; }

    /// <summary>Iade tutari</summary>
    public decimal RefundAmount { get; set; }

    /// <summary>Iade yontemi: 1=Nakit, 2=KrediKarti, 3=Cari Hesap</summary>
    public int RefundMethodId { get; set; }

    /// <summary>Iade sebebi</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Iade yapan personel</summary>
    public int? PersonnelId { get; set; }
    public CustomerPersonnel? Personnel { get; set; }

    /// <summary>Ilgili kasa hareketi</summary>
    public int? CashTransactionId { get; set; }

    public DateTime RefundDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
