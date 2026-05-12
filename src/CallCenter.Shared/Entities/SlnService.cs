namespace CallCenter.Shared.Entities;

public class SlnService
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int CategoryId { get; set; }
    public SlnServiceCategory? Category { get; set; }

    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = 30;
    public int BufferBeforeMinutes { get; set; }
    public int BufferAfterMinutes { get; set; }
    public int ProcessingMinutes { get; set; }
    public decimal Price { get; set; }
    /// <summary>KDV orani (varsayilan %10)</summary>
    public decimal TaxRate { get; set; } = 10;
    public int? ParentServiceId { get; set; }
    public SlnService? ParentService { get; set; }
    public bool IsAddOn { get; set; }
    public bool RequiresConsultation { get; set; }
    public bool RequiresPatchTest { get; set; }
    public string? PrerequisiteNotes { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public ICollection<SlnService> Variants { get; set; } = [];
    public ICollection<SlnServiceResourceRequirement> ResourceRequirements { get; set; } = [];
}
