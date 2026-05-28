namespace CallCenter.Shared.Entities;

public class SlnGiftCard
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public string Code { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal RemainingBalance { get; set; }

    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public string? SenderName { get; set; }
    public string? Message { get; set; }

    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    public int? SoldByPersonnelId { get; set; }
    public CustomerPersonnel? SoldByPersonnel { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SlnGiftCardTransaction> Transactions { get; set; } = [];
}

public class SlnGiftCardTransaction
{
    public int Id { get; set; }
    public int GiftCardId { get; set; }
    public SlnGiftCard? GiftCard { get; set; }

    /// <summary>1=Yukleme, 2=Harcama, 3=Iade</summary>
    public int TransactionTypeId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }

    public int? RelatedInvoiceId { get; set; }
    public SlnInvoice? RelatedInvoice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
