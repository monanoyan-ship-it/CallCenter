namespace CallCenter.Shared.Entities;

public class SlnProductCategory
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public ICollection<SlnProduct> Products { get; set; } = [];
}
