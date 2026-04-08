namespace CallCenter.Shared.Entities;

/// <summary>
/// Adisyon odeme kalemi. Bir adisyona birden fazla odeme yontemiyle odeme yapilabilir.
/// Ornegin: 80 TL nakit + 120 TL kredi karti = 200 TL toplam.
/// </summary>
public class SlnInvoicePayment
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public SlnInvoice? Invoice { get; set; }

    /// <summary>1=Nakit, 2=KrediKarti, 3=Havale, 4=Veresiye, 5=HediyeKarti, 6=Puan</summary>
    public int PaymentMethodId { get; set; }

    public decimal Amount { get; set; }

    /// <summary>POS cihazi (KK odemesinde)</summary>
    public int? PosDeviceId { get; set; }
    public SlnPosDevice? PosDevice { get; set; }

    /// <summary>Hediye karti ID (hediye karti odemesinde)</summary>
    public int? GiftCardId { get; set; }

    /// <summary>Aciklama/referans</summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
