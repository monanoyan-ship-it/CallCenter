namespace CallCenter.Shared.Entities;

/// <summary>D3 - Kayip musteri geri kazanim kurali</summary>
public class SlnWinbackRule
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>X gun gelmeyince tetiklenir</summary>
    public int InactiveDays { get; set; } = 30;

    /// <summary>1=SMS, 2=WhatsApp, 3=E-posta</summary>
    public int ChannelId { get; set; } = 1;

    public string MessageTemplate { get; set; } = string.Empty;

    /// <summary>Indirim teklif et (%)</summary>
    public int? DiscountPercent { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
