namespace CallCenter.Shared.Entities;

public class SlnServiceCategory
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SlnService> Services { get; set; } = [];
}
