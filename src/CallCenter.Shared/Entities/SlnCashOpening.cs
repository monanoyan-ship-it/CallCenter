namespace CallCenter.Shared.Entities;

/// <summary>
/// Kasa gun basi acilis kaydi.
/// Onceki gunun kapanisindaki tutar otomatik tasir veya manuel girilir.
/// </summary>
public class SlnCashOpening
{
    public int Id { get; set; }
    public int RegisterId { get; set; }
    public SlnCashRegister? Register { get; set; }

    public DateTime OpeningDate { get; set; }

    /// <summary>Acilis bakiyesi (onceki kapanistan veya manuel)</summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>Onceki kapanistan otomatik mi tasinmis?</summary>
    public bool IsCarriedForward { get; set; }

    /// <summary>Acilis yapan personel</summary>
    public int? OpenedByPersonnelId { get; set; }
    public CustomerPersonnel? OpenedByPersonnel { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
