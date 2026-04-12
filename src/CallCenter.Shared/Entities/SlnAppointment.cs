namespace CallCenter.Shared.Entities;

public class SlnAppointment
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Hangi sube (null = merkez)</summary>
    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public int SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    public int PersonnelId { get; set; }
    public CustomerPersonnel? Personnel { get; set; }

    /// <summary>Eski tek hizmet FK (geriye uyumluluk). Yeni kayitlarda null, Services koleksiyonu kullanilir.</summary>
    public int? ServiceId { get; set; }
    public SlnService? Service { get; set; }

    public ICollection<SlnAppointmentService> Services { get; set; } = new List<SlnAppointmentService>();

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    /// <summary>1=Planned, 2=Confirmed, 3=Completed, 4=Cancelled, 5=NoShow</summary>
    public int StatusId { get; set; } = 1;

    public string? Notes { get; set; }

    /// <summary>Alinan depozito tutari</summary>
    public decimal DepositAmount { get; set; }
    /// <summary>Depozito iade edildi mi?</summary>
    public bool DepositRefunded { get; set; }
    /// <summary>Iptal/NoShow nedeniyle kesilen ucret</summary>
    public decimal PenaltyAmount { get; set; }

    /// <summary>Online on odeme yapildi mi?</summary>
    public bool IsPrepaid { get; set; }
    /// <summary>On odeme tutari</summary>
    public decimal PrepaidAmount { get; set; }
    /// <summary>On odeme islem referansi</summary>
    public int? PaymentTransactionId { get; set; }

    public int? CreatedByPersonnelId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
