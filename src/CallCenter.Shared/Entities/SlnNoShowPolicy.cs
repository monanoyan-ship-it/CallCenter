namespace CallCenter.Shared.Entities;

/// <summary>
/// Salon bazli no-show / iptal politikasi
/// </summary>
public class SlnNoShowPolicy
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Depozito zorunlu mu?</summary>
    public bool RequireDeposit { get; set; }

    /// <summary>Depozito tutari (sabit TL)</summary>
    public decimal DepositAmount { get; set; }

    /// <summary>Ucretsiz iptal suresi (saat). Bu sureden sonra iptal ucreti kesilir.</summary>
    public int FreeCancellationHours { get; set; } = 24;

    /// <summary>Gec iptal ucreti (sabit TL, 0=depozito kadar)</summary>
    public decimal LateCancellationFee { get; set; }

    /// <summary>No-show ucreti (sabit TL, 0=depozito kadar)</summary>
    public decimal NoShowFee { get; set; }

    /// <summary>Kara liste esigi: X kez no-show sonrasi otomatik blokla</summary>
    public int BlacklistThreshold { get; set; } = 3;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
