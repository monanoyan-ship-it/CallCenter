namespace CallCenter.Shared.Entities;

public class SlnResource
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? ResourceKind { get; set; }
    public int Quantity { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SlnServiceResourceRequirement> ServiceRequirements { get; set; } = [];
}
